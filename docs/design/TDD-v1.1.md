# RungerHeimdall — 编译期架构与元编程平台技术设计文档

> 品牌：**RungerHeimdall**
> 文档类型：TDD（Technical Design Document）
> 版本：v1.0（决策闭合版）
> 日期：2026-11-23
> 状态：Approved — 可进入 M0

---

## 0. 文档说明

### 0.1 本版本变更

v0.1 为立项草案，包含 6 项待决策。v1.0 已完成决策闭合，主要变更：

| 变更项 | v0.1 | v1.0 |
|--------|------|------|
| 品牌 | 代号 Strata（待定） | **RungerHeimdall**（已定） |
| 范围 | 建议双引擎 + 元编程延后 | **三个全做**，架构旗舰 + 性能同包 v1 + 元编程 M3 |
| 成功基线 | 建议 3k/50k/20 | **正式采纳**为验收标准 |
| 规则配置 | 待定 | v1 = 约定推断 + JSON；C# DSL 留 v2 |
| 元编程定位 | 三选一待定 | **三者整合**（见 7.1） |
| 人力 | 假设 1–5 人 | **个人开发**，不删减任何功能 |
| 路线图 | 44 周 | **重算为 24–30 个月**（见 12 章） |

### 0.2 核心设计原则（v1.0 新增）

> 既然"不砍任何东西"与"个人开发"同时成立，架构必须满足一个硬条件：**每一份逻辑只能写一次**。任何需要"Analyzer 一套 + CLI 一套"的设计都会在 M3 崩溃。

由此推出三条架构红线：

1. **规则引擎与宿主解耦**：规则判定逻辑不依赖 Roslyn 宿主生命周期，Analyzer 与 CLI 共用同一实现（见 4.5）。
2. **产物生成框架化**：元编程的 7 类产物不是 7 份代码，而是一套 Emitter 框架 + 7 个模板（见 7.5）。
3. **文档与规则同源**：规则文档、README 表格、文档站页面全部从 `DiagnosticDescriptor` + 测试用例生成，零手工维护（见 10.3）。

### 0.3 阅读指引

| 角色 | 建议阅读 |
|------|----------|
| 决策者 | 1、3、12、13、15 |
| 架构 | 2、4、5、6、7、8 |
| 实现 | 5、6、7、8、9、16 |
| 运营 | 10、11、14 |

---

## 1. 执行摘要

### 1.1 定位

**RungerHeimdall 是 .NET 的编译期架构与元编程平台**：把架构约束、隐性分配、垂直切片样板，统一收敛到"编译期可执行"的一层。

品牌隐喻：Heimdall 是北欧神话中守望 Bifrost 桥的守护者。架构边界就是 Bifrost——一旦被越过，整个系统就会失序。产品名直译其职责：**守望边界**。

- 英文 tagline：`Heimdall watches the bridge. We watch your architecture.`
- 中文 tagline：`守护你的架构边界`

### 1.2 三条产品线（全量交付，分阶段）

| 产品线 | 角色 | 首个可用版本 | 包 |
|--------|------|--------------|-----|
| **架构守护** | 旗舰，决定首屏印象与传播 | M1 | `RungerHeimdall.Analyzers.Architecture` |
| **性能陷阱检测** | 第二引擎，D 类规则随 v1 同包，形成"护栏"心智 | M1（D 类）/ M2（完整） | `RungerHeimdall.Analyzers.Performance` |
| **元编程** | 生态扩展，决定长期天花板 | M3 | `RungerHeimdall.Generators` |

### 1.3 核心结论

1. **万星在该细分领域无先例**（天花板 Roslynator ≈3.5k）。因此成功基线定为 **3k star + 50k 月下载 + 20 名外部贡献者**，10k 作为拉伸目标而非验收标准。
2. **star 与下载量在分析器品类解耦**：分析器被写进 `Directory.Build.props` 后即"隐形"。这意味着增长策略不能依赖"用户逛仓库"，必须依赖**可截图的首屏效果 + 可分享的 Playground + 团队内 PR 传播**。
3. **"三个全做 + 个人开发"的可行前提是逻辑复用**，不是加班。第 4.5 节的规则引擎解耦是整份文档中最重要的架构决策。
4. **元编程包的竞争位已被占满**（Mapperly 映射、Refit 客户端、NSwag 契约）。唯一空位是"垂直切片编排"，因此采用**三层整合定位**（见 7.1）。
5. **2026 年的时代叙事是"AI 代码的架构护栏"**：AI 每天向仓库灌入的代码量已超人工 Review 的处理能力。MCP 集成（见 10.6）是本项目最强的差异化壁垒。

### 1.4 "不砍任何东西"的承诺方式

用户要求不删减任何功能。本文档的承诺是：

- **功能清单不缩减**：v0.1 中列出的所有能力（含 Playground、Dashboard、MCP、迁移工具、可视化）全部保留在路线图中。
- **只调整顺序与交付形态**：串行交付、分批发布、部分能力以 Preview/社区共建形式交付。
- **明确"永不删除"的清单**（见 12.6），作为文档级契约。

---

## 2. 市场与可行性

### 2.1 Roslyn 能力边界（决定产品上限）

| 能力 | 可行性 | 说明 |
|------|--------|------|
| 语法/语义分析、诊断、Code Fix | 完整 | Analyzer + CodeFixProvider |
| 生成新源文件 | 完整 | `IIncrementalGenerator` |
| 修改已有代码 | 有限 | 只能通过 Code Fix（用户触发）或生成 `partial` 另一半 |
| 跨程序集调用图 | 受限 | 方法体不可见（除非同时编译） |
| 读取真实 IL / JIT 结果 | 不可行 | 看不到内联、逃逸分析 |
| 跨 Generator 通信 | 不可行 | 见 7.2 |
| 运行时插桩 | 不可行 | 不属于 Roslyn 范畴 |

**设计含义**：性能检测只承诺"静态可判定的分配"；"可组合"只承诺产物组合，不承诺生成器编排。

### 2.2 竞品矩阵

> 量级估计，M0 需用 GitHub/NuGet API 实测核准（脚本见 16.4）。

| 项目 | 形态 | Star 量级 | 下载量级 | 核心场景 | 关系 |
|------|------|-----------|----------|----------|------|
| Roslynator | Analyzer + CLI + VSIX | ~3.5k | 千万级 | 通用规范 + 重构 | 直接竞品 |
| Meziantou.Analyzer | Analyzer | ~1k | 数千万 | 通用质量 + 部分性能 | 规则重叠 |
| SonarAnalyzer.CSharp | Analyzer | 中 | 极大 | 企业静态扫描 | 上位替代 |
| StyleCop.Analyzers | Analyzer | ~1.3k | 千万级 | 代码风格 | 无重叠 |
| ErrorProne.NET | Analyzer | ~1k | 中 | 正确性 + 性能 | 规则重叠 |
| NetArchTest | 测试库 | ~1.2k | 中 | 架构规则（测试期） | **核心替代对象** |
| ArchUnitNET | 测试库 | ~700 | 中 | 架构规则（测试期） | 核心替代对象 |
| Mapperly | Generator | ~4k | 高 | 对象映射 | 元编程竞品（被我们编排） |
| CommunityToolkit.Mvvm | Generator | ~3k（主仓更大） | 极高 | MVVM 样板 | 元编程标杆 |
| Refit | 库 + Generator | ~8k | 极高 | HTTP 客户端 | 元编程竞品（可互操作） |
| FluentValidation | 库 | ~9k | 极高 | 验证 | 元编程依赖 |
| Metalama | 编译期 AOP（商业） | ~2k | 低 | 编译期织入 | 上位替代 |
| Microsoft.CodeAnalysis.NetAnalyzers | 官方 | — | 极高 | 官方规则 | 需避让 ID 空间 |

### 2.3 成功基线（已采纳）

| 指标 | 12 个月 | 24 个月 | 说明 |
|------|---------|---------|------|
| GitHub star | 1.5k | **3k（基线）** | 拉伸 10k |
| NuGet 月下载 | 20k | **50k（基线）** | 分析器真实采用度 |
| 外部贡献者 | 5 | **20（基线）** | 规则、文档、issue 分流 |
| Stable 规则数 | 30 | 60 | 质量优先 |
| Issue 首次响应 | < 72h | < 48h | 社区健康 |

**推论**：50k 月下载比 3k star 更难达成，也更难伪造。它要求项目进入至少 300–500 个活跃仓库，也就是必须突破"个人玩具"印象。**佐证**：Meziantou.Analyzer 约 1k star 对应数千万累计下载——下载可以不依赖 star，但需要长期在 NuGet 生态中被 `Directory.Build.props` 收编。

### 2.4 增长策略（已确定 B+C）

| 策略 | 内容 | 采纳 |
|------|------|------|
| A 垂直深耕 | 只做架构守护 | ❌（与全量范围冲突） |
| B 平台化外延 | 分析器 + Playground + 可视化 + CLI + MCP | ✅ |
| C 借势 AI | "AI 代码的架构护栏"叙事 | ✅ |

**组合方式**：B 是载体，C 是叙事。单独 B 会变成"又一个工具站"；单独 C 在 AI 工具自建护栏后失去位置。

---

## 3. 产品定位与范围

### 3.1 目标用户

| 画像 | 特征 | 痛点 | 价值 |
|------|------|------|------|
| **P1 架构师 / Tech Lead** | 30–200 人，微服务，落地 Clean Architecture / Vertical Slice | 规则靠 Code Review，新人跨层引用；NetArchTest 反馈太晚 | 架构可视化 + 实时约束 |
| **P2 高级 .NET 开发者** | 金融、游戏后端、高频 API | 隐蔽分配靠 profiler 事后发现，回归无人拦截 | 热路径语义 + 证据链 |
| **P3 DevEx / 平台工程师** | 维护 `Directory.Build.props`、CI 门禁 | 规范散落 wiki，不可执行；Sonar 太重不可定制 | 单一规则源，IDE 与 CI 同源 |
| **P4 AI 编码工具使用者** | Copilot / Cursor / Claude Code 重度用户 | AI 生成的代码破坏架构、引入隐性分配 | MCP 让 AI 写代码前先读规则 |

> P4 是 2026 年新增画像，也是叙事支点。MCP Server 是本项目对 P4 的唯一交付物，优先级高于 Dashboard。

### 3.2 范围界定

**In Scope**

- 分层/模块依赖规则的分析、诊断、豁免、基线
- 特定类型/成员禁用规则
- 高确定性分配检测（D 类）+ 热路径标注与入口推断
- Vertical Slice / DTO 映射 / OpenAPI 输入的全链生成
- 规则文档自动生成、Playground、CLI、SARIF、MCP
- 架构可视化（Mermaid / SVG）

**Out of Scope（永久不做，写入 README 防预期错位）**

- 通用代码风格/格式化（交给 `.editorconfig` + `dotnet format`）
- 安全漏洞扫描（CodeQL / Sonar）
- 代码重构工具（重命名、提取方法）
- 覆盖率、测试生成
- 运行时 APM / Profiler
- 可视化规则编辑器（v3 之后评估，不在本次承诺内）
- VB.NET（v1 明确不支持，见 15.6）

> 注意区分"Out of Scope"与"延后"：Out of Scope 是永久不做；延后项全部保留在 12 章路线图中。

### 3.3 包结构

| 包 ID | 类型 | TFM | 内容 | 直接引用 |
|-------|------|-----|------|----------|
| `RungerHeimdall.Analyzers` | 元包 | — | 依赖 Architecture + Performance | 推荐入口 |
| `RungerHeimdall.Analyzers.Architecture` | Analyzer | netstandard2.0 | 架构规则，依赖 Engine | 可选 |
| `RungerHeimdall.Analyzers.Performance` | Analyzer | netstandard2.0 | 性能规则，依赖 Engine | 可选 |
| `RungerHeimdall.Engine` | 库 | netstandard2.0 | 规则引擎、配置解析、基线（三方共用） | 否（被依赖） |
| `RungerHeimdall.Abstractions` | 库 | netstandard2.0 | `[HotPath]`、`[HeimdallSlice]` 等 Attribute | 是（生成代码需引用） |
| `RungerHeimdall.Generators` | Generator | netstandard2.0 | 切片生成 | M3 |
| `RungerHeimdall.Cli` | Tool | net8.0 | `heimdall` 命令 | 全局工具 |
| `RungerHeimdall.Templates` | Template | — | `dotnet new heimdall-*` | 否 |

