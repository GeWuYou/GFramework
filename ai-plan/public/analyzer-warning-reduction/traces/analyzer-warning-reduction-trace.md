# Analyzer Warning Reduction 追踪

## 2026-04-21

### 阶段：CQRS `MA0051` 收口（RP-002）

- 依据 active tracking 中“先只选一个结构性切入点”的约束，选定 `GFramework.Cqrs/Internal/CqrsHandlerRegistrar.cs`
  作为低风险下一步，因为它已有稳定的 targeted test 覆盖 generated registry、reflection fallback、缓存和重复注册行为
- 将 `TryRegisterGeneratedHandlers` 拆分为 registry 激活、批量注册和 fallback 结果构建三个辅助阶段，同时把
  `GetReflectionFallbackMetadata` 的直接类型解析与按名称解析拆开，降低长方法复杂度但不改日志文本与回退语义
- 顺手修正 `RegisterAssemblyHandlers` 内部调试日志的缩进，未改注册顺序、生命周期或服务描述符写入逻辑
- 验证通过：
  - `dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:UseSharedCompilation=false -p:RestoreFallbackFolders=`
    - 结果：`0 Warning(s)`，`0 Error(s)`
  - `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter FullyQualifiedName~CqrsHandlerRegistrarTests -p:RestoreFallbackFolders=`
    - 结果：`11 Passed`，`0 Failed`
- 新发现的环境注意事项：
  - 当前 WSL worktree 下若不显式传入 `-p:RestoreFallbackFolders=`，Linux `dotnet` 会读取不存在的 Windows fallback package
    folder 并导致 `ResolvePackageAssets` 失败
  - sandbox 内运行 `dotnet` 会因 MSBuild named-pipe 限制失败；需要在提权上下文中执行 .NET 验证

## 2026-04-19

### 阶段：local-plan 迁移收口（RP-001）

- 复核当前工作树后确认：`local-plan/` 仅保存 analyzer warning reduction 主题的 durable recovery state，不应继续作为
  worktree-root 遗留目录存在
- 按 `ai-plan` 治理规则建立 `ai-plan/public/analyzer-warning-reduction/` 主题目录，并补齐：
  - `todos/`
  - `traces/`
  - `archive/todos/`
  - `archive/traces/`
- 将旧 `local-plan` 中的详细 tracking / trace 迁入主题内历史归档，保留 `RP-001` 的完整实现与验证上下文
- 新建精简版 active tracking / trace 入口，并在 `ai-plan/public/README.md` 中建立
  `fix/analyzer-warning-reduction-batch` -> `analyzer-warning-reduction` 的 worktree 映射
- 删除旧 `local-plan` 文件，避免 `boot` 或后续协作者继续从过时目录恢复
- 验证通过：
  - `find ai-plan/public/analyzer-warning-reduction -maxdepth 3 -type f | sort`
  - `dotnet build GFramework.Core.Abstractions/GFramework.Core.Abstractions.csproj -c Release -p:RestoreFallbackFolders=`

### Archive Context

- 历史跟踪归档：
  - `ai-plan/public/analyzer-warning-reduction/archive/todos/analyzer-warning-reduction-history-rp001.md`
- 历史 trace 归档：
  - `ai-plan/public/analyzer-warning-reduction/archive/traces/analyzer-warning-reduction-history-rp001.md`

### 下一步

1. 若继续 analyzer warning reduction，优先回到 `GFramework.Core` 剩余 `MA0051` 热点，并继续保持“单 warning family、单切入点”的节奏
2. 后续所有 WSL 下的 .NET 定向验证命令继续显式附带 `-p:RestoreFallbackFolders=`，避免把环境问题误判成代码回归
