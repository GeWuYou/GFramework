# CQRS 重写迁移跟踪

## 目标

围绕 `GFramework` 当前的双轨 CQRS 现状，继续完成以“去外部依赖、降低反射、收口公开入口”为目标的
CQRS 迁移与收敛。

## 当前恢复点

- 恢复点编号：`CQRS-REWRITE-RP-059`
- 当前阶段：`Phase 8`
- 当前焦点：
  - 当前功能历史已归档，active 跟踪仅保留 `Phase 8` 主线的恢复入口
  - 已将 mixed fallback 场景进一步收敛：当 runtime 允许同一程序集声明多个 `CqrsReflectionFallbackAttribute` 实例时，generator 现会把可直接引用的 fallback handlers 与仅能按名称恢复的 fallback handlers 拆分发射
  - `CqrsReflectionFallbackAttribute` 现允许多实例，以承载 `Type[]` 与字符串 fallback 元数据的组合输出
  - 已将 generator 的程序集级 fallback 元数据进一步收敛：当全部 fallback handlers 都可直接引用且 runtime 暴露 `params Type[]` 合同时，生成器现优先发射 `typeof(...)` 形式的 fallback 元数据
  - 当 runtime 不支持多实例 fallback 特性或缺少对应构造函数时，mixed fallback 场景仍会整体保守回退到字符串元数据，避免仅部分 handler 走 `Type[]` 时漏掉剩余需按名称恢复的 handlers
  - 已完成 request pipeline executor 形状缓存：`CqrsDispatcher` 现会在单个 request binding 内按 `behaviorCount` 复用强类型 pipeline executor，而不是每次 `SendAsync` 都重建整条 `next` 委托链
  - 已补充 dispatcher pipeline executor 缓存与双行为顺序回归，锁定缓存复用后仍保持现有行为执行顺序
  - 已补充 cached request pipeline executor 的上下文刷新回归，锁定 executor 复用时仍会为当次 handler / singleton behavior 重新注入当前 `ArchitectureContext`
  - 已补充 cached notification / stream dispatch binding 的上下文刷新回归，锁定 binding 复用时仍会为当次 handler 重新注入当前 `ArchitectureContext`
  - 已完成 generated registry 激活路径收敛：`CqrsHandlerRegistrar` 现优先复用缓存工厂委托，避免重复 `ConstructorInfo.Invoke`
  - 已补充私有无参构造 generated registry 的回归测试，确保兼容现有生成器产物
  - 已修正 pointer / function pointer 泛型合同的错误覆盖：生成器不再为这两类类型发射 precise runtime type 重建代码
  - 已补充非法 CQRS 泛型合同的输入诊断断言，明确 `CS0306` 与 fallback / diagnostic 路径的组合语义
  - 已为 registrar 的 reflection 注册路径补充 handler-interface 元数据缓存，减少跨容器重复注册时的 `GetInterfaces()` 反射
  - 已将 registrar 的重复映射判定从线性扫描 `IServiceCollection` 收敛为本地映射索引，减少 fallback 注册路径的重复查找
  - 已完成一轮 `static lambda + state` 微收敛：`CqrsDispatcher` 与 `CqrsHandlerRegistrar` 现会在弱缓存 / 并发缓存入口优先使用无捕获工厂，继续压低热路径上的额外闭包分配
  - 已补充 `CqrsReflectionFallbackAttribute` 叶子级合同测试，锁定空 marker、字符串 fallback 名称归一化、直接 `Type` fallback 归一化与空参数防御语义
  - 中期上继续 `Phase 8` 主线：参考 `ai-libs/Mediator`，继续扩大 generator 覆盖，并选择下一个收益明确的 dispatch / invoker 反射收敛点

## 当前状态摘要

