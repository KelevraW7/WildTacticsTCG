using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Profiling;
using TcgEngine.UI;
using TcgEngine.Client;
using TcgEngine.AI;

namespace TcgEngine.Gameplay
{
    /// <summary>
    /// Execute and resolves game rules and logic
    /// </summary>

    public class GameLogic
    {
        public UnityAction onGameStart;
        public UnityAction<Player> onGameEnd;          //Winner

        public UnityAction onTurnStart;
        public UnityAction onTurnPlay;
        public UnityAction onTurnEnd;

        public UnityAction<Card, Slot> onCardPlayed;
        public UnityAction<Card, Slot> onCardSummoned;
        public UnityAction<Card, Slot> onCardMoved;
        public UnityAction<Card> onCardTransformed;
        public UnityAction<Card> onCardDiscarded;
        public UnityAction<int> onCardDrawn;
        public UnityAction<int> onRollValue;

        public UnityAction<AbilityData, Card> onAbilityStart;
        public UnityAction<AbilityData, Card, Card> onAbilityTargetCard;  //Ability, Caster, Target
        public UnityAction<AbilityData, Card, Slot> onAbilityTargetSlot;
        public UnityAction<AbilityData, Card> onAbilityEnd;

        public UnityAction<Card, Card> onAttackStart;  //Attacker, Defender
        public UnityAction<Card, Card> onAttackEnd;     //Attacker, Defender

        public UnityAction<Card, int> onCardDamaged;
        public UnityAction<Card, int> onCardHealed;

        public UnityAction<Card, Card> onSecretTrigger;    //Secret, Triggerer
        public UnityAction<Card, Card> onSecretResolve;    //Secret, Triggerer

        public UnityAction onRefresh;

        private Game game_data;

        private ResolveQueue resolve_queue;
        private bool is_ai_predict = false;

        private System.Random random = new System.Random();

        private ListSwap<Card> card_array = new ListSwap<Card>();
        private ListSwap<Player> player_array = new ListSwap<Player>();
        private ListSwap<Slot> slot_array = new ListSwap<Slot>();
        private ListSwap<CardData> card_data_array = new ListSwap<CardData>();
        private List<Card> cards_to_clear = new List<Card>();

        // GOLPEAR: deferred card draws — cards killed during the 1st GOLPEAR attack are drawn at
        // the START of that player's next turn, so the new card cannot be targeted by the 2nd attack
        private bool is_golpear_first_hit = false;
        private List<(int playerId, Slot slot)> pending_card_draws = new List<(int, Slot)>();

        public GameLogic(bool is_ai)
        {
            //is_instant ignores all gameplay delays and process everything immediately, needed for AI prediction
            resolve_queue = new ResolveQueue(null, is_ai);
            is_ai_predict = is_ai;
        }

        public GameLogic(Game game)
        {
            game_data = game;
            resolve_queue = new ResolveQueue(game, false);
        }

        public virtual void SetData(Game game)
        {
            game_data = game;
            resolve_queue.SetData(game);
        }

        public virtual void Update(float delta)
        {
            resolve_queue.Update(delta);
        }

        //----- Turn Phases ----------

        public virtual void StartGame()
        {
            if (game_data.state == GameState.GameEnded)
                return;

            game_data.state = GameState.Play;
            game_data.first_player = 0;
            game_data.current_player = game_data.first_player;
            game_data.turn_count = 1;

            WildAssignDecks();
            WildActivateScenario();
            WildPlaceInitialCards();

            RefreshData();
            onGameStart?.Invoke();

            StartTurn();
        }

        private void WildAssignDecks()
        {
            List<CardData> allCards = new List<CardData>(CardData.GetAll());

            // ── Filtrar criaturas/doradas a las cartas desbloqueadas del jugador ────────
            // Eventos y Escenarios no tienen sistema de propiedad: se usan todos los disponibles.
            // Las criaturas sí: solo las que el jugador haya desbloqueado entran al pool.
            UserData udata      = Authenticator.Get()?.UserData;
            VariantData defVar  = VariantData.GetDefault();
            string variant_id   = defVar?.id ?? "";

            List<CardData> creaturePool;
            if (udata != null)
            {
                creaturePool = allCards.FindAll(c =>
                    c.type != CardType.Event &&
                    c.type != CardType.Scenario &&
                    udata.GetCardQuantity(c.id, variant_id, true) > 0);

                if (creaturePool.Count == 0)
                {
                    // Fallback: sin cartas desbloqueadas (no debería ocurrir con el pack inicial)
                    Debug.LogWarning("[WildAssignDecks] Pool de criaturas vacío — usando todas las cartas como fallback.");
                    creaturePool = allCards.FindAll(c => c.type != CardType.Event && c.type != CardType.Scenario);
                }
            }
            else
            {
                creaturePool = allCards.FindAll(c => c.type != CardType.Event && c.type != CardType.Scenario);
            }

            // ── Separar eventos y escenarios (pool global, sin filtro de propiedad) ────
            List<CardData> eventPool    = allCards.FindAll(c => c.type == CardType.Event);
            List<CardData> scenarioPool = allCards.FindAll(c => c.type == CardType.Scenario);

            // ── Separar doradas y comunes dentro del pool desbloqueado ────────────────
            List<CardData> goldPool   = creaturePool.FindAll(c => c.team != null && c.team.id.ToLower() == "gold");
            List<CardData> commonPool = creaturePool.FindAll(c => c.team == null || c.team.id.ToLower() != "gold");

            Debug.Log($"🃏 [WildAssignDecks] Pool desbloqueado — doradas: {goldPool.Count} | comunes: {commonPool.Count} | eventos: {eventPool.Count}");

            foreach (Player player in game_data.players)
            {
                // ── Mazo de criaturas (11 cartas: 1 dorada + 10 comunes) ──────────────
                // Se consume el pool en orden (sin repetición entre jugadores).
                // El pack inicial tiene 20 comunes + 6 doradas = suficiente para 2 jugadores
                // sin repetición (10+10 comunes, 1+1 doradas).
                List<CardData> selected = new List<CardData>();

                // Exactamente 1 carta dorada por jugador
                if (goldPool.Count > 0)
                {
                    selected.Add(goldPool[0]);
                    goldPool.RemoveAt(0);
                }
                else
                {
                    Debug.LogWarning($"⚠️ Sin cartas doradas desbloqueadas para el jugador {player.player_id}");
                }

                // Rellenar hasta 11 con comunes (sin repetición entre jugadores)
                while (selected.Count < 11 && commonPool.Count > 0)
                {
                    selected.Add(commonPool[0]);
                    commonPool.RemoveAt(0);
                }

                // Barajar las cartas seleccionadas antes de añadirlas al mazo
                ShuffleCardDataList(selected);

                foreach (CardData data in selected)
                {
                    Card card = Card.Create(data, VariantData.GetDefault(), player);
                    player.cards_deck.Add(card);
                }

                int goldCount = selected.FindAll(c => c.team != null && c.team.id.ToLower() == "gold").Count;
                Debug.Log($"🃏 Jugador {player.player_id}: {player.cards_deck.Count} criaturas ({goldCount} dorada/s)");

                // ── Mazo de eventos: 10 cartas por jugador del pool global ──────────
                List<CardData> availableEvents = new List<CardData>(eventPool);
                ShuffleCardDataList(availableEvents);

                int eventCount = Mathf.Min(10, availableEvents.Count);
                for (int i = 0; i < eventCount; i++)
                {
                    Card ecard = Card.Create(availableEvents[i], VariantData.GetDefault(), player);
                    player.cards_event_deck.Add(ecard);
                }

                Debug.Log($"🎴 Jugador {player.player_id}: {player.cards_event_deck.Count} cartas de evento en el mazo");
            }
        }

        /// <summary>
        /// Selecciona al azar una carta de escenario del pool y la activa para la partida.
        /// Si no hay cartas de escenario en Resources, la partida sigue sin escenario.
        /// </summary>
        private void WildActivateScenario()
        {
            List<CardData> pool = CardData.GetAll().FindAll(c => c.type == CardType.Scenario);
            if (pool.Count == 0)
            {
                Debug.Log("🗺️ [Scenario] No hay cartas de escenario en el proyecto — partida sin escenario.");
                return;
            }

            CardData chosen = pool[random.Next(pool.Count)];
            game_data.active_scenario_id = chosen.id;

            Debug.Log($"🗺️ [Scenario] Escenario activo: {chosen.title}");

            // Disparar habilidades OnPlay del escenario (afectan a ambos jugadores)
            TriggerScenarioAbility(AbilityTrigger.OnPlay);
        }

