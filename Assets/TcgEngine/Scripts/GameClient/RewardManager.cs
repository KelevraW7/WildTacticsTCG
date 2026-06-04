using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TcgEngine;

namespace TcgEngine.Client
{
    /// <summary>
    /// Grants rewards after a game ends.
    ///
    /// Adventure mode: one-time card/pack/coin rewards per level (existing logic).
    ///
    /// Solo mode — wildcoins per result and difficulty:
    ///   Fácil       (ai_level 0)   : +10 win  /  0 loss
    ///   Medio       (ai_level 1-5) : +25 win  /  0 loss
    ///   Competitivo (ai_level 6+)  : +100 win / -50 loss  (floor at 0)
    /// </summary>
    public class RewardManager : MonoBehaviour
    {
        private bool reward_gained = false;

        private static RewardManager instance;

        void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            GameClient.Get().onGameEnd += OnGameEnd;
        }

        void OnGameEnd(int winner)
        {
            int player_id = GameClient.Get().GetPlayerID();

            // ── Adventure reward (existing) ──────────────────────────────────────
            if (GameClient.game_settings.game_type == GameType.Adventure && winner == player_id)
            {
                UserData udata = Authenticator.Get().UserData;
                LevelData level = LevelData.Get(GameClient.game_settings.level);
                if (level != null && !udata.HasReward(level.id) && !reward_gained)
                {
                    if (Authenticator.Get().IsTest())
                        GainRewardTest(level);
                    if (Authenticator.Get().IsApi())
                        GainRewardAPI(level);
                }
            }

            // ── Solo wildcoins reward ─────────────────────────────────────────────
            if (GameClient.game_settings.game_type == GameType.Solo && !reward_gained)
            {
                bool player_won   = (winner == player_id);
                int  ai_level     = GameClient.ai_settings.ai_level;
                int  coins_delta  = GetSoloWildcoins(ai_level, player_won);

                // La partida terminó normalmente — limpiar flag de cierre forzado y guardar
                PlayerPrefs.DeleteKey("wt_competitive_pending");
                PlayerPrefs.Save();

                // Actualizar estadísticas de partida
                UpdateMatchStats(player_won, ai_level);

                if (coins_delta != 0)
                {
                    string result = player_won ? "Victoria" : "Derrota";
                    Debug.Log($"[RewardManager] Solo {result} (ai_level {ai_level}): {(coins_delta >= 0 ? "+" : "")}{coins_delta} wildcoins");

                    if (Authenticator.Get().IsTest())
                        GainSoloWildcoinsTest(coins_delta);
                    else if (Authenticator.Get().IsApi())
                        GainSoloWildcoinsTest(coins_delta); // Misma lógica — sin endpoint dedicado aún
                }
            }
        }

        // ── Match stats + Avatar unlocks ──────────────────────────────────────────

        /// <summary>
        /// Actualiza victorias/partidas en UserData y comprueba si se deben
        /// desbloquear avatares automáticamente por estadísticas.
        /// </summary>
        private async void UpdateMatchStats(bool player_won, int ai_level)
        {
            UserData udata = Authenticator.Get()?.UserData;
            if (udata == null) return;

            udata.matches++;
            bool is_competitive = ai_level >= 10;
            if (player_won)
            {
                udata.victories++;
                if (is_competitive)
                    udata.competitive_victories++;
            }
            else
            {
                udata.defeats++;
                if (is_competitive)
                    udata.competitive_defeats++;
            }

            // Comprobar desbloqueos de avatares por estadísticas
            bool any_unlocked = false;
            foreach (AvatarData adata in AvatarData.GetAll())
            {
                if (adata.unlock_type == AvatarUnlockType.Default) continue;
                if (adata.unlock_type == AvatarUnlockType.Shop)    continue;
                if (udata.HasAvatar(adata.id))                     continue;
                if (adata.ShouldAutoUnlock(udata))
                {
                    udata.AddAvatar(adata.id);
                    Debug.Log($"[RewardManager] Avatar desbloqueado: {adata.id}");
                    any_unlocked = true;
                }
            }

            if (any_unlocked || player_won)
                await Authenticator.Get().SaveUserData();
        }

        // ── Wildcoins table ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns the wildcoins delta for a Solo game result.
        /// Positive = earn coins. Negative = lose coins (Competitivo only).
        /// </summary>
        public static int GetSoloWildcoins(int ai_level, bool player_won)
        {
            if (ai_level <= 0)
                return player_won ? 10 : 0;        // Fácil
            if (ai_level <= 5)
                return player_won ? 25 : 0;        // Medio
            return player_won ? 100 : -50;         // Competitivo
        }

        private async void GainSoloWildcoinsTest(int coins_delta)
        {
            UserData udata = Authenticator.Get().UserData;
            if (udata == null) return;
            udata.coins = Mathf.Max(0, udata.coins + coins_delta); // nunca por debajo de 0
            reward_gained = true;
            await Authenticator.Get().SaveUserData();
        }

        private async void GainRewardTest(LevelData level)
        {
            VariantData variant = VariantData.GetDefault();
            UserData udata = Authenticator.Get().UserData;
            udata.coins += level.reward_coins;
            udata.xp += level.reward_xp;
            udata.AddReward(level.id);

            foreach (CardData card in level.reward_cards)
            {
                udata.AddCard(card.id, variant.id, 1);
            }

            foreach (PackData pack in level.reward_packs)
            {
                udata.AddPack(pack.id, 1);
            }

            reward_gained = true;
            await Authenticator.Get().SaveUserData();
        }

        private async void GainRewardAPI(LevelData level)
        {
            bool success = await GainRewardAPI(level.id);
            reward_gained = success;
        }

        public async Task<bool> GainRewardAPI(string reward_id)
        {
            RewardGainRequest req = new RewardGainRequest();
            req.reward = reward_id;

            string url = ApiClient.ServerURL + "/users/rewards/gain/" + ApiClient.Get().UserID;
            string json = ApiTool.ToJson(req);
            WebResponse res = await ApiClient.Get().SendPostRequest(url, json);
            Debug.Log("Gain Reward: " + reward_id + " " + res.success);
            return res.success;
        }

        /// <summary>
        /// Aplica la penalización de abandono en modo Competitivo Solo.
        /// Llamado cuando el jugador confirma salir manualmente antes de que la partida termine.
        /// </summary>
        public void ApplyAbandonPenalty()
        {
            if (reward_gained) return;   // ya se procesó recompensa/penalización esta partida
            if (GameClient.game_settings.game_type != GameType.Solo) return;

            int ai_level    = GameClient.ai_settings.ai_level;
            int coins_delta = GetSoloWildcoins(ai_level, false);   // false = derrota

            Debug.Log($"[RewardManager] ApplyAbandonPenalty — ai_level={ai_level} delta={coins_delta} IsTest={Authenticator.Get().IsTest()}");

            if (coins_delta != 0)
            {
                // Modificar monedas directamente y forzar guardado síncrono
                UserData udata = Authenticator.Get().UserData;
                if (udata != null)
                {
                    udata.coins = Mathf.Max(0, udata.coins + coins_delta);
                    reward_gained = true;
                    Debug.Log($"[RewardManager] Coins tras abandono: {udata.coins}");
                    _ = Authenticator.Get().SaveUserData();
                }
            }

            // Abandono manual registrado — limpiar flag de cierre forzado y forzar guardado en disco
            PlayerPrefs.DeleteKey("wt_competitive_pending");
            PlayerPrefs.Save();
        }

        public bool IsRewardGained()
        {
            return reward_gained;
        }

        public static RewardManager Get()
        {
            return instance;
        }
    }
}
