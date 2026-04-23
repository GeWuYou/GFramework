# Analyzer Warning Reduction 跟踪

## 目标

继续以“优先低风险、保持行为兼容”为原则收敛当前仓库的 Meziantou analyzer warnings，并在首轮大规模清理完成后，
判断剩余结构性 warning 是否值得在下一轮继续推进。

## 当前恢复点

- 恢复点编号：`ANALYZER-WARNING-REDUCTION-RP-041`
- 当前阶段：`Phase 41`
- 当前焦点：
  - 已通过第五个有效 subagent 切片完成
    `CqrsHandlerRegistryGeneratorTests.cs`
    `Generates_Precise_Service_Type_For_Hidden_Generic_Type_Definitions()` 的 `MA0051` 收口：
    将内联 `source` 文本提取为类级常量，保持既有 expected 常量和断言语义不变
  - 当前 `GFramework.SourceGenerators.Tests` Release warnings-only 基线已从 `11` 条降到 `10` 条；
    行号 `680` 已从 `MA0051` 输出中消失，剩余热点继续集中在
    `CqrsHandlerRegistryGeneratorTests.cs`
  - 已通过第四个有效 subagent 切片完成
    `CqrsHandlerRegistryGeneratorTests.cs`
    `Generates_Precise_Service_Type_For_Hidden_Array_Type_Arguments()` 的 `MA0051` 收口：
    将内联 `source` 文本提取为类级常量，保持既有 expected 常量和断言语义不变
  - 当前 `GFramework.SourceGenerators.Tests` Release warnings-only 基线已从 `12` 条降到 `11` 条；
    行号 `607` 已从 `MA0051` 输出中消失，剩余热点继续集中在
    `CqrsHandlerRegistryGeneratorTests.cs`
  - 已通过第三个有效 subagent 切片完成
    `CqrsHandlerRegistryGeneratorTests.cs`
    `Generates_Direct_Interface_Registrations_For_Hidden_Implementation_When_Handler_Interface_Is_Public()`
    的 `MA0051` 收口：将内联 `source` 文本提取为类级常量，保持既有 expected 常量和断言语义不变
  - 当前 `GFramework.SourceGenerators.Tests` Release warnings-only 基线已从 `13` 条降到 `12` 条；
    行号 `536` 已从 `MA0051` 输出中消失，剩余热点继续集中在
    `CqrsHandlerRegistryGeneratorTests.cs`
  - 已通过第二个有效 subagent 切片完成
    `CqrsHandlerRegistryGeneratorTests.cs`
    `Generates_Visible_Handlers_And_Self_Registers_Private_Nested_Handler_When_Assembly_Contains_Hidden_Handler()`
    的 `MA0051` 收口：将内联 `source` 文本提取为类级常量，保持既有 expected 常量和断言语义不变
  - 当前 `GFramework.SourceGenerators.Tests` Release warnings-only 基线已从 `14` 条降到 `13` 条；
    行号 `454` 已从 `MA0051` 输出中消失，剩余热点继续集中在
    `CqrsHandlerRegistryGeneratorTests.cs`
  - 已通过 subagent 循环的首个可交付切片完成
    `GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs`
    `Generates_Assembly_Level_Cqrs_Handler_Registry()` 的 `MA0051` 收口：
    将内联 `source` / `expected` 文本提取为类级常量，保持生成文本、断言语义和文件名不变
  - 当前 `GFramework.SourceGenerators.Tests` Release warnings-only 基线已从 `15` 条降到 `14` 条；
    行号 `337` 已从 `MA0051` 输出中消失，剩余热点仍全部集中在
    `CqrsHandlerRegistryGeneratorTests.cs`
  - 已确认当前分支相对 `origin/main` 的唯一变更文件数仍只有 `3`；按该统计口径距离用户要求的
    “接近 `75` 个文件变更”仍很远，需要继续多轮切片
  - 已完成 `GFramework.SourceGenerators.Tests/Config/SchemaConfigGeneratorTests.cs` 的 `MA0051` 收口：
    将共享 consumer runtime fixture 提取到类级常量，并把生成结果收集与 catalog 契约断言拆成小 helper，
    保持 schema 文本、断言语义与生成输出契约不变
  - 当前 `GFramework.SourceGenerators.Tests` Release warnings-only 基线已从 `22` 条降到 `15` 条；
    `SchemaConfigGeneratorTests.cs` 已不再出现在 `MA0051` 列表中，剩余热点全部集中在
    `CqrsHandlerRegistryGeneratorTests.cs`
  - 已完成 `SchemaConfigGeneratorTests` 定向验证：串行重跑 `50` 个用例全部通过；并确认先前并行 build/test
    触发的 `MSB3030` / `CS0006` 属于共享输出竞争噪音，不是代码回归
  - 已按 `gframework-boot` 重新恢复当前 worktree：确认分支 `fix/analyzer-warning-reduction-batch` 仍映射到
    `analyzer-warning-reduction`，且当前不存在 `ai-plan/private/` 私有恢复上下文
  - 已重新抓取当前分支关联的 PR #273 review 状态：PR 已处于 `CLOSED`，latest-head review 仍显示 `2` 条
    CodeRabbit open thread，但本地复核后 `GeneratorSnapshotTest` 的 snapshot 路径空值防御已显式改为
    `InvalidOperationException` 防御，`SchemaConfigGenerator` 的 `dependentSchemas` / `allOf` / conditional helper
    也已补齐 XML 文档，当前更像历史线程未随已关闭 PR 一起收敛
  - 已重新以 `GFramework.SourceGenerators.Tests` Release warnings-only build 复核当前 `MA0051` 热点：
    基线现为 `22` 条，且已不再落在 `GeneratorSnapshotTest`、`ContextRegistrationAnalyzerTests` 或
    `ContextGetGeneratorTests`，而是集中在 `CqrsHandlerRegistryGeneratorTests.cs`（`15` 条）与
    `SchemaConfigGeneratorTests.cs`（`7` 条）
  - 已完成 `GFramework.Core` 当前 `MA0016` / `MA0002` / `MA0015` / `MA0077` 低风险收口批次
  - 已复核 `net10.0` 下的 `MA0158` 基线：`GFramework.Core` / `GFramework.Cqrs` 当前共有 `16` 个 object lock
    建议点，属于跨 target 兼容性风险，不在本轮直接批量替换
  - 已完成 `GFramework.Core.SourceGenerators/Rule/ContextAwareGenerator.cs` 的剩余 `MA0051` 结构拆分，生成输出保持不变
  - 已完成 `GFramework.Cqrs.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 的 `MA0051` 结构拆分，生成输出保持不变
  - 已完成 `GFramework.Game.SourceGenerators/Config/SchemaConfigGenerator.cs` 的 `MA0006` 低风险收口，schema 关键字比较显式使用
    `StringComparison.Ordinal`
  - 已完成 `SchemaConfigGenerator.cs` 的第一批 `MA0051` 结构拆分：schema 入口解析、属性解析、schema 遍历、数组属性解析、
    约束文档生成与若干生成代码发射 helper 已拆出语义阶段
  - 已完成当前 PR #269 review follow-up：`CqrsHandlerRegistryGenerator` 按职责拆分为 partial 生成器文件，
    `ContextAwareGenerator` 已补上字段名去冲突与锁内读取修正，`Option<T>` 补齐 `<remarks>` 契约说明
  - 已完成当前 PR #269 第二轮 follow-up：恢复 `EasyEvents`、`CollectionExtensions`、`LoggingConfiguration` 与
    `FilterConfiguration` 的公共 API 兼容形状，并将 analyzer 兼容性处理收敛到局部 pragma
  - 已完成当前 PR #269 第三轮 follow-up：继续收口 `SchemaConfigGenerator` 的根类型标识符校验与 XML 文档转义，
    并补齐 `LoggingConfigurationTests`、`CollectionExtensionsTests`、`Cqrs` helper 抽取与 `ai-plan` 命令文本修正
  - 已完成当前 PR #269 第四轮 follow-up：将 `CqrsHandlerRegistryGenerator` 的 Roslyn error type 直接引用改为
    运行时精确查找路径，并为 `SchemaConfigGenerator` 补上根 `type` 非字符串时的防御与回归测试
  - 已完成当前 PR #269 第五轮 follow-up：`SchemaConfigGenerator` 补上归一化后属性名冲突诊断并新增
    `GF_ConfigSchema_014`，`CqrsHandlerRegistryGenerator` 将 `dynamic` 归一化为 `global::System.Object`，
    同时收紧相关 generator regression tests
  - 已完成当前 PR #269 failed-test follow-up：修正 `SchemaConfigGeneratorTests`
    `Run_Should_Assign_Globally_Unique_Reference_Metadata_Member_Names` 的测试输入，使其继续覆盖
    reference metadata 成员名全局去冲突，但不再依赖现已被 `GF_ConfigSchema_014` 拦截的非法同层 schema key 冲突
  - 已完成当前 PR #269 Greptile follow-up：`ContextAwareGenerator` 现在会把基类链显式成员名也纳入
    `_gFrameworkContextAware*` 字段分配冲突检测，并新增 inherited-field collision 快照回归测试
  - 已完成当前分支与 `main` 的 `CqrsHandlerRegistryGenerator.cs` 文件级冲突收口：确认 `main` 侧新增的是
    `OrderedRegistrationKind` / `RuntimeTypeReferenceSpec` 的 XML 文档，现已按当前 partial 拆分结构迁移到
    `CqrsHandlerRegistryGenerator.Models.cs`，不回退已完成的生成器拆分
  - 已完成 `SchemaConfigGenerator.cs` 剩余 `MA0051` 收口：将 `dependentRequired` / `allOf` / conditional schema 校验
    拆成更小的验证阶段，并将 `GenerateTableClass`、`GenerateBindingsClass`、`AppendGeneratedConfigCatalogType`
    拆成稳定的代码发射 helper，保持生成输出与快照一致
  - 已更新 `AGENTS.md`：变更模块必须运行对应 `dotnet build -c Release`，并处理或显式报告模块构建 warning，
    不再默认留给长期 warning 清理分支
  - `CoroutineScheduler` 的 tag/group 字典已显式使用 `StringComparer.Ordinal`，保持既有区分大小写语义
  - `EasyEvents.AddEvent<T>()` 的重复注册路径已恢复为 `ArgumentException`，以保持既有异常契约
  - `Option<T>` 已声明 `IEquatable<Option<T>>`，与已有强类型 `Equals(Option<T>)` 契约对齐
  - 当前 `GFramework.Core` `net8.0` warnings-only 基线已降到 `0` 条
  - 当前 `GFramework.Core.SourceGenerators` warnings-only 基线已降到 `0` 条
  - 当前 `GFramework.Cqrs.SourceGenerators` warnings-only 基线已降到 `0` 条
  - 当前 `GFramework.Game.SourceGenerators` warnings-only 基线已从 `46` 条降到 `0` 条
  - 已完成 `GFramework.SourceGenerators.Tests` 低风险 `MA0004` / `MA0048` 收口：测试辅助器改为直接返回 `Task`，
    文件 I/O 显式补齐 `ConfigureAwait(false)`，`AnalyzerTestDriver` 文件名与类型名重新对齐
  - 当前 `GFramework.SourceGenerators.Tests` warnings-only 基线已从 `61` 条降到 `49` 条，剩余 warning 均为 `MA0051`
  - 已完成 `GFramework.SourceGenerators.Tests/Logging/LoggerGeneratorSnapshotTests.cs` 的 `MA0051` 收口：同构 snapshot
    场景已收敛为模板化 helper，保留原有快照目录与生成器输入语义不变
  - 当前 `GFramework.SourceGenerators.Tests` Release build 基线已从 `49` 条降到 `43` 条；`LoggerGeneratorSnapshotTests.cs`
    已不再出现在 `MA0051` 列表中
  - 已完成 `GFramework.SourceGenerators.Tests/Architectures/AutoRegisterModuleGeneratorTests.cs` 的 `MA0051` 收口：
    将内联测试源码与期望快照抽到类级常量、补齐测试类 XML 文档，并将仅作转发的异步测试改为直接返回 `Task`
  - 当前 `GFramework.SourceGenerators.Tests` Release build 基线已从 `43` 条降到 `40` 条；
    `AutoRegisterModuleGeneratorTests.cs` 已不再出现在 `MA0051` 列表中
  - 已完成 `GFramework.SourceGenerators.Tests/Config/SchemaConfigGeneratorSnapshotTests.cs` 的 `MA0051` 收口：
    将 monster 场景的运行时契约与 schema 输入提取为类级常量，并把生成结果与快照目录解析拆成小 helper，保持
    生成文件名、快照目录和断言语义不变
  - 当前 `GFramework.SourceGenerators.Tests` Release build 基线已从 `40` 条降到 `39` 条；
    `SchemaConfigGeneratorSnapshotTests.cs` 已不再出现在 `MA0051` 列表中
  - 已完成当前 PR #273 review follow-up 首轮核对：确认本地仍成立的问题集中在
    `SchemaConfigGenerator` helper XML 文档、`GeneratorSnapshotTest` 的 `StringComparison.Ordinal` /
    snapshot 路径空值防御、`AutoRegisterModuleGeneratorTests` 的 XML 文档位置，以及
    `SchemaConfigGeneratorSnapshotTests` 的 monster 快照覆盖缺口
  - 已将 monster 快照场景扩展到 `dependentRequired`、`dependentSchemas`、`allOf` 与 object-focused
    `if/then/else`，以便把新增 schema 约束文档纳入 snapshot 验证
  - 已完成本轮定向验证：
    `GFramework.Game.SourceGenerators` Release build 通过；
    `GFramework.SourceGenerators.Tests` 在 `-m:1 --no-restore` 下 Release build 通过；
    `SchemaConfigGeneratorSnapshotTests` 与 `AutoRegisterModuleGeneratorTests` 定向测试共 `4` 项全部通过
  - 当前验证仍受环境/基线约束：
    `GFramework.SourceGenerators.Tests` Release build 保留既有 `MA0051` warning 基线；
    NuGet vulnerability audit 在离线环境下产生 `NU1900`
  - `GFramework.Godot` 的 `Timing.cs` 已同步适配新事件签名，但当前 worktree 的 Godot restore 资产仍受 Windows fallback package folder 干扰，独立 build 需在修复资产后补跑
  - 后续继续按 warning 类型和数量批处理，而不是回退到按单文件切片推进
  - 下一轮默认重新抓取 PR #273 最新 review 线程，并确认本轮 snapshot 更新后是否还存在剩余 open thread 或
    `dotnet-format` 细项
  - 单次 `boot` 的工作树改动上限控制在约 `100` 个文件以内，避免 recovery context 与 review 面同时失控
  - 若任务边界互不冲突，允许使用不同模型的 subagent 并行处理不同 warning 类型或不同目录，但必须遵守显式 ownership

## 当前状态摘要

- 已完成 `GFramework.Core`、`GFramework.Cqrs`、`GFramework.Godot` 与部分 source generator 的低风险 warning 清理
- 已完成多轮 CodeRabbit follow-up 修复，并用定向测试与项目/解决方案构建验证了关键回归风险
- 已完成当前 PR #265 review follow-up：修复 `CoroutineScheduler` 的零容量扩容边界，并补上 `Store` dispatch 作用域的异常安全回滚
- 已继续完成当前 PR #265 review follow-up：修复 `Event<T>` 与 `Event<T, TK>` 监听器计数的 off-by-one，并补充回归测试
- 已增强 `gframework-pr-review` 脚本与 skill 文档，降低超长 JSON 直出导致的 review 信号漏看风险
- 已完成 `GFramework.Core` 当前 `MA0046` 批次：将阶段、协程与异步日志事件统一迁移到 `EventHandler<TEventArgs>` 形状，
  并同步更新 `GFramework.Godot` 订阅点、定向测试与 `docs/zh-CN` 示例
- 已完成当前 PR #267 review follow-up：修复 `AsyncLogAppender` 的 `ILogAppender.Flush()` 双重完成通知，并补齐
  `PhaseChanged` / `CoroutineExceptionEventArgs` XML 文档、`PhaseChanged` 迁移说明和 `ai-plan` 基线注释
- 已完成当前 PR #267 failed-test follow-up：修复 `AsyncLogAppender.Flush()` 在队列已被后台线程提前清空时仍可能
  等待满默认超时并返回 `false` 的竞态，并通过整包 `GFramework.Core.Tests` 重新验证
- 已完成当前 `GFramework.Core` `net8.0` 剩余低风险 analyzer warning 批次；warnings-only 基线已降到 `0` 条
- 已完成 `GFramework.Core.SourceGenerators` 中 `ContextAwareGenerator` 的剩余 `MA0051` 收口；warnings-only 基线已降到 `0` 条
- 已完成 `GFramework.Cqrs.SourceGenerators` 中 `CqrsHandlerRegistryGenerator` 的剩余 `MA0051` 收口；warnings-only 基线已降到 `0` 条
- 已完成当前 PR #269 的 review follow-up：收口 `ContextAwareGenerator` 的字段命名冲突 / 锁内读取契约、
  `CqrsHandlerRegistryGenerator` 的运行时类型 null 防御与超大文件拆分、`SchemaConfigGenerator` 的取消语义，
  并恢复 `EasyEvents` / `CollectionExtensions` / logging 配置模型的公共 API 兼容形状
- 已完成当前 PR #269 的第四轮 review follow-up：确认 5 个 latest-head 未解决线程中仅剩 2 个本地仍成立，
  已分别在 `CqrsHandlerRegistryGenerator` 与 `SchemaConfigGenerator` 中收口，并补齐定向 generator regression tests
- 已完成当前 PR #269 的第五轮 review follow-up：收口 `SchemaConfigGenerator` 的归一化字段名冲突诊断、
  `CqrsHandlerRegistryGenerator` 的 `dynamic` 类型引用风险，并同步更新 `AGENTS.md` 的模块 build / warning 治理规范
- 已完成当前 PR #269 的 failed-test follow-up：将 reference metadata 成员名唯一性回归测试改为合法 schema 路径组合，
  并重新通过定向 generator test
- 已完成当前 PR #269 的 Greptile follow-up：修复 `ContextAwareGenerator` 未覆盖基类成员名冲突的问题，并补齐
  inherited-collision 快照测试
- 已完成当前分支与 `main` 的 `CqrsHandlerRegistryGenerator.cs` 冲突化解：保留当前 partial 结构，并把
  `main` 侧新增的模型文档合并到 `CqrsHandlerRegistryGenerator.Models.cs`
- 已完成 `GFramework.Game.SourceGenerators` 中 `SchemaConfigGenerator` 的剩余 `MA0051` 收口；warnings-only 基线已降到 `0` 条
- 已完成 `GFramework.SourceGenerators.Tests` 的首轮低风险 warning 清理；当前项目已清空 `MA0004` / `MA0048`，剩余 warning
  全部收敛为 `MA0051`
- 已完成 `LoggerGeneratorSnapshotTests` 的单文件 `MA0051` 收口；当前 `GFramework.SourceGenerators.Tests` Release build 基线已降到
  `43` 条，并通过 focused snapshot tests 保持行为不变
- 已完成 `AutoRegisterModuleGeneratorTests` 的单文件 `MA0051` 收口；当前 `GFramework.SourceGenerators.Tests` Release build 基线已降到
  `40` 条，并通过 focused generator tests 保持输出契约不变
- 已完成 `SchemaConfigGeneratorSnapshotTests` 的单文件 `MA0051` 收口；当前 `GFramework.SourceGenerators.Tests`
  Release build 基线已降到 `39` 条，并通过 focused snapshot test 保持生成输出契约不变
- 已完成 `RP-035` 启动复核：确认 PR #273 已关闭、历史 open thread 暂无新的本地修复点，且
  `GFramework.SourceGenerators.Tests` 当前剩余 `MA0051` 已重排为 `CqrsHandlerRegistryGeneratorTests` /
  `SchemaConfigGeneratorTests` 两个测试写集
- 已完成 `RP-036`：清空 `SchemaConfigGeneratorTests.cs` 当前 `MA0051`，并将
  `GFramework.SourceGenerators.Tests` Release warnings-only 基线进一步降到 `15` 条
- 已完成 `RP-037` 的首个 subagent 接收：`CqrsHandlerRegistryGeneratorTests.cs` 的
  `Generates_Assembly_Level_Cqrs_Handler_Registry()` 已抽出类级 fixture，当前测试项目基线进一步降到 `14` 条
- 已完成 `RP-038` 的第二个 subagent 接收：`HiddenNestedHandlerSelfRegistrationSource` 已提取到类级常量，
  当前测试项目基线进一步降到 `13` 条
- 已完成 `RP-039` 的第三个 subagent 接收：`HiddenImplementationDirectInterfaceRegistrationSource`
  已提取到类级常量，当前测试项目基线进一步降到 `12` 条
- 已完成 `RP-040` 的第四个 subagent 接收：`HiddenArrayResponseFallbackSource`
  已提取到类级常量，当前测试项目基线进一步降到 `11` 条
- 已完成 `RP-041` 的第五个 subagent 接收：`HiddenGenericEnvelopeResponseSource`
  已提取到类级常量，当前测试项目基线进一步降到 `10` 条

## 当前活跃事实

- 当前主题仍是 active topic，因为剩余结构性 warning 是否继续推进尚未决策
- `RP-001` 的详细实现历史、测试记录和 warning 热点清单已归档到主题内 `archive/`
- `RP-002` 已在不改公共契约的前提下完成 `CqrsHandlerRegistrar` 结构拆分，并通过定向 build/test 验证
- `RP-003` 已在不改生命周期契约的前提下完成 `ArchitectureLifecycle` 初始化主流程拆分，并通过定向 build/test 验证
- `RP-004` 已完成当前 PR review follow-up：修复 `TryCreateGeneratedRegistry` 的可空 `out` 契约并清理 trace 文档重复标题
- `RP-005` 已在不改公共 API 的前提下完成 `PauseStackManager` 两个 `MA0051` 的结构拆分，并补充销毁通知回归测试
- `RP-006` 已在不改公共 API 的前提下完成 `Store` 两个 `MA0051` 的结构拆分，并通过定向 build/test 验证 dispatch、
  多态 reducer 匹配与历史语义未回归
- `RP-007` 已在不改公共 API 的前提下完成 `CoroutineScheduler` 两个 `MA0051` 的结构拆分，并通过定向 build/test 验证
  调度、取消与完成状态语义未回归
- `RP-008` 将后续策略从“单文件 warning 切片”切换为“按类型批处理 + 文件数上限控制”，并允许在非冲突前提下使用
  不同模型的 subagent 并行处理
- `RP-009` 在不改公共 API 的前提下，将同名泛型家族收拢到与类型名一致的单文件中，清空当前 `GFramework.Core`
  `net8.0` 基线中的 `MA0048`，并通过定向 build/test 验证 `Command`、`Query`、`Event` 路径未回归
- `RP-010` 使用 `gframework-pr-review` 复核当前分支 PR #265 后，修复了仍在本地成立的两个 follow-up 风险：
  `CoroutineScheduler` 的 `initialCapacity: 0` 扩容越界，以及 `Store` 在 dispatch 快照阶段抛异常时可能残留
  `_isDispatching = true` 的锁死问题
- `RP-011` 根据补充复核继续收口 PR #265 的 outside-diff comment，修复 `Event<T>` / `Event<T, TK>` 默认 no-op
  委托导致的 `GetListenerCount()` off-by-one，并以定向事件测试验证注册、注销和计数语义
- `RP-012` 为 `gframework-pr-review` 增加 `--json-output`、`--section`、`--path` 与文本截断能力，并更新 skill 推荐用法，
  让“先落盘、再定向抽取”成为默认可操作路径
- `RP-013` 已完成 `GFramework.Core` 当前 `MA0046` 批次，并以新的事件参数类型替换阶段、协程和异步日志事件的
  非标准签名；`GFramework.Core` `net8.0` warnings-only 基线由 `15` 降至 `9`
- `RP-014` 使用 `gframework-pr-review` 复核当前分支 PR #267 的 latest head review threads、outside-diff comment 与
  nitpick comment 后，确认 8 条高信号项中仍成立的是 1 个行为 bug 与 7 个文档/测试/跟踪缺口，并按最小改动收口
- `RP-015` 使用 `$gframework-pr-review` 复核 PR #267 的 CTRF 失败测试评论后，确认 `AsyncLogAppender` 仍存在
  “队列已空但 Flush 仍超时失败”的竞态；该问题在本地整包 `GFramework.Core.Tests` 中可复现，现已修复并补上稳定回归测试
- `RP-016` 将 `GFramework.Core` 当前剩余 `MA0016` / `MA0002` / `MA0015` / `MA0077` 低风险批次清零，并用
  warnings-only build 与 focused tests 验证配置反序列化、集合扩展、事件重复注册、`Option<T>` 相等性和协程 tag/group 语义
- `RP-017` 复核 `MA0158` 当前仍是跨 target 锁类型迁移问题，因此先收口单点 `ContextAwareGenerator` `MA0051`，
  并通过 source generator 项目 build 与 `ContextAwareGeneratorSnapshotTests` 验证生成输出未回归
- `RP-018` 暂缓跨 target `MA0158`，转入 `GFramework.Cqrs.SourceGenerators` 的单文件结构性 warning；
  通过拆分 handler 分析、运行时类型引用构造、注册器源码发射与精确反射注册输出阶段，清空该项目当前 `MA0051`
- `RP-019` 转入 `GFramework.Game.SourceGenerators/Config/SchemaConfigGenerator.cs`，先完成低风险 `MA0006` 批次；
  通过 schema 类型比较 helper 与显式 `StringComparison.Ordinal` 清空当前项目的 `MA0006`
- `RP-020` 继续拆分 `SchemaConfigGenerator.cs` 的 `MA0051` 热点，将当前项目 warnings-only 基线从 `19` 条降到 `9` 条，
  并用 focused schema generator tests 验证 50 个用例通过
- `RP-032` 已完成 `AutoRegisterModuleGeneratorTests` 的 3 个 `MA0051` 收口：通过提取类级常量承载测试源码与快照，保持
  生成文件名、断言路径与源生成输出不变；`GFramework.SourceGenerators.Tests` warnings-only 基线由 `43` 降至 `40`
- `RP-033` 已完成 `SchemaConfigGeneratorSnapshotTests` 的 `MA0051` 收口：monster schema 运行时契约与 schema 输入已提取为
  类级常量，生成结果映射与快照目录解析已拆为小 helper；`GFramework.SourceGenerators.Tests` warnings-only 基线由 `40` 降至 `39`
- `RP-035` 已完成启动级恢复核对：当前分支对应的 GitHub PR #273 已关闭，因此 remaining open thread 仅作为历史信号参考；
  下一轮应以本地 `warnings-only` build 的实时热点为主，而不是继续按已过时的 `GeneratorSnapshotTest` /
  `ContextRegistrationAnalyzerTests` 建议恢复
- `RP-036` 已完成 `SchemaConfigGeneratorTests` 的单文件 `MA0051` 收口：共享 runtime fixture、
  generated-source 收集与 catalog 契约断言均已拆出 helper；当前测试项目剩余 `MA0051` 已全部收敛到
  `CqrsHandlerRegistryGeneratorTests`
- `RP-037` 已验证 subagent 循环开始产生稳定吞吐，但当前一轮只消掉 `1` 个 warning 位点；
  若继续按“唯一变更文件数接近 `75`”推进，需要接受很多轮单文件、单方法级切片
- `RP-038` 继续验证了“单方法 + 主线程记录恢复点”的 subagent 节奏可稳定复用，但按当前速度离
  “接近 `75` 个唯一变更文件”仍然非常远
- `RP-039` 进一步确认：只要 subagent 继续在同一热点文件内逐点消除 warning，唯一变更文件数会基本停留在 `4`
  左右，不会因为重复修改同一文件快速逼近 `75`
- `RP-040` 延续了这一趋势：当前吞吐稳定，但按“唯一变更文件数接近 `75`”作为停止条件并不匹配当前单文件收口节奏
- `RP-041` 再次确认当前分支相对 `origin/main` 的唯一变更文件数仍是 `4`；若继续只收口同一文件的 warning，
  该计数基本不会上涨
- `RP-021` 使用 `$gframework-pr-review` 复核当前分支 PR #269 后，修复仍在本地成立的 4 个项：将
  `CqrsHandlerRegistryGenerator` 拆分为职责清晰的 partial 文件、为 `ContextAwareGenerator` 生成字段增加稳定前缀并补上
  `SetContextProvider` 的运行时 null 校验、为 `Option<T>` 补齐 `<remarks>`，并新增字段重名场景的生成器快照测试
- `RP-022` 继续复核 PR #269 的 latest-head review threads 与 nitpick，确认仍成立的项包括公共 API 兼容回退、
  `ContextAwareGenerator` 字段名真正去冲突与锁内读取、`SchemaConfigGenerator` 取消传播、`Cqrs` 运行时类型 null 防御；
  已补齐对应回归测试与 focused build/test 验证
- `RP-023` 继续复核 PR #269 剩余 nitpick/outside-diff 项，确认仍成立的项集中在 `SchemaConfigGenerator` 根类型名校验、
  aggregate registration comparer XML 文档转义、logging / collection 反射测试补强，以及跟踪文档中的
  `RestoreFallbackFolders=""` 可复制性问题
- `RP-024` 使用 `$gframework-pr-review` 继续复核 PR #269 latest-head unresolved threads，确认 `EasyEvents` 异常契约、
  `SchemaConfigGenerator` 取消传播与 `ContextAwareGenerator` 快照冲突线程均已在本地收口，仅剩 `Cqrs` error type
  直接引用与根 schema `type` 非字符串防御仍成立；现已补齐实现与回归测试
- `RP-025` 继续复核 PR #269 剩余 outside-diff / nitpick 信号后，确认本地仍成立的是 `SchemaConfigGenerator`
  的归一化字段名冲突与 `Cqrs` 对 `dynamic` 的直接类型引用；已分别补上诊断、运行时类型归一化与回归测试，
  并把“变更模块必须运行对应 build 且处理 warning”的治理规则写回 `AGENTS.md`
- `RP-029` 已完成 `SchemaConfigGenerator` 剩余 `MA0051` 收口：`GFramework.Game.SourceGenerators` 独立 Release
  warnings-only build 已清零，并通过 `SchemaConfigGenerator` focused generator tests 锁定生成输出未回退
- `RP-030` 已完成 `GFramework.SourceGenerators.Tests` 低风险 `MA0004` / `MA0048` 收口：`AnalyzerTestDriver` 文件名已与
  类型名一致，测试辅助器与 schema snapshot 断言路径已改为直接返回 `Task` 或显式使用 `ConfigureAwait(false)`；
  当前测试项目 warnings-only 基线从 `61` 条降到 `49` 条，剩余均为 `MA0051`
- `RP-031` 已完成 `LoggerGeneratorSnapshotTests` 的 `MA0051` 收口：重复场景源码改为模板化 helper 生成，
  当前 `GFramework.SourceGenerators.Tests` Release build 基线从 `49` 条降到 `43` 条，并通过 focused test 验证 6 个用例全部通过
- 当前工作树分支 `fix/analyzer-warning-reduction-batch` 已在 `ai-plan/public/README.md` 建立 topic 映射

## 当前风险

- analyzer 收口回退风险：后续若继续压 `MA0015` / `MA0016`，容易再次把公共 API 收窄成与既有契约不兼容的形状
  - 缓解措施：优先保留既有公共 API，并将兼容性例外收敛到局部 pragma；继续用反射断言覆盖返回类型、属性类型与异常类型
- 测试宿主稳定性风险：部分 Godot 失败路径在当前 .NET 测试宿主下仍不稳定
  - 缓解措施：继续优先使用稳定的 targeted test、项目构建和相邻 smoke test 组合验证
- 多目标框架 warning 解释风险：同一源位置会在多个 target framework 下重复计数
  - 缓解措施：继续以唯一源位置和 warning 家族为主要决策依据，而不是只看原始 warning 总数
- net10 专属 warning 风险：`MA0158` 建议使用 `System.Threading.Lock`，但项目多 target 时需要确认兼容边界
  - 缓解措施：下一轮先按 target framework 与 API 可用性评估，不直接批量替换共享源码中的 `object` lock
- source generator warning 外溢风险：运行 `GFramework.SourceGenerators.Tests` 会构建相邻 generator/test 项目，并在输出中混入
  测试项目自身的结构性 warning 基线
  - 缓解措施：继续以被修改项目的独立 warnings-only build 作为主验收，并用 focused generator test 验证行为
- source generator test warning 治理风险：`GFramework.SourceGenerators.Tests` 当前仍有 `43` 条既有 `MA0051` warning；
  一旦继续进入该写集，就必须把测试项目 warning 一并纳入本轮完成条件
  - 缓解措施：已先清空低风险 `MA0004` / `MA0048`，后续继续保持“单 warning family、单测试域”的节奏推进 `MA0051`
- ContextAware 基类命名隐藏风险：若生成器只看当前类型声明成员，派生规则会重新占用基类已声明的
  `_gFrameworkContextAware*` 字段名，导致生成成员隐藏继承状态并让快照无法锁定后缀分配行为
  - 缓解措施：本轮已改为遍历完整 base-type 链收集保留名，并用 inherited collision 快照用例锁定该行为
- Godot 资产文件环境风险：当前 worktree 的 `GFramework.Godot` restore/build 仍会命中 Windows fallback package folder
  - 缓解措施：后续若继续触达 Godot 模块，先用 Linux 侧 restore 资产或 Windows-hosted 构建链刷新该项目，再补跑定向 build
- 并行实现风险：批量收敛时若 subagent 写入边界不清晰，容易引入命名冲突或重复重构
  - 缓解措施：只在 warning 类型或目录边界清晰时并行；每个 subagent 必须有独占文件 ownership，主代理负责合并验证

## 活跃文档

- 历史跟踪归档：[analyzer-warning-reduction-history-rp001.md](../archive/todos/analyzer-warning-reduction-history-rp001.md)
- 历史 trace 归档：[analyzer-warning-reduction-history-rp001.md](../archive/traces/analyzer-warning-reduction-history-rp001.md)

## 验证说明

- `RP-001` 的详细 warning 清理、回归修复与定向验证命令均已迁入主题内历史归档
- `RP-002` 的定向验证结果：
  - `dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:UseSharedCompilation=false -p:RestoreFallbackFolders=`
  - `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter FullyQualifiedName~CqrsHandlerRegistrarTests -p:RestoreFallbackFolders=`
- `RP-003` 的定向验证结果：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -p:RestoreFallbackFolders= -nologo -clp:Summary;WarningsOnly`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter FullyQualifiedName~ArchitectureLifecycleBehaviorTests -p:RestoreFallbackFolders=`
- `RP-004` 的定向验证结果：
  - `dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:UseSharedCompilation=false -p:RestoreFallbackFolders=`
    - 结果：`0 Warning(s)`，`0 Error(s)`
- `RP-005` 的定向验证结果：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -p:RestoreFallbackFolders= -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`27 Warning(s)`，`0 Error(s)`；`PauseStackManager.cs` 已不再出现在 `MA0051` 列表中
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter FullyQualifiedName~PauseStackManagerTests -p:RestoreFallbackFolders=`
    - 结果：`25 Passed`，`0 Failed`
