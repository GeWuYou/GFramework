# CQRS 重写迁移追踪

## 2026-04-29

### 阶段：cached executor 上下文刷新回归（CQRS-REWRITE-RP-057）

- 延续 `gframework-batch-boot 50` 的 `Phase 8` 主线，本轮只处理一个窄写集测试批次：为 cached request pipeline executor 增加“重复分发仍重新注入上下文”的回归
- 先复核上一轮 request pipeline executor 形状缓存实现与测试边界后确认：
  - 当前 runtime 只允许本轮写集落在 `GFramework.Cqrs.Tests/Cqrs/`，除非测试直接打出 `CqrsDispatcher` 的真实缺陷
  - 目标是锁定 executor 缓存不会跨分发保留旧 `ArchitectureContext`，且不扩张到 notification / stream 路径
- 已完成的测试补强：
  - 在 `GFramework.Cqrs.Tests/Cqrs/` 新增 `DispatcherPipelineContextRefreshRequest`、`DispatcherPipelineContextRefreshBehavior`、`DispatcherPipelineContextRefreshRequestHandler`、`DispatcherPipelineContextRefreshState` 与 `DispatcherPipelineContextSnapshot`
  - `DispatcherPipelineContextRefreshBehavior` 与 `DispatcherPipelineContextRefreshRequestHandler` 都基于 `CqrsContextAwareHandlerBase` 记录当次看到的 `ArchitectureContext`
  - `CqrsDispatcherCacheTests` 新增 `Dispatcher_Should_Reinject_Current_Context_When_Reusing_Cached_Request_Pipeline_Executor`，断言同一个 cached executor 在两次分发间保持 executor 形状复用，但 handler 不会被 executor 黏住，且 handler / behavior 都会观察到本次分发的新上下文
- 调试过程中的结论：
  - 初版断言曾要求 behavior 实例编号跨分发变化，随后确认这是错误假设
  - `MicrosoftDiContainer.RegisterCqrsPipelineBehavior<TBehavior>()` 对已闭合的 pipeline behavior 使用的是 `AddSingleton(...)`
  - 因此本轮最终锁定的是“singleton behavior 也必须重新注入上下文”，而不是强行要求 behavior 生命周期为 transient
- 定向验证已通过：
  - `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsDispatcherCacheTests"`
  - `5/5` passed
- 结果：
  - 本轮未暴露新的 runtime 实现缺口，因此没有改动 `GFramework.Cqrs/Internal/CqrsDispatcher.cs`
  - 当前分支相对 `origin/main` 的累计提交 diff 仍为 `14 files`，继续低于 `gframework-batch-boot 50` 的主要 stop condition

### 阶段：pointer runtime-reconstruction 残留清理（CQRS-REWRITE-RP-056）

- 延续 `gframework-batch-boot 50` 的 `Phase 8` 主线，本轮只处理一个写集很窄的 generator 清理切片：删除 `CqrsHandlerRegistryGenerator` 里已经不可达的 pointer runtime-reconstruction 残留
- 先复核当前实现后确认：
  - `TryCreateRuntimeTypeReference` 已在入口直接拒绝 `IPointerTypeSymbol` 与 `IFunctionPointerTypeSymbol`
  - `CanReferenceFromGeneratedRegistry` 也已统一把 pointer / function pointer 判定为不可直接引用
  - 但 `RuntimeTypeReferenceSpec`、`AppendRuntimeTypeReferenceResolution(...)` 和 `ContainsExternalAssemblyTypeLookup(...)` 仍残留 pointer 子结构与 `MakePointerType()` 分支，属于已失效的死代码
- 已完成的清理：
  - `GFramework.Cqrs.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.Models.cs` 已移除 `PointerElementTypeReference`
  - `GFramework.Cqrs.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.SourceEmission.cs` 已移除 pointer 运行时重建分支与 `AppendPointerRuntimeTypeReferenceResolution(...)`
  - `GFramework.Cqrs.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.RuntimeTypeReferences.cs` 已移除 pointer 外部程序集查找递归
  - direct / named / mixed fallback 逻辑未改动，pointer / function pointer 拒绝语义保持不变
- 定向验证已通过：
  - `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release`
  - `0 warning / 0 error`
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests"`
  - `22/22` passed

