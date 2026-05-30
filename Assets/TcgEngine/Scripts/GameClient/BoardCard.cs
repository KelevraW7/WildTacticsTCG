using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;
using UnityEngine.Events;
using TcgEngine.UI;
using TcgEngine.FX;
using TcgEngine;

namespace TcgEngine.Client
{
    /// <summary>
    /// Represents the visual aspect of a card on the board.
    /// Will take the data from Card.cs and display it
    /// </summary>

    public class BoardCard : MonoBehaviour
    {
        public GameObject TeamIcon;
        public GameObject AttackIcon;
        public GameObject HpIcon;

        public CanvasGroup StatusPanel;
        public Text StatusText;
        public SpriteRenderer card_sprite;
        public Sprite reverse_sprite; // Sprite para carta boca abajo

        public SpriteRenderer card_glow;
        public SpriteRenderer card_shadow;

        public Image armor_icon;
        public Text armor;
        public GameObject damageTextPrefab;  // Asignar en el inspector

        public CanvasGroup status_group;
        public Text status_text;

        public BoardCardEquip equipment;

        public AbilityButton[] buttons;

        public Color glow_ally;
        public Color glow_enemy;
        public Color glow_selected = new Color(1f, 0.84f, 0f); // Amarillo dorado
        private bool is_selected = false;

        public UnityAction onKill;

        [SerializeField]
        private CardUI card_ui;
        public GameObject damageFXPrefab;  // Arrástralo en el inspector

        private BoardCardFX card_fx;
        private bool hasPlayedSpawnFX = false;

        private Canvas canvas;

        public string card_uid = "";
        private bool destroyed = false;
        private bool focus = false;
        private float timer = 0f;
        private float status_alpha_target = 0f;
        private float delayed_damage_timer = 0f;
        private int prev_hp = 0;

        // Set in OnAbilityStartBC when an OnBeforeDefend+AbilityTriggerer ability fires.
        // Consumed in OnCardDamagedEvent to suppress PlayHitFX/ShowDamageFX and delay the HP bar.
        private static bool next_hit_is_counter_attack = false;

        // Set by BoardCardFX.OnAttack when the attacker has an ability-specific hit FX
        // (DESTROZAR, GOLPEAR, VOLAR, SUMERGIR, EMBESTIR, INTOXICAR attack).
        // Consumed in OnCardDamagedEvent to suppress the generic HitFX (arañazo).
        public static bool suppress_next_hit_fx = false;

        // Tracks last-known revealed state so we can show/hide icons when the server flips it.
        private bool prev_revealed = false;

        private bool back_to_hand;
        private Vector3 back_to_hand_target;

        private static List<BoardCard> card_list = new List<BoardCard>();

        void Awake()
        {
            card_list.Add(this);
            card_ui = GetComponentInChildren<CardUI>();
            Debug.Log("📌 card_ui asignado correctamente: " + (card_ui != null));
            card_fx = GetComponent<BoardCardFX>();
            canvas = GetComponentInChildren<Canvas>();
            card_glow.color = new Color(card_glow.color.r, card_glow.color.g, card_glow.color.b, 0f);
            canvas.gameObject.SetActive(false);
            status_alpha_target = 0f;

            if (equipment != null)
                equipment.Hide();

            if (status_group != null)
                status_group.alpha = 0f;
        }

        void OnDestroy()
        {
            card_list.Remove(this);
            GameClient client = GameClient.Get();
            if (client != null)
            {
                client.onCardDamaged -= OnCardDamagedEvent;
                client.onCardHealed -= OnCardHealedEvent;
                client.onAbilityStart -= OnAbilityStartBC;
            }
        }

        private void Start()
        {
            //Random slight rotation
            Vector3 board_rot = GameBoard.Get().GetAngles();
            transform.rotation = Quaternion.Euler(board_rot.x, board_rot.y, board_rot.z + Random.Range(-1f, 1f));

            GameClient client = GameClient.Get();
            if (client != null)
            {
                client.onCardDamaged += OnCardDamagedEvent;
                client.onCardHealed += OnCardHealedEvent;
                client.onAbilityStart += OnAbilityStartBC;
            }

            // Carta de reemplazo del rival: voltear automáticamente tras un breve retraso
            Card card = GetCard();
            if (card != null && client != null && card.player_id != client.GetPlayerID() && !card.revealed)
            {
                StartCoroutine(AutoRevealAICard(1.0f));
            }
        }

