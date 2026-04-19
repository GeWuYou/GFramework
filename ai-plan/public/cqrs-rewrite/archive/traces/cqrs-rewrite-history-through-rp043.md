# CQRS 重写迁移追踪

## 2026-04-14

### 阶段：初始化

- 建立 `CQRS-REWRITE-RP-001` 恢复点
- 已确认本次迁移目标：
  - 彻底参考 `Mediator` 思路重写 GFramework 正式 CQRS
  - 不保留对 `Mediator` 的兼容层
  - 使用 `abstractions + runtime 可选模块` 边界
  - 保留 `EventBus`，不与 CQRS notification 合并

### 已确认的实现前提

- `CoreGrid-Migration` 当前仍依赖 NuGet 版 `GeWuYou.GFramework*`
- `CoreGrid/scripts/core/GameArchitecture.cs` 与 `CoreGrid-Migration/scripts/core/GameArchitecture.cs` 通过 `AddMediator(...)` 启用基于生成器的 runtime
- `GFramework` 当前 `IArchitectureContext` 与一批 CQRS 基类直接引用 `Mediator.*`
- `CoreGrid/scripts/cqrs/**` 的 handler 很薄，主要迁移成本在框架 runtime 和注册机制，不在业务逻辑本身

### 当前动作

- 准备更新 `AGENTS.md`，补充恢复点 / trace / subagent 协作规范
- 准备将 `CoreGrid-Migration` 切换为本地项目引用，建立真实验证链路

### 下一步

1. 完成 `AGENTS.md` 规则补充
2. 改造 `CoreGrid-Migration/CoreGrid.csproj` 为本地项目与本地生成器引用
3. 进行第一次构建验证，确认本地链路可用

### 阶段：CQRS 主路径迁移完成

- `CoreGrid-Migration/CoreGrid.csproj` 已切到本地 `ProjectReference` + 本地 source generators
- `CoreGrid-Migration/scripts/core/GameArchitecture.cs` 已删除 `AddMediator(...)` 配置钩子
- `GFramework.Core.Abstractions` 新增 GFramework 自有 CQRS 契约与 `Unit`
- `IArchitectureContext` / `ArchitectureContext` 已切到自有 CQRS 签名
- `ArchitectureBootstrapper` 已内建 handler 扫描注册，使用方无需再显式调用 `AddMediator(...)`
- `CqrsDispatcher` 已补齐 request/notification/stream dispatch 与 pipeline behavior 执行
- `GFramework.Core.Cqrs.*` 基类、`ContextAwareMediator*Extensions`、Godot 协程上下文扩展均已迁到新契约
- `GFramework.Core.Tests` 中原依赖旧 `Mediator` 注册入口的测试已迁移到 `CqrsTestRuntime` 反射注册路径

### 阶段：验证

- `dotnet build GFramework.Core/GFramework.Core.csproj`
  - 结果：通过
- `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj`
  - 结果：通过
- `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj --no-build`
  - 结果：通过
  - 明细：`1621` 个测试全部通过
- `dotnet build GFramework.sln`
  - 结果：通过
- `dotnet build ../CoreGrid-Migration/CoreGrid.sln`
  - 结果：通过
  - 备注：仅存在既有 analyzer warnings，无新增构建错误

### 阶段：CQRS 收尾修正

- 建立 `CQRS-REWRITE-RP-003` 恢复点
- 修正 `IArchitectureContext`、`QueryBase`、`Abstract*Handler` 与 `MessageHandlerDelegate` 的 XML 文档，明确旧
  Command/Query 总线与新 CQRS runtime 的兼容边界
- `CqrsHandlerRegistrar` 改为按程序集名、处理器类型名与处理器接口名稳定排序
- `CqrsHandlerRegistrar` 在 `ReflectionTypeLoadException` 场景下会记录告警并保留可加载类型继续注册
- 自动扫描到的 request / notification / stream handler 改为 transient，避免 `ContextAwareBase` 派生处理器在并发请求间共享可变上下文
- `CqrsTestRuntime` 移除自建 pipeline 执行逻辑，测试改为走 `ArchitectureContext.SendRequestAsync(...)` 正式入口
- `MediatorAdvancedFeaturesTests` 为断路器静态状态补上统一重置，消除测试间污染

### 阶段：补充验证

-
`dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Core.Tests.Cqrs.CqrsHandlerRegistrarTests|FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureModulesBehaviorTests|FullyQualifiedName~GFramework.Core.Tests.Mediator.MediatorAdvancedFeaturesTests|FullyQualifiedName~GFramework.Core.Tests.Mediator.MediatorArchitectureIntegrationTests|FullyQualifiedName~GFramework.Core.Tests.Mediator.MediatorComprehensiveTests"`
    - 结果：通过
    - 明细：`49` 个测试全部通过

### 阶段：review 收尾修正

- 建立 `CQRS-REWRITE-RP-004` 恢复点
- `AbstractStreamCommandHandler<TCommand, TResponse>` 已把上下文注入窗口、瞬态实例约束与流创建/枚举取消边界移入显式
  `<remarks>`
- `CqrsHandlerRegistrarTests` 已改为从 `CqrsTestRuntime.RegisterHandlers(...)` 真实入口覆盖部分类型加载失败场景，不再反射
  `RecoverLoadableTypes(...)`
- `CqrsTestRuntime` 已补齐 XML 文档，并改为按 `IIocContainer + IEnumerable<Assembly> + ILogger` 精确绑定
  `RegisterHandlers(...)` 反射签名，避免未来重载漂移
- 定向验证期间发现 `CqrsHandlerRegistrar.cs` 缺少 `Microsoft.Extensions.DependencyInjection` 的 `using`，导致
  `IServiceCollection` 无法编译；该编译阻塞已一并修复

### 阶段：review 修正验证

- `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Core.Tests.Cqrs.CqrsHandlerRegistrarTests|FullyQualifiedName~GFramework.Core.Tests.Mediator.MediatorComprehensiveTests|FullyQualifiedName~GFramework.Core.Tests.Mediator.MediatorArchitectureIntegrationTests|FullyQualifiedName~GFramework.Core.Tests.Mediator.MediatorAdvancedFeaturesTests"`
  - 结果：通过
  - 明细：`47` 个测试全部通过

### 阶段：去外部依赖收尾

- 建立 `CQRS-REWRITE-RP-005` 恢复点
- `GFramework.Core.Abstractions` 已移除 `Mediator.Abstractions` 包引用，并显式改依赖
  `Microsoft.Extensions.DependencyInjection.Abstractions` 以承接 `IServiceCollection` / `IServiceScope`
- `GFramework.Core.Tests` 已移除 `Mediator.Abstractions` 与 `Mediator.SourceGenerator` 包引用
- `MediatorCoroutineExtensions` 已改为直接走 `IArchitectureContext.SendAsync(...)` 内建 CQRS 入口，不再解析
  `IMediator`
- `Architecture` / `ArchitectureModules` / `IIocContainer` / `MicrosoftDiContainer` / `ArchitectureBootstrapper`
  与主文档已补充“历史命名保留，但 runtime 已为内建 CQRS”说明
- 并行 subagent 盘点确认：生产代码与主文档中剩余的 `Mediator` 主要是历史兼容命名，不再对应任何外部包依赖

### 阶段：去外部依赖验证

- `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
  - 结果：通过
- `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-build`
  - 结果：通过
  - 明细：`1624` 个测试全部通过
- `dotnet build ../CoreGrid-Migration/CoreGrid.sln`
  - 结果：通过
  - 备注：仅存在既有 analyzer warnings 与 `GodotProjectDir` 缺失导致的 generator warning，无新增构建错误
- `dotnet build GFramework.sln -c Release`
  - 结果：失败
  - 原因：当前 WSL 环境下顶层 `GFramework.csproj` 仍引用 Windows NuGet fallback package folder
    `D:\Tool\Development Tools\Microsoft Visual Studio\Shared\NuGetPackages`
  - 结论：属于既有环境配置问题，与本轮 CQRS 去 `Mediator` 改动无关

### 阶段：历史命名中性化

- 建立 `CQRS-REWRITE-RP-006` 恢复点
- `IArchitecture` / `IIocContainer` / `Architecture` / `MicrosoftDiContainer` 已新增
  `RegisterCqrsPipelineBehavior<TBehavior>()` 推荐入口
- 旧的 `RegisterMediatorBehavior<TBehavior>()` 已降级为 `[Obsolete]` 兼容包装层，并转发到新的 CQRS 中性命名
- 新增 `ContextAwareCqrsExtensions`、`ContextAwareCqrsCommandExtensions`、`ContextAwareCqrsQueryExtensions`
  与 `CqrsCoroutineExtensions`
- 旧的 `ContextAwareMediator*Extensions` 与 `MediatorCoroutineExtensions` 已改为 `[Obsolete]` 兼容包装层
- 新的 `ContextAwareCqrs*` / `CqrsCoroutineExtensions` 已放入独立命名空间，避免与旧扩展同时导入时产生调用歧义
- `ArchitectureModulesBehaviorTests`、`CqrsCoroutineExtensionsTests`、`docs/zh-CN/core/cqrs.md`、
  `docs/zh-CN/core/index.md` 与 `CoreGrid-Migration` 命中的旧扩展调用点已迁到新的中性命名入口

### 阶段：历史命名中性化验证

- `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
  - 结果：通过
- `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-build`
  - 结果：通过
  - 明细：`1624` 个测试全部通过
- `dotnet build ../CoreGrid-Migration/CoreGrid.sln`
  - 结果：通过
  - 备注：消除了新增扩展类带来的调用歧义，仅剩既有 analyzer warnings

### 阶段：兼容层正式弃用

- 建立 `CQRS-REWRITE-RP-007` 恢复点
- `IArchitecture`、`IIocContainer`、`Architecture`、`ArchitectureModules` 与 `MicrosoftDiContainer`
  上剩余的 `RegisterMediatorBehavior<TBehavior>()` 已统一补充 future-major 移除说明
- `ContextAwareMediatorExtensions`、`ContextAwareMediatorCommandExtensions`、
  `ContextAwareMediatorQueryExtensions` 与 `MediatorCoroutineExtensions`
  已统一补充 future-major 移除说明
- 上述历史兼容入口已统一加上 `EditorBrowsable(EditorBrowsableState.Never)`，从 IntelliSense 主路径隐藏，
  将迁移默认路径进一步收敛到新的 `Cqrs` 命名入口
- `docs/zh-CN/core/cqrs.md` 与 `CLAUDE.md` 已同步记录“兼容别名进入正式弃用周期”的事实
- `MediatorCompatibilityDeprecationTests` 已新增并通过反射断言锁定：
  - 公开兼容方法保留行为兼容，但必须带有 `Obsolete`
  - 公开兼容方法与兼容扩展类型必须带有 `EditorBrowsable(EditorBrowsableState.Never)`
  - 弃用消息必须明确新的 CQRS 迁移目标与 future-major 移除预期

### 阶段：兼容层正式弃用验证

- `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
  - 结果：通过
- `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~GFramework.Core.Tests.Cqrs.MediatorCompatibilityDeprecationTests|FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureModulesBehaviorTests|FullyQualifiedName~GFramework.Core.Tests.Coroutine.CqrsCoroutineExtensionsTests"`
  - 结果：通过
  - 明细：`8` 个测试全部通过

### 阶段：CQRS source-generator 注册 MVP

- 建立 `CQRS-REWRITE-RP-008` 恢复点
- `GFramework.Core.Abstractions` 已新增：
  - `ICqrsHandlerRegistry`
  - `CqrsHandlerRegistryAttribute`
- `CqrsHandlerRegistrar` 已优先尝试读取程序集级 `CqrsHandlerRegistryAttribute`
  指向的生成注册器；当生成注册器缺失、类型无效或实例化失败时，会记录 warning 并自动回退到原有反射扫描路径
- `GFramework.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 已新增：
  - 为当前消费端程序集中的 concrete request / notification / stream handlers 生成单一程序集级注册器
  - 生成注册顺序与 runtime 反射口径对齐，按实现类型与处理器接口名稳定排序
  - 当程序集包含生成代码无法合法引用的 concrete handler（例如私有嵌套 handler）时，直接放弃生成，让 runtime 保持完整反射回退
- `docs/zh-CN/core/cqrs.md` 与 `CLAUDE.md` 已同步记录“生成注册器优先，反射扫描兜底”的当前行为

### 阶段：CQRS source-generator 注册验证

- `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release`
  - 结果：通过
- `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
  - 结果：通过
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~GFramework.SourceGenerators.Tests.Cqrs.CqrsHandlerRegistryGeneratorTests"`
  - 结果：通过
  - 明细：`2` 个测试全部通过
- `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~GFramework.Core.Tests.Cqrs.CqrsHandlerRegistrarTests|FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureModulesBehaviorTests|FullyQualifiedName~GFramework.Core.Tests.Mediator.MediatorArchitectureIntegrationTests|FullyQualifiedName~GFramework.Core.Tests.Mediator.MediatorComprehensiveTests"`
  - 结果：通过
  - 明细：`41` 个测试全部通过

### 阶段：review 收尾修正（并发 lazy 初始化与共享测试运行时）

- `ArchitectureContext` 的 `ICqrsRuntime` 惰性解析已从 `??=` 改为 `Lazy<ICqrsRuntime>`，
  并显式指定 `LazyThreadSafetyMode.ExecutionAndPublication`
