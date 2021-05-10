using System;
using System.Collections.Generic;

namespace PolygonIo.Demos
{
    public class PolygonService : IDisposable
    {
        public event Action<string> OnStatus;
        public event Action<PolygonResponseMessage> OnData;

        private PolygonWebSocket pwsClient;

        public PolygonService(PolygonWebSocket pwsClient, string apiKeyPolygonIO)
        {
            this.pwsClient = pwsClient;

            this.pwsClient.OnData += ProcessData;

            var cmdAuth = new PolygonCommand()
            {
                Action = "auth",
                Params = apiKeyPolygonIO
            };
            pwsClient.CommandMessageQueue.Enqueue(cmdAuth);
        }

        public bool IsConnected { get; private set; } = false;
        public bool IsAuthenticated { get; private set; } = false;

        private void ProcessData(string data)
        {
            PolygonResponseMessage msg = null;
            try
            {
                msg = new PolygonResponseMessage(data);
            }
            catch (Exception)
            {
            }

            if (msg.EV == "status")
            {
                if (msg.Status == "connected")
                {
                    this.IsConnected = true;
                    RaiseOnStatus(nameof(PolygonService), nameof(ProcessData), msg.Message);
                }

                if (msg.Status == "auth_success")
                {
                    this.IsAuthenticated = true;
                    RaiseOnStatus(nameof(PolygonService), nameof(ProcessData), msg.Message);
                }

                if (msg.Status == "success")
                {
                    var symbol = msg.Message.Split(':')[1].Trim();

                    if (!SymbolSubSuccess.Contains(symbol))
                        SymbolSubSuccess.Add(symbol);

                    if (SymbolsRequested.Contains(symbol))
                        SymbolsRequested.Remove(symbol);

                    RaiseOnStatus(nameof(PolygonService), nameof(ProcessData), msg.Message);
                }
            }
            else
            {
                switch (msg.EV)
                {
                    case "T":
                        RaiseOnData(msg);
                        break;
                    case "S":
                        RaiseOnData(msg);
                        break;
                }
            }
        }

        public void Dispose()
        {
            pwsClient?.Dispose();
        }

        private List<string> SymbolsRequested = new();
        private List<string> SymbolSubSuccess = new();

        public bool TrySubscribe(string symbol)
        {
            if (!IsConnected || !IsAuthenticated)
                return false;

            if (SymbolsRequested.Contains(symbol) || SymbolSubSuccess.Contains(symbol))
                return true;

            SymbolsRequested.Add(symbol);

            var cmd = new PolygonCommand();
            cmd.Action = "subscribe";
            cmd.Params = symbol;
            pwsClient.CommandMessageQueue.Enqueue(cmd);

            return true; // ToDo: make into a call-back/delegate for notification upon receiving confirmation
        }

        private void RaiseOnStatus(string typeName, string methodName, Exception ex)
        {
            RaiseOnStatus(typeName, methodName, $"Unexpected Exception! Message: {ex.Message}");
        }
        private void RaiseOnStatus(string typeName, string methodName, string msgStatus)
        {
            try
            {
                OnStatus?.Invoke($"{typeName} | {methodName} | {msgStatus}");
            }
            catch { }
        }
        private void RaiseOnData(PolygonResponseMessage data)
        {
            try
            {
                OnData?.Invoke(data);
            }
            catch { }
        }

    }
}