- `RP-006` 的定向验证结果：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -p:RestorePackagesPath=<linux-nuget-cache> -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`25 Warning(s)`，`0 Error(s)`；`Store.cs` 已不再出现在 `MA0051` 列表中
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter FullyQualifiedName~StoreTests -p:RestoreFallbackFolders="" -p:RestorePackagesPath=<linux-nuget-cache> -nologo`
    - 结果：`30 Passed`，`0 Failed`
- `RP-007` 的定向验证结果：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -p:RestorePackagesPath=<linux-nuget-cache> -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`23 Warning(s)`，`0 Error(s)`；`CoroutineScheduler.cs` 已不再出现在 `MA0051` 列表中
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter FullyQualifiedName~CoroutineScheduler -p:RestoreFallbackFolders="" -p:RestorePackagesPath=<linux-nuget-cache> -nologo`
    - 结果：`34 Passed`，`0 Failed`
- `RP-008` 的策略基线：
  - 当前 `GFramework.Core` 剩余 warning 分布：`MA0048=8`、`MA0046=6`、`MA0016=5`、`MA0002=2`、`MA0015=1`、`MA0077=1`
  - 后续批处理规则：优先按类型推进；若当轮主类型数量不足，可顺手吸收其他低冲突类型，不限定于 `MA0015` 与 `MA0077`
