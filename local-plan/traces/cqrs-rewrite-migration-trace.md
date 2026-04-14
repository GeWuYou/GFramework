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

- `dotnet build /mnt/f/gewuyou/System/Documents/WorkSpace/GameDev/GFramework/GFramework.Core/GFramework.Core.csproj`
  - 结果：通过
- `dotnet build /mnt/f/gewuyou/System/Documents/WorkSpace/GameDev/GFramework/GFramework.Core.Tests/GFramework.Core.Tests.csproj`
  - 结果：通过
- `dotnet test /mnt/f/gewuyou/System/Documents/WorkSpace/GameDev/GFramework/GFramework.Core.Tests/GFramework.Core.Tests.csproj --no-build`
  - 结果：通过
  - 明细：`1621` 个测试全部通过
- `dotnet build /mnt/f/gewuyou/System/Documents/WorkSpace/GameDev/GFramework/GFramework.sln`
  - 结果：通过
- `dotnet build /mnt/f/gewuyou/System/Documents/WorkSpace/GameDev/CoreGrid-Migration/CoreGrid.sln`
  - 结果：通过
  - 备注：仅存在既有 analyzer warnings，无新增构建错误

### 当前残留

- 文档与少量历史 API 命名仍保留 `Mediator` 前缀
- `MediatorCoroutineExtensions` 与少量测试仍依赖 `Mediator.Abstractions`
- handler 自动注册当前使用运行时反射扫描，尚未切回生成器注册

### 下一步建议

1. 决定是否继续做“完全移除 `Mediator.Abstractions` 包”的第二阶段清理
2. 若继续，优先迁移协程扩展与相关测试
3. 评估是否将 `RegisterMediatorBehavior`、`ContextAwareMediator*Extensions` 等历史命名升级为 CQRS 中性命名