        /// <summary>
        /// Dispara las habilidades del escenario activo para el trigger dado.
        /// Extiende GameLogic en subclases para implementar los efectos de cada escenario.
        /// </summary>
        protected virtual void TriggerScenarioAbility(AbilityTrigger trigger)
        {
            CardData scenario = game_data.GetScenarioData();
            if (scenario == null) return;

            // Cada habilidad con el trigger correspondiente se procesa aquí.
            // En esta versión base solo se hace log; los efectos concretos se implementarán
            // carta a carta cuando el diseño de cada escenario esté finalizado.
            foreach (AbilityData ability in scenario.abilities)
            {
                if (ability == null || ability.trigger != trigger) continue;
                Debug.Log($"🗺️ [Scenario] Trigger {trigger} → {ability.id}  (pendiente de implementación por escenario)");
                // TODO: ResolveScenarioAbility(ability, scenario);
            }
        }

        private void ShuffleCardDataList(List<CardData> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                CardData tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        private void WildPlaceInitialCards()
        {
            foreach (Player player in game_data.players)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (player.cards_deck.Count == 0)
                        break;

                    Card card = player.cards_deck[0];
                    player.cards_deck.RemoveAt(0);

                    card.slot = new Slot(i + 1, 1, player.player_id);
                    card.revealed = player.player_id != 0; // player 0 (humano): boca abajo; player 1 (IA): boca arriba
                    player.cards_board.Add(card);

                    // Apply OnPlay status effects directly (resolve queue isn't running at game start)
                    // This handles SUMERGIR (gives Shell) and any other OnPlay-Self-Status abilities
                    foreach (AbilityData ability in card.GetAbilities())
                    {
                        if (ability != null && ability.trigger == AbilityTrigger.OnPlay)
                        {
                            foreach (StatusData stat in ability.status)
                                card.AddStatus(stat, ability.value, ability.duration);
                        }
                    }

                    Debug.Log($"📍 [Server] Carta {card.card_id} colocada en slot {i + 1} del jugador {player.player_id}");
                }
            }
        }

        public virtual void StartTurn()
        {
            if (game_data.state == GameState.GameEnded)
                return;

            // Reduce status durations at turn START so timed effects (e.g. Paralysed from EMBESTIR)
            // expire exactly when the next player's turn begins — after all attack animations finish —
            // rather than instantly when EndTurn is called mid-animation.
            foreach (Player aplayer in game_data.players)
            {
                aplayer.ReduceStatusDurations();
                foreach (Card card in aplayer.cards_board)
                    card.ReduceStatusDurations();
                foreach (Card card in aplayer.cards_equip)
                    card.ReduceStatusDurations();
            }

            ClearTurnData();
            game_data.has_attacked_this_turn = false;
            game_data.phase = GamePhase.StartTurn;
            RefreshData();
            onTurnStart?.Invoke();

            Player player = game_data.GetActivePlayer();

            // Eliminar robo automático de carta por turno
            // if (game_data.turn_count > 1 || player.player_id != game_data.first_player)
            // {
            //     DrawCard(player, GameplayData.Get().cards_per_turn);
            // }

            // Turn timer and history
            game_data.turn_timer = GameplayData.Get().turn_duration;
            player.history_list.Clear();

            // Poison
            if (player.HasStatus(StatusType.Poisoned))
            {
                // Efectos por veneno
            }

            // Refresh cards and statuses
            for (int i = player.cards_board.Count - 1; i >= 0; i--)
            {
                Card card = player.cards_board[i];

                if (!card.HasStatus(StatusType.Sleep))
                    card.Refresh();

                if (card.HasStatus(StatusType.Poisoned))
                {
                    DamageCard(card, card.GetStatusValue(StatusType.Poisoned));
                }
            }

            // Deferred GOLPEAR replacement draws: fill any empty slots left by the previous turn's
            // first GOLPEAR attack before this player's StartOfTurn abilities fire
            for (int i = pending_card_draws.Count - 1; i >= 0; i--)
            {
                if (pending_card_draws[i].playerId == player.player_id)
                {
                    int pid = pending_card_draws[i].playerId;
                    Slot refillSlot = pending_card_draws[i].slot;
                    pending_card_draws.RemoveAt(i);
                    resolve_queue.AddCallback(() => DrawCardToBoard(game_data.GetPlayer(pid), refillSlot));
                }
            }

            // Habilidades continuas y de inicio de turno
            UpdateOngoing();
            TriggerPlayerCardsAbilityType(player, AbilityTrigger.StartOfTurn);
            TriggerPlayerSecrets(player, AbilityTrigger.StartOfTurn);
            TriggerScenarioAbility(AbilityTrigger.StartOfTurn);  // Efectos de escenario cada turno

            resolve_queue.AddCallback(StartMainPhase);
            resolve_queue.ResolveAll(0.2f);

            StartMainPhase();
        }

        public virtual void StartNextTurn()
        {
            if (game_data.state == GameState.GameEnded)
                return;

            game_data.current_player = (game_data.current_player + 1) % game_data.settings.nb_players;

            if (game_data.current_player == game_data.first_player)
                game_data.turn_count++;

            CheckForWinner();
            StartTurn();
        }

        public virtual void StartMainPhase()
        {
            if (game_data.state == GameState.GameEnded)
                return;
            if (game_data.phase == GamePhase.Main)
                return; // ya está en Main (llamada directa llegó primero), ignorar la cola

            game_data.phase = GamePhase.Main;
            onTurnPlay?.Invoke();
            RefreshData();

            // Auto-pass: si el jugador activo no tiene ninguna acción legal disponible
            // (todas sus criaturas están paralizadas/agotadas y no puede jugar cartas),
            // terminamos el turno automáticamente para que el juego no se quede bloqueado.
            Player activePlayer = game_data.GetActivePlayer();
            if (activePlayer != null && !HasAnyLegalAction(activePlayer))
            {
                resolve_queue.AddCallback(EndTurn);
                resolve_queue.ResolveAll(0.8f); // pausa breve para que el jugador vea el estado
            }
        }

        /// <summary>
        /// Devuelve true si el jugador tiene al menos una acción legal disponible:
        /// atacar con una criatura, jugar una carta de la mano, o jugar una carta de evento (solo humanos).
        /// </summary>
        protected virtual bool HasAnyLegalAction(Player player)
        {
            Player opponent = game_data.GetOpponentPlayer(player.player_id);

            // ¿Puede alguna criatura atacar a algún objetivo?
            foreach (Card attacker in player.cards_board)
            {
                foreach (Card target in opponent.cards_board)
                {
                    if (game_data.CanAttackTarget(attacker, target))
                        return true;
                }
                // ¿Puede atacar al jugador rival directamente?
                if (game_data.CanAttackTarget(attacker, opponent))
                    return true;
            }

            // ¿Puede jugar alguna carta de la mano a algún slot válido?
            foreach (Card card in player.cards_hand)
            {
                int p = Slot.GetP(player.player_id);
                for (int x = Slot.x_min; x <= Slot.x_max; x++)
                {
                    for (int y = Slot.y_min; y <= Slot.y_max; y++)
                    {
                        Slot slot = new Slot(x, y, p);
                        if (game_data.CanPlayCard(card, slot))
                            return true;
                    }
                }
            }

            // ¿El jugador humano tiene cartas de evento disponibles?
            // La IA no sabe jugar cartas de evento todavía, así que se excluye.
            if (!player.is_ai && player.cards_event_hand.Count > 0)
                return true;

            return false;
        }

        public virtual void EndTurn()
        {
            if (game_data.state == GameState.GameEnded)
                return;
            if (game_data.phase != GamePhase.Main)
                return;

            // Revelar todas las cartas boca-abajo del jugador activo antes de ceder el turno.
            // Esto cubre tanto el caso normal (el jugador pulsa "Fin de turno") como el caso
            // en que el juego llama EndTurn directamente (ej: INTOXICAR mata al atacante).
            // Sin esto, las cartas de reemplazo del humano llegan boca-abajo al turno de la IA.
            Player active_player = game_data.GetActivePlayer();
            if (active_player != null)
            {
                foreach (Card c in active_player.cards_board)
                {
                    if (!c.revealed)
                        c.revealed = true;
                }
            }

            game_data.selector = SelectorType.None;
            game_data.phase = GamePhase.EndTurn;

            //End of turn abilities
            Player player = game_data.GetActivePlayer();
            TriggerPlayerCardsAbilityType(player, AbilityTrigger.EndOfTurn);
            TriggerScenarioAbility(AbilityTrigger.EndOfTurn);  // Efectos de escenario al fin de turno

            onTurnEnd?.Invoke();
            RefreshData();

            resolve_queue.AddCallback(StartNextTurn);
            resolve_queue.ResolveAll(0.2f);
        }

