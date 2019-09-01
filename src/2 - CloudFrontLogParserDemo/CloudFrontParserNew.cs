using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;

namespace CloudfrontLogParserDemo
{
    public static class CloudFrontParserNew
    {
        public static async Task<int> ParseAsync(string filePath, CloudFrontRecordStruct[] items)
        {
            var position = 0;

            if (File.Exists(filePath))
            {
                await using var fileStream = File.OpenRead(filePath);
                await using var decompressionStream = new GZipStream(fileStream, CompressionMode.Decompress);

                var pipeReader = PipeReader.Create(decompressionStream);
                   
                while (true)
                {
                    var result = await pipeReader.ReadAsync();
                                       
                    var buffer = result.Buffer;

                    var sequencePosition = ParseLines(items, ref buffer, ref position);
                    
                    pipeReader.AdvanceTo(sequencePosition, buffer.End);

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }

                pipeReader.Complete();
            }

            return position;
        }

        private static SequencePosition ParseLines(CloudFrontRecordStruct[] itemsArray, ref ReadOnlySequence<byte> buffer, ref int position)
        {
            var newLine = Encoding.UTF8.GetBytes(Environment.NewLine).AsSpan();

            var reader = new SequenceReader<byte>(buffer);

            while (!reader.End)
            {
                if (reader.TryReadToAny(out ReadOnlySpan<byte> line, newLine, true))
                {
                    break;
                }

                var parsedLine = LineParser.ParseLine(line);

                if (parsedLine.HasValue) itemsArray[position++] = parsedLine.Value;
            }

            return reader.Position;
        }

        private static class LineParser
        {
            private const byte Tab = (byte)'\t', Hash = (byte)'#';

            public static CloudFrontRecordStruct? ParseLine(ReadOnlySpan<byte> line)
            {
                if (line[0] == Hash) return null;

                var tabCount = 1;

                var record = new CloudFrontRecordStruct();

                while (tabCount < 12)
                {
                    var tabAt = line.IndexOf(Tab);

                    if (tabCount == 1)
                    {
                        {
                            var value = Encoding.UTF8.GetString(line.Slice(0, tabAt));
                            record.Date = value;
                        }
                    }
                    else if (tabCount == 2)
                    {
                        {
                            var value = Encoding.UTF8.GetString(line.Slice(0, tabAt));
                            record.Time = value;
                        }
                    }
                    else if (tabCount == 11)
                    {
                        {
                            var value = Encoding.UTF8.GetString(line.Slice(0, tabAt));
                            record.UserAgent = value;
                        }
                    }

                    line = line.Slice(tabAt + 1);

                    tabCount++;
                }

                return record;
            }
        }
    }
}
