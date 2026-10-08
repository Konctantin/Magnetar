local addonName, T = ...;
_G[addonName] = T;

-- ==================== CONFIG ====================
local TICK_INTERVAL = 0.15                              -- сек между обновлениями сетки
local GRID_ROWS, GRID_COLS, GRID_CELL_PX = 5, 100, 1    -- клетка сетки в физ. пикселях (2-3 надёжнее)
local AURA_SCALE = 1                                    -- 1 = клетка 10 px, 4.8 = 48 px; всё растёт пропорционально
local AURA_CELL, AURA_GAP, AURA_MAX = 10, 0, 10         -- базовая раскладка аур (px при AURA_SCALE = 1)
local AURA_X_PX = 110                                   -- левый край первой колонки аур, физ. px
local SWATCH_TEXTURE = "Interface\\Buttons\\WHITE8X8"
local PANEL_KEYS = { "EnabledButton", "AoeButton", "CDButton", "KickButton" }

-- ==================== STORAGE ====================
-- SavedVariables грузятся после файлов аддона, поэтому дефолты ставим в ADDON_LOADED.
-- Имя должно совпадать с ## SavedVariables в .toc.
local function InitStorage()
    MAGNETAR_PRESONAL_STORAGE = MAGNETAR_PRESONAL_STORAGE or {}
    MAGNETAR_PRESONAL_STORAGE.BUTTON_STATE = MAGNETAR_PRESONAL_STORAGE.BUTTON_STATE or {}
end

function T.GetToggle(key)
    local s = MAGNETAR_PRESONAL_STORAGE
    return s and s.BUTTON_STATE and s.BUTTON_STATE[key]
end

function T.Notify(msg, toChat)
    if T.InfoFrame then
        T.InfoFrame.Msg:SetText(msg)
        T.InfoFrame.Duration = GetTime() + 5
    end
    if toChat then print(msg) end
end

-- ==================== COLOR HELPERS ====================
local function IntToColor(v)
    return (v % 256) / 255, (math.floor(v / 256) % 256) / 255, (math.floor(v / 65536) % 256) / 255, 1
end

local function To255(x) return math.floor(x * 255 + 0.5) end
local function Half(v) return math.floor(v / 2) end

-- Сплошной квадрат внутри FontString, r/g/b: 0..255
local function Swatch(r, g, b)
    return ("|T%s:%d:%d:0:0:8:8:0:8:0:8:%d:%d:%d|t"):format(SWATCH_TEXTURE, AURA_CELL, AURA_CELL, r, g, b);
end

local COLOR_TRUE, COLOR_FALSE = CreateColor(IntToColor(1)), CreateColor(IntToColor(0));

-- Step: [0.00, 0.05) -> 0, [0.05, 0.10) -> 5, ..., 1.00 -> 100
local function CreatePercentCurve()
    local curve = C_CurveUtil.CreateColorCurve();
    curve:SetType(Enum.LuaCurveType.Step);
    for k = 0, 20 do
        curve:AddPoint(k / 20, CreateColor(IntToColor(k * 5)));
    end
    return curve;
end

-- ==================== SCALE ====================
-- Эффективный масштаб 768 / высота экрана => 1 единица = 1 физ. пиксель при любом UI Scale
local function PixelScale()
    local _, h = GetPhysicalScreenSize();
    return (h and h > 0) and 768 / h or 1;
end

local function ApplyScales()
    local s = PixelScale()
    if T.MagnetarFrame then T.MagnetarFrame:SetScale(s) end -- без родителя: свой scale = эффективный
    if InCombatLockdown() then return end                   -- контейнеры досчитаются на PLAYER_REGEN_ENABLED
    for _, key in ipairs({ "PlayerContainer", "TargetContainer" }) do
        local c = T[key]
        if c then c:SetScale(s * AURA_SCALE / UIParent:GetEffectiveScale()) end
    end
end

-- ==================== GRID CELLS ====================
local function Cell(row, col) return T.MagnetarFrame.Textures[row][col] end

local function SetIntCell(row, col, value) Cell(row, col):SetColorTexture(IntToColor(value or 0)) end

