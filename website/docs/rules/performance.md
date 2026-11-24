---
sidebar_position: 2
title: Performance rules
---

# Performance rules

The family has two groups. Both honour `heimdall-baseline.json` and the exemptions declared in `architecture.json`: a `type` exemption suppresses every performance rule for that type, and a file-level baseline entry suppresses every performance rule in that file.

**Allocation rules** (`PERF1001`–`PERF1012`) detect a deterministic allocation pattern. They do not try to guess whether code is on a hot path: if the pattern is present, it allocates. Severity defaults to `warning`. `PERF1001` and `PERF1007` default to `info` because the .NET 9 JIT already eliminates the allocation in the common case (an escaping array and a non-constant flag argument are still reported). `PERF1006` also defaults to `info`: the benchmark shows no speed advantage, only the testability benefit.

**Context-aware rules** (`PERF2001`–`PERF2008`) depend on where the code runs. They default to `info` and, where relevant, only fire inside loops, `[HotPath]` methods, or framework entry points such as controllers and hosted services. `PERF2001` is suppressed inside `[ColdPath]` methods; `PERF2004` defaults to `warning` because blocking on a task can deadlock regardless of context.

## PERF1001 — Empty params array

```csharp
Log("ready"); // allocates an empty object[] on every call

public static void Log(string format, params object[] args) { }
```

Use an overload without `params`, or pass `Array.Empty<object>()`.

The rule inspects the implicit array creation operation that the compiler inserts, so an explicit `Log("ready", new object[0])` is not reported.

