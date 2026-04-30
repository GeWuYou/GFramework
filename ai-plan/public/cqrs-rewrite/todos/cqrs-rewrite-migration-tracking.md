# CQRS 重写迁移跟踪

## 目标

围绕 `GFramework` 当前的双轨 CQRS 现状，继续完成以“去外部依赖、降低反射、收口公开入口”为目标的
CQRS 迁移与收敛。

## 当前恢复点

- 恢复点编号：`CQRS-REWRITE-RP-073`
- 当前阶段：`Phase 8`
- 当前焦点：
  - 已完成一轮 `CQRS vs Mediator` 只读评估归档，结论已沉淀到 `archive/todos/cqrs-vs-mediator-assessment-rp063.md`
  - 当前评估结论已明确：`GFramework.Cqrs` 已完成对外部 `Mediator` 的生产级替代，但仓库内部旧总线 API、
    兼容 seam、fallback 旧语义与测试命名仍未完全收口
  - 当前评估结论已明确：相对 `ai-libs/Mediator`，框架已吸收统一消息模型、generator 优先注册与热路径缓存思路，
    但仍未完整吸收 publisher 策略抽象、细粒度 pipeline、telemetry / diagnostics / benchmark 体系与 runtime 主体生成
  - 下一阶段建议优先级已收敛为：`notification publisher seam`、`dispatch/invoker 生成前移`、`pipeline 分层扩展`、
    `可观测性 seam` 与 `benchmark / allocation baseline`
  - 当前功能历史已归档，active 跟踪仅保留 `Phase 8` 主线的恢复入口
  - 已完成一轮 notification publisher seam 最小落地：`GFramework.Cqrs` 新增 `INotificationPublisher`、
    `NotificationPublishContext<TNotification>` 与默认 `SequentialNotificationPublisher`
  - `CqrsDispatcher` 现会在解析当前通知处理器集合后，把执行顺序委托给 publisher seam；默认行为仍保持
    “零处理器静默完成、顺序执行、首错即停”
  - `CqrsRuntimeFactory`、`CqrsRuntimeModule` 与 `GFramework.Tests.Common.CqrsTestRuntime` 现支持在 runtime 创建前复用
    容器里已显式注册的 `INotificationPublisher`
  - 已补充 `CqrsNotificationPublisherTests`，覆盖自定义 publisher 接管、上下文注入、零处理器静默完成、首错即停，以及
    `RegisterInfrastructure` 默认接线复用预注册 publisher 的回归
  - 已完成一轮 `Mediator` 测试命名收口：
    - `MediatorAdvancedFeaturesTests` -> `CqrsArchitectureContextAdvancedFeaturesTests`
    - `MediatorArchitectureIntegrationTests` -> `CqrsArchitectureContextIntegrationTests`
    - `MediatorComprehensiveTests` -> `ArchitectureContextComprehensiveTests`
  - `GFramework.Cqrs.Tests` 中这三份历史测试现已统一迁入 `Cqrs/` 目录，并将命名空间、类名、中文注释与嵌套测试类型中的
    `Mediator` 语义收口为 `CQRS` / `ArchitectureContext`
  - 已补充 `ArchitectureContextTests` 并发 lazy-resolution 回归，锁定 `PublishAsync(...)` 与 `CreateStream(...)`
    在并发首次访问时也只会解析一次 `ICqrsRuntime`
  - 已完成一轮 `LegacyICqrsRuntime` compatibility slice 收口：
    - `CqrsRuntimeModule` 与 `GFramework.Tests.Common.CqrsTestRuntime` 现把 legacy alias 注册收敛到显式 helper
    - `MicrosoftDiContainerTests` 已补充“只预注册正式 `ICqrsRuntime` seam 时，也会回填 legacy alias 且保持同实例”的回归
    - `GFramework.Core.Abstractions/README.md`、`docs/zh-CN/abstractions/core-abstractions.md` 与
      `docs/zh-CN/core/cqrs.md` 现已明确：旧命名空间下的 `ICqrsRuntime` 仅作为 compatibility alias 保留，
      新代码应直接依赖 `GFramework.Cqrs.Abstractions.Cqrs.ICqrsRuntime`
  - 已完成一轮 `dispatch/invoker` 生成前移的最小 request 切片：
    - `GFramework.Cqrs` 新增 `ICqrsRequestInvokerProvider`、`IEnumeratesCqrsRequestInvokerDescriptors`、
      `CqrsRequestInvokerDescriptor` 与 `CqrsRequestInvokerDescriptorEntry`
    - generated registry 若实现 request invoker provider 契约，`CqrsHandlerRegistrar` 现会在激活 registry 后把 provider 注册进容器，
      并把 provider 枚举出的 request invoker 描述符写入 dispatcher 的进程级弱缓存
    - `CqrsDispatcher` 现会在首次创建 request dispatch binding 时优先命中 generated request invoker 描述符；
      未命中时仍回退到既有 `MakeGenericMethod + Delegate.CreateDelegate` 路径
    - `GFramework.Cqrs.Tests` 已补充 `CqrsGeneratedRequestInvokerProviderTests`，锁定 registrar 接线和 dispatcher 消费 generated invoker 的最小语义
    - `GFramework.SourceGenerators.Tests` 已补充 generator 回归，锁定当 runtime 暴露新契约时，generated registry 会额外发射 request invoker provider 成员与 invoker 方法
  - 已完成一轮 `dispatch/invoker` 生成前移的最小 stream 切片：
    - `GFramework.Cqrs` 新增 `ICqrsStreamInvokerProvider`、`IEnumeratesCqrsStreamInvokerDescriptors`、
      `CqrsStreamInvokerDescriptor` 与 `CqrsStreamInvokerDescriptorEntry`
    - generated registry 若实现 stream invoker provider 契约，`CqrsHandlerRegistrar` 现会在激活 registry 后把 provider 注册进容器，
      并把 provider 枚举出的 stream invoker 描述符写入 dispatcher 的进程级弱缓存
    - `CqrsDispatcher` 现会在首次创建 stream dispatch binding 时优先命中 generated stream invoker 描述符；
      未命中时仍回退到既有 `MakeGenericMethod + Delegate.CreateDelegate` 流式 binding 路径
    - `GFramework.Cqrs.Tests` 已扩充 `CqrsGeneratedRequestInvokerProviderTests`，锁定 registrar 接线和 dispatcher 消费 generated stream invoker 的最小语义
    - `GFramework.SourceGenerators.Tests` 已补充 generator 回归，锁定当 runtime 暴露新契约时，generated registry 会额外发射 stream invoker provider 成员与 invoker 方法
    - `GFramework.Cqrs/README.md`、`GFramework.Cqrs.SourceGenerators/README.md`、`docs/zh-CN/core/cqrs.md` 与
      `docs/zh-CN/source-generators/cqrs-handler-registry-generator.md` 现已同步说明 generated stream invoker 的接线与回退边界
  - 已完成一轮 generated invoker 发射范围补强：
    - `CqrsHandlerRegistryGenerator` 现会把 generated request / stream invoker 的发射范围，从“仅 direct registration”扩大到“实现类型隐藏、但 handler interface 仍可直接表达”的 reflected-implementation registration
    - 当前扩展仍刻意避开 `PreciseReflectedRegistrationSpec`，不把隐藏 request/response 类型误拉进 provider 发射，继续保持生成源码可编译边界
    - `GFramework.SourceGenerators.Tests` 已新增两条 hidden-implementation 回归，锁定 request / stream provider 在该场景下都会继续发射 descriptor 与静态 invoker 方法
  - 已完成一轮 hidden-implementation generated invoker runtime 回归补强：
    - `GFramework.Cqrs.Tests/Cqrs/CqrsGeneratedRequestInvokerProviderTests.cs` 现覆盖“实现类型隐藏、但 handler interface 可见”场景下的 generated request / stream invoker 消费路径
    - `HiddenImplementationGeneratedRequestInvokerProviderRegistry`、`HiddenImplementationGeneratedStreamInvokerProviderRegistry` 与对应 container / handler fixture 现锁定 registrar 接线后，dispatcher 会优先命中 generated descriptor，而不是退回反射 invoker
    - 当前 runtime 回归继续保持 `PreciseReflectedRegistrationSpec` 排除边界不变，只验证已允许发射 provider 元数据的 visible-interface hidden-implementation 场景
  - 已完成一轮 precise reflected invoker provider 合同边界回归：
    - `GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs` 现新增 request / stream 两条回归，明确当 handler 仍需走 `PreciseReflectedRegistrationSpec` 时，generator 即使检测到 invoker provider runtime 合同，也不会错误发射 descriptor、枚举接口或静态 invoker 桥接
    - 本轮接受了一条只读 subagent 的“继续评估 precise reflected + provider 发射”候选思路，但主线程复核后确认该候选并不存在可安全放宽的 `typeof(request/response)` 子集，因此收敛为“锁定当前排除边界”的测试批次，而不是修改生产 generator 逻辑
  - 已完成一轮 invoker provider gate 合同回归：
    - `GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs` 现新增四条回归，分别锁定 request / stream 在缺少 `ICqrsRequestInvokerProvider`、`IEnumeratesCqrsRequestInvokerDescriptors`、`ICqrsStreamInvokerProvider` 或 `IEnumeratesCqrsStreamInvokerDescriptors` 时，generator 都会整体跳过对应 provider 元数据发射
    - 本轮最初采用固定源码片段替换来裁剪测试输入，但因三引号字符串缩进差异导致 helper 过脆；当前已收敛为按稳定起止标记移除源码块的 `RemoveBlock(...)` helper，避免 gate 回归依赖精确空格对齐
  - 已完成一轮 generated invoker provider runtime 失败边界修复：
    - `GFramework.Cqrs.Tests/Cqrs/CqrsGeneratedRequestInvokerProviderTests.cs` 现新增 request / stream 两组 `non-static invoker` 与 `incompatible invoker` 回归，锁定 dispatcher 在首次绑定阶段会显式拒绝非法 generated descriptor
    - `GFramework.Cqrs/Internal/CqrsDispatcher.cs` 现把 `Delegate.CreateDelegate(...)` 抛出的 `ArgumentException` 统一包装为已有 XML 文档承诺的 `InvalidOperationException`，保持 request / stream 两条错误消息语义一致
    - 本轮顺手为新增异步断言补齐 `ConfigureAwait(false)`，消除新测试引入的 `MA0004` warning
  - 当前相对 `origin/main` 的累计 branch diff 为 `24 files / 1754 changed lines`，仍低于本轮 `$gframework-batch-boot 50` 的主要 stop condition，可继续推进下一批低风险切片
  - 已将 mixed fallback 场景进一步收敛：当 runtime 允许同一程序集声明多个 `CqrsReflectionFallbackAttribute` 实例时，generator 现会把可直接引用的 fallback handlers 与仅能按名称恢复的 fallback handlers 拆分发射
  - `CqrsReflectionFallbackAttribute` 现允许多实例，以承载 `Type[]` 与字符串 fallback 元数据的组合输出
  - 已将 generator 的程序集级 fallback 元数据进一步收敛：当全部 fallback handlers 都可直接引用且 runtime 暴露 `params Type[]` 合同时，生成器现优先发射 `typeof(...)` 形式的 fallback 元数据
  - 当 runtime 不支持多实例 fallback 特性或缺少对应构造函数时，mixed fallback 场景仍会整体保守回退到字符串元数据，避免仅部分 handler 走 `Type[]` 时漏掉剩余需按名称恢复的 handlers
  - 已完成 request pipeline executor 形状缓存：`CqrsDispatcher` 现会在单个 request binding 内按 `behaviorCount` 复用强类型 pipeline executor，而不是每次 `SendAsync` 都重建整条 `next` 委托链
  - 已补充 dispatcher pipeline executor 缓存与双行为顺序回归，锁定缓存复用后仍保持现有行为执行顺序
  - 已补充 cached request pipeline executor 的上下文刷新回归，锁定 executor 复用时仍会为当次 handler / singleton behavior 重新注入当前 `ArchitectureContext`
  - 已补充 cached notification / stream dispatch binding 的上下文刷新回归，锁定 binding 复用时仍会为当次 handler 重新注入当前 `ArchitectureContext`
  - 已补充非 `IArchitectureContext` 的 dispatcher 失败语义回归，锁定 context-aware request / notification / stream handler 在注入前置条件不满足时会显式抛出异常
  - 已补充 registrar fallback 失败分支回归，锁定 named fallback 无法解析、named fallback 解析抛异常、direct fallback 跨程序集三类 warning 语义
  - 已完成 generated registry 激活路径收敛：`CqrsHandlerRegistrar` 现优先复用缓存工厂委托，避免重复 `ConstructorInfo.Invoke`
  - 已补充私有无参构造 generated registry 的回归测试，确保兼容现有生成器产物
  - 已修正 pointer / function pointer 泛型合同的错误覆盖：生成器不再为这两类类型发射 precise runtime type 重建代码
  - 已补充非法 CQRS 泛型合同的输入诊断断言，明确 `CS0306` 与 fallback / diagnostic 路径的组合语义
  - 已为 registrar 的 reflection 注册路径补充 handler-interface 元数据缓存，减少跨容器重复注册时的 `GetInterfaces()` 反射
  - 已将 registrar 的重复映射判定从线性扫描 `IServiceCollection` 收敛为本地映射索引，减少 fallback 注册路径的重复查找
  - 已完成一轮 `static lambda + state` 微收敛：`CqrsDispatcher` 与 `CqrsHandlerRegistrar` 现会在弱缓存 / 并发缓存入口优先使用无捕获工厂，继续压低热路径上的额外闭包分配
  - 已补充 `CqrsReflectionFallbackAttribute` 叶子级合同测试，锁定空 marker、字符串 fallback 名称归一化、直接 `Type` fallback 归一化与空参数防御语义
  - 已完成 `PR #304` review follow-up 收敛：`CqrsDispatcher` 现补齐 pipeline executor / continuation 缓存的线程模型文档，并把 request pipeline invoker 从按 `behaviorCount` 重复创建收敛为 binding 内复用
  - 已补齐 `CqrsDispatcherContextValidationTests` 三个上下文校验 handler 的 XML `param` / `returns` 注释，以及 `DispatcherNotificationContextRefreshNotification`、`DispatcherStreamContextRefreshRequest` 的 `DispatchId` XML 参数注释，收敛上一轮 PR review 遗留的文档类 minor feedback
  - 已收紧 CQRS / generator 回归测试的脆弱断言：日志断言改为语义匹配，precise runtime type lookup 回归改为锁定数组秩、外部类型查找与“未发射 fallback metadata”这些稳定语义
  - 已为 dispatcher cache / context refresh / pipeline order 三组测试状态容器补齐并发保护，并将 `CqrsDispatcherCacheTests` 标记为 `NonParallelizable`，避免静态缓存与共享快照在并行测试中相互污染
  - 中期上继续 `Phase 8` 主线：参考 `ai-libs/Mediator`，继续扩大 generator 覆盖，并选择下一个收益明确的 dispatch / invoker 反射收敛点

