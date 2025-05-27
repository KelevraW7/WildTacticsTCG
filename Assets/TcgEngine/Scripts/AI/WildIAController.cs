using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;
using TcgEngine.Client;

namespace TcgEngine.AI
{
    public class WildIAController : MonoBehaviour
    {
        public static WildIAController instance;

        private int IA_ID = 1; // ID de la IA (suponiendo que 0 es el jugador humano)
        private int PLAYER_ID = 0;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject); // Esto mantiene el objeto activo entre escenas
            }
            else
            {
                Destroy(gameObject); // Si ya existe uno, destruye el duplicado
            }
        }

        public void PlayTurn()
        {
            StartCoroutine(PlayTurnCoroutine());
        }

        private IEnumerator PlayTurnCoroutine()
        {
            yield return new WaitForSeconds(1f); // Delay para que el jugador vea las acciones

            List<BoardCard> iaCards = BoardCard.GetAll().FindAll(c => !c.IsDead() && c.GetCard().player_id == IA_ID);
            List<BoardCard> playerCards = BoardCard.GetAll().FindAll(c => !c.IsDead() && c.GetCard().player_id == PLAYER_ID);

            foreach (BoardCard attacker in iaCards)
            {
                BoardCard target = ElegirObjetivo(attacker, playerCards);

                if (target != null)
                {
                    Debug.Log($"🤖 IA ataca con {attacker.GetCard().card_id} a {target.GetCard().card_id}");

                    // Llamamos a AttackTarget usando las cartas reales
                    GameManager.instance.GameData.AttackTarget(attacker.GetCard(), target.GetCard());

                    yield return new WaitForSeconds(1.2f); // Delay entre ataques
                }
            }

            yield return new WaitForSeconds(0.5f);

            // Finalizar turno de IA
            GameManager.instance.GameData.EndTurn();
        }

        private BoardCard ElegirObjetivo(BoardCard atacante, List<BoardCard> posibles)
        {
            if (posibles.Count == 0)
                return null;

            string tipoAtacante = atacante.GetCard().CardData.team?.id;
            if (string.IsNullOrEmpty(tipoAtacante))
                return posibles[Random.Range(0, posibles.Count)];  // Fallback si no tiene team

            // 1. Atacar criatura dorada si hay
            foreach (BoardCard objetivo in posibles)
            {
                string tipoObjetivo = objetivo.GetCard().CardData.team?.id;
                if (tipoObjetivo == "gold")
                    return objetivo;
            }

            // 2. Buscar objetivo al que más daño se le haría
            BoardCard mejorObjetivo = null;
            int mayorDaño = -1;

            foreach (BoardCard objetivo in posibles)
            {
                int daño = CalcularDañoEsperado(atacante.GetCard(), objetivo.GetCard());
                if (daño > mayorDaño)
                {
                    mayorDaño = daño;
                    mejorObjetivo = objetivo;
                }
            }

            if (mejorObjetivo != null)
                return mejorObjetivo;

            // 3. Si no hay ventaja, devolver uno al azar
            return posibles[Random.Range(0, posibles.Count)];
        }

        private bool TieneVentajaDeTipo(string atacante, string objetivo)
        {
            return (atacante == "Fire" && objetivo == "Plant") ||
                   (atacante == "Plant" && objetivo == "Water") ||
                   (atacante == "Water" && objetivo == "Fire");
        }

        private int CalcularDañoEsperado(Card atacante, Card objetivo)
        {
            int base_dano = atacante.GetAttack();
            string tipo_atacante = atacante.CardData.team?.id;
            string tipo_objetivo = objetivo.CardData.team?.id;

            if (tipo_atacante == null || tipo_objetivo == null)
                return base_dano;

            if (TieneVentajaDeTipo(tipo_atacante, tipo_objetivo))
                return base_dano + 1;

            if (TieneVentajaDeTipo(tipo_objetivo, tipo_atacante))
                return Mathf.Max(base_dano - 1, 0); // Evitar daño negativo

            return base_dano;
        }
    }
}
