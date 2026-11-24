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

### PERF1004 — String concatenation inside a loop

| Method | Mean | Ratio | Allocated |
|--------|------|-------|-----------|
| `ConcatInLoop` | 937.0 ns | 1.00 | 5,456 B |
| `StringBuilderInLoop` | 265.8 ns | 0.28 | 752 B |

3.5× faster and 7.3× less allocated memory with `StringBuilder`.

### PERF1009 — `Substring` vs `Span` for parsing

| Method | Mean | Ratio | Allocated |
|--------|------|-------|-----------|
| `SubstringParse` | 15.8 ns | 1.00 | 32 B |
| `SpanParse` | 9.3 ns | 0.59 | 0 B |

1.7× faster and zero allocation.

### PERF1012 — `ToLower()` vs `OrdinalIgnoreCase`

| Method | Mean | Ratio | Allocated |
|--------|------|-------|-----------|
| `ToLowerCompare` | 28.1 ns | 1.00 | 32 B |
| `OrdinalIgnoreCase` | 1.0 ns | 0.04 | 0 B |

28× faster and zero allocation.

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

### PERF1007 — `Enum.HasFlag`

| Method | Mean | Allocated |
|--------|------|-----------|
| `HasFlag` | 0.033 ns | 0 B |
| `Bitwise` | 0.065 ns | 0 B |

.NET 5+ recognizes `Enum.HasFlag` as a JIT intrinsic and removes the boxing. No allocation was
observed; the intrinsic form is even slightly faster than the manual bitwise comparison in this
short run. **The rule was downgraded to `Info`.** It remains useful for older runtimes and for
readers who want the intent made explicit.

## Reproducing

```bash
dotnet run -c Release --project benchmarks/RungerHeimdall.Benchmarks -- --filter '*' --job short
```

Artifacts are written to `BenchmarkDotNet.Artifacts/` and are not committed.

Short runs are noisy. Numbers cited in rule documentation should come from a full run with
`--job default` before a stable release.