# Documentation Full Coverage Governance Trace

## 2026-04-22

### 当前恢复点：RP-001

- 按长期治理计划新建 active topic `documentation-full-coverage-governance`
- 在 `ai-plan/public/README.md` 中将当前分支 `docs/sdk-update-documentation` 映射到该 topic
- 复核已知缺口模块的 `*.csproj` 后确认：
  - `GFramework.Ecs.Arch.Abstractions` 是可打包消费模块，需要独立 README
  - `GFramework.Core.SourceGenerators.Abstractions`、`GFramework.Godot.SourceGenerators.Abstractions`、
    `GFramework.SourceGenerators.Common` 都是 `IsPackable=false` 的内部支撑模块
- 基于该结论，本轮没有为内部支撑模块新增独立 README，而是在根 README 与 abstractions / API 入口中明确其 owner

### 当前决策

- 新主题的完成条件采用长期治理口径：`P0` 清零、无 README 缺失、无导航死链，并完成连续两轮稳定巡检
- 本轮先做治理基础设施与 inventory，不把整个长期计划伪装成单轮完成
- `api-reference` 页面改为“模块 -> README / docs / XML / tutorial”的阅读链路入口，避免继续维护失真的伪签名列表
- `Ecs.Arch` family 被列为高优先 backlog：抽象层入口已补齐，但 runtime docs 仍需按源码重写
- `Core` / `Core.Abstractions` 波次先收口 README、landing page 和 abstractions 页的目录映射，再补显式 XML 覆盖 inventory
- VitePress 站内页面不直接链接仓库根模块 `README.md`；站内仅保留可构建的 docs 链接，模块 README 以文本路径或仓库 README 承接

### 当前验证

- 文档校验：
  - `validate-all.sh docs/zh-CN/abstractions/index.md`：通过
  - `validate-all.sh docs/zh-CN/abstractions/ecs-arch-abstractions.md`：通过
  - `validate-all.sh docs/zh-CN/api-reference/index.md`：通过
  - `validate-all.sh docs/zh-CN/core/index.md`：通过
  - `validate-all.sh docs/zh-CN/abstractions/core-abstractions.md`：通过
- 构建校验：
  - `cd docs && bun run build`：通过
  - `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build GFramework.Core.Abstractions/GFramework.Core.Abstractions.csproj -c Release -p:RestoreFallbackFolders=`：通过，`0 Warning(s) / 0 Error(s)`
  - `dotnet build GFramework.Ecs.Arch.Abstractions/GFramework.Ecs.Arch.Abstractions.csproj -c Release -p:RestoreFallbackFolders=`：通过，`0 Warning(s) / 0 Error(s)`

### 下一步

1. 继续 `Core` / `Core.Abstractions` 波次，按类型族建立 XML 文档覆盖 inventory
2. 在 `Ecs` 波次重写运行时 docs，并把 `Ecs.Arch.Abstractions` 纳入完整模块闭环