- 新增 `ArchitectureContextTests.SendRequestAsync_Should_ResolveCqrsRuntime_OnlyOnce_When_AccessedConcurrently`
  以回归锁定并发首次访问行为
- 两个测试项目中的 `CqrsTestRuntime` 重复实现已收敛为单一共享源码文件 `tests/Shared/CqrsTestRuntime.cs`
- 共享 `CqrsTestRuntime` 已移除 `GetType(..., throwOnError: true)` 后不可达的 `?? throw` 分支

### 阶段：review 收尾修正验证

- `dotnet test GFramework/GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsHandlerRegistrarTests|FullyQualifiedName~GFramework.Cqrs.Tests.Mediator.MediatorComprehensiveTests|FullyQualifiedName~GFramework.Cqrs.Tests.Mediator.MediatorArchitectureIntegrationTests|FullyQualifiedName~GFramework.Cqrs.Tests.Mediator.MediatorAdvancedFeaturesTests"`
  - 结果：通过
  - 明细：`49` 个测试全部通过
- `dotnet test GFramework/GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureContextTests|FullyQualifiedName~GFramework.Core.Tests.Ioc.MicrosoftDiContainerTests"`
  - 结果：通过
  - 明细：`57` 个测试全部通过
- 并行执行两条 `dotnet test` 时命中过一次 `project.assets.json` 文件竞争；
  顺序重跑 `GFramework.Core.Tests --no-restore` 后通过，确认是 restore 并发噪音而非实现缺陷

### 阶段：测试公共基础设施模块化

- 已将临时共享源码目录 `tests/Shared` 收敛为正式项目 `GFramework.Tests.Common`
- `CqrsTestRuntime` 已迁入 `GFramework.Tests.Common` 并改为由测试项目通过 `ProjectReference` 访问

## 2026-04-16

### 阶段：review 收尾修正（线程安全文档、未冻结态去重与 runtime 契约说明）

- 建立 `CQRS-REWRITE-RP-020` 恢复点
- `GFramework.Cqrs/Internal/DefaultCqrsRegistrationService.cs` 已在类级 `<remarks>` 中明确“该类型不是线程安全的，必须由外部同步边界串行访问”的设计约束
- `GFramework.Cqrs.Abstractions/Cqrs/ICqrsRuntime.cs` 已补齐三个公开方法的 XML 契约：
  - `null` 参数对应 `ArgumentNullException`
  - handler 缺失或上下文不满足 `IArchitectureContext` 注入前提时对应 `InvalidOperationException`
  - `CreateStream(...)` 额外说明了上下文需覆盖整个异步枚举生命周期
- `GFramework.Cqrs/Internal/CqrsHandlerRegistrar.cs` 已补充线性去重扫描的性能特征注释，并记录未来若出现大服务集合热点，可在更高层批处理中引入 `HashSet<(Type, Type)>`
- `GFramework.Core/Ioc/MicrosoftDiContainer.cs` 已将未冻结态 `GetAll<T>()` / `GetAll(Type)` 的引用去重逻辑收窄为：
  - 仍会折叠“不同 `ServiceType` 指向同一实例”的兼容别名重复
  - 不再吞掉“同一 `ServiceType` 对同一实例的重复显式注册”
  - 当同一实例同时暴露多个服务类型时，优先保留请求类型对应分组，否则保留注册次数最多且首次出现最早的分组，以维持确定性
- `GFramework.Core.Tests/Ioc/MicrosoftDiContainerTests.cs` 已新增两个回归测试，分别锁定泛型与非泛型 `GetAll` 在未冻结态下的上述语义

### 阶段：RP-020 验证

- `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Core.Tests.Ioc.MicrosoftDiContainerTests"`
  - 结果：通过
  - 明细：`40` 个测试全部通过
- `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~GFramework.Core.Tests.Ioc.MicrosoftDiContainerTests"`
  - 结果：通过
  - 明细：`40` 个测试全部通过

### 下一步

1. 继续评估 `CqrsHandlerRegistrar` / generated registry 的命中率提升空间，优先减少需要 `GetTypes()` 全量扫描的回退场景
2. 若后续 `MicrosoftDiContainer` 继续保留“未冻结时支持按可赋值类型观察实例”的宽语义，可考虑为 mixed alias + duplicate registration 组合场景再补一条更细的回归测试

## 2026-04-16

### 阶段：review 收尾修正（CQRS 程序集枚举输入固定与前置校验）

- 建立 `CQRS-REWRITE-RP-021` 恢复点
- `GFramework.Core/Ioc/MicrosoftDiContainer.cs` 已将 `RegisterCqrsHandlersFromAssemblies(...)` 调整为：
  - 在进入容器写锁前先 `ToArray()` 固定输入枚举
  - 逐项执行 `ArgumentNullException.ThrowIfNull(...)`
  - 再把固定后的数组交给 `ICqrsRegistrationService`
- 该调整把 `null` 元素和依赖外部可变状态的延迟枚举拦截在容器入口处，避免更深层的反射/注册路径才暴露输入问题，也减少在写锁内枚举外部序列的非确定性
- `GFramework.Core.Tests/Ioc/MicrosoftDiContainerTests.cs` 已新增空程序集元素回归测试，锁定“先校验，再委托”的失败边界

### 阶段：RP-021 验证

- `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Core.Tests.Ioc.MicrosoftDiContainerTests"`
  - 结果：通过
  - 明细：`41` 个测试全部通过

### 下一步

1. 若后续 review 继续聚焦容器显式注册入口，可考虑补一条“延迟枚举只在入口处物化一次”的行为测试
- `GFramework.sln` 已纳入 `GFramework.Tests.Common`，测试公共基础设施不再悬空在 solution 外

### 阶段：测试公共基础设施模块化验证

- `dotnet test GFramework/GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureContextTests|FullyQualifiedName~GFramework.Core.Tests.Ioc.MicrosoftDiContainerTests"`
  - 结果：通过
  - 明细：`57` 个测试全部通过
- `dotnet test GFramework/GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsHandlerRegistrarTests|FullyQualifiedName~GFramework.Cqrs.Tests.Mediator.MediatorComprehensiveTests|FullyQualifiedName~GFramework.Cqrs.Tests.Mediator.MediatorArchitectureIntegrationTests|FullyQualifiedName~GFramework.Cqrs.Tests.Mediator.MediatorAdvancedFeaturesTests"`
  - 结果：通过
  - 明细：`49` 个测试全部通过

### 阶段：日志入口下沉与剩余 runtime behaviors 迁移

- 建立 `CQRS-REWRITE-RP-016` 恢复点
- `LoggerFactoryResolver` 已从 `GFramework.Core` 下沉到 `GFramework.Core.Abstractions`，继续保留 `GFramework.Core.Logging` 命名空间
- 新的抽象层 `LoggerFactoryResolver` 默认会优先反射解析 `GFramework.Core.Logging.ConsoleLoggerFactoryProvider`
- 当宿主未加载 `GFramework.Core` 默认日志实现时，`LoggerFactoryResolver` 会退回静默 provider，避免上层模块只为拿日志入口而重新依赖实现层
- `GFramework.Core` 已新增 type forward，继续对外暴露 `LoggerFactoryResolver`，降低已编译消费端的运行时兼容风险
- `LoggingBehavior<TRequest, TResponse>` 与 `PerformanceBehavior<TRequest, TResponse>` 已迁移到 `GFramework.Cqrs`，同时继续保留 `GFramework.Core.Cqrs.Behaviors` 命名空间
- 迁移后 `GFramework.Core/Cqrs/*` 已全部搬空；当前 runtime 物理迁移残项只剩 `CqrsCoroutineExtensions`
- 并行 explorer 结论：
  - `ICqrsRuntime` 的真实阻塞链路是 `ICqrsRuntime -> IArchitectureContext -> IContextAware / handler Context`
  - 只要 CQRS handler 上下文模型仍直接依赖完整 `IArchitectureContext`，`ICqrsRuntime` 就不能无损迁到 `GFramework.Cqrs.Abstractions`
  - `CqrsCoroutineExtensions` 依赖 `TaskCoroutineExtensions` 与 `WaitForTask*`，本质属于 `Core` 协程桥接层，不宜原样迁到 `GFramework.Cqrs`
  - 若只追求下一步最小可行推进，应优先设计更窄的 CQRS 专用上下文 seam，并继续把协程桥接保留在 `GFramework.Core`

### 阶段：RP-016 验证

## 2026-04-16

### 阶段：ICqrsRuntime 归属收敛与兼容别名落地

- 建立 `CQRS-REWRITE-RP-017` 恢复点
- `GFramework.Cqrs.Abstractions/Cqrs/ICqrsContext.cs` 已新增，作为 CQRS runtime 使用的最小上下文 marker seam：
  - 该 seam 仅用于切断 `ICqrsRuntime -> IArchitectureContext` 的编译期循环依赖
  - 当前 runtime 仍允许在实现层识别更具体的 `IArchitectureContext`，以兼容现有 `IContextAware` handler 注入模型
- `GFramework.Cqrs.Abstractions/Cqrs/ICqrsRuntime.cs` 已新增并成为正式 runtime seam 归属
- `GFramework.Core.Abstractions/Cqrs/ICqrsRuntime.cs` 已改为兼容别名：
  - 旧接口现在继承新的 `GFramework.Cqrs.Abstractions.Cqrs.ICqrsRuntime`
  - 通过 `EditorBrowsable(EditorBrowsableState.Never)` 隐藏历史路径，避免新代码继续绑定旧 namespace
- `IArchitectureContext` 已实现 `ICqrsContext`，继续作为默认架构内的 CQRS 分发上下文
- `CqrsDispatcher` 已改为按 `ICqrsContext` 接收分发上下文：
  - 对现有 `IContextAware` handler 的注入保留运行时兼容检查
  - 若未来引入非架构型 CQRS context，实现层可显式阻止将其注入到仍要求 `IArchitectureContext` 的历史 handler
- `ArchitectureContext`、`CqrsRuntimeFactory` 与默认 runtime 注册路径已切到新的 `GFramework.Cqrs.Abstractions.Cqrs.ICqrsRuntime`
- `CqrsRuntimeModule` 与 `GFramework.Tests.Common/CqrsTestRuntime.cs` 已同时注册新旧 `ICqrsRuntime` 接口：
  - 主代码路径默认解析新接口
  - 历史公开路径仍可解析到同一个 runtime 实例，避免 consumer 或测试基础设施立即断裂
- 本轮结论同步收敛：
  - 旧 `GFramework.Core.Abstractions.Cqrs*` 兼容面只保留 `ICqrsRuntime` 这一最小公共别名
  - `GFramework.Core.Cqrs.Extensions` 不再视为需要继续正式维护的旧路径；仓库与 `CoreGrid-Migration` 已切到新的 `GFramework.Cqrs.Extensions` 入口

### 阶段：迁移目标修正为“Core = App Runtime，CQRS = 集成子系统”

- 在重新评估 Phase 5/7 的边界目标后，确认此前“继续推动 `Core` 尽量不依赖 `Cqrs`”的方向已经偏离本任务的真正目标
- 迁移计划现正式修正为：
  - `GFramework.Core.Abstractions -> GFramework.Cqrs.Abstractions`
  - `GFramework.Core -> GFramework.Cqrs`
  - `Core` 作为 App Runtime，默认集成 CQRS 子系统
  - `CQRS` 作为被集成的正式子系统，继续承接抽象层、实现层、generator 层
- 本次修正特别明确两点：
  - `Core` 强依赖 `Cqrs` 是允许的，不再以“彻底零依赖”为目标
  - 但 `Core` 不应依赖 CQRS 的细节结构，例如直接 `new` 具体 dispatcher、直接依赖 generator 生成类型、硬编码 handler/internal registry 细节
- 因此后续健康依赖标准改为：
  - 单向依赖
  - 通过 seam / 模块入口装配
  - 默认集成，但理论上可替换 runtime
- `CqrsCoroutineExtensions` 的定位也一并修正：
  - 它不再是“尚未迁移干净的 CQRS runtime 残项”
  - 而是 `Core` 对 CQRS 的协程桥接层，保留在 `Core` 是合理结果
- 方向修正后的下一步主线：
  1. 停止继续为了纯边界推进高成本的上下文/桥接迁移
  2. 保持 `Core -> Cqrs` 的健康单向依赖，继续避免细节泄漏
  3. 把精力转向 CQRS 子系统增强，尤其是 generator 覆盖面与低反射路径
- 同时补充一条实现约束：
  - 后续 CQRS runtime、pipeline、generator 与低反射路径的设计与实现，应明确参考 `Mediator` 中已经成熟可用的实现
  - 目标是吸收其已验证的结构与经验，减少重复踩坑，而不是把这些运行时细节完全从零发明

- `dotnet build GFramework/GFramework.Core.Abstractions/GFramework.Core.Abstractions.csproj -c Release`
  - 结果：通过
- `dotnet build GFramework/GFramework.Cqrs/GFramework.Cqrs.csproj -c Release`
  - 结果：通过
- `dotnet build GFramework/GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
  - 结果：通过
- `dotnet build GFramework/GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release`
  - 结果：通过
- `dotnet test GFramework/GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~GFramework.Core.Tests.Logging.LoggerFactoryTests|FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureModulesBehaviorTests|FullyQualifiedName~GFramework.Core.Tests.Cqrs.MediatorCompatibilityDeprecationTests"`
  - 结果：通过
  - 明细：`20` 个测试全部通过
