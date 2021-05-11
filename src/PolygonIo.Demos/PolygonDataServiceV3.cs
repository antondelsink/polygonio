using System;

namespace PolygonIo.Demos
{
    internal class PolygonDataServiceV3 : IPolygonDataService
    {
        public event Action<Quote> OnQuote;
        public event Action<Trade> OnTrade;

        public PolygonDataServiceV3(IPolygonJsonService svcPolygon)
        {
            svcPolygon.OnJSON += ProcessJSON;
        }

        private void ProcessJSON(ReadOnlyMemory<byte> json)
        {
            switch (json.Span[7])
            {
                case (byte)'Q':
                    OnQuote?.Invoke(Quote.Parse(json.Span));
                    break;
                case (byte)'T':
                    OnTrade?.Invoke(Trade.Parse(json.Span));
                    break;
            }
        }
    }
}