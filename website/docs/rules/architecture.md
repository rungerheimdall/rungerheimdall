---
sidebar_position: 1
title: Architecture rules
---

# Architecture rules

Architecture rules are driven by `architecture.json`. Every rule is enabled explicitly, except ARCH1001, which is always active when layers are configured.

## Layer model

Layers are declared once:

```json
{
  "layers": [
    { "name": "Domain", "namespaces": ["MyApp.Domain", "MyApp.Domain.*"], "rank": 0 },
    { "name": "Application", "namespaces": ["MyApp.Application.*"], "rank": 1 },
    { "name": "Infrastructure", "namespaces": ["MyApp.Infrastructure.*"], "rank": 2 },
    { "name": "Api", "namespaces": ["MyApp.Api.*"], "rank": 3 }
  ]
}
```

A namespace pattern ending in `.*` matches the namespace itself and all children. Lower rank means closer to the domain core.

## ARCH1001 — Layer dependency direction

Reports a type that references a type from a layer with a higher rank.

```json
{ "id": "ARCH1001", "kind": "layerDependency", "from": "Domain", "deny": ["Infrastructure", "Api"], "severity": "warning" }
```

When `from` and `deny` are present, the rule checks only that exact pair. Remove them to fall back to global rank monotonicity: any lower-rank layer referencing a higher-rank layer is reported.

## ARCH1002 — Forbidden type reference

Bans specific types inside a scope. Works on declarations and inside method bodies, including `new`, invocations, member access and `typeof`.

```json
{ "id": "ARCH1002", "kind": "forbiddenType", "in": ["Api", "Application"], "types": ["Microsoft.EntityFrameworkCore.DbContext", "Microsoft.EntityFrameworkCore.DbSet`1"] }
```

`types` accepts simple names (`DbContext`), fully qualified names (`Microsoft.EntityFrameworkCore.DbContext`) and metadata names (``DbSet`1``).

## ARCH1003 — Forbidden namespace reference

Bans every type from a namespace inside a scope.

```json
{ "id": "ARCH1003", "kind": "forbiddenNamespace", "in": ["Domain"], "deny": ["MyApp.Infrastructure.*"] }
```

## ARCH1004 — Namespace must match folder

Checks that the declared namespace ends with the folder path that the file lives in. The rule reads `MSBuildProjectDirectory` from the analyzer configuration and falls back to the last folder segment when the project directory is unavailable.

```json
{ "id": "ARCH1004", "kind": "namespaceMatchesFolder", "in": ["Domain", "Application"] }
```

| File | Declared namespace | Result |
|------|--------------------|--------|
| `Entities/Order.cs` | `MyApp.Domain.Entities` | pass |
| `Entities/Order.cs` | `MyApp.Domain` | report |
| `Domain/Entities/Order.cs` | `MyApp.Domain.Entities` | pass |

Generated files, `obj/` and `bin/` are skipped.

## ARCH1005 — Repositories and services must be interfaces

```json
{ "id": "ARCH1005", "kind": "requiredInterface", "in": ["Domain"], "suffix": ["Repository", "Service"], "mustBeInterface": true }
```

`mustBeInterface` must be `true`; the field exists so that configuring `suffix` alone does not silently enable the rule.

## ARCH1006 — Framework allow-list

Reports references to namespaces outside the allow-list, excluding types that belong to a configured layer.

```json
{ "id": "ARCH1006", "kind": "frameworkAllowList", "in": ["Domain"], "allow": ["System", "System.*", "Microsoft", "Microsoft.*"] }
```

When `allow` is omitted, `System*` and `Microsoft*` are allowed by default.

## ARCH1007 — Avoid static mutable state

Reports static fields that are neither `readonly` nor `const`.

```json
{ "id": "ARCH1007", "kind": "noStaticMutableState", "in": ["Domain", "Application"] }
```

## ARCH1008 — Endpoint layer must not create infrastructure types

Reports `new` expressions in the configured scope whose target type lives in a denied namespace.

```json
{ "id": "ARCH1008", "kind": "endpointForbiddenCreation", "in": ["Api"], "deny": ["MyApp.Infrastructure.*"] }
```

## Suppressing a finding

Three options, in order of preference:

1. Fix the code.
2. Declare a temporary exemption with a required reason in `architecture.json`.
3. Add the violation to the baseline.

Adding `#pragma warning disable ARCH1001` or `[SuppressMessage]` also works, since these are ordinary Roslyn diagnostics.