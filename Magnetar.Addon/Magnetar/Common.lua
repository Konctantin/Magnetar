local addonName, T = ...;
_G[addonName] = T;

function T.CheckInterrupt(unit, sec)
    local name, text, texture, startTime, endTime, isTradeSkill, castID, notInterruptible, spellID = UnitCastingInfo(unit);
    if name and not (notInterruptible or isTradeSkill) then
        if (((endTime / 1000) - GetTime()) - PING) <= (sec or 3) then
            return true;
        end
    end

    local name, text, texture, startTime, endTime, isTradeSkill, notInterruptible, spellID = UnitChannelInfo(unit);
    if name and not (notInterruptible or isTradeSkill) then
        return true;
    end
end