- `RP-009` 的定向验证结果：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`15 Warning(s)`，`0 Error(s)`；当前 `GFramework.Core` `net8.0` warnings-only 输出中已不再出现 `MA0048`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~CommandExecutorTests|FullyQualifiedName~AbstractAsyncCommandTests|FullyQualifiedName~QueryExecutorTests|FullyQualifiedName~AbstractAsyncQueryTests|FullyQualifiedName~EventTests" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`83 Passed`，`0 Failed`
- `RP-010` 的定向验证结果：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`15 Warning(s)`，`0 Error(s)`；新增修复未引入新的 `GFramework.Core` `net8.0` 构建错误
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~CoroutineSchedulerTests.Run_Should_Grow_From_Zero_Initial_Capacity|FullyQualifiedName~StoreTests.Dispatch_Should_Reset_Dispatching_Flag_When_Snapshot_Creation_Throws" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`2 Passed`，`0 Failed`
- `RP-011` 的定向验证结果：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`15 Warning(s)`，`0 Error(s)`；`Event.cs` 的 listener count 修复未引入新的 `GFramework.Core` `net8.0` 构建错误
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~EventTests.EventT_GetListenerCount_Should_Exclude_Placeholder_Handler|FullyQualifiedName~EventTests.EventTTK_GetListenerCount_Should_Exclude_Placeholder_Handler" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`2 Passed`，`0 Failed`
- `RP-012` 的定向验证结果：
  - `python3 -m py_compile .codex/skills/gframework-pr-review/scripts/fetch_current_pr_review.py`
    - 结果：通过；使用 `PYTHONPYCACHEPREFIX=/tmp/codex-pycache` 规避技能目录只读导致的 `__pycache__` 写入限制
  - `python3 .codex/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --help`
    - 结果：通过；`--json-output`、`--section`、`--path`、`--max-description-length` 已出现在 CLI 帮助中
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`0 Warning(s)`，`0 Error(s)`
- `RP-013` 的定向验证结果：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`9 Warning(s)`，`0 Error(s)`；相对 `RP-009` / `RP-011` 的 warnings-only 基线 `15 Warning(s)` 已降到 `9 Warning(s)`，
      当前 `GFramework.Core` `net8.0` 输出中已不再出现 `MA0046`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~ArchitectureLifecycleBehaviorTests|FullyQualifiedName~CoroutineSchedulerTests|FullyQualifiedName~AsyncLogAppenderTests" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`50 Passed`，`0 Failed`
  - `dotnet build GFramework.Godot/GFramework.Godot.csproj -c Release --no-restore -p:RestoreFallbackFolders="" -nologo`
    - 结果：失败；当前 worktree 的 Godot restore 资产仍引用 Windows fallback package folder，尚未完成独立项目编译验证
