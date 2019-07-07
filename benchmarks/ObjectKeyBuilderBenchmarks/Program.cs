using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ObjectKeyBuilderDemo;

namespace ObjectKeyBuilderBenchmarks
{
    class Program
    {
        static void Main(string[] args) => _ = BenchmarkRunner.Run<SpanBenchmarks>();
    }

    [MemoryDiagnoser]
    public class SpanBenchmarks
    {
        private EventContext _context;
        private string _data;

        [GlobalSetup]
        public void Setup()
        {
            //_context = new EventContext
            //{
            //    MessageId = "ebc6d78b-8b29-4fc3-a412-bc43be6a9d21",
            //    EventDateUtc = new DateTime(2019, 04, 01, 10, 00, 00, DateTimeKind.Utc),
            //    EventName = "My Event",
            //    Product = "My Product",
            //    SiteKey = "Site Key"
            //};

            _data = "MyProduct|";
        }

        [Params(5, 20, 100)]
        public int Count { get; set; }

        [Benchmark]
        public string StringCreate()
        {
            var length = _data.Length * Count;           

            var s = string.Create(length, (_data), (chars, data) =>
            {
                var position = 0;

                for (int i = 0; i < Count; i++)
                {
                    data.AsSpan().CopyTo(chars.Slice(position));
                    position += data.Length;
                }
            });

            return s;
        }

        [Benchmark]
        public string StackAlloc()
        {
            var length = _data.Length * Count;

            Span<char> chars = stackalloc char[length];

            var position = 0;

            for (int i = 0; i < Count; i++)
            {
                _data.AsSpan().CopyTo(chars.Slice(position));
                position += _data.Length;
            }
            
            return chars.ToString();
        }

        //[Benchmark(Baseline = true)]
        //public void Original()
        //{
        //    _ = S3ObjectKeyGenerator.GenerateSafeObjectKey(_context);
        //}

        //[Benchmark]
        //public void SpanBased()
        //{
        //    _ = S3ObjectKeyGeneratorNew.GenerateSafeObjectKey(_context);
        //}

        //[Benchmark]
        //public void StringCreate()
        //{
        //    _ = S3ObjectKeyGeneratorNewV2.GenerateSafeObjectKey(_context);
        //}
    }
}
