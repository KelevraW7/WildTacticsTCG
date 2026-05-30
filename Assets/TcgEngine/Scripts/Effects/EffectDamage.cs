using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// Effect that damages a card (lose hp)
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Damage", order = 10)]
    public class EffectDamage : EffectData
    {
        public TraitData bonus_damage;

        [Tooltip("When true, damage bypasses Shell entirely (no break, no absorption). Used for ability damage like INTOXICAR.")]
        public bool bypass_shell = false;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            int damage = GetDamage(logic.GameData, caster, ability.value);
            if (bypass_shell)
                logic.DamageCard(target, damage);   // no Shell check — ability damage pierces Shell
            else
                logic.DamageCard(caster, target, damage, true);
        }

        private int GetDamage(Game data, Card caster, int value)
        {
            // Calcula el daño sumando el valor base y el bonus de daño del caster
            int damage = value + caster.GetTraitValue(bonus_damage);
            return damage;
        }
    }
}
