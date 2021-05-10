using System;
using System.Buffers;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PolygonIo.WebSocket.Contracts;
using System.Net.WebSockets;

namespace PolygonIo.WebSocket
{
    internal static class ClientWebSocketExtensions
    {
        enum SubscriptionType { AggregatePerSecond, AggregatePerMinute, Trade, Quote };
        const int SubsciptionChunkSize = 1000;
        const int SendChunkSize = 2048;

        public static async Task SendAuthentication(this ClientWebSocket websocket, string apiKey, CancellationToken cancellationToken)
        {
            await websocket.SendAsChunksAsync("{\"action\":\"auth\",\"params\":\"" + apiKey + "\"}", SendChunkSize, cancellationToken);
        }

        public static async Task SubscribeToAggregatePerSecond(this ClientWebSocket websocket, IEnumerable<string> symbols, CancellationToken cancellationToken)
        {
            await Subscribe(websocket, symbols, SubscriptionType.AggregatePerSecond, cancellationToken);
        }

        public static async Task SubscribeToAggregatePerMinute(this ClientWebSocket websocket, IEnumerable<string> symbols, CancellationToken cancellationToken)
        {
            await Subscribe(websocket, symbols, SubscriptionType.AggregatePerMinute, cancellationToken);
        }

        public static async Task SubscribeToTrades(this ClientWebSocket websocket, IEnumerable<string> symbols,  CancellationToken cancellationToken)
        {
            await Subscribe(websocket, symbols, SubscriptionType.Trade, cancellationToken);
        }

        public static async Task SubscribeToQuotes(this ClientWebSocket websocket, IEnumerable<string> symbols, CancellationToken cancellationToken)
        {
            await Subscribe(websocket, symbols, SubscriptionType.Quote, cancellationToken);
        }

        static private async Task Subscribe(this ClientWebSocket websocket, IEnumerable<string> symbols, SubscriptionType subscriptionType, CancellationToken cancellationToken)
        {          
            var chunks = symbols
                            .Select((s, i) => new { Value = s, Index = i })
                            .GroupBy(x => x.Index / SubsciptionChunkSize)
                            .Select(grp => grp.Select(x => x.Value));

            foreach (var chunk in chunks)
            {
                if (cancellationToken.IsCancellationRequested == true)
                    break;

                var subscriptionString = GetSubscriptionString(chunk.ToArray(), subscriptionType);

                await websocket.SendAsChunksAsync("{\"action\":\"subscribe\",\"params\":\"" + subscriptionString + "\"}", SendChunkSize, cancellationToken);
            }
        }

        static string GetSubscriptionString(string[] symbols, SubscriptionType subscriptionType)
        {
            var sb = new StringBuilder(GetParam(symbols[0], subscriptionType));

            if (symbols.Length == 1)
                return sb.ToString();

            for (int i = 1; i < symbols.Length; i++)
            {
                sb.Append(",");
                sb.Append(GetParam(symbols[i], subscriptionType));
            }

            return sb.ToString();
        }

        static string GetParam(string symbol, SubscriptionType subscriptionType)
        {
            return subscriptionType switch
            {
                SubscriptionType.AggregatePerSecond => StreamFieldNames.AggregatePerSecond + "." + symbol,
                SubscriptionType.AggregatePerMinute => StreamFieldNames.AggregatePerMinute + "." + symbol,
                SubscriptionType.Quote => StreamFieldNames.Quote + "." + symbol,
                SubscriptionType.Trade => StreamFieldNames.Trade + "." + symbol,
                _ => throw new ArgumentException(nameof(subscriptionType)),
            };
        }
        public static async Task SendAsChunksAsync(this ClientWebSocket webSocket, string message, int sendChunkSize, CancellationToken cancellationToken)
        {
            await webSocket.SendAsChunksAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, sendChunkSize, cancellationToken);
        }

        public static async Task SendAsChunksAsync(this ClientWebSocket webSocket, byte[] data, WebSocketMessageType type, int sendChunkSize, CancellationToken cancellationToken)
        {
            var messagesCount = (int)Math.Ceiling((double)data.Length / sendChunkSize);

            for (var i = 0; i < messagesCount; i++)
            {
                var offset = (sendChunkSize * i);
                var count = sendChunkSize;
                var lastMessage = ((i + 1) == messagesCount);

                if ((count * (i + 1)) > data.Length)
                {
                    count = data.Length - offset;
                }

                await webSocket
                        .SendAsync(new ArraySegment<byte>(data, offset, count), type, lastMessage, cancellationToken);
            }
        }
    }
}