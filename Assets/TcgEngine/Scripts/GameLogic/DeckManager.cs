
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

            // Asignación para cada jugador
            player1Deck = mainDeck.Take(10).ToList();
            player1Deck.Add(mainDeck.FirstOrDefault(c => c.tipo == "Dorada"));

            player2Deck = mainDeck.Skip(11).Take(10).ToList();
            player2Deck.Add(mainDeck.Skip(11).FirstOrDefault(c => c.tipo == "Dorada"));

            // Eliminar del mazo principal
            mainDeck.RemoveAll(c => player1Deck.Contains(c) || player2Deck.Contains(c));

            Debug.Log($"Cartas asignadas: Jugador 1: {player1Deck.Count} | Jugador 2: {player2Deck.Count}");
        }
    }
}
