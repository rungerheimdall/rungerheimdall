using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
[MemoryDiagnoser]
public class ToListForeachBenchmarks
{
    private List<int> _items = Enumerable.Range(0, 100).ToList();

    [Benchmark(Baseline = true)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int MaterializeThenForeach()
    {
        var total = 0;
        foreach (var item in _items.ToList())
        {
            total += item;
        }

        return total;
    }

    [Benchmark]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int DirectForeach()
    {
        var total = 0;
        foreach (var item in _items)
        {
            total += item;
        }

        return total;
    }
}

[MemoryDiagnoser]
public class ClosureBenchmarks
{
    private string[] _items = Enumerable.Range(0, 20).Select(value => value.ToString()).ToArray();

    [Benchmark(Baseline = true)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int ClosurePerIteration()
    {
        var count = 0;
        foreach (var item in _items)
        {
            Action callback = () => count += item.Length;
            callback();
        }

        return count;
    }

    [Benchmark]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int NoClosure()
    {
        var count = 0;
        foreach (var item in _items)
        {
            count += item.Length;
        }

        return count;
    }
}

[MemoryDiagnoser]
public class ClockBenchmarks
{
    private TimeProvider _time = TimeProvider.System;

    [Benchmark(Baseline = true)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public long DateTimeUtcNow() => DateTime.UtcNow.Ticks;

    [Benchmark]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public long TimeProviderUtcNow() => _time.GetUtcNow().Ticks;
}

[MemoryDiagnoser]
public class DefaultComparerBenchmarks
{
    private Plain _plainLeft = new Plain { X = 1 };
    private Plain _plainRight = new Plain { X = 1 };
    private Equatable _left = new Equatable { X = 1 };
    private Equatable _right = new Equatable { X = 1 };

    [Benchmark(Baseline = true)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool PlainComparer() => EqualityComparer<Plain>.Default.Equals(_plainLeft, _plainRight);

    [Benchmark]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool EquatableComparer() => EqualityComparer<Equatable>.Default.Equals(_left, _right);

    public struct Plain
    {
        public int X;
    }

    public struct Equatable : IEquatable<Equatable>
    {
        public int X;

        public bool Equals(Equatable other) => X == other.X;

        public override bool Equals(object? obj) => obj is Equatable other && Equals(other);

        public override int GetHashCode() => X;
    }
}

[MemoryDiagnoser]
public class RegexBenchmarks
{
    private static readonly Regex Cached = new Regex("^a");
    private string _value = "abc";

    [Benchmark(Baseline = true)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool NewRegex() => new Regex("^a").IsMatch(_value);

    [Benchmark]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool CachedRegex() => Cached.IsMatch(_value);
}

[MemoryDiagnoser]
public class NonGenericForeachBenchmarks
{
    private ArrayList _arrayList = new ArrayList { 1, 2, 3, 4, 5, 6, 7, 8 };
    private List<int> _list = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 };

    [Benchmark(Baseline = true)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int NonGeneric()
    {
        var total = 0;
        foreach (int item in _arrayList)
        {
            total += item;
        }

        return total;
    }

    [Benchmark]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int Generic()
    {
        var total = 0;
        foreach (var item in _list)
        {
            total += item;
        }

        return total;
    }
}
[MemoryDiagnoser]
public class LinqInLoopBenchmarks
{
    private List<int> _items = Enumerable.Range(0, 100).ToList();

    [Benchmark(Baseline = true)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int LinqInsideLoop()
    {
        var total = 0;
        for (var index = 0; index < 100; index++)
        {
            total += _items.Where(item => item > 50).Count();
        }

        return total;
    }

    [Benchmark]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int QueryOutsideLoop()
    {
        var filtered = _items.Where(item => item > 50).ToList();
        var total = 0;
        for (var index = 0; index < 100; index++)
        {
            total += filtered.Count;
        }

        return total;
    }
}

[MemoryDiagnoser]
public class TaskRunBenchmarks
{
    [Benchmark(Baseline = true)]
    public async Task<int> TaskRunInsideAsync() => await Task.Run(() => 42);

    [Benchmark]
    public async Task<int> AwaitDirect() => await Task.FromResult(42);
}

[MemoryDiagnoser]
public class BlockingTaskBenchmarks
{
    private Task<int> _task = Task.FromResult(42);

    [Benchmark(Baseline = true)]
    public int BlockingResult() => _task.Result;

    [Benchmark]
    public async Task<int> AwaitTask() => await _task;
}

[MemoryDiagnoser]
public class MultipleEnumerationBenchmarks
{
    private IEnumerable<int> _query = Enumerable.Range(0, 100).Select(value => value);

    [Benchmark(Baseline = true)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int EnumerateTwice()
    {
        var total = 0;
        foreach (var item in _query)
        {
            total += item;
        }

        foreach (var item in _query)
        {
            total += item;
        }

        return total;
    }

    [Benchmark]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int MaterializeOnce()
    {
        var items = _query.ToList();
        var total = 0;
        foreach (var item in items)
        {
            total += item;
        }

        foreach (var item in items)
        {
            total += item;
        }

        return total;
    }
}

[MemoryDiagnoser]
public class TaskFromResultBenchmarks
{
    [Benchmark(Baseline = true)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int FromResultInLoop()
    {
        var total = 0;
        for (var index = 0; index < 100; index++)
        {
            total += Task.FromResult(index).Result;
        }

        return total;
    }

    [Benchmark]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int DirectValue()
    {
        var total = 0;
        for (var index = 0; index < 100; index++)
        {
            total += index;
        }

        return total;
    }
}

[MemoryDiagnoser]
public class ArrayResizeBenchmarks
{
    private int[] _source = Enumerable.Range(0, 50).ToArray();

    [Benchmark(Baseline = true)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int ResizeInLoop()
    {
        var target = Array.Empty<int>();
        foreach (var item in _source)
        {
            Array.Resize(ref target, target.Length + 1);
        }

        return target.Length;
    }

    [Benchmark]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int ListAdd()
    {
        var target = new List<int>();
        foreach (var item in _source)
        {
            target.Add(item);
        }

        return target.Count;
    }
}