using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.UI;

namespace TcgEngine.Client
{
    /// <summary>
    /// Muestra la mano de cartas de evento del jugador: una zona horizontal face-up
    /// donde cada carta puede ser jugada con un clic (antes de atacar en el propio turno).
    ///
    /// Setup en Unity:
    ///   1. Crear un GameObject con RectTransform en la escena de juego (p. ej. anclado
    ///      en la parte inferior de la pantalla).
    ///   2. Asignar card_prefab → el mismo prefab de HandCard que usa HandCardArea
    ///      (o uno específico para eventos).
    ///   3. Asignar card_area → el RectTransform que contiene las cartas.
    ///   4. Activar is_opponent = false para el jugador humano.
    /// </summary>

    public class BoardEventHand : MonoBehaviour
    {
        [Header("Configuración")]
        public bool is_opponent = false;          // false = mano del jugador local
        public GameObject card_prefab;             // Prefab con EventHandCard (o HandCard)
        public RectTransform card_area;            // Contenedor de las cartas
        public float card_spacing = 120f;          // Separación horizontal entre cartas

        [Header("Elementos visuales opcionales")]
        public GameObject empty_label;             // Objeto a mostrar cuando no hay cartas de evento
        public Text event_count_text;              // Texto con "N cartas de evento restantes en el mazo"

        // Lista de instancias de tarjetas actualmente mostradas
        private List<EventHandCard> displayed_cards = new List<EventHandCard>();

        private static BoardEventHand _instance_player;
        private static BoardEventHand _instance_opponent;

        void Awake()
        {
            if (is_opponent) _instance_opponent = this;
            else             _instance_player   = this;
        }

        void Update()
        {
            if (!GameClient.Get().IsReady())
                return;

            Game gdata = GameClient.Get().GetGameData();
            int local_id = GameClient.Get().GetPlayerID();
            int target_id = is_opponent
                ? (1 - local_id)  // el rival
                : local_id;

            Player player = gdata.GetPlayer(target_id);
            if (player == null)
                return;

            List<Card> event_hand = player.cards_event_hand;

            // ── Añadir cartas nuevas ──────────────────────────────────────────────────
            foreach (Card card in event_hand)
            {
                if (!HasDisplayed(card.uid))
                    SpawnCard(card);
            }

            // ── Eliminar cartas que ya no están en la mano de eventos ─────────────────
            for (int i = displayed_cards.Count - 1; i >= 0; i--)
            {
                EventHandCard ec = displayed_cards[i];
                if (ec == null || player.GetEventHandCard(ec.CardUID) == null)
                {
                    displayed_cards.RemoveAt(i);
                    if (ec != null)
                        Destroy(ec.gameObject);
                }
            }

            // ── Reposicionar horizontalmente ──────────────────────────────────────────
            // Notificamos a cada EventHandCard su posición base para que ella misma
            // anime el hover-lift sin que nosotros pisemos su posición cada frame.
            float total_width = (displayed_cards.Count - 1) * card_spacing;
            float start_x = -total_width / 2f;
            for (int i = 0; i < displayed_cards.Count; i++)
            {
                if (displayed_cards[i] != null)
                    displayed_cards[i].SetBasePosition(new Vector2(start_x + i * card_spacing, 0f));
            }

            // ── Label vacío ───────────────────────────────────────────────────────────
            if (empty_label != null)
                empty_label.SetActive(event_hand.Count == 0 && !is_opponent);

            // ── Contador de mazo de eventos ───────────────────────────────────────────
            if (event_count_text != null)
                event_count_text.text = player.cards_event_deck.Count.ToString();
        }

        private void SpawnCard(Card card)
        {
            if (card_prefab == null || card_area == null)
                return;

            GameObject go = Instantiate(card_prefab, card_area);
            EventHandCard ec = go.GetComponent<EventHandCard>();
            if (ec == null)
                ec = go.AddComponent<EventHandCard>();

            ec.Init(card.uid, is_opponent);
            displayed_cards.Add(ec);
        }

        private bool HasDisplayed(string uid)
        {
            foreach (EventHandCard ec in displayed_cards)
            {
                if (ec != null && ec.CardUID == uid)
                    return true;
            }
            return false;
        }

        public static BoardEventHand GetPlayer()   => _instance_player;
        public static BoardEventHand GetOpponent() => _instance_opponent;
    }
}
