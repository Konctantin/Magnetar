-- Функция для превращения Lua-таблицы в красивую JSON-подобную строку
local function TableToJSON(tbl)
    local result = "{\n";
    for name, ids in pairs(tbl) do
		result = result .. string.format('  "%s": [', name:gsub('"', '\\"'));
		for icon, spells in pairs(ids) do
		    for _, sp in ipairs(spells) do
			    
			end
		end
        result = result .. string.format('  "%s": [', name:gsub('"', '\\"'));
        local idStrings = {};
        for _, id in ipairs(ids) do
            table.insert(idStrings, id);
        end
        result = result .. table.concat(idStrings, ",") .. "],\n";
    end
    -- Убираем последнюю запятую
    if result:sub(-2, -2) == "," then result = result:sub(1, -3) .. "\n" end
    return result .. "}";
end

-- Основная функция сканирования
local function StartSpellMining()
    print("|cff00ffff[DataMiner]|r Начинаю сканирование базы заклинаний. Пожалуйста, подождите...");
    
    MAGNETAR_DB = {}; -- Сюда пишем [Имя заклинания] = {id1, id2, ...}
    
    local CURRENT_ID = 1;
    local MAX_ID = 1000000; -- Максимальный ID для сканирования (хватит для большинства классических баффов)
    local BATCH_SIZE = 5000; -- Сколько ID проверять за один кадр (чтобы игра не зависала)
    
    local function ScanBatch()
        local limit = math.min(CURRENT_ID + BATCH_SIZE, MAX_ID);
        
        for id = CURRENT_ID, limit do
			-- /dump C_Spell.GetSpellInfo(2363)
            -- Проверяем существование заклинания через современный API Midnight
            local spellInfo = C_Spell.GetSpellInfo(id);
            if spellInfo and spellInfo.name and spellInfo.name ~= "" then
                -- Фильтруем: берем только те, у которых есть реальная иконка (это отсекает технический мусор)
                if spellInfo.iconID and spellInfo.iconID > 0 then
                    if not MAGNETAR_DB[spellInfo.name] then
                        MAGNETAR_DB[spellInfo.name] = {};
                    end
					if not MAGNETAR_DB[spellInfo.name][spellInfo.iconID] then
						MAGNETAR_DB[spellInfo.name][spellInfo.iconID] = {};
					end
                    table.insert(MAGNETAR_DB[spellInfo.name][spellInfo.iconID], id);
                end
            end
        end
        
        CURRENT_ID = limit + 1;
        
        if CURRENT_ID < MAX_ID then
            -- Выводим прогресс в чат раз в 50к ID
            if CURRENT_ID % 50000 < BATCH_SIZE then
                print(string.format("Прогресс: %d%%", math.floor((CURRENT_ID / MAX_ID) * 100)));
            end
            -- Запускаем следующую порцию на следующем кадре игры
            C_Timer.After(0.01, ScanBatch);
        else
            -- Сканирование завершено! Выводим результат
            print("|cff00ffff[DataMiner]|r Сканирование успешно завершено!");
            --ShowOutputWindow(TableToJSON(MAGNETAR_DB));
        end
    end
    
    ScanBatch();
end

-- Создание текстового окна в игре для удобного копирования (Ctrl+C)
function ShowOutputWindow(textData)
    local f = CreateFrame("Frame", "MagnetarExportFrame", UIParent, "DialogBoxFrame");
    f:SetSize(600, 500);
    f:SetPoint("CENTER");
    f:SetMovable(true);
    f:EnableMouse(true);
    f:RegisterForDrag("LeftButton");
    f:SetScript("OnDragStart", f.StartMoving);
    f:SetScript("OnDragStop", f.StopMovingOrSizing);
    
    -- Скролл-контейнер
    local sf = CreateFrame("ScrollFrame", nil, f, "UIPanelScrollFrameTemplate");
    sf:SetPoint("TOPLEFT", f, "TOPLEFT", 12, -30);
    sf:SetPoint("BOTTOMRIGHT", f, "BOTTOMRIGHT", -30, 40);
    
    -- Многострочное текстовое поле
    local eb = CreateFrame("EditBox", nil, sf);
    eb:SetMultiLine(true);
    eb:SetMaxLetters(9999999);
    eb:SetFontObject("ChatFontNormal");
    eb:SetWidth(550);
    eb:SetText(textData);
    eb:HighlightText();
    eb:SetFocus();
    
    sf:SetScrollChild(eb);
    f:Show();
end

-- Регистрируем команду в чате
SLASH_MINESPELLS1 = "/minespells"
SlashCmdList["MINESPELLS"] = StartSpellMining;

