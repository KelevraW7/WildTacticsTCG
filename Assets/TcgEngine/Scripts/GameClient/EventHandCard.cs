using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TcgEngine.UI;

namespace TcgEngine.Client
{
    /// <summary>
    /// Representa una única carta de evento en la zona face-up (BoardEventHand).
    ///
    /// Visual: imagen del board art + título debajo, más pequeño que las criaturas.
    /// Hover  : registra esta carta como foco → CardPreviewUI la pinta en grande.
    /// Clic   : juega la carta si es tu turno y no has atacado aún.
    ///
    /// Estructura de prefab recomendada:
    ///   EventHandCard (RectTransform)
    ///   ├── Frame      (Image – borde/fondo decorativo, opcional)
    ///   ├── BoardImage (Image – art_board)
    ///   ├── TitleBg    (Image – franja semitransparente, opcional)
    ///   └── TitleLabel (Text  – nombre de la carta)
    /// </summary>
    public class EventHandCard : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Referencias del prefab")]
        public Image  board_image;      // Muestra card.CardData.art_board
        public Text   title_label;      // Muestra card.CardData.GetTitle()
        public Image  hover_highlight;  // Borde luminoso al hacer hover (puede ser null)

        [Header("Animación")]
        public float hover_lift   = 12f;   // Píxeles que sube al hacer hover
        public float anim_speed   = 10f;

        // ── Estado ────────────────────────────────────────────────────────────────
        private string  card_uid;
        private bool    is_opponent;
        private bool    hovering;
        private Vector2 base_pos;       // asignado por BoardEventHand, no al Awake

        // ── Foco global (leído por CardPreviewUI) ─────────────────────────────────
        private static EventHandCard s_focused;
        public  static EventHandCard GetFocus() => s_focused;

        // ─────────────────────────────────────────────────────────────────────────

        void Awake() { }   // base_pos se fija via SetBasePosition()

        /// <summary>
        /// BoardEventHand llama a esto cada vez que recalcula el layout horizontal.
        /// La carta anima desde/hacia este punto aplicando el lift del hover.
        /// </summary>
        public void SetBasePosition(Vector2 pos)
        {
            base_pos = pos;
        }

        void Update()
        {
            if (!GameClient.Get().IsReady()) return;

            Card card = GetCard();
            if (card == null) return;

            // ── Imagen del board art ──────────────────────────────────────────────
            if (board_image != null && card.CardData != null)
                board_image.sprite = card.CardData.art_board;

            // ── Título ────────────────────────────────────────────────────────────
            if (title_label != null && card.CardData != null)
                title_label.text = card.CardData.GetTitle();

            // ── Highlight ────────────────────────────────────────────────────────
            if (hover_highlight != null)
                hover_highlight.enabled = hovering;

            // ── Lift al hacer hover ───────────────────────────────────────────────
            RectTransform rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                float target_y = hovering ? base_pos.y + hover_lift : base_pos.y;
                rt.anchoredPosition = Vector2.Lerp(
                    rt.anchoredPosition,
                    new Vector2(base_pos.x, target_y),
                    Time.deltaTime * anim_speed);
            }
        }

        /// <summary>Llamado por BoardEventHand al instanciar esta carta.</summary>
        public void Init(string uid, bool opponent)
        {
            card_uid    = uid;
            is_opponent = opponent;

            // Sincronizar visuales inmediatamente
            Card card = GetCard();
            if (card == null) return;

            if (board_image != null && card.CardData != null)
                board_image.sprite = card.CardData.art_board;
            if (title_label != null && card.CardData != null)
                title_label.text = card.CardData.GetTitle();
        }

        // ── EventSystem callbacks ─────────────────────────────────────────────────

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

        public void OnPointerClick(PointerEventData eventData)
        {
            if (is_opponent) return;
            if (eventData.button != PointerEventData.InputButton.Left) return;

            if (!CanPlay())
            {
                Debug.Log("[EventHandCard] No se puede jugar ahora (no es tu turno o ya atacaste).");
                return;
            }

            Card card = GetCard();
            if (card == null) return;

            // Las cartas de evento no necesitan slot destino
            GameClient.Get().PlayCard(card, Slot.None);
            Debug.Log($"[EventHandCard] Jugando carta de evento: {card.card_id}");
        }

        private void OnDisable()
        {
            hovering = false;
            if (s_focused == this) s_focused = null;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private bool CanPlay()
        {
            if (is_opponent) return false;
            Game gdata = GameClient.Get()?.GetGameData();
            if (gdata == null) return false;
            Player player = gdata.GetPlayer(GameClient.Get().GetPlayerID());
            return gdata.IsPlayerActionTurn(player) && !gdata.has_attacked_this_turn;
        }

        public Card GetCard()
            => GameClient.Get()?.GetGameData()?.GetCard(card_uid);

        public string CardUID => card_uid;
    }
}
