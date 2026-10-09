using Magnetar.UI.Models;
using System.Text;

namespace Magnetar.UI.Services;

public class StateService(ForegroundWindowService foreground)
{
    public bool IsValidArea {  get; private set; }

    public string ValidationError { get; private set; } = "";

    public bool IsInWorld { get; private set; }

    public bool IsInCombat { get; private set; }

    public WowClass PlayerClass { get; private set; }

    public int PlayerHpp { get; private set; }

    public int PlayerPower { get; private set; }

    public int PlayerCombopoints { get; private set; }

    public bool PlayerIsDead { get; private set; }

    public bool PlayerOsMoving { get; private set; }

    public int PlayerCasting { get; private set; }

    public int PlayerChanneling { get; private set; }


    public WowClass TargetClass { get; private set; }

    public int TargetHpp { get; private set; }

    public int TargetCasting { get; private set; }

    public int TargetChanneling { get; private set; }

    public int TargetLevel { get; private set; }

    public bool TargetIsBoss {  get; private set; }

    public int TargetAgro {  get; private set; }


    public List<int> PlayerAbilities { get; private set; } = [];

    public List<int> PlayerBuffs { get; private set; } = [];

    public List<int> TargetDebuffs { get; private set; } = [];


    public void Parse()
    {
        if (!ParseValidation())
            return;

        IsInWorld = foreground.GetBool(0, 0);
        PlayerClass = (WowClass)foreground.GetInt(0, 0);

        ParsePlayerAbilities();
        ParsePlayerBuffs();
        ParseTargetDebuffs();
    }

    private bool ParseValidation()
    {
        var topLeft = foreground.GetInt(0, 0);
        var topRight = foreground.GetInt(0, 0);
        var bottomRight = foreground.GetInt(0, 0);
        var bottomLeft = foreground.GetInt(0, 0);

        if (topLeft != 0
            && topRight != 0
            && bottomRight != 0
            && bottomLeft != 0)
        {
            IsValidArea = true;
            ValidationError = "";
        }
        else
        {
            IsValidArea = false;
            ValidationError = "Area Validation Error";
        }

        return IsValidArea;
    }

    private void ParsePlayerAbilities()
    {
    }

    private void ParsePlayerBuffs()
    {
    }

    private void ParseTargetDebuffs()
    {
    }

    public override string ToString()
    {
        var buff = new StringBuilder();

        buff.AppendLine($"IsInWorld: {IsInWorld}");
        buff.AppendLine($"PlayerClass: {PlayerClass}");

        //todo: add more

        return buff.ToString();
    }
}