**Engine 独立包的理由（决策 A3）**：Architecture 与 Performance 共享同一套规则引擎。若各自内联同名 DLL，Roslyn 加载时会冲突；若改名内联，则代码重复，与个人开发的维护能力冲突。独立包由 NuGet 统一放入 `analyzers/dotnet/cs/` 一次，两个分析器共用，CLI 亦可直接依赖。

**命名空间**：`RungerHeimdall.Analyzers.Architecture` 较长，但必须在 `using` 中保持完整以避免与其他包冲突。规则内部的用户可见类型只有 Attribute，其命名空间为 `RungerHeimdall`（短），例如 `RungerHeimdall.HotPathAttribute`。

**关键约束**：`RungerHeimdall.Abstractions` **不得**设 `PrivateAssets=all`（生成代码引用 Attribute，裁剪会导致下游编译失败）。

### 3.4 命令与 URL

| 项 | 值 | 说明 |
|----|-----|------|
| CLI 命令 | `heimdall` | 短、可记；M0 需检查与现有工具冲突 |
| CLI 长命令 | `dotnet heimdall` | 备选，便于与 `dotnet` 生态一致 |
| 文档域名 | `rungerheimdall.github.io/rungerheimdall/`（免费） | `.dev` 待有赞助收入后注册并 301 跳转 |
| GitHub | `rungerheimdall/rungerheimdall` | monorepo |
| NuGet 前缀 | `RungerHeimdall.*` | M0 抢占（含内部保留 ID） |

> `heimdall` 命令名存在冲突风险（社区有同名监控面板与 CLI 工具）。若冲突，回退为 `dotnet-heimdall` 或 `hdall`。此项 M0 决策，不影响源码结构。

---

## 4. 总体架构

### 4.1 架构图

    开发者（IDE）                    CI（dotnet build）          AI 工具（MCP）
          │                                │                          │
          └────────────────┬───────────────┘                          │
                           ▼                                          │
            ┌────────────────────────────────┐                        │
            │  Roslyn 宿主（devenv/MSBuild）   │                        │
            │                                │                        │
            │  ┌──────────────────────────┐  │                        │
            │  │ Analyzers                 │  │                        │
            │  │ ├ Architecture            │  │                        │
            │  │ └ Performance             │  │                        │
            │  └──────────┬───────────────┘  │                        │
            │  ┌──────────┴───────────────┐  │                        │
            │  │ Generators（M3）           │  │                        │
            │  └──────────┬───────────────┘  │                        │
            └─────────────┼──────────────────┘                        │
                          │                                           │
                     Diagnostics                                      │
                          │                                           │
           ┌──────────────┴───────────────┐                           │
           │ IDE 波浪线 / 错误列表 / SARIF   │                           │
           └──────────────┬───────────────┘                           │
                          ▼                                           ▼
            ┌──────────────────────────────────────────────────────────┐
            │  RungerHeimdall.Cli（net8.0，命令：heimdall）               │
            │  ├ analyze / baseline / doctor / graph / report           │
            │  ├ 复用 RuleEngine（与 Analyzer 同源）                      │
            │  └ mcp（stdio server）────────────────────────────────────┘
            └──────────────────────────────────────────────────────────┘

### 4.2 模块分解

| 模块 | 职责 | TFM | 依赖 |
|------|------|-----|------|
| `Engine`（源码内联，不单独发包） | 规则判定、模型、配置解析、基线 | netstandard2.0 | 仅 Roslyn |
| `Analyzers.Architecture` | 宿主适配 + 诊断注册 | netstandard2.0 | Engine |
| `Analyzers.Performance` | 宿主适配 + 诊断注册 | netstandard2.0 | Engine |
| `Generators` | 模型分析、IR、Emitter | netstandard2.0 | Engine + Abstractions |
| `Abstractions` | 公开 Attribute | netstandard2.0 | 无 |
| `Cli` | 解决方案加载、批量执行、报告、MCP | net8.0 | Engine（以 netstandard2.0 引用） |
| `Playground` | WASM 试用 | net8.0-wasm | Engine |
| `Docs` | Docusaurus | — | 生成的规则文档 |

### 4.3 技术选型

| 决策 | 选择 | 理由 |
|------|------|------|
| Analyzer TFM | `netstandard2.0` | Roslyn 宿主唯一安全选择 |
| Roslyn baseline | `4.8.0` | 覆盖 VS 17.8+ / SDK 8+，且向后兼容 |
| Generator API | `IIncrementalGenerator` | 增量缓存，IDE 性能必需 |
| JSON | `System.Text.Json` 8.x + 源生成 | 唯一允许的第三方依赖，需隔离验证 |
| 测试 | `Microsoft.CodeAnalysis.*.Testing` + xUnit + Verify | 官方推荐 + 快照 |
| 基准 | BenchmarkDotNet | 可复现、可发布 |
| CLI | `MSBuildWorkspace` + `Microsoft.Build.Locator` | 加载完整语义 |
| 文档站 | Docusaurus | 搜索、版本化、规则浏览器 |
| 图形输出 | Mermaid（默认）+ SVG（M4） | 零依赖渲染 |

### 4.4 数据流

    architecture.json / .editorconfig
              │
              ▼
        ConfigLoader ──→ RuleSet（不可变，已编译）
              │
              ▼
        RuleEngine（宿主无关）
          ├─ LayerIndex（前缀树）
          ├─ PatternMatcher（无 Regex）
          └─ Analyzer 回调集合
              │
       ┌──────┴──────┐
       ▼             ▼
    Diagnostic    报告模型（JSON）
       │             │
     IDE/SARIF    CLI/Playground/MCP

### 4.5 规则引擎解耦（v1.0 最重要的架构决策）

**问题**：Analyzer 的逻辑绑定在 `CompilationStartAnalysisContext` / `SymbolAnalysisContext` 生命周期上；CLI 需要在自己的进程里跑同一套规则。若不复用，60 条规则要写两遍——个人开发必然崩溃。

**方案**：规则实现为纯函数式判定器，宿主只做适配。

    接口（Engine 层，不依赖宿主类型）

    public interface IRule
    {
        string Id { get; }
        DiagnosticDescriptor Descriptor { get; }
        bool IsEnabled(RuleOptions options);
    }

    public interface ISymbolRule : IRule
    {
        void Analyze(SymbolContext context);   // 自定义轻量上下文
    }

    public interface IOperationRule : IRule
    {
        void Analyze(OperationContext context);
    }

    Analyzer 宿主适配：

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ArchitectureAnalyzer : DiagnosticAnalyzer
    {
        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(start =>
            {
                var ruleSet = RuleSetLoader.Load(start.Options, start.AdditionalFiles);
                if (ruleSet.IsEmpty) return;

                var engine = new RuleEngine(ruleSet);
                start.RegisterSymbolAction(c => engine.RunSymbolRules(c), SymbolKind.NamedType);
                start.RegisterOperationAction(c => engine.RunOperationRules(c), ...);
            });
        }
    }

    CLI 宿主适配：

    var compilation = await project.GetCompilationAsync();
    var engine = new RuleEngine(ruleSet);
    foreach (var tree in compilation.SyntaxTrees)
    {
        var model = compilation.GetSemanticModel(tree);
        engine.RunOnTree(tree, model, ct);
    }

**约束**：
- `Engine` 中**禁止**出现 `AnalysisContext` / `Compilation` 参数（用自定义 `SymbolContext` / `OperationContext` 包装）。
- `SymbolContext` 内不缓存 `ISymbol`（生命周期由宿主控制）。
- Analyzer 侧的 `Engine` 调用必须是 O(1) 的委托跳转，不可在回调里做配置解析。

**收益**：
- 规则只写一次，Analyzer / CLI / Playground / MCP 四处复用。
- 规则的单元测试不需要 Roslyn Testing 框架（可直接构造 `SymbolContext` 的测试双），测试速度快 5–10 倍。
- 元编程包的模型分析同样复用 `Engine`（作为 `IModelRule`）。

---

## 5. 方向一：架构守护（旗舰）

### 5.1 问题定义

| 现状 | 缺陷 |
|------|------|
| 规则写在 Wiki / 口头约定 | 不可执行 |
| NetArchTest 写在测试里 | 反馈晚、报错难懂、规则与代码分离 |
| Sonar 架构规则 | 不可定制、商业绑定 |

目标体验：输入 `using MyApp.Infrastructure;` 的瞬间出现波浪线；错误消息说明"哪层不能依赖哪层、为什么、怎么改"；CI 中同样阻断。

### 5.2 领域模型

    Layer
      ├─ Name: string
      ├─ Namespaces: glob[]
      ├─ Rank: int（0 最低层，越大越靠外）
      └─ MatchMode: namespace | assembly | project | suffix

    Rule
      ├─ Id / Kind / From / Deny / Severity / Message
      └─ Kind ∈ { LayerDependency, ForbiddenType, ForbiddenMember,
                  ForbiddenNamespace, RequiredSuffix, MustBeInterface,
                  NoStaticMutableState, NamespaceMatchesFolder }

    Exemption
      ├─ Scope（type | member | namespace | assembly）
      ├─ Target / Reason（必填）/ ExpiresOn（可选）

### 5.3 配置方案（已决策）

| 方案 | 采纳 | 说明 |
|------|------|------|
| A `.editorconfig` 自定义键 | ❌ | 表达力弱 |
| B MSBuild Item | ❌ | 与 props 耦合过深 |
| C **JSON + AdditionalFiles** | ✅ v1 精确控制 | Schema 校验、可生成、可迁移 |
| D C# DSL | ⏳ v2 | 表达力最强 |
| E Attribute 声明 | ❌ | 污染业务代码 |
| F **命名约定自动推断** | ✅ v1 默认 | 零配置首屏效果 |

**启动策略（决策 B1/B2/B3/B5）**：
1. 无配置文件时，默认按约定推断层。约定集支持两套：
   - 主约定：`Domain` / `Application` / `Infrastructure` / `Api`
   - 扩展约定：`Core` / `Shared` / `Modules` / `Web` / `Endpoints` / `Host`
2. 推断模式下仅启用 5 条最高置信度规则（ARCH1001/1002/1006/1008/1011），**全部 Warning**——不会阻断构建。
3. 存在 `architecture.json` 时，完全以文件为准（不叠加推断）。
4. 提供三档预设：`Minimal`（3 条）、`Recommended`（默认，12 条）、`Strict`（全部 + Error）。安装后默认 Recommended；`heimdall init --strict` 切换。
5. `heimdall init` 生成推断结果对应的 JSON 骨架；若文件已存在，写入 `architecture.json.new`，不覆盖。

### 5.4 配置文件

位置：解决方案根 `architecture.json`，通过包内 `build/*.props` 自动注入 `AdditionalFiles`。

    {
      "$schema": "https://rungerheimdall.github.io/rungerheimdall/schemas/architecture.v1.json",
      "version": 1,
      "layers": [
        { "name": "Domain",         "namespaces": ["MyApp.Domain", "MyApp.Domain.*"], "rank": 0 },
        { "name": "Application",    "namespaces": ["MyApp.Application.*"],            "rank": 1 },
        { "name": "Infrastructure", "namespaces": ["MyApp.Infrastructure.*"],         "rank": 2 },
        { "name": "Api",            "namespaces": ["MyApp.Api.*"],                    "rank": 3 }
      ],
      "rules": [
        { "id": "ARCH1001", "kind": "layerDependency", "from": "Domain", "deny": ["Infrastructure", "Api"], "severity": "warning" },
        { "id": "ARCH1002", "kind": "forbiddenType",  "in": ["Api", "Application"], "types": ["Microsoft.EntityFrameworkCore.DbContext", "Microsoft.EntityFrameworkCore.DbSet`1"], "severity": "warning" },
        { "id": "ARCH1003", "kind": "requiredSuffix", "in": ["Domain"], "suffix": ["Repository"], "mustBeInterface": true, "severity": "warning" }
      ],
      "exemptions": [
        { "scope": "type", "target": "MyApp.Infrastructure.LegacyOrderAdapter", "reason": "迁移期临时豁免", "expiresOn": "2027-06-30" }
      ]
    }