- `RP-014` 的定向验证结果：
  - `dotnet restore GFramework.Core.Tests/GFramework.Core.Tests.csproj -p:RestoreFallbackFolders="" -nologo`
    - 结果：通过；host Windows `dotnet` 首次验证前补齐了缺失的 `Meziantou.Analyzer 3.0.48` 包
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`9 Warning(s)`，`0 Error(s)`；`AsyncLogAppender` 行为修复与 XML / 文档补充未引入新的 `GFramework.Core` `net8.0` 构建错误
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~CoroutineSchedulerTests.Scheduler_Should_Raise_OnCoroutineException_With_EventArgs|FullyQualifiedName~AsyncLogAppenderTests.Flush_Should_Raise_OnFlushCompleted_With_Sender_And_Result|FullyQualifiedName~AsyncLogAppenderTests.ILogAppender_Flush_Should_Raise_OnFlushCompleted_Only_Once|FullyQualifiedName~ArchitectureLifecycleBehaviorTests.InitializeAsync_Should_Raise_PhaseChanged_With_Sender_And_EventArgs" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`4 Passed`，`0 Failed`
- `RP-015` 的验证结果：
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-restore --disable-build-servers --filter "FullyQualifiedName~AsyncLogAppenderTests"`
    - 结果：`15 Passed`，`0 Failed`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-restore --disable-build-servers`
    - 结果：`1607 Passed`，`0 Failed`
