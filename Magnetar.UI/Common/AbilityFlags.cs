using System;
using System.Collections.Generic;
using System.Text;

namespace Magnetar.UI.Common;

[Flags]
public enum AbilityFlags: int
{
    IsOnGCD = 1 << 0,

    IsActive = 1 << 1,

    IsEnabled = 1 << 2,

    IsCurrent = 1 << 3,

    IsUsable = 1 << 4,

    IsLacking = 1 << 5,

    IsAttack = 1 << 6,

    IsHelpful = 1 << 7,

    IsHarmful = 1 << 8,

    HasRangeReq = 1 << 9,

    IsInRangeTraget = 1 << 10,

    IsInRangeFocus = 1 << 11,

    OneMore
}

public static class AbilityFlagsExtensions
{
    public static bool HasAnyFlag(this AbilityFlags flags, AbilityFlags flag)
    {
        return (flags & flag) != 0;
    }
}