        //End game with winner
        public virtual void EndGame(int winner)
        {
            if (game_data.state != GameState.GameEnded)
            {
                game_data.state = GameState.GameEnded;
                game_data.phase = GamePhase.None;
                game_data.selector = SelectorType.None;
                game_data.current_player = winner; //Winner player
                resolve_queue.Clear();
                Player player = game_data.GetPlayer(winner);
                onGameEnd?.Invoke(player);
                RefreshData();
            }
        }

        //Progress to the next step/phase
        public virtual void NextStep()
        {
            if (game_data.state == GameState.GameEnded)
                return;

            CancelSelection();

            // WildTactics: reveal all face-down player cards before ending the turn by timer
            Player active = game_data.GetActivePlayer();
            if (active != null)
            {
                foreach (Card card in active.cards_board)
                {
                    if (!card.revealed)
                        card.revealed = true;
                }
            }

            //Add to resolve queue in case its still resolving
            resolve_queue.AddCallback(EndTurn);
            resolve_queue.ResolveAll();
        }

        //Check if a player is winning the game, if so end the game
        //WildTactics: a player is eliminated when they have no cards on the board and no cards left in the deck
        protected virtual void CheckForWinner()
        {
            int count_alive = 0;
            Player alive = null;
            foreach (Player player in game_data.players)
            {
                bool isEliminated = player.cards_board.Count == 0 && player.cards_deck.Count == 0;
                if (!isEliminated)
                {
                    alive = player;
                    count_alive++;
                }
            }

            if (count_alive == 0)
            {
                EndGame(-1); //Draw
            }
            else if (count_alive == 1)
            {
                EndGame(alive.player_id); //Winner
            }
        }

        protected virtual void ClearTurnData()
        {
            game_data.selector = SelectorType.None;
            resolve_queue.Clear();
            card_array.Clear();
            player_array.Clear();
            slot_array.Clear();
            card_data_array.Clear();
            game_data.last_played = null;
            game_data.last_destroyed = null;
            game_data.last_target = null;
            game_data.last_summoned = null;
            game_data.ability_triggerer = null;
            game_data.selected_value = 0;
            game_data.ability_played.Clear();
            game_data.cards_attacked.Clear();
            game_data.golpear_pending_uid = "";
            game_data.golpear_first_target_uid = "";
        }

        //--- Setup ------

        //Set deck using a Deck in Resources
        public virtual void SetPlayerDeck(Player player, DeckData deck)
        {
            player.cards_all.Clear();
            player.cards_deck.Clear();
            player.deck = deck.id;

            VariantData variant = VariantData.GetDefault();

            foreach (CardData card in deck.cards)
            {
                if (card != null)
                {
                    Card acard = Card.Create(card, variant, player);
                    player.cards_deck.Add(acard);
                }
            }

            DeckPuzzleData puzzle = deck as DeckPuzzleData;

            //Board cards
            if (puzzle != null)
            {
                foreach (DeckCardSlot card in puzzle.board_cards)
                {
                    Card acard = Card.Create(card.card, variant, player);
                    acard.slot = new Slot(card.slot, Slot.GetP(player.player_id));
                    player.cards_board.Add(acard);
                }
            }

            //Shuffle deck
            if (puzzle == null || !puzzle.dont_shuffle_deck)
                ShuffleDeck(player.cards_deck);
        }

        //Set deck using custom deck in save file or database
        public virtual void SetPlayerDeck(Player player, UserDeckData deck)
        {
            player.cards_all.Clear();
            player.cards_deck.Clear();
            player.deck = deck.tid;


            foreach (UserCardData card in deck.cards)
            {
                CardData icard = CardData.Get(card.tid);
                VariantData variant = VariantData.Get(card.variant);
                if (icard != null && variant != null)
                {
                    for (int i = 0; i < card.quantity; i++)
                    {
                        Card acard = Card.Create(icard, variant, player);
                        player.cards_deck.Add(acard);
                    }
                }
            }

            //Shuffle deck
            ShuffleDeck(player.cards_deck);
        }

        //---- Gameplay Actions --------------

        public virtual void PlayCard(Card card, Slot slot, bool skip_cost = false)
        {
            if (game_data.CanPlayCard(card, slot, skip_cost))
            {
                Player player = game_data.GetPlayer(card.player_id);

                //Play card
                player.RemoveCardFromAllGroups(card);

                //Add to board
                CardData icard = card.CardData;
                if (icard.IsBoardCard())
                {
                    player.cards_board.Add(card);
                    card.slot = slot;
                    card.exhausted = true; //Cant attack first turn
                }
                else if (icard.IsEquipment())
                {
                    Card bearer = game_data.GetSlotCard(slot);
                    EquipCard(bearer, card);
                    card.exhausted = true;
                }
                else if (icard.IsSecret())
                {
                    player.cards_secret.Add(card);
                }
                else
                {
                    player.cards_discard.Add(card);
                    card.slot = slot; //Save slot in case spell has PlayTarget
                }

                //History
                if (!is_ai_predict && !icard.IsSecret())
                    player.AddHistory(GameAction.PlayCard, card);

                //Update ongoing effects
                game_data.last_played = card.uid;
                UpdateOngoing();

                //Trigger abilities
                TriggerSecrets(AbilityTrigger.OnPlayOther, card); //After playing card
                TriggerCardAbilityType(AbilityTrigger.OnPlay, card);
                TriggerOtherCardsAbilityType(AbilityTrigger.OnPlayOther, card);

                RefreshData();

                onCardPlayed?.Invoke(card, slot);
                resolve_queue.ResolveAll(0.3f);
            }
        }

        public virtual void MoveCard(Card card, Slot slot, bool skip_cost = false)
        {
            if (game_data.CanMoveCard(card, slot, skip_cost))
            {
                card.slot = slot;

                //Moving doesn't really have any effect in demo so can be done indefinitely
                //if(!skip_cost)
                //card.exhausted = true;
                //card.RemoveStatus(StatusEffect.Stealth);
                //player.AddHistory(GameAction.Move, card);

                //Also move the equipment
                Card equip = game_data.GetEquipCard(card.equipped_uid);
                if (equip != null)
                    equip.slot = slot;

                UpdateOngoing();
                RefreshData();

                onCardMoved?.Invoke(card, slot);
                resolve_queue.ResolveAll(0.2f);
            }
        }

        public virtual void CastAbility(Card card, AbilityData iability)
        {
            if (game_data.CanCastAbility(card, iability))
            {
                Player player = game_data.GetPlayer(card.player_id);
                if (!is_ai_predict && iability.target != AbilityTarget.SelectTarget)
                    player.AddHistory(GameAction.CastAbility, card, iability);
                card.RemoveStatus(StatusType.Stealth);
                TriggerCardAbility(iability, card);
                resolve_queue.ResolveAll();
            }
        }

        public virtual void AttackTarget(Card attacker, Card target, bool skip_cost = false)
        {
            if (game_data.CanAttackTarget(attacker, target, skip_cost))
            {
                Player player = game_data.GetPlayer(attacker.player_id);
                if (!is_ai_predict)
                    player.AddHistory(GameAction.Attack, attacker, target);

                game_data.last_target = target.uid;

                //Trigger before attack abilities
                TriggerCardAbilityType(AbilityTrigger.OnBeforeAttack, attacker, target);
                TriggerCardAbilityType(AbilityTrigger.OnBeforeDefend, target, attacker);
                TriggerSecrets(AbilityTrigger.OnBeforeAttack, attacker);
                TriggerSecrets(AbilityTrigger.OnBeforeDefend, target);

                //Resolve attack
                resolve_queue.AddAttack(attacker, target, ResolveAttack, skip_cost);
                resolve_queue.ResolveAll();
            }
        }

