using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace RungerHeimdall.Benchmarks;

[MemoryDiagnoser]
public class ParamsArrayBenchmarks
{
    private string _format = "ready";
    private object[] _empty = Array.Empty<object>();

    [Benchmark(Baseline = true)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int EmptyParams() => Log(_format);

    [Benchmark]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int ExplicitEmptyArray() => Log(_format, _empty);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Log(string format, params object[] args) => format.Length + args.Length;
}

[MemoryDiagnoser]
public class CountBenchmarks
{
    private List<int> _items = Enumerable.Range(0, 100).ToList();

    [Benchmark(Baseline = true)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int LinqCount() => _items.Count();

    [Benchmark]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int PropertyCount() => _items.Count;
}

[MemoryDiagnoser]
public class StringConcatBenchmarks
{
    private string[] _items = Enumerable.Range(0, 50).Select(value => value.ToString()).ToArray();

    [Benchmark(Baseline = true)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public string ConcatInLoop()
    {
        var result = string.Empty;
        foreach (var item in _items)
        {
            result += item;
        }

        return result;
    }

    [Benchmark]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public string StringBuilderInLoop()
    {
        var builder = new StringBuilder();
        foreach (var item in _items)
        {
            builder.Append(item);
        }

        return builder.ToString();
    }
}

[MemoryDiagnoser]
public class HasFlagBenchmarks
{
    private Options _options = Options.A | Options.B;
    private Options _flag = Options.A;

    [Benchmark(Baseline = true)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool HasFlag() => _options.HasFlag(_flag);

    [Benchmark]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool Bitwise() => (_options & _flag) != 0;

    [Flags]
    public enum Options
    {
        None = 0,
        A = 1,
        B = 2,
    }
}

[MemoryDiagnoser]
public class SubstringBenchmarks
{
    private string _value = "1234567890";

    [Benchmark(Baseline = true)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int SubstringParse() => int.Parse(_value.Substring(0, 2));

    [Benchmark]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int SpanParse() => int.Parse(_value.AsSpan(0, 2));
}

[MemoryDiagnoser]
public class ToLowerBenchmarks
{
    private string _value = "HELLO";

    [Benchmark(Baseline = true)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool ToLowerCompare() => _value.ToLower() == "hello";

    [Benchmark]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool OrdinalIgnoreCase() => string.Equals(_value, "hello", StringComparison.OrdinalIgnoreCase);
}