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

## What these rules do not cover

- LINQ inside loops (needs a hot-path model)
- `Task.Run` inside `async` methods (needs context)
- `ValueTask` reuse
- Boxing of value types in non-generic collections

Those require call-site context and are planned for the hot-path analyzer.