**设计要点**：
- `rank` 单调而非 allow-list：新增层不需改规则。
- 默认 deny 而非 allow：架构规则通常是"禁止少数"。
- `exemptions.reason` 必填：防止豁免变垃圾桶。
- `expiresOn` 到期后报 `HDL9001`（提醒清理），但**不自动恢复规则**（避免突然红屏）。

### 5.5 检测算法

    Phase 1 CompilationStart
      1. 加载 JSON（或推断）→ LayerIndex（最长前缀匹配）
      2. 编译 RuleSet；空则 return（零开销）
      3. 构建 RuleEngine，注册 SymbolAction + OperationAction

    Phase 2 声明位置（NamedType）
      检查：基类、接口、字段、属性、参数、返回类型、泛型约束、Attribute

    Phase 3 方法体（Operation）
      Invocation / ObjectCreation / FieldReference / PropertyReference /
      MethodReference / TypeOf / Conversion / DelegateCreation / EventAssignment

    Phase 4 去重与抑制
      键：(ruleId, containingSymbol, targetSymbol)
      尊重 #pragma / SuppressMessage / EditorConfig severity / 基线

**必须显式处理的 20 个边界情况**（每条一个单测）：

| # | 情况 | 期望 |
|---|------|------|
| 1 | `using X = Infra.Foo;` 别名 | 穿透命中 |
| 2 | `global using Infra;` | 命中，仅报一次 |
| 3 | `var x = new SqlRepo();` | ObjectCreation 命中 |
| 4 | 泛型 `IRepository<T>` | `OriginalDefinition` 比较 |
| 5 | 方法组 `Select(repo.Map)` | MethodReference 命中 |
| 6 | `nameof(InfraType)` | 默认不命中（可开关） |
| 7 | `typeof(InfraType)` | 命中（可配置） |
| 8 | Attribute 引用 | 命中（级别独立配置） |
| 9 | XML doc `cref` | 默认不命中 |
| 10 | `partial` 跨文件 | Symbol 聚合，只报一次 |
| 11 | `#if DEBUG` 内引用 | 按实际语法树判定 |
| 12 | `dynamic` | 不命中（文档明示） |
| 13 | 表达式树 | 命中 |
| 14 | 生成代码 | 默认跳过 |
| 15 | `base.Method()` | 按目标符号层判定 |
| 16 | 扩展方法 | 按声明类型层判定 |
| 17 | 隐式/自定义转换 | 按目标类型判定 |
| 18 | `record` 主构造参数 | 视为构造参数检查 |
| 19 | `file` 局部类型 | 归当前 namespace |
| 20 | `InternalsVisibleTo` | 不特殊放行 |

### 5.6 诊断设计

**三段式文案**（首行 ≤ 140 字符）：

    ARCH1001  Domain 层不允许依赖 Infrastructure 层。
    此处使用了 SqlOrderRepository（MyApp.Infrastructure.Persistence）。
    依赖方向必须由外层指向内层（Api → Application → Domain）。
    Domain 是业务核心，不应知晓持久化实现。建议：在 Domain 定义
    IOrderRepository 接口，在 Infrastructure 实现，在 Api 层注入。

**能力清单**：
- `helpLinkUri` → `https://rungerheimdall.github.io/rungerheimdall/rules/ARCH1001`
- Code Action「解释这条规则」→ 打开文档锚点（不做自动修复）
- Code Action「添加豁免」→ 写入 `architecture.json`（带 reason 占位）
- 同规则多违规聚合：message 列前 3 个，避免波浪线刷屏

### 5.7 豁免与渐进式采纳

| 机制 | 用途 | 实现 |
|------|------|------|
| 基线文件 | 存量冻结 | `heimdall baseline create` → `heimdall-baseline.json`；必须进版本控制（决策 B4） |
| Severity 分级 | 灰度上线 | `dotnet_diagnostic.ARCH1001.severity = suggestion` |
| 豁免条目 | 精确放行 | `architecture.json` 带 reason + 过期 |

**基线签名**：`(ruleId, symbolFullName, targetFullName)`，不用行号（代码一动就失效）。
**基线纪律**：CI 可开启"基线只能缩小"校验；`heimdall baseline prune` 清理失效条目。

> 这是分析器类产品的第一生命线：没有基线，任何存量项目都无法接入。

### 5.8 性能设计

| 风险 | 措施 |
|------|------|
| 大解决方案慢 | 命名空间前缀 `Ordinal` 快速失败 |
| `ToDisplayString()` 分配 | 仅诊断时调用 |
| 正则回溯 | 不用 Regex，前缀/后缀匹配器 |
| 缓存泄漏 | 键为字符串，LRU 上限 10k |
| 持有 Compilation | 禁止；缓存挂在 CompilationStart 闭包内 |
| 生成代码干扰 | `GeneratedCodeAnalysisFlags.None` |

**门禁**（Roslyn 源码 + ASP.NET Core 仓库）：
- 冷启动额外时间 < 8%
- 增量单文件 < 30ms（P95）
- 内存 < 60MB

### 5.9 与 NetArchTest 对比及迁移

| 维度 | NetArchTest | RungerHeimdall |
|------|-------------|----------------|
| 反馈时机 | 测试运行 | 输入时 |
| 规则表达 | C# 代码 | JSON / 约定 |
| 报错 | 断言堆栈 | 行定位 + 原因 + 建议 |
| 跨项目 | 手动加载程序集 | 自动随 Compilation |
| 架构图 | 无 | CLI 生成 Mermaid |
| 开销 | 测试时间 | 编译期 |

**迁移工具**：`heimdall migrate netarchtest --path ./tests/ArchitectureTests.cs`，解析 `Types.InAssembly(...).ShouldNot()...` 调用链输出 JSON；无法覆盖的模式列为 TODO。

> 迁移工具本身就是传播点：它出现在"从 NetArchTest 迁移"的长尾搜索里。

### 5.10 规则清单（MVP → 全量）

**M1（8 条，Stable）**

| ID | 规则 | 默认 |
|----|------|------|
| ARCH1001 | 分层依赖方向 | Warning（Strict: Error） |
| ARCH1002 | 禁止类型引用（DbContext 等） | Warning（Strict: Error） |
| ARCH1003 | 禁止命名空间引用 | Warning（Strict: Error） |
| ARCH1004 | 命名空间匹配文件夹 | Warning |
| ARCH1005 | 仓储/服务以接口定义在 Domain | Warning |
| ARCH1006 | Domain 不得引用第三方框架 | Warning（Strict: Error） |
| ARCH1007 | 禁止静态可变状态 | Warning |
| ARCH1008 | 端点层禁止 new 基础设施类型 | Warning（Strict: Error） |

> **决策 B2**：v1 所有规则默认 Warning。Error 会直接阻断用户构建，是分析器类产品的第一卸载原因。需要强制的团队用 `--strict` 预设或显式配置。

**M3（4 条）**：ARCH1009 跨模块内部命名空间、ARCH1010 环依赖、ARCH1011 `InternalsVisibleTo` 限制、ARCH1012 Domain 时间抽象。

**M4+（社区扩展）**：模块化单体边界、CQRS 读写分离约束、事件溯源约束等，由规则提案产出。

---

## 6. 方向二：性能陷阱检测

### 6.1 问题定义

编译器看不见"每秒分配 4KB"，Profiler 只在运行时可见。空白地带是：**静态可判定、但需上下文才知是否值得报的分配模式**。

核心难题：**判断代码是否在热路径上**。现有规则库都回避此问题（全量报 → 噪音爆炸）。

### 6.2 热路径模型（四层）

| 层级 | 依据 | 置信度 | 默认等级 | 示例 |
|------|------|--------|----------|------|
| L1 显式 | `[HotPath]` / `[ColdPath]` | 确定 | Warning | 用户标注的 `ProcessRequest` |
| L2 框架入口 | 已知签名/Attribute | 高 | Warning | Controller action、`IHostedService`、`MapGet` lambda、`BackgroundService`、SignalR Hub、Azure Function、MassTransit consumer |
| L3 结构上下文 | 循环体、迭代器、Span 回调、事件处理器 | 中高 | Warning（仅 D 类） | `foreach` 内 `.Where()` |
| L4 命名启发式 | `Process/Handle/Execute/Serialize/Render` | 低 | **默认关闭** | `ProcessOrder` |