### 阶段：缓存工厂闭包收敛（CQRS-REWRITE-RP-055）

- 延续 `gframework-batch-boot 50` 的 `Phase 8` 主线，本轮在不扩大语义面的前提下继续做一个更窄的 runtime 微切片：把弱缓存 / 并发缓存入口剩余的捕获型工厂收敛为 `static lambda + state`
- 先复核当前 runtime 热点后确认：
  - `CqrsDispatcher` 的 notification / stream / request binding 与 pipeline executor 缓存仍存在少量可消除的捕获型工厂
  - `CqrsHandlerRegistrar` 的程序集元数据缓存与可加载类型缓存也仍通过捕获 `logger` 的 lambda 建值
  - 这些入口都只影响内部缓存建值，不触碰 handler / behavior 生命周期和 fallback 合同
- 已完成的收敛：
  - `CqrsDispatcher` 现为 notification / stream / request binding 命中路径改用无捕获工厂；pipeline executor 缓存改为显式状态对象承载 `requestType`
  - `CqrsHandlerRegistrar` 现为 `AssemblyMetadataCache` 与 `LoadableTypesCache` 改用 `static` 工厂 + `logger` 显式状态参数
  - 该批次没有改动 `RequestPipelineInvocation` 的 `next` 语义，也没有缓存 handler / behavior 实例
- 同轮继续补了一个独立 generator 覆盖缺口：
  - `GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs` 新增“外部程序集隐藏泛型定义 + 可见类型实参”的 precise registration 回归
  - 该回归锁定生成器会输出 `ResolveReferencedAssemblyType("...ProtectedEnvelope\`1")` 与 `MakeGenericType(typeof(string))` 的组合，而不是退回程序集级字符串 fallback
- 定向验证已通过：
  - `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~RegisterHandlers_Should_Cache_Assembly_Metadata_Across_Containers|FullyQualifiedName~RegisterHandlers_Should_Cache_Loadable_Types_Across_Containers|FullyQualifiedName~Dispatcher_Should_Cache_Request_Pipeline_Executors_Per_Behavior_Count"`
  - `3/3` passed
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests"`
  - `22/22` passed
  - `dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release`
  - `0 warning / 0 error`

### 阶段：低风险并行批次收口（CQRS-REWRITE-RP-054）

- 继续按 `gframework-batch-boot 50` 推进 `Phase 8`，本轮先完成批次评估后再并行拆分写集，避免把 generator、runtime 与 docs 改动揉进同一片上下文
- 先复核当前 worktree、active tracking 与 `origin/main` 基线后确认：
  - 当前分支头最初与 `origin/main` 对齐，批次阈值从 `0 files / 0 lines` 起算
  - 本轮可以安全拆成三个互不冲突的切片：request pipeline executor 形状缓存、precise runtime type lookup 数组回归补强、CQRS 入口文档对齐
  - 主线程保留集成与验证职责，subagent 只负责各自写集
- 本轮继续收口一个更窄的 generator 覆盖缺口：
  - 在 `GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs` 新增“外部程序集隐藏泛型定义 + 可见类型实参”的 precise registration 回归
  - 该回归锁定生成器会输出 `ResolveReferencedAssemblyType("...ProtectedEnvelope\`1")` 与 `MakeGenericType(typeof(string))` 的组合，而不是退回程序集级字符串 fallback
  - 定向测试 `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests"` 通过，结果为 `22/22` passed，因此本轮未触发 `RuntimeTypeReferences` / `SourceEmission` 的实现修正
- 已接受并整合的并行写集：
  - docs 切片：更新 `GFramework.Cqrs/README.md`、`docs/zh-CN/core/cqrs.md`、`docs/zh-CN/api-reference/index.md`，明确 generated registry 优先、targeted fallback 只补剩余 handler
  - generator 切片：在 `GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs` 新增多维数组、交错数组、外部程序集隐藏元素类型三组 precise lookup 回归
  - dispatcher 切片：在 `GFramework.Cqrs/Internal/CqrsDispatcher.cs` 中将 request pipeline 从“每次分发重建 next 链”收敛为“binding 内按 behaviorCount 缓存 executor 形状”，并补充 dispatcher cache / 顺序回归
