using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.Client
{
    /// <summary>
    /// Gestiona el fondo del tablero en función del escenario activo.
    ///
    /// Sistema híbrido (Opción C):
    ///   · Tinte de color   → siempre funciona, sin assets adicionales.
    ///                        Lerp suave entre el color del escenario anterior y el nuevo.
    ///   · Arte de fondo    → opcional por escenario (Sprite en CardData.scenario_background).
    ///                        Se hace crossfade suave cuando cambia el escenario.
    ///
    /// Jerarquía de Canvas recomendada (de atrás hacia adelante):
    ///   [0] default_background  — fondo base del tablero (siempre visible)
    ///   [1] scenario_layer      — sprite de arte del escenario (puede ser null → alpha 0)
    ///   [2] color_overlay       — Image de color semitransparente encima de todo
    ///   [3] ...resto de la UI del tablero (criaturas, mazos, etc.)
    ///
    /// Setup en Unity:
    ///   · Añade este script a un GameObject vacío en la escena de juego.
    ///   · Asigna las tres Images en el Inspector.
    ///   · color_overlay debe tener color inicial (0,0,0,0) y raycast target = OFF.
    ///   · scenario_layer debe tener color inicial (1,1,1,0) y raycast target = OFF.
    /// </summary>
    public class BoardBackground : MonoBehaviour
    {
        [Header("Capas de fondo (orden de Canvas: default → scenario → overlay)")]
        [Tooltip("Fondo base del tablero — siempre visible, nunca cambia.")]
        public Image default_background;

        [Tooltip("Capa donde se pinta el arte de fondo del escenario (Sprite opcional). " +
                 "Tamaño: mismo que default_background, stretch to fill.")]
        public Image scenario_layer;

        [Tooltip("Overlay semitransparente de color encima de todo. " +
                 "Color inicial: (0,0,0,0). Raycast target: OFF.")]
        public Image color_overlay;

        [Header("Transición")]
        [Tooltip("Velocidad del lerp de color y alpha (unidades/segundo).")]
        public float fade_speed    = 2.5f;

        [Tooltip("Alpha máximo del overlay de color cuando hay escenario activo (0–1).")]
        [Range(0f, 0.6f)]
        public float overlay_alpha = 0.28f;

        // ── Estado interno ─────────────────────────────────────────────────────────
        private string current_scenario_id = "";   // ID del escenario actualmente cargado
        private Color  target_overlay;             // Color destino del overlay
        private float  target_art_alpha;           // Alpha destino de scenario_layer

        // ─────────────────────────────────────────────────────────────────────────

        void Start()
        {
            // Garantizar estado inicial limpio
            if (color_overlay  != null) color_overlay.color  = new Color(0, 0, 0, 0);
            if (scenario_layer != null) scenario_layer.color = new Color(1, 1, 1, 0);
            target_overlay    = new Color(0, 0, 0, 0);
            target_art_alpha  = 0f;
        }

        void Update()
        {
            if (!GameClient.Get().IsReady()) return;

            CardData scenario = GameClient.Get().GetGameData()?.GetScenarioData();
            string new_id = scenario != null ? scenario.id : "";

            // ── Detectar cambio de escenario ──────────────────────────────────────
            if (new_id != current_scenario_id)
            {
                current_scenario_id = new_id;
                OnScenarioChanged(scenario);
            }

            // ── Lerp color overlay ────────────────────────────────────────────────
            if (color_overlay != null)
                color_overlay.color = Color.Lerp(color_overlay.color, target_overlay,
                                                 Time.deltaTime * fade_speed);

            // ── Lerp alpha del arte de fondo ──────────────────────────────────────
            if (scenario_layer != null)
            {
                Color c = scenario_layer.color;
                c.a = Mathf.Lerp(c.a, target_art_alpha, Time.deltaTime * fade_speed);
                scenario_layer.color = c;

                // Cuando el fade-out ha terminado, limpiamos el sprite para ahorrar memoria
                if (target_art_alpha < 0.01f && c.a < 0.01f)
                    scenario_layer.sprite = null;
            }
        }

        // ── Aplicar nuevo escenario ────────────────────────────────────────────────

        private void OnScenarioChanged(CardData scenario)
        {
            if (scenario == null)
            {
                // Sin escenario: desvanecer todo
                target_overlay   = new Color(0, 0, 0, 0);
                target_art_alpha = 0f;
                return;
            }

            // ── Color overlay ─────────────────────────────────────────────────────
            // scenario_color en el Inspector del escenario es el color base (RGB).
            // Forzamos el alpha al overlay_alpha configurado en este componente.
            Color oc = scenario.scenario_color;
            target_overlay = new Color(oc.r, oc.g, oc.b, overlay_alpha);

            // ── Arte de fondo (opcional) ──────────────────────────────────────────
            if (scenario_layer != null && scenario.scenario_background != null)
            {
                // Asignamos el nuevo sprite ya con alpha 0 para que el lerp lo suba suave
                scenario_layer.sprite = scenario.scenario_background;
                Color c = scenario_layer.color;
                c.a = 0f;
                scenario_layer.color = c;
                target_art_alpha = 1f;
            }
            else
            {
                // Sin arte de fondo: solo el overlay de color
                target_art_alpha = 0f;
            }
        }

        // ── Acceso estático opcional ───────────────────────────────────────────────

        private static BoardBackground _instance;
        public  static BoardBackground Get() => _instance;

        void Awake() { _instance = this; }
    }
}
