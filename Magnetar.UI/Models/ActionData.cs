using Magnetar.UI.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Magnetar.UI.Models;

public class ActionData
{
    public string Name { get; set; } = "";

    public int Magic { get; set; }

    public int Type { get; set; }

    public int Enabled { get; set; }

    public int Ready { get; set; }

    public AbilityFlags Flags { get; set; }

    public KeyRecord HotKey { get; set; } = new KeyRecord(0, 0);

    public bool IsReady { get; set; } = false;

    public bool IsInRangeTarget { get; set; } = false;

    public bool IsInRangeFocus { get; set; } = false;

    public override string ToString()
    {
        return $"[{Name}]: Type: {Type}, Flags: {Flags}, HotKey: {HotKey}";
    }
}