- `RP-016` 的验证结果：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`0 Warning(s)`，`0 Error(s)`；当前 `GFramework.Core` `net8.0` analyzer baseline 已清零
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~LoggingConfigurationTests|FullyQualifiedName~ConfigurableLoggerFactoryTests|FullyQualifiedName~CollectionExtensionsTests|FullyQualifiedName~EasyEventsTests|FullyQualifiedName~OptionTests|FullyQualifiedName~CoroutineGroupTests|FullyQualifiedName~CoroutineSchedulerTests" -m:1 -nologo`
    - 结果：`112 Passed`，`0 Failed`；测试构建仍会显示既有 `net10.0` `MA0158` 与 source generator `MA0051` warning
- `RP-017` 的验证结果：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net10.0 -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`16 Warning(s)`，`0 Error(s)`；当前 `MA0158` 跨 `GFramework.Core` / `GFramework.Cqrs`，本轮只记录基线不批量改锁
  - `dotnet build GFramework.Core.SourceGenerators/GFramework.Core.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`0 Warning(s)`，`0 Error(s)`；`ContextAwareGenerator.cs` 已不再出现 `MA0051`
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~ContextAwareGeneratorSnapshotTests" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`1 Passed`，`0 Failed`；测试构建仍显示相邻 source generator 和测试项目的既有 analyzer warning
- `RP-018` 的验证结果：
  - `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders= -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`0 Warning(s)`，`0 Error(s)`；`CqrsHandlerRegistryGenerator.cs` 当前 `MA0051` 已清零
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~CqrsHandlerRegistryGeneratorTests -m:1 -p:RestoreFallbackFolders= -nologo`
    - 结果：`14 Passed`，`0 Failed`
    - 说明：该 test project 构建仍显示 `GFramework.Game.SourceGenerators` 与测试项目中的既有 analyzer warning；本轮关注的
      `GFramework.Cqrs.SourceGenerators` 独立 build 已清零
- `RP-019` 的验证结果：
  - `dotnet restore GFramework.Game.SourceGenerators/GFramework.Game.SourceGenerators.csproj -p:RestoreFallbackFolders= -nologo`
    - 结果：通过；刷新 Linux 侧资产以清除 stale Windows fallback package folder
  - `dotnet build GFramework.Game.SourceGenerators/GFramework.Game.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders= -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`19 Warning(s)`，`0 Error(s)`；当前项目输出已不再出现 `MA0006`，剩余均为 `SchemaConfigGenerator.cs` 的
      `MA0051`
  - `dotnet restore GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -p:RestoreFallbackFolders= -nologo`
    - 结果：通过；刷新 test project 资产以清除 stale Windows fallback package folder
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~SchemaConfigGenerator -m:1 -p:RestoreFallbackFolders= -nologo`
    - 结果：`50 Passed`，`0 Failed`
    - 说明：测试项目构建仍显示既有 source generator test analyzer warning；不属于本轮写集
- `RP-020` 的验证结果：
  - `dotnet build GFramework.Game.SourceGenerators/GFramework.Game.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders= -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`9 Warning(s)`，`0 Error(s)`；当前项目剩余 warning 均为 `SchemaConfigGenerator.cs` 的 `MA0051`
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~SchemaConfigGenerator -m:1 -p:RestoreFallbackFolders= -nologo`
    - 结果：`50 Passed`，`0 Failed`
    - 说明：测试项目构建仍显示既有 source generator test analyzer warning；不属于本轮写集
- `RP-021` 的验证结果：
  - `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders= -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`0 Warning(s)`，`0 Error(s)`；拆分后最大单文件已降到 `851` 行，满足仓库 800-1000 行上限
  - `dotnet build GFramework.Core.SourceGenerators/GFramework.Core.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders= -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`0 Warning(s)`，`0 Error(s)`；`ContextAwareGenerator` 的字段命名与 provider 契约修复未引入新的 generator warning
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~ContextAwareGeneratorSnapshotTests|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests" -m:1 -p:RestoreFallbackFolders= -nologo`
    - 结果：先并行运行两条 `dotnet test` 时触发共享输出文件锁冲突；改为串行重跑后 `ContextAwareGeneratorSnapshotTests=2 Passed`、
      `CqrsHandlerRegistryGeneratorTests=14 Passed`
    - 说明：失败来自测试宿主并行写入同一 build 输出，不是代码回归；串行重跑后快照新增的字段重名场景和 CQRS 快照均通过
