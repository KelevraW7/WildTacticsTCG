using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using System.Linq;

namespace TcgEngine
{
    public class DeckManager : MonoBehaviour
    {
        [System.Serializable]
        public class CreatureData
        {
            public int id;
            public string nombre;
            public string tipo;
            public int vida;
            public int daño;
            public string habilidad;
            public string imagen;
        }

        public List<CreatureData> allCreatures = new List<CreatureData>();
        public List<CreatureData> mainDeck = new List<CreatureData>();
        public List<CreatureData> player1Deck = new List<CreatureData>();
        public List<CreatureData> player2Deck = new List<CreatureData>();

        void Start()
        {
            LoadCreaturesFromJson();
            InitializeMainDeck();
            ShuffleAndDistributeDecks();
        }

        void LoadCreaturesFromJson()
        {
            TextAsset jsonData = Resources.Load<TextAsset>("criaturas_wildtactics");
            allCreatures = JsonConvert.DeserializeObject<List<CreatureData>>(jsonData.text);
            Debug.Log($"Se han cargado {allCreatures.Count} criaturas.");
        }

        void InitializeMainDeck()
        {
            mainDeck = allCreatures.Where(c => c.tipo != "Dorada").ToList();
            var doradas = allCreatures.Where(c => c.tipo == "Dorada").ToList();
            mainDeck.AddRange(doradas);

            Debug.Log($"Mazo principal inicializado con {mainDeck.Count} cartas.");
        }

        void ShuffleAndDistributeDecks()
        {
            System.Random rng = new System.Random();
            mainDeck = mainDeck.OrderBy(a => rng.Next()).ToList();

            // Distribución de cartas para el Jugador 1
            player1Deck = mainDeck.Take(10).ToList();
            var doradaP1 = mainDeck.FirstOrDefault(c => c.tipo == "Dorada");
            if (doradaP1 != null) mainDeck.Remove(doradaP1);

            // Insertar la dorada en una posición aleatoria
            int randomPositionP1 = rng.Next(0, 11);
            player1Deck.Insert(randomPositionP1, doradaP1);

            // Eliminar del mazo principal
            mainDeck.RemoveAll(c => player1Deck.Contains(c));

            // Distribución de cartas para el Jugador 2
            player2Deck = mainDeck.Take(10).ToList();
            var doradaP2 = mainDeck.FirstOrDefault(c => c.tipo == "Dorada");
            if (doradaP2 != null) mainDeck.Remove(doradaP2);

            // Insertar la dorada en una posición aleatoria
            int randomPositionP2 = rng.Next(0, 11);
            player2Deck.Insert(randomPositionP2, doradaP2);

            // Eliminar del mazo principal
            mainDeck.RemoveAll(c => player2Deck.Contains(c));

            Debug.Log($"Jugador 1 tiene {player1Deck.Count} cartas. Jugador 2 tiene {player2Deck.Count} cartas.");
        }

        public CreatureData DrawCardForPlayer(int playerId)
        {
            if (playerId == 1 && player1Deck.Count > 0)
            {
                var card = player1Deck[0];
                player1Deck.RemoveAt(0);
                return card;
            }
            if (playerId == 2 && player2Deck.Count > 0)
            {
                var card = player2Deck[0];
                player2Deck.RemoveAt(0);
                return card;
            }
            return null;
        }
    }
}