- `dotnet test GFramework/GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsHandlerRegistrarTests|FullyQualifiedName~GFramework.Cqrs.Tests.Coroutine.CqrsCoroutineExtensionsTests|FullyQualifiedName~GFramework.Cqrs.Tests.Mediator.MediatorArchitectureIntegrationTests|FullyQualifiedName~GFramework.Cqrs.Tests.Mediator.MediatorComprehensiveTests|FullyQualifiedName~GFramework.Cqrs.Tests.Mediator.MediatorAdvancedFeaturesTests"`
  - 结果：通过
  - 明细：`54` 个测试全部通过

### 阶段：Core 泄漏盘点与 handler 注册协调器下沉

- 建立 `CQRS-REWRITE-RP-018` 恢复点
- 并行 explorer 盘点结论已收敛：
  - `ArchitectureContext -> ICqrsRuntime` 属于合理的 runtime seam，不视为实现细节泄漏
  - `Core` 当前未直接依赖任何 generator 生成类型名或生成注册器具体类型
  - 主要值得继续收敛的实现细节泄漏点集中在：
    - `MicrosoftDiContainer` 里原本维护的 handler 程序集去重状态与 registrar 调度
    - `ArchitectureBootstrapper` 对默认 handler 程序集的硬编码
    - 更长线的 `IIocContainer` / `IArchitecture` CQRS 装配 API 暴露面
- 本轮先完成最小落地收敛：
  - `GFramework.Cqrs/ICqrsRegistrationService.cs` 已新增，作为 CQRS handler 程序集接入协调入口
  - `GFramework.Cqrs/Internal/DefaultCqrsRegistrationService.cs` 已新增，将稳定程序集键去重与 `ICqrsHandlerRegistrar` 调度统一收敛到 CQRS runtime 内部
  - `GFramework.Cqrs/CqrsRuntimeFactory.cs` 已新增 `CreateRegistrationService(...)`
  - `GFramework.Core/Services/Modules/CqrsRuntimeModule.cs` 现会同时注册：
    - `ICqrsRuntime`
    - 旧 `GFramework.Core.Abstractions.Cqrs.ICqrsRuntime` 兼容别名
    - `ICqrsHandlerRegistrar`
    - `ICqrsRegistrationService`
  - `GFramework.Core/Ioc/MicrosoftDiContainer.cs` 已移除本地 `_registeredCqrsHandlerAssemblyKeys` 状态与 registrar 直连逻辑，`RegisterCqrsHandlersFromAssemblies(...)` 现仅委托给 `ICqrsRegistrationService`
  - `GFramework.Tests.Common/CqrsTestRuntime.cs` 已同步补齐 `ICqrsRegistrationService` 接线，保持裸测试容器与生产路径一致
- 这一轮结论是：
  - `Core -> Cqrs` 的单向依赖可以保留
  - 但 CQRS handler 注册细节应继续下沉到 CQRS 子系统，而不是分散在容器壳层中
  - 下一步主线可开始转向 generator / 低反射增强，而不是继续为边界纯化推进高成本桥接迁移

### 阶段：RP-018 验证

- `dotnet test GFramework/GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Core.Tests.Ioc.MicrosoftDiContainerTests|FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureAdditionalCqrsHandlersTests|FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureModulesBehaviorTests"`
  - 结果：通过
  - 明细：`43` 个测试全部通过
- `dotnet test GFramework/GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Mediator.MediatorArchitectureIntegrationTests|FullyQualifiedName~GFramework.Cqrs.Tests.Mediator.MediatorComprehensiveTests|FullyQualifiedName~GFramework.Cqrs.Tests.Mediator.MediatorAdvancedFeaturesTests"`
  - 结果：通过
  - 明细：`45` 个测试全部通过
- 在一次并行执行两条 `dotnet test` 的尝试中再次命中 `GFramework.Cqrs.deps.json` 文件锁冲突；
  顺序重跑后稳定通过，确认属于本地并发构建输出竞争，而不是本轮实现缺陷

### 阶段：generator 局部回退落地

- 建立 `CQRS-REWRITE-RP-019` 恢复点
- 已将 CQRS handler generator 从“整程序集回退”推进到“局部 generated + 局部 reflection fallback”：
  - `GFramework.Cqrs/CqrsReflectionFallbackAttribute.cs` 已新增，作为程序集级 fallback marker
  - `GFramework.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 现会：
    - 继续为可由生成代码合法引用的 handlers 生成 registry
    - 当程序集内同时存在不可见 handlers 且 runtime 合同支持 marker 时，额外生成 `CqrsReflectionFallbackAttribute`
    - 若消费端仍缺少该 marker 合同，则保持旧的整程序集回退行为，避免静默漏掉不可见 handlers
- `GFramework.Cqrs/Internal/CqrsHandlerRegistrar.cs` 已配套支持新的组合路径：
  - 优先执行 generated registry
  - 若程序集带有 `CqrsReflectionFallbackAttribute`，则继续补一次 reflection 扫描
  - reflection 补扫会按 `ServiceType + ImplementationType` 去重，避免已由 generated registry 注册的映射重复写入
- `GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs` 已更新/新增：
  - 正常生成快照继续覆盖
  - “私有嵌套 handler” 场景改为断言“仍生成可见 handler + 输出 fallback marker”
  - 新增“旧 runtime 合同缺少 marker 时仍整程序集回退”的兼容回归
- `GFramework.Cqrs.Tests/Cqrs/CqrsHandlerRegistrarTests.cs` 已新增组合路径回归：
  - 当 generated registry 与 reflection fallback 同时参与时，剩余 handlers 会被补齐
  - 已由 generated registry 注册的 handler 映射不会重复写入服务集合

### 阶段：RP-019 验证

- `dotnet test GFramework/GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.SourceGenerators.Tests.Cqrs.CqrsHandlerRegistryGeneratorTests"`
  - 结果：通过
  - 明细：`4` 个测试全部通过
- `dotnet test GFramework/GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsHandlerRegistrarTests"`
  - 结果：通过
  - 明细：`5` 个测试全部通过
- `dotnet test GFramework/GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureAdditionalCqrsHandlersTests"`
  - 结果：通过
  - 明细：`2` 个测试全部通过
- 在并行执行三条 `dotnet test` 时，`SourceGenerators` 与 `Cqrs` 两条命中过一次本地 restore / deps 文件竞争；
  顺序重跑后全部通过，确认属于本地并发构建输出竞争，不是本轮实现缺陷

### 阶段：runtime 物理迁移（第一批）

- 建立 `CQRS-REWRITE-RP-014` 恢复点
- `GFramework.Core/Cqrs/Internal/CqrsDispatcher.cs`、`CqrsHandlerRegistrar.cs` 与
  `DefaultCqrsHandlerRegistrar.cs` 已迁移到 `GFramework.Cqrs` 项目，保留原 `GFramework.Core.Cqrs.Internal`
  命名空间，降低消费端源码层面的 breaking change
- `GFramework.Core/Cqrs/Command/CommandBase.cs`、`Query/QueryBase.cs`、`Request/RequestBase.cs` 与
  `Notification/NotificationBase.cs` 已迁移到 `GFramework.Cqrs`
- `GFramework.Core/Extensions/ContextAwareCqrsExtensions.cs`、`ContextAwareCqrsCommandExtensions.cs` 与
  `ContextAwareCqrsQueryExtensions.cs` 已迁移到 `GFramework.Cqrs`
- `ICqrsHandlerRegistry` 与 `CqrsHandlerRegistryAttribute` 已从 `GFramework.Core.Abstractions`
  收敛到 `GFramework.Cqrs` 根命名空间，作为 runtime 专属契约：
  - 这样避免了把依赖 `ILogger` / `IServiceCollection` 的类型继续塞进 `GFramework.Cqrs.Abstractions`
  - 同时规避 `GFramework.Core.Abstractions <-> GFramework.Cqrs.Abstractions` 循环引用
- `GFramework.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 已同步迁移 metadata name：
  - handler interface 解析改为指向 `GFramework.Cqrs.Abstractions.Cqrs`
  - generated registry contract / attribute 改为指向 `GFramework.Cqrs`
- `GFramework.Cqrs/CqrsRuntimeFactory.cs` 已新增，`GFramework.Core/Services/Modules/CqrsRuntimeModule.cs`
  与 `GFramework.Tests.Common/CqrsTestRuntime.cs` 现通过公开工厂接线默认 runtime / registrar，
  不再跨程序集直接实例化内部实现
- 迁移过程中确认 `GFramework.Core/Coroutine/Extensions/CqrsCoroutineExtensions.cs`
  仍依赖 `TaskCoroutineExtensions.AsCoroutineInstruction()` 等 `Core` 协程工具链，
  因此本轮暂留 `GFramework.Core`，未强行迁移

### 阶段：runtime 物理迁移（第一批）验证

- `dotnet build GFramework/GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release`
  - 结果：通过
- `dotnet test GFramework/GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~GFramework.SourceGenerators.Tests.Cqrs.CqrsHandlerRegistryGeneratorTests"`
  - 结果：通过
  - 明细：`3` 个测试全部通过
- `dotnet build GFramework/GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release`
  - 结果：通过
- `dotnet test GFramework/GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsHandlerRegistrarTests|FullyQualifiedName~GFramework.Cqrs.Tests.Mediator.MediatorArchitectureIntegrationTests|FullyQualifiedName~GFramework.Cqrs.Tests.Mediator.MediatorComprehensiveTests|FullyQualifiedName~GFramework.Cqrs.Tests.Mediator.MediatorAdvancedFeaturesTests"`
  - 结果：通过
  - 明细：`49` 个测试全部通过
- `dotnet build GFramework/GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
  - 结果：通过
- `dotnet test GFramework/GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureAdditionalCqrsHandlersTests|FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureModulesBehaviorTests|FullyQualifiedName~GFramework.Core.Tests.Ioc.MicrosoftDiContainerTests|FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureContextTests"`
  - 结果：通过
  - 明细：`63` 个测试全部通过
- 并行执行两个会触发编译输出写入同一 `GFramework.Tests.Common.dll` 的一次性 copy retry warning；
  单次顺序验证通过，确认属于本地并行构建噪音，不是实现缺陷

## 2026-04-15

### 阶段：非默认程序集显式 CQRS 接入

- 建立 `CQRS-REWRITE-RP-009` 恢复点
- `IArchitecture` / `IIocContainer` / `Architecture` / `ArchitectureModules` / `MicrosoftDiContainer`
  已新增：
  - `RegisterCqrsHandlersFromAssembly(Assembly assembly)`
  - `RegisterCqrsHandlersFromAssemblies(IEnumerable<Assembly> assemblies)`
- `ArchitectureBootstrapper` 已改为复用容器公开入口完成默认程序集注册，
  让“默认启动路径”和“额外程序集显式接入路径”统一走同一套 CQRS handler 注册逻辑
- `MicrosoftDiContainer` 已按稳定程序集键去重 CQRS handler 注册，
  避免同一程序集被默认路径、模块安装或用户初始化阶段重复接入时写入重复 handler 映射
- `ArchitectureAdditionalCqrsHandlersTests` 已新增并锁定两类行为：
  - 显式接入扩展程序集时，运行时会通过程序集级 `CqrsHandlerRegistryAttribute` 成功注册 handlers
  - 同一额外程序集被重复声明时，不会重复注册 handlers
- `docs/zh-CN/core/cqrs.md` 与 `CLAUDE.md` 已同步补充新的显式接入入口与行为说明

### 阶段：非默认程序集显式 CQRS 接入验证

- `dotnet build GFramework/GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
  - 结果：通过
  - 备注：仅存在既有 `MA0048` warnings，无新增构建错误
- `dotnet test GFramework/GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureAdditionalCqrsHandlersTests|FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureModulesBehaviorTests|FullyQualifiedName~GFramework.Core.Tests.Cqrs.CqrsHandlerRegistrarTests|FullyQualifiedName~GFramework.Core.Tests.Architectures.RegistryInitializationHookBaseTests"`
  - 结果：通过
  - 明细：`13` 个测试全部通过

### 下一步

1. 评估如何进一步降低“显式额外程序集接入”场景中的反射回退占比，让更多程序集直接命中生成注册器
2. 评估是否将 CQRS 正式拆分为独立 abstractions 模块与 runtime 实现模块，并明确 `GFramework.Core` 的最终依赖边界
3. 为 future-major 版本准备兼容层真正移除时的 API 清理清单与升级说明

### 当前残留

- 兼容 API、测试目录与部分历史文档仍保留 `Mediator` 前缀，但主入口已新增中性 CQRS 命名，且兼容 API 已进入正式弃用周期
- handler 自动注册当前已具备默认路径与显式扩展路径的统一入口，但仍有一部分程序集只能走反射回退
- solution 级 `dotnet build GFramework.sln -c Release` 仍受既有 Windows NuGet fallback package folder 配置影响

### 下一步建议

1. 优先补齐“非默认程序集”的 CQRS handler generator 接入点，例如显式模块程序集或扩展包程序集
2. 在 source-generator 覆盖范围更明确后，评估是否把现有 CQRS runtime 从 `GFramework.Core` 中继续外提为独立模块
3. 再规划 `RegisterMediatorBehavior`、`MediatorCoroutineExtensions` 与 `ContextAwareMediator*Extensions` 的最终删除窗口
4. 评估是否为兼容层移除提供 analyzer 或 upgrade note，以降低 future-major 迁移成本

### 阶段：review 跟进微调

