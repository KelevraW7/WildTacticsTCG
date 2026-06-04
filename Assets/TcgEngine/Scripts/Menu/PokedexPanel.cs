using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TcgEngine.Client;

namespace TcgEngine.UI
{
    /// <summary>
    /// Pokédex — muestra todas las cartas del juego agrupadas por tipo de animal.
    /// Cartas desbloqueadas: arte completo en color.
    /// Cartas bloqueadas:    reverso negro (criaturas) o blanco (eventos).
    ///
    /// Jerarquía esperada en Unity:
    ///   PokedexPanel  (este script + UIPanel + CanvasGroup)
    ///   ├── TopBar
    ///   │   ├── Btn_Criaturas   → OnClick → PokedexPanel.OnClickCreatures()
    ///   │   ├── Btn_Eventos     → OnClick → PokedexPanel.OnClickEvents()
    ///   │   └── Counter_Text    (Text — "45 / 60")
    ///   ├── TeamFilters
    ///   │   └── IconButton × N  (group="pokedex_team", value=team.id)
    ///   ├── ScrollRect
    ///   │   └── Viewport → Content  (CardGrid + VerticalLayoutGroup)
    ///   └── Btn_Back            → OnClick → PokedexPanel.OnClickBack()
    /// </summary>
    public class PokedexPanel : UIPanel
    {
        [Header("Grid")]
        public ScrollRect    scroll_rect;
        public RectTransform scroll_content;
        public CardGrid      grid_content;
        public GameObject    card_prefab;           // CardCollection.prefab

        [Header("Sprites de carta bloqueada")]
        public Sprite locked_creature_sprite;       // reverse_black_card
        public Sprite locked_event_sprite;          // reverse_white_card

        [Header("Filtros de tipo animal")]
        public GameObject    factions_group;        // GameObject padre de los IconButtons de facción
        public IconButton[] team_filters;           // .value = TeamData.id

        [Header("Contador")]
        public TextMeshProUGUI counter_text;        // "45 / 60"

        [Header("Preview derecha")]
        public CardUI preview_card;                 // CardUI grande en SidebarRight

        [Header("Canje de duplicados")]
        public GameObject      redeem_panel;        // Panel que envuelve texto + botón (bajo el preview)
        public TextMeshProUGUI redeem_text;         // "X / 10"
        public Button          btn_redeem;          // Botón "Canjear"
        public TextMeshProUGUI redeem_hint_text;    // "10 copias → +100 WildCoins"

        // ── Constantes de canje ───────────────────────────────────────────
        private const int REDEEM_THRESHOLD    = 10;  // copias necesarias para canjear
        private const int REDEEM_COINS_NORMAL = 100; // WC por criatura normal
        private const int REDEEM_COINS_GOLDEN = 200; // WC por criatura dorada

        // ── Estado ────────────────────────────────────────────────────────
        private bool     show_events = false;
        private TeamData filter_team = null;

        private CardData    selected_card    = null;
        private VariantData selected_variant = null;

        private readonly List<CollectionCard> all_cards = new List<CollectionCard>();
        private bool  spawned           = false;
        private bool  update_grid       = false;
        private float update_grid_timer = 0f;

        private static PokedexPanel _instance;
        public  static PokedexPanel Get() => _instance;

        // ── Lifecycle ─────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            _instance = this;

            // Limpiar hijos del grid que pudieran existir en la escena
            for (int i = 0; i < grid_content.transform.childCount; i++)
                Destroy(grid_content.transform.GetChild(i).gameObject);

            foreach (IconButton btn in team_filters)
                btn.onClick += OnClickTeam;
        }

        public override void Show(bool instant = false)
        {
            base.Show(instant);
            if (!spawned) SpawnCards();
            RefreshCards();
            // Preview + canje vacíos al abrir
            if (preview_card != null)
                preview_card.gameObject.SetActive(false);
            selected_card    = null;
            selected_variant = null;
            if (redeem_panel != null)
                redeem_panel.SetActive(false);
        }

