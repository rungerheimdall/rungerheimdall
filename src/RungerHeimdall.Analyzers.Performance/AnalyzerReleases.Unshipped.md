; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
PERF1001 | Performance | Info | Avoid empty params array allocation
PERF1002 | Performance | Warning | Avoid Enumerable.Count() on a countable collection
PERF1003 | Performance | Warning | Avoid ToList before foreach
PERF1004 | Performance | Warning | Avoid string concatenation inside loops
PERF1007 | Performance | Info | Avoid Enum.HasFlag boxing
PERF1009 | Performance | Warning | Avoid Substring allocations for parsing and comparison
PERF1010 | Performance | Warning | Prefer GeneratedRegex over new Regex
PERF1012 | Performance | Warning | Avoid ToLower/ToUpper for comparison
PERF2001 | Performance | Info | Avoid LINQ inside loops
PERF2002 | Performance | Info | Avoid Task.Run inside async methods
PERF2003 | Performance | Info | Avoid async void
PERF2004 | Performance | Warning | Avoid blocking on tasks
PERF2005 | Performance | Info | Avoid enumerating an IEnumerable more than once
PERF2006 | Performance | Info | Avoid awaiting a ValueTask more than once
PERF2007 | Performance | Info | Avoid Task.FromResult on hot paths
PERF2008 | Performance | Info | Avoid Array.Resize inside loops