# Analyzer Warning Reduction 追踪

## 2026-04-23 — RP-026

### 阶段：PR #269 failed-test follow-up（RP-026）

- 启动复核：
  - 使用 `$gframework-pr-review` 抓取当前分支 PR #269 的 test report，确认最新失败信号来自
    `SchemaConfigGeneratorTests.Run_Should_Assign_Globally_Unique_Reference_Metadata_Member_Names`
  - 本地复测前先对 `GFramework.SourceGenerators.Tests` 执行 `dotnet restore -p:RestoreFallbackFolders=""`，
    规避当前 WSL worktree 仍残留的 Windows NuGet fallback package folder 资产干扰
- 决策：
  - 保持 `SchemaConfigGenerator` 当前 `GF_ConfigSchema_014` 语义不变；PR 失败是测试输入陈旧，而不是生成器行为回退
  - 将用例改写为“合法 schema 路径在 reference metadata member name 上碰撞”的场景，继续覆盖全局唯一后缀分配逻辑
- 实施调整：
  - 将测试 schema 从根级 `drop-items` / `drop_items` 非法同层冲突改为 `drop.items`、`drop.items1`、`dropItems`、
    `dropItems1` 的合法组合
  - 更新断言，验证 `MonsterConfigBindings.g.cs` 中继续生成 `DropItems`、`DropItems1`、`DropItems2` 与 `DropItems11`
- 验证结果：
  - `dotnet restore GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -p:RestoreFallbackFolders="" -nologo`
    - 结果：通过
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~Run_Should_Assign_Globally_Unique_Reference_Metadata_Member_Names -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`1 Passed`，`0 Failed`
    - 说明：`GFramework.SourceGenerators.Tests` 在构建阶段仍会打印既有 `MA0048`、`MA0051`、`MA0004` warning；本轮未扩展到该测试项目的 warning 清理
- 下一步建议：
  - 若继续收口 PR #269，可再次抓取最新 test report / open thread，确认是否还有新的 CI 失败信号
  - 若回到 analyzer 主线，优先决定是否为 `GFramework.SourceGenerators.Tests` 单独开一轮 warning 清理切片

## 2026-04-23 — RP-025

### 阶段：PR #269 第五轮 review follow-up 与模块 build / warning 治理补充（RP-025）

- 启动复核：
  - 继续使用 `$gframework-pr-review` 读取 PR #269 当前 latest review、outside-diff comment、nitpick comment 与 open-thread 摘要
  - 本地核对后确认 `SchemaConfigGenerator` 的取消传播、根 `type` 非字符串防御、`ContextAware` 冲突快照与
    `Cqrs` error type 线程均已是陈旧信号；仍成立的是归一化字段名冲突与 `dynamic` 运行时类型引用问题
- 决策：
  - `SchemaConfigGenerator` 不复用 `GF_ConfigSchema_006`，改为新增专门的冲突诊断 `GF_ConfigSchema_014`，
    避免把“标识符非法”和“归一化后重名”混成同一类错误
  - `CqrsHandlerRegistryGenerator` 对 `dynamic` 采用“生成期归一化为 `global::System.Object`”策略，而不是退回更宽泛的
    fallback 路径，保持精确注册能力且避免发射 `typeof(dynamic)`
  - `AGENTS.md` 增加模块级 build / warning 治理规则，要求后续改代码时必须对受影响模块跑 Release build，并处理或显式报告 warning
- 实施调整：
  - 为 `SchemaConfigGenerator` 增加对象级生成属性名登记 helper，在 `ParseObjectSpec(...)` 中拦截 `foo-bar` /
    `foo_bar` 这类归一化后冲突，并新增 `ConfigSchemaDiagnostics.DuplicateGeneratedIdentifier`
  - 为 `SchemaConfigGeneratorTests` 补上冲突诊断回归测试；为 `CqrsHandlerRegistryGeneratorTests` 收紧
    unresolved-type 断言并新增 `dynamic` 类型归一化回归测试
  - 为 `CqrsHandlerRegistryGenerator.RuntimeTypeReferences` 增加 `TypeKind.Dynamic` 归一化处理，并保持
    `TypeKind.Error` 的保守回退
  - 为 `AGENTS.md` 补充“受影响模块必须独立 build 且 warning 不能默认甩给长期分支”的硬性规范
- 验证结果：
  - `dotnet restore GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -p:RestoreFallbackFolders="" -nologo`
    - 结果：通过；并行 restore 时出现一次共享 `obj` 文件已存在的竞争噪音，串行验证后未再复现
  - `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release --no-restore -p:RestoreFallbackFolders="" -clp:"Summary;WarningsOnly" -nologo`
    - 结果：`0 Warning(s)`，`0 Error(s)`
  - `dotnet build GFramework.Game.SourceGenerators/GFramework.Game.SourceGenerators.csproj -c Release --no-restore -p:RestoreFallbackFolders="" -clp:"Summary;WarningsOnly" -nologo`
    - 结果：`9 Warning(s)`，`0 Error(s)`；维持既有 `SchemaConfigGenerator.cs` `MA0051` 基线，未新增 warning
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~Run_Should_Report_Diagnostic_When_Schema_Keys_Collide_After_Identifier_Normalization|FullyQualifiedName~Emits_Object_Type_Reference_When_Handler_Response_Uses_Dynamic|FullyQualifiedName~Emits_Runtime_Type_Lookup_When_Handler_Contract_Contains_Unresolved_Error_Types" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`3 Passed`，`0 Failed`
    - 说明：测试项目构建仍打印既有 `MA0051` / `MA0004` / `MA0048` warning，不属于本轮 generator 模块写集，但已在 tracking 风险中记录
- 下一步建议：
  - 若继续收口 PR #269，可再次抓取最新 unresolved threads，确认 GitHub 上剩余 open thread 是否全部转为陈旧信号
  - 若回到 analyzer 主线，继续推进 `GFramework.Game.SourceGenerators/Config/SchemaConfigGenerator.cs` 剩余 `MA0051`

## 2026-04-22 — RP-024

### 阶段：PR #269 第四轮 review follow-up 收口（RP-024）

- 启动复核：
  - 延续 `$gframework-pr-review` 对 PR #269 latest-head unresolved threads 的复核，重点核对最新 5 个未解决线程是否仍与当前
    worktree 一致
  - 本地确认 `EasyEvents` 异常契约、`SchemaConfigGenerator` 取消传播与 `ContextAwareGenerator` 字段冲突线程已是陈旧信号，
    真正仍成立的仅剩 `CqrsHandlerRegistryGenerator` 的 Roslyn error type 直接引用，以及根 schema `type` 非字符串时的
    `GetString()` 防御
- 决策：
  - `CqrsHandlerRegistryGenerator` 保持现有“优先精确重建、必要时退回运行时查找”的设计，不引入新的程序集级 fallback 契约分支；
    只在 `CanReferenceFromGeneratedRegistry(...)` 中显式拒绝 `TypeKind.Error`，让未解析类型走已有运行时查找路径
  - `SchemaConfigGenerator` 继续沿用现有 `GF_ConfigSchema_002` 诊断，不新增诊断 ID；仅在根对象校验入口补上
    `JsonValueKind.String` 前置判断
- 实施调整：
  - 为 `CqrsHandlerRegistryGenerator.RuntimeTypeReferences` 增加 `TypeKind.Error` 防御，避免把未解析类型写成生成代码里的
    `typeof(...)`
  - 为 `SchemaConfigGeneratorTests` 补上根 `type` 为数字时返回 `GF_ConfigSchema_002` 的回归测试
  - 为 `CqrsHandlerRegistryGeneratorTests` 补上未解析 error type 会改走运行时 `GetType(...)` 精确查找的回归测试