- `ArchitectureAdditionalCqrsHandlersTests` 已改为复用现有 `SyncTestArchitecture` + `AddPostRegistrationHook(...)`
  测试基建，不再维护仅用于注入初始化逻辑的专用 `AdditionalHandlersTestArchitecture`
- “显式额外程序集去重”回归用例已加强为两个不同 `Assembly` mock 实例，但共享相同 `FullName` 稳定键，
  直接锁定 `MicrosoftDiContainer` 的程序集键去重语义，而不是只验证同一 mock 实例的重复注册

### 下一步

1. 运行 `ArchitectureAdditionalCqrsHandlersTests` 定向回归，确认基建复用后显式程序集接入与去重行为保持通过
2. 如需继续收敛测试样板，可再盘点 `Architecture` 相关测试里仍然只用于初始化注入的本地架构类型

### 阶段：CQRS 模块边界评估

- 建立 `CQRS-REWRITE-RP-010` 恢复点
- 已完成 Phase 5 的模块边界再评估，并新增 `ai-plan/migration/CQRS_MODULE_SPLIT_PLAN.md`
- 本轮结论：
  - 将 CQRS 拆为独立 abstractions/runtime 模块是成立的
  - 但当前不能直接做“搬项目”式拆分，必须先完成 `GFramework.Core -> CQRS runtime abstraction` 的依赖倒置
- 已确认的硬耦合点：
  - `ArchitectureContext` 直接实例化 `CqrsDispatcher`
  - `MicrosoftDiContainer` 直接调用 `CqrsHandlerRegistrar`
  - `ArchitectureBootstrapper` 在默认启动路径内隐含 CQRS runtime 已存在
- 已确认的消费端兼容压力：
  - `CoreGrid-Migration/scripts/cqrs/**` 大量直接依赖 `CommandBase`、`Abstract*Handler` 与 `GFramework.Core.Cqrs.Extensions`
  - `GFramework.Godot/Coroutine/ContextAwareCoroutineExtensions.cs` 直接依赖 `GFramework.Core.Cqrs.Extensions`
- 新拆分草案明确了推荐目标：
  - `GFramework.Cqrs.Abstractions`：正式 CQRS 契约 + runtime seam
  - `GFramework.Cqrs`：dispatcher、handler registrar、base handlers、behaviors、extensions
  - `GFramework.Core.Abstractions`：转为依赖 `GFramework.Cqrs.Abstractions`
  - `GFramework.Core`：只保留架构集成与兼容壳层

### 下一步

1. 进入 Phase 6，优先为 runtime 建立 `ICqrsRuntime` / `ICqrsHandlerRegistrar` seam
2. 将 `ArchitectureContext` 与 `MicrosoftDiContainer` 改为依赖该 seam，而不是直接依赖 `CqrsDispatcher` / `CqrsHandlerRegistrar`
3. 在默认启动行为与消费端源码保持兼容的前提下，补齐针对 seam 改造的定向测试
4. 待 seam 稳定后，再创建 `GFramework.Cqrs.Abstractions` / `GFramework.Cqrs` 项目骨架并推进源码迁移

### 阶段：CQRS runtime seam 落地

- 建立 `CQRS-REWRITE-RP-011` 恢复点
- `GFramework.Core.Abstractions/Cqrs/ICqrsRuntime.cs` 与 `ICqrsHandlerRegistrar.cs` 已新增，明确 runtime 调度与 handler 接入的依赖倒置 seam
- `CqrsDispatcher` 已改为实现 `ICqrsRuntime`，并改为在调用时接收 `IArchitectureContext`
- `ArchitectureContext` 已改为解析 `ICqrsRuntime`，不再直接 `new CqrsDispatcher(...)`
- `MicrosoftDiContainer.RegisterCqrsHandlersFromAssemblies(...)` 已改为解析 `ICqrsHandlerRegistrar`，不再直接调用 `CqrsHandlerRegistrar`
- `DefaultCqrsHandlerRegistrar` 与 `CqrsRuntimeModule` 已落地，使默认架构启动路径继续自动具备 CQRS runtime 与 handler 注册能力
- `CqrsTestRuntime.RegisterInfrastructure(...)` 已新增，用于为裸 `MicrosoftDiContainer` 测试容器补齐与生产路径一致的 seam 基础设施
- 针对 `Clear()` 后重新接入 handler 的回归已补上“先恢复测试基础设施再验证 dedup 状态重置”的显式步骤

### 阶段：CQRS runtime seam 验证

- `dotnet build GFramework/GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
  - 结果：通过
- `dotnet test GFramework/GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Core.Tests.Ioc.MicrosoftDiContainerTests|FullyQualifiedName~GFramework.Core.Tests.Cqrs.CqrsHandlerRegistrarTests|FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureModulesBehaviorTests|FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureAdditionalCqrsHandlersTests|FullyQualifiedName~GFramework.Core.Tests.Architectures.RegistryInitializationHookBaseTests|FullyQualifiedName~GFramework.Core.Tests.Mediator.MediatorArchitectureIntegrationTests|FullyQualifiedName~GFramework.Core.Tests.Mediator.MediatorComprehensiveTests|FullyQualifiedName~GFramework.Core.Tests.Mediator.MediatorAdvancedFeaturesTests"`
  - 结果：通过
  - 明细：`97` 个测试全部通过

### 下一步

1. 进入 Phase 7，创建 `GFramework.Cqrs.Abstractions` / `GFramework.Cqrs` 项目骨架
2. 先迁移 `GFramework.Core.Abstractions/Cqrs/*`，并保持现有命名空间，避免同时引入 namespace breaking change
3. 再迁移 `GFramework.Core/Cqrs/*`、`GFramework.Core.Cqrs.Extensions` 与相关协程扩展
4. 最后处理 `GFramework.SourceGenerators`、顶层 meta package 与消费端 transitive 依赖图

### 阶段：CQRS abstractions 项目骨架与纯契约迁移

- 建立 `CQRS-REWRITE-RP-012` 恢复点
- `GFramework.Cqrs.Abstractions` 与 `GFramework.Cqrs` 项目骨架已创建，并加入 `GFramework.sln`
- `GFramework.Core.Abstractions` 已改为引用 `GFramework.Cqrs.Abstractions`
- 以下纯 CQRS 契约已迁移到 `GFramework.Cqrs.Abstractions`，同时保持原命名空间 `GFramework.Core.Abstractions.Cqrs*` 不变：
  - `IRequest*` / `IStreamRequest*` / `INotification*`
  - `IPipelineBehavior`
  - `MessageHandlerDelegate`
  - `Unit`
  - `Command/Query/Request/Notification` 输入与标记契约
  - `ICqrsHandlerRegistrar`
- 在实际迁移中确认：
  - `ICqrsRuntime -> IArchitectureContext`
  - `ICqrsHandlerRegistry -> ILogger`
  这两条依赖会导致 `GFramework.Core.Abstractions <-> GFramework.Cqrs.Abstractions` 循环引用风险，因此当前暂不迁出这三类类型：
  - `ICqrsRuntime`
  - `ICqrsHandlerRegistry`
  - `CqrsHandlerRegistryAttribute`

### 阶段：CQRS abstractions 迁移验证

- `dotnet build GFramework/GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
  - 结果：通过
  - 备注：仅存在既有 `MA0048` warnings（`IStreamCommand` / `IStreamQuery` 文件命名）
- `dotnet test GFramework/GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~GFramework.Core.Tests.Ioc.MicrosoftDiContainerTests|FullyQualifiedName~GFramework.Core.Tests.Cqrs.CqrsHandlerRegistrarTests|FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureModulesBehaviorTests|FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureAdditionalCqrsHandlersTests|FullyQualifiedName~GFramework.Core.Tests.Architectures.RegistryInitializationHookBaseTests|FullyQualifiedName~GFramework.Core.Tests.Mediator.MediatorArchitectureIntegrationTests|FullyQualifiedName~GFramework.Core.Tests.Mediator.MediatorComprehensiveTests|FullyQualifiedName~GFramework.Core.Tests.Mediator.MediatorAdvancedFeaturesTests"`
  - 结果：通过
  - 明细：`97` 个测试全部通过

### 下一步

1. 先为 `ICqrsRuntime` / `ICqrsHandlerRegistry` 收敛新的边界方案，避免 `Core.Abstractions` 与 `Cqrs.Abstractions` 形成循环引用
2. 再开始迁移 `GFramework.Core/Cqrs/*`、`GFramework.Core.Cqrs.Extensions` 与协程扩展到 `GFramework.Cqrs`
3. 迁移 runtime 后，再处理 `GFramework.SourceGenerators` 与 meta package 的依赖图更新

### 阶段：CQRS 聚焦测试拆分与 CI 接入

- 建立 `CQRS-REWRITE-RP-013` 恢复点
- 已新增 `GFramework.Cqrs.Tests` 项目，并加入 `GFramework.sln`
- 以下 CQRS 聚焦测试已从 `GFramework.Core.Tests` 迁移到 `GFramework.Cqrs.Tests`：
  - `Cqrs/CqrsHandlerRegistrarTests.cs`
  - `Coroutine/CqrsCoroutineExtensionsTests.cs`
  - `Mediator/MediatorAdvancedFeaturesTests.cs`
  - `Mediator/MediatorArchitectureIntegrationTests.cs`
  - `Mediator/MediatorComprehensiveTests.cs`
- `GFramework.Cqrs.Tests/GlobalUsings.cs` 已补齐 `NUnit` / `Moq` / `System.*` / `Microsoft.Extensions.DependencyInjection` 等基础编译上下文
- `GFramework.Cqrs.Tests/Logging/TestLogger.cs` 已新增，使新测试项目不再依赖 `GFramework.Core.Tests/Logging/LoggerTests.cs` 中的内嵌测试辅助
- `GFramework.Core.Tests/Ioc/MicrosoftDiContainerTests.cs` 已改为依赖本地 `ContainerRegistrationFixtures.cs`，避免继续编译期引用已迁出的 CQRS 测试类型
- `GFramework/.github/workflows/ci.yml` 已新增 `dotnet test GFramework.Cqrs.Tests ...` step，确保 PR CI 覆盖新测试项目

### 阶段：CQRS 聚焦测试拆分验证

- `dotnet build GFramework/GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release`
  - 结果：通过
- `dotnet test GFramework/GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --no-build`
  - 结果：通过
  - 明细：`54` 个测试全部通过
- `dotnet build GFramework/GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
  - 结果：通过
- `dotnet test GFramework/GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~MicrosoftDiContainerTests|FullyQualifiedName~ArchitectureAdditionalCqrsHandlersTests|FullyQualifiedName~ArchitectureModulesBehaviorTests|FullyQualifiedName~MediatorCompatibilityDeprecationTests"`
  - 结果：通过
  - 明细：`44` 个测试全部通过

### 下一步

1. 开始迁移 `GFramework.Core/Cqrs/*`、`GFramework.Core.Cqrs.Extensions` 与相关协程扩展到 `GFramework.Cqrs`
2. 在 runtime 迁移过程中保持 `GFramework.Cqrs.Tests` 绿灯，持续作为 CQRS 模块的主回归项目
3. 等 runtime 物理迁移完成后，再处理 source-generator 引用图与顶层 package/transitive 依赖整理

### 阶段：轻量 handler 上下文基类与 CoreGrid 兼容性收敛

- 建立 `CQRS-REWRITE-RP-015` 恢复点
- `GFramework.Cqrs/Cqrs/CqrsContextAwareHandlerBase.cs` 已新增，作为只依赖 `IContextAware` / `IArchitectureContext` 的轻量 CQRS handler 基类
- 各类 `Abstract*Handler` 已改为继承该轻量基类，不再直接依赖 `GFramework.Core.Rule.ContextAwareBase`
- 新增 `GFramework.Cqrs.Tests/Cqrs/AbstractCqrsHandlerContextTests.cs`，回归锁定“未注入前 fail-fast、注入后可访问 Context、`OnContextReady()` 仍生效”的行为
- 真实消费端验证阶段，`CoreGrid-Migration` 暴露了旧 `GFramework.Core.Abstractions.Cqrs*` 与旧 `GFramework.Core.Cqrs.Extensions` namespace 已不再自动可用的问题
- 为了先恢复迁移验证链路，本轮先采取 consumer 侧最小修复：
  - `scripts/GlobalUsings.cs` 已补齐新的 `GFramework.Cqrs.Abstractions.Cqrs*`、`GFramework.Cqrs.*` 与 `GFramework.Core.Cqrs.*` 导入
  - `scripts/cqrs/**` 中显式写死旧 `Unit` / `INotification` / `IQuery` 路径的文件已切到新的 `GFramework.Cqrs.Abstractions.Cqrs*`
  - `GlobalInputController.cs`、`PauseMenu.cs`、`OptionsMenu.cs` 与两个组合 handler 已切到 `GFramework.Cqrs.Extensions.ContextAwareCqrs*Extensions`

### 阶段：RP-015 验证

- `dotnet build CoreGrid-Migration/CoreGrid.sln`
  - 结果：通过
  - 备注：仅存在 `CoreGrid-Migration` 既有 analyzer warnings，无新增 CQRS 编译错误

### 下一步

1. 明确是否正式承诺旧 `GFramework.Core.Abstractions.Cqrs*` / `GFramework.Core.Cqrs.Extensions` public namespace 兼容
2. 若不承诺兼容，补充迁移文档并继续沿 `GFramework.Cqrs*` 命名空间推进
3. 若承诺兼容，再单独设计兼容层，而不是让 `CoreGrid-Migration` 这类消费端各自散落修补
4. 无论兼容策略如何，下一阶段都应继续收敛 `ICqrsRuntime -> IArchitectureContext` 与 `CqrsCoroutineExtensions -> TaskCoroutineExtensions` 的剩余边界

