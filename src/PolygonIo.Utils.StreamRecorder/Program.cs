﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PolygonIo.WebSocket;
using Serilog;
using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Threading.Tasks.Dataflow;

namespace PolygonIo.Utils.StreamRecorder
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Error: please supply tickers to subscribe to as arguments.");
                return;
            }

            using var log = new LoggerConfiguration().WriteTo.Console().CreateLogger();
            var loggerFactory = new LoggerFactory().AddSerilog(log);
            var logger = loggerFactory.CreateLogger<Program>();

            var devEnvironmentVariable = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");

            var isDevelopment = string.IsNullOrEmpty(devEnvironmentVariable) ||
                                devEnvironmentVariable.ToLower() == "development";

            var configuration = new ConfigurationBuilder()
                                        .AddUserSecrets<Program>()
                                        .Build();

            var apiKey = configuration.GetSection("PolygonIo").GetValue<string>("ApiKey");

            var filename = $"polygonio_dump_{DateTime.Now.ToString("yyyyMMddTHHmmss")}.dmp";

            using var binaryWriter = new BinaryWriter(File.OpenWrite(filename));

            var lastUpdate = DateTime.UtcNow;
            long count = 0;

            var writerActionBlock = new ActionBlock<ReadOnlySequence<byte>>((data) =>
            {
                count =+ data.Length;
                binaryWriter.Write(data.ToArray());
                binaryWriter.Write("\n"); // delimit frames with new line

                var span = (DateTime.UtcNow - lastUpdate);

                if (span.TotalSeconds > 10)
                {
                    Console.WriteLine($"Written {count:n0} bytes in {span.TotalSeconds} seconds.");
                    lastUpdate = DateTime.UtcNow;
                }
            });

            using var polygonConnection = new PolygonConnection(apiKey, "wss://socket.polygon.io/stocks", writerActionBlock, TimeSpan.FromSeconds(15), loggerFactory);

            polygonConnection.Start(args.Select(x => x.ToUpper()));

            Console.WriteLine($"Now recording to {filename}, press any key to stop...");
            Console.ReadKey();

            polygonConnection.Stop();
            writerActionBlock.Complete();
            writerActionBlock.Completion.Wait();
            binaryWriter.Flush();
            binaryWriter.Close();
        }
    }
}
