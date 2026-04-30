# CQRS 重写迁移追踪

## 2026-04-30

### 阶段：precise reflected invoker provider 合同边界回归（CQRS-REWRITE-RP-071）

- 在 `RP-070` 提交后继续按 `gframework-batch-boot 50` 执行；当前已提交 branch diff 仍为 `24 files`，headroom 充足，因此继续下一批 generator-only 合同收敛
- 本轮先接受一条只读 subagent 的候选建议，评估是否可把 `PreciseReflectedRegistrationSpec` 的某个安全子集也纳入 request / stream provider 发射
- 主线程复核 `TryCreatePreciseReflectedRegistration(...)`、`CreateRequestInvokerEmissions(...)` / `CreateStreamInvokerEmissions(...)` 与现有 precise 测试素材后确认：
  - precise reflected 分支之所以存在，正是因为 handler interface 的请求或响应类型无法完全通过 `typeof(...)` 稳定表达
  - 当前 provider descriptor 合同需要直接发射 `typeof(requestType)` / `typeof(responseType)`；因此不存在可无条件放宽的“安全子集”
  - 本轮最终不改生产 generator，而是把这条边界显式固化到回归测试，避免后续误把不存在的子集当成已支持能力
- 主线程已完成：
  - `GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs` 新增两条回归，分别锁定 request / stream 的 precise reflected 注册不会发射 invoker provider 元数据
  - 同一组定向测试同时复核 hidden-implementation + visible-interface 场景仍会继续发射 provider 元数据，确保“允许发射”和“继续排除”的边界没有被本轮测试收紧弄混

### 验证（RP-071）

- `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`
- `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`
  - 备注：并行验证时曾出现 `MSB3026` 输出文件竞争噪音，随后已串行重跑同批命令并取得干净结果
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Request_Invoker_Provider_Metadata_For_Precise_Reflected_Request_Registrations|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Stream_Invoker_Provider_Metadata_For_Precise_Reflected_Stream_Registrations|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_Request_Invoker_Provider_Metadata_For_Hidden_Implementation_With_Visible_Handler_Interface|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_Stream_Invoker_Provider_Metadata_For_Hidden_Implementation_With_Visible_Handler_Interface"`
  - 结果：通过，`4/4` passed
- `git diff --name-only origin/main...HEAD | wc -l`
  - 结果：通过
  - 备注：当前相对 `origin/main` 的已提交 branch diff 仍为 `24 files`
- `git diff --numstat origin/main...HEAD`
  - 结果：通过
  - 备注：当前相对 `origin/main` 的工作分支累计 diff 为 `1793 changed lines`

### 当前下一步（RP-071）

1. 先提交本轮 generator 合同边界回归，保持恢复点、trace 与已验证测试状态一致
2. 继续挑选下一批低风险切片，优先考虑 request / stream provider 的 runtime 或 generator 诊断边界，而不是贸然扩大 precise reflected 支持面
3. 若下一批仍可拆分为非冲突文件，再恢复只读 / 写入 subagent 的分工方式压低主线程上下文
### 阶段：hidden-implementation generated invoker runtime 回归补强（CQRS-REWRITE-RP-070）

- 在 `5a77e2fb` 提交后补齐 active `ai-plan` 恢复入口，继续按 `gframework-batch-boot 50` 执行，基线仍为当前本地 `origin/main`
- 当前已提交 branch diff 复算为 `24 files / 1754 changed lines`，仍低于主要 stop condition，因此本轮只补 runtime 回归与恢复点，不改 generator / runtime 生产实现
- 本轮关键目标是把 `RP-069` 已落地的 hidden-implementation provider 发射范围补强，继续向 runtime 消费侧闭环，避免 active tracking 只记录了 generator 侧验证
- 主线程已完成：
  - `GFramework.Cqrs.Tests/Cqrs/CqrsGeneratedRequestInvokerProviderTests.cs` 新增 hidden-implementation + visible-interface 的 request / stream runtime 回归
  - `HiddenImplementationGeneratedRequestInvokerProviderRegistry`、`HiddenImplementationGeneratedStreamInvokerProviderRegistry` 与对应 container fixture 已被纳入同一组 provider 消费测试，锁定 registrar 接线与 dispatcher 优先命中 generated descriptor 的语义
  - 当前测试仍保持 `PreciseReflectedRegistrationSpec` 排除边界不变，不把隐藏 request/response 类型场景错误抬升为 runtime 支持承诺