## 当前状态摘要

- 已完成 `Mediator` 外部依赖移除、CQRS runtime 重建、默认架构接线和显式程序集 handler 注册入口
- 已完成 `GFramework.Cqrs.Abstractions` / `GFramework.Cqrs` 项目骨架与 runtime seam 收敛
- 已完成 handler registry generator 的多轮收敛，当前合法 closed handler contract 已统一收敛到更窄的注册路径
- 已完成一轮公开入口文档与 source-generator 命名空间收口
- 已完成一轮 `CQRS vs Mediator` 对照评估，确认当前主问题已从“是否能替代外部依赖”转为“框架内部收口与能力深化顺序”
- 已接入 `$gframework-pr-review`，可直接抓取当前分支对应 PR 的 CodeRabbit 评论、checks 和测试结果

## 当前活跃事实

- `Phase 8` 仍是当前主线，不再回退到 `Phase 7`
- `2026-04-20` 已重新执行 `$gframework-pr-review`：
  - 当前分支对应 `PR #261`，状态为 `OPEN`
  - latest reviewed commit 当前剩余 `1` 条 open CodeRabbit thread，指向 `ai-plan/public/cqrs-rewrite/traces/cqrs-rewrite-migration-trace.md` 中 `RP-047` 与 `RP-050` 的历史语义冲突
  - 本地已同步修正该追踪歧义：`RP-047` 明确标注为已被 `RP-050` 覆盖，后续不得恢复 `MakePointerType()` precise registration
  - 远端测试信号保持通过：最新 CTRF 汇总为 `2118/2118` passed；MegaLinter 仅剩 `dotnet-format` restore failure 预警，当前未提供本地仍然成立的文件级格式问题
