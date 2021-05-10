using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PolygonIo.Demos
{
    public sealed class PolygonWebSocketV2 : IDisposable
    {
        public event Action<string> OnStatus;
        public event Action<string> OnError;

        public event Action<string> OnDataAsString;
        public event Action<ReadOnlyMemory<byte>> OnDataAsBytes;

        public ConcurrentQueue<PolygonCommand> PolygonCommandSendQueue { get; init; } = new();

        private const int ReceiveBufferSize = 8192 * 4;

        private const int SendRetryDelayOnDisconnect = 250;
        private const int SendDelayBetweenCommandBatches = 500;
        private const int OuterLoop_OnException_ThreadSleepDuration = 10_000;

        private TimeSpan TryConnectTimeout = TimeSpan.FromSeconds(7);
        private TimeSpan TryConnectRetryInterval = TimeSpan.FromMilliseconds(250);
        private const int TryConnectMaxRetryCount = 5;

        private Thread ReceiveThread;
        private Thread SendThread;

        private Uri URI;
        private CancellationToken CancellationToken;

        private Stream Log;
        public byte[] LogMessageSeparator { get; init; } = Encoding.UTF8.GetBytes(Environment.NewLine);

        private bool IsCancelled => CancellationToken.IsCancellationRequested;

        private ClientWebSocket ClientWebSocket = null;

        private bool IsConnected => !Disposed && ClientWebSocket is not null && ClientWebSocket.State == WebSocketState.Open;

        public PolygonWebSocketV2(Uri uriPolygonIO, CancellationToken token = default, ThreadPriority priority = ThreadPriority.AboveNormal, Stream log = null, byte[] logMsgSeparator = null)
        {
            this.URI = uriPolygonIO;
            this.CancellationToken = token;

            this.Log = log;
            this.LogMessageSeparator = (logMsgSeparator is null) ? this.LogMessageSeparator : logMsgSeparator;

            this.ReceiveThread = new Thread(new ThreadStart(LoopConnectReceive));
            this.ReceiveThread.Name = nameof(PolygonWebSocketV2) + " Receiver Thread";
            this.ReceiveThread.Priority = priority;
            this.ReceiveThread.IsBackground = true;

            this.SendThread = new Thread(new ThreadStart(LoopSend));
            this.SendThread.Name = nameof(PolygonWebSocketV2) + " Sender Thread";
            this.SendThread.IsBackground = true;
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

        #region Dispose
        private bool Disposed = false;
        public void Dispose()
        {
            try
            {
                if (!Disposed)
                {
                    ClientWebSocket?.Dispose();

                    SendThread = null;
                    ReceiveThread = null;
                }
            }
            finally
            {
                Disposed = true;
            }
        }
        #endregion

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
            RaiseOnStatus(nameof(LoopConnectReceive), $"Starting Connection Attempt(s).");

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

            RaiseOnError(nameof(LoopSend), "Failed to Connect (without Exception).");
            return false;
        }
        private async Task LoopReceive()
        {
            RaiseOnStatus(nameof(LoopConnectReceive), $"Starting Receive Loop.");

            Memory<byte> receiveBuffer = new byte[ReceiveBufferSize];

            while (!IsCancelled)
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

                if (Log is not null)
                {
                    await WriteToLogStream(msg);
                }

                RaiseOnDataAsBytes(msg);
                RaiseOnDataAsString(msg);
            }
            RaiseOnStatus(nameof(LoopConnectReceive), $"Receive Loop Exit (without Exception).");
        }
        private async Task WriteToLogStream(ReadOnlyMemory<byte> receiveBuffer)
        {
            try
            {
                await Log.WriteAsync(receiveBuffer, CancellationToken);
                await Log.WriteAsync(LogMessageSeparator, CancellationToken);
                await Log.FlushAsync();
            }
            catch (Exception ex)
            {
                Log = null;
                RaiseOnError(nameof(LoopReceive), ex);
            }
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
                        while (PolygonCommandSendQueue.TryDequeue(out PolygonCommand msg))
                        {
                            Memory<byte> memMsg = Encoding.UTF8.GetBytes(msg.ToString());
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
        private void RaiseOnStatus(string methodName, string msgStatus)
        {
            try
            {
                OnStatus?.Invoke($"{nameof(PolygonWebSocketV2)} | {methodName} | {msgStatus}");
            }
            catch { }
        }
        private void RaiseOnError(string methodName, string message)
        {
            try
            {
                OnError?.Invoke($"{nameof(PolygonWebSocketV2)} | {methodName} | Error: {message}");
            }
            catch { }
        }
        private void RaiseOnError(string methodName, Exception ex)
        {
            try
            {
                OnError?.Invoke($"{nameof(PolygonWebSocketV2)} | {methodName} | Unexpected Exception! Message: {ex.Message}");
            }
            catch { }
        }
        private void RaiseOnDataAsString(ReadOnlyMemory<byte> receiveBuffer)
        {
            if (OnDataAsString is not null)
            {
                try
                {
                    var data = Encoding.UTF8.GetString(receiveBuffer.Span);

                    OnDataAsString?.Invoke(data);
                }
                catch { }

            }
        }
        private void RaiseOnDataAsBytes(ReadOnlyMemory<byte> receiveBuffer)
        {
            if (OnDataAsBytes is not null)
            {
                try
                {
                    var data = new byte[receiveBuffer.Length].AsMemory();
                    receiveBuffer.CopyTo(data);

                    OnDataAsBytes?.Invoke(data);
                }
                catch { }
            }
        }
    }
}