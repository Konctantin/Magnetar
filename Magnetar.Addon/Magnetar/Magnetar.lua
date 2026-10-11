local addonName, T = ...;
_G[addonName] = T;

T.ACTION_BAR_TYPES = { 'Action', 'MultiBarBottomLeft', 'MultiBarBottomRight', 'MultiBarRight', 'MultiBarLeft' };

-- ==================== CONFIG ====================
local INFO_WIDTH = 200;
local INFO_HEIGHT = 70;
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

local function UpdateTextData(blockIndex, text)
    local pixels = T.AbilityBar[blockIndex];
    if not pixels then return end
    pixels = pixels.TextBar;
    if not pixels then return end

    -- Важно: используем string.len, так как она возвращает длину в БАЙТАХ, а не символах
    local byteLen = text and string.len(text) or 0;
    local maxBytes = 75; -- 5 пикселей * 5 пикселей * 3 канала = 75 байт

    -- Если строка в байтах длиннее 75, мы должны БЕЗОПАСНО обрезать её по границе UTF-8
    if byteLen > maxBytes then
        -- Ищем последнюю валидную границу символа UTF-8 в пределах 75 байт
        local safeLength = maxBytes;
        while safeLength > 0 do
            local byte = string.byte(text, safeLength);
            -- В UTF-8 байты продолжения символа лежат в диапазоне 128-191 (0x80-0xBF)
            -- Если байт начинается с 10xxxxxx, это не начало символа.
            if byte and byte >= 128 and byte <= 191 then
                safeLength = safeLength - 1;
            else
                -- Нашли начало многобайтового символа. 
                -- Но если этот символ целиком не влезает в лимит, сдвигаемся еще на 1 байт назад
                safeLength = safeLength - 1;
                break;
            end
        end
        text = string.sub(text, 1, safeLength);
        byteLen = safeLength;
    end

    local byteIndex = 1;
    for y = 1, 10 do
        for x = 1, 5 do
            if byteIndex > byteLen then
                -- Оставшиеся пиксели заливаем черным
                pixels[x][y]:SetColorTexture(0, 0, 0, 1);
            else
                -- Извлекаем сырые байты UTF-8. string.byte идеально подходит для этого.
                local r = (string.byte(text, byteIndex) or 0) / 255;
                local g = (string.byte(text, byteIndex + 1) or 0) / 255;
                local b = (string.byte(text, byteIndex + 2) or 0) / 255;

                pixels[x][y]:SetColorTexture(r, g, b, 1);
                byteIndex = byteIndex + 3;
            end
        end
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
local function SetCurveCell(col, color)
    T.GeneralBar[col]:SetColorTexture(color:GetRGBA());
end

local function UpdateGeneralInfo()
    local hc = T.MagnetarFrame.hc;

    T.GeneralBar[1]:SetColorTexture(IntToColor(123456)); --magic

    T.GeneralBar[2]:SetColorTexture(IntToColor(T.GetToggle("Magnetar_ControlPanel_EnabledButton") and 1 or 0));
    T.GeneralBar[3]:SetColorTexture(IntToColor(T.GetToggle("Magnetar_ControlPanel_AoeButton") and 1 or 0));
    T.GeneralBar[4]:SetColorTexture(IntToColor(T.GetToggle("Magnetar_ControlPanel_KickButton") and 1 or 0));
    T.GeneralBar[5]:SetColorTexture(IntToColor(T.GetToggle("Magnetar_ControlPanel_CDButton") and 1 or 0));

    T.GeneralBar[10]:SetColorTexture(IntToColor(UnitAffectingCombat("player") and 1 or 0));
    T.GeneralBar[11]:SetColorTexture(IntToColor(UnitIsDead("player") and 1 or 0));
    T.GeneralBar[12]:SetColorTexture(IntToColor(IsPlayerMoving() and 1 or 0));

    SetCurveCell(13, UnitHealthPercent("player", false, hc));
    SetCurveCell(14, UnitPowerPercent("player", nil, false, hc));
    SetCurveCell(15, UnitPowerPercent("player", Enum.PowerType.ComboPoints, false, hc));

    T.GeneralBar[16]:SetColorTexture(IntToColor(UnitLevel("player")));
    T.GeneralBar[17]:SetColorTexture(IntToColor(select(3, UnitClass("player"))));
    T.GeneralBar[18]:SetColorTexture(IntToColor(GetShapeshiftForm()));

    local endTime = select(5, UnitCastingInfo("player")) or 0;
    local remains = math.max(0, math.floor((endTime - (GetTime() * 1000))));
    T.GeneralBar[19]:SetColorTexture(IntToColor(remains));

    endTime = select(5, UnitChannelInfo("player")) or 0;
    remains = math.max(0, math.floor((endTime - (GetTime() * 1000))));
    T.GeneralBar[20]:SetColorTexture(IntToColor(remains));


    T.GeneralBar[30]:SetColorTexture(IntToColor(UnitExists("target") and 1 or 0));
    T.GeneralBar[31]:SetColorTexture(IntToColor(T.CheckInterrupt("target") and 1 or 0));
    SetCurveCell(35, UnitHealthPercent("target", false, hc));
    SetCurveCell(36, UnitPowerPercent("target", nil, false, hc));
    SetCurveCell(37, UnitPowerPercent("target", Enum.PowerType.ComboPoints, false, hc));

    T.GeneralBar[50]:SetColorTexture(IntToColor(UnitExists("focus") and 1 or 0));
    T.GeneralBar[51]:SetColorTexture(IntToColor(T.CheckInterrupt("focus") and 1 or 0));
    SetCurveCell(55, UnitHealthPercent("focus", false, hc));
    SetCurveCell(56, UnitPowerPercent("focus", nil, false, hc));
    SetCurveCell(57, UnitPowerPercent("focus", Enum.PowerType.ComboPoints, false, hc));
