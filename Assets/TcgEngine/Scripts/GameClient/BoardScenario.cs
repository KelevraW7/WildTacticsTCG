using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TcgEngine.UI;

namespace TcgEngine.Client
{
    /// <summary>
    /// Slot visual del escenario activo: aparece en el lado izquierdo del tablero.
    ///
    /// Muestra el board art del escenario + su nombre.
    /// Hover → CardPreviewUI muestra la carta completa (texto del efecto, etc.).
    /// No es clickable para jugar — el escenario es permanente durante la partida.
    ///
    /// Setup en Unity:
    ///   1. Crear un GameObject con RectTransform en el Canvas, lado izquierdo del tablero.
    ///   2. Añadir este script + asignar board_image, title_label, empty_label.
    ///   3. El GameObject puede tener un CanvasGroup para aparecer/desaparecer suavemente.
    ///
    /// Estructura de prefab recomendada (misma proporción que board cards de criaturas,
    /// pero ligeramente más pequeño, ej. 140 × 140 px):
    ///   ScenarioSlot (RectTransform)
    ///   ├─ EmptyFrame     (Image – borde punteado visible cuando no hay escenario)
    ///   ├─ BoardImage     (Image – art_board del escenario activo)
    ///   ├─ HoverHighlight (Image – borde luminoso al hacer hover)
    ///   └─ TitleLabel     (Text  – nombre del escenario)
    /// </summary>
    public class BoardScenario : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Referencias del prefab")]
        public Image  board_image;       // art_board del escenario activo
        public Text   title_label;       // Nombre del escenario
        public Image  hover_highlight;   // Borde luminoso al hacer hover (puede ser null)
        public GameObject empty_frame;   // Visible cuando no hay escenario activo

        [Header("Animación")]
        public float hover_scale = 1.08f;
        public float anim_speed  = 8f;

        // ── Estado ─────────────────────────────────────────────────────────────────
        private bool hovering;
        private Vector3 base_scale;

        // ── Foco global leído por CardPreviewUI ────────────────────────────────────
        private static BoardScenario s_focused;

        /// <summary>Devuelve la CardData del escenario si el cursor está encima, null si no.</summary>
        public static CardData GetFocusData()
        {
            if (s_focused == null) return null;
            return GameClient.Get()?.GetGameData()?.GetScenarioData();
        }

        private static BoardScenario _instance;
        public  static BoardScenario Get() => _instance;

        // ──────────────────────────────────────────────────────────────────────────

        void Awake()
        {
            _instance  = this;
            base_scale = transform.localScale;
        }

        void Update()
        {
            if (!GameClient.Get().IsReady()) return;

            CardData scenario = GameClient.Get().GetGameData()?.GetScenarioData();
            bool has_scenario = scenario != null;

            // ── Board art ─────────────────────────────────────────────────────────
            if (board_image != null)
            {
                board_image.enabled = has_scenario;
                if (has_scenario)
                    board_image.sprite = scenario.art_board;
            }

            // ── Título ────────────────────────────────────────────────────────────
            if (title_label != null)
            {
                title_label.enabled = has_scenario;
                if (has_scenario)
                    title_label.text = scenario.GetTitle();
            }

            // ── Marco vacío (guía visual cuando no hay escenario aún) ─────────────
            if (empty_frame != null)
                empty_frame.SetActive(!has_scenario);

            // ── Highlight ─────────────────────────────────────────────────────────
            if (hover_highlight != null)
                hover_highlight.enabled = hovering && has_scenario;

            // ── Scale hover ───────────────────────────────────────────────────────
            float target_s = (hovering && has_scenario) ? hover_scale : 1f;
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                base_scale * target_s,
                Time.deltaTime * anim_speed);
        }

        // ── EventSystem ───────────────────────────────────────────────────────────

        public void OnPointerEnter(PointerEventData _)
        {
            hovering  = true;
            s_focused = this;
        }

        public void OnPointerExit(PointerEventData _)
        {
            hovering = false;
            if (s_focused == this) s_focused = null;
        }

        private void OnDisable()
        {
            hovering = false;
            if (s_focused == this) s_focused = null;
        }
    }
}
