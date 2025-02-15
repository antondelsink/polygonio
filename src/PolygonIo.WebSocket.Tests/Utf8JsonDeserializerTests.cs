using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using PolygonIo.WebSocket.Contracts;
using PolygonIo.WebSocket.Deserializers;
using PolygonIo.WebSocket.Factory;
using System;
using System.Buffers;
using System.Text;

namespace PolygonIo.WebSocket.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void CanProcessData()
        {
            var utf8JsonDeserializer = new Utf8JsonDeserializer(new EventFactory<Quote, Trade, TimeAggregate, Status>());

            var str = "[{ \"ev\":\"status\",\"status\":\"connected\",\"message\":\"Connected Successfully\"}]";

            IStatus s = null;
            utf8JsonDeserializer.Deserialize(
                                    new ReadOnlySpan<byte>(Encoding.UTF8.GetBytes(str)),
                                    (quote) => { },
                                    (trade) => { },
                                    (aggregate) => { },
                                    (aggregate) => { },
                                    (status) => { s = status; },
                                    (error) => { });


            Assert.IsNotEmpty(s.Message);
        }
    }
}