``` ini

BenchmarkDotNet=v0.11.5, OS=Windows 10.0.17763.437 (1809/October2018Update/Redstone5)
Intel Core i7-8700 CPU 3.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=3.0.100-preview5-011568
  [Host]     : .NET Core 3.0.0-preview5-27626-15 (CoreCLR 4.6.27622.75, CoreFX 4.700.19.22408), 64bit RyuJIT
  DefaultJob : .NET Core 3.0.0-preview5-27626-15 (CoreCLR 4.6.27622.75, CoreFX 4.700.19.22408), 64bit RyuJIT


```
|    Method |     Mean |    Error |   StdDev |      Gen 0 |      Gen 1 | Gen 2 | Allocated |
|---------- |---------:|---------:|---------:|-----------:|-----------:|------:|----------:|
| Optimised | 437.4 ms | 3.229 ms | 3.020 ms | 36000.0000 | 15000.0000 |     - |   2.77 KB |