- `2026-04-20` 已完成一轮冷启动反射收敛：
  - generated registry 类型首次分析后，会缓存一个可复用的激活工厂，而不是在后续容器注册时重复走 `ConstructorInfo.Invoke`
  - 若运行环境不允许动态方法，仍保留原有的反射激活回退，避免阻塞 generated registry 路径
  - `GFramework.Cqrs.Tests` 已补充“私有无参构造 registry 仍可激活”的回归覆盖
- `2026-04-20` 已完成一轮 generator 覆盖面扩展：
  - `CqrsHandlerRegistryGenerator` 现会在 runtime type 建模入口直接拒绝 `IPointerTypeSymbol` 与 `IFunctionPointerTypeSymbol`
  - `CanReferenceFromGeneratedRegistry` 不再递归判断 pointer / function pointer 的内部元素，而是统一返回 `false`
  - 相关 source-generator 回归已改为区分输入源诊断与生成源诊断，避免把非法泛型合同误判为成功生成
- `2026-04-20` 已完成一轮 registrar reflection 路径收敛：
  - `CqrsHandlerRegistrar` 现会按 `Type` 弱键缓存已筛选且排序好的 supported handler interface 列表
  - 同一 handler 类型跨容器重复注册时，不再重复执行 `GetInterfaces()` 与支持接口筛选
  - `GFramework.Cqrs.Tests` 已补充 registrar 静态缓存隔离与 supported interface 缓存复用回归
