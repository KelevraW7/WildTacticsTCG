using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;
using TcgEngine;
using Wobblewares.Coin;

namespace TcgEngine.UI
{
    /// <summary>
    /// Panel de intro de partida con lanzamiento de moneda (wobblecoin).
    /// Flujo:
    ///   1. Panel aparece — jugador elige CARA o CRUZ
    ///   2. Jugador hace clic en la moneda para lanzarla
    ///   3. Al hacer clic se arranca un timer garantizado: FlipDuration + extra_wait
    ///   4. Si la física detecta el resultado antes (OnSettled), muestra antes
    ///   5. Se muestra resultado + botón SALTAR con countdown
    /// </summary>
    public class CoinFlipPanel : UIPanel
    {
        // ── Avatares ──────────────────────────────────────────────────────
        [Header("Avatares")]
        public Image player_avatar;
        public Image opponent_avatar;
        public Text  player_name_text;
        public Text  opponent_name_text;

        // ── Moneda wobblecoin ─────────────────────────────────────────────
        [Header("Moneda wobblecoin")]
        public Coin     coin;
        public RawImage coin_display;
        public Button   coin_button;

        // ── Toggle CARA / CRUZ ────────────────────────────────────────────
        [Header("Toggle CARA / CRUZ")]
        public Button cara_button;
        public Button cruz_button;
        public Color  color_selected   = new Color(1f, 0.85f, 0.1f);
        public Color  color_unselected = new Color(0.65f, 0.65f, 0.65f);

        // ── Resultado ─────────────────────────────────────────────────────
        [Header("Resultado")]
        public Text result_text;

        // ── Botón SALTAR ──────────────────────────────────────────────────
        [Header("Botón SALTAR")]
        public Button skip_button;
        public Text   skip_button_text;

        // ── Configuración ─────────────────────────────────────────────────
        [Header("Configuración")]
        [Tooltip("Segundos de countdown antes de cerrar automáticamente.")]
        public float countdown_time = 5f;
        [Tooltip("Segundos adicionales tras FlipDuration para mostrar resultado.")]
        public float extra_wait = 2.5f;

        [Header("Activación")]
        [Tooltip("Desactiva el panel de lanzamiento de moneda. La partida arranca directamente.")]
        public bool disable_panel = true;

        // ── Estado interno ────────────────────────────────────────────────
        private bool           choice_made       = false;
        private bool           player_chose_cara = false;
        private bool           player_wins       = false;
        private bool           coin_flipped      = false;
        private bool           result_shown      = false;
        private Coin.CoinSide  pending_side      = Coin.CoinSide.Heads;
        private Coroutine      result_coro       = null;
        private Coroutine      countdown_coro    = null;

        // Flag global: bloquea GameBoard.Update() mientras el panel está activo
        public static bool isPanelShowing { get; private set; } = false;

        private static CoinFlipPanel _instance;

        // ─────────────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            _instance = this;
        }

        protected override void Start()
        {
            base.Start();
            Hide(true);

            if (cara_button != null) cara_button.onClick.AddListener(OnClickCara);
            if (cruz_button != null) cruz_button.onClick.AddListener(OnClickCruz);
            if (coin_button != null) coin_button.onClick.AddListener(OnClickCoin);
            if (skip_button != null) skip_button.onClick.AddListener(OnClickSkip);

            // Suscribir a eventos de la moneda
            if (coin != null)
            {
                coin.OnSettled += OnCoinSettled;
                coin.OnCollide += OnCoinCollide;   // acorta el timer cuando toca el suelo
            }

            GameClient.Get().onGameStart += OnGameStart;
        }

        // ── Entrada desde GameClient ──────────────────────────────────────

        private void OnGameStart()
        {
            ShowFlip();
        }

        // ── Lógica principal ──────────────────────────────────────────────

