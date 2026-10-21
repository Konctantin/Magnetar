local addonName, T = ...;
_G[addonName] = T;

local INFO_WIDTH    = 200;
local INFO_HEIGHT   = 70;
local AURA_CELL     = 10;
local AURA_MAX      = 10;

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
    return (v % 256) / 255, (math.floor(v / 256) % 256) / 255, (math.floor(v / 65536) % 256) / 255, 1;
end

-- ==================== SCALE ====================
local function PixelScale()
    local _, h = GetPhysicalScreenSize();
    return (h and h > 0) and 768 / h or 1;
end

local function ApplyScales()
    local s = PixelScale();
    if T.MagnetarFrame then
        T.MagnetarFrame:SetScale(s);
    end
    if InCombatLockdown() then
        return;
    end
    for _, key in ipairs({ "PlayerContainer", "TargetContainer" }) do
        local c = T[key]
        if c then c:SetScale(s / UIParent:GetEffectiveScale()) end
    end
end

local function UpdateTextData(blockIndex, text)
    local pixels = T.AbilityBar[blockIndex];
    if not pixels then return end
    pixels = pixels.TextBar;
    if not pixels then return end

    local byteLen = text and string.len(text) or 0;
    local maxBytes = 75;

    if byteLen > maxBytes then
        local safeLength = maxBytes;
        while safeLength > 0 do
            local byte = string.byte(text, safeLength);
            if byte and byte >= 128 and byte <= 191 then
                safeLength = safeLength - 1;
            else
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
                pixels[x][y]:SetColorTexture(0, 0, 0, 1);
            else
                local r = (string.byte(text, byteIndex) or 0) / 255;
                local g = (string.byte(text, byteIndex + 1) or 0) / 255;
                local b = (string.byte(text, byteIndex + 2) or 0) / 255;
                pixels[x][y]:SetColorTexture(r, g, b, 1);
                byteIndex = byteIndex + 3;
            end
        end
    end
end

local function UpdateGeneralInfo()
    T.GeneralBar[1]:SetColorValue(123456); --magic

    T.GeneralBar[2]:SetColorValue(T.GetToggle("Magnetar_ControlPanel_EnabledButton"));
    T.GeneralBar[3]:SetColorValue(T.GetToggle("Magnetar_ControlPanel_AoeButton"));
    T.GeneralBar[4]:SetColorValue(T.GetToggle("Magnetar_ControlPanel_KickButton"));
    T.GeneralBar[5]:SetColorValue(T.GetToggle("Magnetar_ControlPanel_CDButton"));

    T.GeneralBar[10]:SetColorValue(UnitAffectingCombat("player"));
    T.GeneralBar[11]:SetColorValue(UnitIsDead("player"));
    T.GeneralBar[12]:SetColorValue(IsPlayerMoving());

    T.GeneralBar[13]:SetColorValue(UnitHealthPercent("player", false, T.PercentCurve));
    T.GeneralBar[14]:SetColorValue(UnitPowerPercent("player", nil, false, T.PercentCurve));
    T.GeneralBar[15]:SetColorValue(UnitPowerPercent("player", Enum.PowerType.ComboPoints, false, T.PercentCurve));

    T.GeneralBar[16]:SetColorValue(UnitLevel("player"));
    T.GeneralBar[17]:SetColorValue(select(3, UnitClass("player")));
    T.GeneralBar[18]:SetColorValue(GetShapeshiftForm());
    T.GeneralBar[19]:SetColorValue(T.GetCastingRemains("player"));
    T.GeneralBar[20]:SetColorValue(T.GetChannelRemains("player"));
    T.GeneralBar[21]:SetColorValue(T.InRangeCount("Rend"));

    T.GeneralBar[30]:SetColorValue(UnitExists("target"));
    T.GeneralBar[31]:SetColorValue(T.GetUnitCastingCurve("target"));
    T.GeneralBar[32]:SetColorValue(T.GetUnitChannelCurve("target"));
    T.GeneralBar[35]:SetColorValue(UnitHealthPercent("target", false, T.PercentCurve));
    T.GeneralBar[36]:SetColorValue(UnitPowerPercent("target", nil, false, T.PercentCurve));
    T.GeneralBar[37]:SetColorValue(UnitPowerPercent("target", Enum.PowerType.ComboPoints, false, T.PercentCurve));

    T.GeneralBar[50]:SetColorValue(UnitExists("focus"));
    T.GeneralBar[51]:SetColorValue(T.GetUnitCastingCurve("focus"));
    T.GeneralBar[52]:SetColorValue(T.GetUnitChannelCurve("focus"));
    T.GeneralBar[55]:SetColorValue(UnitHealthPercent("focus", false, T.PercentCurve));
    T.GeneralBar[56]:SetColorValue(UnitPowerPercent("focus", nil, false, T.PercentCurve));
    T.GeneralBar[57]:SetColorValue(UnitPowerPercent("focus", Enum.PowerType.ComboPoints, false, T.PercentCurve));
