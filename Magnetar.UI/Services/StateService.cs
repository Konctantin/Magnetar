using H.NotifyIcon.Core;
using Magnetar.UI.Common;
using Magnetar.UI.Models;
using System.Drawing;
using System.Globalization;
using System.Text;

namespace Magnetar.UI.Services;

public class StateService
{
    const int MAX_ABILITIES = 24;
    const int MAX_AURAS = 20;

    const int GENERAL_ROW = 1;

    private Rect AreaRect;

    public bool IsValidArea {  get; private set; }

    public string ValidationError { get; private set; } = "";

    public int Magic { get; private set; }

    public bool Enabled { get; private set; }

    public bool IsAoe { get; private set; }

    public bool IsKick { get; private set; }

    public bool IsBurst { get; private set; }

    public bool IsInWorld { get; private set; }

    public bool IsInCombat { get; private set; }

    public WowClass PlayerClass { get; private set; }

    public int PlayerLevel { get; private set; }

    public int PlayerForm { get; private set; }

    public int PlayerHpp { get; private set; }

    public int PlayerPower { get; private set; }

    public int PlayerCombopoints { get; private set; }

    public bool PlayerIsDead { get; private set; }

    public bool PlayerIsMoving { get; private set; }

    public int PlayerCasting { get; private set; }

    public int PlayerChanneling { get; private set; }


    public WowClass TargetClass { get; private set; }

    public bool TargetExists { get; private set; }

    public int TargetHpp { get; private set; }

    public bool TargetKick { get; private set; }

    public int TargetLevel { get; private set; }

    public bool TargetIsBoss { get; private set; }

    public int TargetAgro { get; private set; }


    public ActionData[] PlayerAbilities { get; init; } = new ActionData[MAX_ABILITIES];

    public AuraData[] PlayerBuffs { get; init; } = new AuraData[MAX_AURAS];

    public AuraData[] TargetDebuffs { get; init; } = new AuraData[MAX_AURAS];

    public AuraData[] FocusDebuffs { get; init; } = new AuraData[MAX_AURAS];

    public int[] Colors = new int[200];

    private readonly ForegroundWindowService foreground;

    public StateService(ForegroundWindowService foreground)
    {
        this.foreground = foreground;
        for (int i = 0; i < PlayerAbilities.Length; ++i)
        {
            PlayerAbilities[i] = new ActionData();
        }
    }


    public void Parse()
    {
        Magic = 0;
        if (!ParseValidation(ref AreaRect))
            return;

        Magic = GetInt(1, GENERAL_ROW);

        Enabled = GetBool(2, GENERAL_ROW);
        IsAoe = GetBool(3, GENERAL_ROW);
        IsKick = GetBool(4, GENERAL_ROW);
        IsBurst = GetBool(5, GENERAL_ROW);

        IsInCombat = GetBool(10, GENERAL_ROW);
        PlayerIsDead = GetBool(11, GENERAL_ROW);
        PlayerIsMoving = GetBool(12, GENERAL_ROW);

        PlayerHpp = GetInt(13, GENERAL_ROW);
        PlayerPower = GetInt(14, GENERAL_ROW);
        PlayerCombopoints = GetInt(15, GENERAL_ROW);


        PlayerLevel = GetInt(16, GENERAL_ROW);
        PlayerClass = (WowClass)GetInt(17, GENERAL_ROW);
        PlayerForm = GetInt(18, GENERAL_ROW);
        PlayerCasting = GetInt(19, GENERAL_ROW);
        PlayerChanneling = GetInt(20, GENERAL_ROW);

        TargetExists = GetBool(30, GENERAL_ROW);
        TargetKick = GetBool(31, GENERAL_ROW);
        TargetHpp = GetInt(35, GENERAL_ROW);


        ParsePlayerAbilities();

        ParseAuras(0); // player buffs
        ParseAuras(0); // target debuffs
        ParseAuras(0); // focus debuffs
    }

