using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace TcgEngine.Client
{
    /// <summary>
    /// Grants rewards after a game ends.
    ///
    /// Adventure mode: one-time card/pack/coin rewards per level (existing logic).
    ///
    /// Solo mode — wildcoins per result and difficulty:
    ///   Fácil       (ai_level 0)   : +1 win  /  0 loss
    ///   Medio       (ai_level 1-5) : +10 win  /  0 loss
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

                if (coins_delta != 0)
                {
                    string result = player_won ? "Victoria" : "Derrota";
                    Debug.Log($"[RewardManager] Solo {result} (ai_level {ai_level}): {(coins_delta >= 0 ? "+" : "")}{coins_delta} wildcoins");

                    if (Authenticator.Get().IsTest())
                        GainSoloWildcoinsTest(coins_delta);
                    if (Authenticator.Get().IsApi())
                        GainSoloWildcoinsTest(coins_delta); // Misma lógica — sin endpoint dedicado aún
                }
            }
        }

        // ── Wildcoins table ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns the wildcoins delta for a Solo game result.
        /// Positive = earn coins. Negative = lose coins (Competitivo only).
        /// </summary>
        public static int GetSoloWildcoins(int ai_level, bool player_won)
        {
            if (ai_level <= 0)
                return player_won ? 1 : 0;         // Fácil
            if (ai_level <= 5)
                return player_won ? 10 : 0;        // Medio
            return player_won ? 100 : -50;         // Competitivo
        }

        private async void GainSoloWildcoinsTest(int coins_delta)
        {
            UserData udata = Authenticator.Get().UserData;
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
