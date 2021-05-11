using System;

namespace PolygonIo.Demos
{
    internal interface IPolygonWebSocket : IDisposable
    {
        event Action<ReadOnlyMemory<byte>> OnMessage;

        void Start();

        bool IsConnected { get; }

        void Send(string message);
    }
}