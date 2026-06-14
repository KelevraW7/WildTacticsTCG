using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace TcgEngine.UI
{
    /// <summary>
    /// Gestiona los 3 estados visuales (Normal / Hover / Active) de un panel de modo de juego.
    /// Coloca este script en la raíz de cada panel (Panel_Desafio, Panel_Solo, Panel_Online).
    /// El OnClick real (arrancar partida) se conecta desde el Inspector via Button.onClick.
    /// </summary>
    public class PlayPanelCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Referencias UI")]
        public Image frameImage;
        public TMP_Text titleText;

        [Header("Sprites de estado")]
        public Sprite spriteNormal;
        public Sprite spriteHover;
        public Sprite spriteActive;

        [Header("Config")]
        [Tooltip("Escala cuando el panel está activo (ej: 1.05)")]
        public float activeScale = 1.05f;
        [Tooltip("Duración de la transición de escala en segundos")]
        public float scaleDuration = 0.12f;

        // ── Estado ────────────────────────────────────────────────────────────
        private bool isActive = false;
        private static PlayPanelCard s_currentActive;

        /// <summary>Se dispara cuando el jugador selecciona este panel.</summary>
        public static event Action<PlayPanelCard> OnPanelSelected;

        // ─────────────────────────────────────────────────────────────────────

        private void Start()
        {
            ApplyState();
        }

        // ── Pointer events ────────────────────────────────────────────────────

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isActive && spriteHover != null)
                frameImage.sprite = spriteHover;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isActive)
                frameImage.sprite = spriteNormal;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Select();
        }

        // ── API pública ───────────────────────────────────────────────────────

        /// <summary>Activa este panel y desactiva el anterior.</summary>
        public void Select()
        {
            if (s_currentActive != null && s_currentActive != this)
                s_currentActive.Deselect();

            s_currentActive = this;
            isActive = true;
            ApplyState();

            OnPanelSelected?.Invoke(this);
        }

        /// <summary>Vuelve al estado normal.</summary>
        public void Deselect()
        {
            isActive = false;
            ApplyState();
        }

        public bool IsActive() => isActive;

        // ── Internos ──────────────────────────────────────────────────────────

        private void ApplyState()
        {
            if (frameImage == null) return;

            frameImage.sprite = isActive ? spriteActive : spriteNormal;

            float targetScale = isActive ? activeScale : 1f;
            StopAllCoroutines();
            StartCoroutine(ScaleTo(targetScale));
        }

        private IEnumerator ScaleTo(float target)
        {
            Vector3 start = transform.localScale;
            Vector3 end   = new Vector3(target, target, 1f);
            float elapsed = 0f;

            while (elapsed < scaleDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / scaleDuration);
                transform.localScale = Vector3.Lerp(start, end, t);
                yield return null;
            }

            transform.localScale = end;
        }
    }
}