## 2026-04-16

### 阶段：review 收尾修正（partial reflection fallback 精确定向补扫）

- 建立 `CQRS-REWRITE-RP-022` 恢复点
- `GFramework.Cqrs/CqrsReflectionFallbackAttribute.cs` 已扩展为可携带精确 fallback handler 类型名清单：
  - 生成器能够把“无法在生成代码中直接引用”的 concrete handler 记录为程序集级 metadata
  - 当 metadata 为空时，runtime 仍保持旧版“整程序集补扫”的兼容语义
- `GFramework.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 已改为：
  - 对不可直接引用的 concrete handler 生成精确的 runtime type-name 清单
  - 若消费端 runtime 仅支持旧版无参 fallback marker，则自动退回旧语义，避免破坏兼容
  - 无 unsupported handler 时不再错误输出 fallback marker
- `GFramework.Cqrs/Internal/CqrsHandlerRegistrar.cs` 已改为：
  - 生成注册器命中后，优先读取 `CqrsReflectionFallbackAttribute` 中的精确 type-name 清单
  - 若清单存在，则按稳定顺序定向 `Assembly.GetType(...)` 解析剩余 handlers，而不是重新 `GetTypes()` 扫描整个程序集
  - 只有在旧 marker 或手写 marker 未提供清单时，才继续回退到整程序集扫描路径
- `GFramework.Cqrs.Tests/Cqrs/CqrsHandlerRegistrarTests.cs` 已新增/更新回归：
  - 锁定“partial fallback 通过精确 type-name 补扫”
  - 断言该路径不会调用 `Assembly.GetTypes()`
- `GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs` 已新增/更新回归：
  - 锁定“私有嵌套 handler => 输出精确 type-name fallback marker”
  - 锁定“旧版 runtime 仅支持无参 marker 时 => 生成器自动退回旧语义”

### 阶段：RP-022 验证

- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.SourceGenerators.Tests.Cqrs.CqrsHandlerRegistryGeneratorTests"`
  - 结果：通过
  - 明细：`5` 个测试全部通过
- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsHandlerRegistrarTests"`
  - 结果：通过
  - 明细：`5` 个测试全部通过

### 下一步

1. 继续扩大 generator 命中面，优先盘点仍会落到“旧 marker / 无精确清单”路径的实际场景
2. 评估是否需要为手写/第三方程序集提供更正式的精确 fallback metadata 入口，而不是只依赖 generator 自动产出
3. 在 handler 注册路径之外，继续评估 dispatch / invoker 链路可否进一步减少运行时反射

## 2026-04-16

### 阶段：review 收尾修正（手写/第三方程序集的精确 fallback metadata 入口）

- 建立 `CQRS-REWRITE-RP-023` 恢复点
- `GFramework.Cqrs/CqrsReflectionFallbackAttribute.cs` 已扩展为同时支持：
  - `params string[] fallbackHandlerTypeNames`
  - `params Type[] fallbackHandlerTypes`
  - 无参兼容 marker
- 这使手写或第三方程序集在“有 generated registry，但仍需补少量 reflection-only handlers”时，可以直接声明可引用的 handler `Type`，避免再走字符串名称回查。
- `GFramework.Cqrs/Internal/CqrsHandlerRegistrar.cs` 已改为按以下优先级处理 fallback metadata：
  - 先消费显式 `Type` 集合
  - 再消费字符串 type-name 清单
  - 只有没有任何精确 metadata 时，才继续回退到整程序集 `GetTypes()` 扫描
- registrar 对 direct `Type` fallback 的程序集一致性校验已从对象引用比较收敛为稳定程序集键比较，避免代理/测试场景下把语义等价的程序集误判为不一致。
- `GFramework.Cqrs.Tests/Cqrs/CqrsHandlerRegistrarTests.cs` 已新增回归：
  - 锁定“direct `Type` fallback 不触发 `Assembly.GetType()`”
  - 锁定“direct `Type` fallback 也不触发 `Assembly.GetTypes()`”

### 阶段：RP-023 验证

- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsHandlerRegistrarTests"`
  - 结果：通过
  - 明细：`6` 个测试全部通过

### 下一步

1. handler 注册层的“手写 fallback 精确入口”已补齐，后续优先转向 `CqrsDispatcher` 热路径
2. 盘点 `PublishAsync` / `SendAsync` / `CreateStream` 中每次分发都会发生的 `MakeGenericType` / service-type 构造，评估是否用稳定缓存进一步减少反射
3. 若热路径缓存收益成立，再补一组聚焦 `CqrsDispatcher` 的回归或性能守护测试

## 2026-04-16

### 阶段：review 收尾修正（dispatcher 热路径 service-type 缓存）

- 建立 `CQRS-REWRITE-RP-024` 恢复点
- `GFramework.Cqrs/Internal/CqrsDispatcher.cs` 已新增三组热路径 service-type 缓存：
  - `NotificationHandlerServiceTypes`
  - `RequestServiceTypes`
  - `StreamHandlerServiceTypes`
- 这些缓存把以下重复工作收敛为“首次构造一次，后续复用”：
  - `PublishAsync(...)` 中 `typeof(INotificationHandler<>).MakeGenericType(...)`
  - `SendAsync(...)` 中 request handler / pipeline behavior 的服务类型构造
  - `CreateStream(...)` 中 stream handler 的服务类型构造
- 现有 invoker delegate 缓存继续保留；这轮补的是容器 service lookup 前的泛型服务类型构造缓存，而不是替换已有 invoker cache。
- `GFramework.Cqrs.Tests/Cqrs/CqrsDispatcherCacheTests.cs` 已新增回归：
  - 用唯一的 request / notification / stream 测试类型命中三条 dispatcher 路径
  - 通过反射读取 dispatcher 内部缓存字典，锁定“首次分发新增一条缓存，后续同类型分发不再增长”

### 阶段：RP-024 验证

- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsHandlerRegistrarTests|FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsDispatcherCacheTests"`
  - 结果：通过
  - 明细：`7` 个测试全部通过

### 下一步

1. 继续评估 request pipeline / invoker 路径是否还有值得继续缓存或收敛的动态构造
2. 若收益有限，再回到 generator 命中面扩张与 legacy fallback 面积压缩
3. 若后续要做更强性能守护，可考虑补一个更窄的 benchmark 或轻量性能回归，而不是只依赖功能性缓存测试

## 2026-04-16

### 阶段：review 收尾修正（dispatcher invoker method-definition 静态缓存与统一缓存回归）

- 建立 `CQRS-REWRITE-RP-025` 恢复点
- `GFramework.Cqrs/Internal/CqrsDispatcher.cs` 已将以下泛型方法定义查找收敛为静态一次解析：
  - `InvokeRequestHandlerAsync`
  - `InvokeRequestPipelineAsync`
  - `InvokeNotificationHandlerAsync`
  - `InvokeStreamHandler`
- 这样每种新消息类型首次命中 invoker cache 时，只剩 `MakeGenericMethod(...) + CreateDelegate(...)`，不再重复执行 `GetMethod(...)` 查找。
- `GFramework.Cqrs.Tests/Cqrs/CqrsDispatcherCacheTests.cs` 已扩展为同时覆盖：
  - request/no-pipeline 的 `RequestInvokers`
  - request/with-pipeline 的 `RequestPipelineInvokers`
  - notification 的 `NotificationInvokers`
  - stream 的 `StreamInvokers`
  - 以及对应的 service-type cache
- 测试通过显式注册 `DispatcherPipelineCacheBehavior` 命中 pipeline 分支，避免当前缓存回归只覆盖无行为请求路径。

### 阶段：RP-025 验证

- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsHandlerRegistrarTests|FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsDispatcherCacheTests"`
  - 结果：通过
  - 明细：`7` 个测试全部通过

### 下一步

1. dispatcher 侧基础低反射缓存已覆盖 service-type 与 invoker 两层，后续优先判断 pipeline delegate 链的每次分发构造是否仍值得继续收敛
2. 若 pipeline 链构造收益不大，则回到 generator 命中面扩张与 legacy fallback 面积压缩
3. 若需要进一步证明收益，可考虑增加更窄的性能守护而不是继续堆功能性反射缓存测试

## 2026-04-16

### 阶段：review 收尾修正（generated registry 内部定向反射注册隐藏 handler）

- 建立 `CQRS-REWRITE-RP-026` 恢复点
- `GFramework.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 已将“不可直接 `typeof(...)` 引用的 concrete handler”从程序集级 fallback marker 输出收敛为 generated registry 内部的定向反射注册：
  - 对可见 handler 仍保持 `typeof(...)` 直注册路径
  - 对私有/不可直接引用但仍可按元数据名从当前程序集重新定位的 handler，改为生成 `Assembly.GetType(...)` + `GetInterfaces()` 的本地注册逻辑
  - 这类场景不再额外要求 runtime registrar 读取 `CqrsReflectionFallbackAttribute`
- 生成器不再依赖 runtime 是否暴露 `CqrsReflectionFallbackAttribute` 才决定是否产出 registry；隐藏 handler 已由生成代码自身覆盖，因此旧版“无 marker 就整程序集放弃生成”的分支已去除。
- `GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs` 已同步更新：
  - 私有嵌套 handler 快照改为断言 generated registry 自行调用 `RegisterReflectedHandler(...)`
  - 旧版无参 marker 合同场景改为断言“不再输出 legacy marker”
  - 完全不存在 fallback marker 合同时，仍断言 generator 会继续产出 registry 并覆盖隐藏 handler

### 阶段：RP-026 验证

- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.SourceGenerators.Tests.Cqrs.CqrsHandlerRegistryGeneratorTests"`
  - 结果：通过
  - 明细：`5` 个测试全部通过
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~GFramework.SourceGenerators.Tests.Cqrs.CqrsHandlerRegistryGeneratorTests"`
  - 结果：通过
  - 明细：`5` 个测试全部通过
- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsHandlerRegistrarTests|FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsDispatcherCacheTests"`
  - 结果：通过
  - 明细：`7` 个测试全部通过

### 下一步

1. 继续盘点 generator 仍未覆盖的 handler 形态，确认是否还存在必须依赖 runtime fallback metadata 的真实残余场景
2. 若 generator 命中面已接近稳定，再回到 dispatcher pipeline delegate 链构造，判断是否值得继续做低反射/低分配收敛
3. 若后续继续参考 `Mediator`，优先找“生成期已知、运行期只做常量时间绑定”的模式，而不是新增另一层宽反射补扫

## 2026-04-16

### 阶段：review 收尾修正（隐藏 implementation + 可见 handler interface 的直连接线）

- 建立 `CQRS-REWRITE-RP-027` 恢复点
- 并行/本地盘点结论收敛：
  - `CqrsDispatcher` 当前剩余开销主要是容器解析、pipeline 链重建与 `object` 装箱，不再是优先级最高的“反射缺口”
  - 因此本轮继续回到 generator 主线，而不是提前做 dispatcher 微优化
- `GFramework.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 已新增一条更窄的生成路径：
  - 当 handler implementation 不能被生成代码直接 `typeof(...)` 引用，但 handler interface 仍可直接引用时
  - generated registry 现在只会对 implementation 做一次 `Assembly.GetType(...)`
  - 随后直接以 `typeof(IRequestHandler<...>)` / `typeof(INotificationHandler<...>)` / `typeof(IStreamRequestHandler<...>)` 完成注册
  - 不再为这类场景额外生成 `GetInterfaces()`、`IsSupportedHandlerInterface(...)` 与 `GetRuntimeTypeDisplayName(...)` 辅助逻辑
- 该调整把“隐藏 handler 的 generated registry 内部处理”继续从“implementation + interface 运行时发现”收敛为“implementation-only lookup + direct interface binding”
- `GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs` 已新增快照回归：
  - 锁定“隐藏 implementation + 可见 interface”场景只生成 implementation lookup + direct service registration
  - 同时保持已有“隐藏 implementation + 隐藏 contract”场景仍走旧的本地 `RegisterReflectedHandler(...)` 辅助路径
- 本轮实现过程中命中过一次生成器运行时异常：
  - 原因是新增分支后 `ImmutableArray` builder 不再总是满容量，`MoveToImmutable()` 触发 `Count equals Capacity` 约束
  - 已收敛为 `ToImmutable()`，并同步修正 generated source 分支顺序与 helper 输出条件

### 阶段：RP-027 验证

- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.SourceGenerators.Tests.Cqrs.CqrsHandlerRegistryGeneratorTests"`
  - 结果：通过
  - 明细：`6` 个测试全部通过
- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsHandlerRegistrarTests"`
  - 结果：通过
  - 明细：`6` 个测试全部通过

### 下一步

1. 继续盘点 generator 仍会落到 `GetInterfaces()` 或 runtime fallback metadata 的 handler 形态，优先找还能压成“生成期已知 service type + implementation lookup”的场景
2. 若 generator 侧残余面积已经很小，再回到 dispatcher，重点考虑是否真的值得为值类型响应装箱与 pipeline 链重建做更复杂的 typed-invoker 优化
3. 若要继续参考 `Mediator`，优先寻找“生成期把接口/handler 绑定静态化”的模式，而不是继续扩大运行时辅助反射工具面

## 2026-04-16

### 阶段：review 收尾修正（隐藏 interface 的精确 service type 重建）

