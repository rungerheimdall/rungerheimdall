---
title: The Allocations the Compiler Cannot See
date: 2026-11-24
description: Four allocation patterns, measured. Plus two rules that the JIT made obsolete.
---

A profiler tells you where memory went. A compiler tells you where it will go. The gap between them is the set of allocations that are statically provable but invisible to the language.

Here are four, measured with BenchmarkDotNet on .NET 9.0.14, Windows 11, X64 RyuJIT AVX2. ShortRun, so treat the numbers as ratios rather than absolutes.

## `Enumerable.Count()` on a list

| Method | Mean | Ratio | Allocated |
|--------|------|-------|-----------|
| `items.Count()` | 1.90 ns | 1.00 | 0 B |
| `items.Count` | 0.14 ns | 0.07 | 0 B |

No allocation either way. The cost is enumerator dispatch, and it is 13x.

## String concatenation in a loop

| Method | Mean | Ratio | Allocated |
|--------|------|-------|-----------|
| `result += item` | 937 ns | 1.00 | 5,456 B |
| `StringBuilder` | 266 ns | 0.28 | 752 B |

3.5x faster, 7.3x less memory. This is the one that shows up in production as GC pressure rather than a slow method.

## `Substring` used for parsing

| Method | Mean | Ratio | Allocated |
|--------|------|-------|-----------|
| `int.Parse(value.Substring(0, 2))` | 15.8 ns | 1.00 | 32 B |
| `int.Parse(value.AsSpan(0, 2))` | 9.3 ns | 0.59 | 0 B |

1.7x faster and zero allocation. The analyzer only reports when the substring flows directly into a parse or comparison, because a substring stored in a local is a legitimate value.

## `ToLower()` used for comparison

| Method | Mean | Ratio | Allocated |
|--------|------|-------|-----------|
| `value.ToLower() == "hello"` | 28.1 ns | 1.00 | 32 B |
| `string.Equals(..., OrdinalIgnoreCase)` | 1.0 ns | 0.04 | 0 B |

28x faster, zero allocation. This one is in almost every code review, and almost never gets fixed.

## Two rules the JIT made obsolete

The empty `params` array is allocated in IL, but .NET 9 removes it when the array does not escape:

| Method | Mean | Allocated |
|--------|------|-----------|
| `Log(format)` | 1.14 ns | 0 B |
| `Log(format, Array.Empty<object>())` | 1.46 ns | 0 B |

`Enum.HasFlag` was a classic boxing example. .NET 5+ treats it as a JIT intrinsic:

| Method | Mean | Allocated |
|--------|------|-----------|
| `options.HasFlag(flag)` | 0.033 ns | 0 B |
| `(options & flag) != 0` | 0.065 ns | 0 B |

Both rules are still shipped, both are `Info`, and both are documented as targeting older runtimes. A tool that reports a cost the runtime already removed teaches its users to ignore it.

## The rule for building rules

Measure before you ship. The benchmark is not marketing material; it is the test that decides whether the rule deserves a warning.