        private void LateUpdate()
        {
            update_grid_timer += Time.deltaTime;
            if (update_grid && update_grid_timer > 0.2f)
            {
                grid_content.GetColumnAndRow(out int rows, out int cols);
                if (cols > 0)
                {
                    float rh = grid_content.GetGrid().cellSize.y + grid_content.GetGrid().spacing.y;
                    scroll_content.sizeDelta = new Vector2(scroll_content.sizeDelta.x, rows * rh + 100f);
                    update_grid = false;
                }
            }
        }

        // ── Spawn ─────────────────────────────────────────────────────────

        private void SpawnCards()
        {
            spawned = true;
            foreach (CollectionCard c in all_cards) Destroy(c.gameObject);
            all_cards.Clear();

            var creatures = new List<CardData>();
            var events    = new List<CardData>();

            foreach (CardData card in CardData.GetAll())
            {
                if (card.IsCharacter()) creatures.Add(card);
                else if (card.IsEvent()) events.Add(card);
            }

            // Criaturas: primero por tipo de animal (team.title), luego alfabético
            creatures.Sort((a, b) =>
            {
                string ta = a.team != null ? a.team.title : "";
                string tb = b.team != null ? b.team.title : "";
                int cmp = string.Compare(ta, tb, System.StringComparison.OrdinalIgnoreCase);
                return cmp != 0 ? cmp
                    : string.Compare(a.title, b.title, System.StringComparison.OrdinalIgnoreCase);
            });

            // Eventos: alfabético
            events.Sort((a, b) =>
                string.Compare(a.title, b.title, System.StringComparison.OrdinalIgnoreCase));

            VariantData def = VariantData.GetDefault();
            foreach (CardData c in creatures) SpawnOne(c, def);
            foreach (CardData c in events)    SpawnOne(c, def);
        }

        private void SpawnOne(CardData card, VariantData variant)
        {
            GameObject     go = Instantiate(card_prefab, grid_content.transform);
            CollectionCard cc = go.GetComponent<CollectionCard>();
            cc.SetCard(card, variant, 0);
            cc.card_ui.SetStatsVisible(false);  // vista compacta: sin ataque ni HP en el grid
            cc.onClick += OnClickCard;
            all_cards.Add(cc);
            go.SetActive(false);
        }

        // ── Refresh ───────────────────────────────────────────────────────

        public void RefreshCards()
        {
            if (!spawned) return;

            UserData    udata = Authenticator.Get()?.UserData;
            VariantData def   = VariantData.GetDefault();

            int total = 0, unlocked_count = 0;

            foreach (CollectionCard cc in all_cards)
            {
                CardData card        = cc.GetCard();
                bool     is_creature = card.IsCharacter();
                bool     is_event    = card.IsEvent();

                bool tab_match  = show_events ? is_event : is_creature;
                bool team_match = !is_creature || filter_team == null || card.team == filter_team;
                bool visible    = tab_match && team_match;

                cc.gameObject.SetActive(visible);
                if (!visible) continue;

                total++;
                int  qty   = udata != null ? udata.GetCardQuantity(card.id, def.id, true) : 0;
                bool owned = qty > 0;

                if (owned)
                {
                    unlocked_count++;
                    cc.SetQuantity(qty);        // muestra 1, 2, 3… según copias
                    cc.SetLocked(false, null);
                }
                else
                {
                    Sprite sp = is_event ? locked_event_sprite : locked_creature_sprite;
                    cc.SetLocked(true, sp);
                }
            }

            if (counter_text != null)
                counter_text.text = $"{unlocked_count} / {total}";

            update_grid       = true;
            update_grid_timer = 0f;

            if (scroll_rect != null)
                scroll_rect.verticalNormalizedPosition = 1f;
        }

        // ── Callbacks ─────────────────────────────────────────────────────

