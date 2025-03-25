obs = obslua
local bit = require("bit")

-- スクリプトの説明
function script_description()
    return
        "8バイトビッグエンディアンのバイト列を52ビット精度のdoubleに変換し、テキストソースに表示します。"
end
-- 設定可能なプロパティ
function script_properties()
    local props = obs.obs_properties_create()
    obs.obs_properties_add_text(props, "text_source",
                                "テキストソース名", obs.OBS_TEXT_DEFAULT)
    return props
end

-- バイト列を定義
local bytes = {
    {0xF6, 0x07, 0x5B, 0xCD, 0x15, 0x00, 0x00, 0x00},    
    {0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}, -- -2^63
    {0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF}, -- 2^63 - 1
    {0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xFF}, -- 大きな正数
    {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF}, -- -1
    {0x00, 0x00, 0x00, 0x00, 0x7F, 0xFF, 0xFF, 0xFF}, -- 32bit
    {0xFF, 0xFF, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x00},
    {0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00}, -- 32bit+1
    {0xFF, 0xFF, 0xFF, 0xFF, 0x7F, 0xFF, 0xFF, 0xFF},
    {0x00, 0x00, 0x00, 0x7F, 0xFF, 0xFF, 0xFF, 0xFF}, -- 40bit
    {0xFF, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x00, 0x00},
    {0x00, 0x00, 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF}, -- 48bit
    {0xFF, 0xFF, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00},
    {0x00, 0x07, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF}, -- 52bit
    {0xFF, 0xF8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00},
    {0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}, -- 52bit+1
    {0xFF, 0xF7, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF},
    {0x00, 0x1F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF}, -- 56bit
    {0xFF, 0xE0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}
}

function bigendian_int64(bytes)
    local ffi = require("ffi")

    -- ビッグエンディアンでint64に変換
    local int64 = ffi.new("int64_t")
    for i = 0, 7 do int64 = bit.bor(bit.lshift(int64, 8), bytes[i + 1]) end

    -- 結果を表示
    return tostring(int64) .. "\n"
end

function int64test()
    local ffi = require("ffi")

    ffi.cdef [[
    typedef long long int64_t;
]]

    local a = ffi.new("int64_t", -987654321098765)
    local b = ffi.new("int64_t", 987654321098765)

    -- 加算
    local sum = ffi.new("int64_t", a + b)
    obs.script_log(obs.LOG_INFO, "a+b:" .. tostring(sum))

    -- 減算
    local diff = ffi.new("int64_t", a - b)
    obs.script_log(obs.LOG_INFO, "a+b:" .. tostring(diff))

    -- 比較 (==, <, >, >=, <=)
    obs.script_log(obs.LOG_INFO, "a == b:" .. tostring(a == b))
    obs.script_log(obs.LOG_INFO, "a < b:" .. tostring(a < b))
    obs.script_log(obs.LOG_INFO, "a > b:" .. tostring(a > b))
    obs.script_log(obs.LOG_INFO, "a >= b:" .. tostring(a >= b))
    obs.script_log(obs.LOG_INFO, "a <= b:" .. tostring(a <= b))

    -- C の int64_t に対して直接演算を行うため、Lua の number 型に変換する際は tonumber() を使用
    -- オーバーフローには注意。C の int64_t の範囲を超える場合、結果は未定義。
    return
        tostring(a) .. " + " .. tostring(b) .. " = " .. tostring(sum) .. "\n" ..
            tostring(a) .. " - " .. tostring(b) .. " = " .. tostring(diff) ..
            "\n"
end

-- 32ビット内でnを加算するビット演算
local function bitAdd32(x, n)
    local sum = x
    local carry = n
    while carry ~= 0 do
        local newSum = bit.bxor(sum, carry)
        carry = bit.lshift(bit.band(sum, carry), 1)
        sum = newSum
        -- 32ビットに制限
        sum = bit.band(sum, 0xFFFFFFFF)
        carry = bit.band(carry, 0xFFFFFFFF)
    end
    return sum
end

-- 64ビット値にnを加算（32ビット分割）
local function bitAdd64(high, low, n)
    -- nを32ビット部分に分割（仮にnは下位32ビットに収まると仮定）
    local nHigh = 0
    local nLow = bit.band(n, 0xFFFFFFFF)

    -- 下位32ビットに加算
    local newLow = bitAdd32(low, nLow)
    local carry = (newLow < low) and 1 or 0 -- オーバーフローでキャリー判定

    -- 上位32ビットに加算（キャリーを考慮）
    local newHigh = bitAdd32(high, nHigh)
    if carry == 1 then newHigh = bitAdd32(newHigh, 1) end

    return newHigh, newLow
end

-- 32ビット内でnを乗算
local function bitMul32(x, n)
    local result = 0
    local tempN = n
    local shift = 0
    while tempN ~= 0 do
        if bit.band(tempN, 1) == 1 then
            result = bitAdd32(result, bit.lshift(x, shift))
        end
        tempN = bit.rshift(tempN, 1)
        shift = shift + 1
    end
    return bit.band(result, 0xFFFFFFFF)
end

