using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TcgEngine.Client;

namespace TcgEngine.UI
{
    /// <summary>
    /// Panel de selección de dificultad para la partida Solo.
    ///
    /// Jerarquía esperada en Unity:
    ///   DifficultyPanel  (este script + UIPanel + CanvasGroup + VerticalLayoutGroup)
    ///   ├── Row_Easy        (Button + Image fondo + HorizontalLayoutGroup)
    ///   │   ├── Label       (TMP_Text — "FÁCIL")
    ///   │   ├── Desc        (TMP_Text — descripción)
    ///   │   └── Reward      (TMP_Text — wildcoins)
    ///   ├── Row_Medium      (ídem)
    ///   │   ├── Label / Desc / Reward
    ///   ├── Row_Competitive (ídem)
    ///   │   ├── Label / Desc / Reward
    ///   └── Btn_Cancel      (Button simple)
    ///
    /// Modos y economía (ver RewardManager.GetSoloWildcoins):
    ///   Fácil       → ai_level = 0  → AIPlayerRandom → +1 wc / 0 derrota
    ///   Medio       → ai_level = 3  → AIPlayerMedium → +10 wc / 0 derrota
    ///   Competitivo → ai_level = 10 → AIPlayerMM     → +100 wc / -50 derrota
    /// </summary>
    public class DifficultyPanel : UIPanel
    {
        // ── Referencias a las filas completas (botones clicables) ─────────────────
        [Header("Filas de dificultad (botones)")]
        public Button row_easy;
        public Button row_casual;
        public Button row_competitive;
        public Button btn_cancel;

        // ── Fondos de cada fila (Image para el tinte de color) ───────────────────
        [Header("Fondos de fila (Image)")]
        public Image bg_easy;
        public Image bg_casual;
        public Image bg_competitive;

        // ── Iconos decorativos al inicio de cada fila ─────────────────────────────
        [Header("Iconos de dificultad")]
        public Image icon_easy;
        public Image icon_casual;
        public Image icon_competitive;

        // ── Textos descriptivos y de recompensa ───────────────────────────────────
        [Header("Textos descriptivos")]
        public TMP_Text desc_easy;
        public TMP_Text desc_casual;
        public TMP_Text desc_competitive;

        [Header("Textos de recompensa")]
        public TMP_Text reward_easy;
        public TMP_Text reward_casual;
        public TMP_Text reward_competitive;

        // ── Colores de fondo por dificultad ───────────────────────────────────────
        [Header("Colores de fila")]
        public Color color_easy        = new Color(0.20f, 0.55f, 0.20f, 0.80f);
        public Color color_casual      = new Color(0.65f, 0.50f, 0.10f, 0.80f);
        public Color color_competitive = new Color(0.60f, 0.12f, 0.12f, 0.80f);
        public Color color_selected    = new Color(1.00f, 1.00f, 1.00f, 0.15f); // extra brillo al seleccionado

        // ── Estado ────────────────────────────────────────────────────────────────
        private static int s_selected_level = 0; // por defecto: Fácil

        public static int SelectedAILevel => s_selected_level;

        private static DifficultyPanel _instance;
        public  static DifficultyPanel Get() => _instance;

        // ── Inicialización ────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            _instance = this;

            // Textos descriptivos — sincronizados con los valores reales de RewardManager
            SetDescriptions();

            // Colores iniciales de fondo
            ApplyColors();
        }

        private void SetDescriptions()
        {
            if (desc_easy        != null) desc_easy.text        = "IA aleatoria. Aprende a jugar sin riesgo.";
            if (desc_casual      != null) desc_casual.text      = "IA con ventaja de tipo. Partida casual.";
            if (desc_competitive != null) desc_competitive.text = "IA al máximo. Arriesga tus WildCoins.";

            if (reward_easy        != null) reward_easy.text        = "+1 WC";
            if (reward_casual      != null) reward_casual.text      = "+10 WC";
            if (reward_competitive != null) reward_competitive.text = "+100 WC\n−50 WC";
        }

        private void ApplyColors()
        {
            if (bg_easy        != null) bg_easy.color        = color_easy;
            if (bg_casual      != null) bg_casual.color      = color_casual;
            if (bg_competitive != null) bg_competitive.color = color_competitive;
        }

        private void HighlightSelected(Image selected_bg)
        {
            // Restaura todos y resalta el seleccionado
            ApplyColors();
            if (selected_bg != null)
                selected_bg.color = selected_bg.color + color_selected;
        }

        // ── Botones ───────────────────────────────────────────────────────────────

        public void OnClickEasy()
        {
            s_selected_level = 0;
            HighlightSelected(bg_easy);
            StartSolo();
        }

        public void OnClickCasual()
        {
            s_selected_level = 3;
            HighlightSelected(bg_casual);
            StartSolo();
        }

        public void OnClickCompetitive()
        {
            s_selected_level = 10;
            HighlightSelected(bg_competitive);
            StartSolo();
        }

        public void OnClickCancel()
        {
            Hide();
        }

        // ── Lanzar partida ────────────────────────────────────────────────────────

        private void StartSolo()
        {
            Hide();
            GameClient.ai_settings.ai_level = s_selected_level;
            GameClient.game_settings.scene  = GameplayData.Get().GetRandomArena();
            MainMenu.Get().StartGame(GameType.Solo, GameMode.Casual);
        }
    }
}