- `RP-022` 的验证结果：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -v minimal`
    - 结果：通过；`EasyEvents`、`CollectionExtensions` 与 logging 配置模型的兼容性回退未引入新的 `net8.0` 构建错误
  - `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -v minimal`
    - 结果：通过；`ContainingAssembly` null 防御与发射 helper 精简未引入新的构建错误
  - `dotnet build GFramework.Game.SourceGenerators/GFramework.Game.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -v minimal`
    - 结果：通过；仍保留既有 `9` 条 `SchemaConfigGenerator.cs` `MA0051` warning，非本轮新增
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~SchemaConfigGeneratorTests|FullyQualifiedName~ContextAwareGeneratorSnapshotTests|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests" -m:1 -p:RestoreFallbackFolders="" -v minimal`
    - 结果：`63 Passed`，`0 Failed`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~EasyEventsTests|FullyQualifiedName~CollectionExtensionsTests|FullyQualifiedName~LoggingConfigurationTests|FullyQualifiedName~ConfigurableLoggerFactoryTests" -m:1 -p:RestoreFallbackFolders="" -v minimal`
    - 结果：`38 Passed`，`0 Failed`
- `RP-023` 的验证结果：
  - `dotnet build GFramework.Game.SourceGenerators/GFramework.Game.SourceGenerators.csproj -c Release --no-restore -p:RestoreFallbackFolders="" -v minimal`
    - 结果：通过；仍保留既有 `9` 条 `SchemaConfigGenerator.cs` `MA0051` warning，未新增新的 generator warning
  - `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release --no-restore -p:RestoreFallbackFolders="" -v minimal`
    - 结果：通过；并行构建时 `GFramework.SourceGenerators.Common.dll` 复制阶段出现一次 `MSB3026` 重试，随后成功完成
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~SchemaConfigGeneratorTests|FullyQualifiedName~SchemaConfigGeneratorSnapshotTests|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests" -m:1 -p:RestoreFallbackFolders="" -v minimal`
    - 结果：`63 Passed`，`0 Failed`
    - 说明：测试项目构建仍会显示既有 `GFramework.SourceGenerators.Tests` analyzer warning；不属于本轮写集
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~CollectionExtensionsTests|FullyQualifiedName~LoggingConfigurationTests" -m:1 -p:RestoreFallbackFolders="" -v minimal`
    - 结果：`27 Passed`，`0 Failed`