    private bool ParseValidation(ref Rect rect)
    {
        var w = foreground.Image.Width;
        var h = foreground.Image.Height;

        for (int x = 0; x < w; ++x)
        {
            for (int y = 0; y < h; ++y)
            {
                var pixel = foreground.GetInt(x, y);
                if (pixel == 0xFF0000)
                {
                    rect.Left = x;
                    rect.Top = y;
                }
                else if (pixel == 0x00FF00)
                {
                    rect.Right = x;
                    rect.Top = y;
                }
                else if (pixel == 0x0000FF)
                {
                    rect.Right = x;
                    rect.Bottom = y;
                }
            }
        }

        // revalidate
        var topLeft = foreground.GetInt(rect.Left, rect.Top);
        var topRight = foreground.GetInt(rect.Right, rect.Top);
        var bottomRight = foreground.GetInt(rect.Right, rect.Bottom);

        //

        if (topLeft == 0xFF0000 && topRight == 0x00FF00 && bottomRight == 0x0000FF)
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
        for (int x = 0; x < MAX_ABILITIES; ++x)
        {
            var ab = PlayerAbilities[x];
            ab.Magic   = GetInt((x * 5) + 1, 2);
            ab.Type    = GetInt((x * 5) + 2, 2);
            ab.Enabled = GetInt((x * 5) + 3, 2);
            ab.Ready   = GetInt((x * 5) + 4, 2);
            ab.HotKey = GetHotKey((x * 5) + 5, 2);

            var matrix = GetPixels((x * 5) + 1, 12, 5, 10);
            var text = WoWPixelDecoder.DecodeTextFromMatrix(matrix);
            ab.Name = text;
        }
    }

    private void ParseAuras(int rowOffset)
    {
    }

    private KeyRecord GetHotKey(int x, int y)
    {
        var x_real = AreaRect.Left + x - 1;
        var y_real = AreaRect.Bottom - y; // bottom is a zero level ???
        var p = foreground.GetPixel(x_real, y_real);
        var color = p.ToArgb() & 0x00FFFFFF;
        var result = new KeyRecord(0, 0);
        if (KeyMap.KEY_MAP.TryGetValue(color, out var keyRec))
            result = keyRec;
        return result;
    }

    private int GetInt(int x, int y)
    {
        var x_real = AreaRect.Left + x - 1;
        var y_real = AreaRect.Bottom - y; // bottom is a zero level ???
        var p = foreground.GetPixel(x_real, y_real);
        var result = p.B << 16 | p.G << 8 | p.R;
        return result;
    }

    private bool GetBool(int x, int y)
    {
        var x_real = AreaRect.Left + x - 1;
        var y_real = AreaRect.Bottom - y; // bottom is a zero level ???
        var result = foreground.GetBool(x_real, y_real);
        return result;
    }

    public Color[,] GetPixels(int x_start, int y_start, int width, int height)
    {
        var x_real = AreaRect.Left + x_start - 1;
        var y_real = AreaRect.Bottom - y_start; // bottom is a zero level ???
        var result = new Color[height, width];

        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                var pixel = foreground.GetPixel(x_real + x, y_real + y);
                result[y, x] = pixel;
            }
        }

        return result;
    }

    public override string ToString()
    {
        var buff = new StringBuilder();

        buff.AppendLine($"Rect: {AreaRect} => Valid: {IsValidArea} ({ValidationError})");
        buff.AppendLine($"Magic: {Magic}");

        buff.AppendLine($"IsInWorld: {Magic == 123456}");

        buff.AppendLine($"Enable: {Enabled}");
        buff.AppendLine($"AOE: {IsAoe}");
        buff.AppendLine($"Burst: {IsBurst}");
        buff.AppendLine($"Kick: {IsKick}");

        buff.AppendLine($"Combat: {IsInCombat}");
        buff.AppendLine($"Dead: {PlayerIsDead}");
        buff.AppendLine($"Moving: {PlayerIsMoving}");
        buff.AppendLine($"PlayerLevel: {PlayerLevel}");
        buff.AppendLine($"PlayerClass: {PlayerClass}");
        buff.AppendLine($"PlayerForm: {PlayerForm}");
        buff.AppendLine($"PlayerCasting: {PlayerCasting/1000f:0.00}");
        buff.AppendLine($"PlayerChanneling: {PlayerChanneling / 1000f:0.00}");

        buff.AppendLine($"PlayerHPP: {PlayerHpp}");
        buff.AppendLine($"PlayerPower: {PlayerPower}");
        buff.AppendLine($"PlayerCombo: {PlayerCombopoints}");

        //todo: add more

        buff.AppendLine($"TargetExists: {TargetExists}");
        buff.AppendLine($"TargetHPP: {TargetHpp}");
        buff.AppendLine($"TargetKick: {TargetKick}");

        buff.AppendLine();
        buff.AppendLine($">> Abilities: >>>");
        foreach (var ab in PlayerAbilities)
        {
            if (ab.Magic > 0)
                buff.AppendLine(ab.ToString());
        }

        return buff.ToString();
    }
}