**标注 API**：

    namespace RungerHeimdall;

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property)]
    public sealed class HotPathAttribute : Attribute
    {
        public string? Reason { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class ColdPathAttribute : Attribute { }

**传播策略（有意保守）**：不跨程序集；同编译单元内仅传播 1 层；可通过 `.editorconfig` 关闭传播。

**为什么显式标注是优势**：把"是否热路径"交还给团队，工具从"猜测者"变成"契约执行者"。这既降低误报，也是产品叙事的支点。

### 6.3 规则分级

| 级别 | 定义 | 误报目标 | 默认 |
|------|------|----------|------|
| D 确定性分配 | 静态可证明，无 JIT 优化空间 | < 1% | Warning |
| C 情境性开销 | 取决于运行时上下文 | < 5% | Suggestion / Off |
| X 风格惯例 | 非性能 | — | Off |

**红线**：任何规则在 corpus 上误报率超阈值，不得默认开启。写入 `CONTRIBUTING.md`。

### 6.4 规则清单（20 条）

**M1 随 v1 同包（8 条 D 类，形成"护栏"心智）**

| ID | 规则 | 级别 | 修复 |
|----|------|------|------|
| PERF1001 | `params` 空数组分配 | D | `ReadOnlySpan` 重载 |
| PERF1002 | `Enumerable.Count()` 可避免 | D | `.Count` / `.Length` |
| PERF1003 | `ToList()` 后立即 foreach | D | 省略物化 |
| PERF1004 | 循环内字符串拼接 | D | `StringBuilder` |
| PERF1005 | 闭包捕获循环变量 | D | 提取局部变量 |
| PERF1006 | 热路径内 `DateTime.Now` | D | `TimeProvider` |
| PERF1007 | `Enum.HasFlag` 装箱 | D | 位运算 |
| PERF1012 | `ToLower()` 比较 | D | `StringComparison.OrdinalIgnoreCase` |

**M2（12 条）**

| ID | 规则 | 级别 |
|----|------|------|
| PERF1008 | 结构体默认比较器 | D |
| PERF1009 | `Substring` 可改 `Span` | D |
| PERF1010 | `Regex` 未源生成 | D |
| PERF1011 | 值类型 `foreach` 装箱 | D |
| PERF2001 | 循环内 LINQ | C |
| PERF2002 | `async` 内 `Task.Run` | C |
| PERF2003 | `async void` | C |
| PERF2004 | `.Result` / `.Wait()` | C |
| PERF2005 | 多次枚举 `IEnumerable` | C |
| PERF2006 | `ValueTask` 多次 await | C |
| PERF2007 | `Task.FromResult` 非缓存 | C |
| PERF2008 | 循环内 `Array.Resize` | C |

### 6.5 证据链（差异化核心）

1. **诊断内嵌数据**：`此处调用 WriteLine(string, params object[]) 未传参，分配 24 字节空数组；每秒 1 万次即 240KB/s GC 压力。`
2. **规则文档附 BenchmarkDotNet 表**，CI 每次发版重跑，防止过期。
3. **CLI 生成 `hotpath-report.md`**：按方法聚合的分配热点，可直接贴 PR。
4. **反例仓库** `samples/PerfAntiPatterns`：`dotnet run` 输出诊断数，作为端到端测试 + 演示素材。

> 没有基准数据的性能分析器会被资深开发者归类为"AI 生成的规则库"。证据链是本方向唯一可信度来源。

### 6.6 误报控制

| 来源 | 对策 |
|------|------|
| 非热路径 | 默认只报 L1/L2/L3；全量需 `--aggressive` |
| JIT 已优化 | 只报 IL 层可确认的分配 |
| 生成代码 | `GeneratedCodeAnalysisFlags.None` |
| 测试项目 | 默认排除 |
| 泛型实例化 | `OriginalDefinition` / `ConstructedFrom` |
| 同名方法误导 | 匹配**精确 BCL 符号**，不按方法名 |

**corpus 回归**：Roslyn、ASP.NET Core、EF Core、一个中型业务仓库；诊断数变化 > 5% 需人工审查。

---

## 7. 方向三：元编程（M3）

### 7.1 定位决策（v1.0 已定）

用户表示"Vertical Slice / DTO 映射 / OpenAPI 全栈都行"。决策：**三者整合，分层承担不同职责**。

| 层 | 采用 | 理由 |
|----|------|------|
| 输入层 | **C# 模型** 或 **OpenAPI 契约** | 两种入口覆盖"代码先行"与"契约先行"两类团队 |
| 编排层 | **Vertical Slice** | 唯一没有主流工具占位的空白位 |
| 产物层 | **DTO + Mapping + Validation + Endpoint + Client** | DTO/Mapping 单独做打不过 Mapperly，但作为切片产物是自然需求 |

**整合后的价值主张**：`从一份契约或一个模型，生成一条完整可运行的垂直切片。`

**为什么不做成三个独立产品**：三者共享同一套模型分析、IR 与 Emitter 框架；拆开会把个人开发的成本乘以三，且无法形成"一条链"的叙事。

### 7.2 "可组合"的技术边界（必须先对齐）

Roslyn 的三个硬约束：

1. Generator 之间**不可见**（A 的输出不是 B 的输入）。
2. `RegisterPostInitializationOutput` 是唯一共享通道，且只能注入静态内容。
3. Generator **不能修改用户代码**，只能生成 `partial` 另一半。

因此"可组合"定义调整为：

    [HeimdallSlice] 用户标注 / OpenAPI 输入
              │
              ▼
    ┌──────────────────────────────────┐
    │ 单一 Generator                     │
    │ 1. 模型发现（ForAttributeWithMetadataName）│
    │ 2. 构建 IR（不可变 record）          │
    │ 3. Emitter 框架 + 可开关模板          │
    └──────────────────────────────────┘
        │      │        │       │       │
    Endpoint Handler Validator Mapper Client

**对外表述规范**：称"可组合的产物"，不称"可组合的生成器"。这一条要写进 README 与文档首页，避免预期落差。

**真正的组合能力由以下提供**：
- 产物开关矩阵（`.editorconfig` / MSBuild property）
- 宿主适配（Minimal API / FastEndpoints / MediatR）
- `partial` 逃生舱（用户可覆盖任何生成物）
- 互操作（生成 FluentValidation 语法、调用 Mapperly 映射、生成 Refit 接口）

### 7.3 用户 API

    [HeimdallSlice("Orders/Create", Host = SliceHost.MinimalApi)]
    public sealed partial record CreateOrderRequest
    {
        [Required, MaxLength(64)] public required string CustomerId { get; init; }
        [MinLength(1)]            public required IReadOnlyList<OrderLine> Lines { get; init; }
    }

    public sealed class CreateOrderHandler
        : ISliceHandler<CreateOrderRequest, CreateOrderResponse>
    {
        public ValueTask<CreateOrderResponse> HandleAsync(
            CreateOrderRequest request, CancellationToken ct) => ...;
    }

**OpenAPI 输入方式**：

    <AdditionalFiles Include="openapi/orders.v1.json" />

    [HeimdallSliceFromOpenApi("openapi/orders.v1.json", "POST /orders")]
    public sealed partial class CreateOrderSlice { }

生成器读取契约 → 生成模型 + 切片骨架 → 业务实现仍需手写（这是有意的，生成器不写业务逻辑）。

**版本与宿主约束（决策 C8/C9/C10）**：
- 最低 C# 语言版本：**11**（使用 `required` / `init`），对应 .NET 7+ / SDK 8+。
- OpenAPI：支持 **3.0 与 3.1**，不支持 2.0（Swagger 2.0 需用户先转换）。
- 宿主（M3）：**Minimal API / FastEndpoints / MediatR**。Controller 模式排在 v1.1，仍保留在 12.7 永不删除清单内。

### 7.4 产物矩阵

| 产物 | 内容 | 默认 | 可关闭 |
|------|------|------|--------|
| Endpoint | `MapPost` 扩展 / FastEndpoints / MediatR 注册 | ✅ | ✅ |
| Request DTO | 模型即 DTO 时跳过 | ✅ | ✅ |
| Response DTO | 从 Handler 返回类型推导 | ✅ | ✅ |
| Validator | FluentValidation 或轻量校验 | ✅ | ✅ |
| Mapper | 模型 ↔ DTO | ✅ | ✅ |
| Client | `IOrdersClient` + `HttpClient` | ⭕ | ✅ |
| OpenAPI 注解 | `.WithOpenApi()` 元数据 | ✅ | ✅ |

### 7.5 Emitter 框架（个人开发的关键）

**问题**：7 类产物 × 3 种宿主 = 21 个模板。硬写会失控。

**方案**：统一 Emitter 框架。

    public interface IEmitter
    {
        string Name { get; }                       // "endpoint.minimalapi"
        bool IsEnabled(SliceOptions options);
        void Emit(EmitContext ctx, SliceIr ir);
    }

    EmitContext
      ├─ SourceWriter（缩进、换行、using 管理、确定性排序）
      ├─ FileHint（唯一、稳定、可读）
      └─ Diagnostics（生成期诊断，如模型不合法）

    EmitterRegistry（按 Name 注册，按 Option 过滤）

**收益**：
- 新增产物 = 新增一个 Emitter 类 + 模板方法，不改主流程。
- 每个 Emitter 可独立单测（给定 IR → 断言输出）。
- 快照测试按 Emitter 粒度组织，失败定位精确。

### 7.6 生成代码质量标准（12 条，全部有测试）

1. 首行 `// <auto-generated/>`
2. `#nullable enable`
3. `[GeneratedCode("RungerHeimdall.Generators", "x.y.z")]`
4. 无时间戳（确定性）
5. 类型默认 `sealed`
6. 集合返回 `IReadOnlyList<T>`
7. `CancellationToken` 全链路
8. 库代码 `ConfigureAwait(false)`
9. 无 `dynamic`、无反射（AOT/trim 安全）
10. JSON 用源生成上下文
11. 生成顺序按全限定名 `Ordinal` 排序
12. 公开成员有 XML doc（可关）

### 7.7 增量生成器性能

    var models = context.SyntaxProvider
        .ForAttributeWithMetadataName(
            "RungerHeimdall.HeimdallSliceAttribute",
            predicate: static (n, _) => n is TypeDeclarationSyntax,
            transform: static (ctx, ct) => SliceModel.From(ctx, ct))  // → record
        .Where(static m => m is not null)
        .Select(static (m, _) => m!)
        .Collect();

    context.RegisterSourceOutput(models, static (spc, all) => Emit(spc, all));

**反模式（CI 静态检查）**：
- transform 返回 `INamedTypeSymbol`
- `RegisterSourceOutput` 中访问 `Compilation`
- pipeline 中做 I/O
- 缺 `Collect()` 导致每模型独立生成

### 7.8 互操作策略（不替代，只编排）

| 库 | 策略 |
|----|------|
| Mapperly | 调用其 `[Mapper]` 类；`runger_slice_mapper=mapperly` |
| FluentValidation | Validator 继承 `AbstractValidator<T>` |
| Refit | Client 可切换为 Refit 接口（只生成接口） |
| MediatR | Handler 实现 `IRequestHandler<TReq, TRes>` |
| FastEndpoints | Endpoint 继承 `Endpoint<TReq, TRes>` |

---

## 8. 跨模块技术设计

### 8.1 解决方案结构

    RungerHeimdall/
    ├─ src/
    │  ├─ RungerHeimdall.Analyzers/              # meta package
    │  ├─ RungerHeimdall.Engine/                 # 规则引擎（internal 编入各包）
    │  ├─ RungerHeimdall.Analyzers.Architecture/
    │  ├─ RungerHeimdall.Analyzers.Performance/
    │  ├─ RungerHeimdall.Abstractions/
    │  ├─ RungerHeimdall.Generators/
    │  ├─ RungerHeimdall.Cli/
    │  └─ RungerHeimdall.Playground/
    ├─ tests/
    │  ├─ RungerHeimdall.Engine.Tests/           # 快速单测（无 Roslyn 宿主）
    │  ├─ RungerHeimdall.Analyzers.*.Tests/      # Roslyn Testing
    │  ├─ RungerHeimdall.Generators.Tests/       # Verify 快照
    │  ├─ RungerHeimdall.IntegrationTests/
    │  └─ RungerHeimdall.Benchmarks/
    ├─ samples/
    │  ├─ CleanArchitecture.Sample/
    │  ├─ PerfAntiPatterns/
    │  └─ VerticalSlice.Sample/
    ├─ docs/
    ├─ build/
    └─ .github/workflows/

**monorepo 理由**：三个包共享 Engine；独立仓库会导致版本同步地狱；star 集中主仓。

### 8.2 公共组件

| 组件 | 职责 | 关键设计 |
|------|------|----------|
| `DiagnosticFactory` | 统一诊断创建 | 静态 Descriptor；消息模板预编译 |
| `SymbolFacts` | 符号谓词 | `IsInNamespace`、`IsAssignableTo`、`GetOriginalDefinition` |
| `LayerIndex` | 命名空间 → 层 | 前缀树 + 最长匹配，不可变 |
| `PatternMatcher` | glob 匹配 | 前缀/后缀匹配，无 Regex |
| `OptionsReader` | 配置读取 | `AnalyzerConfigOptionsProvider` 强类型包装 |
| `SourceGuard` | 生成/第三方代码检测 | `IsGeneratedCode`、`IsInObjFolder` |
| `BaselineStore` | 基线 | 签名 HashSet |
| `ExemptionStore` | 豁免与过期 | 过期报 `HDL9001` |
| `RuleDocGenerator` | 文档生成 | CI 校验一致性 |
| `SourceWriter` | 生成代码写出 | 缩进、换行、确定性排序 |

### 8.3 打包

    RungerHeimdall.Analyzers.Architecture.nupkg
    ├─ analyzers/dotnet/cs/RungerHeimdall.Analyzers.Architecture.dll
    ├─ analyzers/dotnet/cs/RungerHeimdall.Engine.dll      （由依赖包提供）
    ├─ analyzers/dotnet/cs/System.Text.Json.dll           （唯一第三方依赖）
    ├─ build/RungerHeimdall.Analyzers.Architecture.props
    ├─ README.md
    ├─ icon.png
    └─ LICENSE

    RungerHeimdall.Engine.nupkg
    ├─ analyzers/dotnet/cs/RungerHeimdall.Engine.dll
    └─ lib/netstandard2.0/RungerHeimdall.Engine.dll       （供 CLI 引用）

**元数据**：`DevelopmentDependency`、MIT、`PackageIcon`、`PackageReadmeFile`、`PublishRepositoryUrl`、`EmbedUntrackedSources`、`IncludeSymbols`、`ContinuousIntegrationBuild`。

**用户侧（README 必须写）**：

    <PackageReference Include="RungerHeimdall.Analyzers" Version="0.1.0-preview.1" PrivateAssets="all" />

**发布**：GitHub Actions + NuGet OIDC Trusted Publishing；tag 触发；起步版本 `0.1.0-preview.1`（决策 A6），M2 发布 `1.0.0`。

### 8.4 测试策略（四层 + 一层新增）

| 层 | 工具 | 覆盖 | 目标 |
|----|------|------|------|
| L0 Engine 单测 | xUnit（无 Roslyn 宿主） | 规则判定纯逻辑 | 行覆盖 > 90%，**毫秒级** |
| L1 规则单测 | `Microsoft.CodeAnalysis.*.Testing` | 每条规则正例/反例/边界 | 100% 规则 |
| L2 快照 | `Verify.SourceGenerators` | Generator 输出全文 | 100% 产物 |
| L3 集成 | sample solution + `dotnet build` | 端到端诊断 / SARIF | 关键路径 |
| L4 基准/误报 | BenchmarkDotNet + corpus | 性能 / 误报率 | 见 5.8 / 6.6 |

**L0 是个人开发的加速器**：规则逻辑不依赖 Roslyn 宿主，单测可在 1 秒内跑完 500 个用例，反馈循环极快。

**命名**：`{RuleId}_{Scenario}_Should{Expected}`，便于反查。

**必测反例**：每条规则至少 3 个"不应报"场景。误报控制主要靠反例，不是正例。

### 8.5 CI/CD

    jobs:
      build:       windows/ubuntu × SDK 8/9/10
      test:        L0 + L1 + L2
      integration: sample 构建 + SARIF 断言
      benchmark:   PR 对比基线，回归 > 10% 失败
      pack:        nupkg 内容清单断言
      docs:        规则文档一致性校验
      publish:     tag → OIDC 发布

**预算**：PR < 8 分钟；全量 < 25 分钟。

**个人开发的 CI 优化**：L0/L1 分 job 并行；benchmark 只在 `main` 跑（PR 上抽样）；NuGet 缓存 reference assemblies。

### 8.6 可观测性与调试

| 场景 | 手段 |
|------|------|
| Analyzer 调试 | `Debugger.Launch()` + `devenv /rootsuffix Roslyn` |
| Generator 输出 | `EmitCompilerGeneratedFiles=true` |
| 规则命中统计 | CLI JSON 输出 |
| 用户排障 | `heimdall doctor` |
| 崩溃保护 | 顶层 try/catch → `HDL9000` |

> `heimdall doctor` 是个人维护的杠杆：80% 的 issue 是"装了没生效"，自动化诊断能砍掉一半 support 成本。

### 8.7 安全

| 风险 | 措施 |
|------|------|
| 供应链攻击 | 包签名、2FA、OIDC、依赖锁定、`SECURITY.md` |
| 传递依赖污染 | `PrivateAssets=all` + 文档 |
| 代码外泄 | **零遥测、零网络请求**（README 显著位置） |
| JSON 注入 | Schema 校验 + 1MB 上限 + 深度上限 |
| 正则 DoS | 不使用用户 Regex |

---

## 9. 非功能设计

### 9.1 性能预算

| 指标 | 预算 |
|------|------|
| 冷启动额外编译 | < 8% |
| 增量编辑 P95 | < 30ms |
| Analyzer 峰值内存 | < 60MB |
| Generator（100 切片） | < 50ms |
| CLI 全量（100 万行） | < 60s |

### 9.2 兼容矩阵

| 宿主 | 最低版本 |
|------|----------|
| Visual Studio 2022 | 17.8 |
| Rider | 2024.1 |
| VS Code + C# Dev Kit | 最新 |
| .NET SDK | 8.0.100 |
| MSBuild（CI） | 随 SDK |
| Neovim + csharp-ls | 不承诺 |

### 9.3 稳定性

    规则生命周期：Proposed → Preview（默认 Off）→ Stable（默认 Warning）
                 → Deprecated → Removed（跨 major）

- 规则 ID 永不复用。
- 行为变更 = minor，默认 severity 变更 = major。
- 不做 `AnalysisMode`（第三方无需模仿官方的复杂模式）。

### 9.4 国际化

v1 仅英文诊断消息；中文文档独立维护；文案不用感叹号、不用 emoji、不用 "Oops"。

**Issue / PR 语言（决策 E2）**：英文为主，明确允许德语、法语、中文。回复时优先使用提交者所用的语言（在能力范围内），否则用英文。

---

## 10. 开发者体验

### 10.1 30 秒路径

| 入口 | 命令 | 见效时间 |
|------|------|----------|
| NuGet | `dotnet add package RungerHeimdall.Analyzers` → 跨层引用 | ~20s |
| CLI | `dotnet tool install -g RungerHeimdall.Cli` → `heimdall init` | ~40s |
| Playground | 网页粘贴代码 | ~5s |

**首屏 GIF 脚本（star 第一驱动力）**：
1. 0–3s：在 `Domain/Order.cs` 输入 `using MyApp.Infrastructure;`
2. 3–6s：红色波浪线 + hover 完整解释
3. 6–10s：改 `architecture.json` → 波浪线实时变化
4. 10–15s：`dotnet build` 同样报错 → CI 阻断

### 10.2 文案规范

    第 1 行：[规则ID] 什么错了（≤140 字符）
    第 2 行：为什么是问题
    第 3 行：怎么修（1–3 个具体建议）

**禁止**：模糊表述（"潜在问题"）、只有 ID 无解释、消息内放 URL、感叹号。

### 10.3 文档站

    /                          落地页（GIF + tagline + 安装）
    /docs/getting-started
    /docs/architecture
    /docs/performance
    /docs/generators
    /docs/cli
    /docs/ci
    /docs/troubleshooting
    /rules                     规则浏览器（筛选）
    /rules/ARCH1001
    /playground
    /blog

**规则文档由 `RuleDocGenerator` 从 Descriptor + 测试用例生成**，CI 校验一致性——这是文档永不漂移的唯一可靠做法，也是个人开发者的减负手段。

**视觉调性**：
- 深色为主，等宽字体标题
- 无圆角、无渐变、无霓虹色
- 主色：北欧冷灰（`#1C1E21` / `#E6E8EB`）+ 单一强调色（琥珀 `#C08A3E` 或冰蓝 `#7BA7BC`）
- Logo：项目用 **Strata**（六块向上楔形，底部中间块半透明），组织用 **Concentric**（三重同心六边形）；均为直角几何，无描边圆角
- 参考基调：Rust 文档站的克制、Vercel Docs 的排版、Linear 的密度

### 10.4 README 规范（交付物必备）

    1. 名称 + tagline（≤20 字）
    2. 首屏 GIF + 徽章（NuGet / CI / License）
    3. 为什么需要它（3 个 bullet）
    4. 30 秒上手（安装 + 最小配置 + 预期结果）
    5. 规则一览（表格 + 规则浏览器链接）
    6. 与现有方案对比
    7. 配置示例
    8. CLI 用法
    9. 兼容矩阵
    10. 贡献 + 路线图
    11. License

### 10.5 Playground

| 项 | 方案 | 风险 |
|----|------|------|
| 运行时 | Blazor WASM + Roslyn | 首次加载 8–12MB |
| 编辑器 | Monaco | 低 |
| 分析器 | 需 WASM 兼容验证（spike） | **高** |
| 分享 | URL base64（限 4KB） | 低 |

**分两步**：M2 先做"预置示例版"（不可输入任意代码），M4 做完整版。这样既不砍功能，也不在早期承担 WASM 改造风险。

### 10.6 MCP Server

    heimdall mcp（stdio）

    - find_violations(path?, ruleId?)
    - explain_rule(ruleId)
    - suggest_fix(violationId)
    - architecture_graph()

**价值**：让 AI 在写代码前"看见"架构约束。这是本项目对 P4 画像的唯一交付，也是 2026 年最强差异化。

---

## 11. 增长策略

### 11.1 飞轮

    零配置见效 → 截图分享 → 社区讨论 → 新用户 → 规则提案/PR
         → 规则变强 → 更好用 → …

### 11.2 发布事件（8 个）

| # | 事件 | 里程碑 | 预期 star |
|---|------|--------|-----------|
| 1 | v0.1 preview + Show HN | M1 | 200–800 |
| 2 | 博客《为什么 Clean Architecture 一定会腐化》 | M1 | 100–300 |
| 3 | v1.0 + Reddit + X | M2 | 400–1500 |
| 4 | 博客《C# 里 8 个编译器看不见的分配》 | M2 | 200–600 |
| 5 | Playground 上线 | M2 | 100–300 |
| 6 | .NET Conf / 社区演讲 | M3 | 100–400 |
| 7 | MCP 集成发布 | M3 | 300–1000 |
| 8 | 元编程包 v1 | M3/M4 | 300–800 |

### 11.3 内容计划（12 篇）

1. Clean Architecture 6 个月后必然腐化（以及怎么用编译器阻止）
2. 我们用 Roslyn 把架构规则变成了编译错误
3. C# 里 8 个编译器看不见的分配
4. `params` 数组：被忽视 20 年的性能陷阱
5. Roslyn 增量生成器的 7 个性能陷阱
6. 为什么分析器的误报比漏报更致命
7. 从 NetArchTest 迁移到编译期规则
8. 我们如何用 4 个真实仓库做误报回归
9. Roslyn 分析器在 VS / Rider / CI 的行为差异
10. 让 AI 写代码前先读架构规则（MCP 实践）
11. 分析器 NuGet 打包完全指南
12. 单人和 60 条规则的 24 个月（M4 复盘）

### 11.4 社区杠杆（个人开发的必需品）

| 杠杆 | 目标 |
|------|------|
| 规则提案 Issue 模板 | 社区提规则，我只做评审与合并 |
| 规则脚手架 | `dotnet new heimdall-rule` 生成规则 + 测试 + 文档骨架 |
| Good First Issue | 文档、文案、测试用例（不要求懂 Roslyn） |
| 贡献者指南 | 3 个反例 + 1 个真实案例的硬门槛 |
| 文档翻译 | 中文文档由社区维护 |

**目标**：M4 时 **30% 的 Stable 规则来自社区**。这不是口号——个人维护 60 条规则的回归测试是不可持续的，必须在此前建立分摊机制。

### 11.5 治理

| 项 | 设计 |
|----|------|
| 规则提案 | Issue 模板：问题、误报风险、真实案例、建议 severity |
| 评审 SLA | 7 天内首次评审 |
| 行为准则 | Contributor Covenant |
| 安全 | `SECURITY.md`，明确零遥测 |
| 弃用 | 提前 2 个 minor 通知 |

---

## 12. 路线图（个人开发 / 全量不删减）

### 12.1 前提与估算方法

**约束**：个人开发，三个方向全做，不删任何功能。

**估算口径**：以"可发布质量"计（含测试、文档、CI），非原型。

| 阶段 | 内容 | 工时 | 12h/周 | 25h/周 |
|------|------|------|--------|--------|
| M0 | 基础设施 | 60 | 5 周 | 2.5 周 |
| M1 | 架构旗舰 + 性能 D 类 + CLI v0 + 发布 v0.1 | 240 | 20 周 | 10 周 |
| M2 | 性能完整 + Playground + 文档站 + v1.0 | 320 | 27 周 | 13 周 |
| M3 | 元编程 + OpenAPI + MCP + 迁移工具 | 420 | 35 周 | 17 周 |
| M4 | 可视化 + Dashboard + 生态 + 元编程 v1 | 300 | 25 周 | 12 周 |
| **合计** | | **1340** | **112 周（26 个月）** | **54 周（13 个月）** |
| 含 20% buffer | | 1608 | **31 个月** | **16 个月** |

> 结论：**业余 12h/周 → 约 26–31 个月；半职 25h/周 → 约 13–16 个月**。这个数字不上任何美化修饰，因为它决定了 12.6 的"永不删除"承诺是否可以兑现。

### 12.2 M0 立项（第 1–2 周，60h）

| 任务 | 验收 |
|------|------|
| NuGet ID / 域名 / 商标检索 | `RungerHeimdall.*` 全部可注册 |
| 仓库骨架 + CI | `dotnet build` 绿灯，打包可检查 |
| 空规则打包验证 | sample 项目安装后 `dotnet build` 无报错 |
| 规则脚手架 | `dotnet new heimdall-rule` 生成可运行规则 + 测试 |
| Engine 骨架（4.5 接口） | 空实现 + L0 测试通过 |
| 品牌资产 | Logo 草图、配色、GIF 脚本 |

### 12.3 M1 旗舰（第 3–22 周，240h）

| 任务 | 验收 |
|------|------|
| 架构 8 条规则 | 单测 + 反例齐全 |
| JSON 配置 + Schema | VS Code 内补全 |
| 零配置推断 | 空配置下 sample 报出 ≥1 条 |
| 基线机制 | 5000 违规项目可灰度接入 |
| 性能 D 类 8 条 | 同包发布 |
| `heidmdall doctor` / `init` | 可用 |
| 文档页 + 首屏 GIF + README | 15s 内展示价值 |
| **发布 v0.1-preview** | NuGet 可安装 |

**M1 出口条件**：真实 Clean Architecture 项目从安装到第一条诊断 ≤ 60 秒。

### 12.4 M2 增长（第 23–49 周，320h）

| 任务 | 验收 |
|------|------|
| 性能 12 条补充规则 | 误报 < 1%（corpus） |
| 热路径 L1/L2/L3 | ASP.NET Core 零配置命中 |
| 每条规则基准数据 | BenchmarkDotNet 进文档 |
| CLI：analyze / baseline / doctor / report | SARIF 通过 Code Scanning |
| Playground（预置示例版） | 可分享 URL |
| 文档站 + 规则浏览器 | 可筛选 |
| 4 篇博客 + HN/Reddit | star ≥ 1k |
| **发布 v1.0** | 基线 3k 进程中 |

### 12.5 M3 生态（第 50–84 周，420h）

| 任务 | 验收 |
|------|------|
| 元编程：Endpoint/Validator/Mapper | 3 类产物可用 |
| OpenAPI 输入 | 契约 → 切片骨架 |
| Client 产物 | `System.Text.Json` 源生成 |
| MCP Server | Claude/Copilot 可调用 |
| NetArchTest 迁移工具 | 覆盖 80% 常见语法 |
| 环依赖 + 可视化（Mermaid） | 可嵌入 README |
| 架构 4 条补充规则 | ARCH1009–1012 |
| **star ≥ 3k（基线达成）** | |

### 12.6 M4 平台（第 85–109 周，300h）

| 任务 | 验收 |
|------|------|
| 元编程 v1（含 Client 全量 + 三宿主） | 端到端 sample |
| Playground 完整版 | 任意代码输入 |
| 架构 SVG 可视化 | 可导出 |
| Dashboard（商业验证） | 3 个付费意向 |
| 社区规则生态 | 3 个社区规则包 |
| 60 条 Stable 规则 | |

### 12.7 "永不删除"清单（文档级契约）

以下能力在任何里程碑都不得被移除，只允许调整交付时间与形态：

| # | 能力 | 最早交付 |
|---|------|----------|
| 1 | 架构规则分析（12 条） | M1/M3 |
| 2 | 零配置推断 | M1 |
| 3 | JSON 配置 + Schema | M1 |
| 4 | 基线机制 | M1 |
| 5 | 性能 D 类（12 条） | M1/M2 |
| 6 | 性能 C 类（8 条） | M2 |
| 7 | 热路径四层模型 | M2 |
| 8 | 基准数据与证据链 | M2 |
| 9 | CLI（analyze/baseline/doctor/report） | M2 |
| 10 | SARIF + CI 集成 | M2 |
| 11 | Playground（含完整版） | M2/M4 |
| 12 | 文档站 + 规则浏览器 | M2 |
| 13 | 元编程（7 类产物 × 3 宿主） | M3/M4 |
| 14 | OpenAPI 输入 | M3 |
| 15 | MCP Server | M3 |
| 16 | 迁移工具 | M3 |
| 17 | 架构可视化 | M3/M4 |
| 18 | VSIX（可选增强） | M5 |
| 19 | Dashboard | M4 |
| 20 | 社区规则生态 | M4 |

### 12.8 个人开发的可持续性设计

| 风险 | 对策 |
|------|------|
| 单点故障 | 文档化一切流程；关键决策写入 `docs/adr/`；至少 1 名 co-maintainer（M3 前落实） |
| 节奏中断 | 每 2 周一个 preview，不憋大招；发布流程全自动 |
| 倦怠 | M3 后必须完成"社区分摊"（30% 规则来自社区），否则 M4 无限期延后而非删除 |
| 范围蔓延 | 新需求只进 backlog，不插队；插队需等量移除（"等量交换"原则） |
| 支持成本 | `heimdall doctor` + 文档搜索 + issue 模板分流 |

**触发降级预案**（不删功能，只延后）：若连续 8 周投入 < 6h/周，则冻结 M4，把 M3 交付物转为 Preview，并公开说明。**绝不静默消失**——这是开源项目信誉的底线。

---

## 13. 风险登记册

| # | 风险 | 概率 | 影响 | 缓解 | 触发信号 |
|---|------|------|------|------|----------|
| R1 | 万星不可达 | 高 | 高 | 基线已调整为 3k；10k 为拉伸 | 12 个月 < 800 |
| R2 | 误报导致卸载 | 高 | 高 | 分级 + corpus + 默认保守 | 规则关闭率 > 30% |
| R3 | **个人单点故障** | 高 | 高 | co-maintainer + 全流程文档化 | 连续 4 周无 commit |
| R4 | **范围蔓延压垮个人** | **高** | **高** | 等量交换原则 + 里程碑冻结 | 里程碑延期 > 8 周 |
| R5 | "可组合"预期落差 | 高 | 中 | 文案严格区分产物/生成器 | 跨 generator 组合的 issue |
| R6 | 与 Sonar/官方冲突 | 中 | 中 | 差异化定位，不做重叠规则 | 官方发布类似功能 |
| R7 | 分析器性能拖慢 IDE | 中 | 高 | 性能门禁 + 大仓测试 | "卡"类反馈 |
| R8 | Roslyn 5.x breaking | 中 | 中 | 锁 4.8 + 5.x 分支 | SDK 10 后报错 |
| R9 | 维护疲劳 | 高 | 高 | 社区分摊 + 自动化 | issue 积压 > 60 天 |
| R10 | 名称/商标冲突 | 中 | 中 | M0 检索 + 3 个备选 | — |
| R11 | Generator 与 Hot Reload | 中 | 低 | 跟进版本 + 文档 | 编辑后不刷新 |
| R12 | AI 工具自建护栏 | 中 | 高 | 抢先成为 MCP 标准层 | GitHub 发布类似功能 |
| R13 | **Playground WASM 不可行** | 中 | 中 | M2 前 spike；预置示例版兜底 | spike 失败 |
| R14 | 60 条规则长期回归成本 | 高 | 中 | L0 快速测试 + 规则生命周期管理 | 回归测试 > 10 分钟 |

---

## 14. 隐形需求清单

### 14.1 技术（A 类）

| # | 需求 | 为什么隐形 | 做法 | 优先级 |
|---|------|-----------|------|--------|
| A1 | Analyzer 必须 netstandard2.0 | net8.0 在 VS 加载失败且报错隐晦 | M0 固定 + 打包校验 | P0 |
| A2 | Roslyn baseline 锁 4.8.0 | 过高切断 VS 17.x | CI 加 17.8 兼容测试 | P0 |
| A3 | 零第三方依赖（除 STJ） | 与 IDE 宿主版本冲突 | 白名单 + 内容断言 | P0 |
| A4 | 增量生成器可比较性 | 返回 Symbol 导致每次按键全量生成 | pipeline 只传 record + CI 断言 | P0 |
| A5 | 生成代码自标记 | 否则被自身 analyzer 分析 | 首行 + `[GeneratedCode]` | P0 |
| A6 | 规则 ID 永不复用 | 用户 CI 依赖 | ID 注册表 + CI 校验 | P0 |
| A7 | CancellationToken 全链路 | 忽略导致 IDE 无法取消 | review 检查 + 超时单测 | P0 |
| A8 | 缓存有界且不持 Symbol | 内存泄漏 | 字符串键 + LRU 10k | P0 |
| A9 | 默认 severity 策略 | 全 Error 直接毁构建 | D 类 Warning、C 类 Suggestion | P0 |
| A10 | 基线机制 | 存量无法接入 | M1 必做 | P0 |
| A11 | `PrivateAssets=all` 文档 | 库作者不设会传染下游 | README + doctor 提示 | P1 |
| A12 | Abstractions 不得 PrivateAssets | 生成代码引用不到 Attribute | 打包断言 | P1 |
| A13 | SourceLink + 确定性构建 | 调试与复现 | CI 校验 | P1 |
| A14 | 规则文档与代码一致 | 文档漂移是通病 | 自动生成 + CI 校验 | P1 |
| A15 | 多 IDE 行为差异 | VS/Rider/VSCode 处理不同 | 至少验证 3 环境 | P1 |
| A16 | 新语法覆盖 | 语法演进导致漏报 | 每次 SDK 大版本加回归 | P1 |
| A17 | FixAll 幂等 | 批量修复毁代码 | 仅 D 类提供 FixAll | P1 |
| A18 | AOT/trim 安全 | NativeAOT 项目会崩 | 生成代码禁反射 | P1 |
| A19 | `global using` 冲突 | 生成代码 using 冲突 | 全限定名，不生成 using | P1 |
| A20 | 测试项目默认排除 | 噪音 | 按项目名/TestSdk 检测 | P1 |
| A21 | `heimdall doctor` | "装了没生效"最常见 | M1 必做 | P1 |
| A22 | SARIF | 企业 CI | M2 | P1 |
| A23 | 生成器确定性 | 每次构建 diff | 快照校验 | P1 |
| A24 | 跨平台路径 | `\` 与 `/` 混用 | 统一规范化 | P1 |
| A25 | 大小写敏感比较 | Linux 上误判 | 全部 Ordinal | P1 |
| A26 | SupportedDiagnostics 完整性 | 否则 AD0001 | 单测校验 | P2 |
| A27 | Analyzer 异常兜底 | 崩溃显示 | try/catch → HDL9000 | P2 |
| A28 | per-file 配置语义 | 用户误解作用域 | 文档 + doctor 显示 | P2 |
| A29 | `obj/` 与生成代码混淆 | 误报源 | SourceGuard | P2 |
| A30 | 强命名策略 | 企业要求 | 决定：不签名 | P2 |
| A31 | VB 支持 | 可能被要求 | v1 明确不支持 | P2 |
| A32 | Playground WASM 兼容 | 分析器可能用不可 WASM API | M2 前 spike | P2 |
| A33 | 零遥测声明 | 企业敏感 | README + SECURITY.md | P1 |
| A34 | EnabledByDefault 可见性 | 用户不知为何没报 | 文档 + doctor | P2 |
| A35 | 多 TFM 重复诊断 | net8/net9 各报一次 | 按 TFM 去重 | P2 |
| A36 | **Engine 与宿主解耦** | 否则规则写两遍 | 4.5 设计 + 架构测试（禁止 Engine 引用 AnalysisContext） | **P0** |
| A37 | **Emitter 框架统一** | 7×3 模板失控 | 7.5 设计 | P1 |
| A38 | **规则脚手架** | 个人写 60 条规则的唯一出路 | M0 交付 | P0 |
| A39 | **L0 快速测试** | 反馈循环决定迭代速度 | 8.4 设计 | P0 |
| A40 | **文档生成器** | 60 条规则的手工文档不可维护 | M1 交付 | P1 |

### 14.2 产品/运营（B 类）

| # | 需求 | 优先级 |
|---|------|--------|
| B1 | README 首屏（用户明确要求） | P0 |
| B2 | 首屏 GIF | P0 |
| B3 | 规则浏览器 | P1 |
| B4 | 迁移指南 | P1 |
| B5 | 规则提案流程 | P1 |
| B6 | 贡献者脚手架 | P1 |
| B7 | 双周发布节奏 | P1 |
| B8 | 商标与域名检索 | P0 |
| B9 | NuGet ID 抢占 | P0 |
| B10 | 组织与仓库结构 | P1 |
| B11 | 许可证 + DCO | P1 |
| B12 | 行为准则 | P2 |
| B13 | 安全策略 | P1 |
| B14 | 支持矩阵 | P2 |
| B15 | 赞助与商业化 | P2 |
| B16 | 文档中英双语（社区维护） | P2 |
| B17 | 基准可复现 | P1 |
| B18 | 品牌视觉（专业克制） | P1 |
| B19 | 示例仓库 | P1 |
| B20 | MCP 集成 | P2（战略高） |
| B21 | CI 集成文档 | P1 |
| B22 | 错误码查询页 | P1 |
| B23 | NuGet 包 README | P1 |
| B24 | **co-maintainer 招募** | **P0**（M3 前） |
| B25 | 品牌叙事（Heimdall 守望 Bifrost） | P1 |

### 14.3 组织/资源（C 类）

| # | 需求 | 说明 |
|---|------|------|
| C1 | 投入评估：12h/周 vs 25h/周 | 决定 26 个月 vs 14 个月 |
| C2 | 每周固定发布窗口 | 可持续性 |
| C3 | 每条规则 3 反例 + 1 真实案例 | 质量门槛 |
| C4 | issue 分级（误报 > 功能 > 文档 > 新规则） | 支持成本 |
| C5 | 弃用提前 2 个 minor | 用户预期 |
| C6 | **每月一次"删除日"**：清理过期豁免、失效基线、陈旧 issue | 防止技术债累积 |
| C7 | **每季度一次架构复盘** | 防止 Engine 耦合回潮 |

---

## 15. 决策记录

### 15.1 已决策（v1.0 闭合）

| # | 决策项 | 结论 | 影响 |
|---|--------|------|------|
| M1 | 品牌 | **RungerHeimdall** | 全部包名、命名空间、文档 |
| M2 | 范围 | **三个全做**，架构旗舰 + 性能同包 v1 + 元编程 M3 | 路线图 26–31 个月 |
| M3 | 成功基线 | **3k star + 50k 月下载 + 20 贡献者** | 增长指标、验收标准 |
| M4 | 规则配置 | v1 = 约定推断 + JSON；C# DSL 留 v2 | 第 5.3 节 |
| M5 | 元编程定位 | **三者整合**（VS 核心 + DTO 产物 + OpenAPI 输入） | 第 7.1 节 |
| M6 | 人力 | **个人开发，不删任何功能** | 第 12 章、4.5 架构红线 |
| M7 | 许可证 | MIT（确认见 15.2 A1） | 打包元数据 |
| M8 | 域名/命令 | `rungerheimdall.github.io/rungerheimdall/`（免费）/ `heimdall`（M0 检索冲突）；`.dev` 延后 | 文档、CLI |

### 15.2 决策记录（36 项，已全部闭合）

**结论汇总**（2026-11-23 确认；未特别说明的项均按"我的建议"采纳）：

| # | 结论 |
|---|------|
| A1 | 许可证 **MIT** |
| A2 | **零成本方案**：GitHub 组织 + NuGet + GitHub Pages 全部免费；文档域名用 `rungerheimdall.github.io/rungerheimdall/`；`rungerheimdall.dev` 延后至有赞助收入再注册；CLI `heimdall`（冲突则 `dotnet-heimdall`）；社交账号按需注册 |
| A3 | Engine **独立包** `RungerHeimdall.Engine` |
| A4 | 主色 **琥珀 `#C08A3E`**；Logo **自绘**（外包需预算，暂缓） |
| A5 | GitHub **组织** `rungerheimdall` |
| A6 | 版本起步 **`0.1.0-preview.1`** |
| B1 | 零配置推断 **默认开启**，仅 5 条高置信规则 |
| B2 | v1 架构规则 **全部 Warning**，Strict 预设才升 Error |
| B3 | 三档预设 **Minimal / Recommended（默认）/ Strict** |
| B4 | 基线 **必须进版本控制**；CI 校验默认关、可开启 |
| B5 | 层名支持 **主约定 + 扩展约定** 两套 |
| B6 | 豁免过期报 **HDL9001（Info）** |
| B7 | 诊断 **v1 仅英文** |
| B8 | **接受** 12.8 降级预案 |
| C1 | **不签名** |
| C2 | 测试识别 **Test SDK 为主 + 项目名辅助** |
| C3 | Playground **M2 预置示例 → M4 完整版** |
| C4 | 文档站 **GitHub Pages**（Cloudflare Pages 因控制台入口不可用而放弃） |
| C5 | 生成代码 **默认跳过 + `analyze_generated_code` 开关** |
| C6 | 热路径传播 **默认开** |
| C7 | Attribute 命名 **`[HeimdallSlice]`** |
| C8 | 最低 **C# 11** |
| C9 | OpenAPI **3.0 + 3.1** |
| C10 | M3 宿主 **Minimal API / FastEndpoints / MediatR**；Controller 排 v1.1（保留） |
| D1 | co-maintainer **尽力而为，M4 启动前必须有 1 人** |
| D2 | M4 资源不足 **无限期延后，不删除，公开说明** |
| D3 | 商业化 **v1 纯赞助，M4 验证** |
| D4 | 社区共治 **M3 后下放规则评审，核心架构保留** |
| D5 | VSIX **M5 评估** |
| D6 | 商标 **早期不注册，3k star 后评估** |
| D7 | 零遥测 **永久承诺** |
| D8 | Stable 规则 **封顶 60 条** |
| E1 | "Runger" 释义 **待补** |
| E2 | Issue / PR **英文为主，允许德语、法语、中文** |
| E3 | 品牌风格指南 **M0 做一页简版** |
| E4 | 中文文档 **社区维护，你审核术语** |

**遗留待补（2 项，不阻塞开工）**：

| # | 遗留项 | 说明 |
|---|--------|------|
| E1a | "Runger" 的官方释义 | 需你提供 1 句话，用于品牌故事与 FAQ |
| A4a | Logo 具体产出方式 | 自绘还是外包，M0 结束前定 |

#### 原始决策依据（保留供追溯）

> 标注说明：`决策型` = 需要你选择方向；`确认型` = 已有推荐，只需你确认或否决。
> 若超过截止里程碑仍未决策，按"我的建议"执行，并记录假设到 `docs/adr/`。

#### A 组：阻塞 M0（立项前必须定，6 项）

| # | 需要你定什么 | 选项 | 我的建议 | 为什么必须你定 | 类型 |
|---|--------------|------|----------|----------------|------|
| A1 | 许可证 | MIT / Apache-2.0 | **MIT** | 影响企业法务采用意愿与后续商业化空间，不可中途更改 | 决策型 |
| A2 | 品牌资产一次性注册 | 域名：`rungerheimdall.dev` / 仅 GitHub Pages；CLI：`heimdall` / `dotnet-heimdall` / `hdall`；社交账号：X / Bluesky / Reddit | 全部注册；CLI 优先 `heimdall`，冲突则 `dotnet-heimdall` | 域名与命令名一旦发布极难更改；且需你承担注册成本 | 决策型 |
| A3 | **Engine 打包形态** | a) 合并单 DLL；b) Engine 独立包；c) 双程序集名 | **b** | 直接决定包数量、用户安装方式与维护成本，见下方技术说明 | 决策型 |
| A4 | 品牌主色与 Logo 产出 | 主色：琥珀 `#C08A3E` / 冰蓝 `#7BA7BC`；Logo：自绘 / 外包 / AI 生成后精修 | 琥珀 + 自绘或外包（避免 AI 生成痕迹） | 视觉基调决定"专业还是廉价"的第一印象，与你个人审美直接相关 | 决策型 |
| A5 | 账号形态 | GitHub 组织（`rungerheimdall`）/ 个人账号 | **组织** | 组织便于未来转移、添加 co-maintainer、开 Sponsors；个人账号 star 归属个人 | 决策型 |
| A6 | 版本起步 | `0.1.0-preview.1` / `0.0.1` / `1.0.0-preview.1` | **`0.1.0-preview.1`** | 0.x 允许 breaking，诚实表达成熟度；1.0 应在 M2 才发 | 决策型 |

