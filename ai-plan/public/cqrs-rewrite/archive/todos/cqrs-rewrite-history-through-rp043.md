# CQRS 重写迁移跟踪

## 目标

围绕 `GFramework` 当前的双轨 CQRS 现状，完成一轮以“去 Mediator 外部依赖”为目标的架构迁移：

- 将 `Mediator` 从 GFramework 公共 API 和运行时主路径中移除
- 基于 GFramework 自有抽象重建正式 CQRS runtime、行为管道和注册机制
- 保留 `EventBus` 作为框架级事件系统，不与 CQRS notification 混同
- 让 `CoreGrid-Migration` 直连本地 `GFramework`，作为真实迁移验证工程
- 为复杂迁移建立明确恢复点与进度追踪，避免上下文过长或中断后失去状态

## 当前恢复点

- 恢复点编号：`CQRS-REWRITE-RP-042`
- 当前阶段：`Phase 8`
- 当前焦点：
  - 已将迁移目标修正为 `Core = App Runtime`、`CQRS = 默认集成进去的可替换子系统`，停止继续追求 `Core` 对 `Cqrs` 的零依赖
  - 已完成 `Core` 侧 CQRS 实现细节泄漏盘点，并将容器内的 handler 注册去重/调度细节下沉到 `GFramework.Cqrs`
  - 已将 generator 从“整程序集回退”推进到“可见 handlers 走 `typeof(...)` 直注册，不可直接引用但可定位的 handlers 走 generated registry 内部定向 `Assembly.GetType(...)` 注册”
  - 已将 partial reflection fallback 从“命中 marker 后整程序集 `GetTypes()`”收敛到“优先消费显式 `Type` / type-name metadata；且对 generator 已能重新定位的隐藏 handlers 不再额外依赖 runtime fallback marker”
  - 已为手写/第三方程序集补齐正式的精确 fallback 入口：`CqrsReflectionFallbackAttribute` 现在既支持 `string` type-name 清单，也支持直接传入 `Type` 集合
  - 已将“隐藏 implementation 但 handler interface 仍可直接引用”的 generator 输出进一步收敛为“仅对 implementation 执行一次 `Assembly.GetType(...)`，随后直接按 `typeof(handlerInterface)` 注册”，不再为这类场景额外生成 `GetInterfaces()` / `IsSupportedHandlerInterface(...)` 辅助逻辑
  - 已将“隐藏 implementation + 隐藏但可精确重建的 handler interface”进一步收敛为“生成期记录 open generic contract 与 type arguments，运行期只做 implementation/type-argument 定向 lookup + `MakeGenericType(...)`”，常见私有嵌套 request 场景已不再需要 `GetInterfaces()` 接口发现
  - 已将 `RuntimeTypeReference` 继续扩成递归结构，可表达数组与构造泛型；`List<HiddenType>`、`HiddenEnvelope<string>` 这类闭包类型现在也能按生成期已知结构直接重建，进一步贴近 `Mediator` 那种“生成期定结构，运行期只做常量时间绑定”的模式
  - 已在 `CqrsDispatcher` 热路径补齐 service-type 缓存，减少 `PublishAsync` / `SendAsync` / `CreateStream` 中重复的 `MakeGenericType` 开销
  - 已将 `CqrsDispatcher` 的 invoker method-definition 查找收敛为静态一次解析，并补齐 request/no-pipeline、request/with-pipeline、notification、stream 四条缓存路径的统一回归
  - 已将 `CqrsDispatcher` 的 request / pipeline invoker 从 `ValueTask<object?>` 缓存改为按 `TResponse` 分层的强类型委托缓存，减少 value-type 响应在热路径上的装箱与额外拆箱
  - 已将 `CqrsDispatcher` 的 notification/request/stream 热路径进一步收敛为聚合 dispatch binding cache，把服务类型与强类型 invoker 合并到单一缓存项；
    request 路径继续按 `TResponse` 分层，并减少单次分发里的重复字典命中
  - 已将 `CqrsHandlerRegistrar` 的程序集级 attribute 读取、registry 激活分析与未命中 generated-registry 时的 `GetTypes()` 扫描收敛为进程级缓存；
    同一 `Assembly` 对象跨容器重复接入时不再重复做 registry metadata / fallback metadata / loadable-types 分析
  - 已完成一轮公开入口文档收口：`README.md`、`CLAUDE.md`、`docs/zh-CN/core/cqrs.md` 与 `docs/zh-CN/source-generators/index.md`
    现已统一到 `GFramework.Cqrs` 与分拆后的 `Core/Game/Godot/Cqrs SourceGenerators` 模块命名，不再把公开入口写成旧聚合 `GFramework.SourceGenerators`
  - 已将 `CqrsHandlerRegistryGenerator` 从“同一 implementation 只走一种注册分支”的互斥输出，收敛为“按 handler interface 粒度组合 direct / reflected-implementation / precise-reflected 注册，并继续按接口名稳定排序”；
    同一 handler implementation 上的可静态绑定接口不再因为另一条接口需要更窄路径而被一起拖进更粗回退
  - 已将 `CqrsHandlerRegistryGenerator` 的“生成注册器是否能直接引用类型”判断改为基于 Roslyn `Compilation.IsSymbolAccessibleWithin(...)`，不再仅靠声明可见性粗判；
    对“当前继承/语义上下文可见、但生成注册器顶层上下文不可直接写”的外部 `protected internal` 嵌套类型，现会直接进入精确 type lookup，而不是误生成不可编译的 `typeof(...)`
  - 已删除 generated registry 内部的 `RegisterRemainingReflectedHandlerInterfaces(...)` implementation 级 `GetInterfaces()` 尾分支；
    当前合法 C# closed handler contract 已统一收敛到 direct / reflected-implementation / precise-reflected 三类注册路径
  - 若未来再出现新的 Roslyn 类型形态暂时无法编码进 `RuntimeTypeReferenceSpec`，生成器现会退回到“保留已知静态注册 + 通过程序集级 `CqrsReflectionFallbackAttribute` 对具体 handler implementation 定向补反射”；
    若 runtime 合同不提供该 marker，则生成器保守地不输出 registry，避免静默漏注册
  - 已完成一轮 review follow-up 小修：补齐 dispatcher `string` pipeline cache 清理，给 generator 可访问性判断默认分支加上假设说明，并收敛多程序集 generator 测试基础设施的运行时引用缓存与编译错误诊断输出
  - 已完成三轮 review follow-up：补齐 `ICqrsRuntime` 异常/上下文约束文档，明确 `DefaultCqrsRegistrationService` 的线程安全假设，收窄 `MicrosoftDiContainer` 未冻结态的实例别名去重范围，提前固定/校验 CQRS 程序集枚举输入，并把一类隐藏 handler 从“implementation + interface 运行时发现”进一步压缩到“implementation-only lookup + direct interface registration”
  - 已将 `GFramework.SourceGenerators` 按目标模块继续拆分为 `GFramework.Core.SourceGenerators` / `GFramework.Core.SourceGenerators.Abstractions` / `GFramework.Cqrs.SourceGenerators` / `GFramework.Game.SourceGenerators`，并同步修正 solution、测试项目与 `Game` schema targets 的引用链
  - 已删除公开 `Mediator` 兼容壳层：`RegisterMediatorBehavior<TBehavior>()`、`ContextAwareMediator*Extensions` 与 `MediatorCoroutineExtensions`，不再继续维护旧命名兼容表面
  - 已将 generator 对“外部程序集不可直引 protected nested 类型”的 residual contract 从 `RegisterRemainingReflectedHandlerInterfaces(...)` 补洞推进到“按程序集名 + metadata name 定向 lookup”；`IRequestHandler<Dep.VisibilityScope.ProtectedRequest, Dep.VisibilityScope.ProtectedResponse[]>` 这类场景现在走精确 type lookup，而不再退回 implementation 级 `GetInterfaces()`
  - 当前已确认在 C# 可编译约束下，generator 主路径已不再需要 implementation 级 `GetInterfaces()` 回退；下一步可继续转向 dispatch/invoker 反射收敛，或补做 source-generator namespace/docs 收口
  - 已将本任务后续参考的 `Mediator` 源码收口到仓库内 `ai-libs/Mediator`；`ai-libs/**` 现作为第三方只读参考区，不纳入常规实现修改范围
  - 已开始把 `docs/zh-CN/**` 中残留的旧 `GFramework.SourceGenerators.Abstractions.*` 示例命名空间收口到当前
    `GFramework.Core.SourceGenerators.Abstractions.*`，避免文档继续指向不存在的旧公开命名空间
  - 已新增项目级 `$gframework-pr-review` skill，可在当前分支下直接抓取 GitHub PR 页面、提取 CodeRabbit 评论、
    `Failed checks` 与测试结果，不再依赖 `gh` CLI
  - 已按 PR `#253` 当前公开 review 信号修正 `gframework-boot` 的 `resume/recovery` 语义、收窄 `AGENTS.md`
    中 `ai-libs/**` 观察写入 active plan/trace 的触发条件，并补齐模板/接口注释中的旧 `Rule` 命名空间残留

## 本轮计划

### Phase 0：工作流基础

- [x] 在 `ai-plan/public/cqrs-rewrite/todos/` 建立本任务跟踪文档
- [x] 在 `ai-plan/public/cqrs-rewrite/traces/` 建立本任务追踪文档
- [x] 将恢复点 / trace / subagent 协作规范写入 `AGENTS.md`

### Phase 1：本地验证链路