        private IEnumerator AutoRevealAICard(float delay)
        {
            yield return new WaitForSeconds(delay);
            Card card = GetCard();
            if (card != null && !card.revealed)
            {
                card.revealed = true;
                // Llamar SetCard() de nuevo para que los iconos (TeamIcon, AttackIcon, HpIcon)
                // se muestren — igual que hace OnMouseDown() al revelar manualmente.
                SetCard(card);
                // Lanzar SpawnFX si aún no se ha reproducido.
                if (card_fx != null && !hasPlayedSpawnFX)
                {
                    card_fx.OnSpawn();
                    hasPlayedSpawnFX = true;
                }
            }
        }

        private void OnAbilityStartBC(AbilityData iability, Card caster)
        {
            if (iability == null) return;
            // Counter-attack abilities (INTOXICAR): OnBeforeDefend + AbilityTriggerer.
            // This fires synchronously BEFORE onCardDamaged, so the flag is ready.
            bool isCounterAttack = iability.trigger == AbilityTrigger.OnBeforeDefend
                                   && iability.target == AbilityTarget.AbilityTriggerer;
            if (isCounterAttack)
                next_hit_is_counter_attack = true;
        }

        private void OnCardDamagedEvent(Card card, int value)
        {
            if (card != null && card.uid == card_uid)
            {
                if (next_hit_is_counter_attack)
                {
                    // INTOXICAR counter-attack: suppress the immediate HitFX and damage number.
                    // Delay the HP bar drop so it coincides with the poison FX at ~0.75s.
                    next_hit_is_counter_attack = false;
                    DelayDamage(value, 0.75f);
                    return;
                }

                // Retrasamos el número del corazón ~0.4s para que aparezca cuando el atacante
                // llega visualmente al objetivo (0.3s retroceso + 0.1s embestida = ~0.4s).
                TimeTool.WaitFor(0.4f, () => ShowDamageFX(value));

                // Suppress the generic HitFX when an ability-specific hit FX handles the visual.
                if (suppress_next_hit_fx)
                {
                    suppress_next_hit_fx = false;
                    return;
                }
                PlayHitFX();
            }
        }

        private void OnCardHealedEvent(Card card, int value)
        {
            // Only show heal FX if the card is face-up (revealed)
            if (card != null && card.uid == card_uid && card.revealed)
            {
                ShowHealFX(value);
            }
        }