-- secret boolean нельзя проверять через and/or
local function SetBoolCell(row, col, value)
    local tex = Cell(row, col)
    if issecretvalue(value) then
        tex:SetColorTexture(1, 1, 1, 1)
        tex:SetVertexColorFromBoolean(value, COLOR_TRUE, COLOR_FALSE)
    else
        tex:SetVertexColor(1, 1, 1, 1)
        tex:SetColorTexture(IntToColor(value and 1 or 0))
    end
end

-- результат кривой может быть secret: только передаём дальше
local function SetCurveCell(row, col, color) Cell(row, col):SetColorTexture(color:GetRGBA()) end

local function Tick()
    local hc = T.MagnetarFrame.hc
    for i, key in ipairs(PANEL_KEYS) do
        SetIntCell(1, i, T.GetToggle("Magnetar_ControlPanel_" .. key) and 1 or 0)
    end
    SetIntCell(1, 5, UnitLevel("player"))
    SetIntCell(1, 6, select(3, UnitClass("player")))
    SetBoolCell(1, 7, UnitAffectingCombat("player"))
    SetBoolCell(1, 8, UnitIsDead("player"))
    SetBoolCell(1, 9, IsPlayerMoving())
    SetCurveCell(1, 10, UnitHealthPercent("player", false, hc))
    SetCurveCell(1, 11, UnitPowerPercent("player", nil, false, hc))
    SetCurveCell(1, 12, UnitPowerPercent("player", Enum.PowerType.ComboPoints, false, hc))
end

-- ==================== FRAMES ====================
local function OnUpdate(self, elapsed)
    self.timer = (self.timer or 0) + elapsed
    if self.timer < TICK_INTERVAL then return end
    self.timer = 0
    Tick()
end

local function OnEvent(self, event, arg1)
    if event == "ADDON_LOADED" then
        if arg1 == addonName then
            InitStorage();
            self:UnregisterEvent("ADDON_LOADED");
        end
    elseif event == "PLAYER_ENTERING_WORLD" then
        if self.initialized then return end -- срабатывает на каждом экране загрузки
        self.initialized = true;
        if T.CreateControlPanel then T.CreateControlPanel() end
        if T.ControlPanel then T.ControlPanel:Show() end
        self:SetScript("OnUpdate", OnUpdate);
        print("|cff00ff00PixelTable|r загружен. Таблица внизу слева.")
    elseif event == "PLAYER_TARGET_CHANGED" then
        if T.TargetContainer then T.TargetContainer:UpdateAllAuras(); end
    else -- UI_SCALE_CHANGED / DISPLAY_SIZE_CHANGED / PLAYER_REGEN_ENABLED
        ApplyScales();
    end
end

local function InitInfoFrame()
    local f = CreateFrame("Frame", nil, UIParent);
    f:SetSize(600, 300);
    f:SetPoint("CENTER", UIParent, "CENTER", 0, 200);
    f.Msg = f:CreateFontString(nil, "BACKGROUND", "PVPInfoTextFont");
    f.Msg:SetAllPoints();
    f:SetScript("OnUpdate", function(self)
        if self.Duration and self.Duration < GetTime() then
            self.Msg:SetText("");
            self.Duration = nil;
        end
    end);
    T.InfoFrame = f;
end

local function InitMainFrame()
    local f = CreateFrame("Frame"); -- без родителя: не прячется по Alt+Z и не наследует UI Scale
    f:SetFrameStrata("TOOLTIP");
    f:SetFrameLevel(10);
    f:SetSize(GRID_COLS * GRID_CELL_PX, GRID_ROWS * GRID_CELL_PX);
    f:SetPoint("BOTTOMLEFT", UIParent, "BOTTOMLEFT", 0, 0);

    f.Textures = {};
    for r = 1, GRID_ROWS do
        f.Textures[r] = {};
        for c = 1, GRID_COLS do
            local tex = f:CreateTexture(nil, "BACKGROUND");
            tex:SetSize(GRID_CELL_PX, GRID_CELL_PX);
            tex:SetPoint("TOPLEFT", f, "TOPLEFT", (c - 1) * GRID_CELL_PX, -(r - 1) * GRID_CELL_PX);
            tex:SetColorTexture(0, 0, 0, 1);
            f.Textures[r][c] = tex;
        end
    end

    f.hc = CreatePercentCurve(); -- нужна до первого тика
    local events = {
        "ADDON_LOADED",
        "PLAYER_ENTERING_WORLD",
        "PLAYER_TARGET_CHANGED",
        "UI_SCALE_CHANGED",
        "DISPLAY_SIZE_CHANGED",
        "PLAYER_REGEN_ENABLED"
    };
    for _, e in ipairs(events) do
        f:RegisterEvent(e);
    end
    f:SetScript("OnEvent", OnEvent);
    T.MagnetarFrame = f;
    ApplyScales();
