using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PolygonIo.Demos
{
    [TestClass]
    public class Tests_Mock
    {
        private const string apiKey = "********";

        [TestMethod]
        public async Task Test_WebSocket()
        {
            List<Quote> quotes = new();
            List<Trade> trades = new();

            var cts = new CancellationTokenSource();

            using (IPolygonWebSocket wsPolygon = new PolygonWebSocketV3(ct:cts.Token))
            {
                ((PolygonWebSocketV3)wsPolygon).OnStatus += (status) => Debug.WriteLine($"{nameof(PolygonWebSocketV3)} | {nameof(wsPolygon)} | Status: {status}");

                IPolygonJsonService jsonPolygon = new PolygonJsonServiceV3(wsPolygon);
                jsonPolygon.OnJSON += (json) => Debug.WriteLine($"{nameof(PolygonJsonServiceV3)} | {nameof(jsonPolygon)} | JSON Data: {Encoding.UTF8.GetString(json.Span)}");

                IPolygonDataService dataPolygon = new PolygonDataServiceV3(jsonPolygon);
                dataPolygon.OnQuote += (quote) => { quotes.Add(quote); Debug.WriteLine($"{nameof(PolygonDataServiceV3)} | {nameof(dataPolygon)} | Quote Count: {quotes.Count} ({quote.Symbol})."); };
                dataPolygon.OnTrade += (trade) => { trades.Add(trade); Debug.WriteLine($"{nameof(PolygonDataServiceV3)} | {nameof(dataPolygon)} | Trade Count: {trades.Count} ({trade.Symbol})."); };

                wsPolygon.Start();
                AssertWithTimeout.IsTrue(() => ((PolygonWebSocketV3)wsPolygon).IsConnected, $"Time limit exceeded while waiting for {nameof(wsPolygon)} IsConnected.", TimeSpan.FromSeconds(3));
                AssertWithTimeout.IsTrue(() => ((PolygonJsonServiceV3)jsonPolygon).IsConnected, $"Time limit exceeded while waiting for {nameof(jsonPolygon)} IsConnected.", TimeSpan.FromSeconds(3));

                jsonPolygon.Authenticate(apiKey);
                AssertWithTimeout.IsTrue(() => ((PolygonJsonServiceV3)jsonPolygon).IsAuthenticated, "Time limit exceeded while waiting for IsAuthenticated.", TimeSpan.FromSeconds(3));

                var symbol = "T.SPY";
                jsonPolygon.Subscribe(symbol);
                AssertWithTimeout.IsTrue(() => ((PolygonJsonServiceV3)jsonPolygon).IsSubscribed(symbol), $"Time limit exceeded while waiting for IsSubscribed(\"{symbol}\").", TimeSpan.FromSeconds(3));

                AssertWithTimeout.IsTrue(() => trades.Count > 0, "Time limit exceeded while waiting for trades to arrive.", TimeSpan.FromSeconds(3));
                Debug.WriteLine("Success! Initiating Cancel...");
                cts.Cancel();
            }
        }

        FileStream fs = null;
        string logFileName = @"c:\Polygon\polygon.SPY.log";
        
        [TestMethod]
        public async Task Test_WebSocket_ToFile()
        {
            var cts = new CancellationTokenSource();
            using (IPolygonWebSocket wsPolygon = new PolygonWebSocketV3(ct: cts.Token))
            {
                ((PolygonWebSocketV3)wsPolygon).OnStatus += (status) => Debug.WriteLine($"{nameof(PolygonWebSocketV3)} | {nameof(wsPolygon)} | Status: {status}");

                IPolygonJsonService jsonPolygon = new PolygonJsonServiceV3(wsPolygon);
                jsonPolygon.OnJSON += JsonPolygon_OnJSON;

                wsPolygon.Start();
                AssertWithTimeout.IsTrue(() => ((PolygonWebSocketV3)wsPolygon).IsConnected, $"Time limit exceeded while waiting for {nameof(wsPolygon)} IsConnected.", TimeSpan.FromSeconds(3));
                AssertWithTimeout.IsTrue(() => ((PolygonJsonServiceV3)jsonPolygon).IsConnected, $"Time limit exceeded while waiting for {nameof(jsonPolygon)} IsConnected.", TimeSpan.FromSeconds(3));

                jsonPolygon.Authenticate(apiKey);
                AssertWithTimeout.IsTrue(() => ((PolygonJsonServiceV3)jsonPolygon).IsAuthenticated, "Time limit exceeded while waiting for IsAuthenticated.", TimeSpan.FromSeconds(3));

                var symbols = new string[] { "A.SPY", "Q.SPY", "T.SPY" };
                foreach (var symbol in symbols)
                    jsonPolygon.Subscribe(symbol);

                Task.WaitAll(Task.Delay(TimeSpan.FromMinutes(60)));
                Debug.WriteLine("Success! Initiating Cancel...");
                cts.Cancel();
            }
        }

        private readonly ReadOnlyMemory<byte> newLineBytes = Encoding.UTF8.GetBytes(Environment.NewLine).AsMemory();
        private void JsonPolygon_OnJSON(ReadOnlyMemory<byte> json)
        {
            if (fs is null)
                fs = new FileStream(logFileName, FileMode.Append, FileAccess.Write, FileShare.Read);

            lock (fs)
            {
                try
                {
                    fs.Write(json.Span);
                    fs.Flush();
                    fs.Write(newLineBytes.Span);
                    fs.Flush();
                }
                catch
                {
                    fs = null;
                }
            }
        }

        [TestMethod]
        public void Test_Mock_WebSocket()
        {
        }
        [TestMethod]
        public void Test_Mock_DataFromStringLiterals()
        {
            List<string> jsonData = new();

            IPolygonJsonService jsonPolygon = new PolygonJsonServiceMockFromStrings();
            jsonPolygon.OnJSON += (json) => jsonData.Add(Encoding.UTF8.GetString(json.Span));

            IPolygonDataService dataPolygon = new PolygonDataServiceV3(jsonPolygon);
            dataPolygon.OnQuote += (q) => Debug.WriteLine(q.Symbol);
            dataPolygon.OnTrade += (t) => Debug.WriteLine(t.Symbol);

            jsonPolygon.Authenticate("**");
            jsonPolygon.Subscribe("T.SPY");
            jsonPolygon.Subscribe("Q.SPY");

            //PolygonJsonServiceMockFromStrings.SimulateTrades("T.SPY");

            Assert.AreEqual(4, jsonData.Count);
            Assert.IsTrue(jsonData[0].Contains("connected"));
            Assert.IsTrue(jsonData[1].Contains("auth_success"));
            Assert.IsTrue(jsonData[2].Contains("T.SPY"));
            Assert.IsTrue(jsonData[3].Contains("Q.SPY"));
        }
        [TestMethod]
        public void Test_Mock_DataFromFile()
        {

        }
    }
}
