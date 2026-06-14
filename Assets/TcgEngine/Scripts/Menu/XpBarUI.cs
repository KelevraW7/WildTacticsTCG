using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TcgEngine.Client;

namespace TcgEngine.UI
{
    /// <summary>
    /// Barra de progreso de XP debajo del TopBar.
    /// Muestra nivel y XP en una etiqueta permanente a la derecha.
    ///
    /// ─── Jerarquía ──────────────────────────────────────────────────────────
    ///  XpBar          [XpBarUI | RectTransform H=10]
    ///  ├── Rail       [Image color oscuro, stretch full]
    ///  ├── Fill       [Image Source=None, color dorado, Filled/Horizontal/Left]
    ///  ├── Ticks      [RectTransform stretch full]
    ///  └── XpLabel    [TMP_Text anclado a la derecha, debajo de la barra]
    /// </summary>
    public class XpBarUI : MonoBehaviour
    {
        [Header("Refs")]
        [Tooltip("Image con type=Filled y fillMethod=Horizontal.")]
        public Image fill_image;

        [Tooltip("Contenedor donde se generan las rayas divisorias (stretch full).")]
        public RectTransform tick_container;

        [Header("Etiqueta permanente")]
        [Tooltip("TMP_Text visible siempre — muestra 'Nv.3  240 / 2.000 XP'.")]
        public TMP_Text xp_label;

        [Header("Ticks")]
        [Tooltip("Número de divisiones (20 = cada 5%).")]
        public int tick_count = 20;

        [Tooltip("Color de las rayas divisorias.")]
        public Color tick_color = new Color(0f, 0f, 0f, 0.35f);

        [Header("Animación")]
        [Tooltip("Velocidad de llenado suave (unidades/seg). 0 = instantáneo.")]
        public float fill_speed = 0.8f;

        [Tooltip("Segundos entre cada refresco automático.")]
        public float refresh_interval = 4f;

        // ── Estado interno ────────────────────────────────────────────────────
        private float _target_fill = 0f;
        private float _current_fill = 0f;
        private float _timer = 0f;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Start()
        {
            BuildTicks();
            Refresh();

            if (fill_image != null)
                fill_image.fillAmount = _target_fill;
            _current_fill = _target_fill;
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= refresh_interval)
            {
                _timer = 0f;
                Refresh();
            }

            // Animación suave del fill
            if (fill_image != null && !Mathf.Approximately(_current_fill, _target_fill))
            {
                _current_fill = fill_speed <= 0f
                    ? _target_fill
                    : Mathf.MoveTowards(_current_fill, _target_fill, fill_speed * Time.deltaTime);

                fill_image.fillAmount = _current_fill;
            }
        }

        // ── API pública ───────────────────────────────────────────────────────

        public void Refresh()
        {
            UserData udata = Authenticator.Get()?.UserData;
            if (udata == null) return;

            _target_fill = udata.GetXpProgress();

            if (xp_label != null)
            {
                int lvl     = udata.GetLevel();
                int current = udata.GetXpInCurrentLevel();
                int needed  = udata.GetXpNeededForNextLevel();
                int pct     = Mathf.RoundToInt(udata.GetXpProgress() * 100f);
                xp_label.text = $"Nv.{lvl}   {current} / {needed} XP ({pct}%)";
            }
        }

        // ── Ticks ─────────────────────────────────────────────────────────────

        private void BuildTicks()
        {
            if (tick_container == null) return;

            for (int i = tick_container.childCount - 1; i >= 0; i--)
                Destroy(tick_container.GetChild(i).gameObject);

            for (int i = 1; i < tick_count; i++)
            {
                float t = i / (float)tick_count;

                GameObject go = new GameObject("Tick", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(tick_container, false);

                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin        = new Vector2(t, 0f);
                rt.anchorMax        = new Vector2(t, 1f);
                rt.sizeDelta        = new Vector2(1f, 0f);
                rt.anchoredPosition = Vector2.zero;

                go.GetComponent<Image>().color = tick_color;
            }
        }
    }
}