- 建立 `CQRS-REWRITE-RP-028` 恢复点
- 在继续盘点 generator 命中面后，本轮把一类仍落到 `GetInterfaces()` 的常见场景继续收窄：
  - implementation 不可直接 `typeof(...)`
  - handler interface 也不可直接 `typeof(...)`
  - 但 open generic contract 与闭包 type arguments 仍然能在生成期被精确表达
- `GFramework.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 已新增更窄的精确重建路径：
  - 对 `IRequestHandler<,>` / `INotificationHandler<>` / `IStreamRequestHandler<,>` 记录 open generic contract
  - 对不可直引、但属于当前编译程序集的 type arguments 输出定向 `Assembly.GetType(...)`
  - 运行期通过 `MakeGenericType(...)` 重建精确 service type
  - 再把该 service type 直接绑定到 implementation，而不是先 `GetInterfaces()` 再做支持接口筛选
- 这一轮已覆盖的典型场景包括：
  - 私有嵌套 request + 私有 handler + 可见 response
  - 可见 implementation + 隐藏消息类型
  - 隐藏 implementation + 隐藏消息类型
- 为避免误把“当前仍无法精确表达的 type 形态”静默漏掉，生成器仍保留旧的本地回退辅助分支：
  - 当闭包 type arguments 含有当前 helper 还无法精确重建的形态（本轮用“隐藏元素类型数组”做回归）时
  - 仍会退回 `RegisterReflectedHandler(...)` + `GetInterfaces()` 的安全路径
- 本轮顺手修复了两处实现细节问题：
  - `ImmutableArray` builder 在分支化后不再总是满容量，`MoveToImmutable()` 已收敛为 `ToImmutable()`
  - open generic contract 的生成文本已从 `IRequestHandler<TRequest, TResponse>` 修正为合法的 `IRequestHandler<,>` 形式

### 阶段：RP-028 验证

- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.SourceGenerators.Tests.Cqrs.CqrsHandlerRegistryGeneratorTests"`
  - 结果：通过
  - 明细：`7` 个测试全部通过
- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsHandlerRegistrarTests"`
  - 结果：通过
  - 明细：`6` 个测试全部通过

### 下一步

1. 继续评估 `RuntimeTypeReference` 是否可以递归覆盖数组/更复杂闭包类型，进一步缩小仍需 `GetInterfaces()` 的残余场景
2. 若 generator 侧只剩极少数复杂形态，再回到 dispatcher，重新判断 typed-invoker 去装箱优化是否值得进入实现阶段
3. 若后续继续参考 `Mediator`，优先找“生成器输出精确 closed service type，运行期只负责常量时间绑定”的对应模式

## 2026-04-16

### 阶段：review 收尾修正（递归 generic type reconstruction）

- 建立 `CQRS-REWRITE-RP-029` 恢复点
- 继续按“优先参考 Mediator 的生成期定结构思路，而不是新增运行时宽反射”推进 generator 主线：
  - 本轮没有新增任何新的 runtime 接口发现分支
  - 而是把 `RuntimeTypeReference` 从“直接类型 + 数组”扩成递归可组合结构
- `GFramework.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 现支持递归重建：
  - 直接可引用类型
  - 当前编译程序集内的隐藏类型定向 `Assembly.GetType(...)`
  - 数组类型的 `MakeArrayType(...)`
  - 构造泛型类型的 `MakeGenericType(...)`
- 因此以下场景已能直接生成 closed service type，而不再退回 `GetInterfaces()`：
  - `HiddenResponse[]`
  - `List<HiddenResponse>` 这类“可引用泛型定义 + 隐藏实参”
  - `HiddenEnvelope<string>` 这类“隐藏泛型定义 + 可重建实参”
- 实现过程中额外收敛了两处细节：
  - open generic definition 的直接引用改为通过 `ConstructUnboundGenericType()` 输出合法 `typeof(List<>)` / `typeof(IRequestHandler<,>)` 形式
  - `TryCreateRuntimeTypeReference(...)` 的失败分支改为显式 `null` 输出，避免新的递归结构继续引入可空警告
- `GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs` 已新增/更新快照回归：
  - 隐藏数组响应场景现在锁定为 `MakeArrayType()` 路径
  - 新增“隐藏泛型定义 + 常量实参”场景，锁定递归 `MakeGenericType()` 输出

### 阶段：RP-029 验证

- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.SourceGenerators.Tests.Cqrs.CqrsHandlerRegistryGeneratorTests"`
  - 结果：通过
  - 明细：`8` 个测试全部通过
- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsHandlerRegistrarTests"`
  - 结果：通过
  - 明细：`6` 个测试全部通过

### 下一步

1. 继续盘点仍必须落回 `GetInterfaces()` 的残余类型形态，优先判断是否只剩极少数低价值边角分支
2. 若 generator 命中面已足够高，再回到 dispatcher，评估 typed-invoker 去装箱是否值得作为下一条独立优化线
3. 若继续参考 `Mediator`，优先找“生成器直接输出 closed contract 表”的更强静态化模式，而不是继续给 runtime 补更多推断能力

## 2026-04-16

### 阶段：review 收尾修正（dispatcher typed invoker cache 去装箱）

- 建立 `CQRS-REWRITE-RP-030` 恢复点
- 在完成 `RuntimeTypeReference` 递归扩展后，本轮先对 dispatcher 做一轮低风险热路径优化：
  - 重新盘点后确认，当前 C# 可编译的常见 handler interface 形态里，generator 剩余 `GetInterfaces()` 回退已接近低价值尾部
  - 因此本轮不再继续堆新的 runtime 类型推断分支，而是转回 dispatcher，落实此前待定的 typed-invoker 去装箱优化
- `GFramework.Cqrs/Internal/CqrsDispatcher.cs` 已将 request 两条调用路径改为按 `TResponse` 分层的强类型委托缓存：
  - `RequestInvokerCache<TResponse>` 负责无 pipeline 的 request handler 调用缓存
  - `RequestPipelineInvokerCache<TResponse>` 负责带 pipeline 的 request 调用缓存
  - 缓存键从 `(RequestType, ResponseType)` 收敛为当前 `TResponse` 层内的 `RequestType`
  - `InvokeRequestHandlerAsync<TRequest, TResponse>(...)` 与 `InvokeRequestPipelineAsync<TRequest, TResponse>(...)` 现直接返回 `ValueTask<TResponse>`，不再通过 `ValueTask<object?>` 桥接 request 结果
- 这一轮的实现约束是：
  - 不改变 notification / stream 现有缓存结构
  - 不改变公开 runtime 契约与 observable dispatch 语义
  - 只减少 request 热路径里 value-type 响应的装箱/拆箱成本
- `GFramework.Cqrs.Tests/Cqrs/CqrsDispatcherCacheTests.cs` 已同步更新：
  - 原有“一次建缓存，多次复用”的 service-type / invoker 回归已适配新的泛型嵌套缓存结构
  - 新增 `Dispatcher_Should_Cache_Request_Invokers_Per_Response_Type()`，锁定 `int` 与 `string` 响应会分别命中各自的 request invoker cache
  - `SetUp()` 现在会显式清空 dispatcher 静态缓存，避免跨测试共享进程级状态导致缓存计数断言漂移

### 阶段：RP-030 验证

- `dotnet test GFramework/GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsDispatcherCacheTests|FullyQualifiedName~GFramework.Cqrs.Tests.Mediator.MediatorArchitectureIntegrationTests|FullyQualifiedName~GFramework.Cqrs.Tests.Mediator.MediatorComprehensiveTests|FullyQualifiedName~GFramework.Cqrs.Tests.Mediator.MediatorAdvancedFeaturesTests"`
  - 结果：通过
  - 明细：`47` 个测试全部通过

### 下一步

1. 回到 generator 主线，确认当前剩余 `GetInterfaces()` 本地回退是否只服务于极少数几乎不可达的边角形态
2. 若该尾部分支确实价值有限，优先评估是否直接推进“生成器输出 closed contract 表”的更强静态化方案
3. 若后续还要继续做 dispatcher 微优化，再单独评估 notification / stream 路径是否存在同等级别、且有明确收益的可观测热点

## 2026-04-16

### 阶段：review 收尾修正（generator mixed registration composition）

- 建立 `CQRS-REWRITE-RP-031` 恢复点
- 在回到 generator 主线后，确认当前更值得优先修正的问题不是继续扩 `RuntimeTypeReference`，而是生成器输出粒度过粗：
  - `TransformHandlerCandidate(...)` 已能把同一 implementation 上的不同 handler interface 分流到 direct / reflected-implementation / precise-reflected 三条路径
  - 但原 `GenerateSource(...)` 仍按 implementation 做互斥分支选择
  - 因此一旦同一个 implementation 同时命中多种路径，后面的分支会吞掉前面本已可静态绑定的注册
- `GFramework.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 已将该逻辑收敛为“按 handler interface 组合输出”：
  - direct-only implementation 仍保持原有直注册输出
  - 只要同一 implementation 上存在 reflected-implementation 或 precise-reflected 注册，生成器现在会先把三类注册步骤合并到一个有序列表
  - 合并后的步骤继续按 `HandlerInterfaceLogName` 做稳定排序，避免因为内部 bucket 化而偏离原先按接口名排序的生成顺序
  - 对可见 implementation，会复用 `typeof(ImplementationType)` 作为统一 implementation 变量
  - 对隐藏 implementation，则继续只做一次 `Assembly.GetType(...)`，再在同一块里完成可见接口直绑与精确 `MakeGenericType(...)` 注册
- 这一轮的收敛点是：
  - 不新增新的 runtime 宽反射分支
  - 也不改变“真正 unresolved 形态仍可整实现退回 `RegisterReflectedHandler(...)`”的安全边界
  - 只修正“本可静态绑定的接口被同实现上的另一条更窄路径连带降级”的生成器粒度问题
- `GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs` 已新增两条快照回归：
  - `Generates_Mixed_Direct_And_Precise_Registrations_For_Same_Implementation()`
  - `Generates_Mixed_Reflected_Implementation_And_Precise_Registrations_For_Same_Implementation()`
  - 这两条测试分别锁定“可见 implementation”与“隐藏 implementation”上的混合路径输出，防止后续再退回 implementation 级互斥分支

### 阶段：RP-031 验证

- `dotnet test GFramework/GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.SourceGenerators.Tests.Cqrs.CqrsHandlerRegistryGeneratorTests"`
  - 结果：通过
  - 明细：`10` 个测试全部通过

### 下一步

1. 继续盘点当前剩余必须整实现退回 `RegisterReflectedHandler(...)` 的类型形态，确认是否真的只剩低价值尾部分支
2. 若该判断成立，下一步优先评估“生成器直接输出 closed contract 表”的静态化方案，而不是继续给 runtime 增加推断辅助
3. 若要继续参考 `Mediator`，重点比较其生成代码是否已经把“多接口 implementation 的 contract 表”静态化到比当前更细的粒度

## 2026-04-16

### 阶段：review 收尾修正（partial full-fallback 与 generated-registry accessibility）

- 建立 `CQRS-REWRITE-RP-032` 恢复点
- 在继续盘点 generator 剩余 full-fallback 后，确认这一轮真正需要同时收敛的是两件事：
  - 先前“是否能在生成注册器里直接 `typeof(...)` 某个类型”的判断只看声明可见性，无法区分“当前语义上下文可见”和“生成注册器顶层上下文可直接书写”
  - 即使某个 implementation 上只剩一条未知接口，旧逻辑也会直接清空此前已收集注册，整实现退回 `GetInterfaces()` 发现
- `GFramework.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 已将可访问性判断改为：
  - `Compilation.IsSymbolAccessibleWithin(symbol, compilation.Assembly, null)`
  - 这使生成器现在按“顶层 generated registry 所在上下文”判断能否直接引用类型，而不再把 `protected internal` 之类的声明可见类型粗暴视为一定可直引
  - 同程序集私有/隐藏类型仍继续走 metadata-name lookup，不影响当前已落地的精确定向注册路径
- `TransformHandlerCandidate(...)` 已不再在遇到单个未知接口时直接 `return` 丢掉已收集注册：
  - 改为设置 `RequiresRuntimeInterfaceDiscovery = true`
  - 继续把同实现上仍可确定的 direct / reflected-implementation / precise-reflected 注册收集完整
- 生成代码模型已新增“局部补洞”路径：
  - `ImplementationRegistrationSpec` 现携带 `RequiresRuntimeInterfaceDiscovery`
  - 当同一 implementation 仍残留未知接口时，生成代码会先建立 `knownServiceTypes`
  - 先完成所有已知 service type 的注册，并把这些 service type 加入 `knownServiceTypes`
  - 再调用 `RegisterRemainingReflectedHandlerInterfaces(...)`，只对 `GetInterfaces()` 中尚未出现在 `knownServiceTypes` 的 supported handler interface 做补注册
- 这一轮的结果是：
  - full-fallback 仍保留为最后的安全边界
  - 但已从“整实现吞掉已知注册”收敛为“implementation 内部局部补洞”
  - 同时修正了外部 `protected internal` 嵌套类型这类边界下，旧判断可能误生成不可编译 `typeof(...)` 的风险
- 为了覆盖这个真实边界，测试基础设施也做了最小扩展：
  - `GFramework.SourceGenerators.Tests/Core/GeneratorTest.cs` 新增带 `AdditionalReferences` 的重载
  - `MetadataReferenceTestBuilder.cs` 已新增，可把内存源码编成元数据引用

### 阶段：RP-032 验证

- `dotnet test GFramework/GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.SourceGenerators.Tests.Cqrs.CqrsHandlerRegistryGeneratorTests"`
  - 结果：通过
  - 明细：`11` 个测试全部通过

