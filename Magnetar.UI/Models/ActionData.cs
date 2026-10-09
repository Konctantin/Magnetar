using System;
using System.Collections.Generic;
using System.Text;

namespace Magnetar.UI.Models;

public class ActionData
{
    public string Name { get; set; } = "";

    public int HotKey { get; set; } = 0;

    public bool IsReady { get; set; } = false;
}
