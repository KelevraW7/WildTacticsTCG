using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    public enum ConditionStatType
    {
        None = 0,
        Attack = 10,
    }

    /// <summary>
    /// Compares basic card or stats such as attack
    /// </summary>

    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/Stat", order = 10)]
    public class ConditionStat : ConditionData
    {
        [Header("Card stat is")]
        public ConditionStatType type;
        public ConditionOperatorInt oper;
        public int value;

        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            if (type == ConditionStatType.Attack)
            {
                return CompareInt(target.GetAttack(), oper, value);
            }


            return false;
        }

        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Player target)
        {

            return false;
        }
    }
}