﻿using Crpg.Module.Helpers;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Crpg.Module.Common.Models;

/// <summary>
/// Used to adjust dmg calculations.
/// </summary>
internal class CrpgAgentApplyDamageModel : MultiplayerAgentApplyDamageModel
{
    private readonly CrpgConstants _constants;

    public CrpgAgentApplyDamageModel(CrpgConstants constants)
    {
        _constants = constants;
    }

    public override float CalculateDamage(
        in AttackInformation attackInformation,
        in AttackCollisionData collisionData,
        in MissionWeapon weapon,
        float baseDamage)
    {
        float finalDamage = base.CalculateDamage(attackInformation, collisionData, weapon, baseDamage);
        if (weapon.IsEmpty)
        {
            // Increase fist damage with strength.
            int strengthSkill = GetSkillValue(attackInformation.AttackerAgentOrigin, CrpgSkills.Strength);
            return finalDamage * (1 + 0.03f * strengthSkill);
        }

        // CalculateShieldDamage only has dmg as parameter. Therefore it cannot be used to get any Skill values.
        if (collisionData.AttackBlockedWithShield && finalDamage > 0)
        {
            int shieldSkill = GetSkillValue(attackInformation.VictimAgentOrigin, CrpgSkills.Shield);
            finalDamage /= MathHelper.RecursivePolynomialFunctionOfDegree2(shieldSkill, _constants.DurabilityFactorForShieldRecursiveCoefs);
            if (weapon.CurrentUsageItem.WeaponFlags.HasAnyFlag(WeaponFlags.BonusAgainstShield))
            {
                // this bonus is on top of the native x2 in MissionCombatMechanicsHelper
                // so the final bonus is 3.5. We do this instead of nerfing the impact of shield skill so shield can stay virtually unbreakable against sword.
                // it is the same logic as arrows not dealing a lot of damage to horse but spears dealing extra damage to horses
                // As we want archer to fear cavs and cavs to fear spears, we want swords to fear shielders and shielders to fear axes.
                finalDamage *= 1.75f;
            }
        }

        // We want to decrease survivability of horses against melee weapon and especially against spears and pikes.
        // By doing that we ensure that cavalry stays an archer predator while punishing cav errors like running into a wall or an obstacle
        if (!attackInformation.IsVictimAgentHuman
            && !attackInformation.DoesAttackerHaveMountAgent
            && !weapon.CurrentUsageItem.IsConsumable
            && weapon.CurrentUsageItem.IsMeleeWeapon
            && !weapon.IsAnyConsumable())
        {
            if (
                collisionData.StrikeType == (int)StrikeType.Thrust
                && collisionData.DamageType == (int)DamageTypes.Pierce
                && weapon.CurrentUsageItem.IsPolearm)
            {
                finalDamage *= 1.85f;
            }
            else
            {
                finalDamage *= 1.4f;
            }
        }

        // For bashes (with and without shield) - Not for allies cause teamdmg might reduce the "finalDamage" below zero. That will break teamhits with bashes.
        else if (collisionData.IsAlternativeAttack && !attackInformation.IsFriendlyFire)
        {
            finalDamage = 1f;
        }

        if (attackInformation.DoesAttackerHaveMountAgent && attackInformation.IsAttackerAgentDoingPassiveAttack)
        {
            finalDamage *= 0.2f; // Decrease damage from couched lance.
        }

        return finalDamage;
    }

    public override void CalculateCollisionStunMultipliers(
        Agent attackerAgent,
        Agent defenderAgent,
        bool isAlternativeAttack,
        CombatCollisionResult collisionResult,
        WeaponComponentData attackerWeapon,
        WeaponComponentData defenderWeapon,
        out float attackerStunMultiplier,
        out float defenderStunMultiplier)
    {
        attackerStunMultiplier = 1f;
        if (collisionResult == CombatCollisionResult.Blocked && defenderAgent.WieldedOffhandWeapon.IsShield())
        {
            int shieldSkill = 0;
            if (defenderAgent.Origin is CrpgBattleAgentOrigin crpgOrigin)
            {
                shieldSkill = crpgOrigin.Skills.GetPropertyValue(CrpgSkills.Shield);
            }

            defenderStunMultiplier = 1 / MathHelper.RecursivePolynomialFunctionOfDegree2(shieldSkill, _constants.ShieldDefendStunMultiplierForSkillRecursiveCoefs);

            return;
        }

        defenderStunMultiplier = 1f;
    }

    private int GetSkillValue(IAgentOriginBase agentOrigin, SkillObject skill)
    {
        if (agentOrigin is CrpgBattleAgentOrigin crpgOrigin)
        {
            return crpgOrigin.Skills.GetPropertyValue(skill);
        }

        return 0;
    }
}
