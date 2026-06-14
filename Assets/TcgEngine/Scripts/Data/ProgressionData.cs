using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// Configuración centralizada del sistema de progresión del jugador.
    /// Crea el asset en Assets/Resources/ con el nombre exacto "ProgressionData".
    /// Menú: TcgEngine → ProgressionData
    /// </summary>
    [CreateAssetMenu(fileName = "ProgressionData", menuName = "TcgEngine/ProgressionData", order = 8)]
    public class ProgressionData : ScriptableObject
    {
        [Header("XP por partida Solo")]
        public int xp_win_easy   = 10;
        public int xp_win_casual = 25;
        public int xp_win_expert = 100;
        public int xp_loss       = 5;

        [Header("XP por otras acciones")]
        public int xp_pack_open       = 20;
        public int xp_new_card        = 25;
        public int xp_creature_alive  = 3;

        [Header("Bonus primera victoria del día (multiplicador)")]
        public int first_win_multiplier = 2;

        [Header("XP necesaria por nivel  —  índice 0 = nivel 1→2")]
        [Tooltip("Edita aquí para rebalancear sin tocar código. Para niveles fuera del array se aplica un escalado automático.")]
        public int[] xp_per_level =
        {
              500,   1000,   1500,   2500,   3500,   5000,   6500,   // niveles 1-7
             8750,  11000,  13250,  15500,  17750,  20000,  22250,   // niveles 8-14
            24750,  27250,  29750,  32250,  34750,                   // niveles 15-19
            37750,  40750,  43750,  46750,  49750,                   // niveles 20-24
            53250,  56750,  60250,  63750,  67250,                   // niveles 25-29
        };

        // ── Singleton ─────────────────────────────────────────────────────────

        private static ProgressionData _instance;

        public static ProgressionData Get()
        {
            if (_instance == null)
                _instance = Resources.Load<ProgressionData>("ProgressionData");
            return _instance;
        }

        // ── API ───────────────────────────────────────────────────────────────

        /// <summary>XP necesaria para subir del nivel <paramref name="level"/> al siguiente.</summary>
        public int GetXpForLevel(int level)
        {
            int index = level - 1;
            if (xp_per_level != null && index >= 0 && index < xp_per_level.Length)
                return xp_per_level[index];

            // Para niveles más allá del array: escala desde el último valor (+3500/nivel)
            int last   = (xp_per_level != null && xp_per_level.Length > 0) ? xp_per_level[xp_per_level.Length - 1] : 67250;
            int beyond = index - (xp_per_level != null ? xp_per_level.Length - 1 : 28);
            return last + Mathf.Max(0, beyond) * 3500;
        }

        /// <summary>XP ganada en Solo según resultado y nivel de IA.</summary>
        public int GetSoloXP(bool player_won, int ai_level)
        {
            if (!player_won) return xp_loss;
            if (ai_level <= 0) return xp_win_easy;
            if (ai_level <= 5) return xp_win_casual;
            return xp_win_expert;
        }
    }
}