        void Update()
        {
            if (!GameClient.Get().IsReady() || string.IsNullOrEmpty(card_uid))
                return;

            Game data = GameClient.Get().GetGameData();
            Card card = data.GetCard(card_uid);

            if (card == null)
                return;

            delayed_damage_timer -= Time.deltaTime;
            timer += Time.deltaTime;

            if (timer > 0.15f && !destroyed && !canvas.gameObject.activeSelf)
                canvas.gameObject.SetActive(true);

            PlayerControls controls = PlayerControls.Get();
            Player player = GameClient.Get().GetPlayer();

            if (!destroyed)
            {
                card_ui.SetCard(card);
                card_ui.SetHP(prev_hp);

                // Sync Attack/HP icons when the server flips card.revealed to true
                // (e.g. NextStep() reveals face-down cards before ending a turn).
                // BoardCard.SetCard() only runs on spawn/manual reveal, so we must
                // mirror the icon visibility here whenever the flag changes.
                if (card.revealed != prev_revealed)
                {
                    prev_revealed = card.revealed;
                    if (AttackIcon != null) AttackIcon.SetActive(card.revealed);
                    if (HpIcon != null) HpIcon.SetActive(card.revealed);
                    if (StatusPanel != null) StatusPanel.gameObject.SetActive(card.revealed);
                    if (StatusText != null) StatusText.enabled = card.revealed;
                }
            }

            if (!IsDamagedDelayed())
                prev_hp = card.GetHP();

            bool selected = controls.GetSelected() == this;
            Vector3 targ_pos = GetTargetPos();
            float speed = 12f;
            transform.position = Vector3.MoveTowards(transform.position, targ_pos, speed * Time.deltaTime);

            float target_alpha = (IsFocus() || selected) ? 1f : 0f;
            if (destroyed || timer < 1f)
                target_alpha = 0f;
            if (equipment != null && equipment.IsFocus())
                target_alpha = 0f;

            // 🟡 Color del glow: dorado si está seleccionada, azul/rojo si no
            Color ccolor;
            float calpha;

            if (is_selected)
            {
                ccolor = glow_selected;

                // Pulso entre 0.4 y 1.0 con efecto seno (brillo)
                float pulse = 0.3f * Mathf.Sin(Time.time * 4f) + 0.7f;
                calpha = pulse;

                // Pulso de escala (latido)
                float scale_pulse = 1f + 0.03f * Mathf.Sin(Time.time * 4f);
                transform.localScale = new Vector3(scale_pulse, scale_pulse, 1f);
            }
            else
            {
                ccolor = player.player_id == card.player_id ? glow_ally : glow_enemy;

                target_alpha = (IsFocus()) ? 1f : 0f;
                if (destroyed || timer < 1f || (equipment != null && equipment.IsFocus()))
                    target_alpha = 0f;

                calpha = Mathf.MoveTowards(card_glow.color.a, target_alpha, 4f * Time.deltaTime);

                // Reset escala cuando no está seleccionada
                transform.localScale = Vector3.one;
            }

            card_glow.color = new Color(ccolor.r, ccolor.g, ccolor.b, calpha);
            card_shadow.enabled = !destroyed && timer > 0.4f;
            // Color del sprite según estado: Stealth → gris, Paralizado → gris-azulado apagado, normal → blanco
            if (card.HasStatus(StatusType.Stealth))
                card_sprite.color = Color.gray;
            else if (card.HasStatus(StatusType.Paralysed) && card.revealed)
                card_sprite.color = new Color(0.45f, 0.45f, 0.50f, 1f);
            else
                card_sprite.color = Color.white;
            card_ui.hp.color = (destroyed || card.damage > 0) ? Color.yellow : Color.white;

            // Armor
            int armor_val = card.GetStatusValue(StatusType.Armor);
            armor.text = armor_val.ToString();
            armor.enabled = armor_val > 0;
            armor_icon.enabled = armor_val > 0;

            // Card image
            bool isPlayer = card.player_id == GameClient.Get().GetPlayerID();
            bool isRevealed = card.revealed;

            Sprite sprite = (!isRevealed && reverse_sprite != null)
                ? reverse_sprite
                : card.CardData.GetBoardArt(card.VariantData);

            if (sprite != card_sprite.sprite)
                card_sprite.sprite = sprite;

            // Frame image
            Sprite frame = card.VariantData.frame_board;
            if (frame != null && card_ui.frame_image != null)
                card_ui.frame_image.sprite = frame;

            // Equipment
            if (equipment != null)
            {
                Card equip = data.GetEquipCard(card.equipped_uid);
                equipment.SetEquip(equip);
            }

            // Ability buttons
            foreach (AbilityButton button in buttons)
                button.Hide();

            if (selected && card.player_id == player.player_id)
            {
                int index = 0;
                List<AbilityData> abilities = card.GetAbilities();
                foreach (AbilityData iability in abilities)
                {
                    if (iability != null && iability.trigger == AbilityTrigger.Activate)
                    {
                        if (index < buttons.Length)
                        {
                            AbilityButton button = buttons[index];
                            button.SetAbility(card, iability);
                            button.SetInteractable(data.CanCastAbility(card, iability));
                        }
                        index++;
                    }
                }

                Card equip = data.GetEquipCard(card.equipped_uid);
                if (equip != null)
                {
                    List<AbilityData> equip_abilities = equip.GetAbilities();
                    foreach (AbilityData iability in equip_abilities)
                    {
                        if (iability != null && iability.trigger == AbilityTrigger.Activate)
                        {
                            if (index < buttons.Length)
                            {
                                AbilityButton button = buttons[index];
                                button.SetAbility(equip, iability);
                                button.SetInteractable(data.CanCastAbility(equip, iability));
                            }
                            index++;
                        }
                    }
                }
            }

            // Status bar
            if (status_group != null)
                status_group.alpha = Mathf.MoveTowards(status_group.alpha, status_alpha_target, 5f * Time.deltaTime);
        }

