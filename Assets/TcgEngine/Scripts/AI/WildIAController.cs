using System.Collections;
using UnityEngine;
using TcgEngine.Client;

namespace TcgEngine.AI
{
    /// <summary>
    /// Visual-only component for AI feedback.
    /// AIPlayerMM (server-side) handles all game decisions and attacks.
    /// This script subscribes to onAttackStart to show a glow on the AI attacker card.
    /// </summary>
    public class WildIAController : MonoBehaviour
    {
        public static WildIAController instance;

        private const int IA_ID = 1;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            GameClient client = GameClient.Get();
            if (client != null)
                client.onAttackStart += OnAttackStart;
        }

        private void OnDestroy()
        {
            GameClient client = GameClient.Get();
            if (client != null)
                client.onAttackStart -= OnAttackStart;
        }

        private void OnAttackStart(Card attacker, Card target)
        {
            if (attacker == null || attacker.player_id != IA_ID)
                return;

            BoardCard boardCard = BoardCard.Get(attacker.uid);
            if (boardCard == null)
                return;

            boardCard.SetSelectedVisual(true);
            StartCoroutine(ClearGlowAfterDelay(boardCard, 0.6f));
        }

        private IEnumerator ClearGlowAfterDelay(BoardCard card, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (card != null)
                card.SetSelectedVisual(false);
        }
    }
}