        protected virtual void ResolveAttack(Card attacker, Card target, bool skip_cost)
        {
            // ── Caso 1: el objetivo abandonó el tablero ─────────────────────────────
            // No hay nada a lo que golpear — simplemente terminamos el turno.
            if (!game_data.IsOnBoard(target))
            {
                if (game_data.phase == GamePhase.Main)
                {
                    game_data.has_attacked_this_turn = true;
                    game_data.golpear_pending_uid = "";
                    game_data.golpear_first_target_uid = "";
                    EndTurn();
                }
                return;
            }

            // ── Caso 2: el atacante murió por OnBeforeDefend (INTOXICAR) ─────────────
            // El combate es simultáneo: el ataque ya estaba comprometido, así que el
            // atacante AÚN inflige su daño y dispara sus habilidades OnAfterAttack (ej.
            // EMBESTIR → paraliza al rival) aunque haya muerto del veneno.
            // Lanzamos onAttackStart para que el cliente muestre audio/FX del impacto.
            if (!game_data.IsOnBoard(attacker))
            {
                onAttackStart?.Invoke(attacker, target);
                UpdateOngoing();
                resolve_queue.AddAttack(attacker, target, ResolveAttackHit, skip_cost);
                resolve_queue.AddCallback(() =>
                {
                    if (game_data.phase == GamePhase.Main)
                    {
                        game_data.has_attacked_this_turn = true;
                        game_data.golpear_pending_uid = "";
                        game_data.golpear_first_target_uid = "";
                        EndTurn();
                    }
                });
                resolve_queue.SetDelay(0f);
                resolve_queue.ResolveAll();
                return;
            }

            // ── Caso 3: ambas cartas siguen en el tablero — flujo normal ─────────────
            onAttackStart?.Invoke(attacker, target);
            attacker.RemoveStatus(StatusType.Stealth);
            UpdateOngoing();

            resolve_queue.AddAttack(attacker, target, ResolveAttackHit, skip_cost);
            resolve_queue.SetDelay(0f);
            resolve_queue.ResolveAll();
        }

        protected virtual void ResolveAttackHit(Card attacker, Card target, bool skip_cost)
        {
            int datt1 = CalcularDañoTipo(attacker, target);
            int datt2 = CalcularDañoTipo(target, attacker);

            Debug.Log($"🧮 Daño calculado: attacker {attacker.card_id} ({datt1}) → target {target.card_id} ({target.GetHP()} HP)");

            if (!skip_cost)
                ExhaustBattle(attacker);

            // GOLPEAR: two-attack sequence
            bool hasGolpear = attacker.abilities.Contains("wild_golpear");
            bool isGolpearSecond = !string.IsNullOrEmpty(game_data.golpear_pending_uid)
                                   && game_data.golpear_pending_uid == attacker.uid;
            bool isGolpearFirst = hasGolpear && !isGolpearSecond;

            // Pre-signal that a GOLPEAR first hit is in progress so DiscardCard can defer the
            // opponent's replacement draw — the flag is cleared after DamageCard returns
            if (isGolpearFirst)
                is_golpear_first_hit = true;

            DamageCard(attacker, target, datt1);

            is_golpear_first_hit = false;

            // After damage, check if there is a valid second target (must be different from the first)
            Player opponent = game_data.GetOpponentPlayer(attacker.player_id);
            bool hasTargetsForSecondAttack = false;
            if (isGolpearFirst && opponent != null)
            {
                foreach (Card c in opponent.cards_board)
                {
                    if (c.uid != target.uid) // second attack must hit a different creature
                    {
                        hasTargetsForSecondAttack = true;
                        break;
                    }
                }
            }

            if (isGolpearFirst && hasTargetsForSecondAttack)
            {
                // First GOLPEAR attack: save state, skip EndTurn, unexhaust so it can attack again
                game_data.golpear_pending_uid = attacker.uid;
                game_data.golpear_first_target_uid = target.uid;
                // has_attacked_this_turn stays false so the 2nd-attack check in client doesn't block it
                attacker.exhausted = false;
            }
            else
            {
                // Normal attack OR second GOLPEAR attack OR no targets left: end the turn
                game_data.has_attacked_this_turn = true;
                game_data.golpear_pending_uid = "";
                game_data.golpear_first_target_uid = "";
                EndTurn();
            }

            UpdateOngoing();

            // Fire OnAfterAttack regardless of board status — the attack was committed,
            // so EMBESTIR → Paralysed applies even if the attacker died simultaneously
            // from an INTOXICAR counter-attack (simultaneous combat rule).
            TriggerCardAbilityType(AbilityTrigger.OnAfterAttack, attacker, target);
            if (game_data.IsOnBoard(target))
                TriggerCardAbilityType(AbilityTrigger.OnAfterDefend, target, attacker);

            // GOLPEAR safety: if the attacker died during OnAfterDefend (e.g. INTOXICAR) while
            // waiting for a second attack, the turn would never end — force EndTurn here.
            if (!string.IsNullOrEmpty(game_data.golpear_pending_uid)
                && !game_data.IsOnBoard(attacker))
            {
                game_data.has_attacked_this_turn = true;
                game_data.golpear_pending_uid = "";
                game_data.golpear_first_target_uid = "";
                EndTurn();
            }

            if (game_data.IsOnBoard(attacker))
                TriggerSecrets(AbilityTrigger.OnAfterAttack, attacker);
            if (game_data.IsOnBoard(target))
                TriggerSecrets(AbilityTrigger.OnAfterDefend, target);

            onAttackEnd?.Invoke(attacker, target);
            RefreshData();
            CheckForWinner();

            resolve_queue.ResolveAll(0.2f);
        }

        private int CalcularDañoTipo(Card atacante, Card objetivo)
        {
            int base_dano = atacante.GetAttack();
            string tipo_atacante = atacante.CardData.team?.id;
            string tipo_objetivo = objetivo.CardData.team?.id;

            if (string.IsNullOrEmpty(tipo_atacante) || string.IsNullOrEmpty(tipo_objetivo))
                return base_dano;

            if (TieneVentajaDeTipo(tipo_atacante, tipo_objetivo))
                return base_dano + 1;

            if (TieneVentajaDeTipo(tipo_objetivo, tipo_atacante))
                return base_dano > 1 ? base_dano - 1 : base_dano; // mínimo 1 si el ataque base es >= 1

            return base_dano;
        }

        private bool TieneVentajaDeTipo(string atacante, string objetivo)
        {
            return (atacante == "fire" && objetivo == "plant") ||
                   (atacante == "plant" && objetivo == "water") ||
                   (atacante == "water" && objetivo == "fire");
        }

        //Exhaust after battle
        public virtual void ExhaustBattle(Card attacker)
        {
            bool attacked_before = game_data.cards_attacked.Contains(attacker.uid);
            game_data.cards_attacked.Add(attacker.uid);
            bool attack_again = attacker.HasStatus(StatusType.Fury) && !attacked_before;
            attacker.exhausted = !attack_again;
        }

        //Redirect attack to a new target
        public virtual void RedirectAttack(Card attacker, Card new_target)
        {
            foreach (AttackQueueElement att in resolve_queue.GetAttackQueue())
            {
                if (att.attacker.uid == attacker.uid)
                {
                    att.target = new_target;
                    att.ptarget = null;
                    att.callback = ResolveAttack;
                    att.pcallback = null;
                }
            }
        }

        public virtual void RedirectAttack(Card attacker, Player new_target)
        {
            foreach (AttackQueueElement att in resolve_queue.GetAttackQueue())
            {
                if (att.attacker.uid == attacker.uid)
                {
                    att.ptarget = new_target;
                    att.target = null;
                    att.callback = null;
                }
            }
        }

