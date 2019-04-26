using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;

namespace MiscDemos
{
    class Program
    {
        public static void Main(string[] args) =>
            _ = BenchmarkRunner.Run<ArrayBenchmarks>();
    }

    [MemoryDiagnoser]
    public class ArrayBenchmarks
    {
        private int[] _myArray;

        private static readonly Consumer Consumer = new Consumer();

        [Params(10, 1000, 10000)]
        public int Size { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _myArray = new int[Size];

            for (var i = 0; i < Size; i++)
                _myArray[i] = i;
        }

        [Benchmark(Baseline = true)]
        public void Original() => _myArray.Skip(Size / 2).Take(Size / 4).Consume(Consumer);

        [Benchmark]
        public int[] ArrayCopy()
        {
            var newArray = new int[Size / 4];
            Array.Copy(_myArray, Size / 2, newArray, 0, Size / 4);
            return newArray;
        }

        [Benchmark]
        public void NewArray()
        {
            var newArray = new int[Size / 4];

            for (var i = 0; i < Size / 4; i++)
                newArray[i] = _myArray[(Size / 2) + i];
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