### 下一步

1. 继续盘点当前仍必须依赖 `RegisterRemainingReflectedHandlerInterfaces(...)` 的真实类型形态，确认是否已经收敛到少量低价值边角路径
2. 若该判断成立，下一步优先评估“生成器直接输出 closed contract 表”的更强静态化方案
3. 若继续参考 `Mediator`，重点比较其对“多接口 implementation + 少量未知 contract”的生成期建模是否还比当前更细

## 2026-04-16

### 阶段：review 小修（RP-033）

- 接收四条 follow-up 建议后，先确认都命中当前仓库：
  - dispatcher 缓存清理遗漏了 `RequestPipelineInvokerCache<string>`
  - generator 可访问性判断默认分支缺少显式假设说明
  - `MetadataReferenceTestBuilder` 每次都会重建运行时元数据引用集合
  - `RunGenerator(...)` 的编译错误断言消息上下文偏少
- 已完成对应实现：
  - 在 `ClearDispatcherCaches()` 中补齐 `string` pipeline invoker cache 清理
  - 为 `CanReferenceFromGeneratedRegistry(...)` 默认分支补上“暂按可引用处理，后续可收紧”的注释
  - 用 `Lazy<ImmutableArray<MetadataReference>>` 缓存 `TRUSTED_PLATFORM_ASSEMBLIES` 解析结果
  - 让生成器测试失败消息输出完整 `Diagnostic.ToString()` 列表
- 本轮属于 review follow-up 小修，没有改动公开 API 或运行时主路径语义；核心目标是减少测试间静态状态泄漏风险，并提升 source-generator 测试的调试效率
- 定向验证已完成：
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.SourceGenerators.Tests.Cqrs.CqrsHandlerRegistryGeneratorTests"`
    - 结果：通过，`11` 个测试全部通过
  - `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsDispatcherCacheTests"`
    - 结果：通过，`2` 个测试全部通过

### 下一步

1. 回到 generator 主线，继续盘点当前仍必须依赖 `RegisterRemainingReflectedHandlerInterfaces(...)` 的真实类型形态
2. 若该尾部分支价值确实很低，优先评估“生成器直接输出 closed contract 表”的更强静态化方案

## 2026-04-16

### 阶段：review 小修（RP-034）

- 建立 `CQRS-REWRITE-RP-034` 恢复点
- 为了把“继续盘点 residual runtime interface discovery 形态”从人工读代码改成可观察输出，
  `GFramework.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 现在会在每个仍需
  `RegisterRemainingReflectedHandlerInterfaces(...)` 的 implementation 注册块前，生成显式注释：
  `// Remaining runtime interface discovery target: ...`
- 注释内容直接复用对应 closed handler interface 的日志显示名，因此不会引入额外 runtime 逻辑，也能和现有 debug 日志口径保持一致
- `GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs` 已补齐断言，锁定当前已知尾部分支案例
  `IRequestHandler<Dep.VisibilityScope.ProtectedRequest, Dep.VisibilityScope.ProtectedResponse[]>`
  会出现在生成注释中，避免后续又退回“只知道走了 fallback，但不知道具体是哪条 contract”的状态
- 本轮没有改动公开 API，也没有扩大 runtime 反射面；目标仅是为下一步 closed-contract 评估提供稳定盘点入口
- 补充验证已完成：
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.SourceGenerators.Tests.Cqrs.CqrsHandlerRegistryGeneratorTests"`
    - 结果：通过，`11` 个测试全部通过
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Core.Tests.Ioc.MicrosoftDiContainerTests|FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureModulesBehaviorTests|FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureAdditionalCqrsHandlersTests|FullyQualifiedName~GFramework.Core.Tests.Architectures.RegistryInitializationHookBaseTests"`
    - 结果：通过，`52` 个测试全部通过

### 下一步

1. 基于生成注释清单，继续归类当前 residual contract 是否都集中在“外部程序集不可直引类型”这类低频边角
2. 若判断成立，下一步直接设计 closed-contract 表最小原型，优先验证能否覆盖外部程序集 `protected internal` 嵌套类型，而不是继续扩大 `GetInterfaces()` 补洞

## 2026-04-16

### 阶段：主线推进（RP-035）

- 建立 `CQRS-REWRITE-RP-035` 恢复点
- 已将原 `GFramework.SourceGenerators` 继续按目标模块拆分为：
  - `GFramework.Core.SourceGenerators`
  - `GFramework.Core.SourceGenerators.Abstractions`
  - `GFramework.Cqrs.SourceGenerators`
  - `GFramework.Game.SourceGenerators`
- `GFramework.SourceGenerators.Tests` 已改为同时引用新的 `Core/Cqrs/Game` 生成器项目；
  `GFramework.Core.Tests` 与 `GFramework.Game.Tests` 也已切到新的本地 analyzer 引用链，
  其中 `Game.Tests` 额外改为导入 `GFramework.Game.SourceGenerators.targets`
- `GFramework.Cqrs.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 已继续收敛 residual tail：
  - `RuntimeTypeReferenceSpec` 现可表达“外部程序集 + metadata name”的定向 lookup
  - 对外部程序集中的不可直引 `protected nested` 类型，不再只能退回 `RegisterRemainingReflectedHandlerInterfaces(...)`
  - 当前已验证 `IRequestHandler<Dep.VisibilityScope.ProtectedRequest, Dep.VisibilityScope.ProtectedResponse[]>`
    这类案例会生成 `ResolveReferencedAssemblyType(...)` 精确查找，而不是保留 residual 注释 + `GetInterfaces()` 补洞
- 已删除不再保留的公开 `Mediator` 兼容层：
  - `RegisterMediatorBehavior<TBehavior>()`
  - `ContextAwareMediatorExtensions`
  - `ContextAwareMediatorCommandExtensions`
  - `ContextAwareMediatorQueryExtensions`
  - `MediatorCoroutineExtensions`
- 相关兼容测试 `MediatorCompatibilityDeprecationTests` 已删除；
  `ArchitectureModulesBehaviorTests` 与 `RegistryInitializationHookBaseTests` 已同步移除对旧别名的断言/测试替身实现
- 文档已做最小同步：
  - `docs/zh-CN/core/cqrs.md` 不再声明旧 `Mediator` 兼容别名
  - `CLAUDE.md` 已改为统一使用 `Cqrs` 命名入口

### 阶段：RP-035 验证

- `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release`
  - 结果：通过
- `dotnet build GFramework.Core.SourceGenerators/GFramework.Core.SourceGenerators.csproj -c Release`
  - 结果：通过
- `dotnet build GFramework.Game.SourceGenerators/GFramework.Game.SourceGenerators.csproj -c Release`
  - 结果：通过
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.SourceGenerators.Tests.Cqrs.CqrsHandlerRegistryGeneratorTests"`
  - 结果：通过
  - 明细：`11` 个测试全部通过
- `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureModulesBehaviorTests|FullyQualifiedName~GFramework.Core.Tests.Architectures.RegistryInitializationHookBaseTests"`
  - 结果：通过
  - 明细：`8` 个测试全部通过
- `dotnet build GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release`
  - 结果：通过
  - 备注：仍有一条既有 `GF_ContextRegistration_003` warning，属于测试项目里的静态注册分析提示，不是本轮拆分或 CQRS 主线回归

### 下一步

1. 继续盘点当前 residual `RegisterRemainingReflectedHandlerInterfaces(...)` 是否还剩下“外部程序集定向 lookup”无法覆盖的真实类型形态
2. 若只剩极少数低价值边角，直接评估 closed-contract 表最小原型，优先彻底消掉 generator 里的 implementation 级 `GetInterfaces()` 尾分支
3. 视需要再决定是否把现仍保留旧 namespace 的 source-generator abstractions/public docs 一并统一改名到 `GFramework.Core.SourceGenerators*`

## 2026-04-17

### 阶段：主线推进（RP-036）

- 建立 `CQRS-REWRITE-RP-036` 恢复点
- `GFramework.Cqrs.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 已删除 generated registry 内部的 implementation 级 `GetInterfaces()` 尾分支：
  - 不再生成 `RegisterRemainingReflectedHandlerInterfaces(...)`
  - 不再生成 `knownServiceTypes` / `Remaining runtime interface discovery target` 注释 / `GetRuntimeTypeDisplayName(...)` 等仅服务于该尾分支的辅助代码
  - 当前合法 C# closed handler contract 统一收敛到 direct / reflected-implementation / precise-reflected 三类注册路径
- 为保持未来未知 Roslyn 类型形态下的保守正确性，生成器现改为：
  - 若仍有无法编码进 `RuntimeTypeReferenceSpec` 的 handler contract，则保留已知静态注册
  - 同时通过程序集级 `CqrsReflectionFallbackAttribute` 对具体 handler implementation 输出 targeted fallback metadata
  - 若当前 runtime 合同不提供该 marker，则直接放弃生成 registry，避免静默漏注册
- `GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs` 已为多程序集 `protected internal` 嵌套类型场景补齐显式断言：
  - 锁定不会重新生成 `RegisterRemainingReflectedHandlerInterfaces(...)`
  - 锁定不会重新出现 `Remaining runtime interface discovery target` 注释
  - 继续保持 `ResolveReferencedAssemblyType(...)` 精确 lookup 输出

### 阶段：RP-036 验证

- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.SourceGenerators.Tests.Cqrs.CqrsHandlerRegistryGeneratorTests"`
  - 结果：通过
  - 明细：`11` 个测试全部通过

### 下一步

1. 继续收敛 CQRS runtime 热路径里剩余的反射绑定点，优先盘点 dispatcher / registrar 是否还有可转成静态缓存或强类型委托的分支
2. 视需要补做 source-generator 命名空间 / 文档收口，决定是否把仍残留旧 `GFramework.SourceGenerators*` 表述的公开文档一并统一到新的模块名

## 2026-04-17

### 阶段：主线推进（RP-037）

- 建立 `CQRS-REWRITE-RP-037` 恢复点
- `GFramework.Cqrs/Internal/CqrsDispatcher.cs` 已继续收敛 dispatcher 热路径里的首次反射绑定与重复缓存命中：
  - notification 路径由分散的 `NotificationHandlerServiceTypes + NotificationInvokers` 合并为 `NotificationDispatchBindings`
  - stream 路径由分散的 `StreamHandlerServiceTypes + StreamInvokers` 合并为 `StreamDispatchBindings`
  - request 路径由 `RequestServiceTypes + RequestInvokerCache<TResponse> + RequestPipelineInvokerCache<TResponse>` 收敛为 `RequestDispatchBindingCache<TResponse>`
- 新的 dispatch binding 会把服务类型与强类型 invoker 委托绑定到同一缓存项：
  - 首次命中仍只做一次必要的 `MakeGenericType(...)` / `MakeGenericMethod(...)` / `CreateDelegate(...)`
  - 后续 `SendAsync(...)` / `PublishAsync(...)` / `CreateStream(...)` 会减少热路径上的重复字典查询
  - request 绑定继续按 `TResponse` 分层，不回退到 `object` 结果桥接
- `GFramework.Cqrs.Tests/Cqrs/CqrsDispatcherCacheTests.cs` 已同步更新：
  - 断言从旧的 service-type / invoker 分散缓存切换为新的 dispatch binding 缓存结构
  - 继续锁定 request/no-pipeline、request/with-pipeline、notification、stream 四条路径都是“一次建绑定，多次复用”
  - 继续锁定 request 绑定按 `int` / `string` 等不同响应类型分层缓存

### 阶段：RP-037 验证

- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsDispatcherCacheTests"`
  - 结果：通过
  - 明细：`2` 个测试全部通过
- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsDispatcherCacheTests|FullyQualifiedName~GFramework.Cqrs.Tests.Mediator.MediatorComprehensiveTests|FullyQualifiedName~GFramework.Cqrs.Tests.Mediator.MediatorArchitectureIntegrationTests|FullyQualifiedName~GFramework.Cqrs.Tests.Mediator.MediatorAdvancedFeaturesTests"`
  - 结果：通过
  - 明细：`47` 个测试全部通过

### 下一步

1. 继续盘点 `CqrsHandlerRegistrar` 初始化路径里剩余的 attribute 读取、registry 实例化与 fallback metadata 解析反射，评估是否值得再做进程级静态缓存
2. 若 registrar 冷路径收益有限，再回到 source-generator 命名空间 / 文档收口，把仍残留旧 `GFramework.SourceGenerators*` 表述的公开文档统一到新模块名

## 2026-04-17

### 阶段：主线推进（RP-038）

- 建立 `CQRS-REWRITE-RP-038` 恢复点
- `GFramework.Cqrs/Internal/CqrsHandlerRegistrar.cs` 已把 registrar 冷路径里重复出现的程序集反射前置解析收敛为进程级缓存：
  - `AssemblyMetadataCache` 复用 generated registry attribute 与 reflection fallback metadata 的分析结果
  - `RegistryActivationMetadataCache` 复用 registry 类型是否实现 `ICqrsHandlerRegistry`、是否抽象、是否存在可用无参构造等激活分析
  - `LoadableTypesCache` 复用未命中 generated registry 时的 `GetTypes()` / `ReflectionTypeLoadException` 恢复结果
