using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BulkResponseParsingDemo;

namespace BulkResponseParserBenchmarks
{
    public class Program
    {
        public static void Main(string[] args) => _ = BenchmarkRunner.Run<BulkResponseParserBenchmarks>();   
        
        //public static async Task Main()
        //{
        //    using (FileStream fs = File.OpenRead(AppDomain.CurrentDomain.BaseDirectory + "/SuccessResponseSample.txt"))
        //    {
        //        var result = await BulkResponseParserNew.FromStreamAsync(fs);
        //    }
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

        public IEnumerable<Stream> Streams()
        {
            yield return _errorStream;
            yield return _successStream;
        }

        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(Streams))]
        public void ParseResponseOriginal(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            _ = BulkResponseParser.ParseResponse(stream);
        }

        [Benchmark]
        [ArgumentsSource(nameof(Streams))]
        public async Task ParseResponseNew(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            _ = await BulkResponseParserNew.FromStreamAsync(stream);
        }
    }
}
