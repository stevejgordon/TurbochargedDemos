using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Buffers;
//using JetBrains.Profiler.Windows.Api;
using System;

namespace CloudfrontLogParserDemo
{
    class Program
    {
        static async Task Main()
        {
            var directoryPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var filePath = Path.Combine(directoryPath, "sample-cloudfront-access-logs.gz");

            //if (MemoryProfiler.IsActive && MemoryProfiler.CanControlAllocations)
            //    MemoryProfiler.EnableAllocations();

            // ORIGINAL

            //MemoryProfiler.Dump();

            //for (int i = 0; i < 5; i++)
            //{
            //    await OldCloudFrontParser.ParseAsync(filePath);
            //}

            //MemoryProfiler.Dump();

            // NEW

            var pool = ArrayPool<CloudFrontRecord>.Shared;

            var array = pool.Rent(1000);
            pool.Return(array);

            //MemoryProfiler.Dump();
            
            for (int i = 0; i < 5; i++)
            {
                var newData = ArrayPool<CloudFrontRecord>.Shared.Rent(1000);

                try
                {
                    //MemoryProfiler.Dump();

                    await CloudFrontParserNew.ParseAsync(filePath, newData);

                    //MemoryProfiler.Dump();
                }
                finally
                {
                    ArrayPool<CloudFrontRecord>.Shared.Return(newData, clearArray: true);
                }
            }

            //MemoryProfiler.Dump();

            //Console.ReadKey();
        }
    }
}