The compiler emits the array allocation in IL, but .NET 9 removes it when the array does not escape. The rule therefore defaults to `info`: it still fires when the array escapes, where the allocation survives. Benchmark: see [benchmarks](https://github.com/rungerheimdall/rungerheimdall/blob/main/docs/benchmarks.md).

## PERF1002 — Enumerable.Count() on a countable collection

```csharp
var count = items.Count(); // enumerates a List<int>
```

Use the `Count` property or the `Length` of the array instead. The rule only reports when the receiver is known to implement `ICollection<T>` or `IReadOnlyCollection<T>`.

## PERF1003 — ToList() immediately followed by foreach

```csharp
foreach (var item in items.ToList()) { }
```

Iterate the source directly.

## PERF1004 — String concatenation inside a loop

```csharp
var result = "";
foreach (var item in items)
{
    result += item; // allocates a new string per iteration
}
```

Use `StringBuilder`. Both `a + b` and `a += b` are detected.

## PERF1005 — Closure capturing the loop variable

```csharp
foreach (var item in items)
{
    Action callback = () => Console.WriteLine(item);
    callback();
}
```

The compiler allocates a closure object on every iteration. Replace the delegate with a plain statement, or move it outside the loop. Measured: 413 ns and 1,944 B versus 8.8 ns and 0 B for the manual loop.

## PERF1006 — System clock on a hot path

```csharp
[HotPath]
public long Stamp() => DateTime.Now.Ticks;
```

Inject `TimeProvider` and call `GetUtcNow()` instead. The rule fires inside `[HotPath]` and framework entry points, inside iterators and inside loops. It defaults to `info`: on .NET 9 the direct clock read is slightly faster, so the benefit is testability rather than throughput.

## PERF1007 — Enum.HasFlag boxing

```csharp
if (options.HasFlag(Options.A)) { }
```

`HasFlag` took an `Enum` argument and boxed it before .NET 5. Use a bitwise comparison:

```csharp
if ((options & Options.A) != 0) { }
```

.NET 5+ treats `HasFlag` as a JIT intrinsic, so this rule defaults to `info`. A bitwise comparison is still faster on older runtimes and when the flag argument is not a constant. Benchmark: see [benchmarks](https://github.com/rungerheimdall/rungerheimdall/blob/main/docs/benchmarks.md).


## PERF1008 — Default comparer for a struct

```csharp
EqualityComparer<Money>.Default.Equals(left, right);
```

`EqualityComparer<T>.Default` and `Comparer<T>.Default` box a struct that implements neither `IEquatable<T>` nor `IComparable<T>`. Implement the generic interface. Measured: 19.4 ns and 48 B versus 0.26 ns and 0 B.

## PERF1009 — Substring used for parsing or comparison

```csharp
int.Parse(value.Substring(0, 2));
value.Substring(0, 2).Equals("ab");
```

`Substring` allocates a new string. Use a span-based overload (`int.Parse(span)`, `span.SequenceEqual(...)`) or `AsSpan()`.

The rule only reports when the result flows directly into a parse or comparison method, so assigning a substring to a local variable is not flagged.

## PERF1010 — Regex constructed inside a method

```csharp
var regex = new Regex("^a");
```

Use `[GeneratedRegex]`:

```csharp
[GeneratedRegex("^a")]
private static partial Regex StartsWithA();
```

Field initializers are not reported because they run once per type load.

## PERF1011 — Non-generic foreach over value types

```csharp
foreach (int item in arrayList) { }
```

`foreach` over `System.Collections.IEnumerable` boxes the enumerator and every element. Use `List<T>` or another generic collection. Measured: 41.7 ns and 48 B versus 5.7 ns and 0 B.

## PERF1012 — ToLower/ToUpper used for comparison

```csharp
if (value.ToLower() == "x") { }
```

Use `StringComparison.OrdinalIgnoreCase`:

```csharp
if (string.Equals(value, "x", StringComparison.OrdinalIgnoreCase)) { }
```

Only comparisons are reported. A standalone `ToLower()` call and the `CultureInfo` overload are ignored.

## PERF2001 — LINQ inside loops

```csharp
foreach (var order in orders)
{
    total += orders.Where(x => x.IsPaid).Count();
}
```

LINQ inside a loop builds a new query and enumerates it on every iteration. Move the query out of the loop or use a manual loop. Rules are skipped inside methods marked `[ColdPath]`.

## PERF2002 — Task.Run inside async methods

```csharp
public async Task Handle()
{
    await Task.Run(() => DoWork());
}
```

`Task.Run` inside an `async` method adds a thread-pool hop without doing CPU-bound work. Await the operation directly.

## PERF2003 — async void

```csharp
public static async void Fire() { await Task.Delay(1); }
```

Exceptions from an `async void` method cannot be observed. Return `Task` instead. Event handlers named `On...` with `(object, EventArgs)` are excluded.

## PERF2004 — Task.Result and Task.Wait()

```csharp
var value = task.Result;
task.Wait();
```

Blocking on a task can deadlock in a synchronization context. Await the task instead.

## PERF2005 — Enumerating an IEnumerable twice

```csharp
foreach (var item in items) { Use(item); }
foreach (var item in items) { Use(item); }
```

If `items` is declared as `IEnumerable<T>`, each foreach runs the query again. Materialize it once or use a concrete collection. `List<T>` and arrays are not reported.

## PERF2006 — Awaiting a ValueTask twice

```csharp
var a = await value;
var b = await value;
```

A `ValueTask` may only be consumed once. Convert it to a `Task` first or await it once.

## PERF2007 — Task.FromResult on hot paths

```csharp
foreach (var item in items)
{
    var task = Task.FromResult(item);
}
```

`Task.FromResult` allocates a new task per call. Cache the completed task or return the value directly. Only reported inside loops or `[HotPath]` methods.

## PERF2008 — Array.Resize inside loops

```csharp
foreach (var item in items)
{
    Array.Resize(ref items, items.Length + 1);
}
```

`Array.Resize` copies the whole array on every call. Use `List<T>` or `ArrayPool<T>`.

## What these rules do not cover

- Boxing of value types in non-generic collections
- Allocations inside `Span` callbacks that need alias analysis
- Interpolated string handlers on custom types