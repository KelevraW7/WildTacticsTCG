using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// Compares basic player stats such as attack/hp
    /// </summary>

    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/PlayerStat", order = 10)]
    public class ConditionPlayerStat : ConditionData
    {
        [Header("Card stat is")]
        public ConditionStatType type;
        public ConditionOperatorInt oper;
        public int value;

        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            Player ptarget = data.GetPlayer(target.player_id);
            return IsTargetConditionMet(data, ability, caster, ptarget);
        }

        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Player target)
        {
            return false;
        }

    }
}