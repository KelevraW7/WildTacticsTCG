using UnityEngine;
using TcgEngine;
using TcgEngine.Client;
using TcgEngine.Gameplay;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    private GameLogic logic;
    private Game game;


    void Start()
    {
        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        yield return new WaitUntil(() => GameClient.Get() != null && GameClient.Get().IsReady());

        game = GameClient.Get().GetGameData();
        if (game == null)
        {
            Debug.LogError("❌ No se encontró GameData incluso después de esperar.");
            yield break;
        }

        logic = new GameLogic(game);

        AssignDecks(game);
        PlaceInitialCards(game);
        logic.StartGame();

        Debug.Log("✅ GameManager: partida iniciada correctamente");
    }

    private void AssignDecks(Game game)
    {
        List<CardData> pool = new List<CardData>(CardData.GetAll());
        System.Random rand = new System.Random();

        foreach (Player player in game.players)
        {
            List<CardData> copy = new List<CardData>(pool);
            List<CardData> selected = new List<CardData>();

            // 1 dorada
            CardData gold = copy.Find(c => c.rarity != null && c.rarity.id == "gold");
            if (gold != null)
            {
                selected.Add(gold);
                copy.Remove(gold);
            }

            // 10 comunes
            while (selected.Count < 11 && copy.Count > 0)
            {
                int index = rand.Next(copy.Count);
                CardData c = copy[index];
                if (!selected.Contains(c))
                {
                    selected.Add(c);
                    copy.Remove(c);
                }
            }

            foreach (CardData data in selected)
            {
                Card card = Card.Create(data, VariantData.GetDefault(), player);
                player.cards_deck.Add(card);
            }

            Debug.Log($"🃏 Jugador {player.player_id} recibió {player.cards_deck.Count} cartas.");
        }
    }

    private void PlaceInitialCards(Game game)
    {
        foreach (Player player in game.players)
        {
            for (int i = 0; i < 3; i++)
            {
                if (player.cards_deck.Count == 0)
                    break;

                Card card = player.cards_deck[0];
                player.cards_deck.RemoveAt(0);


                Slot slot = new Slot(i + 1, 1, player.player_id);
                card.slot = slot;
                card.revealed = player.player_id != GameClient.Get().GetPlayerID(); // el jugador ve sus cartas tapadas
                player.cards_board.Add(card);

                Debug.Log($"📍 Colocada carta {card.card_id} en slot {slot.x},{slot.y},{slot.p}");
            }
        }
    }
}
