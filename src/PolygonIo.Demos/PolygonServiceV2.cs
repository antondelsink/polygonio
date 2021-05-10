using System;
using System.Collections.Generic;

namespace PolygonIo.Demos
{
    public class PolygonServiceV2 : IDisposable
    {
        public event Action<string> OnStatusString;
        public event Action<PolygonResponseMessage> OnStatusMessage;
        public event Action<string> OnError;

        public event Action<PolygonResponseMessage> OnData;

        private PolygonWebSocketV2 wsClient;

        public PolygonServiceV2(PolygonWebSocketV2 pws, string apiKey)
        {
            this.wsClient = pws;

            this.wsClient.OnDataAsBytes += ProcessByteData;
            this.wsClient.OnDataAsString += ProcessData;

            EnqueueAuthCommand(this.wsClient, apiKey);
        }

        private static void EnqueueAuthCommand(PolygonWebSocketV2 pws, string apiKeyPolygonIO)
        {
            var cmdAuth = new PolygonCommand()
            {
                Action = "auth",
                Params = apiKeyPolygonIO
            };
            pws.PolygonCommandSendQueue.Enqueue(cmdAuth);
        }

        private int count = 0;
        private void ProcessByteData(ReadOnlyMemory<byte> obj)
        {
            count++;
        }

        #region Start
        public void Start()
        {
            wsClient.Start();
        }
        #endregion

        public bool IsConnected { get; private set; } = false;
        public bool IsAuthenticated { get; private set; } = false;

        private void ProcessData(string data)
        {
            if (data.Contains("},{"))
            {
                foreach (string element in data.Split("},{"))
                {
                    ProcessMessage(element.TrimStart('[', '{').TrimEnd('}', ']'));
                }
            }
            else
            {
                ProcessMessage(data.TrimStart('[', '{').TrimEnd('}', ']'));
            }
        }
        private void ProcessMessage(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return;

            PolygonResponseMessage msg = new PolygonResponseMessage(data);

            if (msg.IsStatusMessage)
            {
                if (msg.IsConnectedSuccessfullyStatusMessage)
                    this.IsConnected = true;

                if (msg.IsAuthenticatedSuccessfullyStatusMessage)
                    this.IsAuthenticated = true;

                if (msg.IsSubscribedSuccessfullyStatusMessage) // Successfully Subscribed to Symbol
                    UpdateSubscriptionStatus(msg);

                RaiseOnStatus(nameof(PolygonServiceV2), nameof(ProcessMessage), msg);
                RaiseOnStatus(nameof(PolygonServiceV2), nameof(ProcessMessage), msg.Original);
            }
            else
            {
                switch (msg.EV)
                {
                    case "T":
                        RaiseOnData(msg);
                        break;
                    case "Q":
                        RaiseOnData(msg);
                        break;
                    case "A":
                        RaiseOnData(msg);
                        break;
                }
            }
        }

        private void UpdateSubscriptionStatus(PolygonResponseMessage msg)
        {
            var symbol = msg.Message.Split(':')[1].Trim();

            if (!SymbolSubSuccess.Contains(symbol))
                SymbolSubSuccess.Add(symbol);

            if (SymbolsRequested.Contains(symbol))
                SymbolsRequested.Remove(symbol);
        }

        private bool Disposed = false;
        public void Dispose()
        {
            if (!Disposed)
            {
                Disposed = true;
                wsClient?.Dispose();
            }
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

            var cmd = new PolygonCommand()
            {
                Action = "subscribe",
                Params = symbol
            };
            wsClient.PolygonCommandSendQueue.Enqueue(cmd);

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
                OnStatusString?.Invoke($"{typeName} | {methodName} | {msgStatus}");
            }
            catch { }
        }
        private void RaiseOnStatus(string methodName, PolygonResponseMessage status)
        {
            RaiseOnStatus(nameof(PolygonServiceV2), methodName, status);
        }
        private void RaiseOnStatus(string typeName, string methodName, PolygonResponseMessage status)
        {
            try
            {
                OnStatusMessage?.Invoke(status);
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
        private void RaiseOnError(string error)
        {
            try
            {
                OnError?.Invoke(error);
            }
            catch { }
        }
    }
}
