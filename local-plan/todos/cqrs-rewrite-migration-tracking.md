# CQRS 重写迁移跟踪

## 目标

围绕 `GFramework` 当前的双轨 CQRS 现状，完成一轮以“去 Mediator 外部依赖”为目标的架构迁移：

- 将 `Mediator` 从 GFramework 公共 API 和运行时主路径中移除
- 基于 GFramework 自有抽象重建正式 CQRS runtime、行为管道和注册机制
- 保留 `EventBus` 作为框架级事件系统，不与 CQRS notification 混同
- 让 `CoreGrid-Migration` 直连本地 `GFramework`，作为真实迁移验证工程
- 为复杂迁移建立明确恢复点与进度追踪，避免上下文过长或中断后失去状态

## 当前恢复点

- 恢复点编号：`CQRS-REWRITE-RP-002`
- 当前阶段：`Phase 4`
- 当前焦点：
  - 清理剩余 `Mediator` 包依赖与文档残留
  - 评估是否继续把协程扩展和测试项目中的 `Mediator.Abstractions` 完全移除
  - 规划第二阶段优化：代码生成注册、性能收敛、行为 API 命名统一

## 本轮计划

### Phase 0：工作流基础

- [x] 在 `local-plan/todos/` 建立本任务跟踪文档
- [x] 在 `local-plan/traces/` 建立本任务追踪文档
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

- [ ] 移除 `Mediator` 包依赖与相关测试/文档残留
- [x] 运行目标构建与测试
- [x] 记录剩余风险与下一恢复点

## 当前完成结果

- `CoreGrid-Migration` 已直连本地 `GFramework` 源码与本地 source generators。
- `GameArchitecture` 已不再依赖 `collection.AddMediator(...)` 即可使用 CQRS。
- `GFramework.Core.Abstractions` 新增自有 CQRS 契约：
  - `IRequest<TResponse>` / `INotification` / `IStreamRequest<TResponse>`
  - `IRequestHandler<,>` / `INotificationHandler<>` / `IStreamRequestHandler<,>`
  - `Unit`
  - `IPipelineBehavior<,>` / `MessageHandlerDelegate<,>`
- `ArchitectureBootstrapper` 会在初始化阶段自动扫描并注册当前架构程序集与 `GFramework.Core` 程序集中的 CQRS handlers。
- `CqrsDispatcher` 已支持：
  - request dispatch
  - notification publish
  - stream dispatch
  - context-aware handler 注入
  - request pipeline behavior 链式执行
- `GFramework.Core.Tests` 中原依赖 `Mediator` 注册路径的测试已切换到框架内建 CQRS 注册路径。
- 当前验证状态：
  - `dotnet build GFramework/GFramework.sln` 通过
  - `dotnet test GFramework/GFramework.Core.Tests/GFramework.Core.Tests.csproj --no-build` 通过，`1621` 个测试全部通过
  - `dotnet build CoreGrid-Migration/CoreGrid.sln` 通过

## 当前已知事实

- `GFramework` 当前仍同时维护：
  - 基于 `CommandExecutor` / `QueryExecutor` / `EventBus` 的轻量旧 CQRS
  - 基于 GFramework 自有抽象的新 CQRS runtime
- 仍存在 `Mediator` 残留的区域主要集中在：
  - 文档中的历史说明
  - `MediatorCoroutineExtensions` 及对应测试
  - 测试项目对 `Mediator.Abstractions` 的少量残余依赖
- `CoreGrid-Migration` 已切到本地源码引用，并在当前恢复点完成构建验证

## 当前风险

- `GFramework` 仓库存在与本任务无关的既有改动，提交时必须避免覆盖
- `CoreGrid-Migration` 是 worktree，WSL 下原生 `git` 解析该 worktree 路径有兼容问题
- 当前 `RegisterMediatorBehavior` 命名仍保留历史前缀，但底层已切换为框架自有 CQRS pipeline；若后续要彻底脱媒介命名，需要一次 API 命名迁移
- 当前 handler 自动注册基于运行时反射扫描；若后续追求冷启动与 AOT 友好性，需要补 source-generator 注册路径

## 下次恢复建议

若本轮中断，优先从以下顺序恢复：

1. 查看 `local-plan/traces/cqrs-rewrite-migration-trace.md`
2. 确认当前恢复点 `CQRS-REWRITE-RP-002` 已对应到最新提交
3. 优先决定是否继续移除 `Mediator.Abstractions` 包与 `MediatorCoroutineExtensions` 历史兼容层
4. 若继续演进，再处理 CQRS 注册的生成器化与 API 命名统一

## 备注

- 本文档是当前任务的主恢复点，后续每个关键阶段完成后都要更新
- 发生方向调整时，不覆盖旧结论，直接追加阶段记录与新的恢复点编号
