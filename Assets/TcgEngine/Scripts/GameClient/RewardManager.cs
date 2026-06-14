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

                int xp_delta = GetSoloXP(ai_level, player_won);

                // Bonus criaturas supervivientes (solo en victoria)
                int creatures_alive = 0;
                if (player_won)
                {
                    creatures_alive = CountAliveCreatures(player_id);
                    int xp_per_creature = ProgressionData.Get()?.xp_creature_alive ?? 3;
                    xp_delta += creatures_alive * xp_per_creature;
                }

                string result = player_won ? "Victoria" : "Derrota";
                Debug.Log($"[RewardManager] Solo {result} (ai_level {ai_level}): {(coins_delta >= 0 ? "+" : "")}{coins_delta} WC  |  +{xp_delta} XP  (criaturas vivas: {creatures_alive})");

                if (coins_delta != 0 || xp_delta != 0)
                {
                    if (Authenticator.Get().IsTest())
                        GainSoloRewardsTest(coins_delta, xp_delta);
                    else if (Authenticator.Get().IsApi())
                        GainSoloRewardsTest(coins_delta, xp_delta);
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
        /// <summary>Monedas ganadas/perdidas en Solo según dificultad y resultado.</summary>
        public static int GetSoloWildcoins(int ai_level, bool player_won)
        {
            if (ai_level <= 0)
                return player_won ? 10 : 0;
            if (ai_level <= 5)
                return player_won ? 25 : 0;
            return player_won ? 100 : -50;
        }

        /// <summary>XP ganada en Solo según dificultad y resultado, leída de ProgressionData.</summary>
        public static int GetSoloXP(int ai_level, bool player_won)
        {
            ProgressionData pd = ProgressionData.Get();
            if (pd != null) return pd.GetSoloXP(player_won, ai_level);
            // Fallback
            if (!player_won) return 5;
            if (ai_level <= 0) return 10;
            if (ai_level <= 5) return 25;
            return 100;
        }

        /// <summary>
        /// Comprueba si esta es la primera victoria del día del jugador.
        /// Si lo es, actualiza la fecha guardada y devuelve true.
        /// </summary>
        private static bool IsFirstWinToday()
        {
            string key   = "wt_first_win_date_" + Authenticator.Get().Username;
            string today = System.DateTime.Now.ToString("yyyy-MM-dd");
            string saved = PlayerPrefs.GetString(key, "");
            if (saved == today) return false;
            PlayerPrefs.SetString(key, today);
            PlayerPrefs.Save();
            return true;
        }

        /// <summary>
        /// Cuenta las criaturas vivas del jugador al terminar la partida:
        /// - Tablero: solo las que tienen hp > 0
        /// - Mano y mazo: todas las criaturas (no han recibido daño)
        /// </summary>
        private static int CountAliveCreatures(int player_id)
        {
            Player player = GameClient.Get().GetPlayer();
            if (player == null) return 0;

            int count = 0;

            foreach (Card card in player.cards_board)
            {
                if (card.hp <= 0) continue;
                CardData cdata = CardData.Get(card.card_id);
                if (cdata != null && cdata.IsCharacter())
                    count++;
            }

            foreach (Card card in player.cards_deck)
            {
                CardData cdata = CardData.Get(card.card_id);
                if (cdata != null && cdata.IsCharacter())
                    count++;
            }

            return count;
        }

        private async void GainSoloRewardsTest(int coins_delta, int xp_delta)
        {
            UserData udata = Authenticator.Get().UserData;
            if (udata == null) return;

            if (coins_delta != 0)
                udata.coins = Mathf.Max(0, udata.coins + coins_delta);

            if (xp_delta > 0)
            {
                bool first_win_bonus = (coins_delta > 0) && IsFirstWinToday();
                if (first_win_bonus)
                {
                    int multiplier = ProgressionData.Get()?.first_win_multiplier ?? 2;
                    Debug.Log($"[RewardManager] Primera victoria del día — XP x{multiplier}: {xp_delta} → {xp_delta * multiplier}");
                    xp_delta *= multiplier;
                }
                udata.xp += xp_delta;
            }

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
