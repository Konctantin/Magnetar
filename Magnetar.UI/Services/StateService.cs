using Magnetar.UI.Common;
using Magnetar.UI.Models;
using System.Drawing;
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

    public bool IsInWorld => Magic == 123456;

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
    public int PlayerBuffCount { get; private set; }

    public int InRangeCount { get; private set; }


    public WowClass TargetClass { get; private set; }

    public bool TargetExists { get; private set; }

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

    public int[] Colors = new int[200];

    private readonly ForegroundWindowService foreground;
    private readonly AuraRepoService auraRepoService;

    public StateService(ForegroundWindowService foreground, AuraRepoService auraRepoService)
    {
        this.foreground = foreground;
        this.auraRepoService = auraRepoService;

        for (int i = 0; i < PlayerAbilities.Length; ++i)
            PlayerAbilities[i] = new ActionData();

        for (int i = 0; i < PlayerBuffs.Length; ++i)
            PlayerBuffs[i] = new AuraData();

        for (int i = 0; i < TargetDebuffs.Length; ++i)
            TargetDebuffs[i] = new AuraData();

        for (int i = 0; i < FocusDebuffs.Length; ++i)
            FocusDebuffs[i] = new AuraData();
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
        InRangeCount = GetInt(21, GENERAL_ROW);

        TargetExists = GetBool(30, GENERAL_ROW);
        TargetCasting = GetInt(31, GENERAL_ROW);
        TargetChanneling = GetInt(32, GENERAL_ROW);
        TargetHpp = GetInt(35, GENERAL_ROW);


        ParsePlayerAbilities();

        ParseAuras(PlayerBuffs, 22); // player buffs
        ParseAuras(TargetDebuffs, 35); // target debuffs
        ParseAuras(FocusDebuffs, 50); // focus debuffs
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
            ab.Flags   = (AbilityFlags)GetInt((x * 5) + 3, 2);
            ab.Cooldown= GetInt((x * 5) + 4, 2);
            ab.HotKey  = GetHotKey((x * 5) + 5, 2);

            var matrix = GetPixels((x * 5) + 1, 12, 5, 10);
            var text = WoWPixelDecoder.DecodeTextFromMatrix(matrix);
            ab.Name = text;
        }
    }

    private void ParseAuras(AuraData[] auras, int rowOffset)
    {
        for (int x = 0; x < MAX_AURAS; ++x)
        {
            var aura = auras[x];

            aura.Exists = false;
            aura.Remains = 0;
            aura.Count = 0;
            aura.Name = "";

            var x_offset = (x * 5);

            var m1 = GetInt(x_offset + 1, rowOffset);
            var m2 = GetInt(x_offset + 2, rowOffset);
            var m3 = GetInt(x_offset + 1, rowOffset + 1);
            var m4 = GetInt(x_offset + 2, rowOffset + 1);

            if (m1 == 22 && m2 == 22 && m3 == 22 && m4 == 22)
            {
                aura.Remains = GetInt(x_offset + 3, rowOffset);
                aura.Count = GetInt(x_offset + 5, rowOffset);

                var matrix = GetPixels(x_offset + 1, rowOffset + 32, 5, 30);
                var text = WoWPixelDecoder.DecodeTextFromMatrix(matrix);
                aura.Name = text;
                //GetIcon(aura, x_offset + 1, rowOffset + 11);
                //auraRepoService.AddIfNotExists(aura.Hash, aura.Image);
                //aura.Name = auraRepoService.GetName(aura.Hash);
                //aura.Image.Save("d:/temp/icons/123.bmp");
                // todo: parse icon

                aura.Exists = true;
            }
        }
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

    public void GetIcon(AuraData aura, int x_start, int y_start)
    {
        var x_real = AreaRect.Left + x_start - 1;
        var y_real = AreaRect.Bottom - y_start; // bottom is a zero level ???
        aura.CropIcon(foreground.Image, x_real, y_real);
    }

    public override string ToString()
    {
        var buff = new StringBuilder();

        buff.AppendLine($"Rect: {AreaRect} => Valid: {IsValidArea} ({ValidationError}), Magic: {Magic}");

        buff.AppendLine($"State=> Enabled: {Enabled}, AOE: {IsAoe}, Burst: {IsBurst}, Kick: {IsKick}");
        buff.AppendLine($"Player: {PlayerClass} [{PlayerForm}] ({PlayerLevel}) InCombat: {IsInCombat}");
        buff.AppendLine($"PlayerHPP: {PlayerHpp}, Power: {PlayerPower}, Combo: {PlayerCombopoints}");
        buff.AppendLine($"Moving: {PlayerIsMoving}");
        buff.AppendLine($"PlayerCasting: {PlayerCasting/1000f:0.00}");
        buff.AppendLine($"PlayerChanneling: {PlayerChanneling / 1000f:0.00}");
        buff.AppendLine($"InRangeCount: {InRangeCount}");
        buff.AppendLine($"PlayerBuffCount: {PlayerBuffs.Count(b => b.Exists)}");

        buff.AppendLine($"Target: {TargetExists}, HPP: {TargetHpp}, Casting: {TargetCasting}, Channeling: {TargetChanneling}");
        //buff.AppendLine($"TargetHPP: {TargetHpp}");
        //buff.AppendLine($"TargetKick: {TargetKick}");

        buff.AppendLine();
        buff.AppendLine($">> Player Auras: >>>");
        foreach (var aura in PlayerBuffs)
        {
            if (aura.Exists)
            {
                buff.AppendLine($"Aura: {aura.Name} [Remains: {aura.Remains/100:N2}, Count: {aura.Count}]");
            }
        }
        buff.AppendLine($">> Target Auras: >>>");
        foreach (var aura in TargetDebuffs)
        {
            if (aura.Exists)
            {
                buff.AppendLine($"Aura: {aura.Name} [Remains: {aura.Remains / 100:N2}, Count: {aura.Count}]");
            }
        }

        buff.AppendLine($">> Focus Auras: >>>");
        foreach (var aura in FocusDebuffs)
        {
            if (aura.Exists)
            {
                buff.AppendLine($"Aura: {aura.Name} [Remains: {aura.Remains / 100:N2}, Count: {aura.Count}]");
            }
        }

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