        public void ShowFlip()
        {
            if (disable_panel) return;   // panel desactivado — la partida arranca directamente

            isPanelShowing = true;   // bloquea GameBoard mientras el panel esté visible

            Game  gdata = GameClient.Get().GetGameData();
            player_wins  = (gdata.first_player == GameClient.Get().GetPlayerID());
            choice_made  = false;
            coin_flipped = false;
            result_shown = false;

            // Avatares
            Player me  = GameClient.Get().GetPlayer();
            Player opp = GameClient.Get().GetOpponentPlayer();

            if (player_name_text   != null) player_name_text.text   = me.username;
            if (opponent_name_text != null) opponent_name_text.text  = opp.username;

            AvatarData myAvat = ResolvePlayerAvatar(me);
            if (player_avatar != null && myAvat != null)
                player_avatar.sprite = myAvat.avatar;

            AvatarData aiAvat = ResolveAIAvatar(myAvat);
            if (opponent_avatar != null && aiAvat != null)
                opponent_avatar.sprite = aiAvat.avatar;

            // Reset UI
            if (result_text != null)      result_text.text = "";
            if (skip_button != null)      skip_button.gameObject.SetActive(false);
            if (skip_button_text != null) skip_button_text.text = "SALTAR";

            SetToggleVisual(cara_button, false);
            SetToggleVisual(cruz_button, false);
            if (coin_button != null) coin_button.interactable = false;

            Show(true);
        }

        // ── Toggle CARA / CRUZ ────────────────────────────────────────────

        public void OnClickCara()
        {
            player_chose_cara = true;
            choice_made       = true;
            SetToggleVisual(cara_button, true);
            SetToggleVisual(cruz_button, false);
            if (coin_button != null) coin_button.interactable = true;
        }

        public void OnClickCruz()
        {
            player_chose_cara = false;
            choice_made       = true;
            SetToggleVisual(cara_button, false);
            SetToggleVisual(cruz_button, true);
            if (coin_button != null) coin_button.interactable = true;
        }