- 已完成 `Mediator` 外部依赖移除、CQRS runtime 重建、默认架构接线和显式程序集 handler 注册入口
- 已完成 `GFramework.Cqrs.Abstractions` / `GFramework.Cqrs` 项目骨架与 runtime seam 收敛
- 已完成 handler registry generator 的多轮收敛，当前合法 closed handler contract 已统一收敛到更窄的注册路径
- 已完成一轮公开入口文档与 source-generator 命名空间收口
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
- 当前主线优先级：
  - generator 覆盖面继续扩大
  - dispatch/invoker 反射占比继续下降
  - package / facade / 兼容层继续收口

## 当前风险

- 当前 `dotnet build GFramework.sln -c Release` 在 WSL 环境仍会受顶层 `GFramework.csproj` 的 Windows NuGet fallback 配置影响
- 当前 `GFramework.Cqrs.Tests` 仍直接引用 `GFramework.Core`，说明测试已按模块意图拆分，但 runtime 物理迁移尚未完全切断依赖
- `RegisterMediatorBehavior`、`MediatorCoroutineExtensions` 与 `ContextAwareMediator*Extensions` 仍作为兼容层存在，未来真正移除时仍需单独规划弃用窗口

## 活跃文档

- 历史跟踪归档：[cqrs-rewrite-history-through-rp043.md](../archive/todos/cqrs-rewrite-history-through-rp043.md)
- 历史 trace 归档：[cqrs-rewrite-history-through-rp043.md](../archive/traces/cqrs-rewrite-history-through-rp043.md)

## 验证说明

- `RP-043` 之前的详细阶段记录、定向验证命令和阶段性决策均已移入主题内归档
- active 跟踪文件只保留当前恢复点、当前活跃事实、风险和下一步，避免 `boot` 在默认入口中重复扫描 1000+ 行历史 trace
- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsDispatcherCacheTests"`
  - 结果：通过
  - 备注：`5/5` 测试通过；本轮新增 cached executor 上下文刷新回归，确认 executor 复用时仍按当次分发重新注入上下文
- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsReflectionFallbackAttributeTests"`
  - 结果：通过
  - 备注：`5/5` 测试通过；本轮锁定 fallback attribute 的公开归一化合同与空参数防御语义
- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsDispatcherCacheTests"`
  - 结果：通过
  - 备注：`7/7` 测试通过；本轮新增 cached notification / stream binding 上下文刷新回归，确认 binding 复用时仍按当次分发重新注入上下文
- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --no-restore -p:RestoreFallbackFolders= -m:1 -nodeReuse:false`
  - 结果：通过
  - 备注：`63/63` 测试通过；当前沙箱限制了 MSBuild named pipe，验证需在提权环境下运行
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore -p:RestoreFallbackFolders= -m:1 -nodeReuse:false --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests"`
  - 结果：通过
  - 备注：`14/14` 测试通过；本轮覆盖 pointer / function pointer 合同拒绝、fallback 诊断与现有精确注册路径
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore -p:RestoreFallbackFolders= -m:1 -nodeReuse:false --filter "FullyQualifiedName~Reports_Compilation_Error_And_Skips_Precise_Registration_For_Hidden_Pointer_Response|FullyQualifiedName~Reports_Diagnostic_And_Skips_Registry_When_Fallback_Metadata_Is_Required_But_Runtime_Contract_Lacks_Fallback_Attribute|FullyQualifiedName~Emits_Assembly_Level_Fallback_Metadata_When_Fallback_Is_Required_And_Runtime_Contract_Is_Available"`
  - 结果：通过
  - 备注：`3/3` 测试通过；本轮直接覆盖 PR #261 指向的 3 个 pointer / function pointer 回归场景
- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --no-restore -p:RestoreFallbackFolders= -m:1 -nodeReuse:false --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsHandlerRegistrarTests"`
  - 结果：通过
  - 备注：`11/11` 测试通过；本轮覆盖 registrar 的 supported handler interface 缓存与 duplicate mapping 去重路径