### 验证（RP-070）

- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsGeneratedRequestInvokerProviderTests"`
  - 结果：通过，`8/8` passed
- `git diff --name-only origin/main...HEAD | wc -l`
  - 结果：通过
  - 备注：当前相对 `origin/main` 的已提交 branch diff 为 `24 files`
- `git diff --numstat origin/main...HEAD`
  - 结果：通过
  - 备注：当前相对 `origin/main` 的已提交 branch diff 为 `1754 changed lines`

### 当前下一步（RP-070）

1. 先提交本轮 `ai-plan` 恢复点更新，保持 batch 追踪与已提交代码状态一致
2. 在剩余 headroom 内继续选择下一批低风险 `dispatch/invoker` 收敛切片，优先考虑 request / stream provider 的诊断、入口或测试补强
3. 如下一批写集仍可拆分，再用只读 / 写入 subagent 分离非冲突切片，继续降低主线程上下文压力

### 阶段：generated stream invoker provider 最小落地（CQRS-REWRITE-RP-068）

- 继续按 `gframework-batch-boot 50` 执行，基线仍为当前本地 `origin/main`
- 本轮开始前，`origin/main` 已追平到当前 `HEAD`；因此 branch diff 重新归零，主 stop condition 仍为“相对 `origin/main` 接近 `50 files`”
- 当前批次沿用上一轮 request invoker provider 的设计形状，只做 stream 路径的最小对称扩展，避免把 notification publisher seam、pipeline 或 telemetry 一并卷入
- 本轮切片拆分：
  - worker：`GFramework.Cqrs/README.md`、`docs/zh-CN/core/cqrs.md`、`docs/zh-CN/source-generators/cqrs-handler-registry-generator.md`
  - worker：`GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs`
  - 主线程：`GFramework.Cqrs/Internal/CqrsDispatcher.cs`、`GFramework.Cqrs/Internal/CqrsHandlerRegistrar.cs`、
    `GFramework.Cqrs/*.cs` 新增 stream provider 契约、`GFramework.Cqrs.SourceGenerators/Cqrs/*`、
    `GFramework.Cqrs.Tests/Cqrs/CqrsGeneratedRequestInvokerProviderTests.cs`
- 主线程关键设计调整：
  - 继续保持 dispatcher 的 stream binding 静态缓存只依赖 `requestType + responseType`，不回调具体容器实例
  - stream provider 与 request provider 一样在 registrar 注册阶段一次性枚举 descriptor，并写入 dispatcher 的进程级弱缓存
  - generated registry 同时实现 request 与 stream 两组 descriptor 枚举契约时，改用显式接口实现 `GetDescriptors()`，避免同名方法冲突
- 已完成实现：
  - `GFramework.Cqrs` 新增 `ICqrsStreamInvokerProvider`、`IEnumeratesCqrsStreamInvokerDescriptors`、
    `CqrsStreamInvokerDescriptor` 与 `CqrsStreamInvokerDescriptorEntry`
  - `CqrsHandlerRegistrar` 新增 stream provider 接线与 descriptor 登记路径
  - `CqrsDispatcher` 新增 generated stream invoker 弱缓存，并在 `CreateStream(...)` 首次创建 stream binding 时优先消费 generated stream invoker 元数据
  - `CqrsHandlerRegistryGenerator` 新增 stream invoker registration 建模、descriptor 发射、显式枚举接口实现与 `InvokeStreamHandler{n}(...)` 静态桥接方法
  - `GFramework.Cqrs.Tests` 新增 `GeneratedStreamInvokerProviderRegistry`、`GeneratedStreamInvokerRequest`、`GeneratedStreamInvokerRequestHandler`，并扩充 `CqrsGeneratedRequestInvokerProviderTests`
  - `GFramework.Cqrs.SourceGenerators/README.md` 额外补齐模块级 README，对齐 generated stream invoker 语义
- worker 产出已接受：
  - 文档切片已把 request / stream invoker provider 作为并列 reader-facing 语义写入公开文档
  - generator 测试切片已补齐 stream invoker provider fixture 与断言；主线程根据最终实现把 request / stream 的 `GetDescriptors()` 断言统一收敛到显式接口实现版本

### 验证（RP-068）