- 验证结果：
  - `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release --no-restore -p:RestoreFallbackFolders="" -v minimal`
    - 结果：`0 Warning(s)`，`0 Error(s)`
  - `dotnet build GFramework.Game.SourceGenerators/GFramework.Game.SourceGenerators.csproj -c Release --no-restore -p:RestoreFallbackFolders="" -v minimal`
    - 结果：通过；仍保留既有 `9` 条 `SchemaConfigGenerator.cs` `MA0051`
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_Root_Type_Metadata_Is_Not_A_String|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_Runtime_Type_Lookup_When_Handler_Contract_Contains_Unresolved_Error_Types" -m:1 -p:RestoreFallbackFolders="" -v minimal`
    - 结果：`2 Passed`，`0 Failed`
    - 说明：测试命令需在无沙箱环境下运行，因为当前 test host 在沙箱内创建本地 socket 会收到 `Permission denied`
- 下一步建议：
  - 若继续压缩 PR #269 的 review backlog，可再次抓取最新 unresolved threads，确认 GitHub 上仅剩陈旧线程后再决定是否继续代码改动
  - 若回到 analyzer 主线，继续推进 `SchemaConfigGenerator.cs` 剩余 `MA0051`

## 2026-04-22 — RP-023

### 阶段：PR #269 第三轮 review follow-up 收口（RP-023）

- 启动复核：
  - 延续 `$gframework-pr-review` 对 PR #269 的 latest-head unresolved threads、outside-diff comment 与 nitpick comment
  - 本地核实后确认剩余仍成立的项集中在 `SchemaConfigGenerator` 根类型名校验、aggregate registration comparer XML 文档转义、
    `LoggingConfigurationTests` / `CollectionExtensionsTests` 断言补强，以及 `ai-plan` 命令文本可复制性
- 决策：
  - `SchemaConfigGenerator` 沿用现有 `InvalidGeneratedIdentifier` 诊断，不新增诊断 ID；将根类型名校验收敛到独立 helper，
    让顶层 schema 文件名与属性名共享同一类安全边界
  - aggregate registration comparer 文档直接复用现有 `EscapeXmlDocumentation(...)`，避免在 `///` 注释里再次写入原始泛型尖括号
  - `CqrsHandlerRegistryGenerator` 的重复反射查找分支采用小 helper 抽取，不改变 fallback 语义和快照输出
- 实施调整：
  - 为 `SchemaConfigGenerator` 新增 `TryBuildRootTypeIdentifiers(...)`，在进入 `ParseObjectSpec(...)` 前拦截非法根类型名
  - 调整 aggregate registration comparer 属性的 XML 文档，使用 `<c>...</c>` 包裹并转义泛型类型文本
  - 为 `SchemaConfigGeneratorTests` 增加非法 schema 文件名诊断回归，并补强 generated catalog 中 comparer 文档断言
  - 为 `LoggingConfigurationTests` 增加正向键存在和值断言，为 `CollectionExtensionsTests` 补齐返回类型泛型参数绑定断言
  - 为 `CqrsHandlerRegistryGenerator.RuntimeTypeReferences` 抽取共享反射查找 helper，并修正 active tracking 中的转义引号
- 验证结果：
  - `dotnet build GFramework.Game.SourceGenerators/GFramework.Game.SourceGenerators.csproj -c Release --no-restore -p:RestoreFallbackFolders="" -v minimal`
    - 结果：通过；仍保留既有 `9` 条 `SchemaConfigGenerator.cs` `MA0051`
  - `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release --no-restore -p:RestoreFallbackFolders="" -v minimal`
    - 结果：通过；并行构建时出现一次 `MSB3026` 文件占用重试，自动恢复后完成
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~SchemaConfigGeneratorTests|FullyQualifiedName~SchemaConfigGeneratorSnapshotTests|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests" -m:1 -p:RestoreFallbackFolders="" -v minimal`
    - 结果：`63 Passed`，`0 Failed`
    - 说明：测试项目构建仍打印既有 source-generator-tests analyzer warning，不属于本轮写集
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~CollectionExtensionsTests|FullyQualifiedName~LoggingConfigurationTests" -m:1 -p:RestoreFallbackFolders="" -v minimal`
    - 结果：`27 Passed`，`0 Failed`
- 下一步建议：
  - 若本轮验证通过，继续回到 `SchemaConfigGenerator.cs` 剩余 `MA0051`
  - 若 PR #269 仍有未关闭 review thread，再按“先本地复核、再最小修复”的节奏收口

## 2026-04-22 — RP-022

### 阶段：PR #269 第二轮 review follow-up 收口（RP-022）

- 启动复核：
  - 延续 `$gframework-pr-review` 的 PR #269 结果，继续核对 latest-head unresolved threads 与 nitpick comment
  - 结合本地实现确认仍成立的项不止第一轮记录的 4 个，还包括公共 API 兼容回退、`SchemaConfigGenerator` 取消传播、
    `ContextAwareGenerator` 真正的字段名去冲突与锁内读取修正、`Cqrs` 运行时类型 null 防御
- 决策：
  - 对公共 API 兼容项优先保持既有契约，不为了压 analyzer 而继续收窄返回类型、属性类型或异常类型
  - `ContextAwareGenerator` 采用保守并发修复：移除未加锁 fast-path，统一在锁内读取上下文缓存，并让生成字段名按已有成员去冲突
  - `SchemaConfigGenerator` 在取消已请求时直接重新抛出 `OperationCanceledException`，避免把取消误报告成普通诊断
- 实施调整：
  - 将 `EasyEvents.AddEvent<T>()` 的重复注册异常恢复为 `ArgumentException`，并在测试中恢复既有异常契约断言
  - 将 `CollectionExtensions.ToDictionarySafe(...)` 返回类型恢复为 `Dictionary<TKey, TValue>`，并新增反射测试锁定公开 API 形状
  - 将 `LoggingConfiguration` / `FilterConfiguration` 的公开集合属性恢复为具体 `List<>` / `Dictionary<,>` 类型，
    并新增反射测试与默认 comparer 语义断言
  - 为 `CqrsHandlerRegistryGenerator` 的命名类型引用构造补上 `ContainingAssembly is null` 防御，移除发射 helper 冗余布尔参数
  - 为 `SchemaConfigGenerator` 补上“仅在 cancellationToken 已取消时重抛”的 catch 分支，并为测试驱动添加多 `AdditionalText` 重载
  - 为 `ContextAwareGenerator` 增加生成成员名分配逻辑，新增 `_gFrameworkContextAware*` 与旧 `_context*` 双冲突快照场景，
    同时移除 getter 中未加锁 fast-path
- 验证结果：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -v minimal`
    - 结果：通过
  - `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -v minimal`
    - 结果：通过
  - `dotnet build GFramework.Game.SourceGenerators/GFramework.Game.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -v minimal`
    - 结果：通过；仍有既有 `9` 条 `MA0051`
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~SchemaConfigGeneratorTests|FullyQualifiedName~ContextAwareGeneratorSnapshotTests|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests" -m:1 -p:RestoreFallbackFolders="" -v minimal`
    - 结果：`63 Passed`，`0 Failed`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~EasyEventsTests|FullyQualifiedName~CollectionExtensionsTests|FullyQualifiedName~LoggingConfigurationTests|FullyQualifiedName~ConfigurableLoggerFactoryTests" -m:1 -p:RestoreFallbackFolders="" -v minimal`
    - 结果：`38 Passed`，`0 Failed`
- 下一步建议：
  - 回到 `GFramework.Game.SourceGenerators/Config/SchemaConfigGenerator.cs` 的剩余 `MA0051`
  - 若后续 review 再提 analyzer 兼容建议，先做公共契约回归检查，再决定是否接受该建议