- 为避免 `Assembly` 在 mock/proxy 场景下的自定义相等语义干扰缓存命中，上述程序集级缓存统一改为引用相等键
- 运行时行为保持不变：
  - generated registry 仍优先于整程序集扫描
  - fallback metadata 仍先消费显式 `Type`，再消费 type-name lookup
  - 没有 generated registry 时仍按稳定排序扫描可加载 handlers
- `GFramework.Cqrs.Tests/Cqrs/CqrsHandlerRegistrarTests.cs` 已新增两条缓存回归：
  - 同一程序集对象跨两个容器重复接入时，程序集级 registry/fallback metadata 与 `Assembly.GetType(...)` 只解析一次
  - 同一程序集对象在 full-scan 路径下跨两个容器重复接入时，`GetTypes()` 只执行一次，且两个容器都能复用首次恢复出的可加载 handler 集合

### 阶段：RP-038 验证

- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsHandlerRegistrarTests"`
  - 结果：通过
  - 明细：`8` 个测试全部通过
- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsHandlerRegistrarTests|FullyQualifiedName~GFramework.Cqrs.Tests.Mediator.MediatorComprehensiveTests|FullyQualifiedName~GFramework.Cqrs.Tests.Mediator.MediatorArchitectureIntegrationTests|FullyQualifiedName~GFramework.Cqrs.Tests.Mediator.MediatorAdvancedFeaturesTests"`
  - 结果：通过
  - 明细：`53` 个测试全部通过
- `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureAdditionalCqrsHandlersTests"`
  - 结果：通过
  - 明细：`2` 个测试全部通过

### 下一步

1. 评估 registrar 冷路径里剩余的 registry 实例创建本身是否值得继续压缩；若收益有限，主线可转回 source-generator 命名空间 / 文档收口
2. 盘点公开文档和说明文件中仍残留的旧 `GFramework.SourceGenerators*` / 旧 CQRS 表述，决定是否在下一恢复点统一收口

## 2026-04-17

### 阶段：主线推进（RP-039）

- 建立 `CQRS-REWRITE-RP-039` 恢复点
- 结合 RP-038 后的冷路径现状，判断 registrar 继续下压的边际收益已经明显变小，主线转向公开入口文档收口
- 已完成以下公开入口文档的最小同步：
  - `README.md`
  - `CLAUDE.md`
  - `docs/zh-CN/core/cqrs.md`
  - `docs/zh-CN/source-generators/index.md`
- 本轮收口重点：
  - 根 README 的模块表、仓库结构与包选择说明已补入 `GFramework.Cqrs` / `GFramework.Cqrs.Abstractions`
  - 根 README 与 `CLAUDE.md` 已从旧的单体 `GFramework.SourceGenerators` 模块表述切到当前的 `Core/Game/Godot/Cqrs SourceGenerators` 家族
  - `docs/zh-CN/core/cqrs.md` 的代码示例命名空间已切到当前 `GFramework.Cqrs*` 路径，不再继续展示旧 `GFramework.Core.CQRS*` / `GFramework.Core.Abstractions.CQRS*`
  - `docs/zh-CN/core/cqrs.md` 的迁移说明已收敛为“直接使用 `RegisterCqrsPipelineBehavior<TBehavior>()`”，不再把已删除的 `RegisterMediatorBehavior<TBehavior>()` 写成仍可替换的并存入口
  - `docs/zh-CN/source-generators/index.md` 已从“单一 `GFramework.SourceGenerators` 包”口径改写为“按模块拆分的 Source Generators 家族”口径，同时保留“旧聚合包不存在”的说明
- 期间尝试把 `README.md` / `CLAUDE.md` 分派给 worker 并行处理，但 subagent 上游接口报错，最终由主 agent 本地完成，不影响结果正确性

### 阶段：RP-039 验证

- 对以下文件执行定向全文扫描，确认已不存在旧公开入口表述：
  - `README.md`
  - `CLAUDE.md`
  - `docs/zh-CN/core/cqrs.md`
  - `docs/zh-CN/source-generators/index.md`
- 扫描结果：
  - 已无旧 `GFramework.Core.CQRS*` / `GFramework.Core.Abstractions.CQRS*` 示例命名空间残留
  - 已无旧 `GFramework.SourceGenerators` 聚合模块图或聚合包定位残留
  - 保留的旧名仅存在于迁移说明和“不存在旧聚合包”的显式说明中，属于有意保留

### 下一步

1. 若继续文档主线，扩大扫描范围到 `docs/zh-CN/**` 与根 README 之外的说明文件，逐步清理更多历史 `GFramework.SourceGenerators*` / `Mediator` 表述
2. 若回到实现主线，可再次评估 registrar 冷路径是否还值得继续压缩，重点看 registry 实例创建本身的收益是否足以覆盖复杂度

## 2026-04-18

### 阶段：规则与参考源收口（RP-040）

- 建立 `CQRS-REWRITE-RP-040` 恢复点
- 已确认用户将当前 CQRS 主线对照用的 `Mediator` 源码放入仓库内 `ai-libs/Mediator`
- `AGENTS.md` 已新增仓库规则：
  - `ai-libs/` 为第三方源码参考区
  - `ai-libs/**` 默认只读，不允许作为常规实现改动目标
  - 后续计划、trace、评审与设计说明引用第三方实现时，应优先写明仓库内路径
- CQRS 迁移跟踪文档已同步把“参考 Mediator 成熟实现”的当前执行语义收口为“优先参考 `ai-libs/Mediator`”

### 验证

- `dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release`
  - 结果：通过
  - 备注：存在既有 `MA0051` 与 `MA0158` analyzer warnings，无新增构建错误
- `rg -n "ai-libs/|ai-libs/Mediator|只读|第三方源码参考区|第三方项目源码副本" AGENTS.md ai-plan/public/cqrs-rewrite/todos/cqrs-rewrite-migration-tracking.md ai-plan/public/cqrs-rewrite/traces/cqrs-rewrite-migration-trace.md`
  - 结果：通过
  - 备注：三处文档都已命中 `ai-libs` 只读规则与 `ai-libs/Mediator` 参考路径

### 下一步

1. 继续推进 CQRS 子系统增强时，把 `ai-libs/Mediator` 作为唯一仓库内参考源使用
2. 若后续需要变更 `ai-libs/Mediator` 内容，单独建“第三方快照同步”任务，不与 GFramework 实现改动共用一个提交

## 2026-04-18

### 阶段：文档主线推进（RP-041）

- 建立 `CQRS-REWRITE-RP-041` 恢复点
- 结合 `feat/cqrs-optimization` 当前主线与 RP-039 的文档收口结果，继续扩大扫描范围到 `docs/zh-CN/**`
- 盘点确认：
  - 旧公开示例主要残留在大量文档代码块中的 `using GFramework.SourceGenerators.Abstractions.*;`
  - 当前真实公开命名空间已经是 `GFramework.Core.SourceGenerators.Abstractions.*`
  - `GFramework.SourceGenerators.Common` 等命名仍对应仓库内部项目，不应作为本轮批量替换目标
- 已批量把文档示例命名空间收口到当前公开路径，并额外手工修正三处叙述性旧口径：
  - `docs/zh-CN/source-generators/logging-generator.md`
  - `docs/zh-CN/source-generators/context-get-generator.md`
  - `docs/zh-CN/api-reference/index.md`

### 验证

- 执行：
  - `rg -n "using GFramework\\.SourceGenerators\\.Abstractions\\.|### GFramework\\.SourceGenerators|GFramework\\.SourceGenerators 自动生成|`GFramework\\.SourceGenerators` 现在还会分析" docs/zh-CN`
- 结果：
  - 通过
  - `docs/zh-CN/**` 已不再残留上述旧公开命名空间与旧聚合说明

### 下一步

1. 若继续文档主线，优先再扫 `docs/zh-CN/api-reference` 与教程入口页，补齐仍过时的 API / 命名空间表述
2. 若切回实现主线，重新盘点 `GFramework.Cqrs` 当前剩余的反射绑定点，并只选择收益明确的优化项推进

## 2026-04-19

### 阶段：PR review 技能接入与 PR-253 follow-up（RP-042）

- 建立 `CQRS-REWRITE-RP-042` 恢复点
- 新增项目级 skill `.codex/skills/gframework-pr-review/`：
  - 暗号为 `$gframework-pr-review`
  - 使用 Windows Git 解析当前分支，并通过 GitHub PR API 定位当前分支对应的 PR
  - 通过 GitHub issue comments / reviews / review comments API 提取 `Summary by CodeRabbit`、最新 head
    commit review threads、`Failed checks` 与 CTRF 测试结果
  - 不再把重型 PR HTML 页面作为主数据源，只在调试或兼容场景下保留为兜底思路
  - 不依赖 `gh` CLI，也不要求登录态；脚本会显式绕过当前 shell 中失效的代理变量
- 用新脚本验证了 PR `#253` 的当前状态：
  - latest head commit review threads 已可直接从 API 提取；在远端最新提交未更新前，当前仍显示 4 条 open
    threads，其中 2 条落在 `fetch_current_pr_review.py`、2 条落在 `ai-plan/public/cqrs-rewrite/todos/cqrs-rewrite-migration-tracking.md`
  - PR 页面当前无 `Failed Tests`，CTRF 测试报告显示 `2103 passed / 0 failed`
  - `Failed checks` 当前可稳定提取到 `Docstring Coverage` warning；该项属于 PR 级文档注释覆盖率问题，不是 FPR
    skill 解析链路故障
- 已按 PR `#253` 的公开建议完成本地修正：
  - `gframework-boot` 的恢复 heuristics 改为“先检索 `ai-plan/`，再判定 `resume` 或 `recovery`”
  - `AGENTS.md` 将 `ai-libs/**` 观察写入 active plan/trace 的要求收窄到“多步/复杂任务或已有 active tracking document”
  - `Godot` 模板与 `IController` 文档注释中的旧
    `GFramework.SourceGenerators.Abstractions.Rule` 引用已收口到
    `GFramework.Core.SourceGenerators.Abstractions.Rule`

### 验证

- `python3 - <<'PY' ... ast.parse(...) ... PY`
  - 结果：通过
  - 备注：`fetch_current_pr_review.py` 语法正确，且避免了只读文件系统下写 `__pycache__` 的问题
- `python3 .codex/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --pr 253`
  - 结果：通过
  - 备注：成功通过 API-first 路径解析当前 PR 元数据、latest head commit review threads、`Docstring Coverage`
    warning 和 CTRF 测试报告
- `python3 .codex/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --branch feat/cqrs-optimization`
  - 结果：通过
  - 备注：验证 branch -> PR 解析也已摆脱 HTML 搜索
- `dotnet build GFramework.Core.Abstractions/GFramework.Core.Abstractions.csproj -c Release`
  - 结果：通过
  - 备注：`GFramework.Cqrs.Abstractions` 与 `GFramework.Core.Abstractions` 均成功构建，0 warning / 0 error
- `rg -n "GFramework\\.SourceGenerators\\.Abstractions\\.Rule" Godot GFramework.Core.Abstractions docs/zh-CN -g '*.cs' -g '*.md'`
  - 结果：通过
  - 备注：本轮目标范围内已无旧 `Rule` 命名空间残留

### 下一步

1. 若继续沿用当前 PR 驱动修复流程，可直接用 `$gframework-pr-review` 复查后续 PR 的 CodeRabbit 评论与测试状态
2. 若要验证本轮本地修正已经消除远端 latest head review threads，需要在提交并推送当前分支后重新执行 `$gframework-pr-review`

## 2026-04-19

### 阶段：PR review 复核与 FPR 输出澄清（RP-043）

- 建立 `CQRS-REWRITE-RP-043` 恢复点
- 复核 PR `#253` 时确认：
  - `ai-plan/public/cqrs-rewrite/todos/cqrs-rewrite-migration-tracking.md` 的“下次恢复建议”仍把主线写成 `Phase 7`
  - 这与 tracking 顶部当前阶段 `Phase 8` 不一致，恢复时会造成执行顺序歧义
- 进一步检查 `$gframework-pr-review` 脚本后确认：
  - 该评论并非“未覆盖”，而是已出现在 latest head commit 的 open review threads 中
  - 我先前误把评论正文里可见的 `Addressed in commit ...` 文案当成了“已经本地验证修复”
  - 实际脚本只会在 CodeRabbit thread 带有隐藏 marker `<!-- <review_comment_addressed> -->` 时才将状态归类为 `addressed`
- 已据此完成两项修正：
  - 将 tracking 中的恢复主线更新为 `Phase 8（当前主线）`
  - 调整 `fetch_current_pr_review.py` 输出：当 open thread 里出现 `Addressed in commit ...` 文案但线程并未真正 closed/addressed 时，显式提示“仍需本地验证，不要视为已解决”

### 验证

- `sed -n '469,475p' ai-plan/public/cqrs-rewrite/todos/cqrs-rewrite-migration-tracking.md`
  - 结果：通过
  - 备注：恢复建议已切到 `Phase 8（当前主线）`
- `python3 -m py_compile .codex/skills/gframework-pr-review/scripts/fetch_current_pr_review.py`
  - 结果：通过
  - 备注：脚本语法正确

### 下一步

1. 推送当前分支后重新执行 `$gframework-pr-review`，确认 PR `#253` 的 latest head review threads 是否已收敛
2. 若后续仍保留 CodeRabbit 的 `Addressed in commit ...` 文案但 thread 继续 open，优先以 latest thread 状态和本地文件事实为准，不再按文案字面判定
