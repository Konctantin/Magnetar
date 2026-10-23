local addonName, T = ...;
_G[addonName] = T;

local function UpdateTextData(frame, text)
    if not frame and not frame.TextBar then
        return;
    end
    -- /dump math.floor(15.2)
    local height = frame:GetHeight();
    local width = frame:GetWidth();
    local maxBytes = math.floor(height * width * 3);
    local byteLen = text and string.len(text) or 0;
    --print(maxBytes)

    -- encode utf-8 text to bytes
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

    -- fill pixels
    local byteIndex = 1;
    local pixels = frame.TextBar;
    for y = 1, height do
        for x = 1, width do
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

local function CreatePixel(parent, x, y)
    local pixel = parent:CreateTexture(nil, "ARTWORK");
    pixel:SetSize(1, 1);
    pixel:SetPoint("TOPLEFT", parent, "TOPLEFT", x, y);
    pixel:SetColorTexture(0, 0, 0, 1);
    return pixel;
end

function T.CreateTextCell(parent, width, height)
    local frame = CreateFrame("Frame", nil, parent);
    frame:SetSize(width, height);
    frame:SetPoint("TOPLEFT", parent, "TOPLEFT", 0, 0);
    frame.TextBar = {};
    for x = 1, width do
        frame.TextBar[x] = {};
        for y = 1, height do
            -- +1 y offset
            frame.TextBar[x][y] = CreatePixel(frame, x-1, y+height-1);
        end
    end

    frame.Clear = function(self)
        if self.StoredText ~= "" then
            for x = 1, #self.TextBar do
                for y = 1, #self.TextBar[x] do
                    self.TextBar[x][y]:SetColorTexture(0, 0, 0, 1);
                end
            end
            self.StoredText = "";
        end
    end

    -- set text
    frame.SetText = function(self, text)
        if self.StoredText ~= text then
            UpdateTextData(self, text);
            self.StoredText = text;
        end
    end;

    return frame;
end