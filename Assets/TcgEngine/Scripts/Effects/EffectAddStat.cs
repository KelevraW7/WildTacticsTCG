using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// Effect that adds or removes basic card/player stats such as hp, attack
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/AddStat", order = 10)]
    public class EffectAddStat : EffectData
    {
        public EffectStatType type;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            if (type == EffectStatType.HP)
            {
                target.hp += ability.value;
                target.hp_max += ability.value;
            }

        }

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            if (type == EffectStatType.Attack)
                target.attack += ability.value;
            if (type == EffectStatType.HP)
                target.hp += ability.value;
        }

        public override void DoOngoingEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            if (type == EffectStatType.Attack)
                target.attack_ongoing += ability.value;
            if (type == EffectStatType.HP)
                target.hp_ongoing += ability.value;
        }

    }

    public enum EffectStatType
    {
        None = 0,
        Attack = 10,
        HP = 20,
    }
}