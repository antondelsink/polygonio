using System;
using System.Linq;
using System.Text.Json;
using Newtonsoft.Json;

namespace PolygonIo.Demos
{
    /// <summary>
    /// Stock Quote from <a href="https://polygon.io/docs/websockets/ws_stocks_Q_anchor">polygon.io</a>.
    /// </summary>
    public struct Quote
    {
        public string Symbol;

        public uint AskExchangeId;
        public uint AskSize;
        public float AskPrice;

        public uint BidExchangeId;
        public uint BidSize;
        public float BidPrice;

        public DateTimeOffset Timestamp;
        public uint Condition;
        public uint Tape;

        /// <summary>
        /// Produces a Quote variable with all available fields populated using Utf8JsonReader.
        /// </summary>
        /// <param name="json">JSON Object wrapped in '{' and '}'. Does not accept array(s) and will return false if wrapped with '[' and/or ']'.</param>
        /// <returns>true if successfully obtained values for all required fields of a Quote.</returns>
        public static bool TryParse(ReadOnlySpan<byte> json, out Quote q)
        {
            q = new Quote();

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

            if (reader.ValueSpan[0] != (byte)'Q')
                return false;

            return Quote.TryParseQuote(ref reader, out q);
        }

        private const string jsonPropertyName_Symbol = "sym";
        private const string jsonPropertyName_Condition = "c";
        private const string jsonPropertyName_BidExchangeID = "bx";
        private const string jsonPropertyName_AskExchangeID = "ax";
        private const string jsonPropertyName_BidPrice = "bp";
        private const string jsonPropertyName_AskPrice = "ap";
        private const string jsonPropertyName_BidSize = "bs";
        private const string jsonPropertyName_AskSize = "as";
        private const string jsonPropertyName_Timestamp = "t";
        private const string jsonPropertyName_Tape = "z";
        private static readonly string[] jsonPropertyNames = new string[] { jsonPropertyName_Symbol, jsonPropertyName_Condition, jsonPropertyName_BidExchangeID, jsonPropertyName_AskExchangeID, jsonPropertyName_BidPrice, jsonPropertyName_AskPrice, jsonPropertyName_BidSize, jsonPropertyName_AskSize, jsonPropertyName_Timestamp, jsonPropertyName_Tape };
        private static readonly int ixSymbol = Array.IndexOf(jsonPropertyNames, jsonPropertyName_Symbol);
        private static readonly int ixCondition = Array.IndexOf(jsonPropertyNames, jsonPropertyName_Condition);
        private static readonly int ixBidExchangeID = Array.IndexOf(jsonPropertyNames, jsonPropertyName_BidExchangeID);
        private static readonly int ixAskExchangeID = Array.IndexOf(jsonPropertyNames, jsonPropertyName_AskExchangeID);
        private static readonly int ixBidPrice = Array.IndexOf(jsonPropertyNames, jsonPropertyName_BidPrice);
        private static readonly int ixAskPrice = Array.IndexOf(jsonPropertyNames, jsonPropertyName_AskPrice);
        private static readonly int ixBidSize = Array.IndexOf(jsonPropertyNames, jsonPropertyName_BidSize);
        private static readonly int ixAskSize = Array.IndexOf(jsonPropertyNames, jsonPropertyName_AskSize);
        private static readonly int ixTimestamp = Array.IndexOf(jsonPropertyNames, jsonPropertyName_Timestamp);
        private static readonly int ixTape = Array.IndexOf(jsonPropertyNames, jsonPropertyName_Tape);

        /// <summary>
        /// Parsing partial JSON object only from 2nd property onwards (caller already consumed StartObject and "ev":"Quote")
        /// </summary>
        private static bool TryParseQuote(ref Utf8JsonReader reader, out Quote q)
        {
            Span<bool> readStatus = stackalloc bool[jsonPropertyNames.Length];
            readStatus[ixCondition] = true; // Condition is often omitted for Quotes

            q = new Quote();

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
                        bool readSuccess_Symbol = readStatus[ixSymbol] = Utf8JsonReaderUtils.TryGetString(ref reader, out q.Symbol);
                        if (!readSuccess_Symbol)
                            return false;
                        break;
                    case "c":
                        bool readSuccess_Condition = readStatus[ixCondition] = Utf8JsonReaderUtils.TryGetUInt32(ref reader, ref q.Condition);
                        if (!readSuccess_Condition)
                            return false;
                        break;
                    case "bx":
                        bool readSuccess_BidExchangeID = readStatus[ixBidExchangeID] = Utf8JsonReaderUtils.TryGetUInt32(ref reader, ref q.BidExchangeId);
                        if (!readSuccess_BidExchangeID)
                            return false;
                        break;
                    case "ax":
                        bool readSuccess_AskExchangeID = readStatus[ixAskExchangeID] = Utf8JsonReaderUtils.TryGetUInt32(ref reader, ref q.AskExchangeId);
                        if (!readSuccess_AskExchangeID)
                            return false;
                        break;
                    case "bp":
                        bool readSuccess_BidPrice = readStatus[ixBidPrice] = Utf8JsonReaderUtils.TryGetFloat(ref reader, ref q.BidPrice);
                        if (!readSuccess_BidPrice)
                            return false;
                        break;
                    case "ap":
                        bool readSuccess_AskPrice = readStatus[ixAskPrice] = Utf8JsonReaderUtils.TryGetFloat(ref reader, ref q.AskPrice);
                        if (!readSuccess_AskPrice)
                            return false;
                        break;
                    case "bs":
                        bool readSuccess_BidSize = readStatus[ixBidSize] = Utf8JsonReaderUtils.TryGetUInt32(ref reader, ref q.BidSize);
                        if (!readSuccess_BidSize)
                            return false;
                        break;
                    case "as":
                        bool readSuccess_AskSize = readStatus[ixAskSize] = Utf8JsonReaderUtils.TryGetUInt32(ref reader, ref q.AskSize);
                        if (!readSuccess_AskSize)
                            return false;
                        break;
                    case "t":
                        ulong time = 0;
                        if (!Utf8JsonReaderUtils.TryGetUInt64(ref reader, ref time))
                            return false;
                        q.Timestamp = DateTimeOffset.UnixEpoch.AddMilliseconds(time);
                        readStatus[ixTimestamp] = true;
                        break;
                    case "z":
                        bool readSuccess_Tape = readStatus[ixTape] = Utf8JsonReaderUtils.TryGetUInt32(ref reader, ref q.Tape);
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
            return $"Quote {Symbol} Bid Price: {BidPrice:C2}, Ask Price: {AskPrice:C2}.";
        }
    }
    public struct Quote_JsonDeserializeObject
    {
        [JsonProperty("sym")]
        public string Symbol;
        [JsonProperty("c")]
        public uint Conditions;
        [JsonProperty("bx")]
        public uint BidExchangeId;
        [JsonProperty("ax")]
        public uint AskExchangeId;
        [JsonProperty("bp")]
        public float BidPrice;
        [JsonProperty("ap")]
        public float AskPrice;
        [JsonProperty("as")]
        public uint AskSize;
        [JsonProperty("bs")]
        public uint BidSize;
        [JsonProperty("t")]
        public ulong Timestamp;
        [JsonProperty("z")]
        public uint Tape;
    }
}