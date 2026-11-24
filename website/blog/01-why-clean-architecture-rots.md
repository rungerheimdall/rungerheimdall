---
title: Why Clean Architecture Rots in Six Months
date: 2026-11-24
description: The rules are written down. They are still violated, because nothing enforces them.
---

Every team that adopts Clean Architecture writes the same document. It has a diagram with four boxes, a paragraph about dependency direction, and a sentence that says the domain must not know about the database.

Six months later the domain knows about the database.

Not because anyone disagreed with the diagram. Because the diagram lives in Confluence, the rule lives in a code review checklist, and the compiler has never heard of either.

## The three ways teams try to enforce it

**Review.** A senior developer catches the violation. This works until there are more pull requests than senior developers, which happens around the fifth hire. It also fails on the most common case: the violation arrives as a using directive in a file nobody thought to open.

**Architecture tests.** NetArchTest and ArchUnitNET let you write the rule as a test. This is better than a wiki page, but the feedback arrives after the code is written. A failing architecture test in CI means someone has to stop, read a stack trace, and rewrite code they already believed was finished. The rule is also expressed in a second language, so it drifts from the diagram it was supposed to protect.

**Documentation.** Nobody reads it. This is not a criticism of documentation, it is a statement about incentives. A developer under deadline pressure follows the compiler, not the wiki.

## What actually stops a violation

The only rule that survives is the one that fails the build.

That is not a rhetorical point. It is an architectural constraint. If the domain project references the infrastructure project in the `.csproj`, then the domain can reference the database, and no amount of documentation will stop it. The reference is the permission.

So the enforcement has to live at the same level as the reference: in the compiler.

## The shape of the fix

A Roslyn analyzer reads the compilation, knows which namespace belongs to which layer, and reports the moment a lower layer touches a higher one. The diagnostic appears as a squiggle in the editor, not as a failed test an hour later. The same rule runs in CI, so the pull request cannot merge.

The rule itself is data, not code:

```json
{
  "layers": [
    { "name": "Domain", "namespaces": ["MyApp.Domain.*"], "rank": 0 },
    { "name": "Infrastructure", "namespaces": ["MyApp.Infrastructure.*"], "rank": 2 }
  ],
  "rules": [
    { "id": "ARCH1001", "kind": "layerDependency", "from": "Domain", "deny": ["Infrastructure"], "severity": "warning" }
  ]
}
```

That file is reviewed like code, versioned like code, and enforced by the compiler. When the architecture changes, the file changes, and the change is visible in the diff.

## The part nobody talks about

A rule that fires on day one with 4 000 violations does not improve the architecture. It gets uninstalled.

Adoption has to be incremental: freeze the existing violations in a baseline, report only new ones, and allow temporary exemptions with an expiry date. The domain still knows about the database today. The difference is that tomorrow it cannot learn about it any more.