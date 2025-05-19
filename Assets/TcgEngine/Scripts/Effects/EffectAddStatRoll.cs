using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// Effect that adds or removes basic card/player stats such as hp, attack, by the value of the dice roll
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/AddStatRoll", order = 10)]
    public class EffectAddStatRoll : EffectData
    {
        public EffectStatType type;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            Game data = logic.GetGameData();


        }

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            Game data = logic.GetGameData();

            if (type == EffectStatType.Attack)
                target.attack += data.rolled_value;
        }
    }
}