## 2026-04-22 — RP-021

### 阶段：PR #269 review follow-up 收口（RP-021）

- 启动复核：
  - 使用 `$gframework-pr-review` 读取当前分支 PR #269 的 CodeRabbit outside-diff 与 nitpick 汇总
  - 本地复核后确认仍成立的 4 个项分别是：`CqrsHandlerRegistryGenerator.cs` 超过仓库文件大小上限、
    `ContextAwareGenerator` 生成字段名可能与用户 partial 类型冲突、`SetContextProvider` 缺少运行时 null 防御、
    `Option<T>` 缺少 `<remarks>` 契约说明
- 决策：
  - `CqrsHandlerRegistryGenerator` 继续采用既有 partial helper 风格，按“主流程 / 运行时类型引用 / 源码发射 / 模型”四个文件拆分，
    保持生成顺序、日志文本、fallback 契约和快照输出不变
  - `ContextAwareGenerator` 只收口仍成立的 review 项，不引入未被本地证实的 `Volatile.Read/Write` 变更
  - 为字段命名冲突新增生成器快照场景，避免后续回退到 `_context` / `_contextProvider` / `_contextSync`
- 实施调整：
  - 将 `GFramework.Cqrs.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 拆为 4 个 partial 文件，分别承载主生成管线、
    runtime type reference 构造、source emission helper 与嵌套 specs/models
  - 将 `ContextAwareGenerator` 生成字段统一改为 `_gFrameworkContextAware*` 前缀，同步更新 XML 文档、注释和显式接口实现
  - 为 `SetContextProvider(...)` 增加 `ArgumentNullException.ThrowIfNull(provider)` 与 XML `<exception>` 说明
  - 为 `Option<T>` 补充 `<remarks>`，明确 `Some/None`、`null` 约束、不可变语义与推荐使用方式
  - 新增 `CollisionProneRule.ContextAware.g.cs` 快照，覆盖用户字段名与生成字段名冲突场景
- 验证结果：
  - `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders= -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`0 Warning(s)`，`0 Error(s)`；拆分后 `CqrsHandlerRegistryGenerator` 最大单文件为 `851` 行
  - `dotnet build GFramework.Core.SourceGenerators/GFramework.Core.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders= -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`0 Warning(s)`，`0 Error(s)`
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~CqrsHandlerRegistryGeneratorTests -m:1 -p:RestoreFallbackFolders= -nologo`
    - 结果：`14 Passed`，`0 Failed`
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~ContextAwareGeneratorSnapshotTests -m:1 -p:RestoreFallbackFolders= -nologo`
    - 结果：`2 Passed`，`0 Failed`
    - 说明：最初并行跑两个 `dotnet test` 命令时触发共享输出文件锁冲突；串行重跑后确认是测试宿主环境噪音而非代码回归
- 下一步建议：
  - 若本轮验证通过，可继续回到 `SchemaConfigGenerator` 剩余 `MA0051`
  - 若 review 再次聚焦 `ContextAwareGenerator` 并发可见性问题，需要先补最小复现测试，再决定是否引入 `Volatile` 语义

## 2026-04-22 — RP-020

### 阶段：`SchemaConfigGenerator` 第一批 `MA0051` 结构拆分（RP-020）

- 启动复核：
  - 当前 worktree 仍映射到 `analyzer-warning-reduction` active topic
  - `GFramework.Game.SourceGenerators` warnings-only build 复现 `19` 条 warning，全部为
    `GFramework.Game.SourceGenerators/Config/SchemaConfigGenerator.cs` 的 `MA0051`
- 决策：
  - 本轮继续低风险结构拆分，不改变 schema 支持范围、诊断 ID、生成类型形状或输出顺序
  - 未使用 subagent；critical path 是本地复现 warning、拆分语义阶段并用 focused schema generator tests 验证行为
- 实施调整：
  - 将 schema 入口解析拆为文本读取、root 验证、id key 验证和 `SchemaFileSpec` 构造阶段
  - 将属性解析拆为共享上下文提取、类型分派、标量/对象/数组属性构造 helper
  - 将统一 schema 遍历拆为对象属性、dependentSchemas、allOf、条件分支、not、array items / contains 等遍历阶段
  - 将约束文档生成拆为 const、numeric、string、array、object 约束片段
  - 将 catalog/registration/YAML/lookup/object type 等生成代码发射路径中的小型高收益 helper 拆出
- 验证结果：
  - `dotnet build GFramework.Game.SourceGenerators/GFramework.Game.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders= -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`9 Warning(s)`，`0 Error(s)`；当前项目剩余 warning 均为 `SchemaConfigGenerator.cs` 的 `MA0051`
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~SchemaConfigGenerator -m:1 -p:RestoreFallbackFolders= -nologo`
    - 结果：`50 Passed`，`0 Failed`
    - 说明：测试项目构建仍显示既有 source generator test analyzer warning；不属于本轮写集
- 下一步建议：
  - 继续该主题时，优先拆分 `GenerateBindingsClass`、`AppendGeneratedConfigCatalogType` 或对象/条件 schema target 验证方法
  - 若转回 `MA0158`，仍需先设计多 target 条件编译方案，再考虑替换共享源码中的 `object` lock

## 2026-04-22 — RP-019

### 阶段：`SchemaConfigGenerator` 当前 `MA0006` 收口（RP-019）

- 启动复核：
  - 当前 worktree 仍映射到 `analyzer-warning-reduction` active topic
  - Windows Git interop 在当前 shell 中返回 WSL socket 错误；本轮使用显式 `--git-dir` / `--work-tree` 读取状态
  - `GFramework.Game.SourceGenerators` 首次 build 受 stale Windows fallback package folder 影响，刷新 restore 资产后复现
    `46` 条 warning，其中 `MA0006=27`，其余为 `SchemaConfigGenerator.cs` 的 `MA0051`
- 决策：
  - 本轮先收口低风险 `MA0006`，不在同一 slice 中拆分 `SchemaConfigGenerator.cs` 的长方法
  - 未使用 subagent；critical path 是本地复现 warning、替换 schema 字符串比较并用 focused schema generator tests 验证输出行为
- 实施调整：
  - 为 schema 类型关键字新增 `IsSchemaType` / `IsNumericSchemaType` helper，统一使用 `StringComparison.Ordinal`
  - 将 id key 类型验证、约束文档生成、required property 文档和路径拼接中的直接字符串比较改为显式 ordinal 比较
  - 修正 `JsonElement.GetString()` 后的 nullable flow，避免新增 `CS8604`
- 验证结果：
  - `dotnet restore GFramework.Game.SourceGenerators/GFramework.Game.SourceGenerators.csproj -p:RestoreFallbackFolders= -nologo`
    - 结果：通过
  - `dotnet build GFramework.Game.SourceGenerators/GFramework.Game.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders= -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`19 Warning(s)`，`0 Error(s)`；当前项目输出已无 `MA0006`，剩余均为 `MA0051`
  - `dotnet restore GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -p:RestoreFallbackFolders= -nologo`
    - 结果：通过
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~SchemaConfigGenerator -m:1 -p:RestoreFallbackFolders= -nologo`
    - 结果：`50 Passed`，`0 Failed`
    - 说明：测试项目构建仍显示既有 analyzer warning；不属于本轮写集
- 下一步建议：
  - 继续该主题时，优先拆分 `GFramework.Game.SourceGenerators/Config/SchemaConfigGenerator.cs` 的 `MA0051`
  - 若回到 `MA0158`，先设计多 target 条件编译方案，再考虑替换共享源码中的 `object` lock

## 2026-04-22 — RP-018

### 阶段：`CqrsHandlerRegistryGenerator` 剩余 `MA0051` 收口（RP-018）

- 启动复核：
  - 当前 worktree 仍映射到 `analyzer-warning-reduction` active topic
  - `MA0158` 锁迁移仍然跨 `GFramework.Core` / `GFramework.Cqrs` 多 target 共享源码，继续视为需要单独设计的兼容性问题
  - `GFramework.Cqrs.SourceGenerators` warnings-only build 复现 `CqrsHandlerRegistryGenerator.cs` 的 `6` 个 `MA0051`
- 决策：
  - 本轮暂缓 `MA0158`，转入单文件、可由生成器测试覆盖的 `GFramework.Cqrs.SourceGenerators` 结构拆分
  - 未使用 subagent；critical path 是本地复现 warning、拆分源码发射流程并用 focused generator tests 验证输出未变
- 实施调整：
  - 将 handler candidate 分析拆为接口收集、候选构造和单接口注册分类阶段
  - 将运行时类型引用构造拆为已构造泛型、命名类型反射查找等独立 helper
  - 将注册器源码生成拆为文件头、程序集特性、注册器类型、`Register` 方法和服务注册日志发射 helper
  - 将有序注册与精确反射注册输出拆为独立阶段，保留原有排序和生成文本形状
- 验证结果：
  - `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders= -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`0 Warning(s)`，`0 Error(s)`
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~CqrsHandlerRegistryGeneratorTests -m:1 -p:RestoreFallbackFolders= -nologo`
    - 结果：`14 Passed`，`0 Failed`
    - 说明：测试项目构建仍显示 `GFramework.Game.SourceGenerators` 与测试项目中的既有 analyzer warning；不属于本轮写集
- 下一步建议：
  - 继续该主题时，优先处理 `GFramework.Game.SourceGenerators/Config/SchemaConfigGenerator.cs` 的 `MA0006` 低风险批次
  - 若回到 `MA0158`，先设计多 target 条件编译方案，再考虑替换共享源码中的 `object` lock

## 2026-04-22 — RP-017

### 阶段：`ContextAwareGenerator` 剩余 `MA0051` 收口（RP-017）

- 启动复核：
  - 当前 worktree 仍映射到 `analyzer-warning-reduction` active topic
  - `GFramework.Core` `net10.0` warnings-only build 在刷新 restore fallback 资产后复现 `16` 个 `MA0158`
  - `GFramework.Core.SourceGenerators` warnings-only build 复现 `ContextAwareGenerator.GenerateContextProperty` 的单个
    `MA0051`
- 决策：
  - `MA0158` 涉及 `GFramework.Core` 与 `GFramework.Cqrs` 的 object lock 字段，且项目仍多 target 到 `net8.0` / `net9.0`
    / `net10.0`，因此本轮不直接批量替换为 `System.Threading.Lock`
  - 先处理单文件、单 warning、生成输出可由 snapshot 验证的 `ContextAwareGenerator` 结构拆分
  - 未使用 subagent；本轮 critical path 是本地复现 warning、拆分方法并验证生成输出，拆分后写集只包含单个 generator 文件和
    active `ai-plan` 文档
- 实施调整：
  - 将 `GenerateContextProperty` 拆为 `GenerateContextBackingFields`、`GenerateContextGetter` 与
    `GenerateContextProviderConfiguration`
  - 保留原有 `StringBuilder` 追加顺序与生成代码文本，避免 snapshot 变更
  - 为新增 helper 补充 XML 注释，说明字段、getter 与 provider 配置 API 的生成职责
- 验证结果：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net10.0 -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`16 Warning(s)`，`0 Error(s)`；记录当前 `MA0158` 基线，不作为本轮修改范围
  - `dotnet build GFramework.Core.SourceGenerators/GFramework.Core.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`0 Warning(s)`，`0 Error(s)`；`ContextAwareGenerator.cs` 的 `MA0051` 已清零
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~ContextAwareGeneratorSnapshotTests" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`1 Passed`，`0 Failed`
    - 说明：该 test project 构建仍显示相邻 generator/test 项目的既有 analyzer warning；本轮关注的
      `GFramework.Core.SourceGenerators` 独立 build 已清零