        public virtual void ShuffleDeck(List<Card> cards)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                Card temp = cards[i];
                int randomIndex = random.Next(i, cards.Count);
                cards[i] = cards[randomIndex];
                cards[randomIndex] = temp;
            }
        }

        public virtual void DrawCard(Player player, int nb = 1)
        {
            for (int i = 0; i < nb; i++)
            {
                if (player.cards_deck.Count > 0 && player.cards_hand.Count < GameplayData.Get().cards_max)
                {
                    Card card = player.cards_deck[0];
                    player.cards_deck.RemoveAt(0);

                    // Solo se añaden a la mano si NO son de tipo "Character"
                    if (card.CardData.type != CardType.Character)
                    {
                        player.cards_hand.Add(card);
                        Debug.Log("✋ Robada carta válida a la mano: " + card.card_id);
                    }
                    else
                    {
                        Debug.Log("🚫 No se roba criatura a la mano: " + card.card_id);
                    }
                }
            }

            onCardDrawn?.Invoke(nb);
        }

        //Put a card from deck into discard
        public virtual void DrawDiscardCard(Player player, int nb = 1)
        {
            for (int i = 0; i < nb; i++)
            {
                if (player.cards_deck.Count > 0)
                {
                    Card card = player.cards_deck[0];
                    player.cards_deck.RemoveAt(0);
                    player.cards_discard.Add(card);
                }
            }
        }

        //Summon copy of an exiting card
        public virtual Card SummonCopy(Player player, Card copy, Slot slot)
        {
            CardData icard = copy.CardData;
            return SummonCard(player, icard, copy.VariantData, slot);
        }

        //Summon copy of an exiting card into hand
        public virtual Card SummonCopyHand(Player player, Card copy)
        {
            CardData icard = copy.CardData;
            return SummonCardHand(player, icard, copy.VariantData);
        }

        //Create a new card and send it to the board
        public virtual Card SummonCard(Player player, CardData card, VariantData variant, Slot slot)
        {
            if (!slot.IsValid())
                return null;

            if (game_data.GetSlotCard(slot) != null)
                return null;

            Card acard = SummonCardHand(player, card, variant);
            PlayCard(acard, slot, true);

            onCardSummoned?.Invoke(acard, slot);

            return acard;
        }

        //Create a new card and send it to your hand
        public virtual Card SummonCardHand(Player player, CardData card, VariantData variant)
        {
            Card acard = Card.Create(card, variant, player);
            player.cards_hand.Add(acard);
            game_data.last_summoned = acard.uid;
            return acard;
        }

        //Transform card into another one
        public virtual Card TransformCard(Card card, CardData transform_to)
        {
            card.SetCard(transform_to, card.VariantData);

            onCardTransformed?.Invoke(card);

            return card;
        }

        public virtual void EquipCard(Card card, Card equipment)
        {
            if (card != null && equipment != null && card.player_id == equipment.player_id)
            {
                if (!card.CardData.IsEquipment() && equipment.CardData.IsEquipment())
                {
                    UnequipAll(card); //Unequip previous cards, only 1 equip at a time

                    Player player = game_data.GetPlayer(card.player_id);
                    player.RemoveCardFromAllGroups(equipment);
                    player.cards_equip.Add(equipment);
                    card.equipped_uid = equipment.uid;
                    equipment.slot = card.slot;
                }
            }
        }

        public virtual void UnequipAll(Card card)
        {
            if (card != null && card.equipped_uid != null)
            {
                Player player = game_data.GetPlayer(card.player_id);
                Card equip = player.GetEquipCard(card.equipped_uid);
                if (equip != null)
                {
                    card.equipped_uid = null;
                    DiscardCard(equip);
                }
            }
        }

        //Change owner of a card
        public virtual void ChangeOwner(Card card, Player owner)
        {
            if (card.player_id != owner.player_id)
            {
                Player powner = game_data.GetPlayer(card.player_id);
                powner.RemoveCardFromAllGroups(card);
                powner.cards_all.Remove(card.uid);
                owner.cards_all[card.uid] = card;
                card.player_id = owner.player_id;
            }
        }

        //Heal a card
        public virtual void HealCard(Card target, int value)
        {
            if (target == null)
                return;

            if (target.HasStatus(StatusType.Invincibility))
                return;

            // Only apply and broadcast healing if the card actually has damage to recover
            int prev_damage = target.damage;
            target.damage -= value;
            target.damage = Mathf.Max(target.damage, 0);
            int actual_heal = prev_damage - target.damage;

            if (actual_heal > 0)
                onCardHealed?.Invoke(target, actual_heal);
        }

        //Generic damage that doesnt come from another card
        public virtual void DamageCard(Card target, int value)
        {
            if (target == null)
                return;

            if (target.HasStatus(StatusType.Invincibility))
                return; //Invincible

            if (target.HasStatus(StatusType.SpellImmunity))
                return; //Spell immunity

            target.damage += value;

            onCardDamaged?.Invoke(target, value);

            if (target.GetHP() <= 0)
                DiscardCard(target);
        }

        //Damage a card with attacker/caster
        public virtual void DamageCard(Card attacker, Card target, int value, bool spell_damage = false)
        {
            if (attacker == null || target == null)
                return;

            if (target.HasStatus(StatusType.Invincibility))
                return; //Invincible

            if (target.HasStatus(StatusType.SpellImmunity) && attacker.CardData.type != CardType.Character)
                return; //Spell immunity

            //Shell
            bool doublelife = target.HasStatus(StatusType.Shell);
            if (doublelife && value > 0)
            {
                target.RemoveStatus(StatusType.Shell);
                return;
            }

            //Armor
            if (!spell_damage && target.HasStatus(StatusType.Armor))
                value = Mathf.Max(value - target.GetStatusValue(StatusType.Armor), 0);

            //Damage
            int damage_max = Mathf.Min(value, target.GetHP());
            int extra = value - target.GetHP();
            target.damage += value;

            //Remove sleep on damage
            target.RemoveStatus(StatusType.Sleep);

            //Callback
            onCardDamaged?.Invoke(target, value);

            //Deathtouch
            if (value > 0 && attacker.HasStatus(StatusType.Deathtouch) && target.CardData.type == CardType.Character)
                KillCard(attacker, target);

            //Kill card if no hp
            if (target.GetHP() <= 0)
                KillCard(attacker, target);
        }

        //A card that kills another card
        public virtual void KillCard(Card attacker, Card target)
        {
            if (attacker == null || target == null)
                return;

            if (!game_data.IsOnBoard(target) && !game_data.IsEquipped(target))
                return; //Already killed

            if (target.HasStatus(StatusType.Invincibility))
                return; //Cant be killed

            Player pattacker = game_data.GetPlayer(attacker.player_id);
            if (attacker.player_id != target.player_id)
            {
                pattacker.kill_count++;

                // WildTactics: al derrotar una criatura rival, el atacante roba
                // 1 carta de evento de su propio mazo de eventos.
                if (pattacker.cards_event_deck.Count > 0)
                    resolve_queue.AddCallback(() => DrawEventCard(pattacker));
            }

            DiscardCard(target);

            TriggerCardAbilityType(AbilityTrigger.OnKill, attacker, target);
        }

        /// <summary>
        /// Mueve la carta del tope del mazo de eventos del jugador a su mano de eventos.
        /// </summary>
        protected virtual void DrawEventCard(Player player)
        {
            if (player == null || player.cards_event_deck.Count == 0)
                return;

            Card ecard = player.cards_event_deck[0];
            player.cards_event_deck.RemoveAt(0);
            player.cards_event_hand.Add(ecard);

            Debug.Log($"🎴 Jugador {player.player_id} roba carta de evento: {ecard.card_id}  " +
                      $"(quedan {player.cards_event_deck.Count} en mazo)");

            RefreshData();
        }

        //Send card into discard
        public virtual void DiscardCard(Card card)
        {
            if (card == null)
                return;

            if (game_data.IsInDiscard(card))
                return; //Already discarded

            CardData icard = card.CardData;
            Player player = game_data.GetPlayer(card.player_id);
            bool was_on_board = game_data.IsOnBoard(card) || game_data.IsEquipped(card);
            Slot freed_slot = card.slot; // capturar slot antes de eliminar la carta del tablero

            //Unequip card
            UnequipAll(card);

            //Remove card from board and add to discard
            player.RemoveCardFromAllGroups(card);
            player.cards_discard.Add(card);
            game_data.last_destroyed = card.uid;

            //Remove from bearer
            Card bearer = player.GetBearerCard(card);
            if (bearer != null)
                bearer.equipped_uid = null;

            if (was_on_board)
            {
                //Trigger on death abilities
                TriggerCardAbilityType(AbilityTrigger.OnDeath, card);
                TriggerOtherCardsAbilityType(AbilityTrigger.OnDeathOther, card);
                TriggerSecrets(AbilityTrigger.OnDeathOther, card);
                UpdateOngoingCards(); //Not UpdateOngoing() here to avoid recursive calls in UpdateOngoingKills

                // WildTactics: reemplazar la carta derrotada con una del mazo.
                // During a GOLPEAR first attack, defer the opponent's replacement draw so the new
                // card cannot be selected as the target for the second GOLPEAR attack this turn.
                if (is_golpear_first_hit)
                    pending_card_draws.Add((player.player_id, freed_slot));
                else
                    resolve_queue.AddCallback(() => DrawCardToBoard(player, freed_slot));
            }

            cards_to_clear.Add(card); //Will be Clear() in the next UpdateOngoing, so that simultaneous damage effects work
            onCardDiscarded?.Invoke(card);
        }

        // WildTactics: invocar la siguiente carta del mazo al slot que quedó libre
        public virtual void DrawCardToBoard(Player player, Slot slot)
        {
            if (game_data.state == GameState.GameEnded)
                return;
            if (player.cards_deck.Count == 0)
            {
                CheckForWinner();
                return;
            }

            Card card = player.cards_deck[0];
            player.cards_deck.RemoveAt(0);

            card.slot = slot;
            // Cartas de reemplazo: la IA siempre entra boca-arriba (igual que WildPlaceInitialCards).
            // Las cartas del humano también entran boca-arriba en sustituciones mid-game para que el jugador
            // sepa qué carta recibió antes de que la IA actúe en el siguiente turno.
            card.revealed = true;
            player.cards_board.Add(card);

            // Apply OnPlay statuses (e.g. Shell from SUMERGIR) so mid-game drawn cards get their
            // passive statuses just like cards placed at game start via OnPlay triggers
            foreach (AbilityData iability in card.GetAbilities())
            {
                if (iability != null && iability.trigger == AbilityTrigger.OnPlay)
                {
                    foreach (StatusData stat in iability.status)
                        card.AddStatus(stat, iability.value, iability.duration);
                }
            }

            UpdateOngoing();
            RefreshData();

            onCardSummoned?.Invoke(card, slot);
            resolve_queue.ResolveAll(0.3f);
        }

        public int RollRandomValue(int dice)
        {
            return RollRandomValue(1, dice + 1);
        }

        public virtual int RollRandomValue(int min, int max)
        {
            game_data.rolled_value = random.Next(min, max);
            onRollValue?.Invoke(game_data.rolled_value);
            resolve_queue.SetDelay(1f);
            return game_data.rolled_value;
        }

        //--- Abilities --

        public virtual void TriggerCardAbilityType(AbilityTrigger type, Card caster, Card triggerer = null)
        {
            foreach (AbilityData iability in caster.GetAbilities())
            {
                if (iability && iability.trigger == type)
                {
                    TriggerCardAbility(iability, caster, triggerer);
                }
            }

            Card equipped = game_data.GetEquipCard(caster.equipped_uid);
            if (equipped != null)
                TriggerCardAbilityType(type, equipped, triggerer);
        }

        public virtual void TriggerOtherCardsAbilityType(AbilityTrigger type, Card triggerer)
        {
            foreach (Player oplayer in game_data.players)
            {

                foreach (Card card in oplayer.cards_board)
                    TriggerCardAbilityType(type, card, triggerer);
            }
        }

        public virtual void TriggerPlayerCardsAbilityType(Player player, AbilityTrigger type)
        {
            foreach (Card card in player.cards_board)
            {
                // All cards trigger their abilities regardless of face-down state.
                // VOLAR heal applies to the local player's own face-down cards too.
                // Visual FX for face-down cards are suppressed client-side (OnCardHealedEvent).
                TriggerCardAbilityType(type, card, card);
            }
        }

        public virtual void TriggerCardAbility(AbilityData iability, Card caster)
        {
            TriggerCardAbility(iability, caster, caster);
        }

        public virtual void TriggerCardAbility(AbilityData iability, Card caster, Card triggerer)
        {
            Card trigger_card = triggerer != null ? triggerer : caster; //Triggerer is the caster if not set
            if (!caster.HasStatus(StatusType.Silenced) && iability.AreTriggerConditionsMet(game_data, caster, trigger_card))
            {
                // VOLAR heal: pausa la cola 1 segundo para que el efecto de curación sea visible.
                // Solo cuando la carta está dañada (la curación tiene efecto real).
                // La cola de la IA tiene skip_delay=true, así que no se ve afectada.
                bool isStartOfTurnSelfHeal = iability.trigger == AbilityTrigger.StartOfTurn
                                             && iability.target == AbilityTarget.Self
                                             && caster.damage > 0;
                if (isStartOfTurnSelfHeal)
                    resolve_queue.SetDelay(1f);

                resolve_queue.AddAbility(iability, caster, trigger_card, ResolveCardAbility);
            }
        }

        public virtual void TriggerCardAbility(AbilityData iability, Card caster, Player triggerer)
        {
            if (!caster.HasStatus(StatusType.Silenced) && iability.AreTriggerConditionsMet(game_data, caster, triggerer))
            {
                resolve_queue.AddAbility(iability, caster, caster, ResolveCardAbility);
            }
        }

        public virtual void TriggerAbilityDelayed(AbilityData iability, Card caster)
        {
            resolve_queue.AddAbility(iability, caster, caster, TriggerCardAbility);
        }

        public virtual void TriggerAbilityDelayed(AbilityData iability, Card caster, Card triggerer)
        {
            Card trigger_card = triggerer != null ? triggerer : caster; //Triggerer is the caster if not set
            resolve_queue.AddAbility(iability, caster, trigger_card, TriggerCardAbility);
        }

        //Resolve a card ability, may stop to ask for target
        protected virtual void ResolveCardAbility(AbilityData iability, Card caster, Card triggerer)
        {
            if (!caster.CanDoAbilities())
                return; //Silenced card cant cast

            //Debug.Log("Trigger Ability " + iability.id + " : " + caster.card_id);

            onAbilityStart?.Invoke(iability, caster);
            game_data.ability_triggerer = triggerer.uid;
            game_data.ability_played.Add(iability.id);

            bool is_selector = ResolveCardAbilitySelector(iability, caster);
            if (is_selector)
                return; //Wait for player to select

            ResolveCardAbilityPlayTarget(iability, caster);
            ResolveCardAbilityCards(iability, caster);
            ResolveCardAbilitySlots(iability, caster);
            ResolveCardAbilityCardData(iability, caster);
            ResolveCardAbilityNoTarget(iability, caster);
            AfterAbilityResolved(iability, caster);
        }

        protected virtual bool ResolveCardAbilitySelector(AbilityData iability, Card caster)
        {
            if (iability.target == AbilityTarget.SelectTarget)
            {
                //Wait for target
                GoToSelectTarget(iability, caster);
                return true;
            }
            else if (iability.target == AbilityTarget.CardSelector)
            {
                GoToSelectorCard(iability, caster);
                return true;
            }
            else if (iability.target == AbilityTarget.ChoiceSelector)
            {
                GoToSelectorChoice(iability, caster);
                return true;
            }
            return false;
        }

        protected virtual void ResolveCardAbilityPlayTarget(AbilityData iability, Card caster)
        {
            if (iability.target == AbilityTarget.PlayTarget)
            {
                Slot slot = caster.slot;
                Card slot_card = game_data.GetSlotCard(slot);

                if (slot_card != null)
                {
                    if (iability.CanTarget(game_data, caster, slot_card))
                    {
                        game_data.last_target = slot_card.uid;
                        ResolveEffectTarget(iability, caster, slot_card);
                    }
                }
                else
                {
                    if (iability.CanTarget(game_data, caster, slot))
                        ResolveEffectTarget(iability, caster, slot);
                }
            }
        }

        protected virtual void ResolveCardAbilityCards(AbilityData iability, Card caster)
        {
            //Get Cards Targets based on conditions
            List<Card> targets = iability.GetCardTargets(game_data, caster, card_array);

            //Resolve effects
            foreach (Card target in targets)
            {
                ResolveEffectTarget(iability, caster, target);
            }
        }

        protected virtual void ResolveCardAbilitySlots(AbilityData iability, Card caster)
        {
            //Get Slot Targets based on conditions
            List<Slot> targets = iability.GetSlotTargets(game_data, caster, slot_array);

            //Resolve effects
            foreach (Slot target in targets)
            {
                ResolveEffectTarget(iability, caster, target);
            }
        }

        protected virtual void ResolveCardAbilityCardData(AbilityData iability, Card caster)
        {
            //Get Cards Targets based on conditions
            List<CardData> targets = iability.GetCardDataTargets(game_data, caster, card_data_array);

            //Resolve effects
            foreach (CardData target in targets)
            {
                ResolveEffectTarget(iability, caster, target);
            }
        }

        protected virtual void ResolveCardAbilityNoTarget(AbilityData iability, Card caster)
        {
            if (iability.target == AbilityTarget.None)
                iability.DoEffects(this, caster);
        }

        protected virtual void ResolveEffectTarget(AbilityData iability, Card caster, Card target)
        {
            iability.DoEffects(this, caster, target);

            onAbilityTargetCard?.Invoke(iability, caster, target);
        }

        protected virtual void ResolveEffectTarget(AbilityData iability, Card caster, Slot target)
        {
            iability.DoEffects(this, caster, target);

            onAbilityTargetSlot?.Invoke(iability, caster, target);
        }

        protected virtual void ResolveEffectTarget(AbilityData iability, Card caster, CardData target)
        {
            iability.DoEffects(this, caster, target);
        }

        protected virtual void AfterAbilityResolved(AbilityData iability, Card caster)
        {
            Player player = game_data.GetPlayer(caster.player_id);


            //Recalculate and clear
            UpdateOngoing();
            CheckForWinner();

            //Chain ability
            if (iability.target != AbilityTarget.ChoiceSelector && game_data.state != GameState.GameEnded)
            {
                foreach (AbilityData chain_ability in iability.chain_abilities)
                {
                    if (chain_ability != null)
                    {
                        TriggerCardAbility(chain_ability, caster);
                    }
                }
            }

            onAbilityEnd?.Invoke(iability, caster);
            resolve_queue.ResolveAll(0.5f);
            RefreshData();
        }

        //This function is called often to update status/stats affected by ongoing abilities
        //It basically first reset the bonus to 0 (CleanOngoing) and then recalculate it to make sure it it still present
        //Only cards in hand and on board are updated in this way
        public virtual void UpdateOngoing()
        {
            Profiler.BeginSample("Update Ongoing");
            UpdateOngoingCards(); //Update status and stats
            UpdateOngoingKills(); //Kill cards with 0 HP
            Profiler.EndSample();
        }

        protected virtual void UpdateOngoingCards()
        {
            for (int p = 0; p < game_data.players.Length; p++)
            {
                Player player = game_data.players[p];
                player.ClearOngoing();

                for (int c = 0; c < player.cards_board.Count; c++)
                    player.cards_board[c].ClearOngoing();

                for (int c = 0; c < player.cards_equip.Count; c++)
                    player.cards_equip[c].ClearOngoing();

                for (int c = 0; c < player.cards_hand.Count; c++)
                    player.cards_hand[c].ClearOngoing();
            }

            for (int p = 0; p < game_data.players.Length; p++)
            {
                Player player = game_data.players[p];

                for (int c = 0; c < player.cards_board.Count; c++)
                {
                    Card card = player.cards_board[c];
                    UpdateOngoingAbilities(player, card);
                }

                for (int c = 0; c < player.cards_equip.Count; c++)
                {
                    Card card = player.cards_equip[c];
                    UpdateOngoingAbilities(player, card);
                }
            }

            //Stats bonus
            for (int p = 0; p < game_data.players.Length; p++)
            {
                Player player = game_data.players[p];
                for (int c = 0; c < player.cards_board.Count; c++)
                {
                    Card card = player.cards_board[c];

                    //Taunt effect
                    if (card.HasStatus(StatusType.Protection) && !card.HasStatus(StatusType.Stealth))
                    {
                        player.AddOngoingStatus(StatusType.Protected, 0);

                        for (int tc = 0; tc < player.cards_board.Count; tc++)
                        {
                            Card tcard = player.cards_board[tc];
                            if (!tcard.HasStatus(StatusType.Protection) && !tcard.HasStatus(StatusType.Protected))
                            {
                                tcard.AddOngoingStatus(StatusType.Protected, 0);
                            }
                        }
                    }

                    //Status bonus
                    foreach (CardStatus status in card.status)
                        AddOngoingStatusBonus(card, status);
                    foreach (CardStatus status in card.ongoing_status)
                        AddOngoingStatusBonus(card, status);
                }

                for (int c = 0; c < player.cards_hand.Count; c++)
                {
                    Card card = player.cards_hand[c];
                    //Status bonus
                    foreach (CardStatus status in card.status)
                        AddOngoingStatusBonus(card, status);
                    foreach (CardStatus status in card.ongoing_status)
                        AddOngoingStatusBonus(card, status);
                }
            }
        }

        protected virtual void UpdateOngoingKills()
        {
            //Kill stuff with 0 hp
            for (int p = 0; p < game_data.players.Length; p++)
            {
                Player player = game_data.players[p];
                for (int i = player.cards_board.Count - 1; i >= 0; i--)
                {
                    if (i < player.cards_board.Count)
                    {
                        Card card = player.cards_board[i];
                        if (card.GetHP() <= 0)
                            DiscardCard(card);
                    }
                }
                for (int i = player.cards_equip.Count - 1; i >= 0; i--)
                {
                    if (i < player.cards_equip.Count)
                    {
                        Card card = player.cards_equip[i];
                        if (card.GetHP() <= 0)
                            DiscardCard(card);
                        Card bearer = player.GetBearerCard(card);
                        if (bearer == null)
                            DiscardCard(card);
                    }
                }
            }

            //Clear cards
            for (int c = 0; c < cards_to_clear.Count; c++)
                cards_to_clear[c].Clear();
            cards_to_clear.Clear();
        }

        protected virtual void UpdateOngoingAbilities(Player player, Card card)
        {
            if (card == null || !card.CanDoAbilities())
                return;

            List<AbilityData> cabilities = card.GetAbilities();
            for (int a = 0; a < cabilities.Count; a++)
            {
                AbilityData ability = cabilities[a];
                if (ability != null && ability.trigger == AbilityTrigger.Ongoing && ability.AreTriggerConditionsMet(game_data, card))
                {
                    if (ability.target == AbilityTarget.Self)
                    {
                        if (ability.AreTargetConditionsMet(game_data, card, card))
                        {
                            ability.DoOngoingEffects(this, card, card);
                        }
                    }

                    if (ability.target == AbilityTarget.PlayerSelf)
                    {
                        if (ability.AreTargetConditionsMet(game_data, card, player))
                        {
                            ability.DoOngoingEffects(this, card, player);
                        }
                    }

                    if (ability.target == AbilityTarget.AllPlayers || ability.target == AbilityTarget.PlayerOpponent)
                    {
                        for (int tp = 0; tp < game_data.players.Length; tp++)
                        {
                            if (ability.target == AbilityTarget.AllPlayers || tp != player.player_id)
                            {
                                Player oplayer = game_data.players[tp];
                                if (ability.AreTargetConditionsMet(game_data, card, oplayer))
                                {
                                    ability.DoOngoingEffects(this, card, oplayer);
                                }
                            }
                        }
                    }

                    if (ability.target == AbilityTarget.EquippedCard)
                    {
                        if (card.CardData.IsEquipment())
                        {
                            //Get bearer of the equipment
                            Card target = player.GetBearerCard(card);
                            if (target != null && ability.AreTargetConditionsMet(game_data, card, target))
                            {
                                ability.DoOngoingEffects(this, card, target);
                            }
                        }
                        else if (card.equipped_uid != null)
                        {
                            //Get equipped card
                            Card target = game_data.GetCard(card.equipped_uid);
                            if (target != null && ability.AreTargetConditionsMet(game_data, card, target))
                            {
                                ability.DoOngoingEffects(this, card, target);
                            }
                        }
                    }

                    if (ability.target == AbilityTarget.AllCardsAllPiles || ability.target == AbilityTarget.AllCardsHand || ability.target == AbilityTarget.AllCardsBoard)
                    {
                        for (int tp = 0; tp < game_data.players.Length; tp++)
                        {
                            //Looping on all cards is very slow, since there are no ongoing effects that works out of board/hand we loop on those only
                            Player tplayer = game_data.players[tp];

                            //Hand Cards
                            if (ability.target == AbilityTarget.AllCardsAllPiles || ability.target == AbilityTarget.AllCardsHand)
                            {
                                for (int tc = 0; tc < tplayer.cards_hand.Count; tc++)
                                {
                                    Card tcard = tplayer.cards_hand[tc];
                                    if (ability.AreTargetConditionsMet(game_data, card, tcard))
                                    {
                                        ability.DoOngoingEffects(this, card, tcard);
                                    }
                                }
                            }

                            //Board Cards
                            if (ability.target == AbilityTarget.AllCardsAllPiles || ability.target == AbilityTarget.AllCardsBoard)
                            {
                                for (int tc = 0; tc < tplayer.cards_board.Count; tc++)
                                {
                                    Card tcard = tplayer.cards_board[tc];
                                    if (ability.AreTargetConditionsMet(game_data, card, tcard))
                                    {
                                        ability.DoOngoingEffects(this, card, tcard);
                                    }
                                }
                            }

                            //Equip Cards
                            if (ability.target == AbilityTarget.AllCardsAllPiles)
                            {
                                for (int tc = 0; tc < tplayer.cards_equip.Count; tc++)
                                {
                                    Card tcard = tplayer.cards_equip[tc];
                                    if (ability.AreTargetConditionsMet(game_data, card, tcard))
                                    {
                                        ability.DoOngoingEffects(this, card, tcard);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        protected virtual void AddOngoingStatusBonus(Card card, CardStatus status)
        {
            if (status.type == StatusType.AddAttack)
                card.attack_ongoing += status.value;
            if (status.type == StatusType.AddHP)
                card.hp_ongoing += status.value;
        }

        //---- Secrets ------------

        public virtual bool TriggerPlayerSecrets(Player player, AbilityTrigger secret_trigger)
        {
            for (int i = player.cards_secret.Count - 1; i >= 0; i--)
            {
                Card card = player.cards_secret[i];
                CardData icard = card.CardData;
                if (icard.type == CardType.Secret && !card.exhausted)
                {
                    if (card.AreAbilityConditionsMet(secret_trigger, game_data, card, card))
                    {
                        resolve_queue.AddSecret(secret_trigger, card, card, ResolveSecret);
                        resolve_queue.SetDelay(0.5f);
                        card.exhausted = true;

                        if (onSecretTrigger != null)
                            onSecretTrigger.Invoke(card, card);

                        return true; //Trigger only 1 secret per trigger
                    }
                }
            }
            return false;
        }

        public virtual bool TriggerSecrets(AbilityTrigger secret_trigger, Card trigger_card)
        {
            if (trigger_card != null && trigger_card.HasStatus(StatusType.SpellImmunity))
                return false; //Spell Immunity, triggerer is the one that trigger the trap, target is the one attacked, so usually the player who played the trap, so we dont check the target

            for (int p = 0; p < game_data.players.Length; p++)
            {
                if (p != game_data.current_player)
                {
                    Player other_player = game_data.players[p];
                    for (int i = other_player.cards_secret.Count - 1; i >= 0; i--)
                    {
                        Card card = other_player.cards_secret[i];
                        CardData icard = card.CardData;
                        if (icard.type == CardType.Secret && !card.exhausted)
                        {
                            Card trigger = trigger_card != null ? trigger_card : card;
                            if (card.AreAbilityConditionsMet(secret_trigger, game_data, card, trigger))
                            {
                                resolve_queue.AddSecret(secret_trigger, card, trigger, ResolveSecret);
                                resolve_queue.SetDelay(0.5f);
                                card.exhausted = true;

                                if (onSecretTrigger != null)
                                    onSecretTrigger.Invoke(card, trigger);

                                return true; //Trigger only 1 secret per trigger
                            }
                        }
                    }
                }
            }
            return false;
        }

        protected virtual void ResolveSecret(AbilityTrigger secret_trigger, Card secret_card, Card trigger)
        {
            CardData icard = secret_card.CardData;
            Player player = game_data.GetPlayer(secret_card.player_id);
            if (icard.type == CardType.Secret)
            {
                Player tplayer = game_data.GetPlayer(trigger.player_id);
                if (!is_ai_predict)
                    tplayer.AddHistory(GameAction.SecretTriggered, secret_card, trigger);

                TriggerCardAbilityType(secret_trigger, secret_card, trigger);
                DiscardCard(secret_card);

                if (onSecretResolve != null)
                    onSecretResolve.Invoke(secret_card, trigger);
            }
        }

        //---- Resolve Selector -----

        public virtual void SelectCard(Card target)
        {
            if (game_data.selector == SelectorType.None)
                return;

            Card caster = game_data.GetCard(game_data.selector_caster_uid);
            AbilityData ability = AbilityData.Get(game_data.selector_ability_id);

            if (caster == null || target == null || ability == null)
                return;

            if (game_data.selector == SelectorType.SelectTarget)
            {
                if (!ability.CanTarget(game_data, caster, target))
                    return; //Can't target that target

                Player player = game_data.GetPlayer(caster.player_id);
                if (!is_ai_predict)
                    player.AddHistory(GameAction.CastAbility, caster, ability, target);

                game_data.selector = SelectorType.None;
                game_data.last_target = target.uid;
                ResolveEffectTarget(ability, caster, target);
                AfterAbilityResolved(ability, caster);
                resolve_queue.ResolveAll();
            }

            if (game_data.selector == SelectorType.SelectorCard)
            {
                if (!ability.IsCardSelectionValid(game_data, caster, target, card_array))
                    return; //Supports conditions and filters

                game_data.selector = SelectorType.None;
                game_data.last_target = target.uid;
                ResolveEffectTarget(ability, caster, target);
                AfterAbilityResolved(ability, caster);
                resolve_queue.ResolveAll();
            }
        }

        public virtual void SelectSlot(Slot target)
        {
            if (game_data.selector == SelectorType.None)
                return;

            Card caster = game_data.GetCard(game_data.selector_caster_uid);
            AbilityData ability = AbilityData.Get(game_data.selector_ability_id);

            if (caster == null || ability == null || !target.IsValid())
                return;

            if (game_data.selector == SelectorType.SelectTarget)
            {
                if (!ability.CanTarget(game_data, caster, target))
                    return; //Conditions not met

                Player player = game_data.GetPlayer(caster.player_id);
                if (!is_ai_predict)
                    player.AddHistory(GameAction.CastAbility, caster, ability, target);

                game_data.selector = SelectorType.None;
                ResolveEffectTarget(ability, caster, target);
                AfterAbilityResolved(ability, caster);
                resolve_queue.ResolveAll();
            }
        }

        public virtual void SelectChoice(int choice)
        {
            if (game_data.selector == SelectorType.None)
                return;

            Card caster = game_data.GetCard(game_data.selector_caster_uid);
            AbilityData ability = AbilityData.Get(game_data.selector_ability_id);

            if (caster == null || ability == null || choice < 0)
                return;

            if (game_data.selector == SelectorType.SelectorChoice && ability.target == AbilityTarget.ChoiceSelector)
            {
                if (choice >= 0 && choice < ability.chain_abilities.Length)
                {
                    AbilityData achoice = ability.chain_abilities[choice];
                    if (achoice != null && game_data.CanSelectAbility(caster, achoice))
                    {
                        game_data.selector = SelectorType.None;
                        AfterAbilityResolved(ability, caster);
                        ResolveCardAbility(achoice, caster, caster);
                        resolve_queue.ResolveAll();
                    }
                }
            }
        }

        public virtual void CancelSelection()
        {
            if (game_data.selector != SelectorType.None)
            {
                //End selection
                game_data.selector = SelectorType.None;
                RefreshData();
            }
        }

        //-----Trigger Selector-----

        protected virtual void GoToSelectTarget(AbilityData iability, Card caster)
        {
            game_data.selector = SelectorType.SelectTarget;
            game_data.selector_player_id = caster.player_id;
            game_data.selector_ability_id = iability.id;
            game_data.selector_caster_uid = caster.uid;
            RefreshData();
        }

        protected virtual void GoToSelectorCard(AbilityData iability, Card caster)
        {
            game_data.selector = SelectorType.SelectorCard;
            game_data.selector_player_id = caster.player_id;
            game_data.selector_ability_id = iability.id;
            game_data.selector_caster_uid = caster.uid;
            RefreshData();
        }

        protected virtual void GoToSelectorChoice(AbilityData iability, Card caster)
        {
            game_data.selector = SelectorType.SelectorChoice;
            game_data.selector_player_id = caster.player_id;
            game_data.selector_ability_id = iability.id;
            game_data.selector_caster_uid = caster.uid;
            RefreshData();
        }

        protected virtual void GoToSelectorCost(Card caster)
        {
            game_data.selector = SelectorType.SelectorCost;
            game_data.selector_player_id = caster.player_id;
            game_data.selector_ability_id = "";
            game_data.selector_caster_uid = caster.uid;
            game_data.selected_value = 0;
            RefreshData();
        }

        //-------------

        public virtual void RefreshData()
        {
            onRefresh?.Invoke();
        }

        public virtual void ClearResolve()
        {
            resolve_queue.Clear();
        }

        public virtual bool IsResolving()
        {
            return resolve_queue.IsResolving();
        }

        public virtual bool IsGameStarted()
        {
            return game_data.HasStarted();
        }

        public virtual bool IsGameEnded()
        {
            return game_data.HasEnded();
        }

        public virtual Game GetGameData()
        {
            return game_data;
        }

        public System.Random GetRandom()
        {
            return random;
        }

        public Game GameData { get { return game_data; } }
        public ResolveQueue ResolveQueue { get { return resolve_queue; } }
    }
}