- `2026-04-20` 已完成一轮 registrar 去重路径收敛：
  - `CqrsHandlerRegistrar` 现会在单次 reflection 注册流程开始时构建已注册 handler 映射索引
  - 同一批注册中后续 duplicate handler mapping 不再重复线性扫描 `IServiceCollection`
  - `GFramework.Cqrs.Tests` 已补充“程序集返回重复 handler 类型时仍只注册一份映射”的回归
- `2026-04-29` 已完成一轮 generator fallback 元数据收敛：
  - `CqrsHandlerRegistryGenerator` 现会探测 runtime 是否同时支持 `params string[]` 与 `params Type[]` 两类 `CqrsReflectionFallbackAttribute` 构造函数
  - 当本轮 fallback handlers 全部可被生成代码直接引用时，生成器会优先发射 `typeof(...)` 形式的程序集级 fallback 元数据，减少运行时 `Assembly.GetType(...)` 回查
  - 当 fallback handlers 中仍存在不能直接引用的实现类型时，生成器继续统一发射字符串元数据，避免 mixed 场景只恢复部分 handlers
  - `GFramework.SourceGenerators.Tests` 已补充 runtime 同时暴露两类构造函数时优先选择直接 `Type` 元数据的回归
- `2026-04-29` 已完成一轮 mixed fallback 元数据拆分：
  - `CqrsReflectionFallbackAttribute` 现显式允许 `AllowMultiple = true`
  - `CqrsHandlerRegistryGenerator` 现会探测 runtime 是否允许多个 fallback 特性实例
  - 当本轮 fallback 同时包含可直接引用与仅能按名称恢复的 handlers，且 runtime 同时支持 `Type[]`、`string[]` 和多实例特性时，生成器会拆分输出两段 fallback 元数据
  - `GFramework.Cqrs.Tests` 已补充 mixed fallback metadata 回归，锁定 registrar 只对字符串条目执行定向 `Assembly.GetType(...)`
  - `GFramework.SourceGenerators.Tests` 已补充 mixed fallback emission 回归，锁定 generator 会输出两个程序集级 fallback 特性实例而不是整体退回字符串
- `2026-04-29` 已重新执行 `$gframework-pr-review`：
  - 当前分支对应 `PR #302`，状态为 `OPEN`
  - latest reviewed commit 当前剩余 `3` 条 open AI review threads：`2` 条 Greptile、`1` 条 CodeRabbit
  - 本地核对后确认 `dotnet-format` 仍只有 `Restore operation failed` 噪音，没有附带当前仍成立的文件级格式诊断
  - 已按 review triage 修正 generator source preamble 的多实例 fallback 特性排版、移除死参数，并补强 mixed/direct fallback 发射回归断言与 XML 文档
- `2026-04-30` 已重新执行 `$gframework-pr-review`：
  - 当前分支对应 `PR #305`，状态为 `OPEN`
  - 当前抓取到 `9` 条 CodeRabbit open threads、`2` 条 Greptile open threads；远端 CTRF 汇总为 `2214/2214` passed，MegaLinter 仍只暴露 `dotnet-format` 的 `Restore operation failed` 环境噪音
  - 本地核对后，已确认以下评论仍然成立并已完成修正：`ArchitectureContextTests` 并发测试失败路径释放、`CqrsGeneratedRequestInvokerProviderTests` 的全局 logger provider 恢复与私有缓存断言解耦、`CqrsArchitectureContextIntegrationTests` 的真实上下文注入断言、`GeneratedRequestInvokerRequest` / `INotificationPublisher` XML 文档、`CqrsHandlerRegistrar` 的 provider 注册顺序、`CqrsTestRuntime` 的 legacy alias 显式失败模式，以及 `cqrs-rewrite` trace 重复标题
  - 对于 `ICqrsRequestInvokerProvider` / generated `TryGetDescriptor(...)` 相关 Greptile 评论，本地评估后未改 dispatcher 热路径语义；改为补齐公开注释与生成器方法级注释，明确默认 runtime 只在注册阶段经 `IEnumeratesCqrsRequestInvokerDescriptors` 预热缓存，`TryGetDescriptor(...)` 保留为显式查询 seam
  - 本轮额外修正了 `GFramework.SourceGenerators.Tests` 中先读取 `GeneratedSources[0]` 再断言长度的脆弱顺序，并将 `ArchitectureContextTests` 的并发 orchestration 收敛到公共 helper，消除本轮引入的 `MA0051` warning
