using System;
using System.Collections.Generic;
using System.Text;

namespace PolygonIo.Demos
{
    internal class PolygonJsonServiceMockFromStrings : IPolygonJsonService
    {
        ReadOnlyMemory<byte> emptyStringBytes = Encoding.UTF8.GetBytes(string.Empty).AsMemory();

        public event Action<ReadOnlyMemory<byte>> OnJSON;

        bool isAuthenticated = false;
        public void Authenticate(string apiKey)
        {
            isAuthenticated = true;

            var msgConnected = @"{""ev"":""status"",""status"":""connected"",""message"":""Connected Successfully""}";
            var msgAuthenticated = @"{""ev"":""status"",""status"":""auth_success"",""message"":""authenticated""}";

            OnJSON?.Invoke(Encoding.UTF8.GetBytes(msgConnected).AsMemory());
            OnJSON?.Invoke(Encoding.UTF8.GetBytes(msgAuthenticated).AsMemory());
        }

        List<string> subscriptions = new();
        public void Subscribe(string symbol)
        {
            subscriptions.Add(symbol);

            var msgSubscribed = @"{""ev"":""status"",""status"":""success"",""message"":""subscribed to: "+ symbol + @"""}";

            OnJSON?.Invoke(Encoding.UTF8.GetBytes(msgSubscribed).AsMemory());
        }
    }
}