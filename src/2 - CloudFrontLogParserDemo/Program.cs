using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Buffers;

namespace CloudfrontLogParserDemo
{
    class Program
    {
        static async Task Main()
        {
            var directoryPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var filePath = Path.Combine(directoryPath, "sample-cloudfront-access-logs.gz");


            // ORIGINAL


            for (int i = 0; i < 75; i++)
            {
                await CloudFrontParser.ParseAsync(filePath);
            }

            // NEW

            var pool = ArrayPool<CloudFrontRecordStruct>.Shared;

            for (int i = 0; i < 75; i++)
            {
                var newData = pool.Rent(10000);

                try
                {
                    await CloudFrontParserNew.ParseAsync(filePath, newData);
                }
                finally
                {
                   pool.Return(newData, clearArray: true);
                }
            }

        }
    }
}