        private Vector3 GetTargetPos()
        {
            Game data = GameClient.Get()?.GetGameData();
            if (data == null)
                return transform.position;

            Card card = data.GetCard(card_uid);
            if (card == null || card.slot == null)
            {
                Debug.LogWarning("❌ Carta o slot nulo en GetTargetPos para UID: " + card_uid);
                return transform.position;
            }

            if (destroyed && back_to_hand && timer > 0.5f)
                return back_to_hand_target;

            BoardSlot slot = (BoardSlot)BoardSlot.Get(card.slot);
            if (slot != null)
            {
                return slot.transform.position;
            }

            Debug.LogWarning("❌ No se encontró BoardSlot para slot: " + card.slot.ToString());
            return transform.position;
        }

        public void SetCard(Card card)
        {
            this.card_uid = card.uid;
            this.card_ui.SetCard(card);

            CardData icard = CardData.Get(card.card_id);
            if (card == null || card_uid == null)
                return;
            {
                card_sprite.sprite = icard.GetBoardArt(card.VariantData);
                armor.enabled = false;
                armor_icon.enabled = false;
                status_alpha_target = 0f;
            }

            prev_hp = card.GetHP();
            prev_revealed = card.revealed; // Keep in sync so Update() doesn't re-trigger on spawn

            // Determinar si ocultar la UI
            // 📌 Ocultar info si la carta está boca abajo
            bool isPlayer = card.player_id == GameClient.Get().GetPlayerID();
            bool isRevealed = card.revealed;
            bool hideUI = !isRevealed;

            // El TeamIcon solo aparece en el hover/preview (CardUI), nunca en las cartas de combate
            if (TeamIcon != null)
                TeamIcon.SetActive(false);

            if (AttackIcon != null)
                AttackIcon.SetActive(!hideUI);

            if (HpIcon != null)
                HpIcon.SetActive(!hideUI);

            if (StatusPanel != null)
                StatusPanel.gameObject.SetActive(!hideUI);

            if (StatusText != null)
                StatusText.enabled = !hideUI;

            // 📌 Posicionar en el slot correspondiente
            if (card.slot.IsValid())
            {
                BoardSlot slot = (BoardSlot)BoardSlot.Get(card.slot);

                if (slot != null)
                {
                    transform.position = slot.transform.position;
                }
                else
                {
                    Debug.LogWarning($"⚠️ Slot no encontrado para {card.card_id} con slot {card.slot}");
                }
            }
            else
            {
                Debug.LogWarning($"❌ Slot inválido para {card.card_id}");
            }

            if (card_glow != null)
                card_glow.enabled = true;
        }

        public void SetOrder(int order)
        {
            card_sprite.sortingOrder = order;
            canvas.sortingOrder = order + 1;
        }

        public void SetSelectedVisual(bool selected)
        {
            is_selected = selected;
        }

        public void ShowDamage(int amount)
        {

        }

        public void ShowDamageFX(int amount)
        {
            if (damageFXPrefab == null)
                return;

            GameObject fx = Instantiate(damageFXPrefab, transform);
            fx.transform.position = card_ui.hp.transform.position;

            DamageFX dmg = fx.GetComponent<DamageFX>();
            if (dmg != null)
                dmg.SetValue("-" + amount);
        }

        public void ShowHealFX(int amount)
        {
            if (damageFXPrefab == null)
                return;

            GameObject fx = Instantiate(damageFXPrefab, transform);
            fx.transform.position = card_ui.hp.transform.position;

            DamageFX dmg = fx.GetComponent<DamageFX>();
            if (dmg != null)
                dmg.SetValue("+" + amount);
        }

