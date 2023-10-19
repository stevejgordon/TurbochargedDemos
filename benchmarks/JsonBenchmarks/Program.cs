using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BulkResponseParsingDemo;
using Dia2Lib;

namespace BulkResponseParserBenchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = ManualConfig
                .Create(DefaultConfig.Instance)
                .AddDiagnoser(MemoryDiagnoser.Default)
                .WithSummaryStyle(new SummaryStyle(null, false, SizeUnit.KB, null,
                    ratioStyle: RatioStyle.Percentage));

            BenchmarkRunner.Run<BulkResponseParserBenchmarks>(config);
        }

        //public static async Task Main()
        //{
        //    var thing = new BulkResponseParserBenchmarks();
        //    thing.Setup();
        //    await thing.ParseResponseNew();
        //}
    }

    [MemoryDiagnoser]
    public class BulkResponseParserBenchmarks
    {
        public Stream _errorStream = new MemoryStream();
        public Stream _successStream = new MemoryStream();

        [GlobalSetup]
        public void Setup()
        {
            using (FileStream fs = File.OpenRead(AppDomain.CurrentDomain.BaseDirectory + "/FailedResponseSample.txt"))
            {
                fs.CopyTo(_errorStream);
            }

            using (FileStream fs = File.OpenRead(AppDomain.CurrentDomain.BaseDirectory + "/SuccessResponseSample.txt"))
            {
                fs.CopyTo(_successStream);
            }
        }

        [Benchmark(Baseline = true)]
        public void ParseResponseOriginal()
        {
            var stream = _errorStream;
            stream.Position = 0;
            var (success, errors) = BulkResponseParser.ParseResponse(stream);
        }

        [Benchmark]
        public async Task ParseResponseNew()
        {
            var stream = _errorStream;
            stream.Position = 0;
            var (success, errors) = await BulkResponseParserNew.ParseResponseAsync(stream);
        }
    }
}