**A3 的技术背景（本组唯一有硬技术后果的一项）**：

- **方案 a（合并单 DLL）**：把所有规则打进一个 `RungerHeimdall.Analyzers.dll`。优点：零冲突、打包最简单。缺点：用户无法"只装架构规则"，且单文件会越来越大，加载影响所有用户。
- **方案 b（Engine 独立包）**：`RungerHeimdall.Engine` 作为独立 NuGet 包，Architecture 与 Performance 两个包各自依赖它。NuGet 会把 Engine.dll 放入 `analyzers/dotnet/cs/`，两个分析器共用同一份，无同名冲突。CLI 也能直接依赖 Engine。缺点：用户 `packages.config` 里会看到 3 个包。
- **方案 c（双程序集名）**：两个分析器各自内联一份改名的 Engine（`Architecture.Engine.dll`）。代码重复，维护成本翻倍——**与"个人开发"直接冲突，不建议**。

#### B 组：阻塞 M1（首次体验与默认行为，8 项）

> 本组决定"装上之后第一分钟发生什么"，是卸载率的分水岭。

| # | 需要你定什么 | 选项 | 我的建议 | 为什么必须你定 | 类型 |
|---|--------------|------|----------|----------------|------|
| B1 | 零配置推断是否默认开启 | 默认开启（opt-out）/ 需显式启用（opt-in） | **默认开启，但仅 5 条高置信度规则、Warning 级** | 这是最大的产品风险点：推断错误会让用户在没犯错时看到红波浪线 | 决策型 |
| B2 | 首次安装默认严重级别 | 架构规则 Error / Warning | **v1 全部 Warning**；`heimdall init --strict` 才升 Error | 文档 5.10 原写 Error，但这与"误报致命"原则冲突。Error 会直接让用户构建失败，这是第一卸载原因 | 决策型 |
| B3 | 默认规则集与预设包 | 全部 Stable 启用 / 提供 Minimal / Recommended / Strict 三档 | **三档预设，默认 Recommended** | 决定"装上就有多少噪音"，也决定企业用户的接入路径 | 决策型 |
| B4 | 基线强制策略 | 必须进版本控制 / 可选；CI 校验"只能缩小" 开 / 关 | **必须进版本控制；CI 校验默认关、可开启** | 基线是存量项目的唯一接入路径；CI 校验过严会引发抵触 | 决策型 |
| B5 | 默认层名约定 | 只认 `Domain/Application/Infrastructure/Api` / 再扩展 `Core/Shared/Modules/Web/Endpoints/Host` | **支持两套主约定**，冲突时以 `architecture.json` 为准 | 只认一套会漏掉大量真实项目；扩展太多会误判 | 决策型 |
| B6 | 豁免过期后的行为 | 报 `HDL9001` / 静默恢复规则 / 仅 `doctor` 提示 | **报 `HDL9001`（Info 级）** | 静默恢复会导致用户"某天突然红屏"，比提醒更糟 | 决策型 |
| B7 | 诊断消息语言 | 英文 / 中英双语 | **v1 仅英文** | 双语会显著增加文案维护量，且影响首屏演示的统一性 | 决策型 |
| B8 | 是否接受"降级预案"写入文档 | 接受 / 改为纯延后无公告 | **接受**（12.8 的连续 8 周 < 6h/周 → 冻结 M4 并公开说明） | 这是开源信誉问题，必须你本人认可才能写进对外文档 | 决策型 |