- docs 切片已作为独立提交落地：
  - `66830ba2` `docs(cqrs): 更新入口与回退语义说明`
- 本轮定向验证已通过：
  - `dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release`
  - `0 warning / 0 error`
  - `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsDispatcherCacheTests"`
  - `4/4` passed
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests"`
  - `21/21` passed
- 本轮停止时，当前工作区相对 `origin/main` 的累计 diff 为 `13 files / 709 lines`
- 结论：
  - primary stop condition `50 files` 尚未触发，本轮停止是因为三条低风险切片已收口完毕
  - 下一批更适合重新做一轮热点筛选，而不是在同一轮继续扩写集

### 阶段：precise runtime type lookup 数组回归补强（CQRS-REWRITE-RP-053）

- 延续 `gframework-batch-boot 50` 的 `Phase 8` 主线，本轮选择一个更窄的 generator 覆盖缺口：锁定 precise runtime type lookup 下数组类型形态的回归
- 先复核当前实现后确认：
  - `TryCreateRuntimeTypeReference` 已会把 `IArrayTypeSymbol` 递归建模为 `RuntimeTypeReferenceSpec.FromArray(element, rank)`
  - `AppendArrayRuntimeTypeReferenceResolution` 已按 `ArrayRank == 1` 发射 `MakeArrayType()`，按 `rank > 1` 发射 `MakeArrayType(rank)`
  - 当前缺口主要是测试面不足，尚未显式覆盖多维数组、交错数组、外部程序集隐藏元素类型这三类 precise lookup 场景
- 已在 `GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs` 补充三组回归：
  - 隐藏元素类型的多维数组响应，锁定 `MakeArrayType(2)` 发射
  - 隐藏元素类型的交错数组响应，锁定递归 `MakeArrayType().MakeArrayType()` 发射
  - 外部程序集隐藏元素类型的多维数组响应，锁定 `ResolveReferencedAssemblyType(...)` 与 `MakeArrayType(2)` 的组合
- 本轮定向测试全部通过，未暴露数组发射缺陷：
  - 因此没有修改 `GFramework.Cqrs.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.SourceEmission.cs`
  - 也没有改动 `CqrsHandlerRegistryGenerator.RuntimeTypeReferences.cs`
  - fallback 合同选择逻辑与 direct / named / mixed fallback 排版路径保持不变
- 定向验证已通过：
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests"`
  - `21/21` passed

### 阶段：mixed fallback 元数据拆分（CQRS-REWRITE-RP-052）

- 延续 `gframework-batch-boot 50` 的 `Phase 8` 主线，本轮把上一批的“全部可直接引用 fallback handlers 走 `Type[]`”继续推进到 mixed 场景
- 先复核现状后确认：
  - `CqrsHandlerRegistrar` 已天然支持读取多个 `CqrsReflectionFallbackAttribute` 实例
  - 上一批真正阻止 mixed 场景继续收敛的点，是 runtime attribute 本身尚未开放多实例，以及 generator 只能二选一发射单个 fallback 特性
- 已在 `GFramework.Cqrs/CqrsReflectionFallbackAttribute.cs` 中将特性约束改为 `AllowMultiple = true`，并补充注释说明多个实例的用途
- 已在 `GFramework.Cqrs.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 中扩展 fallback 合同探测：
  - 探测 runtime 是否支持 `params string[]`
  - 探测 runtime 是否支持 `params Type[]`
  - 探测 runtime 是否允许多个 `CqrsReflectionFallbackAttribute` 实例
- 已在 `CqrsHandlerRegistryGenerator.Models.cs` 与 `CqrsHandlerRegistryGenerator.SourceEmission.cs` 中重构 fallback 发射模型：
  - fallback 元数据现在可表示为一个或多个程序集级特性实例
  - 当 fallback handlers 全部可直接引用时，继续优先输出单个 `Type[]` 特性
  - 当 fallback 同时包含可直接引用与仅能按名称恢复的 handlers，且 runtime 支持多实例时，拆分输出一条 `Type[]` 特性和一条字符串特性
  - 若 runtime 不支持多实例或缺少相应构造函数，仍整体回退到字符串元数据，避免 mixed 场景漏注册
- 已补充 runtime 与 generator 双侧回归：
  - `GFramework.Cqrs.Tests/Cqrs/CqrsHandlerRegistrarTests.cs` 新增 mixed fallback metadata 用例，锁定 registrar 只对字符串条目调用一次 `Assembly.GetType(...)`
  - `GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs` 新增 mixed fallback emission 用例，锁定 generator 会输出两个程序集级 fallback 特性实例
- 同步更新：
  - `GFramework.Cqrs.SourceGenerators/README.md`
  - `docs/zh-CN/source-generators/cqrs-handler-registry-generator.md`
  - 说明 mixed 场景现在会拆分 `Type` 元数据与字符串元数据
- 定向验证已通过：
  - `dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release`
  - `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release`
  - `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsHandlerRegistrarTests"`
  - `13/13` passed
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests"`
  - `18/18` passed
