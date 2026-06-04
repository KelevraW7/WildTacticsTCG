using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;
using TcgEngine;

namespace TcgEngine.UI
{
    /// <summary>
    /// Main player UI inside the GameUI, inside the game scene
    /// there is one for each player
    /// </summary>

    public class PlayerUI : MonoBehaviour
    {
        public bool is_opponent;
        public Text pname;
        public AvatarUI avatar;

        public Animator[] secrets;

        public GameObject dead_fx;
        public AudioClip dead_audio;
        public Sprite avatar_dead;

        private bool killed = false;
        private float timer = 0f;
        private AvatarData resolved_avatar = null;
        private bool avatar_resolved = false;


        private static List<PlayerUI> ui_list = new List<PlayerUI>();

        private void Awake()
        {
            ui_list.Add(this);
        }

        private void OnDestroy()
        {
            ui_list.Remove(this);
        }

        void Start()
        {
            pname.text = "";

            for (int i = 0; i < secrets.Length; i++)
                secrets[i].gameObject.SetActive(false);

            avatar.onClick += OnClickAvatar;
            GameClient.Get().onSecretTrigger += OnSecretTrigger;
        }

        void Update()
        {
            if (!GameClient.Get().IsReady())
                return;

            Player player = GetPlayer();

            if (player != null)
            {
                pname.text = player.username;

                if (!avatar_resolved)
                {
                    if (is_opponent && GameClient.game_settings.game_type == GameType.Solo)
                    {
                        // IA: avatar aleatorio excluyendo el del jugador local
                        Player me = GameClient.Get().GetPlayer();
                        AvatarData myAvat = ResolveAvatar(me, true);
                        string excludeId = myAvat != null ? myAvat.id : "";

                        List<AvatarData> candidates = new List<AvatarData>();
                        foreach (AvatarData a in AvatarData.GetAll())
                            if (a.id != excludeId && a.avatar != null)
                                candidates.Add(a);

                        resolved_avatar = candidates.Count > 0
                            ? candidates[UnityEngine.Random.Range(0, candidates.Count)]
                            : (AvatarData.GetAll().Count > 0 ? AvatarData.GetAll()[0] : null);
                    }
                    else
                    {
                        resolved_avatar = ResolveAvatar(player, !is_opponent);
                    }

                    avatar_resolved = resolved_avatar != null;
                }

                if (avatar != null && resolved_avatar != null && !killed)
                    avatar.SetAvatar(resolved_avatar);
            }


            timer += Time.deltaTime;
            if (timer > 0.4f)
            {
                timer = 0f;
                SlowUpdate();
            }
        }

        /// <summary>
        /// Resuelve el AvatarData de un jugador con cadena de fallback:
        /// avatar asignado → UserData (solo jugador local) → primer avatar disponible.
        /// </summary>
        private AvatarData ResolveAvatar(Player player, bool isLocalPlayer)
        {
            // 1. Avatar asignado en la partida
            AvatarData adata = AvatarData.Get(player.avatar);
            if (adata != null) return adata;

            // 2. Avatar guardado en UserData (solo jugador local)
            if (isLocalPlayer)
            {
                UserData udata = Authenticator.Get()?.UserData;
                if (udata != null)
                {
                    adata = AvatarData.Get(udata.avatar);
                    if (adata != null) return adata;
                }
            }

            // 3. Primer avatar disponible
            List<AvatarData> all = AvatarData.GetAll();
            if (all.Count > 0) return all[0];

            return null;
        }

        void SlowUpdate()
        {
            Player player = GetPlayer();
            if (player == null)
                return;

            for (int i = 0; i < secrets.Length; i++)
            {
                bool active = i < player.cards_secret.Count;
                bool was_active = secrets[i].gameObject.activeSelf;
                if (active != was_active)
                    secrets[i].gameObject.SetActive(active);
                if (active && !was_active)
                    secrets[i].SetTrigger("appear");
                if (active && !was_active && !is_opponent)
                    secrets[i].GetComponent<SecretIconUI>().SetCard(player.cards_secret[i]);
                if (!active && was_active)
                    secrets[i].Rebind();
            }
        }

        public void Kill()
        {
            killed = true;
            avatar.SetImage(avatar_dead);
            AudioTool.Get().PlaySFX("fx", dead_audio);
            FXTool.DoFX(dead_fx, avatar.transform.position);
        }

        private void OnClickAvatar(AvatarData avatar)
        {

        }

        private void OnSecretTrigger(Card secret, Card triggerer)
        {
            Player player = GetPlayer();
            int index = player.cards_secret.Count - 1;
            if (player.player_id == secret.player_id && index >= 0 && index < secrets.Length)
            {
                secrets[index].SetTrigger("reveal");
            }
        }

        public Player GetPlayer()
        {
            int player_id = is_opponent ? GameClient.Get().GetOpponentPlayerID() : GameClient.Get().GetPlayerID();
            Game data = GameClient.Get().GetGameData();
            return data.GetPlayer(player_id);
        }

        public static PlayerUI Get(bool opponent)
        {
            foreach (PlayerUI ui in ui_list)
            {
                if (ui.is_opponent == opponent)
                    return ui;
            }
            return null;
        }

    }
}