- [x] 确认 `CoreGrid-Migration` 当前引用形态
- [x] 将 `CoreGrid-Migration` 从 NuGet 包切到本地 `GFramework` 工程引用
- [x] 让 `CoreGrid-Migration` 使用本地 Source Generator 而不是外部已发布版本
- [x] 验证本地引用链路至少能完成 restore / build

### Phase 2：CQRS 基础重建

- [x] 在 `GFramework.Core.Abstractions` 定义自有 CQRS 契约
- [x] 在 `GFramework.Core` 落地 dispatcher / handler registry / behavior pipeline
- [x] 清理 `IArchitectureContext` 中对 `Mediator.*` 的公共签名依赖
- [x] 设计 CQRS 模块启用方式，替代 `Configurator => AddMediator(...)`

### Phase 3：接入迁移

- [x] 迁移 `GFramework.Core.Cqrs.*` 基类到新契约
- [x] 迁移 `ContextAwareMediator*Extensions` 与协程扩展
- [x] 迁移 `CoreGrid-Migration/scripts/cqrs/**` 到新契约
- [x] 删除 `GameArchitecture.Configurator` 中的 `AddMediator(...)`

### Phase 4：收尾

- [x] 移除 `Mediator` 包依赖与相关测试/文档残留
- [x] 运行目标构建与测试
- [x] 记录剩余风险与下一恢复点
- [x] 根据 review 收紧 `AbstractStreamCommandHandler` 的生命周期 XML 文档
- [x] 将 `CqrsHandlerRegistrar` 容错回归改挂到 `RegisterHandlers` 真实入口
- [x] 提升 `CqrsTestRuntime` 反射绑定的签名鲁棒性并补齐 XML 文档
- [x] 将剩余 `Mediator` 兼容入口推进到正式弃用周期（隐藏 IntelliSense + 明确 future major 移除）
- [x] 落地 CQRS handler source-generator MVP，并保留运行时反射回退
- [x] 落地非默认程序集的显式 CQRS handler 注册入口，并避免重复程序集接入导致的重复 handler 映射

### Phase 5：模块边界再评估

- [x] 评估是否将 CQRS 拆分为独立的 abstractions 模块与 runtime 实现模块，并梳理 `GFramework.Core` 对其依赖倒置方案
- [x] 若拆分收益成立，输出包边界与迁移草案：
  - `GFramework.Cqrs.Abstractions`：消息契约、handler 契约、pipeline 契约、registry 契约
  - `GFramework.Cqrs`：dispatcher、runtime 注册、behavior 执行、source-generator 接入
  - `GFramework.Core`：改为依赖 CQRS 抽象或可选 runtime 接入，而不是继续承载全部 CQRS 实现
- [x] 评估拆分后的兼容影响：
  - `IArchitectureContext` / `ArchitectureBootstrapper` / `MicrosoftDiContainer` 的程序集依赖变化
  - 现有 `CoreGrid-Migration` 与其他消费端项目的引用路径变化
  - source-generator、AOT、冷启动、包发布与文档迁移成本

### Phase 6：拆分前置 seams

- [x] 在新 CQRS 抽象边界中定义 `ICqrsRuntime` / `ICqrsHandlerRegistrar` 等 runtime seam
- [x] 将 `ArchitectureContext` 从直接 `new CqrsDispatcher(...)` 改为依赖容器解析的 runtime 抽象
- [x] 将 `MicrosoftDiContainer.RegisterCqrsHandlersFromAssemblies(...)` 从直接调用 `CqrsHandlerRegistrar` 改为依赖 runtime 注册抽象
- [x] 保持默认架构程序集 + `GFramework.Core` 程序集 handler 自动接入行为不变
- [x] 跑通至少覆盖上述 seam 改造的定向测试

### Phase 7：项目骨架与类型迁移

- [x] 创建 `GFramework.Cqrs.Abstractions` 与 `GFramework.Cqrs` 项目骨架，并接入 solution / package graph
- [x] 将纯 `GFramework.Core.Abstractions/Cqrs/*` 契约迁移到新 abstractions 项目，同时保持现有命名空间不变
- [x] 将 CQRS 聚焦测试拆分到 `GFramework.Cqrs.Tests`，并在 `CI` 中纳入独立测试项目
- [x] 将 `ICqrsRuntime` / `ICqrsHandlerRegistry` / `CqrsHandlerRegistryAttribute` 的最终归属与循环依赖处理方案单独收敛
- [x] 收敛 `CqrsCoroutineExtensions` 的最终归属：
  - 结论改为“保留在 `GFramework.Core` 作为 App Runtime 对 CQRS 的协程桥接”，不再作为必须迁往 `GFramework.Cqrs` 的剩余项
- [x] 处理 `GFramework.SourceGenerators` 对 CQRS 抽象程序集元数据名的引用迁移
- [x] 跑通迁移后最小构建与回归

### Phase 8：方向修正后的收敛

- [x] 将 `ai-plan/migration/CQRS_MODULE_SPLIT_PLAN.md` 的目标边界正式改写为：
  - `GFramework.Core.Abstractions -> GFramework.Cqrs.Abstractions`
  - `GFramework.Core -> GFramework.Cqrs`
  - `Core` 默认集成 CQRS，但不依赖其细节结构
- [x] 将后续实现约束明确写入计划：
  - CQRS runtime / generator / 低反射增强阶段必须优先参考 `ai-libs/Mediator` 中已经成熟可用的实现，而不是完全从零重做
- [x] 盘点 `Core` 中仍可能泄漏 CQRS 实现细节的位置：
  - 直接实例化具体 runtime 类型
  - 直接依赖 generator 生成类型
  - 硬编码 handler/internal registry 结构
- [x] 将 partial reflection fallback 从“整程序集 `GetTypes()` 扫描”推进到“generator 输出精确 handler type-name 清单后，runtime 定向 `Assembly.GetType(...)` 补扫”
- [x] 为手写/第三方程序集补齐精确 fallback metadata 入口，避免这类场景只能依赖旧版空 marker 或脆弱的字符串约定
- [x] 为 `CqrsDispatcher` 补齐 notification/request/stream service-type 缓存，减少热路径 `MakeGenericType` 重复开销
- [x] 为 `CqrsDispatcher` 补齐 invoker method-definition 静态缓存，并把 request/no-pipeline、request/with-pipeline、notification、stream 四条缓存路径纳入统一回归
- [x] 将 `CqrsDispatcher` 的分散 service-type / invoker cache 收敛为按消息类别聚合的 dispatch binding cache，进一步减少热路径重复缓存查询
- [x] 将 `CqrsHandlerRegistrar` 的程序集级 metadata 分析与 loadable-types 扫描收敛为进程级缓存，减少跨容器重复初始化时的冷路径反射
- [x] 收口公开入口文档中的旧 SourceGenerators 聚合模块表述与旧 CQRS 示例命名空间
- [x] 将私有/不可直接引用 handler 从“generator 输出 fallback marker + runtime 补扫”推进到“generated registry 内部定向反射注册”，进一步压缩 runtime fallback 面积
- [ ] 将后续主线转向 CQRS 子系统增强：
  - 参考 `ai-libs/Mediator` 现有成熟实现
  - generator 覆盖面继续扩大
  - 减少 dispatch/invoker 路径的反射占比
  - package/facade/兼容层收敛

## 当前完成结果

- `GFramework.Cqrs.Abstractions` 与 `GFramework.Cqrs` 项目骨架已创建，并已加入 `GFramework.sln`。
- `GFramework.Core.Abstractions` 已新增到 `GFramework.Cqrs.Abstractions` 的项目引用。
- `GFramework.Cqrs/CqrsReflectionFallbackAttribute.cs` 已扩展为可承载精确 fallback handler 类型名清单；当清单为空时，仍保持旧版“整程序集补扫”的兼容语义。
- `GFramework.Cqrs/CqrsReflectionFallbackAttribute.cs` 现同时支持 `params string[]` 与 `params Type[]` 两种精确 fallback 声明方式，使手写/第三方程序集可以直接提供 handler 类型而不必再走字符串名称回查。
- `GFramework.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 现会在检测到不可直接引用、但仍可按元数据名从当前程序集重新定位的 concrete handler 时，直接在生成注册器内部输出定向 `Assembly.GetType(...)` 注册逻辑，不再为这些场景额外生成 fallback marker。
- `GFramework.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 已进一步将“隐藏 implementation，但 handler interface 仍可合法 `typeof(...)` 引用”的场景收敛为：
  - generated registry 只对 implementation 做一次 `Assembly.GetType(...)`
  - service type 继续使用 `typeof(IRequestHandler<...>)` / `typeof(INotificationHandler<...>)` / `typeof(IStreamRequestHandler<...>)`
  - 不再为该类场景生成 `GetInterfaces()` / `IsSupportedHandlerInterface(...)` / `GetRuntimeTypeDisplayName(...)` 等全反射辅助代码
- `GFramework.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 已继续把“handler interface 本身不可直接引用，但其 open generic contract 与 type arguments 仍可精确表达”的场景收敛为：
  - 生成期记录 `IRequestHandler<,>` / `INotificationHandler<>` / `IStreamRequestHandler<,>` 的 open generic contract
  - 对不可直引、但属于当前编译程序集的 type argument 输出定向 `Assembly.GetType(...)`
  - 运行期通过 `MakeGenericType(...)` 重建精确 service type 并直接注册到隐藏 implementation
  - 当前合法 closed contract 已不再需要 generated registry 内部的 `GetInterfaces()` 本地回退辅助逻辑；
    若未来出现新的未覆盖类型形态，则改由程序集级 targeted fallback 合同补位
- `GFramework.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 现已支持递归重建以下 `RuntimeTypeReference` 形态：
  - 直接 `typeof(...)` 可引用类型
  - 当前编译程序集内的隐藏类型定向 `Assembly.GetType(...)`
  - 数组类型 `T[]` / 多维数组的 `MakeArrayType(...)`
  - 构造泛型类型 `Generic<T1, T2, ...>` 的递归 `MakeGenericType(...)`
