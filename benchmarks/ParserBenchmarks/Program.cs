using System.Buffers;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using CloudfrontLogParserDemo;

namespace CloudfrontLogParserBenchmarks
{
    class Program
    {
        //static void Main(string[] args) => _ = BenchmarkRunner.Run<CloudFrontParserBenchmarks>();

        static async Task Main()
        {
            var directoryPath = Path.GetDirectoryName(Assembly.GetAssembly(typeof(CloudFrontRecord)).Location);
            var filePath = Path.Combine(directoryPath, "sample-cloudfront-access-logs.gz");

            var newData = ArrayPool<CloudFrontRecordStruct>.Shared.Rent(10000);

            try
            {
                var items = await CloudFrontParserNew.ParseAsync(filePath, newData);
            }
            finally
            {
                ArrayPool<CloudFrontRecordStruct>.Shared.Return(newData);
            }
        }
    }

    [MemoryDiagnoser]
    public class CloudFrontParserBenchmarks
    {
        private string _filePath;

        [GlobalSetup]
        public void Setup()
        {
            var directoryPath = Path.GetDirectoryName(Assembly.GetAssembly(typeof(CloudFrontRecord)).Location);
            _filePath = Path.Combine(directoryPath, "sample-cloudfront-access-logs.gz");
        }

        [Benchmark(Baseline = true)]
        public async Task Original()
        {           
            for (var i = 0; i < 75; i++)
                _ = await CloudFrontParser.ParseAsync(_filePath);
        }

        [Benchmark]
        public async Task Optimised()
        {
            for (var i = 0; i < 75; i++)
            {
                var newData = ArrayPool<CloudFrontRecordStruct>.Shared.Rent(10000);

                try
                {
                    var items = await CloudFrontParserNew.ParseAsync(_filePath, newData);
                }
                finally
                {
                    ArrayPool<CloudFrontRecordStruct>.Shared.Return(newData);
                }
            }
        }        
    }
}