#### C 组：阻塞 M2/M3（10 项）

| # | 需要你定什么 | 选项 | 我的建议 | 为什么必须你定 | 类型 |
|---|--------------|------|----------|----------------|------|
| C1 | 是否签名程序集 | 强名称签名 / 不签名 | **不签名** | 签名会约束依赖版本并增加构建复杂度；只有明确企业需求时才值得 | 决策型 |
| C2 | 测试项目识别策略 | 仅项目名含 `Tests` / 引用 Test SDK / 两者兼有 | **引用 Test SDK 为主 + 项目名辅助** | 误排除业务项目或漏排除测试项目都会产生大量噪音 | 决策型 |
| C3 | Playground 开放程度 | 公开任意代码输入 / 预置示例版 / 仅本地 | **M2 预置示例版 → M4 完整版** | 任意代码输入涉及 WASM 兼容性与滥用风险，需要你接受分两步 | 决策型 |
| C4 | 文档站托管 | GitHub Pages / Cloudflare Pages / Vercel | **Cloudflare Pages**（构建快、自定义域名免费） | 涉及账号、DNS、成本与国内访问速度 | 决策型 |
| C5 | 生成代码是否默认分析 | 默认跳过 / 默认分析 / 提供开关默认跳过 | **默认跳过 + `analyze_generated_code` 开关** | 默认分析会让 EF Core / System.Text.Json 的生成代码刷屏 | 决策型 |
| C6 | 热路径 1 层传播默认 | 开 / 关 | **开**（可 `.editorconfig` 关闭） | 开则覆盖更广但可能误伤被热路径顺带调用的冷方法 | 决策型 |
| C7 | 元编程 Attribute 命名 | `[RungerSlice]` / `[HeimdallSlice]` / `[Slice]` | **`[HeimdallSlice]`** | `[Slice]` 过于通用易冲突；`RungerSlice` 过长；命名一旦发布不可改 | 决策型 |
| C8 | 元编程最低 C# 版本 | C# 9 / C# 11 / C# 12 | **C# 11**（使用 `required` / `init`） | 决定可服务的项目范围；C# 11 是 .NET 7+ 的默认，覆盖 SDK 8 用户 | 决策型 |
| C9 | OpenAPI 版本支持 | 仅 3.1 / 3.0 + 3.1 / 含 2.0 | **3.0 + 3.1，不支持 2.0** | 2.0 转换器成本高且用户占比低；需你确认目标用户契约版本 | 决策型 |
| C10 | 元编程宿主范围（v1） | Minimal API / FastEndpoints / MediatR / Controller | **M3 前三种，Controller 排 v1.1（仍在清单，不删）** | 涉及第 12.7 永久清单的排期表述，需你确认"排期"不等于"删除" | 决策型 |

