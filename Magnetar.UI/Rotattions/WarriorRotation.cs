using Magnetar.UI.Common;
using Magnetar.UI.Models;
using static System.Windows.Forms.AxHost;

namespace Magnetar.UI.Rotattions;

public class WarriorRotation : IRotation
{
    public Ability[] Abilities { get; } = [
        new ("Shield Bash") { Conditions = [
            (state, ability) => state.IsKick && state.TargetCasting > 0 && state.TargetCasting < 300
            ] },
        new ("Victory Rush") { Conditions = [
            (state, ability) => state.PlayerHpp < 90
            ] },
        new ("Thunder Clap") { Conditions = [
            (state, ability) => state.InRangeCount > 2
            ] },

        new ("Revenge"),

        new ("Thunder Clap") { Conditions = [
            (state, ability) => state.InRangeCount < 3 && state.InRangeCount > 0,
            (state, ability) => state.TargetDebuffs.FirstOrDefault(a => a.Name == "Thunder Clap")?.Exists != true,
            ] },

        new ("Sunder Armor") { Conditions = [
            (state, ability) => (state.TargetDebuffs.FirstOrDefault(a => a.Name == "Sunder Armor")?.Count ?? 0) < 5
            ] },

        new ("Rend") {
            Conditions = [
                (state, ability) => state.TargetDebuffs.FirstOrDefault(a => a.Name == "Rend")?.Exists != true,
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

        new("Demoralizing Shout") {
            Conditions = [
                (state, ability) => state.InRangeCount > 0
                && state.TargetDebuffs.FirstOrDefault(a => a.Name == "Demoralizing Shout")?.Exists != true
            ] },
        ];


}