- 下一步建议：
  - 继续该主题时，优先设计 `MA0158` 的多 target 兼容迁移方案；如果风险过高，再单独切入
    `GFramework.Cqrs.SourceGenerators` 或 `GFramework.Game.SourceGenerators` 的结构性 warning

## 2026-04-22 — RP-016

### 阶段：`GFramework.Core` 剩余低风险 warning 批次清零（RP-016）

- 依据 `RP-015` 的下一步建议，本轮恢复到 `MA0016` / `MA0002` 低风险批次，并顺手吸收仍集中在
  `GFramework.Core` 的 `MA0015` 与 `MA0077`
- 基线复核：
  - 首次使用 Linux `dotnet` 时仍被当前 worktree 的 Windows fallback package folder restore 资产阻断
  - 切换到 host Windows `dotnet` 后，`GFramework.Core` `net8.0` warnings-only build 复现 `9` 条 warning：
    `MA0016=5`、`MA0002=2`、`MA0015=1`、`MA0077=1`
- 实施调整：
  - 将 `LoggingConfiguration.Appenders` / `LoggerLevels` 与 `FilterConfiguration.Namespaces` / `Filters`
    的公开类型改为集合抽象接口，同时保留 `List<T>` / `Dictionary<TKey,TValue>` 默认实例，兼顾 analyzer 与现有配置消费路径
  - 将 `CollectionExtensions.ToDictionarySafe(...)` 返回类型改为 `IDictionary<TKey,TValue>`，内部仍使用 `Dictionary<TKey,TValue>`
    保留“重复键以后值覆盖前值”的实现语义
  - 为 `CoroutineScheduler` 的 `_tagged` 与 `_grouped` 字典显式指定 `StringComparer.Ordinal`，将原有默认区分大小写语义写入代码
  - 将 `EasyEvents.AddEvent<T>()` 重复注册失败从 `ArgumentException` 改为 `InvalidOperationException`；该路径表示状态冲突，
    不是某个方法参数无效，因此不能为 `MA0015` 人造参数名
  - 为 `Option<T>` 声明 `IEquatable<Option<T>>`，与已有强类型 `Equals(Option<T>)` 实现对齐
- 验证结果：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`0 Warning(s)`，`0 Error(s)`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~LoggingConfigurationTests|FullyQualifiedName~ConfigurableLoggerFactoryTests|FullyQualifiedName~CollectionExtensionsTests|FullyQualifiedName~EasyEventsTests|FullyQualifiedName~OptionTests|FullyQualifiedName~CoroutineGroupTests|FullyQualifiedName~CoroutineSchedulerTests" -m:1 -nologo`
    - 结果：`112 Passed`，`0 Failed`
    - 说明：测试构建仍显示既有 `net10.0` `MA0158` 与 source generator `MA0051` warning；这些不属于本轮
      `GFramework.Core` `net8.0` 剩余 warning 批次
- 当前结论：
  - `GFramework.Core` `net8.0` 当前 analyzer warning baseline 已清零
  - analyzer topic 仍可继续，但下一轮应转入 `net10.0` 专属 `MA0158` 兼容性评估，或单独处理 source generator 剩余
    `MA0051`
- 下一步建议：
  - 优先评估 `MA0158` 在多 target 源码中的安全推进方式；若风险过高，再处理
    `GFramework.Core.SourceGenerators/Rule/ContextAwareGenerator.cs` 的结构拆分

## 2026-04-21 — RP-015

### 阶段：PR #267 failed-test follow-up 收口（RP-015）

