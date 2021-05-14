using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PolygonIo.Demos
{
    public class PolygonWebSocketV3 : IPolygonWebSocket, IDisposable
    {
        public event Action<string> OnStatus;
        public event Action<string> OnError;

        public event Action<ReadOnlyMemory<byte>> OnMessage;

        private ConcurrentQueue<string> PolygonCommandSendQueue { get; init; } = new();

        private const int ReceiveBufferSize = 8192 * 4;

        private const int SendRetryDelayOnDisconnect = 1000;
        private const int SendDelayBetweenCommandBatches = 500;
        private const int OuterLoop_OnException_ThreadSleepDuration = 10_000;

        private TimeSpan TryConnectTimeout = TimeSpan.FromSeconds(17);
        private TimeSpan TryConnectRetryInterval = TimeSpan.FromSeconds(2);
        private const int TryConnectMaxRetryCount = 5;

        private Uri URI { get; init; }
        private CancellationToken CancellationToken;
        private bool IsCancelled => CancellationToken.IsCancellationRequested;

        private ClientWebSocket ClientWebSocket = null;
        private Thread ReceiveThread;
        private Thread SendThread;

        public bool IsConnected => !Disposed && ClientWebSocket is not null && ClientWebSocket.State == WebSocketState.Open;

        public PolygonWebSocketV3(string uri = "wss://socket.polygon.io/stocks", CancellationToken ct = default)
        {
            this.URI = new Uri(uri);
            this.CancellationToken = ct;

            this.ReceiveThread = new Thread(new ThreadStart(LoopConnectReceive));
            this.ReceiveThread.Name = nameof(PolygonWebSocketV3) + " Receiver Thread";
            this.ReceiveThread.IsBackground = true;

            this.SendThread = new Thread(new ThreadStart(LoopSend));
            this.SendThread.Name = nameof(PolygonWebSocketV3) + " Sender Thread";
            this.SendThread.IsBackground = true;
        }

        private async void LoopConnectReceive()
        {
            RaiseOnStatus(nameof(LoopConnectReceive), $"{Thread.CurrentThread.Name} Started.");

            while (!IsCancelled)
            {
                try
                {
                    if (await TryConnect())
                    {
                        RaiseOnStatus(nameof(LoopConnectReceive), "Connected Successfully!");

                        await LoopReceive();
                    }
                }
                catch (Exception ex)
                {
                    RaiseOnError(nameof(LoopConnectReceive), ex);
                    Thread.Sleep(IsCancelled ? 0 : OuterLoop_OnException_ThreadSleepDuration);
                }
                finally
                {
                    Thread.Yield();
                }
            }

            RaiseOnStatus(nameof(LoopConnectReceive), $"{Thread.CurrentThread.Name} Exit.");
        }

        private async Task<bool> TryConnect()
        {
            RaiseOnStatus(nameof(TryConnect), $"Starting Connection Attempt(s).");

            var retryCount = 0;
            var timer = Stopwatch.StartNew();

            while (!IsCancelled &&
                   !IsConnected &&
                   timer.Elapsed < TryConnectTimeout &&
                   retryCount < TryConnectMaxRetryCount)
            {
                if (ClientWebSocket is null)
                    ClientWebSocket = new ClientWebSocket();

                await ClientWebSocket.ConnectAsync(URI, CancellationToken);

                switch (ClientWebSocket.State)
                {
                    case WebSocketState.Open:
                        return true;
                    case WebSocketState.None:
                    case WebSocketState.Connecting:
                        await Task.Delay(TryConnectRetryInterval, CancellationToken);
                        continue;
                    default:
                        await Task.Delay(TryConnectRetryInterval, CancellationToken);
                        retryCount++;
                        continue;
                }
            }

            RaiseOnError(nameof(TryConnect), "Failed to Connect (without Exception).");
            return false;
        }

        private async Task LoopReceive()
        {
            RaiseOnStatus(nameof(LoopConnectReceive), $"Starting Receive Loop.");

            Memory<byte> receiveBuffer = new byte[ReceiveBufferSize];

            while (!IsCancelled && IsConnected)
            {
                int messageLength = 0;
                ValueWebSocketReceiveResult receiveResult;
                do
                {
                    var receiveBufferPart = receiveBuffer.Slice(messageLength);
                    receiveResult = await ClientWebSocket.ReceiveAsync(receiveBufferPart, CancellationToken);
                    messageLength += receiveResult.Count;
                } while (!receiveResult.EndOfMessage);

                ReadOnlyMemory<byte> msg = receiveBuffer.Slice(0, messageLength);

                OnMessage?.Invoke(msg);
            }
            RaiseOnStatus(nameof(LoopConnectReceive), $"Receive Loop Exit (without Exception).");
        }

        private async void LoopSend()
        {
            RaiseOnStatus(nameof(LoopSend), $"{Thread.CurrentThread.Name} Started.");

            while (!IsCancelled)
            {
                if (IsConnected)
                {
                    try
                    {
                        while (PolygonCommandSendQueue.TryDequeue(out string msg))
                        {
                            ReadOnlyMemory<byte> memMsg = Encoding.UTF8.GetBytes(msg).AsMemory();
                            await ClientWebSocket.SendAsync(memMsg, WebSocketMessageType.Text, endOfMessage: true, CancellationToken);
                        }
                        Thread.Sleep(IsCancelled ? 0 : SendDelayBetweenCommandBatches);
                    }
                    catch (Exception ex)
                    {
                        RaiseOnError(nameof(LoopSend), ex);
                        Thread.Sleep(IsCancelled ? 0 : OuterLoop_OnException_ThreadSleepDuration);
                    }
                }
                else
                {
                    Thread.Sleep(IsCancelled ? 0 : SendRetryDelayOnDisconnect);
                }
            }

            RaiseOnStatus(nameof(LoopSend), $"{Thread.CurrentThread.Name} Exit.");
        }
        public void Send(string json)
        {
            PolygonCommandSendQueue.Enqueue(json);
        }
        #region Start
        private bool Started = false;
        public void Start()
        {
            if (!Started)
            {
                Started = true;
                ReceiveThread.Start();
                SendThread.Start();
            }
            else
            {
                throw new InvalidOperationException("Already Started! Method 'Start' may only be called once.");
            }
        }
        #endregion
        #region IDisposable
        private bool Disposed = false;
        public void Dispose()
        {
            try
            {
                if (!Disposed)
                {
                    ClientWebSocket?.Dispose();
                }
            }
            finally
            {
                Disposed = true;
            }
        }
        #endregion
        private void RaiseOnStatus(string methodName, string msgStatus)
        {
            try
            {
                OnStatus?.Invoke($"{nameof(PolygonWebSocketV3)} | {methodName} | {msgStatus}");
            }
            catch { }
        }
        private void RaiseOnError(string methodName, string message)
        {
            try
            {
                OnError?.Invoke($"{nameof(PolygonWebSocketV3)} | {methodName} | Error: {message}");
            }
            catch { }
        }
        private void RaiseOnError(string methodName, Exception ex)
        {
            try
            {
                OnError?.Invoke($"{nameof(PolygonWebSocketV3)} | {methodName} | Unexpected Exception! Message: {ex.Message}");
            }
            catch { }
        }
    }
}