# Benchmarks

Measured with BenchmarkDotNet 0.14.0, `ShortRun`, .NET 9.0.14, Windows 11, X64 RyuJIT AVX2.

Run order: baseline first, alternative second. `Ratio` is relative to the baseline. `Allocated`
is managed allocation per operation.

## Confirmed

### PERF1002 — `Enumerable.Count()` vs the `Count` property

| Method | Mean | Ratio | Allocated |
|--------|------|-------|-----------|
| `LinqCount` | 1.90 ns | 1.00 | 0 B |
| `PropertyCount` | 0.14 ns | 0.07 | 0 B |

The LINQ call is about 13× slower. Allocation is zero either way; the cost is the enumerator
dispatch, not the heap.

### PERF1003 — `ToList()` before `foreach`

| Method | Mean | Ratio | Allocated |
|--------|------|-------|-----------|
| `MaterializeThenForeach` | 132.1 ns | 1.00 | 456 B |
| `DirectForeach` | 66.3 ns | 0.50 | 0 B |

Materialising copies the whole list and doubles the time. Iterating the source directly is free.

### PERF1004 — String concatenation inside a loop

| Method | Mean | Ratio | Allocated |
|--------|------|-------|-----------|
| `ConcatInLoop` | 937.0 ns | 1.00 | 5,456 B |
| `StringBuilderInLoop` | 265.8 ns | 0.28 | 752 B |

3.5× faster and 7.3× less allocated memory with `StringBuilder`.

### PERF1005 — Closure capturing the loop variable

| Method | Mean | Ratio | Allocated |
|--------|------|-------|-----------|
| `ClosurePerIteration` | 413.1 ns | 1.00 | 1,944 B |
| `NoClosure` | 8.8 ns | 0.02 | 0 B |

The closure allocates on every iteration. The manual loop is 47× faster and allocation-free.

### PERF1008 — Default comparer for a struct without `IEquatable<T>`

| Method | Mean | Ratio | Allocated |
|--------|------|-------|-----------|
| `PlainComparer` | 19.40 ns | 1.00 | 48 B |
| `EquatableComparer` | 0.26 ns | 0.01 | 0 B |

`EqualityComparer<T>.Default` boxes the struct on every call. Implementing `IEquatable<T>` removes
the allocation and is about 74× faster.

### PERF1009 — `Substring` vs `Span` for parsing

| Method | Mean | Ratio | Allocated |
|--------|------|-------|-----------|
| `SubstringParse` | 15.8 ns | 1.00 | 32 B |
| `SpanParse` | 9.3 ns | 0.59 | 0 B |

1.7× faster and zero allocation.

### PERF1010 — `new Regex` vs a cached instance

| Method | Mean | Ratio | Allocated |
|--------|------|-------|-----------|
| `NewRegex` | 727.9 ns | 1.00 | 1,944 B |
| `CachedRegex` | 53.5 ns | 0.07 | 0 B |

Constructing the same pattern inside a method rebuilds the parse tree per call. 13.6× slower and
1.9 KB allocated.

### PERF1011 — `foreach` over a non-generic collection of value types

| Method | Mean | Ratio | Allocated |
|--------|------|-------|-----------|
| `NonGeneric` | 41.69 ns | 1.00 | 48 B |
| `Generic` | 5.69 ns | 0.14 | 0 B |

`foreach` over `IEnumerable` boxes the enumerator and every element. The generic list is 7.3×
faster and allocation-free.

### PERF1012 — `ToLower()` vs `OrdinalIgnoreCase`

| Method | Mean | Ratio | Allocated |
|--------|------|-------|-----------|
| `ToLowerCompare` | 28.1 ns | 1.00 | 32 B |
| `OrdinalIgnoreCase` | 1.0 ns | 0.04 | 0 B |

28× faster and zero allocation.

### PERF2001 — LINQ inside a loop

| Method | Mean | Ratio | Allocated |
|--------|------|-------|-----------|
| `LinqInsideLoop` | 9,798.6 ns | 1.00 | 7,200 B |
| `QueryOutsideLoop` | 268.3 ns | 0.03 | 328 B |

Re-running the query 100 times rebuilds the enumerator and the delegate each pass. 36× slower and
22× more allocated.

### PERF2002 — `Task.Run` inside an `async` method