#### D 组：战略级（影响 12 个月后，8 项）

| # | 需要你定什么 | 选项 | 我的建议 | 为什么必须你定 | 类型 |
|---|--------------|------|----------|----------------|------|
| D1 | co-maintainer 是硬约束还是尽力而为 | 找不到则冻结 M4 / 尽力而为 | **尽力而为，但 M4 启动前必须有至少 1 人** | 涉及你个人对"控制权"与"可持续性"的权衡 | 决策型 |
| D2 | M4 资源不足时如何处理 | 无限期延后 / 转社区 / 降级为文档方案 | **无限期延后，但不删除、公开说明** | 与"不砍任何东西"的承诺直接相关，必须你定口径 | 决策型 |
| D3 | 商业化路径 | 纯赞助 / Open Core / SaaS / 不做 | **v1 纯赞助，M4 再验证** | 影响是否引入 license 校验（会污染架构）与开源纯粹性 | 决策型 |
| D4 | 社区共治程度 | 规则合并权保留 / 下放给 co-maintainer / 全下放 | **M3 后下放规则评审，核心架构保留** | 这是"个人项目"向"社区项目"转变的关键节点 | 决策型 |
| D5 | VSIX 排期 | M5 做 / 永不做 | **M5 评估** | 涉及你愿不愿意投入 VS 扩展的审核与签名流程 | 决策型 |
| D6 | 是否注册商标 | 注册 / 不注册 | **不注册（早期），星达 3k 后再评估** | 成本与时间投入，且需你判断品牌长期价值 | 决策型 |
| D7 | 零遥测是否永久承诺 | 永久承诺 / 保留未来引入可能 | **永久承诺，写入 README** | 一旦写入就是对外契约，不可反悔 | 确认型 |
| D8 | Stable 规则数量是否封顶 60 条 | 封顶 / 不封顶 | **封顶 60 条，超出转入社区扩展包** | 防止规则无限膨胀压垮个人维护能力；需你接受"60 条即完整"的定义 | 决策型 |

