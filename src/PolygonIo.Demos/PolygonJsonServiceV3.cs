using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace PolygonIo.Demos
{
    internal class PolygonJsonServiceV3 : IPolygonJsonService
    {
        public event Action<ReadOnlyMemory<byte>> OnJSON;

        private bool ConnectionMessageReceived = false;
        public bool IsConnected => wsPolygon.IsConnected && ConnectionMessageReceived;
        public bool IsAuthenticated { get; private set; } = false;

        IPolygonWebSocket wsPolygon;

        public PolygonJsonServiceV3(IPolygonWebSocket wsPolygon)
        {
            this.wsPolygon = wsPolygon;
            this.wsPolygon.OnMessage += ProcessWebSocketMessage;
        }

        private void ProcessWebSocketMessage(ReadOnlyMemory<byte> data)
        {
            var reader = new Utf8JsonReader(data.Span);
            
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.StartObject)
                        {
                            long ixJsonObjectStart = reader.TokenStartIndex;
                            reader.Skip(); // move to EndObject
                            long ixJsonObjectEnd = reader.TokenStartIndex;
                            long lengthJsonObject = ixJsonObjectEnd - ixJsonObjectStart + 1;

                            var jsonObject = data.Slice((int)ixJsonObjectStart, (int)lengthJsonObject);

                            switch (jsonObject.Span[7])
                            {
                                case (byte)'Q':
                                case (byte)'T':
                                case (byte)'A':
                                    OnJSON?.Invoke(jsonObject);
                                    break;
                                case (byte)'s':
                                    ProcessStatus(jsonObject);
                                    break;
                            }
                        }
                    }
                }
            }
        }

        private void ProcessStatus(ReadOnlyMemory<byte> data)
        {
            var msg = Encoding.UTF8.GetString(data.Span);

            if (msg.Contains("subscribed to"))
            {
                ProcessSubscriptionStatus(msg);
            }

            if (msg.Contains("auth_success"))
            {
                IsAuthenticated = true;
                return;
            }

            if (msg.Contains("Connected Successfully"))
            {
                ConnectionMessageReceived = true;
                return;
            }
        }

        public void Authenticate(string apiKey)
        {
            var cmdAuth = new PolygonCommand() { Action = "auth", Params = apiKey };
            wsPolygon.Send(cmdAuth.ToString());
        }

        List<string> subscriptionsConfirmed = new();
        private void ProcessSubscriptionStatus(string msg)
        {
            var txt = "subscribed to: ";
            var ix = msg.IndexOf(txt) + txt.Length;
            var len = msg.IndexOf('\"', ix) - ix;
            var symbol = msg.Substring(ix, len);
            subscriptionsConfirmed.Add(symbol);
            subscriptionsRequested.Remove(symbol);
        }

        List<string> subscriptionsRequested = new();
        public void Subscribe(string symbol)
        {
            var cmdSub = new PolygonCommand() { Action = "subscribe", Params = symbol };
            wsPolygon.Send(cmdSub.ToString());
            subscriptionsRequested.Add(symbol);
        }

        public bool IsSubscribed(string symbol)
        {
            return subscriptionsConfirmed.Contains(symbol);
        }
    }
}