- 因此常见“隐藏消息类型嵌套在数组或泛型响应里”的 handler interface，已不再需要退回 `GetInterfaces()` 接口发现。
- `GFramework.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 已进一步修正同一 implementation 的注册组合策略：
  - direct registration、reflected-implementation registration 与 precise-reflected registration 不再互斥
  - 当一个 implementation 同时命中多种注册路径时，生成器会合并这些注册步骤，并继续按 `HandlerInterfaceLogName` 稳定排序输出
  - 这样可静态绑定的接口不会再因为同实现上的另一条接口需要更窄反射路径而被一起降级
- `GFramework.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 现已把“生成注册器顶层上下文是否能合法书写某个类型”改为通过 `Compilation.IsSymbolAccessibleWithin(symbol, compilation.Assembly, null)` 判定：
  - 该调整修正了此前把 `protected internal` 等声明可见类型误当作“可直接 `typeof(...)`”的粗判
  - 对当前程序集私有类型仍继续走 metadata-name 定向 lookup
  - 对外部程序集里只能通过继承语义可见、但生成注册器顶层上下文不可直接写的类型，现会正确进入精确的程序集定向 lookup 路径
- `GFramework.Cqrs.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 已删除 generated registry 内部的 implementation 级 `GetInterfaces()` 补洞辅助逻辑：
  - 不再生成 `RegisterRemainingReflectedHandlerInterfaces(...)`、`knownServiceTypes`、`GetRuntimeTypeDisplayName(...)` 等尾分支辅助代码
  - 对当前合法 closed handler contract，统一走 direct / reflected-implementation / precise-reflected 三类静态化路径
  - 若未来出现暂未被 `RuntimeTypeReferenceSpec` 表达的新类型形态，则仅通过程序集级 `CqrsReflectionFallbackAttribute` 对具体 handler implementation 定向补反射
- `GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs` 已新增两条混合场景快照回归：
  - 同一可见 implementation 同时包含 direct + precise registration
  - 同一隐藏 implementation 同时包含 reflected-implementation + precise registration
- `GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs` 已新增多程序集回归：
  - 通过额外元数据引用构造“外部基类携带 `protected internal` 嵌套 request/response 类型”的真实边界
  - 锁定生成器会保留已知直注册、输出 `ResolveReferencedAssemblyType(...)` 精确 lookup，且不会重新带回 `RegisterRemainingReflectedHandlerInterfaces(...)` 尾分支或对不可访问 protected-nested 类型生成 `typeof(...)`
- `GFramework.SourceGenerators.Tests/Core/GeneratorTest.cs` 与 `MetadataReferenceTestBuilder.cs` 已补齐多程序集测试基础设施，允许 CQRS generator 回归显式追加内存元数据引用。
- `GFramework.Cqrs/Internal/CqrsHandlerRegistrar.cs` 已改为优先消费 fallback metadata 中的显式 `Type` 集合，其次才按 type-name 清单定向 `Assembly.GetType(...)` 补扫；只有缺少精确元数据时才继续走整程序集 `GetTypes()` 扫描。
- `GFramework.Cqrs.Tests/Cqrs/CqrsHandlerRegistrarTests.cs` 已新增回归，分别锁定“string type-name fallback 不触发 `GetTypes()` 全量扫描”与“direct `Type` fallback 同时不触发 `GetType()` / `GetTypes()`”行为；`GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs` 已同步覆盖“隐藏 handler 由 generated registry 自行定向注册”与“无论 runtime 是否暴露 legacy fallback marker，均不再为该场景输出 marker”。
- `GFramework.Cqrs/Internal/CqrsDispatcher.cs` 已新增 notification/request/stream service-type 缓存，避免重复为同一消息类型组合执行 `MakeGenericType`。
- `GFramework.Cqrs/Internal/CqrsDispatcher.cs` 已将 invoker 对应的泛型方法定义查找收敛为静态缓存，避免每种新消息类型首次命中 invoker cache 时再次执行 `GetMethod(...)` 查找。
- `GFramework.Cqrs/Internal/CqrsDispatcher.cs` 已将 request/no-pipeline 与 request/with-pipeline 两条 invoker 路径收敛为按 `TResponse` 分层的强类型缓存：
  - `SendAsync<TResponse>(...)` 现在直接复用 `ValueTask<TResponse>` 委托
  - 不再通过 `object` 桥接承接 request 结果，减少 value-type 响应装箱
- `GFramework.Cqrs/Internal/CqrsDispatcher.cs` 已继续把 notification/request/stream 的分散 service-type / invoker 缓存收敛为聚合 dispatch binding cache：
  - `NotificationDispatchBindings` / `RequestDispatchBindingCache<TResponse>` / `StreamDispatchBindings` 会把服务类型与强类型调用委托绑定到单一缓存项
  - request 路径在保持按 `TResponse` 分层的同时，进一步减少一次分发中的重复字典查询
- `GFramework.Cqrs.Tests/Cqrs/CqrsDispatcherCacheTests.cs` 已补齐回归，额外锁定 request invoker 会按 `int` / `string` 等不同响应类型分层建缓存，并在 `SetUp` 中显式清空 dispatcher 静态缓存，避免跨测试污染导致断言漂移。
- `GFramework.Cqrs.Tests/Cqrs/CqrsDispatcherCacheTests.cs` 已新增/更新回归，统一锁定 dispatcher 对 request/no-pipeline、request/with-pipeline、notification、stream 的 service-type / invoker cache 都满足“一次建缓存，多次复用”。
- `GFramework.Cqrs/Internal/CqrsHandlerRegistrar.cs` 已新增三类进程级缓存：
  - 程序集级 `AssemblyMetadataCache`，复用 generated registry attribute 与 reflection fallback metadata 的分析结果
  - registry 类型级 `RegistryActivationMetadataCache`，复用接口/抽象态/无参构造激活分析
  - `LoadableTypesCache`，为未命中 generated registry 的程序集复用首次 `GetTypes()` / `ReflectionTypeLoadException` 恢复结果
- 上述缓存对 `Assembly` 统一使用引用相等键，避免 mock/proxy 或自定义 `Assembly.Equals(...)` 语义干扰缓存命中。
- `GFramework.Cqrs.Tests/Cqrs/CqrsHandlerRegistrarTests.cs` 已新增回归：
  - 锁定同一程序集对象跨两个容器重复接入时，`CqrsHandlerRegistryAttribute` / `CqrsReflectionFallbackAttribute` / `Assembly.GetType(...)` 只会解析一次
  - 锁定未命中 generated registry 时，同一程序集对象跨两个容器重复接入只会执行一次 `GetTypes()`，且两边都能复用首次恢复出的可加载 handler 集合
- `README.md`、`CLAUDE.md`、`docs/zh-CN/core/cqrs.md` 与 `docs/zh-CN/source-generators/index.md` 已完成最小同步：
  - 根 README 的模块表、仓库结构与安装指引已补入 `GFramework.Cqrs` 以及拆分后的 SourceGenerators 家族
  - `CLAUDE.md` 的依赖图与模块结构说明已从旧 `GFramework.SourceGenerators` 聚合表述改为 `Core/Game/Godot/Cqrs SourceGenerators`
  - `docs/zh-CN/core/cqrs.md` 的示例命名空间已切到当前 `GFramework.Cqrs*` 公开路径，并把迁移说明改成“直接改用 `RegisterCqrsPipelineBehavior<TBehavior>()`”
  - `docs/zh-CN/source-generators/index.md` 已从“单一 `GFramework.SourceGenerators` 包”口径改为“按模块拆分的 Source Generators 家族”口径
- 以下纯 CQRS 契约已从 `GFramework.Core.Abstractions/Cqrs/*` 迁移到 `GFramework.Cqrs.Abstractions/Cqrs/*`，并保持原命名空间不变：
  - `IRequest*` / `IStreamRequest*` / `INotification*`
  - `IPipelineBehavior`
  - `MessageHandlerDelegate`
  - `Unit`
  - `Command/Query/Request/Notification` 输入与标记契约
  - `ICqrsHandlerRegistrar`
- `GFramework.Cqrs.Abstractions/GlobalUsings.cs` 已补齐基础 system 命名空间，避免新项目在关闭 `ImplicitUsings` 约束下丢失 `ValueTask` / `CancellationToken` / `IAsyncEnumerable`。
- `GFramework.Cqrs.Tests` 已创建并加入 `GFramework.sln`，当前已承接以下 CQRS 聚焦测试：
  - `CqrsHandlerRegistrarTests`
  - `CqrsCoroutineExtensionsTests`
  - `MediatorAdvancedFeaturesTests`
  - `MediatorArchitectureIntegrationTests`
  - `MediatorComprehensiveTests`
- `GFramework.Cqrs.Tests/GlobalUsings.cs` 与 `Logging/TestLogger.cs` 已补齐，确保新测试项目不再隐式依赖 `GFramework.Core.Tests` 的编译上下文。
- `GFramework.Core.Tests/Ioc/MicrosoftDiContainerTests.cs` 已移除对已迁移测试类型的编译期耦合，改为复用本地最小 CQRS fixture 验证容器层程序集去重与重新注册行为。
- `GFramework.Cqrs/ICqrsRegistrationService.cs` 与 `Internal/DefaultCqrsRegistrationService.cs` 已新增：
  - 将 CQRS handler 程序集接入的稳定键去重与 registrar 调度从 `MicrosoftDiContainer` 下沉到 CQRS runtime 内部
  - `MicrosoftDiContainer.RegisterCqrsHandlersFromAssemblies(...)` 现只保留公开委托入口，不再直接维护 handler 注册细节状态
- `GFramework.Core/Services/Modules/CqrsRuntimeModule.cs` 与 `GFramework.Tests.Common/CqrsTestRuntime.cs` 已同步注册新的 `ICqrsRegistrationService`，确保生产路径与裸测试容器路径都通过同一协调服务接入 handler 程序集。
- `GFramework.Cqrs/CqrsReflectionFallbackAttribute.cs` 已新增并保留，作为“手写/第三方程序集或 generator 仍未直接覆盖的场景需要 runtime 补反射”的程序集级兼容入口。
- `GFramework.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 已从“发现任一不可见 handler 就整程序集不生成”收敛为：
  - 仍为可由生成代码合法引用的 handlers 生成 registry
  - 对私有/不可直接引用但可按元数据名重新定位的 handlers，在 generated registry 内部输出定向反射注册
  - runtime fallback marker 仅保留给手写 metadata 或未来仍无法由生成器自处理的真正残余回退场景；
    这些场景也不再通过 generated registry 内部 `GetInterfaces()` 尾分支处理
- `GFramework.Cqrs/Internal/CqrsHandlerRegistrar.cs` 已支持“generated registry + reflection fallback”组合路径：
  - 当程序集带有 `CqrsReflectionFallbackAttribute` 时，运行时会在执行 generated registry 后继续补一次 reflection 扫描
  - 反射补扫前会按 `ServiceType + ImplementationType` 去重，避免已由 generated registry 注册的映射重复写入
- 覆盖该行为的新增/更新测试已落地：
  - `GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs`
  - `GFramework.Cqrs.Tests/Cqrs/CqrsHandlerRegistrarTests.cs`
- 本轮盘点结论已明确：
  - `ArchitectureContext -> ICqrsRuntime` 属于合理 seam，不视为实现细节泄漏
  - `Core` 当前未直接依赖任何 generator 生成类型名
  - 仍值得继续收敛的点主要是 `ArchitectureBootstrapper` 默认 handler 程序集硬编码，以及更长线的 `IIocContainer` / `IArchitecture` CQRS 装配 API 暴露面
- `GFramework/.github/workflows/ci.yml` 已新增 `dotnet test GFramework.Cqrs.Tests ...`，使 CQRS 模块拆分后的测试在 PR CI 中继续被覆盖。
- `GFramework.Core/Cqrs/Internal/CqrsDispatcher.cs`、`CqrsHandlerRegistrar.cs` 与 `DefaultCqrsHandlerRegistrar.cs` 已物理迁移到 `GFramework.Cqrs` 项目，同时保持现有 `GFramework.Core.Cqrs.Internal` 命名空间不变，避免消费端源码感知程序集拆分。
- `GFramework.Core/Cqrs/Command/CommandBase.cs`、`Query/QueryBase.cs`、`Request/RequestBase.cs` 与 `Notification/NotificationBase.cs` 已迁移到 `GFramework.Cqrs`，继续保留原公开命名空间。
- `GFramework.Core/Cqrs/Behaviors/LoggingBehavior.cs` 与 `PerformanceBehavior.cs` 已物理迁移到 `GFramework.Cqrs/Cqrs/Behaviors/*`，继续保留 `GFramework.Core.Cqrs.Behaviors` 公开命名空间，避免消费端源码感知程序集调整。
- `GFramework.Core/Extensions/ContextAwareCqrsExtensions.cs`、`ContextAwareCqrsCommandExtensions.cs` 与 `ContextAwareCqrsQueryExtensions.cs` 已迁移到 `GFramework.Cqrs`，`GFramework.Godot` 与兼容层调用点无需修改源码即可继续解析这些扩展。
- `GFramework.Core/Logging/LoggerFactoryResolver.cs` 已下沉到 `GFramework.Core.Abstractions/Logging/LoggerFactoryResolver.cs`，并保持 `GFramework.Core.Logging` 命名空间不变：
  - 默认 provider 会优先通过反射解析 `GFramework.Core` 中的 `ConsoleLoggerFactoryProvider`
  - 若宿主未加载默认日志实现，则退回静默 provider，避免 `GFramework.Cqrs -> GFramework.Core` 形成反向依赖
  - `GFramework.Core` 已通过 type forward 继续暴露该公开类型，降低已编译消费端的运行时兼容风险
- `ICqrsHandlerRegistry` 与 `CqrsHandlerRegistryAttribute` 已从 `GFramework.Core.Abstractions` 收敛到 `GFramework.Cqrs` 运行时根命名空间：
  - 这两个类型依赖 `ILogger` / `IServiceCollection`，不再适合继续放在“纯消息契约”抽象层
  - 该调整避免了 `GFramework.Core.Abstractions <-> GFramework.Cqrs.Abstractions` 循环引用
- `ICqrsRuntime` 已进一步收敛到 `GFramework.Cqrs.Abstractions`：
  - 新增轻量 marker seam `ICqrsContext`，让 runtime 契约不再直接依赖 `IArchitectureContext`
  - `IArchitectureContext` 现在实现 `ICqrsContext`，保留当前架构上下文作为默认 CQRS 分发上下文
  - 旧 `GFramework.Core.Abstractions.Cqrs.ICqrsRuntime` 保留为兼容别名，并由默认 runtime 模块同时注册新旧接口，避免立即打断历史公开路径
- `GFramework.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 已完成 metadata name 迁移：
  - handler/interface 元数据名改为指向 `GFramework.Cqrs.Abstractions.Cqrs`
  - generated registry contract / attribute 元数据名改为指向 `GFramework.Cqrs`
- `GFramework.Cqrs/CqrsRuntimeFactory.cs` 已新增为公开工厂，`GFramework.Core/Services/Modules/CqrsRuntimeModule.cs` 与 `GFramework.Tests.Common/CqrsTestRuntime.cs` 现在通过该工厂接线默认 runtime / registrar，而不再跨程序集直接 `new` 内部实现。
- 已确认 `GFramework.Core/Coroutine/Extensions/CqrsCoroutineExtensions.cs` 暂不适合迁移到 `GFramework.Cqrs`：
  - 它直接依赖 `TaskCoroutineExtensions.AsCoroutineInstruction()` 等 `Core` 协程工具链
  - 若一并迁出会把非 CQRS 的协程基础设施反向拉进 runtime 项目
  - 在方向修正后，该类型不再作为“必须迁走”的剩余项，而是正式视为 `Core` 对 CQRS 的桥接层
- 当前边界结论已修正为：
  - `GFramework.Core.Abstractions -> GFramework.Cqrs.Abstractions`
  - `GFramework.Core -> GFramework.Cqrs`
  - `Core` 默认集成 CQRS，但不应继续依赖其 dispatcher / generator / handler registry 细节结构
- 当前主阻塞不再是“如何让 `Core` 摆脱 `Cqrs`”，而是：
  - 如何继续降低 runtime 对反射的依赖
  - 如何让 generator 从“注册器 MVP”继续走向更完整的低反射支持
  - 如何收敛 package/facade/兼容层，而不破坏 `CoreGrid-Migration` 等真实消费端
- `GFramework.Core.Abstractions/Cqrs/ICqrsRuntime.cs` 与 `ICqrsHandlerRegistrar.cs` 已新增，形成 `ArchitectureContext` 与容器注册路径共享的 runtime seam。
- `GFramework.Core/Cqrs/Internal/DefaultCqrsHandlerRegistrar.cs` 已新增，复用现有 `CqrsHandlerRegistrar` 静态流水线承接 `ICqrsHandlerRegistrar` 默认实现。
- `GFramework.Core/Cqrs/Internal/CqrsDispatcher.cs` 已改为实现 `ICqrsRuntime`，并将当前 `IArchitectureContext` 作为调用参数传入，而不再由 `ArchitectureContext` 直接持有具体实现依赖。
- `GFramework.Core/Architectures/ArchitectureContext.cs` 已从直接 `new CqrsDispatcher(...)` 改为解析 `ICqrsRuntime` seam。
- `GFramework.Core/Ioc/MicrosoftDiContainer.cs` 已从直接调用 `CqrsHandlerRegistrar` 改为解析已注册的 `ICqrsHandlerRegistrar` seam。
- `GFramework.Core/Services/Modules/CqrsRuntimeModule.cs` 已新增，并由 `ServiceModuleManager` 纳入 built-in modules，确保默认架构启动路径继续自动具备 CQRS runtime 与 handler 注册能力。
- `GFramework.Core.Tests/CqrsTestRuntime.cs` 已补充裸测试容器的 CQRS seam 注册辅助，以便不经过 `ServiceModuleManager` 的单元测试继续观察正式 runtime 行为。
- `GFramework.Core.Tests/Ioc/MicrosoftDiContainerTests.cs` 中“Clear 后重新接入 handler”回归已适配 seam 方案：在裸容器 `Clear()` 后显式补回测试基础设施，再验证程序集去重状态重置。
- `ai-plan/migration/CQRS_MODULE_SPLIT_PLAN.md` 已新增，完成 Phase 5 的模块边界评估、依赖倒置方案与包拆分草案输出。
- Phase 5 结论已收敛为“两阶段拆分”：
  - 先做 `Core -> runtime abstraction` 的 seam 改造
  - 再拆 `GFramework.Cqrs.Abstractions` / `GFramework.Cqrs` 项目与 public runtime 类型归属
- 已确认当前真正阻塞 CQRS 拆分的硬耦合点主要是：
  - `ArchitectureContext` 直接实例化 `CqrsDispatcher`
  - `MicrosoftDiContainer` 直接调用 `CqrsHandlerRegistrar`
  - `ArchitectureBootstrapper` 在默认启动路径内建 CQRS handler 注册假设
- 已确认当前消费端兼容面的主要压力来自：
  - `CoreGrid-Migration/scripts/cqrs/**` 对 `CommandBase`、`Abstract*Handler`、`GFramework.Core.Cqrs.Extensions` 的直接依赖
  - `GFramework.Godot/Coroutine/ContextAwareCoroutineExtensions.cs` 对 `GFramework.Core.Cqrs.Extensions` 的直接依赖
- `CoreGrid-Migration` 已直连本地 `GFramework` 源码与本地 source generators。
- `GameArchitecture` 已不再依赖 `collection.AddMediator(...)` 即可使用 CQRS。
- `GFramework.Core.Abstractions` 与 `GFramework.Core.Tests` 已移除 `Mediator.Abstractions` /
  `Mediator.SourceGenerator` 包引用；`IServiceCollection` / `IServiceScope` 所需依赖改为显式引用
  `Microsoft.Extensions.DependencyInjection.Abstractions`。
- `GFramework.Core.Abstractions` 新增自有 CQRS 契约：
  - `IRequest<TResponse>` / `INotification` / `IStreamRequest<TResponse>`
  - `IRequestHandler<,>` / `INotificationHandler<>` / `IStreamRequestHandler<,>`
  - `Unit`
  - `IPipelineBehavior<,>` / `MessageHandlerDelegate<,>`
- `ArchitectureBootstrapper` 会在初始化阶段自动扫描并注册当前架构程序集与 `GFramework.Core` 程序集中的 CQRS handlers。
- `IArchitecture`、`IIocContainer`、`Architecture`、`ArchitectureModules` 与 `MicrosoftDiContainer`
  已新增 `RegisterCqrsHandlersFromAssembly(...)` / `RegisterCqrsHandlersFromAssemblies(...)`，
  允许模块程序集或扩展包程序集在初始化阶段显式接入与默认路径相同的“生成注册器优先 + 反射回退”注册链路。
- `MicrosoftDiContainer` 已把默认启动路径与显式扩展路径统一到同一个 CQRS handler 注册入口，
  并按稳定程序集键去重，避免默认扫描与后续模块接入重复写入同一程序集的 handler 映射。
- `IArchitectureContext`、`QueryBase`、`Abstract*Handler` 以及 `MessageHandlerDelegate` 的 XML 文档已补齐迁移后的契约边界，明确旧
  Command/Query 总线与新 CQRS runtime 的推荐入口。
- `CqrsDispatcher` 已支持：
  - request dispatch
  - notification publish
  - stream dispatch
  - context-aware handler 注入
  - request pipeline behavior 链式执行
- `CqrsHandlerRegistrar` 已补齐三项运行时硬化：
    - 按程序集名、处理器类型名与处理器接口名稳定排序，避免通知处理顺序随反射枚举漂移
    - 在 `ReflectionTypeLoadException` 场景下保留可加载处理器并记录告警
    - 自动扫描到的 request / notification / stream handler 统一改为 transient，避免上下文感知处理器在并发分发时共享可变状态
- `GFramework.Core.Tests` 中原依赖 `Mediator` 注册路径的测试已切换到框架内建 CQRS 注册路径；`CqrsTestRuntime` 现仅保留
  handler 注册职责，行为测试改为走 `ArchitectureContext.SendRequestAsync(...)` 正式入口。
- `AbstractStreamCommandHandler<TCommand, TResponse>` 已把上下文注入窗口、瞬态实例复用约束和流创建/枚举取消边界写入显式
  `<remarks>`，避免公共流式命令处理器基类的生命周期约束只停留在摘要里。
- `CqrsHandlerRegistrarTests` 已改为通过 `CqrsTestRuntime.RegisterHandlers(...)` 真实入口验证部分加载失败恢复路径，并同时断言
  “剩余 handler 仍被注册 + warning 已记录”。
- `CqrsTestRuntime` 已补齐 XML 文档，并改为按 `IIocContainer + IEnumerable<Assembly> + ILogger` 精确绑定
  `RegisterHandlers(...)`，避免未来新增同名重载后出现运行时歧义。
- `MediatorCoroutineExtensions` 已改为直接走 `IArchitectureContext.SendAsync(...)` 内建 CQRS 入口，不再从容器解析
  `IMediator`；该类型名仅作为历史兼容命名保留。
- `RegisterCqrsPipelineBehavior<TBehavior>()` 已作为新的公开推荐入口加入 `IArchitecture`、`IIocContainer`、
  `Architecture` 与 `MicrosoftDiContainer`；旧的 `RegisterMediatorBehavior<TBehavior>()` 改为显式兼容转发。
- `ContextAwareCqrsExtensions`、`ContextAwareCqrsCommandExtensions`、`ContextAwareCqrsQueryExtensions` 与
  `CqrsCoroutineExtensions` 已作为新的中性命名扩展入口加入；旧的 `ContextAwareMediator*Extensions` 与
  `MediatorCoroutineExtensions` 保留为 `[Obsolete]` 兼容包装层。
- `RegisterMediatorBehavior<TBehavior>()`、`ContextAwareMediator*Extensions` 与 `MediatorCoroutineExtensions`
  已进一步收紧为“正式弃用”状态：
  - `Obsolete` 提示统一明确迁移目标与 “future major version” 移除预期
  - `EditorBrowsable(EditorBrowsableState.Never)` 已将这些历史别名从 IntelliSense 主路径隐藏
  - `GFramework.Core.Tests/Cqrs/MediatorCompatibilityDeprecationTests.cs` 已锁定上述弃用元数据，防止兼容层退回“仅名义弃用”
- `GFramework.Cqrs` 已新增 `ICqrsHandlerRegistry` 与 `CqrsHandlerRegistryAttribute`，
  作为消费端程序集暴露源码生成注册器的正式 runtime 契约。
- `GFramework.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 已落地 CQRS handler source-generator MVP：
  - 对当前消费端程序集中的 concrete request / notification / stream handlers 生成单一程序集级注册器
  - 生成注册顺序与 runtime 反射排序口径保持一致，按实现类型名与处理器接口名稳定排序
  - 对生成代码无法直接 `typeof(...)` 引用、但仍可按元数据名从当前程序集重新定位的处理器（例如私有嵌套 handler），生成注册器会改走定向反射注册，而不是退回整程序集补扫
- `CqrsHandlerRegistrar` 已改为优先查找 `CqrsHandlerRegistryAttribute` 指向的生成注册器；
  当注册器缺失、元数据损坏或实例化失败时，会记录 warning 并自动回退到原有反射扫描路径。
- `GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs` 与
  `GFramework.Core.Tests/Cqrs/CqrsHandlerRegistrarTests.cs` 已补齐：
  - 生成器输出快照测试
  - 私有嵌套 handler 走 generated registry 内部定向反射注册的测试
  - runtime 优先使用生成注册器的测试
  - 生成注册器无效时自动回退反射的测试
- 新的 `ContextAwareCqrs*` / `CqrsCoroutineExtensions` 位于独立命名空间，避免与旧 `Mediator` 扩展在同一
  `using` 范围内触发扩展方法解析歧义。
- `CoreGrid-Migration` 中命中的旧扩展调用点已切到新的 `ContextAwareCqrs*` 入口，避免新旧扩展同时可见时的重载歧义。
- `docs/zh-CN/core/cqrs.md`、`docs/zh-CN/core/index.md` 与 `CLAUDE.md` 已从“依赖外部 Mediator”改写为
  “内建 CQRS runtime + 历史命名兼容”表述。
- `docs/zh-CN/core/cqrs.md` 与 `CLAUDE.md` 已补充“如何显式接入非默认程序集的 CQRS handlers”说明，避免文档仍停留在“需要额外接入”
  但未给出正式入口的状态。
- 在验证阶段发现 `CqrsHandlerRegistrar.cs` 缺少 `Microsoft.Extensions.DependencyInjection` 的 `using`，
  已补齐以恢复 `IServiceCollection` 编译通过。
- 当前验证状态：
  - `dotnet build GFramework/GFramework.sln` 通过
  - `dotnet test GFramework/GFramework.Core.Tests/GFramework.Core.Tests.csproj --no-build` 通过，`1621` 个测试全部通过
  - `dotnet build CoreGrid-Migration/CoreGrid.sln` 通过
  -
  `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Core.Tests.Cqrs.CqrsHandlerRegistrarTests|FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureModulesBehaviorTests|FullyQualifiedName~GFramework.Core.Tests.Mediator.MediatorAdvancedFeaturesTests|FullyQualifiedName~GFramework.Core.Tests.Mediator.MediatorArchitectureIntegrationTests|FullyQualifiedName~GFramework.Core.Tests.Mediator.MediatorComprehensiveTests"`
  通过，`49` 个测试全部通过
  -
  `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Core.Tests.Cqrs.CqrsHandlerRegistrarTests|FullyQualifiedName~GFramework.Core.Tests.Mediator.MediatorComprehensiveTests|FullyQualifiedName~GFramework.Core.Tests.Mediator.MediatorArchitectureIntegrationTests|FullyQualifiedName~GFramework.Core.Tests.Mediator.MediatorAdvancedFeaturesTests"`
  通过，`47` 个测试全部通过
  - `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release` 通过
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-build` 通过，`1624` 个测试全部通过
  - `dotnet build CoreGrid-Migration/CoreGrid.sln` 通过，仅存在既有 analyzer warnings 与 Godot generator warning
  - `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release` 在历史命名中性化后再次通过
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-build` 在历史命名中性化后再次通过，`1624` 个测试全部通过
  - `dotnet build CoreGrid-Migration/CoreGrid.sln` 在迁移 `ContextAwareCqrs*` 调用点后再次通过，仅存在既有 analyzer warnings
  - `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release` 在正式弃用兼容层后通过
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~GFramework.Core.Tests.Cqrs.MediatorCompatibilityDeprecationTests|FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureModulesBehaviorTests|FullyQualifiedName~GFramework.Core.Tests.Coroutine.CqrsCoroutineExtensionsTests"` 通过，`8` 个测试全部通过
  - `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release` 在 CQRS generator MVP 后通过
  - `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release` 在 CQRS generator MVP 后通过
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~GFramework.SourceGenerators.Tests.Cqrs.CqrsHandlerRegistryGeneratorTests"` 通过，`2` 个测试全部通过
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~GFramework.Core.Tests.Cqrs.CqrsHandlerRegistrarTests|FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureModulesBehaviorTests|FullyQualifiedName~GFramework.Core.Tests.Mediator.MediatorArchitectureIntegrationTests|FullyQualifiedName~GFramework.Core.Tests.Mediator.MediatorComprehensiveTests"` 通过，`41` 个测试全部通过
- `dotnet build GFramework/GFramework.sln -c Release`
  在当前 WSL 环境下命中既有 `GFramework.csproj` NuGet fallback package folder 配置问题
  （机器本地路径已省略），
  与本轮 CQRS 改动无关；`GFramework.Core.Tests` 相关项目构建与回归已通过
  - `dotnet build GFramework/GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
    在显式额外程序集 CQRS 注册入口落地后通过，仅存在既有 `MA0048` warnings
  - `dotnet test GFramework/GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureAdditionalCqrsHandlersTests|FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureModulesBehaviorTests|FullyQualifiedName~GFramework.Core.Tests.Cqrs.CqrsHandlerRegistrarTests|FullyQualifiedName~GFramework.Core.Tests.Architectures.RegistryInitializationHookBaseTests"`
    在显式额外程序集 CQRS 注册入口落地后通过，`13` 个测试全部通过
  - `dotnet build GFramework/GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
    在 runtime seam 落地后通过，`0` warnings / `0` errors
  - `dotnet test GFramework/GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Core.Tests.Ioc.MicrosoftDiContainerTests|FullyQualifiedName~GFramework.Core.Tests.Cqrs.CqrsHandlerRegistrarTests|FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureModulesBehaviorTests|FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureAdditionalCqrsHandlersTests|FullyQualifiedName~GFramework.Core.Tests.Architectures.RegistryInitializationHookBaseTests|FullyQualifiedName~GFramework.Core.Tests.Mediator.MediatorArchitectureIntegrationTests|FullyQualifiedName~GFramework.Core.Tests.Mediator.MediatorComprehensiveTests|FullyQualifiedName~GFramework.Core.Tests.Mediator.MediatorAdvancedFeaturesTests"`
    在 runtime seam 落地后通过，`97` 个测试全部通过
  - `dotnet build GFramework/GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
    在纯 CQRS 契约外提到 `GFramework.Cqrs.Abstractions` 后通过，仅存在既有 `MA0048` warnings（`IStreamCommand` / `IStreamQuery` 文件命名）
  - `dotnet test GFramework/GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~GFramework.Core.Tests.Ioc.MicrosoftDiContainerTests|FullyQualifiedName~GFramework.Core.Tests.Cqrs.CqrsHandlerRegistrarTests|FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureModulesBehaviorTests|FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureAdditionalCqrsHandlersTests|FullyQualifiedName~GFramework.Core.Tests.Architectures.RegistryInitializationHookBaseTests|FullyQualifiedName~GFramework.Core.Tests.Mediator.MediatorArchitectureIntegrationTests|FullyQualifiedName~GFramework.Core.Tests.Mediator.MediatorComprehensiveTests|FullyQualifiedName~GFramework.Core.Tests.Mediator.MediatorAdvancedFeaturesTests"`
    在纯 CQRS 契约外提后再次通过，`97` 个测试全部通过
  - `dotnet build GFramework/GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release`
    在 CQRS 聚焦测试拆分后通过，`0` warnings / `0` errors
  - `dotnet test GFramework/GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --no-build`
    在 CQRS 聚焦测试拆分后通过，`54` 个测试全部通过
  - `dotnet build GFramework/GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
    在迁出 CQRS 聚焦测试并消除跨项目 fixture 耦合后再次通过，`0` warnings / `0` errors
  - `dotnet test GFramework/GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~MicrosoftDiContainerTests|FullyQualifiedName~ArchitectureAdditionalCqrsHandlersTests|FullyQualifiedName~ArchitectureModulesBehaviorTests|FullyQualifiedName~MediatorCompatibilityDeprecationTests"`
    在测试拆分后通过，`44` 个测试全部通过

## 当前已知事实

- `GFramework` 当前仍同时维护：
  - 基于 `CommandExecutor` / `QueryExecutor` / `EventBus` 的轻量旧 CQRS
  - 基于 GFramework 自有抽象的新 CQRS runtime
- CQRS runtime 主实现已迁到 `GFramework.Cqrs`，`GFramework.Core` 当前主要保留架构接线、默认 behaviors 与协程扩展；`Abstract*Handler` 基类已物理迁到 `GFramework.Cqrs` 并改为依赖轻量 `CqrsContextAwareHandlerBase`，不再直接继承 `GFramework.Core.Rule.ContextAwareBase`。
- Phase 5 评估结论是“拆分收益成立，但必须先做依赖倒置 seam”，该 seam 已在当前恢复点落地完毕，下一步可以正式进入项目骨架拆分。
- Phase 7 已验证可以先把“纯 CQRS 契约”独立成 `GFramework.Cqrs.Abstractions`，并把 runtime registry 契约收敛到 `GFramework.Cqrs`；当前真正残留的边界阻塞点主要是 `ICqrsRuntime -> IArchitectureContext`、`CqrsCoroutineExtensions -> TaskCoroutineExtensions` 的耦合，以及是否继续承诺旧 `GFramework.Core.Abstractions.Cqrs*` namespace 兼容。
- Phase 7 已额外验证：CQRS 聚焦测试可以先拆到 `GFramework.Cqrs.Tests`，而架构壳层、容器 seam 与兼容层测试继续留在 `GFramework.Core.Tests`，不会破坏当前回归覆盖。
- 仍存在 `Mediator` 残留的区域主要集中在：
  - 兼容 API 与测试目录中的历史命名
  - 少量本地计划/历史记录文档中的迁移过程描述
- `CoreGrid-Migration` 已切到本地源码引用，并在当前恢复点完成构建验证
- 本轮已接受的 review / subagent 结论：
    - `CqrsTestRuntime.ExecutePipelineAsync(...)` 会掩盖正式 dispatcher 行为，已移除
    - handler 自动注册必须保持稳定顺序、容忍部分类型加载失败，并避免单例上下文污染
    - `AbstractStreamCommandHandler<TCommand, TResponse>` 的上下文可用窗口、实例复用约束与取消边界需要放入显式
      `<remarks>`，避免公共基类被误用
    - `CqrsHandlerRegistrar` 的部分加载失败回归必须从 `RegisterHandlers` 公开测试入口观察，而不是反射私有
      `RecoverLoadableTypes(...)`
    - `CqrsTestRuntime` 反射绑定不能只按名称解析 `RegisterHandlers`，否则新增重载后会出现不确定行为
    - 生产代码与主文档中保留的 `Mediator` 标识目前只剩历史兼容命名，不再代表外部包依赖，可作为下一阶段
      API 命名统一任务单独处理

## 当前风险

- `GFramework` 仓库存在与本任务无关的既有改动，提交时必须避免覆盖
- `CoreGrid-Migration` 是 worktree，WSL 下原生 `git` 解析该 worktree 路径有兼容问题
- 当前 `RegisterMediatorBehavior`、`MediatorCoroutineExtensions` 等旧名已降级为兼容包装层；若后续要彻底移除历史命名，需要单独规划弃用周期
- 当前历史 `Mediator` 兼容入口虽已隐藏 IntelliSense 并明确 future-major 移除，但仍保留公开签名以避免立即破坏旧调用方
- 当前 handler 自动注册已具备“默认程序集自动接入 + 额外程序集显式接入”的统一入口，并支持“消费端程序集生成注册器 + 其余程序集反射回退”的双路径；若后续追求更强 AOT/冷启动收益，还需继续减少仍依赖反射回退的程序集范围
- 若在依赖倒置 seam 落地前就直接拆项目，`GFramework.Core` 与新 `GFramework.Cqrs` 之间很容易形成“项目名分开、实现仍互相硬引用”的伪边界
- 当前 `CoreGrid-Migration` 与 `GFramework.Godot` 均直接依赖 `GFramework.Core.Cqrs.*` public types；若后续拆分时同时改 namespace 或取消 transitive 依赖，会放大消费端迁移成本
- 当前 `ICqrsRuntime -> IArchitectureContext` 仍会阻止完整搬空 `GFramework.Core.Abstractions/Cqrs`；若不先收敛这条依赖，后续很容易重新引入项目循环引用
- 当前 `Abstract*Handler` 已摆脱 `GFramework.Core.Rule.ContextAwareBase`，但 `CqrsCoroutineExtensions` 仍依赖 `TaskCoroutineExtensions`；若还希望继续压缩 `GFramework.Core` 对 CQRS 的承载范围，下一步仍需处理协程工具链边界
- `CoreGrid-Migration` 本轮通过 consumer 侧 namespace/extension 对齐恢复构建；若产品层仍希望承诺旧 `GFramework.Core.Abstractions.Cqrs*` 公共路径，需要单独设计兼容层或迁移策略，否则会在后续真实消费端继续重复暴露同类断点
- 当前 `GFramework.Cqrs.Tests` 仍直接引用 `GFramework.Core`，说明测试已经按模块意图拆分，但 runtime 物理迁移尚未完成；若后续直接切断该依赖，测试会失去承载实现
- `dotnet build GFramework.sln -c Release` 在当前 WSL 环境仍会受顶层 `GFramework.csproj` 的 Windows NuGet fallback package
  folder 配置影响，若需要恢复 solution 级全量验证，需先处理该环境问题

## 下次恢复建议

若本轮中断，优先从以下顺序恢复：

1. 查看 `ai-plan/public/cqrs-rewrite/traces/cqrs-rewrite-migration-trace.md`
2. 确认当前恢复点 `CQRS-REWRITE-RP-042` 已对应到最新提交
3. 优先继续执行 `ai-plan/migration/CQRS_MODULE_SPLIT_PLAN.md` 中的 Phase 8（当前主线）：
   - 先决定是否正式支持旧 `GFramework.Core.Abstractions.Cqrs*` / `GFramework.Core.Cqrs.Extensions` public namespace 兼容，还是明确要求消费端迁到当前 `GFramework.Cqrs*` 路径
   - 再评估 `CqrsCoroutineExtensions` 是否保留在 `GFramework.Core`，或连同所需协程辅助一起形成更小的可迁移边界
4. 在 runtime 项目真正承接实现后，再处理 source-generator、meta package 与消费端 transitive 依赖的迁移
5. 在规划 future major 版本时，再决定何时真正移除 `RegisterMediatorBehavior` / `MediatorCoroutineExtensions` / `ContextAwareMediator*Extensions`

## 2026-04-15 补充记录（RP-015）

### 阶段：轻量 handler 上下文基类与消费端兼容性收敛

- 建立 `CQRS-REWRITE-RP-015` 恢复点
- `GFramework.Cqrs/Cqrs/CqrsContextAwareHandlerBase.cs` 已新增，作为仅依赖 `IContextAware` / `IArchitectureContext` 的轻量 CQRS handler 上下文基类：
  - 保留 `OnContextReady()` 生命周期扩展点
  - 去掉对 `GameContext` 的兜底依赖
  - 在运行时注入前访问 `Context` / `GetContext()` 时显式抛出异常，避免静默落回全局上下文
- `AbstractCommandHandler` / `AbstractQueryHandler` / `AbstractRequestHandler` / `AbstractNotificationHandler` 及其 stream 变体已物理迁到 `GFramework.Cqrs`，并改为继承该轻量基类
- `GFramework.Cqrs.Tests/Cqrs/AbstractCqrsHandlerContextTests.cs` 已新增，回归覆盖：
  - 未注入上下文时 fail-fast
  - 注入后 `Context` 可用
  - `OnContextReady()` 生命周期钩子仍然生效
- 在真实迁移验证阶段，`CoreGrid-Migration` 暴露出旧 `GFramework.Core.Abstractions.Cqrs*` 与旧 `GFramework.Core.Cqrs.Extensions` 命名空间不再自动可用的问题
- 本轮先采用最小 consumer 修复路径恢复验证链路：
  - `scripts/GlobalUsings.cs` 已补齐新的 `GFramework.Cqrs.Abstractions.Cqrs*`、`GFramework.Cqrs.*` 与 `GFramework.Core.Cqrs.*` handler namespace 导入
  - `scripts/cqrs/**` 中显式写死旧 `Unit` / `INotification` / `IQuery` 路径的文件已切到新的 `GFramework.Cqrs.Abstractions.Cqrs*`
  - `GlobalInputController.cs`、`PauseMenu.cs`、`OptionsMenu.cs` 与两个组合 handler 已改用 `GFramework.Cqrs.Extensions.ContextAwareCqrs*Extensions`

### 阶段：RP-015 验证

- `dotnet build CoreGrid-Migration/CoreGrid.sln`
  - 结果：通过
  - 备注：仅剩 `CoreGrid-Migration` 既有 analyzer warnings，无新增 CQRS 编译错误

## 备注

- 本文档是当前任务的主恢复点，后续每个关键阶段完成后都要更新
- 发生方向调整时，不覆盖旧结论，直接追加阶段记录与新的恢复点编号

## 2026-04-15 补充记录

### 阶段：review 收尾修正（并发 lazy 初始化与共享测试运行时）

- `GFramework.Core/Architectures/ArchitectureContext.cs` 已将 `ICqrsRuntime` 的延迟解析从 `??=`
  改为 `Lazy<ICqrsRuntime>`，并显式使用 `LazyThreadSafetyMode.ExecutionAndPublication`
  保证并发首次访问时只执行一次容器解析
- `ArchitectureContext` 已补齐公开构造函数 XML 文档，以及 `CqrsRuntime` 惰性初始化的并发语义说明
- `GFramework.Core.Tests/Architectures/ArchitectureContextTests.cs` 已新增并发回归测试，
  锁定“多线程首次访问 `SendRequestAsync(...)` 时只解析一次 `ICqrsRuntime`”的行为
- 原先位于 `GFramework.Core.Tests/CqrsTestRuntime.cs` 与 `GFramework.Cqrs.Tests/CqrsTestRuntime.cs`
  的重复实现已提取到共享源码文件 `tests/Shared/CqrsTestRuntime.cs`
- `GFramework.Core.Tests` 与 `GFramework.Cqrs.Tests` 已通过链接编译同一份共享源码并补齐 `global using`，
  避免两个测试项目继续维护分叉的反射绑定逻辑
- 共享版 `CqrsTestRuntime` 已顺手清理 `GetType(..., throwOnError: true)! ?? throw ...`
  里的不可达 `?? throw` 分支

### 阶段：review 收尾修正验证

- `dotnet test GFramework/GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsHandlerRegistrarTests|FullyQualifiedName~GFramework.Cqrs.Tests.Mediator.MediatorComprehensiveTests|FullyQualifiedName~GFramework.Cqrs.Tests.Mediator.MediatorArchitectureIntegrationTests|FullyQualifiedName~GFramework.Cqrs.Tests.Mediator.MediatorAdvancedFeaturesTests"`
  - 结果：通过
  - 明细：`49` 个测试全部通过
- `dotnet test GFramework/GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureContextTests|FullyQualifiedName~GFramework.Core.Tests.Ioc.MicrosoftDiContainerTests"`
  - 结果：通过
  - 明细：`57` 个测试全部通过
- 首次并行执行两个 `dotnet test` 命令时，`NuGet` restore 在共享 `obj/project.assets.json` 上发生文件竞争；
  顺序重跑 `GFramework.Core.Tests --no-restore` 后验证通过，属于本地并行 restore 的环境噪音，不是代码回归

### 阶段：测试公共基础设施模块化

- 已新增 `GFramework.Tests.Common` 项目，并加入 `GFramework.sln`
- 原先临时放在仓库根目录 `tests/Shared/CqrsTestRuntime.cs` 的共享源码已迁入
  `GFramework.Tests.Common/CqrsTestRuntime.cs`
- `GFramework.Core.Tests` 与 `GFramework.Cqrs.Tests` 已从源码链接切换为显式 `ProjectReference`
- 两个测试项目的 `global using` 已从 `GFramework.Tests.Shared` 迁到 `GFramework.Tests.Common`
- 该调整保留了原有测试调用方式，但把公共测试基础设施收敛到单独模块，避免继续以目录约定模拟模块边界

### 阶段：测试公共基础设施模块化验证

- `dotnet test GFramework/GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Core.Tests.Architectures.ArchitectureContextTests|FullyQualifiedName~GFramework.Core.Tests.Ioc.MicrosoftDiContainerTests"`
  - 结果：通过
  - 明细：`57` 个测试全部通过
- `dotnet test GFramework/GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsHandlerRegistrarTests|FullyQualifiedName~GFramework.Cqrs.Tests.Mediator.MediatorComprehensiveTests|FullyQualifiedName~GFramework.Cqrs.Tests.Mediator.MediatorArchitectureIntegrationTests|FullyQualifiedName~GFramework.Cqrs.Tests.Mediator.MediatorAdvancedFeaturesTests"`
  - 结果：通过
  - 明细：`49` 个测试全部通过

## 2026-04-15 补充记录（RP-016）

### 阶段：日志入口下沉与剩余 runtime behaviors 迁移

- 建立 `CQRS-REWRITE-RP-016` 恢复点
- `GFramework.Core.Abstractions/Logging/LoggerFactoryResolver.cs` 已新增，继续使用 `GFramework.Core.Logging` 命名空间：
  - 该类型已从 `GFramework.Core` 实现层下沉到抽象层，允许 `GFramework.Cqrs` 在不反向引用 `GFramework.Core` 的前提下获取日志器
  - 默认 provider 会优先反射创建 `GFramework.Core.Logging.ConsoleLoggerFactoryProvider`
  - 当宿主仅加载抽象层时，会退回到静默 provider，避免抽象层默认日志入口因缺少实现程序集而崩溃
- `GFramework.Core/Properties/TypeForwarders.cs` 已新增，对 `LoggerFactoryResolver` 做 type forwarding，避免把公开类型迁出 `GFramework.Core` 后留下运行时兼容断点
- `GFramework.Core/Cqrs/Behaviors/LoggingBehavior.cs` 与 `PerformanceBehavior.cs` 已物理迁移到 `GFramework.Cqrs/Cqrs/Behaviors/*`
- 上述两个 behavior 继续保留 `GFramework.Core.Cqrs.Behaviors` 命名空间，消费端源码无需改 using 即可继续解析
- 通过这一步，`GFramework.Core/Cqrs/*` 已完全搬空；Phase 7 的 runtime 物理迁移残项现只剩 `CqrsCoroutineExtensions`
- 并行 explorer 结论已收敛：
  - `ICqrsRuntime` 当前不能迁到 `GFramework.Cqrs.Abstractions`，根因不是单个 using，而是整条 `ICqrsRuntime -> IArchitectureContext -> IContextAware / handler Context` 的上下文模型仍绑定 `GFramework.Core.Abstractions`
  - `CqrsCoroutineExtensions` 本质上是 `GFramework.Core` 协程桥接层，不是纯 CQRS runtime 代码；若原样迁到 `GFramework.Cqrs`，会重新形成 `GFramework.Cqrs -> GFramework.Core` 反向依赖
  - 下一阶段的最小可行方案应优先考虑“新增 CQRS 专用上下文 seam + 继续让协程扩展留在 `GFramework.Core`”，而不是先新增组合项目

### 阶段：RP-016 验证

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

## 2026-04-16 补充记录（RP-020）

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

## 2026-04-16 补充记录（RP-033）

### 阶段：review 小修（缓存清理、诊断信息与测试辅助性能）

- 建立 `CQRS-REWRITE-RP-033` 恢复点
- `GFramework.Cqrs.Tests/Cqrs/CqrsDispatcherCacheTests.cs` 的 `ClearDispatcherCaches()` 已补齐
  `RequestPipelineInvokerCache<string>.Invokers` 清理，避免未来新增 `string` pipeline 路径测试时残留静态状态
- `GFramework.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 已在
  `CanReferenceFromGeneratedRegistry(...)` 的默认分支补充注释，明确当前把 `dynamic`、error type 等其他 Roslyn 类型种类视为暂时可引用的实现假设
- `GFramework.SourceGenerators.Tests/Core/MetadataReferenceTestBuilder.cs` 已将运行时可信平台程序集引用收敛为惰性静态缓存，避免多程序集生成器测试反复解析
  `TRUSTED_PLATFORM_ASSEMBLIES`
- `GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs` 已增强 `RunGenerator(...)`
  中的编译错误断言消息，失败时会输出完整 diagnostics 文本，便于直接定位生成代码问题

### 阶段：RP-033 验证

- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.SourceGenerators.Tests.Cqrs.CqrsHandlerRegistryGeneratorTests"`
  - 结果：通过
  - 明细：`11` 个测试全部通过
- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsDispatcherCacheTests"`
  - 结果：通过
  - 明细：`2` 个测试全部通过

## 2026-04-18 补充记录（RP-040）

### 阶段：第三方参考源码治理

- 建立 `CQRS-REWRITE-RP-040` 恢复点
- `AGENTS.md` 已新增仓库级约束：
  - `ai-libs/` 用于存放第三方项目源码副本，仅供对照、追踪与设计参考
  - `ai-libs/**` 默认为只读区域，除非用户明确要求同步或更新第三方快照，否则不允许修改
  - 后续计划、trace、评审与设计说明引用第三方实现时，优先写明仓库内参考路径，而不是使用模糊的外部项目名
- CQRS 迁移主线已将 `Mediator` 的本地参考源正式收口到 `ai-libs/Mediator`
- 本跟踪文档中“后续继续参考 Mediator 成熟实现”的执行语义已同步更新为“优先参考 `ai-libs/Mediator`”

### 阶段：RP-040 验证

- `dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release`
  - 结果：通过
  - 备注：存在既有 `MA0051` 与 `MA0158` analyzer warnings，无新增构建错误
- `rg -n "ai-libs/Mediator|只读|第三方项目源码副本" AGENTS.md ai-plan/public/cqrs-rewrite/todos/cqrs-rewrite-migration-tracking.md ai-plan/public/cqrs-rewrite/traces/cqrs-rewrite-migration-trace.md`
  - 结果：通过
  - 备注：`AGENTS.md`、tracking 与 trace 均已命中新规则和本地参考路径说明

### 下一步

1. 后续若继续推进 CQRS runtime、generator 或低反射优化，统一以 `ai-libs/Mediator` 作为本地参考源
2. 若未来需要升级 `Mediator` 参考副本，单独作为“同步第三方快照”任务处理，不与框架实现改动混在同一变更里

## 2026-04-18 补充记录（RP-041）

### 阶段：source-generator 文档命名空间收口

- 建立 `CQRS-REWRITE-RP-041` 恢复点
- 已将 `docs/zh-CN/**` 中残留的旧示例 `using GFramework.SourceGenerators.Abstractions.*;` 批量改为当前公开命名空间：
  - `GFramework.Core.SourceGenerators.Abstractions.Rule`
  - `GFramework.Core.SourceGenerators.Abstractions.Bases`
  - `GFramework.Core.SourceGenerators.Abstractions.Logging`
  - `GFramework.Core.SourceGenerators.Abstractions.Enums`
  - `GFramework.Core.SourceGenerators.Abstractions.Architectures`
- 已同步修正文档中的叙述性旧口径：
  - `docs/zh-CN/source-generators/logging-generator.md` 不再把日志生成器描述成旧聚合 `GFramework.SourceGenerators`
  - `docs/zh-CN/source-generators/context-get-generator.md` 改为明确由 `GFramework.Core.SourceGenerators` 执行注册可见性分析
  - `docs/zh-CN/api-reference/index.md` 将“`GFramework.SourceGenerators` 单一模块”改写为当前拆分后的 Source Generators 家族说明

### 阶段：RP-041 验证

- `rg -n "using GFramework\\.SourceGenerators\\.Abstractions\\.|### GFramework\\.SourceGenerators|GFramework\\.SourceGenerators 自动生成|GFramework\\.SourceGenerators 现在还会分析" docs/zh-CN`
  - 结果：通过
  - 备注：`docs/zh-CN/**` 中上述旧公开命名空间与旧聚合表述已清理完毕

### 下一步

1. 若继续文档主线，扩大清理范围到更多说明页中的旧 API 参考与历史命名残留，优先处理 adoption 入口最靠前的页面
2. 若切回实现主线，则重新盘点 `GFramework.Cqrs` 中仍值得继续压缩的冷路径/热路径反射点，优先选择能带来明确收益的低复杂度改动

## 2026-04-19 补充记录（RP-042）

### 阶段：PR review 技能接入与 PR-253 follow-up

- 已新增项目级 `$gframework-pr-review` skill：
  - 目录：`.codex/skills/gframework-pr-review/`
  - 作用：定位当前分支对应的 GitHub PR，并优先通过 GitHub PR / issue comments / review comments API 提取
    CodeRabbit 汇总、最新 head commit review threads、`Failed checks` 与 CTRF 测试结果
  - 约束：不依赖 `gh` CLI；不再把重型 PR HTML 页面当作主数据源
- 已根据 PR `#253` 的公开 review 内容完成本地修正：
  - `.codex/skills/gframework-boot/SKILL.md` 的恢复 heuristics 不再把 `next step/continue/继续`
    直接映射为 `recovery`
  - `AGENTS.md` 中 `ai-libs/**` 观察写入 active plan/trace 的规则已收窄到“多步/复杂任务或已有 active
    tracking document”
  - `Godot/script_templates/Node/*.cs` 与 `GFramework.Core.Abstractions/Controller/IController.cs`
    中旧 `Rule` 命名空间残留已同步修正
  - `fetch_current_pr_review.py` 已改为：
    - Git 路径支持环境变量覆盖并回退到 `git.exe` / `git`
    - `--pr` 模式不再强制读取当前分支
    - `--branch` 到 PR 编号的解析改为走 GitHub PR API
    - CodeRabbit summary / CTRF 测试报告改为走 issue comments API
    - 最新 review 依据改为 latest head commit review threads，而不是只看汇总块
  - `ai-plan/public/cqrs-rewrite/todos/cqrs-rewrite-migration-tracking.md` 已移除公开文档中的机器本地绝对路径，并统一
    下次恢复建议里的恢复点编号

### 阶段：RP-042 验证

- `python3 - <<'PY' ... ast.parse(...) ... PY`
  - 结果：通过
  - 备注：`fetch_current_pr_review.py` 语法正确
- `python3 .codex/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --pr 253`
  - 结果：通过
  - 备注：已通过 API-first 路径解析 PR 元数据、latest head commit review threads、CodeRabbit summary
    与 CTRF 测试结果，不再依赖 PR HTML
- `python3 .codex/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --branch feat/cqrs-optimization`
  - 结果：通过
  - 备注：已验证 branch -> PR 解析同样通过 GitHub API 工作
- `dotnet build GFramework.Core.Abstractions/GFramework.Core.Abstractions.csproj -c Release`
  - 结果：通过
  - 备注：相关 C# 改动已完成构建验证

### 下一步

1. 若要让 PR `#253` 上的 latest head review threads 反映本轮本地修正，需要先提交并推送当前分支，再重新执行 `$gframework-pr-review`
2. PR 当前公开 warning 仍包含 `Docstring Coverage`，若后续要继续消除此项，需要单独规划并提交文档注释覆盖率改进
