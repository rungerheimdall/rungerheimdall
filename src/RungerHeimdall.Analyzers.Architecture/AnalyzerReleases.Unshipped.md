; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
ARCH1001 | Architecture | Warning | Layer dependency direction
ARCH1002 | Architecture | Warning | Forbidden type reference
ARCH1003 | Architecture | Warning | Forbidden namespace reference
ARCH1004 | Architecture | Warning | Namespace must match folder
ARCH1005 | Architecture | Warning | Repositories and services must be interfaces
ARCH1006 | Architecture | Warning | Layer must not reference disallowed frameworks
ARCH1007 | Architecture | Warning | Avoid static mutable state
ARCH1008 | Architecture | Warning | Endpoint layer must not create infrastructure types
HDL9001 | Architecture | Info | Expired architecture exemption
HDL9002 | Architecture | Info | Invalid architecture configuration
HDL9003 | Architecture | Info | Invalid baseline file