end

local function Tick()
    UpdateGeneralInfo();

    T.DumpAbilities();

    --UpdateTextData(1, 'Hello world, Привет мЫр !! Вітаннячко ))');
    -- local hc = T.MagnetarFrame.hc
    -- for i, key in ipairs(PANEL_KEYS) do
    --     SetIntCell(1, i, T.GetToggle("Magnetar_ControlPanel_" .. key) and 1 or 0)
    -- end


    -- SetIntCell(1, 5, UnitLevel("player"))
    -- SetIntCell(1, 6, select(3, UnitClass("player")))
    -- SetBoolCell(1, 7, UnitAffectingCombat("player"))
    -- SetBoolCell(1, 8, UnitIsDead("player"))
    -- SetBoolCell(1, 9, IsPlayerMoving())
    -- SetCurveCell(1, 10, UnitHealthPercent("player", false, hc))
    -- SetCurveCell(1, 11, UnitPowerPercent("player", nil, false, hc))
    -- SetCurveCell(1, 12, UnitPowerPercent("player", Enum.PowerType.ComboPoints, false, hc))
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

-- /dump Magnetar.DumpAbilities()
function T.DumpAbilities()
    -- reset
    for _, ab in ipairs(T.AbilityBar) do
        ab:ResetData();
    end

    -- fill
    local index = 1;
    for _, barName in pairs(T.ACTION_BAR_TYPES) do
        for i = 1, 12 do
            local button = _G[barName .. 'Button' .. i];
            if button and button.HotKey then
                local actionType, id = GetActionInfo(button.action);
                local text = C_ActionBar.GetActionText(button.action);
                local hotKey = string.upper(button.HotKey:GetText());
                local typeInt = 3;
                if actionType == 'spell' then
                    typeInt = 1;
                    local info = C_Spell.GetSpellInfo(id);
                    if info and info.name then
                        text = info.name;
                    end
                elseif actionType == 'item' then
                    typeInt = 2;
                    local info = C_Item.GetItemNameByID(id);
                    text = info;
                end
                local hotKeyColor = T.KeyMap[hotKey];
                if text and hotKeyColor then
                    --print(actionType, typeInt, id, text, hotKey);
                    local ab = T.AbilityBar[index];
                    if ab then
                        ab.Magic:SetColorTexture(IntToColor(1));
                        ab.Type:SetColorTexture(IntToColor(typeInt));
                        local cd_info = C_ActionBar.GetActionCooldown(button.action);
                        ---@diagnostic disable-next-line: undefined-field
                        local ready = not cd_info.isOnGCD and not cd_info.isActive and cd_info.isEnabled;
                        ab.Ready:SetColorTexture(IntToColor(ready and 1 or 0));
                        ab.HotKey:SetColorTexture(hotKeyColor.R, hotKeyColor.G, hotKeyColor.B, 1);
                        UpdateTextData(index, text);
                    end
                    index = index + 1;
                end
            end
        end
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
    local f = CreateFrame("Frame");
    f:SetFrameStrata("TOOLTIP");
    f:SetFrameLevel(10);
    f:SetSize(INFO_WIDTH, INFO_HEIGHT);
    f:SetPoint("BOTTOMLEFT", UIParent, "BOTTOMLEFT", 0, 0);

    local bg = f:CreateTexture(nil, "BACKGROUND");
    bg:SetAllPoints(f);
    bg:SetColorTexture(0, 0, 0, 1);

    local m = f:CreateTexture(nil, "ARTWORK");
    m:SetSize(1,1);
    m:SetPoint("TOPLEFT", f, "TOPLEFT");
    m:SetColorTexture(1, 0, 0, 1);

    m = f:CreateTexture(nil, "ARTWORK");
    m:SetSize(1,1);
    m:SetPoint("TOPRIGHT", f, "TOPRIGHT");
    m:SetColorTexture(0, 1, 0, 1);

    m = f:CreateTexture(nil, "ARTWORK");
    m:SetSize(1,1);
    m:SetPoint("BOTTOMRIGHT", f, "BOTTOMRIGHT");
    m:SetColorTexture(0, 0, 1, 1);

    -- General info
    T.GeneralBar = {};
    local GENERAL_ROW = 1;
    for i = 1, INFO_WIDTH-1 do
        local tex = f:CreateTexture(nil, "ARTWORK");
        tex:SetSize(1, 1);
        tex:SetPoint("BOTTOMLEFT", f, "BOTTOMLEFT", i-1, GENERAL_ROW);
        tex:SetColorTexture(0, 0, 0, 1);
        ---@diagnostic disable-next-line: inject-field
        tex.SetColorValue = function(self, value)
            self:SetColorTexture(0, 0, 0, 1);
        end
        T.GeneralBar[i] = tex;
    end

    local function CreateAbilityPixel(parent, x, y)
        local pixel = parent:CreateTexture(nil, "ARTWORK");
        pixel:SetSize(1, 1);
        pixel:SetPoint("TOPLEFT", parent, "TOPLEFT", x, y);
        pixel:SetColorTexture(0, 0, 0, 1);
        return pixel;
    end

    local ABILITY_ROW = 2;
    local ABILITY_WIDTH = 5;
    local ABILITY_HEIGHT = 11;
    T.AbilityBar = {};

    for i = 1, 24 do
        local ab = CreateFrame("Frame", nil, f);
        ab:SetSize(ABILITY_WIDTH, ABILITY_HEIGHT);
        ab:SetPoint("BOTTOMLEFT", f, "BOTTOMLEFT", (i-1)*ABILITY_WIDTH, ABILITY_ROW-ABILITY_HEIGHT+1);

        ab.Magic         = CreateAbilityPixel(ab, 0, 0);
        ab.Type          = CreateAbilityPixel(ab, 1, 0);
        ab.Enabled       = CreateAbilityPixel(ab, 2, 0);
        ab.Ready         = CreateAbilityPixel(ab, 3, 0);
        --ab.Gcd           = CreateAbilityPixel(ab, 4, 0);
        ab.HotKey        = CreateAbilityPixel(ab, 4, 0);
        --ab.InRangeTarget = CreateAbilityPixel(ab, 6, 0);
        --ab.InRangeFocus  = CreateAbilityPixel(ab, 7, 0);

        ab.ResetData = function(self)
            if self.Magic then self.Magic:SetColorTexture(0, 0, 0, 1) end
            if self.Type then self.Type:SetColorTexture(0, 0, 0, 1) end
            if self.Enabled then self.Enabled:SetColorTexture(0, 0, 0, 1) end
            if self.Ready then self.Ready:SetColorTexture(0, 0, 0, 1) end
            if self.HotKey then self.HotKey:SetColorTexture(0, 0, 0, 1) end
            
            -- Сброс закомментированных пикселей (на случай, если вы их раскомментируете)
            --if self.Gcd then self.Gcd:SetColorTexture(0, 0, 0, 1) end
            --if self.InRangeTarget then self.InRangeTarget:SetColorTexture(0, 0, 0, 1) end
            --if self.InRangeFocus then self.InRangeFocus:SetColorTexture(0, 0, 0, 1) end

            -- Сброс пикселей в текстовой панели (TextBar)
            if self.TextBar then
                for x = 1, #self.TextBar do
                    for y = 1, #self.TextBar[x] do
                        self.TextBar[x][y]:SetColorTexture(0, 0, 0, 1)
                    end
                end
            end
        end;

        ab.TextBar = {};
        for x = 1, ABILITY_WIDTH do
            ab.TextBar[x] = {};
            for y = 1, ABILITY_HEIGHT-1 do
                -- +1 y offset
                ab.TextBar[x][y] = CreateAbilityPixel(ab, x-1, ABILITY_HEIGHT-y);
            end
        end
        T.AbilityBar[i] = ab;
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

