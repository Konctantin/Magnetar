using Magnetar.UI.Common;
using Magnetar.UI.Models;
using Magnetar.UI.Rotattions;

namespace Magnetar.UI.Services;

public class RotationService(
    ForegroundWindowService foregroundService,
    AuraRepoService auras,
    StateService state,
    ClickerService clicker)
{
    private readonly Dictionary<WowClass, IRotation> Rotations = new() {
        [WowClass.Warrior] = new WarriorRotation(),
        [WowClass.Paladin] = new PaladinRotation(),
        [WowClass.Hunter] = new HunterRotattion(),
        [WowClass.Rogue] = new HunterRotattion(),
        [WowClass.Priest] = new HunterRotattion(),
        [WowClass.Shaman] = new HunterRotattion(),
        [WowClass.Mage] = new HunterRotattion(),
        [WowClass.Warlock] = new HunterRotattion(),
        [WowClass.Druid] = new HunterRotattion(),
    };

    public void Process()
    {
        if (!state.IsValidArea)
            return;

        if (!state.IsInWorld)
            return;

        if (!state.IsInCombat)
            return;

        if (!Rotations.TryGetValue(state.PlayerClass, out var rotation))
            return;

        foreach (var ab in rotation.Abilities)
        {
            var abil = state.PlayerAbilities.FirstOrDefault(a => a.Name == ab.Name);
            if (abil == null)
                continue;

            if (abil.Flags.HasAnyFlag(AbilityFlags.IsActive))
                continue;

            if (!abil.Flags.HasAnyFlag(AbilityFlags.IsUsable))
                continue;

            if (abil.Flags.HasAnyFlag(AbilityFlags.HasRangeReq)
                && !abil.Flags.HasAnyFlag(AbilityFlags.IsInRangeTraget))
                continue;

            bool good = true;
            foreach (var cond in ab.Conditions)
            {
                if (!cond(state, abil))
                {
                    good = false;
                    break;
                }
            }

            if (good)
            {
                clicker.Click(foregroundService.Handle, abil.HotKey);
                break;
            }
        }
    }
}