- `2026-04-29` 已完成一轮 precise runtime type lookup 的数组回归补强：
  - `GFramework.SourceGenerators.Tests` 已新增多维数组、交错数组、外部程序集隐藏元素类型三类回归
  - 当前生成器在 precise runtime type lookup 下已稳定保留数组秩信息，并递归发射交错数组的 `MakeArrayType()` 链
  - 本轮定向测试未暴露数组发射缺陷，因此未改动 fallback 合同选择逻辑，也未调整 direct / named / mixed fallback 排版路径
- `2026-04-29` 已补齐一轮外部程序集隐藏泛型定义回归覆盖：
  - `GFramework.SourceGenerators.Tests` 已新增“外部程序集隐藏泛型定义 + 可见类型实参”的 precise registration 回归
  - 当前生成器会继续为这类 handler 合同发射 `ResolveReferencedAssemblyType(...) + MakeGenericType(...)` 组合，而不是退回字符串 fallback 元数据
  - 本轮定向测试未暴露新的实现缺口，因此未改动 direct / named / mixed fallback 选择逻辑，也未调整 generator runtime type 建模实现
- `2026-04-29` 已完成一轮缓存工厂闭包收敛：
  - `CqrsDispatcher` 现会在 notification / stream / request binding 与 pipeline executor 缓存入口优先使用无捕获工厂
  - `CqrsHandlerRegistrar` 现会在程序集元数据缓存与可加载类型缓存入口复用 `static` 工厂 + 显式状态参数
  - 本轮未改动公开语义，也未修改 fallback 合同与 handler / behavior 生命周期边界
- `2026-04-29` 已完成一轮 request pipeline executor 形状缓存：
  - `CqrsDispatcher` 现会继续按 `requestType + responseType` 缓存 request dispatch binding，并在 binding 内按 `behaviorCount` 缓存强类型 pipeline executor
  - 每次分发只绑定当前 handler / behaviors 实例，不缓存容器解析结果，因此不改变 transient 生命周期与上下文注入语义
  - `GFramework.Cqrs.Tests` 已补充 executor 首次创建 / 后续复用与双行为顺序回归
- `2026-04-29` 已完成一轮 cached executor 上下文刷新回归补强：
  - `GFramework.Cqrs.Tests` 已新增 `DispatcherPipelineContextRefresh*` 测试替身，分别记录 request handler 与 pipeline behavior 在每次分发中实际观察到的实例身份与 `ArchitectureContext`
  - `CqrsDispatcherCacheTests` 现明确断言：同一个 cached request pipeline executor 在重复分发时会继续命中同一 executor 形状，但不会跨分发保留旧上下文
  - 本轮定向测试未暴露新的 runtime 缺口，因此没有改动 `GFramework.Cqrs/Internal/CqrsDispatcher.cs`
- `2026-04-29` 已完成一轮 cached notification / stream binding 上下文刷新回归补强：
  - `GFramework.Cqrs.Tests` 已新增 `DispatcherNotificationContextRefresh*` 与 `DispatcherStreamContextRefresh*` 测试替身，分别记录 notification handler 与 stream handler 在重复分发时观察到的实例身份与 `ArchitectureContext`
  - `CqrsDispatcherCacheTests` 现明确断言：同一个 cached notification / stream dispatch binding 在重复分发时会继续命中同一 binding，但不会跨分发保留旧上下文
  - 本轮定向测试未暴露新的 runtime 缺口，因此没有改动 `GFramework.Cqrs/Internal/CqrsDispatcher.cs`
- `2026-04-29` 已完成一轮 dispatcher 上下文前置条件失败语义回归：
  - `GFramework.Cqrs.Tests/Cqrs/CqrsDispatcherContextValidationTests.cs` 已通过公开工厂 `CqrsRuntimeFactory.CreateRuntime(...)` 锁定默认 dispatcher 的失败语义
  - 当 context-aware request / notification / stream handler 遇到仅实现 `ICqrsContext`、但未实现 `IArchitectureContext` 的上下文时，dispatcher 会在调用前显式抛出 `InvalidOperationException`
  - 本轮只补测试，不改 runtime 实现与文档口径
- `2026-04-29` 已接受一轮 delegated registrar fallback 失败分支测试：
  - `GFramework.Cqrs.Tests/Cqrs/CqrsHandlerRegistrarFallbackFailureTests.cs` 已覆盖 named fallback 无法解析、named fallback 解析抛异常、direct fallback 跨程序集三类 warning 语义
  - 主线程已复核该新文件并重新执行定向测试，确认当前 registrar 在 fallback 元数据失效时仍保持“跳过条目 + 记录告警”的既有语义
- `2026-04-29` 已接受一轮 delegated 叶子级 fallback 合同测试：
  - `GFramework.Cqrs.Tests/Cqrs/CqrsReflectionFallbackAttributeTests.cs` 已锁定空 marker、字符串 fallback 名称去空/去重/排序、直接 `Type` fallback 去空/去重/排序与空参数数组防御语义
  - 当前 runtime 读取程序集级 fallback 元数据时所依赖的 attribute 归一化合同，现已有独立叶子级测试文件覆盖
