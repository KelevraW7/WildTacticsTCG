using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TcgEngine.FX;

namespace TcgEngine.UI
{
    /// <summary>
    /// Panel "En desarrollo" — aparece en el lado derecho del menú principal cuando
    /// el jugador hace clic en DESAFÍO u ONLINE, indicando que ese modo llegará próximamente.
    ///
    /// ─── Jerarquía de GameObjects recomendada ────────────────────────────────────────
    ///
    ///  WipPanel                   [RectTransform | Image(blue_box, sliced) | UIPanel | CanvasGroup]
    ///  ├── TitleBar               [Image(home_title_bar1.png)]
    ///  │   └── TitleText          [TMP_Text — nombre del modo, ej. "DESAFÍO"]
    ///  ├── BtnClose               [Button(exit.png, sin texto)] → OnClickClose()
    ///  ├── IconImage              [Image(settings.png o loading.png)]
    ///  ├── WipLabel               [TMP_Text — "EN DESARROLLO", fuente grande/bold]
    ///  ├── DotsText               [TMP_Text — "." / ".." / "..." animado, misma fuente]
    ///  ├── Divider                [Image, height 1, color #FFFFFF40, stretch horizontal]
    ///  ├── DescText               [TMP_Text — descripción pequeña / normal]
    ///  └── BtnVolver              [Button(button_large.png)] → OnClickClose()
    ///
    /// Todos los hijos excepto TitleBar y BtnClose pueden estar dentro de un
    /// VerticalLayoutGroup con spacing 10–14 para facilitar el layout.
    ///
    /// El sprite "blue_box.png" (Sprites/UI/) debe tener Image Type = Sliced.
    /// </summary>
    public class WipPanel : UIPanel
    {
        // ── Referencias a los elementos de UI ────────────────────────────────────

        [Header("Textos")]
        [Tooltip("Nombre del modo activo: 'DESAFÍO', 'ONLINE', etc.")]
        public TMP_Text mode_title;

        [Tooltip("Texto principal: 'EN DESARROLLO'")]
        public TMP_Text wip_label;

        [Tooltip("Puntos animados que van de '.' a '...' en bucle.")]
        public TMP_Text dots_text;

        [Tooltip("Descripción secundaria que explica que el modo llegará pronto.")]
        public TMP_Text desc_text;

        // ── Decoración ────────────────────────────────────────────────────────────

        [Header("Decoración")]
        [Tooltip("Icono decorativo (settings.png, loading.png…). Puede quedar vacío.")]
        public Image icon_image;

        [Tooltip("Sprite para el icono de DESAFÍO (opcional).")]
        public Sprite icon_desafio;

        [Tooltip("Sprite para el icono de ONLINE (opcional).")]
        public Sprite icon_online;

        // ── Animación de puntos ───────────────────────────────────────────────────

        [Header("Animación")]
        [Tooltip("Segundos entre cada cambio de puntos suspensivos.")]
        public float dot_interval = 0.55f;

        [Tooltip("Escala del panel al aparecer (pop-in). 0 = sin efecto.")]
        public float pop_scale = 0.92f;

        // ── Estado interno ────────────────────────────────────────────────────────

        private float _dot_timer = 0f;
        private int   _dot_count = 1;
        private bool  _popping   = false;
        private float _pop_timer = 0f;

        // ── Singleton ─────────────────────────────────────────────────────────────

        private static WipPanel _instance;
        public static WipPanel Get()
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<WipPanel>(FindObjectsInactive.Include);
            return _instance;
        }

        // ── Ciclo de vida ─────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            _instance = this;
            // Desactivar inmediatamente para no bloquear raycasts con alpha=0.
            // El singleton ya está registrado antes de desactivar.
            gameObject.SetActive(false);
        }

        protected override void Update()
        {
            base.Update();
            AnimateDots();
            AnimatePop();
        }

        // ── API pública ───────────────────────────────────────────────────────────

        /// <summary>
        /// Muestra el panel indicando qué modo está en desarrollo.
        /// </summary>
        /// <param name="modeName">Nombre visible del modo, ej. "DESAFÍO" u "ONLINE".</param>
        public void ShowMode(string modeName)
        {
            // Rellenar textos
            if (mode_title != null)
                mode_title.text = modeName;

            if (wip_label != null)
                wip_label.text = "EN DESARROLLO";

            if (dots_text != null)
                dots_text.text = ".";

            if (desc_text != null)
                desc_text.text =
                    "Este modo aún no está disponible.\n" +
                    "¡Lo tendrás disponible pronto!";

            // Icono por modo
            if (icon_image != null)
            {
                bool isOnline = modeName.ToUpper().Contains("ONLINE");
                Sprite spr = isOnline ? icon_online : icon_desafio;
                if (spr != null)
                    icon_image.sprite = spr;
                icon_image.gameObject.SetActive(icon_image.sprite != null);
            }

            // Reiniciar animaciones
            _dot_timer = 0f;
            _dot_count = 1;

            // Pop-in
            if (pop_scale > 0f && pop_scale < 1f)
            {
                transform.localScale = Vector3.one * pop_scale;
                _popping   = true;
                _pop_timer = 0f;
            }

            Show();
        }

        // ── Botones ───────────────────────────────────────────────────────────────

        public void OnClickClose()
        {
            Hide();
        }

        // ── Animaciones internas ──────────────────────────────────────────────────

        private void AnimateDots()
        {
            if (!IsVisible() || dots_text == null) return;

            _dot_timer += Time.deltaTime;
            if (_dot_timer >= dot_interval)
            {
                _dot_timer = 0f;
                _dot_count = (_dot_count % 3) + 1;
                dots_text.text = new string('.', _dot_count);
            }
        }

        private void AnimatePop()
        {
            if (!_popping) return;

            _pop_timer += Time.deltaTime;
            float t = Mathf.Clamp01(_pop_timer / 0.18f);   // 0.18s para el pop

            // EaseOutBack: rebote suave al llegar a scale 1
            float scale = EaseOutBack(t, pop_scale, 1f);
            transform.localScale = Vector3.one * scale;

            if (t >= 1f)
            {
                _popping = false;
                transform.localScale = Vector3.one;
            }
        }

        /// <summary>Interpola de <c>from</c> a <c>to</c> con una curva EaseOutBack (overshoot leve).</summary>
        private static float EaseOutBack(float t, float from, float to)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float ease = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
            return Mathf.LerpUnclamped(from, to, ease);
        }
    }
}