- 随后按 `$gframework-pr-review` 重新拉取当前分支 PR 审查数据：
  - 当前 worktree `feat/cqrs-optimization` 已对应 `PR #302`
  - latest head commit 仍有 `3` 条 open AI review threads：Greptile 指向 generator preamble 的死参数与多实例 fallback 特性空行，CodeRabbit 指向 mixed/direct fallback 测试断言过宽
  - MegaLinter 仍只暴露 `dotnet-format` 的 `Restore operation failed`，未给出本地仍成立的格式文件线索，因此按环境噪音处理
- 本轮已继续收口 `RP-052` 的 follow-up：
  - 在 `GFramework.Cqrs.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.SourceEmission.cs` 中移除已不再参与判断的 `generationEnvironment` 透传参数
  - 调整多实例 fallback 特性发射时的换行策略，避免最后一个 fallback 特性与 `CqrsHandlerRegistryAttribute` 之间保留多余空行
  - 在 `GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs` 中补强 direct/mixed fallback 发射断言，锁定特性实例个数、拒绝空 marker，并确保 mixed 场景的程序集级 preamble 排版稳定
  - 在 `GFramework.Cqrs.Tests/Cqrs/ReflectionFallbackNotificationContainer.cs` 中为 `DirectFallbackHandlerType` 补齐 `<returns>` XML 文档
- 本轮 review follow-up 验证已通过：
  - `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release`
  - `0 warning / 0 error`
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests"`
  - `18/18` passed
  - `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsHandlerRegistrarTests"`
  - `13/13` passed

## 2026-04-20

### 阶段：direct fallback 元数据优先级收敛（CQRS-REWRITE-RP-051）

- 重新按 `gframework-batch-boot 50` 恢复 `Phase 8` 后，先复核当前 worktree 的恢复入口、`origin/main` 基线与分支规模：
  - worktree 仍映射到 `cqrs-rewrite`
  - 基线按批处理约定固定为 `origin/main`
  - 本轮开始前分支累计 diff 为 `0 files / 0 lines`
- 结合当前代码热点与历史归档后，选择本轮批次目标为“继续收敛 generator fallback 元数据，进一步减少 runtime 按字符串类型名回查 handler 的场景”
- 已在 `GFramework.Cqrs.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 中新增 runtime fallback 合同探测：
  - 识别 `CqrsReflectionFallbackAttribute` 是否支持 `params string[]`
  - 识别 `CqrsReflectionFallbackAttribute` 是否支持 `params Type[]`
- 已在 `GFramework.Cqrs.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.Models.cs` 与
  `CqrsHandlerRegistryGenerator.SourceEmission.cs` 中收敛 fallback 发射策略：
  - 当本轮所有 fallback handlers 都可被生成代码直接引用，且 runtime 支持 `params Type[]` 时，生成器现优先发射 `typeof(...)` 形式的程序集级 fallback 元数据
  - 当 fallback handlers 中仍存在不能直接引用的实现类型时，生成器继续整体回退到字符串元数据，避免 mixed 场景下部分 handler 走 `Type[]`、其余 handler 丢失恢复入口
- 已在 `GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs` 补充回归：
  - 锁定 runtime 同时暴露字符串与 `Type` 两类 fallback 构造函数时，生成器优先选择直接 `Type` 元数据
  - 保留现有字符串 fallback 合同测试，确保旧 contract 兼容路径不回退
- 同步更新：
  - `GFramework.Cqrs.SourceGenerators/README.md`
  - `docs/zh-CN/source-generators/cqrs-handler-registry-generator.md`
  - 说明“可直接引用的 fallback handlers 会优先走 `typeof(...)` 元数据，减少运行时字符串回查”
- 定向验证已通过：
  - `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release`
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests"`
  - `17/17` passed