end

InitInfoFrame()
InitMainFrame()

local AURA_BUTTON_W = AURA_CELL * 3 + AURA_GAP * 2;
local function SlotX(i)
    return (i - 1) * (AURA_CELL + AURA_GAP) + AURA_CELL / 2;
end -- центр i-й клетки кнопки

-- { верхняя граница t, r, g, b }: t==0 -> 0.0, (0, 0.1] -> 0.1, ..., (9, 10] -> 10, >10 -> серый
local DURATION_PALETTE = {
    { 0.0, 0.50, 0, 1.00 },
    { 0.1, 0.60, 0, 1.00 },
    { 0.2, 0.70, 0, 1.00 },
    { 0.3, 0.80, 0, 1.00 },
    { 0.4, 0.90, 0, 1.00 },
    { 0.5, 1.00, 0, 1.00 },
    { 0.6, 1.00, 0, 0.80 },
    { 0.7, 1.00, 0, 0.60 },
    { 0.8, 1.00, 0, 0.40 },
    { 0.9, 1.00, 0, 0.20 },
    { 1.0, 1.00, 0, 0.00 },
    { 2.0, 1.00, 0.20, 0 },
    { 3.0, 1.00, 0.40, 0 },
    { 4.0, 1.00, 0.60, 0 },
    { 5.0, 1.00, 0.80, 0 },
    { 6.0, 1.00, 1.00, 0 },
    { 7.0, 0.75, 1.00, 0 },
    { 8.0, 0.50, 1.00, 0 },
    { 9.0, 0.25, 1.00, 0 },
    { 10.0, 0.00, 1.00, 0 },
}
local DURATION_OVER_MAX = { 0, 0.25, 0.25, 0.25 }
local DURATION_EPSILON = 0.00001

-- Стаки: 1..10 яркие оттенки, 11..20 те же тёмные, 21..30 те же пастельные, 0 серый, 31+ белый
local STACK_HUES = {
    { 255, 234, 0 },
    { 255, 149, 0 },
    { 255, 64, 0 },
    { 255, 0, 0 },
    { 255, 0, 128 },  -- жёлтый .. малиновый
    { 213, 0, 255 },
    { 43, 0, 255 },
    { 0, 128, 255 },
    { 0, 255, 255 },
    { 0, 255, 0 },  -- фиолетовый .. зелёный
}

local function StackRGB(n)
    if n <= 0 then return 128, 128, 128 end
    if n > 30 then return 255, 255, 255 end
    local band, c = math.floor((n - 1) / 10), STACK_HUES[(n - 1) % 10 + 1]
    if band == 1 then return Half(c[1]), Half(c[2]), Half(c[3]) end
    if band == 2 then return Half(c[1]) + 128, Half(c[2]) + 128, Half(c[3]) + 128 end
    return c[1], c[2], c[3];
end

local function PaletteSwatch(p) return Swatch(To255(p[2]), To255(p[3]), To255(p[4])) end

