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
        static void Main(string[] args)
        {
            _ = BenchmarkRunner.Run<CloudFrontParserBenchmarks>();
        }
    }

    [MemoryDiagnoser]
    public class CloudFrontParserBenchmarks
    {
        [Benchmark(Baseline = true)]
        public async Task Original()
        {
            var directoryPath = Path.GetDirectoryName(Assembly.GetAssembly(typeof(CloudFrontRecord)).Location);
            var filePath = Path.Combine(directoryPath, "sample-cloudfront-access-logs.gz");

            for (var i = 0; i < 75; i++)
            {
                _ = await CloudFrontParser.ParseAsync(filePath);
            }
        }

        [Benchmark]
        public async Task Optimised()
        {
            var directoryPath = Path.GetDirectoryName(Assembly.GetAssembly(typeof(CloudFrontRecord)).Location);
            var filePath = Path.Combine(directoryPath, "sample-cloudfront-access-logs.gz");          

            for (var i = 0; i < 75; i++)
            {
                var newData = ArrayPool<CloudFrontRecordStruct>.Shared.Rent(10000);

                try
                {
                    await CloudFrontParserNew.ParseAsync(filePath, newData);
                }
                finally
                {
                    ArrayPool<CloudFrontRecordStruct>.Shared.Return(newData, clearArray: true);
                }
            }
        }        
    }
}