- 额外修正：
  - active tracking 中原先引用的 `ai-plan/migration/CQRS_MODULE_SPLIT_PLAN.md` 在当前 worktree 已不存在；本轮已移除该失效路径，后续以 active tracking / trace 作为默认恢复入口

### 阶段：pointer / function pointer 泛型合同拒绝（CQRS-REWRITE-RP-050）

- 重新执行 `$gframework-pr-review` 后，确认当前分支对应 `PR #261`，状态仍为 `OPEN`
- latest reviewed commit 当前剩余 `1` 条 open CodeRabbit thread，指向 `RP-047` 历史记录仍把 `MakePointerType()` precise registration 写成现行路径
- 本地核对后确认该评论有效：当前 pointer / function pointer 语义已由 `RP-050` 收敛为 fallback / diagnostic 路径，历史追踪必须显式标注 `RP-047` 已废弃，避免后续恢复时误回滚到旧方案
- 已在 `GFramework.Cqrs.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 中收紧 `TryCreateRuntimeTypeReference` 与 `CanReferenceFromGeneratedRegistry`
- pointer / function pointer 现统一视为不可精确生成的 CQRS 泛型合同，生成器会保守回退到既有 fallback / diagnostic 路径，而不再发射运行时 `MakeGenericType(...)` 风险代码
- 已在 `GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs` 中补充输入源诊断分离，并将相关测试改为显式断言 `CS0306` 与 fallback / diagnostic 结果
- 已同步修正 `ai-plan/public/cqrs-rewrite/traces/cqrs-rewrite-migration-trace.md` 中 `RP-047` 段落，明确其已被 `RP-050` 覆盖，且不得恢复 `MakePointerType()` precise registration
- 定向验证已通过：
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore -p:RestoreFallbackFolders= -m:1 -nodeReuse:false --filter "FullyQualifiedName~Reports_Compilation_Error_And_Skips_Precise_Registration_For_Hidden_Pointer_Response|FullyQualifiedName~Reports_Diagnostic_And_Skips_Registry_When_Fallback_Metadata_Is_Required_But_Runtime_Contract_Lacks_Fallback_Attribute|FullyQualifiedName~Emits_Assembly_Level_Fallback_Metadata_When_Fallback_Is_Required_And_Runtime_Contract_Is_Available"`
  - `3/3` passed
- 扩展验证已通过：
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore -p:RestoreFallbackFolders= -m:1 -nodeReuse:false --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests"`
  - `14/14` passed

### 阶段：registrar duplicate mapping 索引收敛（CQRS-REWRITE-RP-049）

- 已将 `CqrsHandlerRegistrar` 的重复 handler mapping 判定从逐条线性扫描 `IServiceCollection` 收敛为单次构建的本地映射索引
- reflection fallback 或重复类型输入场景下，后续 duplicate mapping 判定改为 `HashSet` 命中，不再重复遍历已有服务描述符
- `GFramework.Cqrs.Tests/Cqrs/CqrsHandlerRegistrarTests.cs` 已补充“程序集枚举返回重复 handler 类型时仍只注册一份映射”的回归
- 定向验证已通过：
  - `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --no-restore -p:RestoreFallbackFolders= -m:1 -nodeReuse:false --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsHandlerRegistrarTests"`
  - `11/11` passed
  - 当前沙箱限制 MSBuild named pipe，因此验证在提权环境下执行

### 阶段：registrar handler-interface 反射缓存（CQRS-REWRITE-RP-048）