- 触发背景：
  - 用户指出“测试好像挂了”，按 `$gframework-pr-review` 重新抓取当前分支 PR #267 的 review / checks / CTRF 评论
  - PR 评论里同时存在一次 `2143 passed / 0 failed` 与一次 `1 failed` 的 CTRF 报告；失败用例为
    `AsyncLogAppenderTests.ILogAppender_Flush_Should_Raise_OnFlushCompleted_Only_Once`
- 复核过程：
  - 先跑定向单测时该用例可以单独通过，因此继续核对 PR head commit 与本地整包测试，避免把旧评论误判成当前状态
  - 在 `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-restore --disable-build-servers`
    下成功复现相同失败，确认问题仍存在于当前代码，而不是单纯的 PR 评论残留
  - 同时发现当前沙箱内如果用 shell 循环反复启动 `dotnet test`，会触发 `MSBuild` named pipe `Permission denied`
    的环境噪音；后续验证改为单次命令并显式加 `--disable-build-servers`
- 根因结论：
  - `AsyncLogAppender.Flush()` 只依赖后台消费循环在处理完某个条目后检查 `_flushRequested`
  - 当调用方执行 `Flush()` 前，后台线程已经把最后一个条目消费完并离开检查点时，`Flush()` 会一直等到默认超时，
    最终通过 `OnFlushCompleted` 发出一次 `Success=false` 的错误完成通知
- 实施修复：
  - 为 `AsyncLogAppender` 增加“当前是否仍有条目在途处理”的状态跟踪
  - 抽出 `TrySignalFlushCompletion()`，让 `Flush()` 在请求发出后先做一次即时完成判定；后台循环在每次处理结束后也复用
    这条判定路径
  - 在 `AsyncLogAppenderTests` 中新增 `Flush_WhenEntriesAlreadyProcessed_Should_Still_ReportSuccess`，稳定覆盖
    “调用 Flush 前队列已被后台线程清空”的场景
- 验证结果：
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-restore --disable-build-servers --filter "FullyQualifiedName~AsyncLogAppenderTests"`
    - 结果：`15 Passed`，`0 Failed`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-restore --disable-build-servers`
    - 结果：`1607 Passed`，`0 Failed`
- 当前结论：
  - PR #267 的 failed-test 信号不是纯粹的历史评论噪音，而是当前实现里仍存在的时序竞态
  - 修复后该竞态已被稳定回归测试覆盖，当前 `GFramework.Core.Tests` 整包通过
- 下一步建议：
  - 若继续 analyzer warning reduction 主题，恢复到 `MA0016` / `MA0002` 低风险批次

## 2026-04-21 — RP-014

### 阶段：PR #267 review follow-up 收口（RP-014）

- 使用 `gframework-pr-review` 抓取当前分支 PR #267 的 latest head review threads、outside-diff comment、nitpick comment、
  MegaLinter 摘要与测试报告，并确认本轮除了 6 条 open thread 之外，还存在 1 条 outside-diff 与 1 条 nitpick 需要一并复核
- 本地复核后确认仍成立的项：
  - `AsyncLogAppender` 的显式接口实现 `ILogAppender.Flush()` 会在调用 `Flush()` 后再次手动触发 `OnFlushCompleted`，
    导致接口路径重复通知
  - `Architecture.PhaseChanged`、`CoroutineExceptionEventArgs` 与 `ArchitecturePhaseCoordinator.EnterPhase` 的 XML/注释契约仍未完全同步
  - `CoroutineSchedulerTests` 的异常事件测试缺少测试级超时
  - `docs/zh-CN/core/architecture.md` 与 `docs/zh-CN/core/lifecycle.md` 仍缺少明确的 `PhaseChanged` 迁移说明
  - `ai-plan` active tracking 中 `RP-013` 的 `9 Warning(s)` 需要明确是相对 `RP-009` / `RP-011` 的 warnings-only 基线收敛
- 实施最小修复：
  - 删除 `ILogAppender.Flush()` 中重复的完成事件触发，只保留 `Flush(TimeSpan?)` 内的单一通知源
  - 为接口调用路径补充单次完成通知回归测试，并为协程异常事件测试增加 `WaitAsync(TimeSpan.FromSeconds(3))`
  - 补齐 `Architecture.PhaseChanged`、`CoroutineExceptionEventArgs` 与 `ArchitecturePhaseCoordinator.EnterPhase` 的契约文档
  - 在 `docs/zh-CN/core/architecture.md` 与 `docs/zh-CN/core/lifecycle.md` 中加入 `phase => ...` 迁移到 `(_, args) => ...` 的说明
  - 更新 `ai-plan/public/analyzer-warning-reduction/todos/analyzer-warning-reduction-tracking.md` 的恢复点、基线描述与验证结果
- 验证结果：
  - `dotnet restore GFramework.Core.Tests/GFramework.Core.Tests.csproj -p:RestoreFallbackFolders="" -nologo`
    - 结果：通过；host Windows `dotnet` 首次验证前补齐了缺失的 `Meziantou.Analyzer 3.0.48`
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`9 Warning(s)`，`0 Error(s)`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~CoroutineSchedulerTests.Scheduler_Should_Raise_OnCoroutineException_With_EventArgs|FullyQualifiedName~AsyncLogAppenderTests.Flush_Should_Raise_OnFlushCompleted_With_Sender_And_Result|FullyQualifiedName~AsyncLogAppenderTests.ILogAppender_Flush_Should_Raise_OnFlushCompleted_Only_Once|FullyQualifiedName~ArchitectureLifecycleBehaviorTests.InitializeAsync_Should_Raise_PhaseChanged_With_Sender_And_EventArgs" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`4 Passed`，`0 Failed`
- 当前结论：
  - PR #267 里当前仍成立的 CodeRabbit 高信号项已在本地收口
  - 修复内容没有改变 `EventHandler<TEventArgs>` 迁移方向，只是补齐行为、文档与恢复信息
- 下一步建议：
  - 恢复到 `MA0016` / `MA0002` 主批次，默认先看 `LoggingConfiguration`、`FilterConfiguration` 与 `CollectionExtensions`

## 2026-04-21 — RP-013

### 阶段：`MA0046` 事件签名批次收口（RP-013）

- 依据 `RP-012` 的下一步建议，本轮恢复到 `GFramework.Core` 的 `MA0046` 主批次，而不是继续停留在 PR review workflow 优化
- 本地 warnings-only 基线确认当前 `GFramework.Core` `net8.0` 仍有 `6` 个 `MA0046`：
  - `Architecture.cs`
  - `ArchitectureLifecycle.cs`
  - `ArchitecturePhaseCoordinator.cs`
  - `AsyncLogAppender.cs`
  - `CoroutineScheduler.cs` 两处事件
- 方案选择：
  - 不再保留 `Action<...>` 事件签名，统一改为标准 `EventHandler<TEventArgs>`
  - 为 `Architecture`、`AsyncLogAppender` 新增放在 `GFramework.Core.Abstractions` 的事件参数类型
  - 为 `CoroutineScheduler` 新增放在 `GFramework.Core` 的事件参数类型，因为 `CoroutineHandle` 定义在 runtime 层，不适合反向放入 Abstractions
  - `Architecture` 相关事件采用 `Coordinator -> Lifecycle -> Architecture` relay，而不是直接透传底层事件，确保公开事件的 sender 始终是实际发布者，并避免引入新的 `MA0091`
- 同步适配：
  - 更新 `GFramework.Godot/Coroutine/Timing.cs` 的 `OnCoroutineFinished` 订阅签名
  - 更新 `ArchitectureLifecycleBehaviorTests`、`CoroutineSchedulerTests`、`AsyncLogAppenderTests` 以覆盖 sender / event args 契约
  - 更新 `docs/zh-CN/core/architecture.md` 与 `docs/zh-CN/core/lifecycle.md` 的 `PhaseChanged` 示例