| Method | Mean | Ratio | Allocated |
|--------|------|-------|-----------|
| `TaskRunInsideAsync` | 1,086.1 ns | 1.00 | 167 B |
| `AwaitDirect` | 24.7 ns | 0.02 | 144 B |

The extra thread-pool hop costs 44× in time. The allocation differs little; the cost is scheduling.

### PERF2004 — `Task.Result` vs `await`

| Method | Mean | Ratio | Allocated |
|--------|------|-------|-----------|
| `BlockingResult` | 0.20 ns | 1.00 | 0 B |
| `AwaitTask` | 19.84 ns | 99.22 | 72 B |

This benchmark measures an already-completed task, where blocking is cheaper. The rule is not
about throughput: blocking can deadlock in a synchronization context and hides the asynchronous
contract. It stays at `warning`.

### PERF2005 — Enumerating an `IEnumerable<T>` twice

| Method | Mean | Ratio | Allocated |
|--------|------|-------|-----------|
| `EnumerateTwice` | 508.9 ns | 1.00 | 96 B |
| `MaterializeOnce` | 266.5 ns | 0.52 | 456 B |

Materialising once is 1.9× faster because the query runs once, but it allocates 4.75× more. The
rule is about repeated work, not always about allocation.

### PERF2007 — `Task.FromResult` inside a loop

| Method | Mean | Ratio | Allocated |
|--------|------|-------|-----------|
| `FromResultInLoop` | 707.2 ns | 1.00 | 6,552 B |
| `DirectValue` | 48.3 ns | 0.07 | 0 B |

Each call allocates a new task. 14.6× slower and 6.4 KB more per 100 iterations.

### PERF2008 — `Array.Resize` inside a loop

| Method | Mean | Ratio | Allocated |
|--------|------|-------|-----------|
| `ResizeInLoop` | 731.6 ns | 1.00 | 6,400 B |
| `ListAdd` | 178.6 ns | 0.24 | 648 B |

`Array.Resize` copies the whole array per call: 4.1× slower and 9.9× more allocated.

## Not confirmed on .NET 9

### PERF1001 — empty `params` array

| Method | Mean | Allocated |
|--------|------|-----------|
| `EmptyParams` | 1.14 ns | 0 B |
| `ExplicitEmptyArray` | 1.46 ns | 0 B |

The JIT removes the empty array when it does not escape the call. The allocation exists in IL,
but not in the measured machine code. **The rule was downgraded to `Info`.** It remains useful
for older target frameworks, AOT builds with different inlining decisions, and code paths where
the array escapes.

### PERF1006 — `DateTime.UtcNow` vs `TimeProvider`

| Method | Mean | Ratio | Allocated |
|--------|------|-------|-----------|
| `DateTimeUtcNow` | 30.97 ns | 1.00 | 0 B |
| `TimeProviderUtcNow` | 35.20 ns | 1.14 | 0 B |

Neither form allocates, and the direct clock read is 14% faster in this run. **The rule was
downgraded to `Info`.** Its value is testability and injectability, not speed: a `TimeProvider`
lets tests replace the clock, and the rule is opt-in context (hot paths or loops only).

### PERF1007 — `Enum.HasFlag`

| Method | Mean | Allocated |
|--------|------|-----------|
| `HasFlag` | 0.033 ns | 0 B |
| `Bitwise` | 0.065 ns | 0 B |

.NET 5+ recognizes `Enum.HasFlag` as a JIT intrinsic and removes the boxing. No allocation was
observed; the intrinsic form is even slightly faster than the manual bitwise comparison in this
short run. **The rule was downgraded to `Info`.** It remains useful for older runtimes and for
readers who want the intent made explicit.

## Not benchmarked

### PERF2003 — `async void`

An `async void` method cannot be awaited, so BenchmarkDotNet cannot measure it and the fix
changes the method contract rather than the instruction mix. The rule exists because exceptions
from `async void` are unobservable and the caller cannot apply back-pressure.

### PERF2006 — awaiting a `ValueTask` twice

A second `await` on a `ValueTask` is a correctness defect, not a slower alternative: behaviour
depends on whether the source completed synchronously and which pool the backing object returned
to. There is no safe baseline to compare against.

## Reproducing

```bash
dotnet run -c Release --project benchmarks/RungerHeimdall.Benchmarks -- --filter '*' --job short
```

Artifacts are written to `BenchmarkDotNet.Artifacts/` and are not committed.

Short runs are noisy. Numbers cited in rule documentation should come from a full run with
`--job default` before a stable release.