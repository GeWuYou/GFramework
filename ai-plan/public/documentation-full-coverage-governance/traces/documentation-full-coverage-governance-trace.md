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

### 当前恢复点：RP-002

- 完成 `Core` / `Core.Abstractions` 的类型族级 XML inventory：
  - `GFramework.Core/README.md`
  - `GFramework.Core.Abstractions/README.md`
  - `docs/zh-CN/core/index.md`
  - `docs/zh-CN/abstractions/core-abstractions.md`
- 通过顶层目录轻量盘点确认：
  - `GFramework.Core` 当前各目录族的公开 / 内部类型声明都已带 XML 注释
  - `GFramework.Core.Abstractions` 当前各契约目录族的公开 / 内部类型声明都已带 XML 注释
- 这轮 inventory 明确限定为“类型声明级基线”，不把结果表述成成员级 XML 合规审计

### 当前决策（RP-002）

- XML inventory 同时落在模块 README 和站内 landing page：
  - README 提供仓库侧入口，方便从包目录直接恢复上下文
  - docs landing 提供更细的类型族 / 代表类型 / 阅读重点表格，方便站内导航
- `Core` 波次在补齐基线后转入巡检，不继续在本轮展开成员级 ``<param>`` / ``<returns>`` 审计
- 下一恢复点切换到 `Ecs` 波次，优先处理仍明显失真的 runtime docs

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

### 当前验证（RP-002）

- 文档校验：
  - `validate-all.sh docs/zh-CN/core/index.md`：通过
  - `validate-all.sh docs/zh-CN/abstractions/core-abstractions.md`：通过
- 构建校验：
  - `cd docs && bun run build`：通过；仅保留 VitePress 大 chunk warning，无构建失败

### 下一步

1. 在 `Ecs` 波次重写运行时 docs，并把 `Ecs.Arch.Abstractions` 纳入完整模块闭环
2. 为 `Ecs.Arch` / `Ecs.Arch.Abstractions` 建立与 `Core` 同粒度的 XML inventory 基线
