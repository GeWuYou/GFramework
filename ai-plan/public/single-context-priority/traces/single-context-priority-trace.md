# Single Context Priority 追踪

## 2026-05-07

### 阶段：启动并收敛实现方向（SINGLE-CONTEXT-PRIORITY-RP-001）

- 依据用户补充的运行时心智模型，确认当前 `Architecture` 更接近框架实例，`IArchitectureContext` 更接近功能入口，而不是需要并存运行的独立宿主
- 启动阶段已按仓库规则读取 `AGENTS.md`、`.ai/environment/tools.ai.yaml`、`ai-plan/public/README.md`，并在 `main` 上执行 `git pull --ff-only origin main` 后创建 `refactor/single-context-priority`
- 本轮实现决策：
  - `GameContext` 维持兼容 API，但内部收敛为单活动上下文模型；允许多个类型键指向同一上下文实例，不允许并存多个不同上下文实例
  - `MicrosoftDiContainer` 先做低风险修复：统一预冻结 `Get<T>()` / `Get(Type)` 的实例可见性规则，并把 CQRS 注册服务解析改为复用同一条实例收集路径
  - 若 `Contains<T>()` 的预冻结语义仍保持“是否已有注册”，则通过 XML 文档和测试显式记录，而不是隐含为“可立即解析”
- 本轮委托记录：
  - explorer `Noether`：梳理 `GameContext` 单活动上下文收敛的兼容风险、测试缺口和必须保留的 API 面
  - explorer `Boole`：梳理 `MicrosoftDiContainer` 预冻结查询、`Contains<T>()`、CQRS 注册依赖点的语义分叉

### 阶段：实现与验证完成（SINGLE-CONTEXT-PRIORITY-RP-001）

- 实现摘要：
  - `GameContext` 新增单活动上下文约束，`GetFirstArchitectureContext()` 改为显式返回当前活动上下文，不再依赖并发字典枚举顺序
  - `GameContext.GetByType(Type)`、`Get<T>()`、`TryGet<T>()` 增加对当前活动上下文的兼容匹配，保留按架构类型别名回查能力
  - `MicrosoftDiContainer.Get<T>()` / `Get(Type)` 的预冻结实例查询改为复用 `CollectRegisteredImplementationInstances(...)`
  - `ResolveCqrsRegistrationService()` 改为复用同一条注册阶段实例可见性路径，并把失败信息收敛为更明确的契约提示
  - `IIocContainer` XML 文档与 `docs/zh-CN/core/context.md`、`docs/zh-CN/core/rule.md`、`docs/zh-CN/source-generators/context-aware-generator.md` 已同步到“当前活动上下文 / 预冻结查询”语义
- 测试与验证：
  - `python3 scripts/license-header.py --check` 通过
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~GameContextTests|FullyQualifiedName~ContextProviderTests|FullyQualifiedName~ContextAwareTests|FullyQualifiedName~MicrosoftDiContainerTests|FullyQualifiedName~IocContainerLifetimeTests|FullyQualifiedName~ArchitectureInitializationPipelineTests"` 通过，`92/92` passed
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release` 通过，`0 warning / 0 error`

### 当前下一步

1. 若后续要进一步彻底移除全局回退，可单独评估 `Architecture` 销毁解绑与 `GameContext` 公开别名字典的收口策略