        public void PlayHitFX()
        {
            GameObject fx = Resources.Load<GameObject>("FX/HitFX");
            if (fx != null)
                Instantiate(fx, transform.position, Quaternion.identity);
        }

        public void Destroy()
        {
            if (!destroyed)
            {
                Game data = GameClient.Get().GetGameData();
                Card card = data.GetCard(card_uid);
                Player player = data.GetPlayer(card.player_id);

                destroyed = true;
                timer = 0f;
                status_alpha_target = 0f;
                card_glow.enabled = false;
                card_shadow.enabled = false;

                SetOrder(card_sprite.sortingOrder - 2);
                Destroy(gameObject, 1.3f);

                TimeTool.WaitFor(0.8f, () =>
                {
                    canvas.gameObject.SetActive(false);
                });

                GameBoard board = GameBoard.Get();
                if (player.HasCard(player.cards_hand, card) || player.HasCard(player.cards_deck, card))
                {
                    back_to_hand = true;
                    back_to_hand_target = player.player_id == GameClient.Get().GetPlayerID() ? -board.transform.up : board.transform.up;
                    back_to_hand_target = back_to_hand_target * 10f;
                }

                if (!back_to_hand)
                {
                    card.hp = 0;
                    card_ui.SetCard(card);
                }

                if (onKill != null)
                    onKill.Invoke();
            }
        }

        //Offset the HP visuals by a value so the HP dont go down before end of animation (like a projectile)
        public void DelayDamage(int damage, float duration = 1f)
        {
            if (damage != 0)
            {
                delayed_damage_timer = duration;
            }
        }

        public bool IsDamagedDelayed()
        {
            return delayed_damage_timer > 0f;
        }

        private void ShowStatusBar()
        {
            Card card = GetCard();
            if (card != null && status_text != null && !destroyed)
            {
                string stxt = GetStatusText();
                string ttxt = GetTraitText();

                if (stxt.Length > 0 && ttxt.Length > 0)
                    status_text.text = ttxt + ", " + stxt;
                else
                    status_text.text = ttxt + stxt;
            }

            bool show_status = status_text != null && status_text.text.Length > 0;
            status_alpha_target = show_status ? 1f : 0f;
        }

        public string GetStatusText()
        {
            Card card = GetCard();
            string txt = "";
            foreach (CardStatus astatus in card.GetAllStatus())
            {
                StatusData istats = StatusData.Get(astatus.type);
                if (istats != null && !string.IsNullOrEmpty(istats.title))
                {
                    int ival = Mathf.Max(astatus.value, Mathf.CeilToInt(astatus.duration / 2f));
                    string sval = ival > 1 ? " " + ival : "";
                    txt += istats.GetTitle() + sval + ", ";
                }
            }
            if (txt.Length > 2)
                txt = txt.Substring(0, txt.Length - 2);
            return txt;
        }

        public string GetTraitText()
        {
            Card card = GetCard();
            string txt = "";
            foreach (CardTrait atrait in card.GetAllTraits())
            {
                TraitData itrait = TraitData.Get(atrait.id);
                if (itrait != null && !string.IsNullOrEmpty(itrait.title))
                {
                    int ival = atrait.value;
                    string sval = ival > 1 ? " " + ival : "";
                    txt += itrait.GetTitle() + sval + ", ";
                }
            }
            if (txt.Length > 2)
                txt = txt.Substring(0, txt.Length - 2);
            return txt;
        }

        public bool IsDead()
        {
            return destroyed;
        }

        public bool IsFocus()
        {
            return focus;
        }

        public bool IsEquipFocus()
        {
            return equipment != null && equipment.IsFocus();
        }

        public void OnMouseEnter()
        {
            if (GameUI.IsUIOpened())
                return;

            if (GameTool.IsMobile())
                return;

            Card card = GetCard(); // ✅ Añadir esto
            if (!card.revealed)
                return;

            focus = true;
            ShowStatusBar();
        }

