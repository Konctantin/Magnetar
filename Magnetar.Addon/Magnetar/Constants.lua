local addonName, T = ...;
_G[addonName] = T;

local DURATION_EPSILON = 0.00001;

local SWATCH_TEXTURE = "Interface\\Buttons\\WHITE8X8";

local function Swatch(r, g, b)
    return ("\124T%s:%d:%d:0:0:8:8:0:8:0:8:%d:%d:%d\124t"):format(SWATCH_TEXTURE, 2, 2, r, g, b);
end

local function IntToColor(v)
    return (v % 256) / 255, (math.floor(v / 256) % 256) / 255, (math.floor(v / 65536) % 256) / 255, 1;
end

local function IntToTextureColor(v)
    return (v % 256), (math.floor(v / 256) % 256), (math.floor(v / 65536) % 256), 1;
end

local function IntToFormat(val)
    local r, g, b = IntToTextureColor(val);
    local result = Swatch(r, g, b);
    return result;
end

-- /dump Magnetar.IntToFormat(200)
T.IntToFormat = IntToFormat;

-- /dump Magnetar.DurationBreakpoints
local function GetDurationBreakpoints()
    local i = 49;
    T.DurationBreakpoints = { { threshold = 5000, format = IntToFormat(50) } };
    while i > 10 do
        table.insert(T.DurationBreakpoints, { threshold = i + DURATION_EPSILON, format = IntToFormat(i*100) });
        i = i - 1;
    end
    while i > 2 do
        table.insert(T.DurationBreakpoints, { threshold = i + DURATION_EPSILON, format = IntToFormat(i*100) });
        i = i - 0.5;
    end
    while i >= 0 do
        table.insert(T.DurationBreakpoints, { threshold = i + DURATION_EPSILON, format = IntToFormat(i*100) });
        i = i - 0.1;
    end
    --print("Count Duration: "..tostring(#T.DurationBreakpoints));
    return T.DurationBreakpoints;
end

local function GetStackBreakpoints()
    local bps = { };
    for i = 0, 50 do
        table.insert(bps, { threshold = i, format = IntToFormat(i) });
    end
    return bps;
end

local function CreateRuleFormatter(breakpoints)
    local f = C_StringUtil.CreateNumericRuleFormatter();
    f:SetBreakpoints(breakpoints);
    return f;
end

-- /dump Magnetar.DurationFormatter
T.DurationFormatter = CreateRuleFormatter(GetDurationBreakpoints());
T.StackFormatter = CreateRuleFormatter(GetStackBreakpoints());

T.KeyMap = {
    ['ESCAPE'] = { R = 0xB8/255, G = 0x17/255, B = 0x20/255 }, -- 0x00B81720
    ['-']      = { R = 0x3D/255, G = 0x30/255, B = 0x80/255 }, -- 0x003D3080
    ['+']      = { R = 0xC2/255, G = 0x49/255, B = 0xE0/255 }, -- 0x00C249E0
    ['F1']     = { R = 0x47/255, G = 0x63/255, B = 0x40/255 }, -- 0x00476340
    ['F2']     = { R = 0xCC/255, G = 0x7C/255, B = 0xA0/255 }, -- 0x00CC7CA0
    ['F3']     = { R = 0x51/255, G = 0x96/255, B = 0x00/255 }, -- 0x00519600
    ['F4']     = { R = 0xD6/255, G = 0xAF/255, B = 0x60/255 }, -- 0x00D6AF60
    ['F5']     = { R = 0x5B/255, G = 0xC8/255, B = 0xC0/255 }, -- 0x005BC8C0
    ['F6']     = { R = 0xE0/255, G = 0xE2/255, B = 0x20/255 }, -- 0x00E0E220
    ['F7']     = { R = 0x65/255, G = 0xFB/255, B = 0x80/255 }, -- 0x0065FB80
    ['F8']     = { R = 0xEB/255, G = 0x14/255, B = 0xE0/255 }, -- 0x00EB14E0
    ['F9']     = { R = 0x70/255, G = 0x2E/255, B = 0x40/255 }, -- 0x00702E40
    ['F10']    = { R = 0xF5/255, G = 0x47/255, B = 0xA0/255 }, -- 0x00F547A0
    ['F11']    = { R = 0x7A/255, G = 0x61/255, B = 0x00/255 }, -- 0x007A6100
    ['F12']    = { R = 0xFF/255, G = 0x7A/255, B = 0x60/255 }, -- 0x00FF7A60
    ['0']      = { R = 0x84/255, G = 0x93/255, B = 0xC0/255 }, -- 0x008493C0
    ['1']      = { R = 0x09/255, G = 0xAD/255, B = 0x20/255 }, -- 0x0009AD20
    ['2']      = { R = 0x8E/255, G = 0xC6/255, B = 0x80/255 }, -- 0x008EC680
    ['3']      = { R = 0x13/255, G = 0xDF/255, B = 0xE0/255 }, -- 0x0013DFE0
    ['4']      = { R = 0x98/255, G = 0xF9/255, B = 0x40/255 }, -- 0x0098F940
    ['5']      = { R = 0x1E/255, G = 0x12/255, B = 0xA0/255 }, -- 0x001E12A0
    ['6']      = { R = 0xA3/255, G = 0x2C/255, B = 0x00/255 }, -- 0x00A32C00
    ['7']      = { R = 0x28/255, G = 0x45/255, B = 0x60/255 }, -- 0x00284560
    ['8']      = { R = 0xAD/255, G = 0x5E/255, B = 0xC0/255 }, -- 0x00AD5EC0
    ['9']      = { R = 0x32/255, G = 0x78/255, B = 0x20/255 }, -- 0x00327820
    ['S-0']    = { R = 0xB7/255, G = 0x91/255, B = 0x80/255 }, -- 0x00B79180
    ['S-1']    = { R = 0x3C/255, G = 0xAA/255, B = 0xE0/255 }, -- 0x003CAAE0
    ['S-2']    = { R = 0xC1/255, G = 0xC4/255, B = 0x40/255 }, -- 0x00C1C440
    ['S-3']    = { R = 0x46/255, G = 0xDD/255, B = 0xA0/255 }, -- 0x0046DDA0
    ['S-4']    = { R = 0xCB/255, G = 0xF7/255, B = 0x00/255 }, -- 0x00CBF700
    ['S-5']    = { R = 0x51/255, G = 0x10/255, B = 0x60/255 }, -- 0x00511060
    ['S-6']    = { R = 0xD6/255, G = 0x29/255, B = 0xC0/255 }, -- 0x00D629C0
    ['S-7']    = { R = 0x5B/255, G = 0x43/255, B = 0x20/255 }, -- 0x005B4320
    ['S-8']    = { R = 0xE0/255, G = 0x5C/255, B = 0x80/255 }, -- 0x00E05C80
    ['S-9']    = { R = 0x65/255, G = 0x75/255, B = 0xE0/255 }, -- 0x006575E0
    ['A']      = { R = 0xEA/255, G = 0x8F/255, B = 0x40/255 }, -- 0x00EA8F40
    ['B']      = { R = 0x6F/255, G = 0xA8/255, B = 0xA0/255 }, -- 0x006FA8A0
    ['C']      = { R = 0xF4/255, G = 0xC2/255, B = 0x00/255 }, -- 0x00F4C200
    ['D']      = { R = 0x79/255, G = 0xDB/255, B = 0x60/255 }, -- 0x0079DB60
    ['E']      = { R = 0xFE/255, G = 0xF4/255, B = 0xC0/255 }, -- 0x00FEF4C0
    ['F']      = { R = 0x84/255, G = 0x0E/255, B = 0x20/255 }, -- 0x00840E20
    ['G']      = { R = 0x09/255, G = 0x27/255, B = 0x80/255 }, -- 0x00092780
    ['H']      = { R = 0x8E/255, G = 0x40/255, B = 0xE0/255 }, -- 0x008E40E0
    ['I']      = { R = 0x13/255, G = 0x5A/255, B = 0x40/255 }, -- 0x00135A40
    ['J']      = { R = 0x98/255, G = 0x73/255, B = 0xA0/255 }, -- 0x009873A0
    ['K']      = { R = 0x1D/255, G = 0x8D/255, B = 0x00/255 }, -- 0x001D8D00
    ['L']      = { R = 0xA2/255, G = 0xA6/255, B = 0x60/255 }, -- 0x00A2A660
    ['M']      = { R = 0x27/255, G = 0xBF/255, B = 0xC0/255 }, -- 0x0027BFC0
    ['N']      = { R = 0xAC/255, G = 0xD9/255, B = 0x20/255 }, -- 0x00ACD920
    ['O']      = { R = 0x31/255, G = 0xF2/255, B = 0x80/255 }, -- 0x0031F280
    ['P']      = { R = 0xB7/255, G = 0x0B/255, B = 0xE0/255 }, -- 0x00B70BE0
    ['Q']      = { R = 0x3C/255, G = 0x25/255, B = 0x40/255 }, -- 0x003C2540
    ['R']      = { R = 0xC1/255, G = 0x3E/255, B = 0xA0/255 }, -- 0x00C13EA0
    ['S']      = { R = 0x46/255, G = 0x58/255, B = 0x00/255 }, -- 0x00465800
    ['T']      = { R = 0xCB/255, G = 0x71/255, B = 0x60/255 }, -- 0x00CB7160
    ['U']      = { R = 0x50/255, G = 0x8A/255, B = 0xC0/255 }, -- 0x00508AC0
    ['V']      = { R = 0xD5/255, G = 0xA4/255, B = 0x20/255 }, -- 0x00D5A420
    ['W']      = { R = 0x5A/255, G = 0xBD/255, B = 0x80/255 }, -- 0x005ABD80
    ['X']      = { R = 0xDF/255, G = 0xD6/255, B = 0xE0/255 }, -- 0x00DFD6E0
    ['Y']      = { R = 0x64/255, G = 0xF0/255, B = 0x40/255 }, -- 0x0064F040
    ['Z']      = { R = 0xEA/255, G = 0x09/255, B = 0xA0/255 }, -- 0x00EA09A0
    ['S-A']    = { R = 0x6F/255, G = 0x23/255, B = 0x00/255 }, -- 0x006F2300
    ['S-B']    = { R = 0xF4/255, G = 0x3C/255, B = 0x60/255 }, -- 0x00F43C60
    ['S-C']    = { R = 0x79/255, G = 0x55/255, B = 0xC0/255 }, -- 0x007955C0
    ['S-D']    = { R = 0xFE/255, G = 0x6F/255, B = 0x20/255 }, -- 0x00FE6F20
    ['S-E']    = { R = 0x83/255, G = 0x88/255, B = 0x80/255 }, -- 0x00838880
    ['S-F']    = { R = 0x08/255, G = 0xA1/255, B = 0xE0/255 }, -- 0x0008A1E0
    ['S-G']    = { R = 0x8D/255, G = 0xBB/255, B = 0x40/255 }, -- 0x008DBB40
    ['S-H']    = { R = 0x12/255, G = 0xD4/255, B = 0xA0/255 }, -- 0x0012D4A0
    ['S-I']    = { R = 0x97/255, G = 0xEE/255, B = 0x00/255 }, -- 0x0097EE00
    ['S-J']    = { R = 0x1D/255, G = 0x07/255, B = 0x60/255 }, -- 0x001D0760
    ['S-K']    = { R = 0xA2/255, G = 0x20/255, B = 0xC0/255 }, -- 0x00A220C0
    ['S-L']    = { R = 0x27/255, G = 0x3A/255, B = 0x20/255 }, -- 0x00273A20
    ['S-M']    = { R = 0xAC/255, G = 0x53/255, B = 0x80/255 }, -- 0x00AC5380
    ['S-N']    = { R = 0x31/255, G = 0x6C/255, B = 0xE0/255 }, -- 0x00316CE0
    ['S-O']    = { R = 0xB6/255, G = 0x86/255, B = 0x40/255 }, -- 0x00B68640
    ['S-P']    = { R = 0x3B/255, G = 0x9F/255, B = 0xA0/255 }, -- 0x003B9FA0
    ['S-Q']    = { R = 0xC0/255, G = 0xB9/255, B = 0x00/255 }, -- 0x00C0B900
    ['S-R']    = { R = 0x45/255, G = 0xD2/255, B = 0x60/255 }, -- 0x0045D260
    ['S-S']    = { R = 0xCA/255, G = 0xEB/255, B = 0xC0/255 }, -- 0x00CAEBC0
    ['S-T']    = { R = 0x50/255, G = 0x05/255, B = 0x20/255 }, -- 0x00500520
    ['S-U']    = { R = 0xD5/255, G = 0x1E/255, B = 0x80/255 }, -- 0x00D51E80
    ['S-V']    = { R = 0x5A/255, G = 0x37/255, B = 0xE0/255 }, -- 0x005A37E0
    ['S-W']    = { R = 0xDF/255, G = 0x51/255, B = 0x40/255 }, -- 0x00DF5140
    ['S-X']    = { R = 0x64/255, G = 0x6A/255, B = 0xA0/255 }, -- 0x00646AA0
    ['S-Y']    = { R = 0xE9/255, G = 0x84/255, B = 0x00/255 }, -- 0x00E98400
    ['S-Z']    = { R = 0x6E/255, G = 0x9D/255, B = 0x60/255 }, -- 0x006E9D60
};