- `2026-04-29` 已完成一轮 CQRS 入口文档对齐：
  - `GFramework.Cqrs/README.md`、`docs/zh-CN/core/cqrs.md` 与 `docs/zh-CN/api-reference/index.md` 现已明确 generated registry 优先、targeted fallback 补齐剩余 handler 的当前语义
- `2026-04-29` 已完成一轮 generator pointer runtime-reconstruction 残留清理：
  - `CqrsHandlerRegistryGenerator` 的运行时类型引用模型已移除不可达的 pointer 子结构
  - `SourceEmission` 不再保留 `MakePointerType()` 源码发射分支，`RuntimeTypeReferences` 也已删掉对应的外部程序集递归扫描死代码
  - pointer / function pointer 的拒绝语义保持不变，direct / named / mixed fallback 逻辑未改动
  - 当前工作区相对 `origin/main` 的累计 diff 已达到 `14 files`，仍低于本轮 `gframework-batch-boot 50` 的主要 stop condition
- `2026-04-30` 已完成一轮 `CQRS vs Mediator` 结构化评估：
  - 生产依赖与默认 runtime 接线层面，`GFramework.Cqrs` 已完成对外部 `Mediator` 的替代
  - 仓库内部收口层面，旧 `Command` / `Query` API、`LegacyICqrsRuntime` 别名、fallback 空 marker 兼容语义与
    `Mediator` 测试命名仍然存在
  - 设计吸收层面，当前已吸收统一消息模型、generator 优先注册与反射收敛思路；仍未完整吸收 publisher 策略抽象、
    stream / exception pipeline、telemetry / diagnostics / benchmark 体系与 runtime 主体生成
  - 详细结论与证据已归档到 `archive/todos/cqrs-vs-mediator-assessment-rp063.md`
- `2026-04-30` 已接受两条只读 subagent 结论并完成 notification publisher seam 最小实现：
  - 相对 `ai-libs/Mediator`，本轮只吸收 notification publisher 的策略接缝，不照搬 `NotificationHandlers<T>` 包装、
    并行 publisher 或异常聚合语义
  - 当前 seam 刻意保持在默认 runtime 内部：`ICqrsRuntime.PublishAsync(...)` 外形不变，dispatcher 仍负责 handler 解析与
    `IContextAware` 上下文注入
  - 用户若需替换通知发布策略，只需在 runtime 创建前向容器显式注册 `INotificationPublisher`
- `2026-04-30` 已接受三条 worker 切片并完成一轮测试命名收口：
  - 三个 worker 分别独立拥有一份 `GFramework.Cqrs.Tests/Mediator/*.cs` 文件，主线程只做集成验证与后续追踪更新
  - 当前分支已不再保留 `GFramework.Cqrs.Tests/Mediator/` 目录下的生产内涵测试，相关文件均迁移到 `GFramework.Cqrs.Tests/Cqrs/`
  - 本轮没有修改测试行为，只收口命名、注释、局部变量与嵌套测试类型语义
- 当前主线优先级：
  - dispatch/invoker 反射占比继续下降，并优先评估生成前移方案
  - 基于已落地 publisher seam，继续评估是否需要公开配置面、并行策略或 telemetry decorator
  - package / facade / 兼容层继续收口
  - pipeline 分层扩展、可观测性 seam 与 benchmark baseline 进入中期候选

## 当前风险

- 当前 `dotnet build GFramework.sln -c Release` 在 WSL 环境仍会受顶层 `GFramework.csproj` 的 Windows NuGet fallback 配置影响
- 当前 `GFramework.Cqrs.Tests` 仍直接引用 `GFramework.Core`，说明测试已按模块意图拆分，但 runtime 物理迁移尚未完全切断依赖
- 当前对外替代已基本完成，但若不单独规划旧 `Command` / `Query`、`LegacyICqrsRuntime` 与测试命名的收口顺序，
  后续仍会持续混淆“生产替代已完成”与“仓库内部收口未完成”这两个不同结论

## 活跃文档

- 历史跟踪归档：[cqrs-rewrite-history-through-rp043.md](../archive/todos/cqrs-rewrite-history-through-rp043.md)
- 验证历史归档：[cqrs-rewrite-validation-history-through-rp062.md](../archive/todos/cqrs-rewrite-validation-history-through-rp062.md)
- CQRS 与 Mediator 评估归档：[cqrs-vs-mediator-assessment-rp063.md](../archive/todos/cqrs-vs-mediator-assessment-rp063.md)
- 历史 trace 归档：[cqrs-rewrite-history-through-rp043.md](../archive/traces/cqrs-rewrite-history-through-rp043.md)
- `RP-046` 至 `RP-061` trace 归档：[cqrs-rewrite-history-rp046-through-rp061.md](../archive/traces/cqrs-rewrite-history-rp046-through-rp061.md)

## 验证说明

- `RP-043` 之前的详细阶段记录、定向验证命令和阶段性决策均已移入主题内归档
- `RP-046` 至 `RP-062` 的历史验证命令与阶段性结果已移入验证归档，active tracking 只保留当前恢复入口需要的最新验证
- `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --format json --json-output /tmp/current-pr-review.json`
  - 结果：通过
  - 备注：确认当前分支对应 `PR #305`，并定位到仍需本地复核的 CodeRabbit / Greptile open thread
- `dotnet build GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release`
  - 结果：通过
  - 备注：`0 warning / 0 error`；本轮确认 XML 文档补齐、`NonParallelizable`、`_syncRoot` 命名与 `ai-plan` 收敛未引入新增编译问题
- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsGeneratedRequestInvokerProviderTests|FullyQualifiedName~CqrsArchitectureContextIntegrationTests.Handler_Can_Access_Architecture_Context|FullyQualifiedName~CqrsArchitectureContextAdvancedFeaturesTests.Request_With_Retry_Behavior_Should_Succeed_On_First_Attempt|FullyQualifiedName~CqrsArchitectureContextAdvancedFeaturesTests.Transient_Error_Request_Should_Succeed_Without_Simulated_Errors"`
  - 结果：通过
  - 备注：`5/5` passed；覆盖 generated invoker provider、真实上下文注入与两条重命名高级行为测试
- `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~SendRequestAsync_Should_ResolveCqrsRuntime_OnlyOnce_When_AccessedConcurrently|FullyQualifiedName~PublishAsync_Should_ResolveCqrsRuntime_OnlyOnce_When_AccessedConcurrently|FullyQualifiedName~CreateStream_Should_ResolveCqrsRuntime_OnlyOnce_When_AccessedConcurrently"`
  - 结果：通过
  - 备注：`3/3` passed；确认并发首次解析测试在失败路径释放调整后保持通过
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~Emits_Request_Invoker_Provider_Metadata_When_Runtime_Contract_Is_Available|FullyQualifiedName~Emits_Direct_Type_Fallback_Metadata_When_All_Fallback_Handlers_Are_Referenceable_And_Runtime_Type_Contract_Is_Available|FullyQualifiedName~Emits_Mixed_Direct_Type_And_String_Fallback_Metadata_When_Runtime_Allows_Multiple_Fallback_Attributes"`
  - 结果：通过
  - 备注：`3/3` passed；确认 provider 生成分支注释与断言顺序修正未改变生成语义
- `dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release`
  - 结果：通过
  - 备注：构建成功；并行验证期间出现过 `MSB3026` 拷贝重试噪音，属于同时运行多个 `dotnet` 命令时的输出文件竞争，不是持久性编译 warning
- `bash scripts/validate-csharp-naming.sh`
  - 结果：通过
  - 备注：使用显式 `GIT_DIR` / `GIT_WORK_TREE` 绑定重跑后，`1045` 个 tracked C# 文件的命名校验全部通过；本轮 `_syncRoot` 改名未引入命名规则回归
- `dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release`
  - 结果：通过
  - 备注：`0 warning / 0 error`；本轮确认 notification publisher seam、README 与文档更新未引入 `GFramework.Cqrs` 构建告警
- `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release`
  - 结果：通过
  - 备注：`0 warning / 0 error`；确认 stream invoker provider 生成与显式枚举接口实现未引入生成器编译问题
- `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release`
  - 结果：通过
  - 备注：`0 warning / 0 error`；确认 stream invoker provider fixture 与回归断言可以编译通过
- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsGeneratedRequestInvokerProviderTests"`
  - 结果：通过
  - 备注：`4/4` passed；覆盖 generated request / stream invoker provider 的 registrar 接线与 dispatcher 消费语义
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_Request_Invoker_Provider_Metadata_When_Runtime_Contract_Is_Available|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_Stream_Invoker_Provider_Metadata_When_Runtime_Contract_Is_Available"`
  - 结果：通过
  - 备注：`2/2` passed；确认 generated registry 会同时发射 request / stream invoker provider 描述符与静态 invoker 方法
- `GIT_DIR=/mnt/f/gewuyou/System/Documents/WorkSpace/GameDev/GFramework/.git/worktrees/GFramework-cqrs GIT_WORK_TREE=/mnt/f/gewuyou/System/Documents/WorkSpace/GameDev/GFramework-WorkTree/GFramework-cqrs bash scripts/validate-csharp-naming.sh`
  - 结果：通过
  - 备注：`1059` 个 tracked C# 文件命名校验全部通过；本轮新增 stream invoker 类型与测试命名未引入回归
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_Request_Invoker_Provider_Metadata_For_Hidden_Implementation_With_Visible_Interface|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_Stream_Invoker_Provider_Metadata_For_Hidden_Implementation_With_Visible_Interface|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_Request_Invoker_Provider_Metadata_When_Runtime_Contract_Is_Available|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_Stream_Invoker_Provider_Metadata_When_Runtime_Contract_Is_Available"`
  - 结果：通过
  - 备注：`4/4` passed；确认 hidden implementation + visible interface 场景也会继续发射 request / stream invoker provider 元数据
- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsGeneratedRequestInvokerProviderTests"`
  - 结果：通过
  - 备注：`8/8` passed；补齐 hidden implementation + visible interface 场景后，确认 generated request / stream invoker 在 runtime 侧也会优先命中 provider descriptor
- `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release`
  - 结果：通过
  - 备注：`0 warning / 0 error`；确认本轮 precise reflected invoker provider 合同回归未引入 generator 编译告警
- `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release`
  - 结果：通过
  - 备注：`0 warning / 0 error`；并行验证时曾出现过 `MSB3026` 输出文件竞争噪音，随后已串行重跑并得到干净构建结果
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Request_Invoker_Provider_Metadata_For_Precise_Reflected_Request_Registrations|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Stream_Invoker_Provider_Metadata_For_Precise_Reflected_Stream_Registrations|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_Request_Invoker_Provider_Metadata_For_Hidden_Implementation_With_Visible_Handler_Interface|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_Stream_Invoker_Provider_Metadata_For_Hidden_Implementation_With_Visible_Handler_Interface"`
  - 结果：通过
  - 备注：`4/4` passed；串行确认 visible-interface hidden-implementation 仍发射 provider 元数据，而 precise reflected 注册继续保持“不发射 provider descriptor”的当前合同
