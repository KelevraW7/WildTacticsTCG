using UnityEngine;

namespace TcgEngine.UI
{
    /// <summary>
    /// Animación del badge "NUEVA" al revelar una carta nueva en apertura de packs.
    ///
    /// Al activarse el GameObject:
    ///   1. Pop-in: escala de 0 → 1 con ligero overshoot (sensación de rebote).
    ///   2. Pulso continuo suave para mantener la atención.
    ///
    /// Añadir este script al GameObject NewCard dentro del prefab PackCard.
    /// </summary>
    public class NewCardBadge : MonoBehaviour
    {
        [Header("Pop inicial")]
        public float pop_duration  = 0.35f;   // segundos que dura el pop-in

        [Header("Pulso continuo")]
        public float pulse_speed   = 1.8f;    // ciclos por segundo
        public float pulse_amount  = 0.07f;   // amplitud (±7 % del tamaño base)

        private Vector3 base_scale;
        private float   timer   = 0f;
        private bool    popping = true;

        void OnEnable()
        {
            base_scale            = Vector3.one;   // asume scale (1,1,1) en reposo
            transform.localScale  = Vector3.zero;
            timer                 = 0f;
            popping               = true;
        }

        void Update()
        {
            timer += Time.deltaTime;

            if (popping)
            {
                // Pop-in con curva EaseOutBack (overshoot)
                float t     = Mathf.Clamp01(timer / pop_duration);
                float scale = EaseOutBack(t);
                transform.localScale = base_scale * scale;
                if (t >= 1f) { popping = false; }
            }
            else
            {
                // Pulso suave continuo
                float pulse = 1f + Mathf.Sin(timer * pulse_speed * Mathf.PI * 2f) * pulse_amount;
                transform.localScale = base_scale * pulse;
            }
        }

        // Curva con rebote al final (equivale a AnimationCurve EaseOutBack)
        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
