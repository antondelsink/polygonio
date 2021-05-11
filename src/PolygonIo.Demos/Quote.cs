using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PolygonIo.Demos
{
    /// <summary>
    /// Stock Quote from <a href="https://polygon.io/docs/websockets/ws_stocks_Q_anchor">polygon.io</a>.
    /// </summary>
    public struct Quote
    {
        [JsonPropertyName("sym")] public string Symbol { get; set; }
        [JsonPropertyName("ax")] public uint AskExchangeId { get; set; }
        [JsonPropertyName("as")] public uint AskSize { get; set; }
        [JsonPropertyName("ap")] public float AskPrice { get; set; }
        [JsonPropertyName("bx")] public uint BidExchangeId { get; set; }
        [JsonPropertyName("bs")] public uint BidSize { get; set; }
        [JsonPropertyName("bp")] public float BidPrice { get; set; }
        [JsonPropertyName("t")] public ulong UnixTimestamp { get; set; }
        [JsonPropertyName("c")] public uint Condition { get; set; }
        [JsonPropertyName("z")] public uint Tape { get; set; }

        public DateTimeOffset Timestamp => DateTimeOffset.UnixEpoch.AddMilliseconds(UnixTimestamp);

        public static bool TryParse(ReadOnlySpan<byte> json, out Quote q)
        {
            try
            {
                var reader = new Utf8JsonReader(json);
                q = JsonSerializer.Deserialize<Quote>(ref reader);
                return true;
            }
            catch
            {
                q = new Quote();
                return false;
            }
        }
        public static Quote Parse(ReadOnlySpan<byte> json)
        {
            var reader = new Utf8JsonReader(json);
            return JsonSerializer.Deserialize<Quote>(ref reader);
        }
    }
}