- `dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`
- `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`
- `dotnet build GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`
- `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`
- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsGeneratedRequestInvokerProviderTests"`
  - 结果：通过，`4/4` passed
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_Request_Invoker_Provider_Metadata_When_Runtime_Contract_Is_Available|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_Stream_Invoker_Provider_Metadata_When_Runtime_Contract_Is_Available"`
  - 结果：通过，`2/2` passed
- `GIT_DIR=/mnt/f/gewuyou/System/Documents/WorkSpace/GameDev/GFramework/.git/worktrees/GFramework-cqrs GIT_WORK_TREE=/mnt/f/gewuyou/System/Documents/WorkSpace/GameDev/GFramework-WorkTree/GFramework-cqrs bash scripts/validate-csharp-naming.sh`
  - 结果：通过
- `git diff --name-only origin/main...HEAD | wc -l`
  - 结果：通过
  - 备注：当前相对 `origin/main` 的已提交 branch diff 为 `4 files`
- `git diff --numstat origin/main...HEAD`
  - 结果：通过
  - 备注：当前相对 `origin/main` 的已提交 branch diff 为 `217 changed lines`

### 当前下一步（RP-068）

1. 在保持 branch diff 远低于 `50 files` 阈值的前提下，继续评估下一个低风险 `dispatch/invoker` 收敛切片
2. 优先候选仍是 notification 路径是否值得引入同类 generated invoker seam，或继续补强 request / stream provider 的公开 API 入口与诊断语义
3. 下一批落地前先提交当前 stream provider 批次，避免未提交改动持续堆叠

### 阶段：generated invoker reflected-implementation 发射范围补强（CQRS-REWRITE-RP-069）

- 在 `RP-068` 提交后，重新复算 branch diff，相对 `origin/main` 升至 `20 files / 1015 changed lines`，仍明显低于 `gframework-batch-boot 50` 的 stop condition，因此继续下一批
- 本轮目标只收敛 source generator，不扩散到 runtime 或公开文档：把 generated request / stream invoker 的发射范围从“仅 direct registration”扩大到“实现类型隐藏、但 handler interface 可直接表达”的 reflected-implementation registration
- 接受只读 subagent 结论后确认：
  - 现有分类阶段已经为 reflected-implementation registration 保留了 request / stream invoker registration 元数据
  - 真正缺口只在 `CreateRequestInvokerEmissions(...)` 与 `CreateStreamInvokerEmissions(...)` 仍只遍历 `DirectRegistrations`
  - `PreciseReflectedRegistrationSpec` 继续排除在 provider 发射范围外，避免隐藏 request/response 类型导致生成源码不可编译
- 主线程已完成：
  - `ReflectedImplementationRegistrationSpec` 显式承载 request / stream invoker registration 元数据
  - `CreateRequestInvokerEmissions(...)` 与 `CreateStreamInvokerEmissions(...)` 现会同时消费 reflected-implementation registration
  - `GFramework.SourceGenerators.Tests` 已新增 hidden-implementation + visible-interface 两条 provider 回归
- 本轮不改 runtime：dispatcher / registrar 对 generated provider 的消费语义保持不变，变化只在 generator 愿意发射更多可安全静态表达的 descriptor

### 验证（RP-069）

- `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Generates_Direct_Interface_Registrations_For_Hidden_Implementation_When_Handler_Interface_Is_Public|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_Request_Invoker_Provider_Metadata_When_Runtime_Contract_Is_Available|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_Stream_Invoker_Provider_Metadata_When_Runtime_Contract_Is_Available"`
  - 结果：通过，`3/3` passed
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_Request_Invoker_Provider_Metadata_For_Hidden_Implementation_With_Visible_Interface|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_Stream_Invoker_Provider_Metadata_For_Hidden_Implementation_With_Visible_Interface|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_Request_Invoker_Provider_Metadata_When_Runtime_Contract_Is_Available|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_Stream_Invoker_Provider_Metadata_When_Runtime_Contract_Is_Available"`
  - 结果：通过，`4/4` passed

### 当前下一步（RP-069）

1. 提交当前 generator-only 批次，继续保持每个低风险切片可独立回滚与审查
2. 继续评估下一个能明显降低反射占比、但不需要同时改动 runtime 语义的切片

### 阶段：generated request invoker provider 最小落地（CQRS-REWRITE-RP-067）

