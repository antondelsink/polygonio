using System;

namespace PolygonIo.Demos
{
    public interface IPolygonJsonService
    {
        event Action<ReadOnlyMemory<byte>> OnJSON;

        void Authenticate(string apiKey);

        void Subscribe(string symbol);
    }
}