- 验证结果：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`9 Warning(s)`，`0 Error(s)`；当前 `GFramework.Core` `net8.0` 输出中已无 `MA0046`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~ArchitectureLifecycleBehaviorTests|FullyQualifiedName~CoroutineSchedulerTests|FullyQualifiedName~AsyncLogAppenderTests" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`50 Passed`，`0 Failed`
  - `dotnet build GFramework.Godot/GFramework.Godot.csproj -c Release --no-restore -p:RestoreFallbackFolders="" -nologo`
    - 结果：失败；当前 worktree 的 `project.assets.json` 仍引用 Windows fallback package folder，尚未完成 Godot 独立编译验证
- 当前结论：
  - `MA0046` 已从 active 批次中移除
  - 剩余 `GFramework.Core` `net8.0` warning 分布更新为：`MA0016=5`、`MA0002=2`、`MA0015=1`、`MA0077=1`
  - 若继续本主题，下一步默认转入 `MA0016` 批次；若继续触达 Godot，再先修复该项目 restore 资产

## 2026-04-21 — RP-012

### 阶段：PR review workflow 输出收窄增强（RP-012）

- 背景：上一轮虽然脚本已经能解析 `outside_diff_comments`，但直接把超长 JSON 打到终端时仍可能因为输出截断而漏看高价值 review 信号
- 本轮对 `gframework-pr-review` 做了工作流级增强，而不是继续依赖 shell 重定向技巧：
  - 为 `fetch_current_pr_review.py` 增加 `--json-output <path>`，允许把完整 JSON 稳定写入文件
  - 增加 `--section`，可只输出 `outside-diff`、`open-threads`、`megalinter` 等高信号文本摘要
  - 增加 `--path`，允许把文本输出收窄到特定文件或路径片段
  - 增加 `--max-description-length`，避免超长 comment/body 在 text 模式下刷屏
  - 当 text 模式搭配 `--json-output` 时，stdout 保持精简，并显式提示完整 JSON 文件路径
- 同步更新 `SKILL.md`：
  - 将“先落盘，再用 `jq` 或 `--section` / `--path` 缩小范围”写成推荐机器工作流
  - 补充按 section 和按路径聚焦的示例命令
- 预期收益：
  - 不再要求操作者肉眼阅读整份长 JSON
  - outside-diff、nitpick 和 open thread 都能成为一等可过滤输出
  - 即使终端输出有 token/长度上限，完整结果仍可通过文件稳定回查
- 定向验证命令：
  - `python3 -m py_compile .codex/skills/gframework-pr-review/scripts/fetch_current_pr_review.py`
    - 结果：通过；使用 `PYTHONPYCACHEPREFIX=/tmp/codex-pycache` 规避 `__pycache__` 写入限制
  - `python3 .codex/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --help`
    - 结果：通过；新增 CLI 选项均已出现在帮助输出中
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`0 Warning(s)`，`0 Error(s)`
- 下一步建议：
  - 之后执行 `$gframework-pr-review` 时，默认优先使用 `--json-output`
  - 在 review 跟进阶段，先看 `outside-diff`、`open-threads`、`megalinter` 三个 section，再决定是否需要打开完整 JSON

## 2026-04-21 — RP-011

### 阶段：PR #265 outside-diff follow-up 补收口（RP-011）

- 用户补充指出 CodeRabbit 在 `Some comments are outside the diff` 中还有 `GFramework.Core/Events/Event.cs` 的 minor finding：
  默认 no-op 委托会被 `GetInvocationList()` 计入，导致 `GetListenerCount()` 在无监听器和单监听器场景分别返回 `1` 和 `2`
- 本地复核确认该问题仍成立：
  - `Event<T>` 当前字段初始化为 `_ => { }`
  - `Event<T, TK>` 当前字段初始化为 `(_, _) => { }`
  - 两个 `Trigger(...)` 实现本身已是 null-safe，因此无需依赖占位委托规避空引用
- 实施最小修复：
  - 移除两个事件字段的 no-op 初始委托，改为以 `null` 表示“无监听器”
  - 保持 `Register` / `UnRegister` / `Trigger` 的公开 API 和调用方式不变
  - 在 `EventTests` 中新增单参数与双参数 `GetListenerCount()` 回归测试，覆盖初始值、注册后和注销后的计数语义
- 过程说明：
  - 这条不是 skill 设计遗漏；`gframework-pr-review` 的目标本来就包含 latest review body 和 outside-diff 信号
  - 上一轮是我在处理时漏看了这条 outside-diff item，且终端里展示的超长 JSON 输出被截断，未单独把 `Event.cs` 项再抽出来复核
- 定向验证命令：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`15 Warning(s)`，`0 Error(s)`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~EventTests.EventT_GetListenerCount_Should_Exclude_Placeholder_Handler|FullyQualifiedName~EventTests.EventTTK_GetListenerCount_Should_Exclude_Placeholder_Handler" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`2 Passed`，`0 Failed`
- 下一步建议：
  - 若继续 PR #265 follow-up，只接受当前本地仍成立的剩余 outside-diff 或 unresolved review 项
  - 若没有新的有效 review 点，再恢复到 `MA0046` 主批次

## 2026-04-21 — RP-010

### 阶段：PR #265 follow-up 收口（RP-010）

- 使用 `gframework-pr-review` 抓取当前分支 PR #265 的 latest head review threads、CodeRabbit review body、MegaLinter 摘要与 CTRF
  测试结果；确认最新 unresolved thread 只剩 `CoroutineScheduler` 零容量扩容边界
- 本地复核后确认两处仍成立的风险：
  - `CoroutineScheduler.Expand()` 在 `_slots.Length == 0` 时会把容量从 `0` 扩到 `0`，首次 `Run` 写槽位会越界
  - `Store.EnterDispatchScope()` 在 `_isDispatching = true` 之后、快照构建完成之前若抛异常，会留下永久的嵌套分发误判
- 实施最小修复：
  - 将 `Expand()` 调整为 `Math.Max(1, _slots.Length * 2)`，保持已有倍增策略，只补上零容量边界
  - 为 `EnterDispatchScope()` 增加快照阶段的异常回滚，确保 `_isDispatching` 与实际 dispatch 生命周期保持一致
  - 新增回归测试覆盖零容量启动路径，以及 dispatch 快照阶段抛错后的可恢复性
- 当前 PR 信号复核结论：
  - CTRF：最新评论显示 `2135 passed / 0 failed`
  - MegaLinter：唯一告警仍是 CI 中 `dotnet-format` restore 失败，未发现新的本地代码格式问题
  - 旧 review body 中提到的 `Store` 异常安全问题虽未表现为最新 open thread，但在本地代码中仍可成立，因此一并收口
- 定向验证命令：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`15 Warning(s)`，`0 Error(s)`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~CoroutineSchedulerTests.Run_Should_Grow_From_Zero_Initial_Capacity|FullyQualifiedName~StoreTests.Dispatch_Should_Reset_Dispatching_Flag_When_Snapshot_Creation_Throws" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`2 Passed`，`0 Failed`
- 下一步建议：
  - 若继续本主题，恢复到 `MA0046` 主批次，不再停留在当前 PR follow-up
  - 若 PR review 还出现新线程，继续遵守“只修复当前本地仍成立的问题”的策略

## 2026-04-21 — RP-009

### 阶段：`MA0048` 批次收口（RP-009）

- 依据 `RP-008` 的批处理策略，本轮继续从 `GFramework.Core` 的 `MA0048` 启动，但不采用重命名公共类型的高风险做法；
  改为把同名不同泛型 arity 的家族收拢到与类型名一致的单文件中
