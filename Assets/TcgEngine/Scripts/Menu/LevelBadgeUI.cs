using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TcgEngine.Client;

namespace TcgEngine.UI
{
    /// <summary>
    /// Muestra el nivel del jugador sobre el escudo (WT_LevelBadge.png)
    /// en la esquina inferior-derecha del avatar del TopBar.
    ///
    /// ─── Jerarquía sugerida ──────────────────────────────────────
    ///  AvatarContainer          [RectTransform, tamaño del avatar]
    ///  ├── AvatarImage          [Image — el avatar]
    ///  └── LevelBadge           [Image(WT_LevelBadge) | LevelBadgeUI]
    ///      └── LevelText        [TMP_Text — número de nivel]
    ///
    /// Ajusta Pivot/Anchor del LevelBadge a (1, 0) para anclar
    /// a la esquina inferior-derecha, luego offset (12, -12).
    /// </summary>
    public class LevelBadgeUI : MonoBehaviour
    {
        [Tooltip("TMP_Text donde se escribe el número de nivel.")]
        public TMP_Text level_text;

        [Tooltip("Segundos entre cada refresco automático.")]
        public float refresh_interval = 5f;

        private float _timer = 0f;

        private void Start()
        {
            Refresh();
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= refresh_interval)
            {
                _timer = 0f;
                Refresh();
            }
        }

        /// <summary>Actualiza el texto con el nivel actual del jugador.</summary>
        public void Refresh()
        {
            if (level_text == null) return;

            UserData udata = Authenticator.Get()?.UserData;
            if (udata == null)
            {
                level_text.text = "—";
                return;
            }

            level_text.text = udata.GetLevel().ToString();
        }
    }
}