- `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release`
  - 结果：通过
  - 备注：`0 warning / 0 error`
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests"`
  - 结果：通过
  - 备注：`17/17` 测试通过；本轮覆盖字符串 fallback 合同兼容路径与直接 `Type` fallback 元数据优先级
- `dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release`
  - 结果：通过
  - 备注：`0 warning / 0 error`
- `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release`
  - 结果：通过
  - 备注：`0 warning / 0 error`
- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsHandlerRegistrarTests"`
  - 结果：通过
  - 备注：`13/13` 测试通过；本轮覆盖 mixed fallback metadata 的 registrar 消费路径
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests"`
  - 结果：通过
  - 备注：`18/18` 测试通过；本轮覆盖 mixed fallback metadata 的双特性发射路径
- `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output /tmp/gframework-pr-review.json`
  - 结果：通过
  - 备注：确认当前分支对应 `PR #302`；latest head review 仍有 `3` 条 open AI threads，其中 MegaLinter 仅报告 `dotnet-format` restore failure 噪音
- `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release`
  - 结果：通过
  - 备注：`0 warning / 0 error`
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests"`
  - 结果：通过
  - 备注：`18/18` 测试通过；本轮直接覆盖 fallback preamble 排版与特性个数断言收紧
- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsHandlerRegistrarTests"`
  - 结果：通过
  - 备注：`13/13` 测试通过；本轮确认 mixed fallback metadata 的 registrar 消费路径未回归
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests"`
  - 结果：通过
  - 备注：`21/21` 测试通过；本轮新增多维数组、交错数组与外部程序集隐藏元素类型的 precise runtime type lookup 回归
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests"`
  - 结果：通过
  - 备注：`22/22` 测试通过；本轮新增“外部程序集隐藏泛型定义 + 可见类型实参”的 precise registration 回归，确认仍走定向运行时类型重建
- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsDispatcherCacheTests"`
  - 结果：通过
  - 备注：`4/4` 测试通过；本轮覆盖 request pipeline executor 的首次创建、复用与双行为顺序回归
- `dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release`
  - 结果：通过
  - 备注：`0 warning / 0 error`；本轮确认 dispatcher request pipeline 形状缓存未破坏 `net8.0` / `net9.0` / `net10.0` 目标构建
- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~RegisterHandlers_Should_Cache_Assembly_Metadata_Across_Containers|FullyQualifiedName~RegisterHandlers_Should_Cache_Loadable_Types_Across_Containers|FullyQualifiedName~Dispatcher_Should_Cache_Request_Pipeline_Executors_Per_Behavior_Count"`
  - 结果：通过
  - 备注：`3/3` 测试通过；本轮确认无捕获缓存工厂没有破坏 registrar / dispatcher 现有缓存行为
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests"`
  - 结果：通过
  - 备注：`22/22` 测试通过；本轮新增外部程序集隐藏泛型定义的 precise registration 回归
- `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release`
  - 结果：通过
  - 备注：`0 warning / 0 error`；本轮确认删除 pointer runtime-reconstruction 残留后生成器项目仍可正常构建
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests"`
  - 结果：通过
  - 备注：`22/22` 测试通过；本轮确认 pointer / function pointer 拒绝语义保持不变，且未回归既有 precise runtime type lookup 场景

## 下一步

1. 继续 `Phase 8` 主线，优先再找一个收益明确且写集独立的 generator 或 registrar/dispatcher 热点；当前工作区若提交主线程 notification / stream 回归批次，相对 `origin/main` 的累计 diff 将达到 `29 files`，仍低于本轮 `gframework-batch-boot 50` 的主要 stop condition
2. 若继续文档主线，优先再扫教程入口页与 API 参考中的 CQRS 采用说明，确认是否还有旧 Command / Query 迁移口径残留
3. 若后续再出现新的 PR review 或 review thread 变化，再重新执行 `$gframework-pr-review` 作为独立验证步骤
