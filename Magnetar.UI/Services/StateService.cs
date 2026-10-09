using Magnetar.UI.Models;
using System.Text;

namespace Magnetar.UI.Services;

public class StateService(ForegroundWindowService foreground)
{
    const int MAX_ABILITIES = 24;
    const int MAX_AURAS = 20;

    public bool IsValidArea {  get; private set; }

    public string ValidationError { get; private set; } = "";

    public bool Enabled { get; private set; }

    public bool IsAoe { get; private set; }

    public bool IsKick { get; private set; }

    public bool IsBurst { get; private set; }

    public bool IsInWorld { get; private set; }

    public bool IsInCombat { get; private set; }

    public WowClass PlayerClass { get; private set; }

    public int PlayerLevel { get; private set; }

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

    public bool TargetIsBoss { get; private set; }

    public int TargetAgro { get; private set; }


    public ActionData[] PlayerAbilities { get; init; } = new ActionData[MAX_ABILITIES];

    public AuraData[] PlayerBuffs { get; init; } = new AuraData[MAX_AURAS];

    public AuraData[] TargetDebuffs { get; init; } = new AuraData[MAX_AURAS];

    public AuraData[] FocusDebuffs { get; init; } = new AuraData[MAX_AURAS];


    public void Parse()
    {
        if (!ParseValidation())
            return;

        IsInWorld = foreground.GetBool(0, 0);
        PlayerClass = (WowClass)foreground.GetInt(0, 0);
        PlayerLevel = foreground.GetInt(0, 0);

        ParsePlayerAbilities();

        ParseAuras(0); // player buffs
        ParseAuras(0); // target debuffs
        ParseAuras(0); // focus debuffs
    }

    private bool ParseValidation()
    {
        var topLeft = foreground.GetInt(0, 0);
        var topRight = foreground.GetInt(0, 200);
        var bottomRight = foreground.GetInt(200, 200);
        var bottomLeft = foreground.GetInt(200, 0);

        if (topLeft != 123456
            && topRight != 123456
            && bottomRight != 123456
            && bottomLeft != 123456)
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

    private void ParseAuras(int rowOffset)
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
