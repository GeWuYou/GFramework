# CQRS 与 Mediator 评估归档（RP-063）

## 背景

- 本轮目的不是继续实现 runtime / generator，而是基于当前 `feat/cqrs-optimization` 工作树事实，
  评估 `GFramework.Cqrs` 相对 `ai-libs/Mediator` 的替代完成度、设计吸收度与后续可借鉴项。
- 本评估只使用仓库内现有实现、文档、`ai-plan` 记录与只读第三方参考副本 `ai-libs/Mediator`。

## 评估结论

### 1. 当前阶段

- `cqrs-rewrite` 当前处于 `Phase 8`，恢复点提升到 `CQRS-REWRITE-RP-063`。
- 当前主线已从“移除外部依赖并让默认 runtime 可用”转入“继续扩大 generator 覆盖、继续压低 dispatch /
  invoker 反射占比、继续收口 facade 与兼容层”的中后期收敛阶段。
- 这一结论延续了 active tracking 中的 `RP-062` 事实，不代表回退到早期迁移阶段。

### 2. 对外部 Mediator 的替代完成度

- 生产依赖层面：已基本完成替代。
  - `GFramework.Cqrs` 当前不再引用外部 `Mediator` 包。
  - 默认 runtime 已切换为自有 `CqrsDispatcher`、`CqrsHandlerRegistrar` 与 `CqrsRuntimeFactory`。
  - `GFramework.Core` 通过 `CqrsRuntimeModule` 自动接线默认 CQRS runtime。
- 运行时主路径层面：已基本完成替代。
  - `ArchitectureContext` 已提供统一的 `SendRequestAsync`、`PublishAsync`、`CreateStream` 入口。
  - handler 注册链路已以 generated registry 优先、targeted fallback 次之、整程序集扫描兜底为主。
- 结论：
  - 若“完全替代”指“不再依赖外部 `Mediator` 作为生产 runtime”，答案是 `是`。
  - 若“完全替代”指“仓库内部所有旧总线、旧 seam、旧命名与兼容入口都已删除”，答案是 `否`。

### 3. 对 Mediator 设计思想的吸收度

- 已吸收：
  - 统一 request / command / query / notification / stream 消息模型。
  - 源码生成优先、反射 fallback 次之的注册策略。
  - 通过缓存压低 registrar / dispatcher 热路径上的重复反射成本。
  - 将 CQRS runtime 明确接入框架架构上下文，而不是维持独立外部服务入口。
- 部分吸收：
  - `GFramework.Cqrs.SourceGenerators` 已深度参与 handler 注册，但当前仍主要生成 registry 与 fallback 元数据，
    而非像 `Mediator` 那样进一步生成 runtime 主体或更大范围的 DI glue。
- 尚未充分吸收：
  - notification publisher 作为可替换策略的一等抽象。
  - stream pipeline、pre/post processor、exception pipeline 这类更细粒度的扩展分层。
  - telemetry、diagnostics、benchmark、allocation tracking 作为框架主能力的完整体系。
  - 进一步把强类型 dispatch / invoker 主体前移到生成器，而不是继续依赖运行时 `MakeGenericMethod` +
    `Delegate.CreateDelegate` 后再缓存。

### 4. 仓库内部仍未完全收口的部分

- 旧 `GFramework.Core.Command` / `GFramework.Core.Query` 路径仍然存在。
- `ArchitectureContext` 仍同时暴露旧 Command / Query 路径与新 CQRS 路径。
- `CqrsRuntimeModule` 仍注册 `LegacyICqrsRuntime = GFramework.Core.Abstractions.Cqrs.ICqrsRuntime` 兼容别名。
- `CqrsReflectionFallbackAttribute` 仍保留空 marker 与字符串 fallback 语义，说明 runtime 继续承担旧版兼容契约。
- `GFramework.Cqrs.Tests/Mediator/` 目录与若干测试类仍沿用 `Mediator` 命名，但测试内部实际已调用当前 CQRS runtime。
- 结论：当前真正残留的主要是兼容层、旧术语与评估/测试命名，而不是外部运行时依赖本身。

## 关键证据

- 当前阶段与主线：
  - `ai-plan/public/cqrs-rewrite/todos/cqrs-rewrite-migration-tracking.md`
  - `ai-plan/public/cqrs-rewrite/traces/cqrs-rewrite-migration-trace.md`
- 自有 runtime 与默认接线：
  - `GFramework.Cqrs/CqrsRuntimeFactory.cs`
  - `GFramework.Cqrs/Internal/CqrsDispatcher.cs`
  - `GFramework.Cqrs/Internal/CqrsHandlerRegistrar.cs`
  - `GFramework.Core/Services/Modules/CqrsRuntimeModule.cs`
  - `GFramework.Core/Architectures/ArchitectureContext.cs`
- generator 与 fallback 合同：
  - `GFramework.Cqrs.SourceGenerators/README.md`
  - `GFramework.Cqrs/CqrsReflectionFallbackAttribute.cs`
  - `docs/zh-CN/core/cqrs.md`
- 第三方参考源：
  - `ai-libs/Mediator/README.md`
  - `ai-libs/Mediator/src/Mediator.SourceGenerator/**`
  - `ai-libs/Mediator/src/Mediator/INotificationPublisher.cs`
  - `ai-libs/Mediator/benchmarks/README.md`

## 建议优先级

### P1

- 评估是否继续前移 generator 职责，生成部分强类型 dispatch / invoker 主体，而不再只停留在 registry。
- 为 notification fan-out 引入可替换 publisher seam，并先定义顺序与并发两种语义模型。

### P2

- 扩展 pipeline 体系，评估 stream pipeline、pre-processor、post-processor、exception handler 的契约边界。
- 为 CQRS runtime 设计 tracing / metrics seam，至少先完成 contracts 与默认 no-op / logger 对齐方案评估。

### P3

- 建立 CQRS runtime 的 benchmark / allocation 基线，让“继续压低反射成本”从经验判断转为可量化验证。
- 单独规划旧 `Command` / `Query`、`LegacyICqrsRuntime` 与测试命名的收口顺序，不与 runtime 微优化混在同一波。

## 默认下一步

1. 以 `notification publisher seam` 与 `dispatch/invoker 生成前移` 为首轮设计评估对象。
2. 若进入实现阶段，优先做 seam 与契约扩展，再决定是否调整旧测试目录与历史命名。
