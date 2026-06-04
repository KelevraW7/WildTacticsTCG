using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

namespace TcgEngine.UI
{
    /// <summary>
    /// Main UI script for all the game scene UI
    /// </summary>

    public class GameUI : MonoBehaviour
    {
        public Canvas game_canvas;
        public Canvas panel_canvas;
        public Canvas top_canvas;
        public UIPanel menu_panel;
        public Text quit_btn;

        [Header("Confirmación de abandono (Competitivo)")]
        [Tooltip("Panel modal que pide confirmación antes de abandonar en modo Competitivo.")]
        public UIPanel confirm_quit_panel;
        [Tooltip("Texto de aviso dentro del panel de confirmación.")]
        public Text confirm_quit_message;

        [Header("Turn Area")]
        public Text turn_count;
        public Text turn_timer;
        public Animator timeout_animator;
        public AudioClip timeout_audio;

        [Header("Tutorial")]
        [Tooltip("Panel modal con el diagrama de ventajas de tipo.")]
        public UIPanel types_panel;
        [Tooltip("Panel modal con las 6 habilidades explicadas.")]
        public UIPanel abilities_panel;

        private float selector_timer = 0f;
        private float end_turn_timer = 0f;
        private int prev_time_val = 0;

        private static GameUI instance;

        void Awake()
        {
            instance = this;

            if (game_canvas.worldCamera == null)
                game_canvas.worldCamera = Camera.main;
            if (panel_canvas.worldCamera == null)
                panel_canvas.worldCamera = Camera.main;
            if (top_canvas.worldCamera == null)
                top_canvas.worldCamera = Camera.main;
        }

        private void Start()
        {
            GameClient.Get().onGameStart += OnGameStart;
            GameClient.Get().onNewTurn += OnNewTurn;
            LoadPanel.Get().Show(true);
            BlackPanel.Get().Show(true);
            BlackPanel.Get().Hide();

            if (quit_btn != null)
                quit_btn.text = GameClient.game_settings.IsOnlinePlayer() ? "Resign" : "Quit";
        }

        void Update()
        {
            Game data = GameClient.Get().GetGameData();
            bool is_connecting = data == null || data.state == GameState.Connecting;
            bool connection_lost = !is_connecting && !GameClient.Get().IsReady();
            ConnectionPanel.Get().SetVisible(connection_lost);

            //Menu
            if (Input.GetKeyDown(KeyCode.Escape))
                menu_panel.Toggle();

            if (!GameClient.Get().IsReady())
                return;

            bool yourturn = GameClient.Get().IsYourTurn();
            LoadPanel.Get().SetVisible(is_connecting && !data.HasStarted());
            end_turn_timer += Time.deltaTime;
            selector_timer += Time.deltaTime;

            //Timer
            turn_count.text = "Turn " + data.turn_count.ToString();
            turn_timer.enabled = data.turn_timer > 0f;
            turn_timer.text = Mathf.RoundToInt(data.turn_timer).ToString();
            turn_timer.enabled = data.turn_timer < 999f;

            //Simulate timer (pause while waiting for GOLPEAR second attack)
            bool golpear_waiting = !string.IsNullOrEmpty(data.golpear_pending_uid);
            if (data.state == GameState.Play && data.turn_timer > 0f && !golpear_waiting)
                data.turn_timer -= Time.deltaTime;

            //Timer warning
            if (data.state == GameState.Play)
            {
                int val = Mathf.RoundToInt(data.turn_timer);
                int tick_val = 10;
                if (val < prev_time_val && val <= tick_val)
                    PulseFX();
                prev_time_val = val;
            }

            //Show selector panels
            foreach (SelectorPanel panel in SelectorPanel.GetAll())
            {
                bool should_show = panel.ShouldShow();
                if (should_show != panel.IsVisible() && selector_timer > 1f)
                {
                    selector_timer = 0f;
                    panel.SetVisible(should_show);

                    if (should_show)
                    {
                        AbilityData ability = AbilityData.Get(data.selector_ability_id);
                        Card caster = data.GetCard(data.selector_caster_uid);
                        panel.Show(ability, caster);
                    }
                }
            }

            //Hide
            if (!yourturn)
            {
                SelectorPanel.HideAll();
            }

        }

        private void PulseFX()
        {
            timeout_animator?.SetTrigger("pulse");
            AudioTool.Get().PlaySFX("time", timeout_audio, 1f);
        }

        private void OnGameStart()
        {

        }

        private void OnNewTurn(int player_id)
        {
            CardSelector.Get().Hide();
            SelectTargetUI.Get().Hide();

            // Mostrar cartel de turno
            TurnBannerUI banner = Object.FindFirstObjectByType<TurnBannerUI>();
            if (banner != null)
            {
                string message = player_id == GameClient.Get().GetPlayerID() ? "Tu turno" : "Turno rival";
                banner.Show(message, GameClient.Get().GetGameData().turn_count);
            }
        }

        public void OnClickRestart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void OnClickMenu()
        {
            menu_panel.Show();
        }

