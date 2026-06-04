using System.Collections.Generic;
using TcgEngine.Client;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// Endgame panel is shown when a game end
    /// Showing winner and rewards obtained
    /// </summary>

    public class EndGamePanel : UIPanel
    {
        public Text winner_text;
        public Image winner_glow;

        public Text player_name;
        public Text other_name;
        public Image player_avatar;
        public Image other_avatar;

        public Text coins_text;
        public Text xp_text;

        private bool reward_loaded = false;
        private float timer = 0f;

        private int target_coins = 0;
        private int target_xp = 0;
        private float coins = 0;
        private float xp = 0;
        private int last_winner_id = -1;

        // ── Animación de WildCoins ────────────────────────────────────────────────
        private float coins_pop_timer = -1f;   // -1 = sin animar
        private const float COINS_POP_DUR = 0.45f;

        private static EndGamePanel _instance;

        protected override void Awake()
        {
            base.Awake();
            _instance = this;
        }

        protected override void Start()
        {
            base.Start();

            coins_text.text = "";
            xp_text.text = "";

        }

        protected override void Update()
        {
            base.Update();

            if (!reward_loaded && IsVisible())
            {
                timer += Time.deltaTime;
                if (timer > 1f)
                {
                    timer = 0f;
                    RefreshRewards();
                }
            }

            if (reward_loaded)
            {
                coins = Mathf.MoveTowards(coins, target_coins, 2000f * Time.deltaTime);
                xp    = Mathf.MoveTowards(xp,    target_xp,    500f  * Time.deltaTime);

                int    coins_int = Mathf.RoundToInt(coins);
                bool   is_solo   = GameClient.game_settings.game_type == GameType.Solo;
                string unit      = is_solo ? "WildCoins" : "coins";

                bool had_text = !string.IsNullOrEmpty(coins_text.text);

                if (coins_int > 0)
                    coins_text.text = "+ " + coins_int + " " + unit;
                else if (coins_int < 0)
                    coins_text.text = "− " + Mathf.Abs(coins_int) + " " + unit;
                else
                    coins_text.text = "";

                // Disparar pop-in la primera vez que aparece el texto de monedas
                if (!had_text && !string.IsNullOrEmpty(coins_text.text) && coins_pop_timer < 0f)
                {
                    coins_pop_timer = 0f;
                    coins_text.transform.localScale = Vector3.zero;
                }

                // Animación pop-in (EaseOutBack: crece de 0 a 1 con rebote)
                if (coins_pop_timer >= 0f)
                {
                    coins_pop_timer += Time.deltaTime;
                    float t     = Mathf.Clamp01(coins_pop_timer / COINS_POP_DUR);
                    float scale = EaseOutBack(t, 0f, 1f);
                    coins_text.transform.localScale = Vector3.one * scale;
                    if (t >= 1f) coins_pop_timer = -1f;
                }
                // Pulso suave mientras el contador sigue subiendo
                else if (Mathf.Abs(coins - target_coins) > 0.5f && target_coins != 0)
                {
                    float pulse = 1f + 0.04f * Mathf.Sin(Time.time * 14f);
                    coins_text.transform.localScale = Vector3.one * pulse;
                }
                else
                {
                    coins_text.transform.localScale = Vector3.one;
                }

                int xp_int = Mathf.RoundToInt(xp);
                xp_text.text = xp_int > 0 ? ("+ " + xp_int + " xp") : "";
            }
        }

        private void RefreshPanel(int winner)
        {
            Game data = GameClient.Get().GetGameData();
            Player pwinner = data.GetPlayer(winner);
            Player player = GameClient.Get().GetPlayer();
            Player oplayer = GameClient.Get().GetOpponentPlayer();

            player_name.text = player.username;
            other_name.text = oplayer.username;

            // ── Avatar del jugador ────────────────────────────────────────────
            // Intenta el avatar activo en partida; si fue borrado, usa el del UserData;
            // si tampoco existe, coge el primero disponible.
            AvatarData avat1 = AvatarData.Get(player.avatar);
            if (avat1 == null)
            {
                UserData udata = Authenticator.Get()?.UserData;
                if (udata != null) avat1 = AvatarData.Get(udata.avatar);
            }
            if (avat1 == null && AvatarData.GetAll().Count > 0)
                avat1 = AvatarData.GetAll()[0];
            if (avat1 != null)
                player_avatar.sprite = avat1.avatar;

            // ── Avatar del oponente ───────────────────────────────────────────
            bool is_solo = GameClient.game_settings.game_type == GameType.Solo;
            if (is_solo)
            {
                // IA: avatar aleatorio de todos los disponibles, distinto al del jugador
                string exclude_id = avat1 != null ? avat1.id : "";
                List<AvatarData> candidates = new List<AvatarData>();
                foreach (AvatarData a in AvatarData.GetAll())
                    if (a.id != exclude_id && a.avatar != null)
                        candidates.Add(a);
                AvatarData avat2 = candidates.Count > 0
                    ? candidates[Random.Range(0, candidates.Count)]
                    : null;
                if (avat2 != null)
                    other_avatar.sprite = avat2.avatar;
            }
            else
            {
                // Online: usar el avatar real del oponente
                AvatarData avat2 = AvatarData.Get(oplayer.avatar);
                if (avat2 != null)
                    other_avatar.sprite = avat2.avatar;
            }

            if (pwinner != null && pwinner == player)
                winner_text.text = "VICTORIA";
            else if (pwinner != null)
                winner_text.text = "DERROTA";
            else
                winner_text.text = "EMPATE";

            if (pwinner == player)
                winner_glow.rectTransform.anchoredPosition = player_avatar.rectTransform.anchoredPosition;
            if (pwinner == oplayer)
                winner_glow.rectTransform.anchoredPosition = other_avatar.rectTransform.anchoredPosition;
            winner_glow.gameObject.SetActive(pwinner != null);
        }

        private async void RefreshRewards()
        {
            //Online rewards
            if (GameClient.game_settings.IsOnline())
            {
                string url = ApiClient.ServerURL + "/matches/" + GameClient.game_settings.game_uid;
                WebResponse res = await ApiClient.Get().SendGetRequest(url);
                if (res.success)
                {
                    reward_loaded = true;
                    MatchResponse match = ApiTool.JsonToObject<MatchResponse>(res.data);
                    string username = ApiClient.Get().Username.ToLower();
                    foreach (MatchDataResponse data in match.udata)
                    {
                        if (data.username.ToLower() == username)
                        {
                            target_coins = data.reward.coins;
                            target_xp = data.reward.xp;
                        }
                    }
                }
            }

            //Adventure Rewards
            if (GameClient.game_settings.game_type == GameType.Adventure)
            {
                LevelData lvl = LevelData.Get(GameClient.game_settings.level);
                if (lvl != null && RewardManager.Get().IsRewardGained())
                {
                    target_coins = lvl.reward_coins;
                    target_xp = lvl.reward_xp;
                    reward_loaded = true;
                }
            }

            // Solo wildcoins
            if (GameClient.game_settings.game_type == GameType.Solo)
            {
                Player player   = GameClient.Get().GetPlayer();
                bool player_won = (player != null && last_winner_id == player.player_id);
                int  ai_level   = GameClient.ai_settings.ai_level;
                target_coins    = RewardManager.GetSoloWildcoins(ai_level, player_won);
                reward_loaded   = true;
            }
        }

        public void ShowEnd(int winner)
        {
            last_winner_id  = winner;
            reward_loaded   = false;
            coins           = 0;
            xp              = 0;
            target_coins    = 0;
            target_xp       = 0;
            coins_pop_timer = -1f;
            coins_text.transform.localScale = Vector3.one;
            RefreshPanel(winner);
            RefreshRewards();
            Show();
        }

        /// <summary>Curva EaseOutBack: crece de <c>from</c> a <c>to</c> con rebote leve al final.</summary>
        private static float EaseOutBack(float t, float from, float to)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float ease = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
            return Mathf.LerpUnclamped(from, to, ease);
        }

        public void OnClickQuit()
        {
            GameUI.Get().OnClickQuit();
        }

        public static EndGamePanel Get()
        {
            return _instance;
        }
    }
}