local function StackRGB(n)
    if n <= 0 then return 128, 128, 128 end
    if n > 30 then return 255, 255, 255 end
    local band, c = math.floor((n - 1) / 10), T.STACK_HUES[(n - 1) % 10 + 1]
    if band == 1 then return Half(c[1]), Half(c[2]), Half(c[3]) end
    if band == 2 then return Half(c[1]) + 128, Half(c[2]) + 128, Half(c[3]) + 128 end
    return c[1], c[2], c[3];
end

local function PaletteSwatch(p) return Swatch(To255(p[2]), To255(p[3]), To255(p[4])) end

-- Брейкпоинты по убыванию: движок берёт первое подходящее правило
local function BuildDurationBreakpoints()
    local P = T.DURATION_PALETTE;
    local bps = {
        {
            threshold = P[#P][1] + T.DURATION_EPSILON,
            format = PaletteSwatch(T.DURATION_OVER_MAX)
        }
    };

    for i = #P, 2, -1 do
        bps[#bps + 1] = {
            threshold = P[i - 1][1] + T.DURATION_EPSILON,
            format = PaletteSwatch(P[i])
        };
    end

    bps[#bps + 1] = {
        threshold = 0,
        format = PaletteSwatch(P[1])
    };
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

local function CreateCellAura(button, width, height)
    local fs = button:CreateFontString(nil, "OVERLAY");
    fs:SetFont(STANDARD_TEXT_FONT, AURA_CELL * 0.8, "");
    fs:SetShadowOffset(0, 0);
    fs:SetTextColor(1, 1, 1, 1);
    fs:SetWordWrap(false);
    -- Задаем размер динамически, чтобы текст не обрезался и не вылезал
    fs:SetSize(width, height); 
    fs:SetJustifyH("CENTER");
    fs:SetJustifyV("MIDDLE");
    return fs;
end

-- [ICON][gap][DURATION][gap][STACK]
local function InitAuraButton(button)
    table.insert(T.auras, button);
    
    -- ВНИМАНИЕ: убедитесь, что общая высота AURA_BUTTON_H теперь учитывает 
    -- размер иконки + высоту ячеек под ней (например, AURA_CELL + высота_текста)
    button:SetSize(AURA_CELL, AURA_BUTTON_H or (AURA_CELL * 1.8));

    -- Иконку прижимаем к верхнему краю кнопки по центру
    button.Icon = button:CreateTexture(nil, "OVERLAY");
    button.Icon:SetSize(AURA_CELL, AURA_CELL);
    button.Icon:SetPoint("TOP", button, "TOP", 0, 0);
    button:SetIcon(button.Icon);

    -- Рассчитываем размеры для под-иконочных ячеек
    local cellWidth = AURA_CELL / 2;
    local cellHeight = AURA_CELL * 0.8; -- Высота подгоняется под размер шрифта

    -- 1. DurationCell: снизу-слева ПОД иконкой
    button.DurationCell = CreateCellAura(button, cellWidth, cellHeight);
    button.DurationCell:SetPoint("TOPLEFT", button.Icon, "BOTTOMLEFT", 0, 0);

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

    -- 2. StackCell: снизу-справа ПОД иконкой (стыкуется к правому краю DurationCell)
    button.StackCell = CreateCellAura(button, cellWidth, cellHeight);
    button.StackCell:SetPoint("TOPLEFT", button.DurationCell, "TOPRIGHT", 0, 0);
    
    button:SetApplicationCount(button.StackCell, { formatter = T.StackFormatter });
end

-- column: 1, 2, ... — колонки идут вправо от AURA_X_PX
local function CreateAuraBar(unit, filter, column)
    local c = CreateFrame("AuraContainer", nil, T.MagnetarFrame, "CustomAuraContainerTemplate");
    c:SetSize(AURA_BUTTON_W, AURA_MAX * (AURA_CELL + AURA_GAP)); -- ровно одна колонка
    c:SetFrameLevel(100);
    c:SetPoint("BOTTOMLEFT", T.MagnetarFrame, "BOTTOMLEFT", AURA_X_PX / AURA_SCALE + (column - 1) * (AURA_BUTTON_W + 10), 0);
    c:SetUnit(unit);
    c:SetFlowLayoutAxis(AnchorUtil.FlowLayoutAxis.Horizontal);
    c:SetFlowLayoutGrowthDirection(1, -1);
    c:AddAuraGroup("magnetarAuras_" .. unit, filter, {
        maxFrameCount = AURA_MAX,
        initializeFrame = InitAuraButton
    });
    c:UpdateAllAuras();
    return c;
end

T.PlayerContainer = CreateAuraBar("player", "HELPFUL|PLAYER|RAID", 1);
T.TargetContainer = CreateAuraBar("target", "HARMFUL|PLAYER", 2);
ApplyScales();