        public void OnClickBack()
        {
            menu_panel.Hide();
        }

        public void OnClickQuit()
        {
            bool online  = GameClient.game_settings.IsOnlinePlayer();
            bool ended   = GameClient.Get().HasEnded();
            bool competitive = !online
                               && GameClient.game_settings.game_type == GameType.Solo
                               && GameClient.ai_settings.ai_level >= 10;

            // En Competitivo con partida activa, pedir confirmación antes de abandonar
            if (competitive && !ended && confirm_quit_panel != null)
            {
                if (confirm_quit_message != null)
                    confirm_quit_message.text = "¿Abandonar la partida?\nPerderás 50 WC.";
                menu_panel.Hide();
                confirm_quit_panel.Show();
                return;
            }

            menu_panel.Hide();
            if (online && !ended)
                GameClient.Get().Resign();
            else
                StartCoroutine(QuitRoutine("Menu"));
        }

        /// <summary>
        /// El jugador confirmó que quiere abandonar la partida Competitiva y asumir la penalización.
        /// </summary>
        public void OnClickQuitConfirm()
        {
            // Aplicar penalización de abandono (-50 WC) antes de salir
            RewardManager.Get()?.ApplyAbandonPenalty();

            confirm_quit_panel?.Hide();
            menu_panel.Hide();

            bool online = GameClient.game_settings.IsOnlinePlayer();
            bool ended  = GameClient.Get().HasEnded();
            if (online && !ended)
                GameClient.Get().Resign();
            else
                StartCoroutine(QuitRoutine("Menu"));
        }

        /// <summary>
        /// El jugador canceló — cierra el modal y reabre el menú de pausa.
        /// </summary>
        public void OnClickQuitCancel()
        {
            confirm_quit_panel?.Hide();
            menu_panel.Show();
        }

        private IEnumerator QuitRoutine(string scene)
        {
            BlackPanel.Get().Show();
            AudioTool.Get().FadeOutMusic("music");
            AudioTool.Get().FadeOutSFX("ambience");
            AudioTool.Get().FadeOutSFX("ending_sfx");

            yield return new WaitForSeconds(1f);

            GameClient.Get().Disconnect();
            SceneNav.GoTo(scene);
        }

        // ── Tutorial ─────────────────────────────────────────────────────────────

        public void OnClickTypes()
        {
            abilities_panel?.Hide();
            types_panel?.Toggle();
        }

        public void OnClickAbilities()
        {
            types_panel?.Hide();
            abilities_panel?.Toggle();
        }

        public void OnClickCloseTutorial()
        {
            types_panel?.Hide();
            abilities_panel?.Hide();
        }

        public void OnClickSwapObserve()
        {
            int other = GameClient.Get().GetPlayerID() == 0 ? 1 : 0;
            GameClient.Get().SetObserverMode(other);
        }

        public static bool IsUIOpened()
        {
            return CardSelector.Get().IsVisible() || EndGamePanel.Get().IsVisible();
        }

        public static bool IsOverUI()
        {
            //return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            return results.Count > 0;
        }

        public static bool IsOverUILayer(string sorting_layer)
        {
            return IsOverUILayer(SortingLayer.NameToID(sorting_layer));
        }

        public static bool IsOverUILayer(int sorting_layer)
        {
            //return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            int count = 0;
            foreach (RaycastResult result in results)
            {
                if (result.sortingLayer == sorting_layer)
                    count++;
            }
            return count > 0;
        }

        public static bool IsOverRectTransform(Canvas canvas, RectTransform rect)
        {
            PointerEventData pevent = new PointerEventData(EventSystem.current);
            pevent.position = Input.mousePosition;

            List<RaycastResult> results = new List<RaycastResult>();
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            raycaster.Raycast(pevent, results);

            foreach (RaycastResult result in results)
            {
                if (result.gameObject.transform == rect || result.gameObject.transform.IsChildOf(rect))
                    return true;
            }
            return false;
        }

        public static Vector2 MouseToRectPos(Canvas canvas, RectTransform rect, Vector2 screen_pos)
        {
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.worldCamera != null)
            {
                Vector2 anchor_pos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screen_pos, canvas.worldCamera, out anchor_pos);
                return anchor_pos;
            }
            else
            {
                Vector2 anchor_pos = screen_pos - new Vector2(rect.position.x, rect.position.y);
                anchor_pos = new Vector2(anchor_pos.x / rect.lossyScale.x, anchor_pos.y / rect.lossyScale.y);
                return anchor_pos;
            }
        }

        public static Vector3 MouseToWorld(Vector2 mouse_pos, float distance = 10f)
        {
            Camera cam = GameCamera.Get() != null ? GameCamera.GetCamera() : Camera.main;
            Vector3 wpos = cam.ScreenToWorldPoint(new Vector3(mouse_pos.x, mouse_pos.y, distance));
            return wpos;
        }

        public static string FormatNumber(int value)
        {
            return string.Format("{0:#,0}", value);
        }

        public static GameUI Get()
        {
            return instance;
        }
    }
}