- 具体调整：
  - 将 `AbstractCommand<TInput>` 与 `AbstractCommand<TInput, TResult>` 合并进 `AbstractCommand.cs`
  - 将 `AbstractAsyncCommand<TInput>` 与 `AbstractAsyncCommand<TInput, TResult>` 合并进 `AbstractAsyncCommand.cs`
  - 将 `AbstractQuery<TInput, TResult>` 合并进 `AbstractQuery.cs`
  - 将 `AbstractAsyncQuery<TInput, TResult>` 合并进 `AbstractAsyncQuery.cs`
  - 将泛型 `Event<T>` / `Event<T, TK>` 从 `EasyEventGeneric.cs` 迁移到 `Event.cs`
- 首次构建暴露出合并后的 `ICommand<TResult>` / `IQuery<TResult>` 命名空间歧义；随后改用
  `GFramework.Core.Abstractions.*` 的限定名完成最小修正，没有引入行为改动
- 定向验证通过：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`15 Warning(s)`，`0 Error(s)`；`MA0048` 已从当前 `GFramework.Core` `net8.0` warnings-only 基线中清空
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~CommandExecutorTests|FullyQualifiedName~AbstractAsyncCommandTests|FullyQualifiedName~QueryExecutorTests|FullyQualifiedName~AbstractAsyncQueryTests|FullyQualifiedName~EventTests" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`83 Passed`，`0 Failed`
- 当前建议的下一批次顺序更新为：
  - 第一优先级：`MA0046`
  - 第二优先级：`MA0016`
  - 顺手吸收：`MA0015`、`MA0077`
  - 单独评估：`MA0002`

## 2026-04-21 — RP-008

### 阶段：批处理策略切换（RP-008）

- 根据当前 `GFramework.Core` warnings-only build 的剩余分布，后续不再默认沿用“单文件、单 warning family”的切片节奏，
  改为按 warning 类型和数量优先级批量推进
- 当前数量基线：
  - `MA0048 = 8`
  - `MA0046 = 6`
  - `MA0016 = 5`
  - `MA0002 = 2`
  - `MA0015 = 1`
  - `MA0077 = 1`
- 新的批处理规则：
  - 先按类型选择主批次，而不是按单文件选切入点
  - 若主批次数量不够，则允许顺手并入其他低冲突类型；`MA0015` 与 `MA0077` 只是当前明显的低数量尾项示例，不是限定范围
  - 单次 `boot` 的工作树改动规模控制在约 `100` 个文件以内，避免 recovery context 和 review 面同时膨胀
  - 当 warning 类型或目录边界清晰且写集不冲突时，允许使用不同模型的 subagent 并行处理，但必须先定义独占 ownership
- 当前建议的下一批次顺序：
  - 第一优先级：`MA0048`
  - 第二优先级：`MA0046`
  - 顺手吸收：其他低冲突类型，当前可见示例包括 `MA0015`、`MA0077`
  - 单独评估：`MA0016`、`MA0002`
- 本轮仅更新 recovery strategy，不改生产代码；验证继续沿用当前基线构建：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -p:RestorePackagesPath=<linux-nuget-cache> -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`23 Warning(s)`，`0 Error(s)`

## 2026-04-21 — RP-007

### 阶段：CoroutineScheduler `MA0051` 收口（RP-007）

- 依据 active tracking 中“继续只选一个 `GFramework.Core` 结构性切入点”的约束，本轮选择
  `GFramework.Core/Coroutine/CoroutineScheduler.cs`，因为剩余两个 `MA0051` 都集中在协程启动与完成清理路径，且已有
  `CoroutineSchedulerTests`、`CoroutineSchedulerAdvancedTests` 覆盖句柄创建、取消、完成状态、标签分组和等待语义
- 将 `Run` 拆分为：
  - `AllocateSlotIndex`
  - `CreateRunningSlot`
  - `RegisterCancellationCallback`
  - `RegisterStartedCoroutine`
  - `CreateCoroutineMetadata`
  - `ResetCompletionTracking`
- 将 `FinalizeCoroutine` 拆分为：
  - `TryGetFinalizableCoroutine`
  - `UpdateCompletionMetadata`
  - `ApplyCompletionMetadata`
  - `ReleaseCompletedCoroutine`
  - `CompleteCoroutineLifecycle`
- 保持取消回调只做跨线程入队、`Prewarm` 时机、统计记录文本、`RemoveTag` / `RemoveGroup` / `WakeWaiters` 顺序以及
  `OnCoroutineFinished` 的同步触发时机不变，只收缩主方法长度并补齐辅助方法意图注释
- 验证通过：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -p:RestorePackagesPath=<linux-nuget-cache> -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`23 Warning(s)`，`0 Error(s)`；`CoroutineScheduler.cs` 已不再出现在 `MA0051` 列表
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter FullyQualifiedName~CoroutineScheduler -p:RestoreFallbackFolders="" -p:RestorePackagesPath=<linux-nuget-cache> -nologo`
    - 结果：`34 Passed`，`0 Failed`
- 当前 `MA0051` 主线已经在本主题下完成；下一步若继续，应先重新评估剩余 `MA0048`、`MA0046`、`MA0002`、`MA0016` 的
  收敛价值与改动风险，再决定是否开启下一轮 warning family

## 2026-04-21 — RP-006

### 阶段：Store `MA0051` 收口（RP-006）

- 依据 active tracking 中“继续只选一个 `GFramework.Core` 结构性切入点”的约束，本轮选择
  `GFramework.Core/StateManagement/Store.cs`，因为该文件的两个 `MA0051` 都集中在 dispatch / reducer snapshot 逻辑，
  且已有 `StoreTests` 覆盖 dispatch、batch、history 和多态 reducer 匹配语义
- 在正式验证前先处理 WSL 环境噪音：当前 worktree 的 `GFramework.Core/obj/project.assets.json` 是 Windows 侧 restore
  产物，`--no-restore` 构建会继续引用宿主 Windows fallback package folder；本轮先执行一次 Linux 侧
  `dotnet restore GFramework.Core/GFramework.Core.csproj -p:RestoreFallbackFolders="" -p:RestorePackagesPath=<linux-nuget-cache> --ignore-failed-sources -nologo`
  刷新资产文件，再继续 warnings-only build
- 将 `Dispatch` 拆分为：
  - `EnterDispatchScope`
  - `TryCommitDispatchResult`
  - `ExitDispatchScope`
- 将 `CreateReducerSnapshotCore` 拆分为：
  - `CreateExactReducerSnapshot`
  - `CreateAssignableReducerSnapshot`
  - `CollectReducerMatches`
  - `CompareReducerMatch`
- 保持 `_dispatchGate -> _lock` 的锁顺序、middleware 锁外执行、批处理通知折叠以及“精确类型 -> 基类 -> 接口 ->
  注册顺序”的 reducer 稳定排序语义不变，只收缩主方法长度并补齐辅助方法意图注释
- 验证通过：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -p:RestorePackagesPath=<linux-nuget-cache> -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`25 Warning(s)`，`0 Error(s)`；`Store.cs` 已不再出现在 `MA0051` 列表
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter FullyQualifiedName~StoreTests -p:RestoreFallbackFolders="" -p:RestorePackagesPath=<linux-nuget-cache> -nologo`
    - 结果：`30 Passed`，`0 Failed`
- 下一步保持同一节奏：只在 `CoroutineScheduler.cs` 的 `Run` / `FinalizeCoroutine` 两个 `MA0051` 中继续，不与其他
  warning 家族混做

## 2026-04-21 — RP-005

### 阶段：PauseStackManager `MA0051` 收口（RP-005）

