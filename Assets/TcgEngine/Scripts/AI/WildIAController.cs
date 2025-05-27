using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;
using TcgEngine.Client;
using TcgEngine.FX;

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
            yield return new WaitForSeconds(1f); // Delay inicial

            List<BoardCard> iaCards = BoardCard.GetAll().FindAll(c => !c.IsDead() && c.GetCard().player_id == IA_ID);
            List<BoardCard> playerCards = BoardCard.GetAll().FindAll(c => !c.IsDead() && c.GetCard().player_id == PLAYER_ID);

            BoardCard attacker = ElegirAtacante(iaCards);
            BoardCard target = attacker != null ? ElegirObjetivo(attacker, playerCards) : null;

            if (attacker != null && target != null)
            {
                Debug.Log($"🤖 IA ataca con {attacker.GetCard().card_id} a {target.GetCard().card_id}");

                // Glow visual y parpadeo anticipado
                PlayerControls.Get().SelectCard(attacker);
                attacker.SetSelectedVisual(true);
                yield return new WaitForSeconds(0.8f);
                attacker.SetSelectedVisual(false);

                // Ejecutar ataque real
                GameClient.Get().ApplyAttack(attacker.GetCard(), target.GetCard());
                yield return new WaitForSeconds(1.2f);

                // Quitar selección
                PlayerControls.Get().UnselectAll();
                yield return new WaitForSeconds(0.3f);
            }

            yield return new WaitForSeconds(0.5f);

            // Finaliza turno
            GameClient.Get().EndTurn();
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

        private BoardCard ElegirAtacante(List<BoardCard> disponibles)
        {
            if (disponibles.Count == 0)
                return null;

            // Por ahora, elige una criatura aleatoria
            return disponibles[Random.Range(0, disponibles.Count)];
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
