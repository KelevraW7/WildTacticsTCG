using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine.AI
{
    /// <summary>
    /// IA de dificultad Intermedia.
    ///
    /// Comportamiento:
    ///   · Siempre prefiere ataques con ventaja de tipo (fuego→planta, agua→fuego, planta→agua).
    ///   · Si hay varias opciones con ventaja, elige una al azar (no razona cuál es óptima).
    ///   · Si no hay ventaja disponible, ataca al azar (igual que Fácil).
    ///   · No tiene en cuenta HP restante, prioridad de objetivos ni gestión de cartas doradas.
    ///   · Cadencia idéntica a AIPlayerRandom para mantener coherencia visual.
    /// </summary>
    public class AIPlayerMedium : AIPlayer
    {
        private bool is_playing   = false;
        private bool is_selecting = false;

        private System.Random rand = new System.Random();

        public AIPlayerMedium(GameLogic gameplay, int id, int level)
        {
            this.gameplay = gameplay;
            player_id     = id;
            ai_level      = level;
        }

        public override void Update()
        {
            if (!CanPlay()) return;

            Game game_data = gameplay.GetGameData();
            Player player  = game_data.GetPlayer(player_id);

            if (game_data.IsPlayerTurn(player) && !gameplay.IsResolving())
            {
                if (!is_playing && game_data.selector == SelectorType.None
                    && game_data.current_player == player_id)
                {
                    is_playing = true;
                    TimeTool.StartCoroutine(AiTurn());
                }

                if (!is_selecting && game_data.selector != SelectorType.None
                    && game_data.selector_player_id == player_id)
                {
                    if (game_data.selector == SelectorType.SelectTarget)
                    {
                        is_selecting = true;
                        TimeTool.StartCoroutine(AiSelectTarget());
                    }
                    if (game_data.selector == SelectorType.SelectorCard)
                    {
                        is_selecting = true;
                        TimeTool.StartCoroutine(AiSelectCard());
                    }
                    if (game_data.selector == SelectorType.SelectorChoice)
                    {
                        is_selecting = true;
                        TimeTool.StartCoroutine(AiSelectChoice());
                    }
                }
            }
        }

        private IEnumerator AiTurn()
        {
            yield return new WaitForSeconds(1f);

            // Ataque: prioriza ventaja de tipo, pero solo intenta una vez
            AttackWithTypeAdvantage();

            yield return new WaitForSeconds(0.5f);

            EndTurn();
            is_playing = false;
        }

        // ── Ataque con ventaja de tipo ─────────────────────────────────────────────

        private void AttackWithTypeAdvantage()
        {
            if (!CanPlay()) return;

            Game game_data    = gameplay.GetGameData();
            Player player     = game_data.GetPlayer(player_id);
            Player opponent   = game_data.GetOpponentPlayer(player_id);

            if (!game_data.IsPlayerActionTurn(player)) return;
            if (player.cards_board.Count == 0) return;

            // Recopilar todos los ataques legales contra criaturas enemigas
            var all_attacks = new List<(Card attacker, Card target)>();
            var advantaged  = new List<(Card attacker, Card target)>();

            foreach (Card attacker in player.cards_board)
            {
                foreach (Card target in opponent.cards_board)
                {
                    if (game_data.CanAttackTarget(attacker, target))
                    {
                        all_attacks.Add((attacker, target));
                        if (HasTypeAdvantage(attacker, target))
                            advantaged.Add((attacker, target));
                    }
                }
            }

            if (all_attacks.Count == 0) return;

            // Elegir entre los ataques con ventaja si los hay; si no, al azar entre todos
            var pool   = advantaged.Count > 0 ? advantaged : all_attacks;
            var chosen = pool[rand.Next(pool.Count)];
            gameplay.AttackTarget(chosen.attacker, chosen.target);
        }

        // ── Ventaja de tipo ────────────────────────────────────────────────────────
        // Triángulo: fuego > planta, agua > fuego, planta > agua

        private bool HasTypeAdvantage(Card attacker, Card target)
        {
            if (attacker?.CardData?.team == null || target?.CardData?.team == null)
                return false;

            string atk = attacker.CardData.team.id.ToLower();
            string def = target.CardData.team.id.ToLower();

            return (atk == "fire"  && def == "plant") ||
                   (atk == "water" && def == "fire")  ||
                   (atk == "plant" && def == "water");
        }

        // ── Selección de objetivos (igual que Random) ──────────────────────────────

        private IEnumerator AiSelectTarget()
        {
            yield return new WaitForSeconds(0.5f);
            SelectTarget();
            yield return new WaitForSeconds(0.5f);
            CancelSelect();
            is_selecting = false;
        }

        private IEnumerator AiSelectCard()
        {
            yield return new WaitForSeconds(0.5f);
            SelectCard();
            yield return new WaitForSeconds(0.5f);
            CancelSelect();
            is_selecting = false;
        }

        private IEnumerator AiSelectChoice()
        {
            yield return new WaitForSeconds(0.5f);
            SelectChoice();
            yield return new WaitForSeconds(0.5f);
            CancelSelect();
            is_selecting = false;
        }

        // ── Helpers (copiados del patrón AIPlayerRandom) ──────────────────────────

        private void SelectTarget()
        {
            if (!CanPlay()) return;
            Game game_data = gameplay.GetGameData();
            if (game_data.selector == SelectorType.None) return;

            int target_player = player_id;
            AbilityData ability = AbilityData.Get(game_data.selector_ability_id);
            if (ability != null && ability.target == AbilityTarget.SelectTarget)
                target_player = player_id == 0 ? 1 : 0;

            Player tplayer = game_data.GetPlayer(target_player);
            if (tplayer.cards_board.Count > 0)
            {
                Card random = tplayer.GetRandomCard(tplayer.cards_board, rand);
                if (random != null)
                    gameplay.SelectCard(random);
            }
        }

        private void SelectCard()
        {
            if (!CanPlay()) return;
            Game game_data = gameplay.GetGameData();
            Player player  = game_data.GetPlayer(player_id);
            AbilityData ability = AbilityData.Get(game_data.selector_ability_id);
            Card caster = game_data.GetCard(game_data.selector_caster_uid);
            if (ability != null && caster != null)
            {
                List<Card> list = ability.GetCardTargets(game_data, caster);
                if (list.Count > 0)
                    gameplay.SelectCard(list[rand.Next(list.Count)]);
            }
        }

        private void SelectChoice()
        {
            if (!CanPlay()) return;
            gameplay.SelectChoice(0);
        }

        private void CancelSelect()
        {
            if (!CanPlay()) return;
            gameplay.CancelSelection();
        }

        private void EndTurn()
        {
            if (!CanPlay()) return;
            Game game_data = gameplay.GetGameData();
            Player player  = game_data.GetPlayer(player_id);
            if (game_data.IsPlayerActionTurn(player))
                gameplay.EndTurn();
        }
    }
}
