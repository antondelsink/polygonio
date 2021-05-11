using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PolygonIo.Demos
{
    /// <summary>
    /// Stock Trade from <a href="https://polygon.io/docs/websockets/ws_stocks_T_anchor">polygon.io</a>
    /// </summary>
    public struct Trade
    {
        [JsonPropertyName("sym")] public string Symbol { get; set; }
        [JsonPropertyName("i")] public string TradeId { get; set; }
        [JsonPropertyName("x")] public uint ExchangeId { get; set; }
        [JsonPropertyName("p")] public float Price { get; set; }
        [JsonPropertyName("s")] public uint Size { get; set; }
        [JsonPropertyName("c")] public uint[] Conditions { get; set; }
        [JsonPropertyName("t")] public ulong UnixTimestamp { get; set; }
        [JsonPropertyName("z")] public uint Tape { get; set; }

        public DateTimeOffset Timestamp => DateTimeOffset.UnixEpoch.AddMilliseconds(UnixTimestamp);

        public static bool TryParse(ReadOnlySpan<byte> json, out Trade t)
        {
            try
            {
                var reader = new Utf8JsonReader(json);
                t = JsonSerializer.Deserialize<Trade>(ref reader);
                return true;
            }
            catch
            {
                t = new Trade();
                return false;
            }
        }
        public static Trade Parse(ReadOnlySpan<byte> json)
        {
            var reader = new Utf8JsonReader(json);
            return JsonSerializer.Deserialize<Trade>(ref reader);
        }
        public override string ToString()
        {
            return $"Trade {Symbol} for {Price:C2}";
        }
    }
}