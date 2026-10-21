local addonName, T = ...;
_G[addonName] = T;

function T.CheckInterrupt(unit, sec)
    -- /dump UnitCastingInfo("target")
    local name, text, texture, startTime, endTime, isTradeSkill, castID, notInterruptible, spellID = UnitCastingInfo(unit);
    if name and not (notInterruptible or isTradeSkill) then
        if (((endTime / 1000) - GetTime()) - 0.01) <= (sec or 3) then
            return true;
        end
    end

    local name, text, texture, startTime, endTime, isTradeSkill, notInterruptible, spellID = UnitChannelInfo(unit);
    if name and not (notInterruptible or isTradeSkill) then
        return true;
    end
end

function T.InRangeCount(spellIdentifier)
    if not spellIdentifier or not C_Spell.SpellHasRange(spellIdentifier) then
        return 0;
    end

    local count = 0;
    for _, namePlate in ipairs(C_NamePlate.GetNamePlates()) do
        ---@diagnostic disable-next-line: undefined-field
        local unit = namePlate.unitToken;
        if UnitExists(unit) and UnitCanAttack("player", unit) then
            if C_Spell.IsSpellInRange(spellIdentifier, unit) then
                count = count + 1;
            end
        end
    end

    return count;
end
