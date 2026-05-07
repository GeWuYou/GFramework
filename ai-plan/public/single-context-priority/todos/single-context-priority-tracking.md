# Single Context Priority 跟踪

## 目标

围绕 `GameContext` 与 `MicrosoftDiContainer` 收敛当前运行时语义：

- 把 `GameContext` 从弱约束的多上下文字典收敛为“单活动上下文 + 兼容别名查找”模型
- 保留按架构类型查找的兼容入口，同时禁止在同一全局上下文表中并存多个不同的架构上下文实例
- 统一 `MicrosoftDiContainer` 预冻结阶段的单实例读取路径，减少 `RegisterPlurality` / CQRS 基础设施别名注册下的查询歧义

## 当前恢复点

- 恢复点编号：`SINGLE-CONTEXT-PRIORITY-RP-001`
- 当前阶段：`Phase 2`
- 当前结论：
  - `GameContext` 已从“字典首个枚举值即默认上下文”收敛为“单活动上下文 + 类型别名兼容查找”；同一全局上下文表不再允许并存两个不同上下文实例
  - `MicrosoftDiContainer` 的预冻结 `Get<T>()` / `Get(Type)` 已改为复用实例可见性收集逻辑，和 `GetAll*` 的实例暴露规则保持一致
  - `IIocContainer` XML 文档已明确预冻结查询与 `Contains<T>()` 的契约边界，避免把注册阶段查询误读为完整 DI 激活语义
  - 当前分支从 `main` 创建，已完成 `git pull --ff-only origin main`

## 当前活跃事实

- 当前分支：`refactor/single-context-priority`
- 当前预期改动面：
  - `GFramework.Core/Architectures/GameContext.cs`
  - `GFramework.Core/Rule/ContextAwareBase.cs`
  - `GFramework.Core/Ioc/MicrosoftDiContainer.cs`
  - `GFramework.Core.Abstractions/Ioc/IIocContainer.cs`
  - 相关 `GFramework.Core.Tests` 与必要文档页

## 当前风险

- `GameContext` 是公开静态入口，任何“允许多个不同上下文并存”的现有测试都需要按单活动上下文语义重写
- `Contains<T>()` 在预冻结阶段目前更接近“是否存在注册”，不等同于“是否能立即解析实例”；本轮若不改其行为，需要在文档和测试中明确这一点
- `ResolveCqrsRegistrationService()` 仍要求注册阶段对 `ICqrsRegistrationService` 可见的是实例绑定；若后续改成工厂或实现类型注册，需要额外设计注册阶段激活 helper

## 最近权威验证

- `git pull --ff-only origin main`
  - 结果：通过，当前主分支已同步
- `rg -n "GetFirstArchitectureContext|GameContext|RegisterPlurality|ResolveCqrsRegistrationService|GetAll\\(" ...`
  - 结果：已确认 `GameContext` 的默认回退集中在 `ContextAwareBase` / `GameContextProvider`，且 `MicrosoftDiContainer` 的预冻结查询实现存在分叉
- `python3 scripts/license-header.py --check`
  - 结果：通过
- `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~GameContextTests|FullyQualifiedName~ContextProviderTests|FullyQualifiedName~ContextAwareTests|FullyQualifiedName~MicrosoftDiContainerTests|FullyQualifiedName~IocContainerLifetimeTests|FullyQualifiedName~ArchitectureInitializationPipelineTests"`
  - 结果：通过，`92/92` passed
- `dotnet build GFramework.Core/GFramework.Core.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`

## 下一推荐步骤

1. 若后续继续推进，可评估 `Architecture` 销毁阶段是否要显式解绑 `GameContext` 当前活动上下文
2. 若 CQRS runtime seam 计划改成工厂式注册，再单独补“注册阶段激活 helper”的设计与测试