- 继续按 `gframework-batch-boot 50` 执行，基线仍为本地现有 `origin/main`
- 在 `RP-066` 提交后复算 branch diff，相对 `origin/main` 增长到 `22 files`，仍明显低于 `50 files` stop condition，因此继续下一批
- 本轮 critical path 保持在主线程，本地完成 `dispatch/invoker` 生成前移的最小 request 切片；尝试委派 source-generator 测试给 worker 时因 subagent 名额已满失败，因此主线程直接接管该测试修改
- 本轮关键设计调整：
  - 不按 `requestType.Assembly` 做 provider 发现，避免“请求定义在 A、handler 与 generated registry 在 B”时漏掉 generated invoker
  - generated registry 若实现 `ICqrsRequestInvokerProvider`，registrar 会在激活 registry 后把 provider 注册进容器，并通过 `IEnumeratesCqrsRequestInvokerDescriptors` 把描述符写入 dispatcher 的进程级弱缓存
  - dispatcher 首次创建 request dispatch binding 时只按 `requestType + responseType` 读取静态弱缓存，不依赖具体容器实例；未命中时仍走既有反射创建路径
- 已完成实现：
  - `GFramework.Cqrs` 新增 `ICqrsRequestInvokerProvider`、`IEnumeratesCqrsRequestInvokerDescriptors`、
    `CqrsRequestInvokerDescriptor` 与 `CqrsRequestInvokerDescriptorEntry`
  - `CqrsHandlerRegistrar` 现会识别 generated registry 的 request invoker provider 能力，并登记 provider 与 request invoker 描述符
  - `CqrsDispatcher` 新增 generated request invoker 弱缓存，并在 request binding 创建时优先消费该元数据
  - `CqrsHandlerRegistryGenerator` 在 runtime 合同可用时，会让 generated registry 额外实现 request invoker provider 相关接口，并发射 descriptor 列表、`TryGetDescriptor(...)`、`GetDescriptors()` 与 request invoker 静态方法
- 已补充测试：
  - `CqrsGeneratedRequestInvokerProviderTests` 锁定 registrar 会注册 generated request invoker provider，且 dispatcher 走 generated invoker 后会返回 `generated:` 前缀结果
  - `CqrsHandlerRegistryGeneratorTests` 锁定 generated source 会包含 request invoker provider 接口、descriptor 条目与 `InvokeRequestHandler0(...)` 方法

### 验证（RP-067）

- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsGeneratedRequestInvokerProviderTests|FullyQualifiedName~CqrsHandlerRegistrarTests|FullyQualifiedName~CqrsDispatcherCacheTests"`
  - 结果：通过，`22/22` passed
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_Request_Invoker_Provider_Metadata_When_Runtime_Contract_Is_Available"`
  - 结果：通过，`1/1` passed
- `dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`

### 当前下一步（RP-067）

1. 评估 notification / stream invoker 是否值得沿同一 provider 模式继续前移，或先补 request provider 的公开说明与诊断语义
2. 继续在保持 branch diff 低于阈值的前提下推进下一批；当前相对 `origin/main` 的 branch diff 为 `22 files`

### 阶段：LegacyICqrsRuntime compatibility slice 收口（CQRS-REWRITE-RP-066）

- 继续按 `gframework-batch-boot 50` 执行，基线仍为本地现有 `origin/main`
- 在 `RP-065` 之后复算 branch diff，相对 `origin/main` 仍为 `19 files`，明显低于 `50 files` stop condition，因此继续下一批
- 本轮按“关键路径本地、非冲突文档委派”的方式拆成两个切片：
  - worker：`GFramework.Core.Abstractions/README.md`、`docs/zh-CN/abstractions/core-abstractions.md`、`docs/zh-CN/core/cqrs.md`
  - 主线程：`GFramework.Core/Services/Modules/CqrsRuntimeModule.cs`、`GFramework.Tests.Common/CqrsTestRuntime.cs`、`GFramework.Core.Tests/Ioc/MicrosoftDiContainerTests.cs`
- 接受只读 subagent 结论后，将 `LegacyICqrsRuntime` 定位为“容器兼容层”，明确本轮不删除别名、不改 dispatcher 主体、不与旧 `Command` / `Query` API 清理混做
- 主线程已完成：
  - `CqrsRuntimeModule` 把 legacy alias 注册收敛到 `RegisterLegacyRuntimeAlias(...)` helper，并在 XML 文档里明确新旧服务类型解析到同一 runtime 实例
  - `CqrsTestRuntime.RegisterInfrastructure(...)` 现也通过同名 helper 补齐 legacy alias；当容器只预注册正式 `ICqrsRuntime` seam 时，会在幂等接线时回填旧命名空间 alias
  - `MicrosoftDiContainerTests` 新增 `RegisterInfrastructure_Should_Backfill_Legacy_Cqrs_Runtime_Alias_With_The_Same_Instance`，锁定“只存在正式 seam 时也会补旧 alias，且两者仍指向同一实例”的兼容合同