end

local function Tick()
    UpdateGeneralInfo();
    T.DumpAbilities();
end

-- ==================== FRAMES ====================
local function OnUpdate(self, elapsed)
    self.timer = (self.timer or 0) + elapsed
    if self.timer < 0.15 then return end
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
        if self.initialized then return end
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
                    text = info or '';
                end
                local hotKeyColor = T.KeyMap[hotKey];
                if text and hotKeyColor then
                    --print(actionType, typeInt, id, text, hotKey);
                    local ab = T.AbilityBar[index];
                    if ab then
                        -- /dump ActionButton1
                        -- /dump C_ActionBar.IsUsableAction(ActionButton2.action)
                        -- /dump C_ActionBar.GetSpell(ActionButton1.action)
                        -- /dump C_ActionBar.GetActionText(ActionButton1.action)
                        -- /dump C_ActionBar.IsHarmfulAction(ActionButton2.action, true)
                        -- /dump C_ActionBar.IsHarmfulAction(ActionButton2.action, false)
                        local cd_info = C_ActionBar.GetActionCooldown(button.action);
                        local isCurrent = C_ActionBar.IsCurrentAction(button.action);
                        local isUsable, isLacking = C_ActionBar.IsUsableAction(button.action);
                        local isAttack = C_ActionBar.IsAttackAction(button.action);
                        local isHelpful = C_ActionBar.IsHelpfulAction(button.action, false);
                        local isHarmful = C_ActionBar.IsHarmfulAction(button.action, false);
                        local hasRange = C_ActionBar.HasRangeRequirements(button.action);
                        local isInRangeTarget = C_ActionBar.IsActionInRange(button.action, "target");
                        local isInRangeFocus = C_ActionBar.IsActionInRange(button.action, "focus");
                        local cd_duration = C_ActionBar.GetActionCooldownDuration(button.action);

                        local flags = T.CreateFlag(
                            cd_info.isOnGCD,
                            ---@diagnostic disable-next-line: undefined-field
                            cd_info.isActive,
                            cd_info.isEnabled,
                            isCurrent,
                            isUsable, isLacking,
                            isAttack,
                            isHelpful,
                            isHarmful,
                            hasRange,
                            isInRangeTarget,
                            isInRangeFocus
                            );
                        ab.Magic:SetColorTexture(IntToColor(1));
                        ab.Type:SetColorTexture(IntToColor(typeInt));
                        ab.Flags:SetColorTexture(IntToColor(flags));

                        -- todo: try to get ability cooldown as color
                        if cd_duration and cd_duration.GetRemainingDuration then
                            local curve = cd_duration:EvaluateRemainingDuration(T.RemainingCurve, Enum.DurationTimeModifier.RealTime);
                            ab.Cooldown:SetColorTexture(curve:GetRGBA());
                        end
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
            local valType = type(value);
            if valType == 'boolean' then
                self:SetColorTexture(IntToColor(value and 1 or 0));
            elseif valType == 'number' then
                self:SetColorTexture(IntToColor(value));
            elseif (valType == "table" or valType == "userdata") and type(value.GetRGBA) == "function" then
                self:SetColorTexture(value:GetRGBA());
            else
                self:SetColorTexture(0, 0, 0, 1);
            end
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

        ab.Magic    = CreateAbilityPixel(ab, 0, 0);
        ab.Type     = CreateAbilityPixel(ab, 1, 0);
        ab.Flags    = CreateAbilityPixel(ab, 2, 0);
        ab.Cooldown = CreateAbilityPixel(ab, 3, 0);
        ab.HotKey   = CreateAbilityPixel(ab, 4, 0);

        ab.ResetData = function(self)
            if self.Magic then self.Magic:SetColorTexture(0, 0, 0, 1) end
            if self.Type then self.Type:SetColorTexture(0, 0, 0, 1) end
            if self.Flags then self.Flags:SetColorTexture(0, 0, 0, 1) end
            if self.Cooldown then self.Cooldown:SetColorTexture(0, 0, 0, 1) end
            if self.HotKey then self.HotKey:SetColorTexture(0, 0, 0, 1) end
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