- `RP-029` 的验证结果：
  - `dotnet restore GFramework.Game.SourceGenerators/GFramework.Game.SourceGenerators.csproj -p:RestoreFallbackFolders="" -nologo`
    - 结果：通过；刷新 Linux 侧 restore 资产以移除 Windows fallback package folder 干扰
  - `dotnet restore GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -p:RestoreFallbackFolders="" -nologo`
    - 结果：通过；focused test 所属测试项目已同步刷新 Linux 侧 restore 资产
  - `dotnet build GFramework.Game.SourceGenerators/GFramework.Game.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`0 Warning(s)`，`0 Error(s)`；`SchemaConfigGenerator.cs` 剩余 `MA0051` 已清零
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~SchemaConfigGenerator -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`54 Passed`，`0 Failed`
    - 说明：测试项目构建仍显示既有 `GFramework.SourceGenerators.Tests` `MA0048` / `MA0051` / `MA0004` warning；不属于本轮
      `GFramework.Game.SourceGenerators` 写集
- `RP-030` 的验证结果：
  - `dotnet restore GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -p:RestoreFallbackFolders="" -nologo`
    - 结果：通过；刷新 Linux 侧 restore 资产以移除 Windows fallback package folder 干扰
  - `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`49 Warning(s)`，`0 Error(s)`；当前项目已不再出现 `MA0004` / `MA0048`，剩余 warning 全部为 `MA0051`
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~GeneratorSnapshotTestSecurityTests|FullyQualifiedName~SchemaConfigGeneratorSnapshotTests|FullyQualifiedName~SchemaConfigGeneratorEnumTests" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`6 Passed`，`0 Failed`
- `RP-031` 的验证结果：
  - `dotnet restore GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -p:RestoreFallbackFolders="" -nologo`
    - 结果：通过；刷新 Linux 侧 restore 资产以支持后续串行 build/test 验证
  - `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:Summary`
    - 结果：`43 Warning(s)`，`0 Error(s)`；`LoggerGeneratorSnapshotTests.cs` 已不再出现在 `MA0051` 列表中
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-build --disable-build-servers --filter FullyQualifiedName~LoggerGeneratorSnapshotTests -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`6 Passed`，`0 Failed`
- `RP-033` 的验证结果：
  - `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -t:Rebuild --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`39 Warning(s)`，`0 Error(s)`；`SchemaConfigGeneratorSnapshotTests.cs` 已不再出现在 `MA0051` 列表中
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-build --disable-build-servers --filter FullyQualifiedName~SchemaConfigGeneratorSnapshotTests -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`1 Passed`，`0 Failed`
- `RP-035` 的验证结果：
  - `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output /tmp/current-pr-review.json`
    - 结果：成功定位当前分支关联的 `PR #273`；状态为 `CLOSED`，latest-head review threads 仍显示 `2` 条 open thread，
      test report 均为通过，MegaLinter 仅保留 docstring coverage / `dotnet-format` 历史信号
  - `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -t:Rebuild --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`22 Warning(s)`，`0 Error(s)`；剩余 `MA0051` 全部集中在 `CqrsHandlerRegistryGeneratorTests.cs` 与
      `SchemaConfigGeneratorTests.cs`
- `RP-036` 的验证结果：
  - `dotnet restore GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -p:RestoreFallbackFolders="" -nologo`
    - 结果：通过；刷新测试依赖输出，规避 `--no-build` 场景下的缺包噪音
  - `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -t:Rebuild --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`15 Warning(s)`，`0 Error(s)`；`SchemaConfigGeneratorTests.cs` 已不再出现在 `MA0051` 列表中
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-build --disable-build-servers --filter FullyQualifiedName~SchemaConfigGeneratorTests -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`50 Passed`，`0 Failed`
- `RP-037` 的验证结果：
  - `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -t:Rebuild --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`14 Warning(s)`，`0 Error(s)`；`CqrsHandlerRegistryGeneratorTests.cs` 的行号 `337` 已不再出现在 `MA0051` 列表中
- `RP-038` 的验证结果：
  - `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -t:Rebuild --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`13 Warning(s)`，`0 Error(s)`；`CqrsHandlerRegistryGeneratorTests.cs` 的行号 `454` 已不再出现在 `MA0051` 列表中