        private void OnClickCard(CardUI card_ui)
        {
            CardData    card    = card_ui.GetCard();
            VariantData variant = card_ui.GetVariant();
            UserData    udata   = Authenticator.Get()?.UserData;

            bool owned = udata != null && udata.GetCardQuantity(card.id, variant.id, true) > 0;
            if (!owned) return; // bloqueada: no hacer nada

            selected_card    = card;
            selected_variant = variant;

            // Mostrar en el preview lateral derecho
            if (preview_card != null)
            {
                preview_card.gameObject.SetActive(true);
                preview_card.SetCard(card, variant);
            }

            RefreshRedeemUI();
        }

        // ── Canje de duplicados ───────────────────────────────────────────

        private void RefreshRedeemUI()
        {
            if (redeem_panel == null) return;

            if (selected_card == null)
            {
                redeem_panel.SetActive(false);
                return;
            }

            UserData    udata = Authenticator.Get()?.UserData;
            VariantData def   = VariantData.GetDefault();

            if (udata == null) { redeem_panel.SetActive(false); return; }

            int qty = udata.GetCardQuantity(selected_card.id, def.id, true);

            redeem_panel.SetActive(true);

            if (redeem_text != null)
                redeem_text.text = $"{qty} / {REDEEM_THRESHOLD}";

            if (redeem_hint_text != null)
            {
                bool is_golden = selected_card.team != null &&
                                 selected_card.team.id.ToLower() == "gold";
                int reward = is_golden ? REDEEM_COINS_GOLDEN : REDEEM_COINS_NORMAL;
                redeem_hint_text.text = $"Cada 10 copias extra = {reward} WC\n(Siempre conservarás 1 copia)";
            }

            if (btn_redeem != null)
                btn_redeem.interactable = qty >= REDEEM_THRESHOLD;
        }

        /// <summary>
        /// Canjea 9 copias de la carta seleccionada por WC.
        /// La décima copia se conserva (siempre queda al menos 1).
        /// Conectar al onClick del botón "Canjear" en Unity.
        /// </summary>
        public void OnClickRedeem()
        {
            if (selected_card == null) return;

            UserData    udata = Authenticator.Get()?.UserData;
            VariantData def   = VariantData.GetDefault();
            if (udata == null) return;

            int qty = udata.GetCardQuantity(selected_card.id, def.id, true);
            if (qty < REDEEM_THRESHOLD) return;

            bool is_golden = selected_card.team != null &&
                             selected_card.team.id.ToLower() == "gold";
            int reward = is_golden ? REDEEM_COINS_GOLDEN : REDEEM_COINS_NORMAL;

            // Quitar 9 copias → queda 1
            udata.RemoveCard(selected_card.id, def.id, REDEEM_THRESHOLD - 1);
            udata.coins += reward;

            _ = Authenticator.Get().SaveUserData();

            Debug.Log($"[Pokedex] Canje: {selected_card.title} → -{REDEEM_THRESHOLD - 1} copias, +{reward} WC");

            RefreshCards();
            RefreshRedeemUI();
        }

        private void OnClickTeam(IconButton btn)
        {
            // IconButton ya gestiona el toggle; si IsActive()=true el filtro se aplica, si false se quita
            filter_team = btn.IsActive() ? TeamData.Get(btn.value) : null;
            RefreshCards();
        }

        // ── Botones públicos (wired en Unity) ─────────────────────────────

        public void OnClickCreatures()
        {
            show_events = false;
            if (factions_group != null) factions_group.SetActive(true);
            RefreshCards();
        }

        public void OnClickEvents()
        {
            show_events = true;
            filter_team = null;
            if (factions_group != null) factions_group.SetActive(false);
            // Quitar selección de filtro de equipo
            if (team_filters.Length > 0)
                IconButton.DeactivateAll(team_filters[0].group);
            RefreshCards();
        }

        public void OnClickBack()
        {
            Hide();
        }
    }
}
