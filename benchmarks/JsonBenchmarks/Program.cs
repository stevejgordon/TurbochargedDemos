using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BulkResponseParsingDemo;
using JetBrains.Profiler.Api;

namespace BulkResponseParserBenchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            _ = BenchmarkRunner.Run<BulkResponseParserBenchmarks>();

            //await using var fs = File.OpenRead(AppDomain.CurrentDomain.BaseDirectory + "/FailedResponseSample.txt");

            //var ms = new MemoryStream();
            //fs.CopyTo(ms);
            //ms.Seek(0, SeekOrigin.Begin);

            //MemoryProfiler.CollectAllocations(true);

            //MemoryProfiler.GetSnapshot();

            //var errors = ArrayPool<string>.Shared.Rent(128); // can be rented to at least the size of the bulk index operation

            //try
            //{
            //    MemoryProfiler.GetSnapshot();

            //    var totalErrors = await BulkResponseParserNewV2.ParseFromStreamAsync(ms, errors);

            //    MemoryProfiler.GetSnapshot();
            //}
            //finally
            //{
            //    ArrayPool<string>.Shared.Return(errors);
            //}

            //MemoryProfiler.GetSnapshot();
        }
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

        public IEnumerable<Stream> Streams()
        {
            yield return _errorStream;
            //yield return _successStream;
        }

        //[Benchmark(Baseline = true)]
        //[ArgumentsSource(nameof(Streams))]
        //public void ParseResponseOriginal(Stream stream)
        //{
        //    stream.Seek(0, SeekOrigin.Begin);
        //    _ = BulkResponseParser.ParseResponse(stream);
        //}

        [Benchmark]
        [ArgumentsSource(nameof(Streams))]
        public async Task ParseResponseNew(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);

            _ = await BulkResponseParserNew.FromStreamAsync(stream);
        }

        [Benchmark]
        [ArgumentsSource(nameof(Streams))]
        public async Task ParseResponseNewV2(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);

            var errors = ArrayPool<string>.Shared.Rent(128); // can be rented to the size of the bulk index operation

            try
            {
                var totalErrors = await BulkResponseParserNewV2.ParseFromStreamAsync(stream, errors);
            }
            finally
            {
                ArrayPool<string>.Shared.Return(errors);
            }
        }
    }
}