        private void SetToggleVisual(Button btn, bool selected)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = selected ? color_selected : color_unselected;
        }

        // ── Lanzamiento de moneda ─────────────────────────────────────────

        public void OnClickCoin()
        {
            if (!choice_made || coin_flipped) return;

            coin_flipped = true;
            result_shown = false;
            if (coin_button  != null) coin_button.interactable  = false;
            if (cara_button  != null) cara_button.interactable  = false;
            if (cruz_button  != null) cruz_button.interactable  = false;

            // Determinar lado objetivo
            if (player_wins)
                pending_side = player_chose_cara ? Coin.CoinSide.Heads : Coin.CoinSide.Tails;
            else
                pending_side = player_chose_cara ? Coin.CoinSide.Tails : Coin.CoinSide.Heads;

            // ── Timer garantizado ─────────────────────────────────────────
            // Arranca AQUÍ, independientemente de si OnFlipEnd u OnSettled disparan.
            // Si la física ya mostró el resultado, result_shown=true y ShowResult() se ignora.
            float totalWait = (coin != null ? coin.FlipDuration : 1f) + extra_wait;
            if (result_coro != null) StopCoroutine(result_coro);
            result_coro = StartCoroutine(GuaranteedResultRoutine(totalWait));

            if (coin != null)
                coin.Flip(coin.transform.position, pending_side);
            else
                ShowResult();
        }

        private IEnumerator GuaranteedResultRoutine(float wait)
        {
            yield return new WaitForSeconds(wait);
            ShowResult();
        }

        // ── Callbacks físicos ─────────────────────────────────────────────

        /// <summary>
        /// La moneda tocó el suelo. Reinicia el timer a 1.5s para no esperar
        /// innecesariamente en caso de que caiga de canto (OnSettled no dispara).
        /// </summary>
        private void OnCoinCollide()
        {
            if (result_shown) return;
            if (result_coro != null) { StopCoroutine(result_coro); result_coro = null; }
            result_coro = StartCoroutine(GuaranteedResultRoutine(1.5f));
        }

        /// <summary>
        /// Llamado por la física cuando la moneda se posa con cara válida.
        /// Cancela el timer y muestra el resultado inmediatamente.
        /// </summary>
        private void OnCoinSettled(Coin.CoinSide side)
        {
            if (result_coro != null) { StopCoroutine(result_coro); result_coro = null; }
            ShowResult();
        }

        // ── Resultado ─────────────────────────────────────────────────────

        private void ShowResult()
        {
            if (result_shown) return;
            result_shown = true;

            // ── Texto del resultado ───────────────────────────────────────
            string ladoStr   = (pending_side == Coin.CoinSide.Heads) ? "CARA" : "CRUZ";
            string resultStr = player_wins ? "¡Empiezas tú!" : "Empieza el rival";
            string guess     = choice_made
                ? (player_wins ? "¡Acertaste!" : "Fallaste...")
                : "";

            if (result_text != null)
                result_text.text = $"{ladoStr}  {guess}\n{resultStr}";

            if (skip_button != null) skip_button.gameObject.SetActive(true);
            if (countdown_coro != null) StopCoroutine(countdown_coro);
            countdown_coro = StartCoroutine(CountdownRoutine());

            // ── Congelar y orientar la moneda (coroutine para evitar conflicto con physics) ──
            if (coin != null)
                StartCoroutine(FreezeCoinRoutine());
        }

        /// <summary>
        /// Congela el rigidbody en el mismo frame y luego anima suavemente la moneda
        /// a la orientación correcta (plana, cara ganadora arriba).
        /// Usar un coroutine garantiza que la física no machaca la rotación forzada.
        /// </summary>
        private IEnumerator FreezeCoinRoutine()
        {
            // 1. Detener coroutines internas (SettleAsync, etc.)
            coin.Stop();   // StopAllCoroutines, useGravity=true, State=Idle

            // 2. Congelar física ANTES de modificar la rotación
            Rigidbody rb = coin.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity  = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic     = true;   // la física ya no puede mover la moneda
            }

            // 3. Esperar que el motor de física confirme el estado kinematic.
            //    Sin esto, un FixedUpdate intermedio puede machac la rotación que fijamos.
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            // 4. Calcular orientación objetivo con FromToRotation (sin gimbal lock).
            //    Funciona correctamente aunque la moneda esté de canto (90°) o girada.
            Quaternion toFlat;
            if (pending_side == Coin.CoinSide.Heads)
                toFlat = Quaternion.FromToRotation(coin.transform.up, Vector3.up);
            else
                toFlat = Quaternion.FromToRotation(coin.transform.up, Vector3.down);

            Quaternion targetRot = toFlat * coin.transform.rotation;

            // 5. Animar suavemente hasta la orientación correcta
            float      elapsed  = 0f;
            const float duration = 0.4f;
            Quaternion startRot  = coin.transform.rotation;

            while (elapsed < duration)
            {
                // Reforzar kinematic cada frame por si algo externo lo resetea
                if (rb != null) rb.isKinematic = true;
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                coin.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }
            coin.transform.rotation = targetRot;
            if (rb != null) rb.isKinematic = true;  // garantía final
        }

        // ── Countdown ─────────────────────────────────────────────────────

        private IEnumerator CountdownRoutine()
        {
            float remaining = countdown_time;
            while (remaining > 0f)
            {
                if (skip_button_text != null)
                    skip_button_text.text = $"SALTAR ({Mathf.CeilToInt(remaining)})";
                remaining -= Time.deltaTime;
                yield return null;
            }
            ClosePanel();
        }

        // ── Botón SALTAR ──────────────────────────────────────────────────

        public void OnClickSkip()
        {
            if (countdown_coro != null) { StopCoroutine(countdown_coro); countdown_coro = null; }
            ClosePanel();
        }

        private void ClosePanel()
        {
            isPanelShowing = false;  // libera GameBoard

            // Restaura el rigidbody de la moneda para la próxima partida
            if (coin != null)
            {
                Rigidbody rb = coin.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = false;
            }

            // Fuerza un refresh inmediato para que el tablero se actualice al estado actual
            GameBoard.Get()?.RefreshBoard();
            Hide();
        }

        // ── Helpers de avatar ─────────────────────────────────────────────

        private AvatarData ResolvePlayerAvatar(Player player)
        {
            AvatarData a = AvatarData.Get(player.avatar);
            if (a != null) return a;
            UserData udata = Authenticator.Get()?.UserData;
            if (udata != null) { a = AvatarData.Get(udata.avatar); if (a != null) return a; }
            var all = AvatarData.GetAll();
            return all.Count > 0 ? all[0] : null;
        }

        private AvatarData ResolveAIAvatar(AvatarData exclude)
        {
            string excludeId = exclude != null ? exclude.id : "";
            var candidates = new List<AvatarData>();
            foreach (AvatarData a in AvatarData.GetAll())
                if (a.id != excludeId && a.avatar != null)
                    candidates.Add(a);
            if (candidates.Count > 0)
                return candidates[UnityEngine.Random.Range(0, candidates.Count)];
            var all = AvatarData.GetAll();
            return all.Count > 0 ? all[0] : null;
        }

        // ── Singleton ─────────────────────────────────────────────────────

        public static CoinFlipPanel Get() => _instance;
    }
}
