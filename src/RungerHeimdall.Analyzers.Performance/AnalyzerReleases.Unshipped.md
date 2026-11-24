; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
PERF1001 | Performance | Warning | Avoid empty params array allocation
PERF1002 | Performance | Warning | Avoid Enumerable.Count() on a countable collection
PERF1007 | Performance | Warning | Avoid Enum.HasFlag boxing
PERF1012 | Performance | Warning | Avoid ToLower/ToUpper for comparison