-- Брейкпоинты по убыванию: движок берёт первое подходящее правило
local function BuildDurationBreakpoints()
    local P = DURATION_PALETTE;
    local bps = { { threshold = P[#P][1] + DURATION_EPSILON, format = PaletteSwatch(DURATION_OVER_MAX) } };
    for i = #P, 2, -1 do
        bps[#bps + 1] = {
            threshold = P[i - 1][1] + DURATION_EPSILON,
            format = PaletteSwatch(P[i])
        };
    end
    bps[#bps + 1] = { threshold = 0, format = PaletteSwatch(P[1]) };
    return bps;
end

local function BuildStackBreakpoints()
    local bps = {};
    for n = 31, 0, -1 do
        bps[#bps + 1] = {
            threshold = n,
            format = Swatch(StackRGB(n))
        };
    end -- 31 ловит всё >= 31
    return bps;
end

local function CreateRuleFormatter(breakpoints)
    local f = C_StringUtil.CreateNumericRuleFormatter();
    f:SetBreakpoints(breakpoints);
    return f;
end

T.DurationFormatter = CreateRuleFormatter(BuildDurationBreakpoints());
T.StackFormatter = CreateRuleFormatter(BuildStackBreakpoints());

-- Белый цвет текста через штатную опцию биндинга, на случай если он тонирует inline-текстуры
local WHITE_TEXT_CURVE = C_CurveUtil.CreateColorCurve();
WHITE_TEXT_CURVE:SetType(Enum.LuaCurveType.Step);
WHITE_TEXT_CURVE:AddPoint(0, CreateColor(1, 1, 1, 1));

T.auras = {} -- debug

-- Бокс вдвое больше квадрата: текст не обрезается в "...", квадрат остаётся по центру клетки
local function CreateCell(button, slot)
    local fs = button:CreateFontString(nil, "OVERLAY");
    fs:SetFont(STANDARD_TEXT_FONT, AURA_CELL * 0.8, "");
    fs:SetShadowOffset(0, 0);
    fs:SetTextColor(1, 1, 1, 1);
    fs:SetWordWrap(false);
    fs:SetSize(AURA_CELL * 2, AURA_CELL * 2);
    fs:SetPoint("CENTER", button, "LEFT", SlotX(slot), 0);
    fs:SetJustifyH("CENTER");
    fs:SetJustifyV("MIDDLE");
    return fs;
end

-- [ICON][gap][DURATION][gap][STACK]
local function InitAuraButton(button)
    table.insert(T.auras, button);
    button:SetSize(AURA_BUTTON_W, AURA_CELL);

    button.Icon = button:CreateTexture(nil, "OVERLAY");
    button.Icon:SetSize(AURA_CELL, AURA_CELL);
    button.Icon:SetPoint("CENTER", button, "LEFT", SlotX(1), 0);
    button:SetIcon(button.Icon);

    button.DurationCell = CreateCell(button, 2);
    local binding = C_DurationUtil.CreateDurationTextBinding();
    binding:SetFormatter(T.DurationFormatter);
    binding:SetUpdateInterval(0.05);
    button:SetDurationText(button.DurationCell, {
        binding = binding,
        textColor = {
            property = Enum.DurationTextBindingProperty.RemainingDuration,
            curve = WHITE_TEXT_CURVE
        },
    });

    button.StackCell = CreateCell(button, 3);
    button:SetApplicationCount(button.StackCell, { formatter = T.StackFormatter });
end

-- column: 1, 2, ... — колонки идут вправо от AURA_X_PX
local function CreateAuraBar(unit, filter, column)
    local c = CreateFrame("AuraContainer", nil, UIParent, "CustomAuraContainerTemplate");
    c:SetSize(AURA_BUTTON_W, AURA_MAX * (AURA_CELL + AURA_GAP)); -- ровно одна колонка
    c:SetFrameLevel(100);
    c:SetPoint("BOTTOMLEFT", UIParent, "BOTTOMLEFT", AURA_X_PX / AURA_SCALE + (column - 1) * (AURA_BUTTON_W + 10), 0);
    c:SetUnit(unit);
    c:SetFlowLayoutAxis(1);
    c:SetFlowLayoutGrowthDirection(1, -1);
    c:AddAuraGroup("magnetarAuras_" .. unit, filter, { maxFrameCount = AURA_MAX, initializeFrame = InitAuraButton });
    c:UpdateAllAuras();
    return c;
end

T.PlayerContainer = CreateAuraBar("player", "HELPFUL|PLAYER|RAID", 1);
T.TargetContainer = CreateAuraBar("target", "HARMFUL|PLAYER", 2);
ApplyScales();
