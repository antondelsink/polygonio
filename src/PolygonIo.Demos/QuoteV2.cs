using System;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace PolygonIo.Demos
{
    [StructLayout(LayoutKind.Sequential)]
    public struct QuoteV2
    {
        [JsonPropertyName("sym")] public StringStructMax12 Symbol { get; set; }
        [JsonPropertyName("ax")] public uint AskExchangeId { get; set; }
        [JsonPropertyName("as")] public uint AskSize { get; set; }
        [JsonPropertyName("ap")] public float AskPrice { get; set; }
        [JsonPropertyName("bx")] public uint BidExchangeId { get; set; }
        [JsonPropertyName("bs")] public uint BidSize { get; set; }
        [JsonPropertyName("bp")] public float BidPrice { get; set; }
        [JsonPropertyName("t")] public ulong UnixTimestamp { get; set; }
        [JsonPropertyName("c")] public uint Condition { get; set; }
        [JsonPropertyName("z")] public uint Tape { get; set; }

        private uint Reserved01;
        private uint Reserved02;
    }
}