- worker 已完成文档收口：
  - `GFramework.Core.Abstractions/README.md`
  - `docs/zh-CN/abstractions/core-abstractions.md`
  - `docs/zh-CN/core/cqrs.md`
  - 三处文档都已明确：`GFramework.Core.Abstractions.Cqrs.ICqrsRuntime` 只是旧命名空间下保留的 compatibility alias，新代码应依赖 `GFramework.Cqrs.Abstractions.Cqrs.ICqrsRuntime`

### 验证（RP-066）

- `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~MicrosoftDiContainerTests"`
  - 结果：通过，`42/42` passed
- `dotnet build GFramework.Core/GFramework.Core.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`

### 当前下一步（RP-066）

1. 在保持 branch diff 低于阈值的前提下，回到 `dispatch/invoker` 生成前移主线
2. 优先尝试只覆盖 request 路径的 generated invoker/provider 最小切片，避免一次卷入 notification / stream / pipeline executor
3. 下一次 batch 结束后继续复算 branch diff，确认距 `50 files` stop condition 的剩余 headroom

### 阶段：测试命名收口与 ArchitectureContext lazy-resolution 回归（CQRS-REWRITE-RP-065）

- 继续按 `gframework-batch-boot 50` 执行，基线仍为本地现有 `origin/main`
- `22f608eb` 之后复算 branch diff，相对 `origin/main` 已达到 `18 files`，仍明显低于 `50 files` stop condition，因此继续下一批
- 本轮拆成四个互不冲突切片：
  - worker 1：`MediatorAdvancedFeaturesTests.cs`
  - worker 2：`MediatorArchitectureIntegrationTests.cs`
  - worker 3：`MediatorComprehensiveTests.cs`
  - 主线程：`GFramework.Core.Tests/Architectures/ArchitectureContextTests.cs`
- 三个 worker 均只收口单文件命名与注释语义，并把测试文件迁移到 `GFramework.Cqrs.Tests/Cqrs/`
- 主线程新增 `ArchitectureContextTests` 并发 lazy-resolution 回归，锁定：
  - `PublishAsync(...)` 在并发首次访问时只解析一次 `ICqrsRuntime`
  - `CreateStream(...)` 在并发首次访问时只解析一次 `ICqrsRuntime`
- 集成后已确认三份测试文件中不再残留 `GFramework.Cqrs.Tests.Mediator` 命名空间或 `Mediator` 语义命名

### 验证（RP-065）

- `dotnet build GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`
- `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~ArchitectureContextTests"`
  - 结果：通过，`22/22` passed

### 当前下一步（RP-065）

1. 继续 `Phase 8` 主线，回到 `dispatch/invoker` 生成前移或 `LegacyICqrsRuntime` 收口的下一个低风险切片
2. 在下一次 batch 结束后复算 branch diff，确认距 `50 files` stop condition 的剩余 headroom

### 阶段：notification publisher seam 最小落地（CQRS-REWRITE-RP-064）

- 本轮按 `gframework-batch-boot 50` 继续 `cqrs-rewrite`，基线使用本地现有 `origin/main`
- 当前 branch diff 相对 `origin/main` 开始时仅 `3 files / 164 lines`，远低于 `50 files` stop condition，因此继续推进真实代码切片
- 主线程锁定 `notification publisher seam` 为本轮最低风险高收益切片，并保持关键路径在本地实现
- 接受两条只读 subagent 结论：
  - 对照 `ai-libs/Mediator` 后，只吸收 notification publisher 策略接缝，不在本轮引入并行 publisher、异常聚合或公开配置面
  - 现有仓库测试需要锁定的兼容语义是：零处理器静默完成、顺序执行、首错即停、上下文逐次注入
