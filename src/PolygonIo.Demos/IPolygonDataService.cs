using System;

namespace PolygonIo.Demos
{
    internal interface IPolygonDataService
    {
        public event Action<Quote> OnQuote;
        public event Action<Trade> OnTrade;
    }
}