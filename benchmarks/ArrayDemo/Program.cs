using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace MiscDemos
{
    class Program
    {
        public static void Main(string[] args)
        {
            var config = ManualConfig
                .Create(DefaultConfig.Instance)
                .AddDiagnoser(MemoryDiagnoser.Default)
                .WithSummaryStyle(new SummaryStyle(null, false, null, null, 
                    ratioStyle: RatioStyle.Percentage));

            _ = BenchmarkRunner.Run<ArrayBenchmarks>(config);
        }
    }

    [MemoryDiagnoser]
    public class Testing
    {
        private static readonly char[] _jsonSuffix = ['.', 'j', 's', 'o', 'n'];
        private static ReadOnlySpan<char> JsonSuffix => _jsonSuffix;
        private static ReadOnlySpan<char> JsonSuffix2 => ['.', 'j', 's', 'o', 'n'];

        [Benchmark]
        public ReadOnlySpan<char> Original() => JsonSuffix;

        [Benchmark]
        public ReadOnlySpan<char> New() => JsonSuffix2;
    }

    [MemoryDiagnoser]
    public class NameParserBenchmarks
    {
        private const string FullName = "Steve J Gordon";
        private static readonly NameParser Parser = new NameParser();

        [Benchmark]
        public void GetLastName()
        {
            Parser.GetLastName(FullName);
        }
    }

    internal sealed class NameParser
    {
        internal string GetLastName(string fullName)
        {
            var parts = fullName.Split(' ');
            return parts.Last();
        }
    }

    [MemoryDiagnoser]
    public class ArrayBenchmarks
    {
        private int[] _myArray;

        [Params(100, 1000, 10000)]
        public int Size { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _myArray = new int[Size];

            for (var i = 0; i < Size; i++)
                _myArray[i] = i;
        }

        [Benchmark(Baseline = true)]
        public int[] Original() => _myArray.Skip(Size / 2).Take(Size / 4).ToArray();

        [Benchmark]
        public int[] ArrayCopy()
        {
            var newArray = new int[Size / 4];
            Array.Copy(_myArray, Size / 2, newArray, 0, Size / 4);
            return newArray;
        }

        [Benchmark]
        public Span<int> Span() => _myArray.AsSpan().Slice(Size / 2, Size / 4);
    }

    [MemoryDiagnoser]
    public class SpanStringBenchmarks
    {
        const string Alphabet = "abcdefghijklmnopqrstuvwxyz";

        private string _myString;

        [Params(10, 1000, 10000)]
        public int CharactersCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            var random = new Random();

            _myString = string.Create(CharactersCount, (alphabet: Alphabet, random), (span, state) =>
            {
                for (var i = 0; i < span.Length; i++)
                    span[i] = state.alphabet[state.random.Next(state.alphabet.Length)];
            });
        }

        [Benchmark(Baseline = true)]
        public string Substring() =>
           _myString.Substring(0, CharactersCount / 2);

        [Benchmark]
        public ReadOnlySpan<char> Span() =>
            _myString.AsSpan().Slice(0, CharactersCount / 2);
    }
}
