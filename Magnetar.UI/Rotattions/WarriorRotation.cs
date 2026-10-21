using Magnetar.UI.Common;
using Magnetar.UI.Models;
using static System.Windows.Forms.AxHost;

namespace Magnetar.UI.Rotattions;

public class WarriorRotation : IRotation
{
    public Ability[] Abilities { get; } = [
        new ("Victory Rush") { Conditions = [
            (state, ability) => state.PlayerHpp < 90
            ] },
        new ("Thunder Clap") { Conditions = [
            (state, ability) => state.InRangeCount > 2
            ] },

        new ("Revenge"),

        new ("Thunder Clap") { Conditions = [
            (state, ability) => state.InRangeCount < 3 && state.InRangeCount > 0,
            (state, ability) => state.TargetDebuffs.FirstOrDefault(a => a.Name == "Thunder Clap") == null,
            ] },

        new ("Sunder Armor") { Conditions = [
            (state, ability) => (state.TargetDebuffs.FirstOrDefault(a => a.Name == "Sunder Armor")?.Count ?? 0) < 5
            ] },

        new ("Rend") {
            Conditions = [
                (state, ability) => state.TargetDebuffs.FirstOrDefault(a => a.Name == "Rend") == null,
                (state, ability) => (state.TargetDebuffs.FirstOrDefault(a => a.Name == "Sunder Armor")?.Count ?? 0) == 5
                ]
        },

        new("Heroic Strike") {
            Conditions = [
                (state, ability) => !ability.Flags.HasAnyFlag(AbilityFlags.IsCurrent)
                    && state.InRangeCount == 1
                    && state.PlayerPower > 20
            ] },

        new("Cleave") {
            Conditions = [
                (state, ability) => !ability.Flags.HasAnyFlag(AbilityFlags.IsCurrent)
                    && state.InRangeCount > 1
                    && state.PlayerPower > 30
            ] },
        ];
}