- 已完成实现：
  - `GFramework.Cqrs` 新增 `INotificationPublisher`、`NotificationPublishContext<TNotification>`、
    `DelegatingNotificationPublishContext<TNotification, TState>` 与默认 `SequentialNotificationPublisher`
  - `CqrsDispatcher.PublishAsync(...)` 改为解析 handlers 后构造发布上下文，并委托给 publisher seam 执行
  - `CqrsRuntimeFactory`、`CqrsRuntimeModule` 与 `GFramework.Tests.Common.CqrsTestRuntime` 现会在 runtime 创建前复用容器里已注册的 `INotificationPublisher`
  - `GFramework.Cqrs.Tests` 新增 `CqrsNotificationPublisherTests`，覆盖自定义 publisher、上下文注入、零处理器、首错即停与默认接线复用
  - `GFramework.Cqrs/README.md` 与 `docs/zh-CN/core/cqrs.md` 已同步说明默认通知语义与可替换 seam
- 中途验证曾因并行 .NET 构建产生输出文件锁噪音；已改为串行重跑并获取干净结果

### 验证（RP-064）

- `dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`
- `dotnet build GFramework.Core/GFramework.Core.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`
- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsNotificationPublisherTests"`
  - 结果：通过，`5/5` passed
- `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~MicrosoftDiContainerTests"`
  - 结果：通过，`41/41` passed
- `GIT_DIR=/mnt/f/gewuyou/System/Documents/WorkSpace/GameDev/GFramework/.git/worktrees/GFramework-cqrs GIT_WORK_TREE=/mnt/f/gewuyou/System/Documents/WorkSpace/GameDev/GFramework-WorkTree/GFramework-cqrs bash scripts/validate-csharp-naming.sh`
  - 结果：通过

### 当前下一步（RP-064）

1. 评估 notification publisher seam 的第二阶段是否需要公开配置面、并行 publisher 或 telemetry decorator
2. 把 `dispatch/invoker` 生成前移重新拉回 `Phase 8` 主线，作为下一个实现切片

### 阶段：CQRS vs Mediator 评估归档（CQRS-REWRITE-RP-063）

- 本轮按用户要求使用 `gframework-boot` 启动上下文后，先完成 `cqrs-rewrite` 现状核对，再并行对照
  `GFramework.Cqrs` 与 `ai-libs/Mediator`
- 只读评估结论已归档到 `ai-plan/public/cqrs-rewrite/archive/todos/cqrs-vs-mediator-assessment-rp063.md`
- 本轮关键判断：
  - `GFramework.Cqrs` 已完成对外部 `Mediator` 作为生产 runtime 依赖的替代
  - 当前尚未完成的是仓库内部旧 `Command` / `Query` API、兼容 seam、fallback 旧语义与测试命名的收口
  - 当前已吸收 `Mediator` 的统一消息模型、generator 优先注册与热路径缓存思路
  - 当前仍未完整吸收 publisher 策略抽象、细粒度 pipeline、telemetry / diagnostics / benchmark 体系与 runtime 主体生成
- 本轮把默认下一步从“继续盯 PR thread”调整为“围绕 publisher seam 与 dispatch/invoker 生成前移做下一轮设计收敛”

### 验证（RP-063）

- `dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`

## 活跃事实

- 当前主题仍处于 `Phase 8`
- 当前主题的主问题已从“是否完成外部依赖替代”转为“内部兼容层收口顺序与下一轮能力深化优先级”
- 已完成阶段的详细执行历史不再留在 active trace；默认恢复入口只保留当前恢复点、活跃事实、风险与下一步

## 当前风险

- 当前 `dotnet build GFramework.sln -c Release` 在 WSL 环境仍会受顶层 `GFramework.csproj` 的 Windows NuGet fallback 配置影响
- 若不把“生产替代完成”与“仓库内部收口完成”分开记录，后续很容易重复争论当前 CQRS 迁移是否已经完成

## Archive Context

- 当前评估归档：
  - `ai-plan/public/cqrs-rewrite/archive/todos/cqrs-vs-mediator-assessment-rp063.md`
- 历史 trace 归档：
  - `ai-plan/public/cqrs-rewrite/archive/traces/cqrs-rewrite-history-through-rp043.md`
  - `ai-plan/public/cqrs-rewrite/archive/traces/cqrs-rewrite-history-rp046-through-rp061.md`

## 当前下一步

1. 补一轮最小 Release 构建验证，确认本次 `ai-plan` 与评估文档更新未引入仓库级异常
2. 以 `notification publisher seam` 与 `dispatch/invoker` 生成前移为优先对象，形成下一轮可执行设计
