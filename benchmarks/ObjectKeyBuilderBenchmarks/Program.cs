using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using ObjectKeyBuilderDemo;

namespace ObjectKeyBuilderBenchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = ManualConfig
                .Create(DefaultConfig.Instance)
                .AddDiagnoser(MemoryDiagnoser.Default)
                .WithSummaryStyle(new SummaryStyle(null, false, null, null,
                    ratioStyle: RatioStyle.Percentage));

            _ = BenchmarkRunner.Run<SpanBenchmarks>(config);
        }
    }

    [MemoryDiagnoser]
    public class SpanBenchmarks
    {
        private EventContext _context;

        [GlobalSetup]
        public void Setup()
        {
            _context = new EventContext
            {
                MessageId = "ebc6d78b-8b29-4fc3-a412-bc43be6a9d21",
                EventDateUtc = new DateTime(2019, 04, 01, 10, 00, 00, DateTimeKind.Utc),
                EventName = "My Event",
                Product = "My Product",
                SiteKey = "Site Key"
            };
        }

        [Benchmark(Baseline = true)]
        public void Original() => _ = S3ObjectKeyGenerator.GenerateSafeObjectKey(_context);

        [Benchmark]
        public void SpanBased() => _ = S3ObjectKeyGeneratorNew.GenerateSafeObjectKey(_context);

        [Benchmark]
        public void StringCreateBased() => _ = S3ObjectKeyGeneratorNewV2.GenerateSafeObjectKey(_context);
    }
}
