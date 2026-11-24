---
sidebar_position: 2
title: Performance rules
---

# Performance rules

Every rule in this family detects a deterministic allocation pattern. They do not try to guess whether code is on a hot path. If a pattern is present, it allocates.

All rules default to `warning`.

## PERF1001 — Empty params array

```csharp
Log("ready"); // allocates an empty object[] on every call

public static void Log(string format, params object[] args) { }
```

Use an overload without `params`, or pass `Array.Empty<object>()`.

The rule inspects the implicit array creation operation that the compiler inserts, so an explicit `Log("ready", new object[0])` is not reported.

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

## PERF1007 — Enum.HasFlag boxing

```csharp
if (options.HasFlag(Options.A)) { }
```

`HasFlag` boxes its argument. Use a bitwise comparison:

```csharp
if ((options & Options.A) != 0) { }
```

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