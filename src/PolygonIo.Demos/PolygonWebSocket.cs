using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PolygonIo.Demos
{
    public sealed class PolygonWebSocket : IDisposable
    {
        public ConcurrentQueue<PolygonCommand> CommandMessageQueue { get; init; } = new();

        public event Action<string> OnStatus;
        public event Action<string> OnData;

        private Uri _URI;
        private CancellationToken _CancellationToken;

        private Thread _ThreadReceive;
        private Thread _ThreadSend;

        private ClientWebSocket _ClientWebSocket = null;


        public PolygonWebSocket(Uri wssUriPolygonIO, CancellationToken token, ThreadPriority priority = ThreadPriority.AboveNormal)
        {
            this._URI = wssUriPolygonIO;
            this._CancellationToken = token;

            _ThreadReceive = new Thread(new ThreadStart(ConnectAndReceive));
            _ThreadReceive.Name = "PolygonWebSocket Receiver";
            _ThreadReceive.Priority = priority;
            _ThreadReceive.IsBackground = true;

            _ThreadSend = new Thread(new ThreadStart(SendIfConnected));
            _ThreadSend.Name = "PolygonWebSocket Sender";
            _ThreadSend.Priority = priority;
            _ThreadSend.IsBackground = true;
        }

        public void Start()
        {
            _ThreadReceive.Start();
            _ThreadSend.Start();
        }

        public void Dispose()
        {
            _ClientWebSocket?.Dispose();
        }

        private static async Task<bool> TryWebSocketConnect(ClientWebSocket wsClient, Uri uri, TimeSpan timeout, TimeSpan retryInterval, int maxRetryCount, CancellationToken ct)
        {
            var retryCount = 0;
            var timer = Stopwatch.StartNew();

            while (wsClient.State != WebSocketState.Open &&
                   ct.IsCancellationRequested == false &&
                   timer.Elapsed < timeout &&
                   retryCount < maxRetryCount)
            {
                await wsClient.ConnectAsync(uri, ct);

                switch (wsClient.State)
                {
                    case WebSocketState.Open:
                        return true;
                    case WebSocketState.None:
                    case WebSocketState.Connecting:
                        await Task.Delay(retryInterval, ct);
                        continue;
                    case WebSocketState.Closed:
                    case WebSocketState.Aborted:
                    case WebSocketState.CloseSent:
                    case WebSocketState.CloseReceived:
                    default:
                        await Task.Delay(retryInterval, ct);
                        retryCount++;
                        continue;
                }
            }
            return false;
        }

        private async void ConnectAndReceive()
        {
            while (_CancellationToken.IsCancellationRequested == false)
            {
                try
                {
                    using (var wsClient = new ClientWebSocket())
                    {
                        this._ClientWebSocket = wsClient;

                        RaiseOnStatus(nameof(PolygonWebSocket), nameof(ConnectAndReceive), "Connecting...");


                        if (await TryWebSocketConnect(wsClient, _URI, TimeSpan.FromSeconds(3), TimeSpan.FromMilliseconds(250), 5, _CancellationToken))
                        {
                            RaiseOnStatus(nameof(PolygonWebSocket), nameof(ConnectAndReceive), "Connected!");

                            await LoopReceive(wsClient, _CancellationToken);
                        }
                        else
                        {
                            RaiseOnStatus(nameof(PolygonWebSocket), nameof(ConnectAndReceive), "Failed to connect after timeout and/or retries.");

                            await Task.Delay(TimeSpan.FromSeconds(30));
                        }
                    }
                }
                catch (Exception ex)
                {
                    RaiseOnStatus(nameof(PolygonWebSocket), nameof(ConnectAndReceive), ex);

                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
            }
        }
        private async void SendIfConnected()
        {
            while (_CancellationToken.IsCancellationRequested == false)
            {
                while (_ClientWebSocket is null ||
                    _ClientWebSocket.State != WebSocketState.Open)
                {
                    Thread.Sleep(500);
                }

                try
                {
                    await LoopSend(_ClientWebSocket, _CancellationToken);
                }
                catch (Exception ex)
                {
                    RaiseOnStatus(nameof(PolygonWebSocket), nameof(SendIfConnected), ex);

                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
            }
        }

        private async Task LoopReceive(ClientWebSocket wsClient, CancellationToken cT)
        {
            Memory<byte> receiveBuffer = new byte[8192 * 4]; // 16kB
            while (cT.IsCancellationRequested == false &&
                _ClientWebSocket.State == WebSocketState.Open)
            {
                try
                {
                    Debug.Assert(wsClient.State == WebSocketState.Open);

                    var streamResult = await wsClient.ReceiveAsync(receiveBuffer, _CancellationToken);
                    Debug.Assert(streamResult.EndOfMessage, "Not End Of Message"); // TBC: one Polygon Message for one WebSocket Message
                    Debug.Assert(streamResult.Count > 0);
                    Debug.Assert(streamResult.Count < receiveBuffer.Length); // am not expecting the message to fill the buffer
                    Debug.Assert(WebSocketMessageType.Text == streamResult.MessageType);

                    var data = Encoding.UTF8.GetString(receiveBuffer.Span.Slice(0, streamResult.Count));
                    RaiseOnData(data);
                }
                catch (Exception ex)
                {
                    RaiseOnStatus(nameof(PolygonWebSocket), nameof(LoopReceive), ex);
                }
            }
        }
        private async Task LoopSend(ClientWebSocket wsClient, CancellationToken cT)
        {
            while (cT.IsCancellationRequested == false)
            {
                if (CommandMessageQueue.TryDequeue(out PolygonCommand msg))
                {
                    try
                    {
                        Memory<byte> memoryMsg = Encoding.UTF8.GetBytes(msg.ToString());
                        await wsClient.SendAsync(memoryMsg, WebSocketMessageType.Text, endOfMessage: true, cT);
                    }
                    catch (Exception ex)
                    {
                        RaiseOnStatus(nameof(PolygonWebSocket), nameof(LoopSend), ex);
                    }
                }
                else
                {
                    Thread.Yield();
                }
            }
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
        private void RaiseOnData(string data)
        {
            try
            {
                OnData?.Invoke(data);
            }
            catch { }
        }
    }
}