        public void OnMouseExit()
        {
            focus = false;
            status_alpha_target = 0f;
        }

        public void OnMouseDown()
        {
            if (GameUI.IsOverUILayer("UI"))
                return;

            Player player = GameClient.Get().GetPlayer();
            Card card = GetCard();

            // Solo si es tu carta y está oculta
            if (card != null && card.player_id == player.player_id && !card.revealed)
            {
                card.revealed = true;
                SetCard(card);
                ShowStatusBar();

                // 🌀 Ejecutar SpawnFX al revelar manualmente
                if (!hasPlayedSpawnFX && card_fx != null)
                {
                    card_fx.OnSpawn();
                    hasPlayedSpawnFX = true;
                }
                // ✅ Mostrar preview/status inmediatamente tras clic
                OnMouseEnter();

                return;
            }
            Debug.Log("🖱️ Carta clicada");
            PlayerControls.Get().SelectCard(this);

            if (GameTool.IsMobile())
            {
                focus = true;
                ShowStatusBar();
            }
        }

        public void OnMouseUp()
        {

        }

        public void OnMouseOver()
        {
            if (Input.GetMouseButtonDown(1))
            {
                PlayerControls.Get().SelectCardRight(this);
            }
        }

        public string GetCardUID()
        {
            return card_uid;
        }

        //Return main card (not equip)
        public Card GetCard()
        {
            Game data = GameClient.Get().GetGameData();
            Card card = data.GetCard(card_uid);
            return card;
        }

        //Return equip card
        public Card GetEquipCard()
        {
            Game data = GameClient.Get().GetGameData();
            Card card = GetCard();
            Card equip = data?.GetEquipCard(card.equipped_uid);
            return equip;
        }

        //Return either main or equip card based on which one is focused
        public Card GetFocusCard()
        {
            if (IsEquipFocus())
                return GetEquipCard();
            return GetCard();
        }

        public CardData GetCardData()
        {
            Card card = GetCard();
            if (card != null)
                return CardData.Get(card.card_id);
            return null;
        }

        public Slot GetSlot()
        {
            return GetCard().slot;
        }

        public BoardCardFX GetCardFX()
        {
            return card_fx;
        }

        public CardData CardData { get { return GetCardData(); } }

        public static int GetNbCardsBoardPlayer(int player_id)
        {
            int nb = 0;
            foreach (BoardCard acard in card_list)
            {
                if (acard != null && acard.GetCard().player_id == player_id)
                    nb++;
            }
            return nb;
        }

        public static BoardCard GetNearestPlayer(Vector3 pos, int skip_player_id, BoardCard skip, float range = 2f)
        {
            BoardCard nearest = null;
            float min_dist = range;
            foreach (BoardCard card in card_list)
            {
                float dist = (card.transform.position - pos).magnitude;
                if (dist < min_dist && card != skip && skip_player_id != card.GetCard().player_id)
                {
                    min_dist = dist;
                    nearest = card;
                }
            }
            return nearest;
        }

        public static BoardCard GetNearest(Vector3 pos, BoardCard skip, float range = 2f)
        {
            BoardCard nearest = null;
            float min_dist = range;
            foreach (BoardCard card in card_list)
            {
                float dist = (card.transform.position - pos).magnitude;
                if (dist < min_dist && card != skip)
                {
                    min_dist = dist;
                    nearest = card;
                }
            }
            return nearest;
        }

        public static BoardCard GetFocus()
        {
            if (GameUI.IsOverUI())
                return null;

            foreach (BoardCard card in card_list)
            {
                if (card.IsFocus() || card.IsEquipFocus())
                    return card;
            }
            return null;
        }

        public static void UnfocusAll()
        {
            foreach (BoardCard card in card_list)
            {
                card.focus = false;
                card.status_alpha_target = 0f;
            }
        }

        public static BoardCard Get(string uid)
        {
            foreach (BoardCard card in card_list)
            {
                if (card.card_uid == uid)
                    return card;
            }
            return null;
        }

        public static List<BoardCard> GetAll()
        {
            return card_list;
        }
    }
}