using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TinyCsvParser;
using TinyCsvParser.Mapping;

namespace CloudfrontLogParserDemo
{
    public static class CloudFrontParser
    {
        public static async Task<IEnumerable<CloudFrontRecord>> ParseAsync(string filePath)
        {     
            if (File.Exists(filePath))
            {   
                using (var fileStream = File.OpenRead(filePath))
                using (var decompressionStream = new GZipStream(fileStream, CompressionMode.Decompress))
                using (var decompressedStream = new MemoryStream())
                {
                    await decompressionStream.CopyToAsync(decompressedStream);

                    // allocations everywhere!!
                    var data = Encoding.UTF8.GetString(decompressedStream.ToArray());

                    if (!string.IsNullOrEmpty(data))
                    {
                        var parser = new CloudWatchLogParser();
                        var results = parser.Parse(data);
                        return results;
                    }
                }
            }
            
            return Enumerable.Empty<CloudFrontRecord>();
        }

        private class CloudWatchLogParser
        {
            private readonly CsvParser<CloudFrontRecord> _csvParser;

            public CloudWatchLogParser()
            {
                var csvParserOptions = new CsvParserOptions(true, "#", 
                    new TinyCsvParser.Tokenizer.RFC4180.RFC4180Tokenizer(
                        new TinyCsvParser.Tokenizer.RFC4180.Options('"', '\\', '\t')));

                var csvMapper = new EmailBeaconLogRecordMapping();

                _csvParser = new CsvParser<CloudFrontRecord>(csvParserOptions, csvMapper);
            }

            public IEnumerable<CloudFrontRecord> Parse(string contents)
            {
                try
                {
                    var results = _csvParser
                        .ReadFromString(new CsvReaderOptions(new[] { "\n" }), contents)
                        .Where(x => x.IsValid)
                        .Select(x => x.Result)
                        .ToArray();

                    return results;
                }
                catch (Exception)
                {
                    return Enumerable.Empty<CloudFrontRecord>();
                }
            }

            private class EmailBeaconLogRecordMapping : CsvMapping<CloudFrontRecord>
            {
                public EmailBeaconLogRecordMapping()
                {
                    base.MapProperty(0, x => x.Date);
                    base.MapProperty(1, x => x.Time);
                    base.MapProperty(10, x => x.UserAgent);
                }
            }
        }
    }
}
