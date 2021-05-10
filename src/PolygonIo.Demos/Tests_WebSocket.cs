using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PolygonIo.Demos
{
    [TestClass]
    public class Tests_WebSocket
    {
        private const string wssUriPolygonIO = "wss://socket.polygon.io/stocks";
        private const string apiKeyPolygonIO = "******************************";

        private int countStatusMessages = 0;
        private int countQuoteMessages = 0;
        private int countTradeMessages = 0;
        private int countAggregateMessages = 0;

        [TestMethod]
        public void Test_101_JsonConvertDeserializeObject()
        {
            var filename = @"C:\PolygonData\2021-05-06-06-12--websocket.log";
            Assert.IsTrue(File.Exists(filename));

            int lineCount = 0;
            int objectCount = 0;
            foreach (var line in File.ReadLines(filename))
            {
                lineCount++;

                IEnumerable<string> jsonObjects;
                jsonObjects = GetEnumerableOfJsonStrings(line);

                foreach (var sJsonObject in jsonObjects)
                {
                    objectCount++;

                    _ = Encoding.UTF8.GetBytes(sJsonObject).AsSpan(); // for equivalency of tests

                    ProcessJsonObjectString(sJsonObject);
                }
            }
            Assert.AreEqual(23518188, countQuoteMessages);
            Assert.AreEqual(6036154, countTradeMessages);
            Assert.AreEqual(537411, countAggregateMessages);
            Assert.AreEqual(1520, countStatusMessages);
            Assert.AreEqual(10951522, lineCount); // compared to command-line "find /C 'ev' ..."
        }

        [TestMethod]
        public void Test_100_Utf8Reader()
        {
            var filename = @"C:\PolygonData\2021-05-06-06-12--websocket.log";
            Assert.IsTrue(File.Exists(filename));

            int lineCount = 0;
            int objectCount = 0;
            foreach (var line in File.ReadLines(filename))
            {
                lineCount++;

                IEnumerable<string> jsonObjects;
                jsonObjects = GetEnumerableOfJsonStrings(line);

                foreach (var sJsonObject in jsonObjects)
                {
                    objectCount++;

                    ReadOnlySpan<byte> json = Encoding.UTF8.GetBytes(sJsonObject).AsSpan();

                    switch (json[7])
                    {
                        case (byte)'Q':
                            Assert.IsTrue(Quote.TryParse(json, out Quote q));
                            Assert.IsTrue(!string.IsNullOrWhiteSpace(q.Symbol));
                            countQuoteMessages++;
                            break;
                        case (byte)'T':
                            Assert.IsTrue(Trade.TryParse(json, out Trade t));
                            Assert.IsTrue(!string.IsNullOrWhiteSpace(t.Symbol));
                            countTradeMessages++;
                            break;
                        case (byte)'A':
                            countAggregateMessages++;
                            break;
                        case (byte)'s':
                            countStatusMessages++;
                            break;
                        default:
                            return;
                    }
                }
            }
            Assert.AreEqual(23518188, countQuoteMessages);
            Assert.AreEqual(6036154, countTradeMessages);
            Assert.AreEqual(537411, countAggregateMessages);
            Assert.AreEqual(1520, countStatusMessages);
            Assert.AreEqual(10951522, lineCount); // compared to command-line "find /C 'ev' ..."
        }

        private static IEnumerable<string> GetEnumerableOfJsonStrings(string line)
        {
            IEnumerable<string> jsonObjects;
            if (line.Contains("},{"))
            {
                jsonObjects = from fragment in line.Split("},{")
                              select "{" + fragment.Trim('[', '{', '}', ']') + "}";
            }
            else
            {
                jsonObjects = new string[] { line.Trim('[', ']') };
            }

            return jsonObjects;
        }

        private void ProcessJsonObjectString(string sJsonObject)
        {
            switch (sJsonObject[7])
            {
                case 'T':
                    Trade_JsonDeserializeObject t = JsonConvert.DeserializeObject<Trade_JsonDeserializeObject>(sJsonObject);
                    Assert.IsTrue(!string.IsNullOrWhiteSpace(t.Symbol));
                    countTradeMessages++;
                    break;
                case 'Q':
                    Quote_JsonDeserializeObject q = JsonConvert.DeserializeObject<Quote_JsonDeserializeObject>(sJsonObject);
                    Assert.IsTrue(!string.IsNullOrWhiteSpace(q.Symbol));
                    countQuoteMessages++;
                    break;
                case 'A':
                    countAggregateMessages++;
                    break;
                case 's':
                    countStatusMessages++;
                    break;
            }
        }

        [TestMethod]
        public async Task Test_PolygonServiceV2()
        {
            var cts = new CancellationTokenSource();
            using (var pws = new PolygonWebSocketV2(new Uri(wssUriPolygonIO), cts.Token))
            {
                pws.OnStatus += (wsStatus) => Debug.WriteLine(wsStatus);

                using (var svcPolygon = new PolygonServiceV2(pws, apiKeyPolygonIO))
                {
                    var statusMessages = new List<string>();
                    svcPolygon.OnStatusString += (svcStatus) => statusMessages.Add(svcStatus); ;

                    var dataMessages = new List<PolygonResponseMessage>();
                    svcPolygon.OnData += (data) => dataMessages.Add(data);

                    svcPolygon.Start();

                    while (!svcPolygon.TrySubscribe("T.AMZN"))
                    {
                        Thread.Sleep(100);
                    }

                    while (statusMessages.Count < 3)
                        Thread.Yield();

                    Assert.IsTrue(statusMessages.Count >= 3);

                    var connectionSuccessMessage = from line in statusMessages
                                                   where line.Contains("Connected Successfully")
                                                   select line;
                    Assert.IsTrue(connectionSuccessMessage.Count() == 1);

                    var authSuccessMessage = from line in statusMessages
                                             where line.Contains("authenticated")
                                             select line;
                    Assert.IsTrue(authSuccessMessage.Count() == 1);

                    var subscribeSuccessMessage = from line in statusMessages
                                                  where line.Contains("subscribed to: T.AMZN")
                                                  select line;
                    Assert.IsTrue(subscribeSuccessMessage.Count() == 1);

                    Thread.Sleep(100);
                    cts.Cancel();
                    Thread.Sleep(100);
                }
            }
        }

        [TestMethod]
        public async Task Test_PolygonWebSocketV2()
        {
            var cts = new CancellationTokenSource();
            using (var pws = new PolygonWebSocketV2(new Uri(wssUriPolygonIO), cts.Token))
            {
                pws.OnStatus += (status) => Debug.WriteLine(status);

                pws.PolygonCommandSendQueue.Enqueue(new PolygonCommand() { Action = "auth", Params = apiKeyPolygonIO });
                pws.PolygonCommandSendQueue.Enqueue(new PolygonCommand() { Action = "subscribe", Params = "T.AMZN" });

                var expectedResponses = new List<string>(3);
                pws.OnDataAsString += (data) => expectedResponses.Add(data);

                pws.Start();

                while (expectedResponses.Count < 3)
                    Thread.Yield();

                cts.Cancel();

                Assert.IsTrue(expectedResponses.Count >= 3);

                var connectionSuccessMessage = from line in expectedResponses
                                               where line.Contains("status") && line.Contains("connected") && line.Contains("Connected Successfully")
                                               select line;
                Assert.IsTrue(connectionSuccessMessage.Count() == 1);

                var authSuccessMessage = from line in expectedResponses
                                         where line.Contains("status") && line.Contains("auth_success") && line.Contains("authenticated")
                                         select line;
                Assert.IsTrue(authSuccessMessage.Count() == 1);

                var subscribeSuccessMessage = from line in expectedResponses
                                              where line.Contains("status") && line.Contains("success") && line.Contains("subscribed to: T.AMZN")
                                              select line;
                Assert.IsTrue(subscribeSuccessMessage.Count() == 1);
            }
        }

        [TestMethod]
        public async Task Test_002_Polygon()
        {
            var uri = new Uri(wssUriPolygonIO);
            var cts = new CancellationTokenSource();

            using (var wsPolygon = new PolygonWebSocket(uri, cts.Token))
            {
                wsPolygon.OnStatus += (s) => Debug.WriteLine(s);

                using (var svcPolygon = new PolygonService(wsPolygon, apiKeyPolygonIO))
                {
                    svcPolygon.OnStatus += (s) => Debug.WriteLine(s);

                    int count = 0;
                    svcPolygon.OnData += (msg) => { count++; Debug.WriteLineIf(count % 100 == 0, $"Count: {count}"); };

                    wsPolygon.Start();

                    while (!svcPolygon.IsConnected || !svcPolygon.IsAuthenticated)
                    {
                        Thread.Yield();
                    }

                    foreach (var symbol in GetSymbols())
                    {
                        while (!svcPolygon.TrySubscribe("T." + symbol))
                        {
                            Thread.Yield();
                        }
                        while (!svcPolygon.TrySubscribe("Q." + symbol))
                        {
                            Thread.Yield();
                        }
                    }

                    Task.WaitAll(Task.Delay(TimeSpan.FromMinutes(60)));
                    cts.Cancel();
                }
            }
        }
        private static IEnumerable<string> GetSymbols()
        {
            var filename = @"C:\PolygonData\symbols.txt";
            Assert.IsTrue(File.Exists(filename));

            foreach (var line in File.ReadLines(filename))
            {
                yield return line;
            }
        }

        [TestMethod]
        public async Task Test_001_Polygon()
        {
            var cS = new CancellationTokenSource();
            var cT = cS.Token;
            var uri = new Uri(wssUriPolygonIO);

            using var wsClient = new ClientWebSocket();

            await wsClient.ConnectAsync(uri, cT);

            Assert.AreEqual(WebSocketState.Open, wsClient.State);

            Memory<byte> receiveBuffer_Connect = new byte[4096];
            var connectResult = await wsClient.ReceiveAsync(receiveBuffer_Connect, cT);
            Assert.IsTrue(connectResult.EndOfMessage); // TBC: one Polygon Message for one WebSocket Message
            Assert.IsTrue(connectResult.Count > 0);
            Assert.IsTrue(connectResult.Count < receiveBuffer_Connect.Length); // am not expecting the message to fill the buffer
            Assert.AreEqual(WebSocketMessageType.Text, connectResult.MessageType);

            var msgConnect = new PolygonResponseMessage(Encoding.UTF8.GetString(receiveBuffer_Connect.Span.Slice(0, connectResult.Count)));
            Assert.AreEqual("status", msgConnect.EV);
            Assert.AreEqual("connected", msgConnect.Status);
            Assert.AreEqual("Connected Successfully", msgConnect.Message);

            var cmdAuth = new PolygonCommand()
            {
                Action = "auth",
                Params = apiKeyPolygonIO
            };

            Memory<byte> msgAuthenticate = Encoding.UTF8.GetBytes(cmdAuth.ToString());
            await wsClient.SendAsync(msgAuthenticate, WebSocketMessageType.Text, endOfMessage: true, cT);

            Memory<byte> receiveBuffer_Auth = new byte[4096];
            var authResult = await wsClient.ReceiveAsync(receiveBuffer_Auth, cT);
            Assert.IsTrue(authResult.EndOfMessage); // TBC: one Polygon Message for one WebSocket Message
            Assert.IsTrue(authResult.Count > 0);
            Assert.IsTrue(authResult.Count < receiveBuffer_Auth.Length); // am not expecting the message to fill the buffer
            Assert.AreEqual(WebSocketMessageType.Text, authResult.MessageType);

            var msgAuth = new PolygonResponseMessage(Encoding.UTF8.GetString(receiveBuffer_Auth.Span.Slice(0, authResult.Count)));
            Assert.AreEqual("status", msgAuth.EV);
            Assert.AreEqual("auth_success", msgAuth.Status);
            Assert.AreEqual("authenticated", msgAuth.Message);

            var cmdSubscribe = new PolygonCommand()
            {
                Action = "subscribe",
                Params = "T.MSFT"
            };
            Memory<byte> msgSubscribe = Encoding.UTF8.GetBytes(cmdSubscribe.ToString());
            await wsClient.SendAsync(msgSubscribe, WebSocketMessageType.Text, endOfMessage: true, cT);

            Memory<byte> receiveBuffer_Sub = new byte[4096];
            var subResult = await wsClient.ReceiveAsync(receiveBuffer_Sub, cT);
            Assert.IsTrue(subResult.EndOfMessage); // TBC: one Polygon Message for one WebSocket Message
            Assert.IsTrue(subResult.Count > 0);
            Assert.IsTrue(subResult.Count < receiveBuffer_Auth.Length); // am not expecting the message to fill the buffer
            Assert.AreEqual(WebSocketMessageType.Text, subResult.MessageType);

            var msgSub = new PolygonResponseMessage(Encoding.UTF8.GetString(receiveBuffer_Sub.Span.Slice(0, subResult.Count)));
            Assert.AreEqual("status", msgSub.EV);
            Assert.AreEqual("success", msgSub.Status);
            Assert.AreEqual("subscribed to: T.MSFT", msgSub.Message);

            var logFileName = @"C:\PolygonData\polygon.T.MSFT.txt";
            using var fs = new FileStream(logFileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);

            int maxReceived = 0;
            Memory<byte> receiveBuffer = new byte[8192 * 4]; // 32kB
            while (true)
            {
                var streamResult = await wsClient.ReceiveAsync(receiveBuffer, cT);
                Assert.IsTrue(streamResult.EndOfMessage); // TBC: one Polygon Message for one WebSocket Message
                Assert.IsTrue(streamResult.Count > 0);
                Assert.IsTrue(streamResult.Count < receiveBuffer.Length); // am not expecting the message to fill the buffer
                Assert.AreEqual(WebSocketMessageType.Text, streamResult.MessageType);

                await fs.WriteAsync(receiveBuffer.Slice(0, streamResult.Count));
                await fs.FlushAsync();

                var data = Encoding.UTF8.GetString(receiveBuffer.Span.Slice(0, streamResult.Count));
                Debug.WriteLine(data);
            }
        }
        [TestMethod]
        public async Task Test_999_LogPolygon1Hr()
        {
            var uri = new Uri(wssUriPolygonIO);
            var cts = new CancellationTokenSource();

            using (var wsPolygon = new PolygonWebSocket(uri, cts.Token))
            {
                wsPolygon.OnStatus += (s) => Debug.WriteLine(s);

                using (var svcPolygon = new PolygonService(wsPolygon, apiKeyPolygonIO))
                {
                    svcPolygon.OnStatus += (s) => Debug.WriteLine(s);

                    int count = 0;
                    svcPolygon.OnData += (msg) => { count++; Debug.WriteLineIf(count % 100 == 0, $"Count: {count}"); };

                    svcPolygon.OnData += LogToDisk;

                    wsPolygon.Start();

                    while (!svcPolygon.IsConnected || !svcPolygon.IsAuthenticated)
                    {
                        Thread.Yield();
                    }

                    foreach (var symbol in GetSymbols())
                    {
                        while (!svcPolygon.TrySubscribe("T." + symbol))
                        {
                            Thread.Yield();
                        }
                        while (!svcPolygon.TrySubscribe("Q." + symbol))
                        {
                            Thread.Yield();
                        }
                        while (!svcPolygon.TrySubscribe("A." + symbol))
                        {
                            Thread.Yield();
                        }
                    }
                    Task.WaitAll(Task.Delay(TimeSpan.FromMinutes(60)));
                    cts.Cancel();
                }
            }
        }

        private StreamWriter sw = null;
        private void LogToDisk(PolygonResponseMessage data)
        {
            if (sw is null)
                sw = File.CreateText(@"C:\PolygonData\polygon.txt");

            lock (sw)
            {
                switch (data.EV)
                {
                    case "T":
                    case "Q":
                    case "A":
                        var line = data.Original;
                        if (!line.Contains("},{"))
                        {
                            sw.WriteLine("{" + line.Trim('[', '{', '}', ']') + "}");
                        }
                        else
                        {
                            var lines = from e in line.Trim('[', ']').Split("},{")
                                        select e.Trim('{', '}');

                            foreach (var txt in lines)
                                sw.WriteLine("{" + txt + "}");
                        }
                        break;
                }
                sw.Flush();
            }
        }
    }
}