- 按 active tracking 中“继续只选一个 `GFramework.Core` 结构性切入点”的约束，本轮选择
  `GFramework.Core/Pause/PauseStackManager.cs`，因为该文件体量明显小于 `CoroutineScheduler` 和 `Store`，
  且已有稳定的 `PauseStackManagerTests` 覆盖暂停栈、跨组独立性、事件通知与并发 `Push/Pop` 行为
- 先用 `warnings-only` 定向构建确认 `DestroyAsync` 与 `Pop` 仍分别命中 `MA0051`，再把逻辑拆分为：
  - `TryBeginDestroy`
  - `NotifyDestroyedGroups`
  - `TryPopEntry`
  - `RemoveEntryFromStack`
- 额外抽出 `CreateHandlerSnapshot` 与 `NotifyHandlersSnapshot`，统一普通通知与销毁补发路径的处理器排序和异常日志，
  保持原有“锁内采集快照、锁外调用处理器与事件”的并发策略不变
- 为销毁路径新增 `DestroyAsync_Should_NotifyResumedGroups`，验证当多个暂停组在销毁前仍为暂停态时，
  处理器和事件订阅者都会收到 `IsPaused=false` 的恢复信号
- 验证通过：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -p:RestoreFallbackFolders= -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`27 Warning(s)`，`0 Error(s)`；`PauseStackManager.cs` 已不再出现在 `MA0051` 列表
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter FullyQualifiedName~PauseStackManagerTests -p:RestoreFallbackFolders=`
    - 结果：`25 Passed`，`0 Failed`
- 下一步保持原节奏：只在 `CoroutineScheduler` 或 `Store` 中二选一继续，不与其他 warning 家族混做

## 2026-04-21 — RP-003

### 阶段：Architecture 生命周期 `MA0051` 收口（RP-003）

- 依据 active tracking 中“继续只选一个 `GFramework.Core` 结构性切入点”的约束，选定
  `GFramework.Core/Architectures/ArchitectureLifecycle.cs`，因为文件体量适中且已有
  `ArchitectureLifecycleBehaviorTests` 覆盖阶段流转、销毁顺序和 late registration 行为
- 先用 `warnings-only` 定向构建确认 `ArchitectureLifecycle.InitializeAllComponentsAsync` 仍在报
  `MA0051`，随后把主流程拆成：
  - `CreateInitializationPlan`
  - `InitializePhaseComponentsAsync`
  - `MarkInitializationCompleted`
- 保持原有阶段顺序 `Before* -> After*`、批量日志文本和异步初始化策略不变，只压缩主方法长度
- 修正新增 `InitializationPlan` 记录类型的 XML `<param>` 名称大小写，避免引入文档告警
- 验证通过：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -p:RestoreFallbackFolders= -nologo -clp:Summary;WarningsOnly`
    - 结果：`29 Warning(s)`，`0 Error(s)`；`ArchitectureLifecycle.cs` 已不再出现在 warning 列表
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter FullyQualifiedName~ArchitectureLifecycleBehaviorTests -p:RestoreFallbackFolders=`
    - 结果：`6 Passed`，`0 Failed`

## 2026-04-21 — RP-004

### 阶段：PR review follow-up（RP-004）

- 使用 `gframework-pr-review` 抓取当前分支 PR #263 的最新 CodeRabbit review threads、MegaLinter 摘要与 CTRF 测试结果，
  只接受仍能在本地工作树复现的 review 点
- 在 `GFramework.Cqrs/Internal/CqrsHandlerRegistrar.cs` 中将 `TryCreateGeneratedRegistry` 的 `out` 参数改为
  `[NotNullWhen(true)] out ICqrsHandlerRegistry?`，移除三处 `null!` 抑制，保持激活失败时的日志文本与回退语义不变
- 修正 active trace 中重复的 `## 2026-04-21` 二级标题，消除 CodeRabbit 报告的 markdownlint `MD024`
- 核实 PR 信号后确认：当前 CTRF 报告为 `2134 passed / 0 failed`；MegaLinter 唯一告警来自 CI 环境中的 `dotnet-format`
  restore 失败，不是本地代码格式问题
- 验证通过：
  - `dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:UseSharedCompilation=false -p:RestoreFallbackFolders=`
    - 结果：`0 Warning(s)`，`0 Error(s)`

## 2026-04-21 — RP-002

### 阶段：CQRS `MA0051` 收口（RP-002）

- 依据 active tracking 中“先只选一个结构性切入点”的约束，选定 `GFramework.Cqrs/Internal/CqrsHandlerRegistrar.cs`
  作为低风险下一步，因为它已有稳定的 targeted test 覆盖 generated registry、reflection fallback、缓存和重复注册行为
- 将 `TryRegisterGeneratedHandlers` 拆分为 registry 激活、批量注册和 fallback 结果构建三个辅助阶段，同时把
  `GetReflectionFallbackMetadata` 的直接类型解析与按名称解析拆开，降低长方法复杂度但不改日志文本与回退语义
- 顺手修正 `RegisterAssemblyHandlers` 内部调试日志的缩进，未改注册顺序、生命周期或服务描述符写入逻辑
- 验证通过：
  - `dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:UseSharedCompilation=false -p:RestoreFallbackFolders=`
    - 结果：`0 Warning(s)`，`0 Error(s)`
  - `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter FullyQualifiedName~CqrsHandlerRegistrarTests -p:RestoreFallbackFolders=`
    - 结果：`11 Passed`，`0 Failed`
- 新发现的环境注意事项：
  - 当前 WSL worktree 下若不显式传入 `-p:RestoreFallbackFolders=`，Linux `dotnet` 会读取不存在的 Windows fallback package
    folder 并导致 `ResolvePackageAssets` 失败
  - sandbox 内运行 `dotnet` 会因 MSBuild named-pipe 限制失败；需要在提权上下文中执行 .NET 验证

## 2026-04-19

### 阶段：local-plan 迁移收口（RP-001）

- 复核当前工作树后确认：`local-plan/` 仅保存 analyzer warning reduction 主题的 durable recovery state，不应继续作为
  worktree-root 遗留目录存在
- 按 `ai-plan` 治理规则建立 `ai-plan/public/analyzer-warning-reduction/` 主题目录，并补齐：
  - `todos/`
  - `traces/`
  - `archive/todos/`
  - `archive/traces/`
- 将旧 `local-plan` 中的详细 tracking / trace 迁入主题内历史归档，保留 `RP-001` 的完整实现与验证上下文
- 新建精简版 active tracking / trace 入口，并在 `ai-plan/public/README.md` 中建立
  `fix/analyzer-warning-reduction-batch` -> `analyzer-warning-reduction` 的 worktree 映射
- 删除旧 `local-plan` 文件，避免 `boot` 或后续协作者继续从过时目录恢复
- 验证通过：
  - `find ai-plan/public/analyzer-warning-reduction -maxdepth 3 -type f | sort`
  - `dotnet build GFramework.Core.Abstractions/GFramework.Core.Abstractions.csproj -c Release -p:RestoreFallbackFolders=`

### Archive Context

- 历史跟踪归档：
  - `ai-plan/public/analyzer-warning-reduction/archive/todos/analyzer-warning-reduction-history-rp001.md`
- 历史 trace 归档：
  - `ai-plan/public/analyzer-warning-reduction/archive/traces/analyzer-warning-reduction-history-rp001.md`

### 下一步

1. 若继续 analyzer warning reduction，优先回到 `GFramework.Core` 剩余 `MA0051` 热点，并继续保持“单 warning family、单切入点”的节奏
2. 后续所有 WSL 下的 .NET 定向验证命令继续显式附带 `-p:RestoreFallbackFolders=`，避免把环境问题误判成代码回归