InitInfoFrame();
InitMainFrame();

local WHITE_TEXT_CURVE = C_CurveUtil.CreateColorCurve();
WHITE_TEXT_CURVE:SetType(Enum.LuaCurveType.Step);
WHITE_TEXT_CURVE:AddPoint(0, CreateColor(1, 1, 1, 1));

T.auras = {} -- debug

local function CreateCellAura(button)
    local fs = button:CreateFontString(nil, "OVERLAY");
    fs:SetFont(STANDARD_TEXT_FONT, AURA_CELL * 0.8, "");
    fs:SetShadowOffset(0, 0);
    fs:SetTextColor(1, 1, 1, 1);
    fs:SetWordWrap(false);
    fs:SetJustifyH("CENTER");
    fs:SetJustifyV("MIDDLE");
    return fs;
end

local function InitAuraButton(button)
    table.insert(T.auras, button);

    button:SetSize(AURA_CELL, AURA_CELL + 2);

    -- icon
    button.Icon = button:CreateTexture(nil, "OVERLAY");
    button.Icon:SetSize(AURA_CELL, AURA_CELL);
    button.Icon:SetPoint("TOP", button, "TOP", 0, 0);
    button:SetIcon(button.Icon);

    -- Magic marker
    button.Magic = button:CreateTexture(nil, "OVERLAY");
    button.Magic:SetSize(2, 2);
    button.Magic:SetPoint("TOPLEFT", button.Icon, "BOTTOMLEFT", 0, 0);
    button.Magic:SetColorTexture(IntToColor(22));

    --  Duration
    button.DurationCell = CreateCellAura(button);
    button.DurationCell:SetPoint("TOPLEFT", button.Icon, "BOTTOMLEFT", 2, 0);
    button.DurationCell:SetSize(2, 2);

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

    -- Stacks
    button.StackCell = CreateCellAura(button);
    button.StackCell:SetPoint("TOPLEFT", button.Icon, "BOTTOMLEFT", 4, 0);
    button.StackCell:SetSize(2,2);

    button:SetApplicationCount(button.StackCell, { formatter = T.StackFormatter });
end

local function CreateAuraBar(unit, filter, row)
    local c = CreateFrame("AuraContainer", nil, T.MagnetarFrame, "CustomAuraContainerTemplate");
    c:SetSize(AURA_CELL + 3, AURA_MAX * AURA_CELL);
    c:SetFrameLevel(100);
    c:SetPoint("BOTTOMLEFT", T.MagnetarFrame, "BOTTOMLEFT", 0, row);
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

T.PlayerContainer = CreateAuraBar("player", "HELPFUL|PLAYER|RAID", 22);
T.TargetContainer = CreateAuraBar("target", "HARMFUL|PLAYER", 35);
T.FocusContainer = CreateAuraBar("focus", "HARMFUL|PLAYER", 50);

ApplyScales();