- `RP-039` 的验证结果：
  - `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -t:Rebuild --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`12 Warning(s)`，`0 Error(s)`；`CqrsHandlerRegistryGeneratorTests.cs` 的行号 `536` 已不再出现在 `MA0051` 列表中
- `RP-040` 的验证结果：
  - `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -t:Rebuild --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`11 Warning(s)`，`0 Error(s)`；`CqrsHandlerRegistryGeneratorTests.cs` 的行号 `607` 已不再出现在 `MA0051` 列表中
- `RP-041` 的验证结果：
  - `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -t:Rebuild --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`10 Warning(s)`，`0 Error(s)`；`CqrsHandlerRegistryGeneratorTests.cs` 的行号 `680` 已不再出现在 `MA0051` 列表中
- active 跟踪文件只保留当前恢复点、活跃事实、风险与下一步，不再重复保存已完成阶段的长篇历史

## 下一步

1. 若要继续该主题，先读 active tracking，再按需展开历史归档中的 warning 热点与验证记录
2. 下一轮优先继续 `GFramework.SourceGenerators.Tests` 的 `MA0051` 收口，并直接进入唯一剩余热点
   `CqrsHandlerRegistryGeneratorTests.cs`；但若用户真正关心“唯一变更文件数接近 `75`”，下一轮应改为选择新的文件写集，
   而不是继续停留在当前同一文件
3. 若改回推进 `MA0158`，先设计 `net8.0` / `net9.0` / `net10.0` 多 target 条件编译方案，不直接批量替换共享源码中的
   `object` lock
4. 若后续继续改动 `GFramework.Godot`，先修复该项目的 Linux 侧 restore 资产，再补跑独立 build
5. 若本主题确认暂缓，可保持当前归档状态，不需要再恢复 `local-plan/`