-- https://grok.com/chat/c5369ed9-4ff1-4ccb-9ff9-d87fa293bc18
-- 8バイトビッグエンディアンを52ビット精度のdoubleに変換,luagitの52bit精度問題の52bitか
function bytesToDouble52BitBigEndian(byteStr)
    if #byteStr ~= 8 then error("Input must be 8 bytes long") end

    -- バイトを取得
    local b1, b2, b3, b4, b5, b6, b7, b8 = byteStr:byte(1, 8)

    -- 符号判定
    local value
    if b1 >= 0x80 then
        -- 負の値の場合、各バイトを8ビットでNOT（ビット反転）
        local n1 = bit.band(bit.bnot(b1), 0xFF)
        local n2 = bit.band(bit.bnot(b2), 0xFF)
        local n3 = bit.band(bit.bnot(b3), 0xFF)
        local n4 = bit.band(bit.bnot(b4), 0xFF)
        local n5 = bit.band(bit.bnot(b5), 0xFF)
        local n6 = bit.band(bit.bnot(b6), 0xFF)
        local n7 = bit.band(bit.bnot(b7), 0xFF)
        local n8 = bit.band(bit.bnot(b8), 0xFF)

        -- 32ビットずつ分割
        local high = bit.bor(bit.lshift(n1, 24), bit.lshift(n2, 16),
                             bit.lshift(n3, 8), n4)
        local low = bit.bor(bit.lshift(n5, 24), bit.lshift(n6, 16),
                            bit.lshift(n7, 8), n8)

        -- 64ビット値を構築し、最後に+1
        value = high * 2 ^ 32 + low + 1
        if (low < 0) then value = value + 2 ^ 32 end
        value = value * -1

    else
        -- 正の値の場合、そのまま処理
        local high = bit.bor(bit.lshift(b1, 24), bit.lshift(b2, 16),
                             bit.lshift(b3, 8), b4)
        local low = bit.bor(bit.lshift(b5, 24), bit.lshift(b6, 16),
                            bit.lshift(b7, 8), b8)
        value = high * 2 ^ 32 + low
        if (low < 0) then value = value + 2 ^ 32 end
    end


    -- 52ビット精度に制限
    local max52Bit = 2 ^ 53 - 1 -- 9,007,199,254,740,991
    local min52Bit = -2 ^ 53 -- -9,007,199,254,740,992
    if value > max52Bit then
        -- value = max52Bit
        obs.script_log(obs.LOG_INFO, value ..
                           "がmaxこえてます 値が9,007,199,254,740,991,53bit以上の場合精度は保証されません")
    elseif value < min52Bit then
        -- value = min52Bit
        obs.script_log(obs.LOG_INFO, value ..
                           "minをこえてます 値が-9,007,199,254,740,992,53bit以下精度は保証されません")
    end

    return value
end

-- テストデータ
local tests = {
    string.char(0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00), -- -2^63
    string.char(0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF), -- 2^63 - 1
    string.char(0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xFF), -- 大きな正数
    string.char(0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF), -- -1
    string.char(0x00, 0x00, 0x00, 0x00, 0x7F, 0xFF, 0xFF, 0xFF), -- 32bit
    string.char(0xFF, 0xFF, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x00),
    string.char(0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00), -- 32bit+1
    string.char(0xFF, 0xFF, 0xFF, 0xFF, 0x7F, 0xFF, 0xFF, 0xFF),
    string.char(0x00, 0x00, 0x00, 0x7F, 0xFF, 0xFF, 0xFF, 0xFF), -- 40bit
    string.char(0xFF, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x00, 0x00),
    string.char(0x00, 0x00, 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF), -- 48bit
    string.char(0xFF, 0xFF, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00),
    string.char(0x00, 0x07, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF), -- 52bit
    string.char(0xFF, 0xF8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00),
    string.char(0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00), -- 52bit+1
    string.char(0xFF, 0xF7, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF),
    string.char(0x00, 0x1F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF), -- 56bit
    string.char(0xFF, 0xE0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00)
}

-- テキストソースを更新する関数
local function update_text_source()
    local source_name = obs.obs_data_get_string(settings, "text_source")
    local source = obs.obs_get_source_by_name(source_name)
    if source then
        local text = ""
        for i, bytes in ipairs(tests) do
            local result = bytesToDouble52BitBigEndian(bytes)
            text = text .. string.format("Test %d: %.0f\n", i, result)
        end
        text = text .. int64test()
        for i, byte in ipairs(bytes) do
            text = text .. bigendian_int64(byte)
        end

        local settings = obs.obs_data_create()
        obs.obs_data_set_string(settings, "text", text)
        obs.obs_source_update(source, settings)
        obs.obs_data_release(settings)
        obs.obs_source_release(source)
    end
end

-- スクリプトの初期化
function script_load(settings_)
    settings = settings_
    obs.script_log(obs.LOG_INFO, "スクリプトがロードされました")
    obs.timer_add(update_text_source, 100000) -- 1秒ごとに更新
end

-- スクリプト設定の更新時
function script_update(settings_)
    settings = settings_
    update_text_source()
end

-- スクリプトのアンロード
function script_unload()
    obs.script_log(obs.LOG_INFO,
                   "スクリプトがアンロードされました")
end
