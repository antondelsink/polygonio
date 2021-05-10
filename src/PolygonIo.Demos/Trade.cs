using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;

namespace PolygonIo.Demos
{
    /// <summary>
    /// Stock Trade from <a href="https://polygon.io/docs/websockets/ws_stocks_T_anchor">polygon.io</a>
    /// </summary>
    public struct Trade
    {
        public string Symbol;
        public string TradeId;
        public uint ExchangeId;
        public float Price;
        public uint Size;
        public uint[] Conditions;
        public DateTimeOffset Timestamp;
        public uint Tape;

        public static bool TryParse(ReadOnlySpan<byte> json, out Trade t)
        {
            t = new Trade();

            var reader = new Utf8JsonReader(json);

            if (!reader.Read())
                return false;

            if (JsonTokenType.StartObject != reader.TokenType)
                return false;

            if (!reader.Read())
                return false;

            if (JsonTokenType.PropertyName != reader.TokenType)
                return false;

            if (!reader.ValueTextEquals("ev"))
                return false;

            if (!reader.Read())
                return false;

            if (JsonTokenType.String != reader.TokenType)
                return false;

            if (reader.ValueSpan[0] != (byte)'T')
                return false;

            return Trade.TryPartialParse(ref reader, out t);
        }

        private const string jsonPropertyName_Symbol = "sym";
        private const string jsonPropertyName_Condition = "c";
        private const string jsonPropertyName_TradeID = "i";
        private const string jsonPropertyName_ExchangeID = "x";
        private const string jsonPropertyName_Price = "p";
        private const string jsonPropertyName_Size = "s";
        private const string jsonPropertyName_Timestamp = "t";
        private const string jsonPropertyName_Tape = "z";
        private static readonly string[] jsonPropertyNames = new string[] { jsonPropertyName_Symbol, jsonPropertyName_TradeID, jsonPropertyName_ExchangeID, jsonPropertyName_Price, jsonPropertyName_Size, jsonPropertyName_Condition, jsonPropertyName_Timestamp, jsonPropertyName_Tape };
        private static readonly int ixSymbol = Array.IndexOf(jsonPropertyNames, jsonPropertyName_Symbol);
        private static readonly int ixCondition = Array.IndexOf(jsonPropertyNames, jsonPropertyName_Condition);
        private static readonly int ixTradeID = Array.IndexOf(jsonPropertyNames, jsonPropertyName_TradeID);
        private static readonly int ixExchangeID = Array.IndexOf(jsonPropertyNames, jsonPropertyName_ExchangeID);
        private static readonly int ixPrice = Array.IndexOf(jsonPropertyNames, jsonPropertyName_Price);
        private static readonly int ixSize = Array.IndexOf(jsonPropertyNames, jsonPropertyName_Size);
        private static readonly int ixTimestamp = Array.IndexOf(jsonPropertyNames, jsonPropertyName_Timestamp);
        private static readonly int ixTape = Array.IndexOf(jsonPropertyNames, jsonPropertyName_Tape);

        /// <summary>
        /// Parsing partial JSON object only from 2nd property onwards (caller already consumed StartObject and "ev":"Trade")
        /// </summary>
        public static bool TryPartialParse(ref Utf8JsonReader reader, out Trade t)
        {
            Span<bool> readStatus = stackalloc bool[jsonPropertyNames.Length]; // choosing readability of code using 8 bytes for stackalloc vs. 4 bytes for BitVector32
            readStatus[ixCondition] = true; // Conditions aka "c" are optional for Trade messages

            t = new Trade();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject && Utf8JsonReaderUtils.AllRequiredFieldsAssigned(readStatus))
                    return true;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    return false;

                var key = reader.GetString();

                if (!jsonPropertyNames.Contains(key))
                    return false;

                switch (key)
                {
                    case "sym":
                        bool readSuccess_Symbol = readStatus[ixSymbol] = Utf8JsonReaderUtils.TryGetString(ref reader, out t.Symbol);
                        if (!readSuccess_Symbol)
                            return false;
                        break;
                    case "i":
                        bool readSuccess_TradeID = readStatus[ixTradeID] = Utf8JsonReaderUtils.TryGetString(ref reader, out string i);
                        if (!readSuccess_TradeID)
                            return false;
                        break;
                    case "x":
                        bool readSuccess_ExchangeID = readStatus[ixExchangeID] = Utf8JsonReaderUtils.TryGetUInt32(ref reader, ref t.ExchangeId);
                        if (!readSuccess_ExchangeID)
                            return false;
                        break;
                    case "p":
                        bool readSuccess_Price = readStatus[ixPrice] = Utf8JsonReaderUtils.TryGetFloat(ref reader, ref t.Price);
                        if (!readSuccess_Price)
                            return false;
                        break;
                    case "s":
                        bool readSuccess_Size = readStatus[ixSize] = Utf8JsonReaderUtils.TryGetUInt32(ref reader, ref t.Size);
                        if (!readSuccess_Size)
                            return false;
                        break;
                    case "c":
                        reader.Read();
                        switch (reader.TokenType)
                        {
                            case JsonTokenType.StartArray:
                                reader.Skip();
                                break;
                            case JsonTokenType.Number:
                                Debugger.Break();
                                break;
                            default:
                                Debugger.Break();
                                break;
                        }
                        readStatus[ixCondition] = true;
                        break;
                    case "t":
                        ulong time = 0;
                        if (!Utf8JsonReaderUtils.TryGetUInt64(ref reader, ref time))
                            return false;
                        t.Timestamp = DateTimeOffset.UnixEpoch.AddMilliseconds(time);
                        readStatus[ixTimestamp] = true;
                        break;
                    case "z":
                        bool readSuccess_Tape = readStatus[ixTape] = Utf8JsonReaderUtils.TryGetUInt32(ref reader, ref t.Tape);
                        if (!readSuccess_Tape)
                            return false;
                        break;
                    default:
                        return false;
                }
            }
            return false;
        }

        public override string ToString()
        {
            return $"Trade {Symbol} for {Price:C2}";
        }
    }
    public struct Trade_JsonDeserializeObject
    {
        [JsonProperty("sym")]
        public string Symbol;

        [JsonProperty("i")]
        public ulong TradeID;

        [JsonProperty("x")]
        public uint ExchangeID;

        [JsonProperty("p")]
        public float Price;

        [JsonProperty("s")]
        public uint Size;

        [JsonProperty("c")]
        public uint[] Conditions;

        [JsonProperty("t")]
        public ulong Timestamp;

        [JsonProperty("z")]
        public uint Tape;
    }
}