- `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release`
  - 结果：通过
  - 备注：并行执行 build/test 时出现 `MSB3026` 输出文件竞争噪音；无真实编译错误，后续以串行 test 结果作为本轮 authoritative 行为验证
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Request_Invoker_Provider_Metadata_When_Runtime_Lacks_Request_Provider_Interface|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Request_Invoker_Provider_Metadata_When_Runtime_Lacks_Request_Descriptor_Enumerator|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Stream_Invoker_Provider_Metadata_When_Runtime_Lacks_Stream_Provider_Interface|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Stream_Invoker_Provider_Metadata_When_Runtime_Lacks_Stream_Descriptor_Enumerator|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_Request_Invoker_Provider_Metadata_When_Runtime_Contract_Is_Available|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_Stream_Invoker_Provider_Metadata_When_Runtime_Contract_Is_Available"`
  - 结果：通过
  - 备注：`6/6` passed；锁定 request / stream provider gate 依赖“provider 接口 + descriptor 枚举接口”同时存在，且原有 happy-path 发射仍保持通过
- `dotnet build GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release`
  - 结果：通过
  - 备注：并行执行 build/test 时出现 `MSB3026` 输出文件竞争噪音；当前已确认没有新增 analyzer warning，`GFramework.Cqrs.Tests` 仍能完成 Release 构建
- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsGeneratedRequestInvokerProviderTests.SendAsync_Should_Throw_When_Generated_Request_Invoker_Is_Not_Static|FullyQualifiedName~CqrsGeneratedRequestInvokerProviderTests.SendAsync_Should_Throw_When_Generated_Request_Invoker_Is_Incompatible|FullyQualifiedName~CqrsGeneratedRequestInvokerProviderTests.CreateStream_Should_Throw_When_Generated_Stream_Invoker_Is_Not_Static|FullyQualifiedName~CqrsGeneratedRequestInvokerProviderTests.CreateStream_Should_Throw_When_Generated_Stream_Invoker_Is_Incompatible|FullyQualifiedName~CqrsGeneratedRequestInvokerProviderTests.SendAsync_Should_Use_Generated_Request_Invoker_When_Provider_Is_Registered|FullyQualifiedName~CqrsGeneratedRequestInvokerProviderTests.CreateStream_Should_Use_Generated_Stream_Invoker_When_Provider_Is_Registered"`
  - 结果：通过
  - 备注：`6/6` passed；确认 request / stream 的非法 generated invoker 现统一抛出 `InvalidOperationException`，且原有 happy-path 未回归
- `dotnet build GFramework.Core/GFramework.Core.csproj -c Release`
  - 结果：通过
  - 备注：`0 warning / 0 error`；确认 `CqrsRuntimeModule` 接线变更未引入 `GFramework.Core` 模块构建问题
- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsNotificationPublisherTests"`
  - 结果：通过
  - 备注：`5/5` 通过；覆盖自定义 publisher 顺序、上下文注入、零处理器、首错即停与默认接线复用
- `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~MicrosoftDiContainerTests"`
  - 结果：通过
  - 备注：`41/41` 通过；确认 CQRS 基础设施默认接线与容器行为未回归
- `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~MicrosoftDiContainerTests"`
  - 结果：通过
  - 备注：`42/42` 通过；本轮新增 legacy alias 回填回归后，确认正式 seam 与旧命名空间 alias 仍指向同一实例
- `dotnet build GFramework.Core/GFramework.Core.csproj -c Release`
  - 结果：通过
  - 备注：`0 warning / 0 error`；确认 legacy alias helper 收敛与文档更新未引入 `GFramework.Core` 模块构建告警
- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsGeneratedRequestInvokerProviderTests|FullyQualifiedName~CqrsHandlerRegistrarTests|FullyQualifiedName~CqrsDispatcherCacheTests"`
  - 结果：通过
  - 备注：`22/22` 通过；确认 generated request invoker provider 的 registrar 接线、dispatcher 消费与现有 request/notification/stream cache 语义未回归
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_Request_Invoker_Provider_Metadata_When_Runtime_Contract_Is_Available"`
  - 结果：通过
  - 备注：`1/1` 通过；锁定 generator 会在 runtime 合同可用时发射 request invoker provider 成员
- `dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release`
  - 结果：通过
  - 备注：`0 warning / 0 error`；确认 request invoker provider seam 与 dispatcher/registrar 接线未引入新增构建告警
- `dotnet build GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release`
  - 结果：通过
  - 备注：`0 warning / 0 error`；确认三份 `Mediator` 命名收口后的 CQRS 测试项目构建仍然干净
- `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~ArchitectureContextTests"`
  - 结果：通过
  - 备注：`22/22` 通过；新增 `PublishAsync` / `CreateStream` 并发首次访问只解析一次 `ICqrsRuntime` 的回归

## 下一步

1. 在保持 branch diff 明显低于 `50 files` 的前提下，继续挑选下一批低风险 `dispatch/invoker` 收敛切片，并优先考虑 request / stream provider 的剩余 runtime 失败边界或 generator gate 合同补强
2. 基于已落地的 notification publisher seam，评估是否需要第二阶段公开配置面、并行 publisher 或 telemetry decorator
3. 单独规划旧 `Command` / `Query` API 的收口顺序；`LegacyICqrsRuntime` compatibility slice 已收口到显式 helper 与专门测试，可暂时移出最高优先级