- 已在 `CqrsHandlerRegistrar` 中新增按 `Type` 弱键缓存的 supported handler interface 元数据，reflection 注册路径现会复用已筛选且排序好的接口列表
- 同一 handler 类型跨容器重复注册时，不再重复执行 `GetInterfaces()` 与支持接口筛选；缓存仍保持卸载安全，不会长期钉住 collectible 类型
- `GFramework.Cqrs.Tests/Cqrs/CqrsHandlerRegistrarTests.cs` 已补充 registrar 静态缓存清理与 supported interface 缓存复用回归
- 定向验证已通过：
  - `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --no-restore -p:RestoreFallbackFolders= -m:1 -nodeReuse:false --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsHandlerRegistrarTests"`
  - `10/10` passed
  - 当前沙箱限制 MSBuild named pipe，因此验证在提权环境下执行

### 阶段：pointer precise runtime type 覆盖扩展（CQRS-REWRITE-RP-047，已由 RP-050 覆盖）

- 曾在 `CqrsHandlerRegistryGenerator` 中尝试补充 pointer 类型的 runtime type 递归建模与源码发射，计划通过 `MakePointerType()` 还原隐藏 pointer 响应类型
- 该方案后续已被 `RP-050` 明确废弃：pointer / function pointer 不能作为 CQRS 泛型合同的 precise registration 输入，当前实现统一回到 fallback / diagnostic 路径，不能恢复到 `MakePointerType()` 精确注册
- 已同步收紧 function pointer 签名的可直接生成判定，只有当签名中的返回值与参数类型均可从 generated registry 安全引用时才走静态注册
- 已保留含隐藏类型 function pointer handler 的 fallback / 诊断回归覆盖，确保 pointer 支持扩展不会误删原有程序集级 fallback 契约边界
- 后续若需恢复当前 pointer / function pointer 行为，应以 `RP-050` 为权威记录，而不是继续沿用本阶段的旧设计假设
- 定向验证与 `CqrsHandlerRegistryGeneratorTests` 全组验证均已通过：
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore -p:RestoreFallbackFolders= -m:1 -nodeReuse:false --filter "FullyQualifiedName~Generates_Precise_Service_Type_For_Hidden_Pointer_Response|FullyQualifiedName~Reports_Diagnostic_And_Skips_Registry_When_Fallback_Metadata_Is_Required_But_Runtime_Contract_Lacks_Fallback_Attribute|FullyQualifiedName~Emits_Assembly_Level_Fallback_Metadata_When_Fallback_Is_Required_And_Runtime_Contract_Is_Available"`
  - `3/3` passed
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore -p:RestoreFallbackFolders= -m:1 -nodeReuse:false --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests"`
  - `14/14` passed
  - 当前沙箱限制 MSBuild named pipe，因此验证在提权环境下执行

### 阶段：generated registry 激活反射收敛（CQRS-REWRITE-RP-046）

- 已在 `CqrsHandlerRegistrar` 中将 generated registry 的无参构造激活改为类型级缓存工厂
- 默认路径优先使用一次性动态方法直接创建 registry，避免后续每次命中缓存仍走 `ConstructorInfo.Invoke`
- 若运行环境不允许动态方法，则保留原有反射激活回退，确保 generated registry 路径不因运行时限制失效
- 已补充“私有无参构造 generated registry 仍可激活”的回归测试，覆盖现有生成器产物兼容性
- 定向验证已通过：
  - `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --no-restore -p:RestoreFallbackFolders= -m:1 -nodeReuse:false`
  - `63/63` passed
  - 当前沙箱限制 MSBuild named pipe，因此验证在提权环境下执行

### Archive Context

- 历史跟踪归档：
  - `ai-plan/public/cqrs-rewrite/archive/todos/cqrs-rewrite-history-through-rp043.md`
- 历史 trace 归档：
  - `ai-plan/public/cqrs-rewrite/archive/traces/cqrs-rewrite-history-through-rp043.md`

### 当前下一步

1. 回到 `Phase 8` 主线，优先再找一个写集独立的 generator 或 runtime 热点；pointer runtime-reconstruction 残留已清空，后续不要恢复任何 `MakePointerType()` 发射路径
2. 若继续文档主线，优先补齐 `docs/zh-CN/api-reference` 与教程入口页中仍过时的 CQRS API / 命名空间表述
3. 若后续 review thread 或 PR 状态再次变化，再重新执行 `$gframework-pr-review` 复核远端信号