#### E 组：品牌与叙事细节（4 项）

| # | 需要你定什么 | 选项 | 我的建议 | 为什么必须你定 | 类型 |
|---|--------------|------|----------|----------------|------|
| E1 | "Runger" 的官方释义 | 你给出释义 / 不解释（视为代号） | 给出 1 句释义 | 品牌故事需要它；且若被问及无答案会显得随意 | 决策型 |
| E2 | Issue / PR 语言 | 仅英文 / 中英均可 | **英文为主，允许中文** | 影响国际贡献者参与度与你自己的回复负担 | 决策型 |
| E3 | 是否做品牌风格指南 | 做 / 不做 | **M0 做一页简版**（配色、字体、Logo 用法、禁用项） | 后续 GIF、文档站、NuGet 图标全部依赖它 | 确认型 |
| E4 | 中文文档归属 | 你维护 / 社区维护 / 不提供 | **社区维护，你只审核术语** | 涉及质量责任与你的时间投入 | 决策型 |

#### 决策优先级建议

若你只想先回答最关键的几项，按此顺序：

1. **A3（Engine 打包形态）** —— 决定项目骨架，不定无法写代码。
2. **B1 + B2（零配置推断 + 默认严重级别）** —— 决定首屏体验与卸载率，是产品成败的核心。
3. **A1（许可证）+ A5（账号形态）** —— 决定能否开始公开发布。
4. **D1 + D8（co-maintainer 与规则封顶）** —— 决定 12 个月后项目是否还活着。
5. 其余项可在对应里程碑前给出，不阻塞开工。

### 15.3 决策对文档的影响（可追溯）

| 决策 | 影响章节 |
|------|----------|
| M1 | 3.3、3.4、8.1、8.3、10、16 |
| M2 | 1.2、12、13 |
| M3 | 1.3、2.3、11.3 |
| M4 | 5.3 |
| M5 | 7.1、7.4 |
| M6 | 4.5、12.1、12.8、14.1(A36–A40)、14.2(B24) |
| M7 / A1 | 8.3 |
| A3 | 3.3、8.3 |
| B1 / B2 / B3 / B5 | 5.3、5.4、5.10 |
| C7 / C8 / C9 / C10 | 7.2、7.3、7.4 |
| D1 / D2 / D8 | 12.7、12.8、13 |
| E2 | 9.4、11.5、16.1 |
| E4 | 11.4、16.1 |

---

## 16. 附录

### 16.1 品牌规范

| 项 | 内容 |
|----|------|
| 项目全称 | RungerHeimdall |
| 组织名 | Rungerheimdall（GitHub 组织显示名；URL slug 为小写 `rungerheimdall`） |
| 词源 | Heimdall：北欧神话中守望 Bifrost 桥的守护者 |
| Tagline（EN） | Heimdall watches the bridge. We watch your architecture. |
| Tagline（ZH） | 守护你的架构边界 |
| 读法 | `RUN-ger-HIME-dahl` |
| 包前缀 | `RungerHeimdall` |
| CLI | `heimdall` |
| 规则前缀 | `ARCH` / `PERF` / `META` / `HDL9xxx` |
| 项目 logo | **Strata**：六个方块组成向上楔形，底部中间块 35% 透明度表示被阻断的依赖（`brand/project/`） |
| 组织 logo | **Concentric**：三重同心六边形，最内层实心（`brand/org/`） |
| Logo 产出 | 自绘 SVG + Pillow 导出的 PNG（零预算） |
| Issue / PR 语言 | 英文为主，允许德语、法语、中文 |
| 主色 | `#C08A3E`（琥珀）/ `#8A5E22`（阴影）/ `#141618`（深色底）/ `#E6E8EB`（浅色纸） |
| 禁用 | 圆角、渐变、紫绿霓虹、拟物化、emoji、组织/项目 logo 混用 |

> 规则 ID 保留语义前缀而非品牌前缀：`ARCH1001` 比 `HDL1001` 更易读，也便于文档聚合。品牌只出现在包名与工具诊断（`HDL9xxx`）。

### 16.2 规则 ID 规范

    ARCH1001–1099   架构规则
    PERF1001–1099   性能 D 类
    PERF2001–2099   性能 C 类
    META1001–1099   元编程/生成器
    HDL9000–9099    工具自身（配置错误、过期豁免、内部异常）

### 16.3 诊断文案模板

    {规则ID}: {主体} {动作} {对象}。
    {为什么是问题}。
    {建议 1}；{建议 2}。

    示例：
    ARCH1001: Domain 层不允许依赖 Infrastructure 层。
    此处使用了 SqlOrderRepository（MyApp.Infrastructure.Persistence）。
    依赖方向必须由外层指向内层（Api → Application → Domain）。
    在 Domain 定义 IOrderRepository 接口；在 Infrastructure 实现；在 Api 层注入。

    PERF1001: 此处调用 WriteLine(string, params object[]) 未传参，
    会分配 24 字节的空数组。热路径每秒 1 万次即 240KB/s 的 GC 压力。
    改用 WriteLine(string) 重载，或传入 Array.Empty<object>()。

### 16.4 竞品核验脚本（M0）

    # GitHub star
    curl -H "Authorization: token $GH_TOKEN" \
         https://api.github.com/repos/dotnet/roslynator | jq '.stargazers_count'

    # NuGet 下载
    curl https://api.nuget.org/v3/registration5-gz-semver2/meziantou.analyzer/index.json \
         | jq '[.items[].items[].packageRegistration.totalDownloads] | add'

**核验清单**：Roslynator、Meziantou.Analyzer、StyleCop.Analyzers、ErrorProne.NET、NetArchTest、ArchUnitNET、Mapperly、Refit、FluentValidation、Metalama、CommunityToolkit.Mvvm。

### 16.5 参考项目

| 项目 | 学习点 |
|------|--------|
| Roslynator | 规则组织、VSIX 分发、CLI 设计 |
| Meziantou.Analyzer | 文档自动化、测试组织 |
| Mapperly | 增量生成器性能、快照测试 |
| NetAnalyzers | 官方规则设计、ID 规范 |
| CommunityToolkit.Mvvm | partial 方法契约的 DX 设计 |
| NetArchTest | 架构规则表达（迁移来源） |
| Metalama | 编译期织入的边界 |
| TryRoslyn | WASM 版 Roslyn 可行性 |

### 16.6 术语表

| 术语 | 含义 |
|------|------|
| Analyzer | Roslyn 分析器，编译期产生诊断 |
| Code Fix | 诊断对应的自动修复 |
| Generator | 源生成器 |
| Incremental Generator | 支持增量缓存的生成器 API |
| Diagnostic | 编译器诊断 |
| Descriptor | 诊断元数据 |
| AdditionalFiles | 传给分析器的额外文件 |
| AnalyzerConfigOptions | `.editorconfig` 强类型访问 |
| SARIF | 静态分析结果交换格式 |
| Baseline | 存量问题冻结机制 |
| Vertical Slice | 按功能垂直切分的架构风格 |
| 热路径 | 高频执行的代码路径 |
| Bifrost | 北欧神话中的彩虹桥，本项目隐喻架构边界 |

### 16.7 本文档未覆盖（后续补充）

- 详细公开 API 签名（`IRule`、`IEmitter` 的完整契约）—— M0 实现时定稿
- ADR 目录（架构决策记录）—— M0 建立
- 商业化财务模型 —— M4 前
- 详细人力排班 —— 取决于 12h/25h 选择

---

## 17. 结语：五条不可妥协的原则

1. **误报比漏报更致命。** 分析器类产品的死因几乎都是"太吵"。宁可少 20 条规则，不可多 1% 误报。
2. **逻辑只写一次。** 规则引擎与宿主解耦是"个人 + 全量"能成立的技术前提，任何破坏它的提交都应被拒绝。
3. **演示优先于完备。** 万星靠 15 秒 GIF，不是 200 条规则。每条规则都要问："它能被拍进 GIF 吗？"
4. **诚实的技术叙事。** 明确说"不检测运行时性能""生成器之间不能组合"。专业开发者对夸大宣传的容忍度为零，对坦诚的边界说明反而会给 star。
5. **不删功能，但可以慢。** 26–31 个月不是失败，是个人开发者的物理约束。真正的失败是静默放弃或质量塌方。