# Documentation Full Coverage Governance Status History Through RP-016

以下内容从 active tracking 中迁出，用于保留 `DOCUMENTATION-FULL-COVERAGE-GOV-RP-001` 到
`DOCUMENTATION-FULL-COVERAGE-GOV-RP-016` 的阶段性状态、治理结论与恢复背景。默认 `boot` 只需要读取 active
tracking 中的最新摘要；若需要追溯已完成波次的详细背景，再回到本归档文件。

## 阶段里程碑

### `RP-001` 到 `RP-007`

- 建立长期 active topic `documentation-full-coverage-governance`，并在 `ai-plan/public/README.md` 中将当前分支
  `docs/sdk-update-documentation` 映射到该 topic。
- 明确消费属性边界：
  - `GFramework.Ecs.Arch.Abstractions` 是可打包直接消费模块，需要 README 与文档入口。
  - `GFramework.Core.SourceGenerators.Abstractions`、
    `GFramework.Godot.SourceGenerators.Abstractions`、`GFramework.SourceGenerators.Common`
    都按 `IsPackable=false` 的内部支撑模块处理。
- 收口 `Core` / `Core.Abstractions` README、landing page 与类型族级 XML inventory。
- 收口 `Ecs.Arch` / `Ecs.Arch.Abstractions` README、landing page、抽象页与 `UseArch(...)` 早于
  `Initialize()` 的采用约束。
- 收口 `Cqrs` family 的 runtime / abstractions / source generator 入口，并为缺失的内部类型补齐 XML 注释。
- 收口 `Game` family 的 README、landing page、抽象页与类型族级 XML inventory。
- 将 `Game` family 从“文档存在但入口失真”推进到“runtime / abstractions / source generator 都有当前可审计入口”。

### `RP-008` 到 `RP-013`

- 消化 PR #271 的文档 follow-up，修正 `gframework-pr-review` 脚本与 skill 中的 WSL Git 策略，使其与
  `AGENTS.md` 保持一致。
- 将 `Godot` family 的核心恢复摘要迁回 active topic，避免默认恢复路径继续依赖 archive 细节。
- 重写 `GFramework.Godot/README.md` 与 `GFramework.Godot.SourceGenerators/README.md`，补齐当前包关系、
  子系统地图、最小接入路径与站内文档入口。
- 更新根 `README.md`、`docs/zh-CN/source-generators/index.md`、`docs/zh-CN/api-reference/index.md`，把
  `GFramework.Godot.SourceGenerators` 的 owner 与能力边界收敛到当前源码口径。
- 完成 `Godot` docs surface 的 validation-only 巡检，确认 landing、tutorial、API reference 与 README
  当前保持一致叙述，没有出现新的入口漂移。

### `RP-014` 到 `RP-016`

- 重写 `docs/zh-CN/godot/storage.md`，补齐 frontmatter、`GodotFileStorage` 的路径语义、repository 分工与
  `GodotYamlConfigLoader` 分流边界。
- 重写 `docs/zh-CN/godot/setting.md`，改回当前 `ISettingsModel` / `RegisterApplicator(...)` 口径，并补齐
  `LocalizationMap` fallback 与当前 consumer wiring。
- 对 `Godot` docs surface 再做一轮 validation-only 巡检，确认 `storage.md`、`setting.md`、landing、README、
  tutorial 与 API reference 没有新的回漂。
- 重写 `docs/zh-CN/game/data.md`、`storage.md`、`serialization.md`、`setting.md`，把 `Game` persistence docs
  surface 收口到当前 repository / storage / serializer / settings 责任边界。
- 将 `DataRepository`、`UnifiedSettingsDataRepository`、`SaveRepository<TSaveData>` 与 `FileStorage` /
  `ScopedStorage` / `SettingsModel<TRepository>` 的分工，统一回写到 README、源码与 `PersistenceTests` 已验证的采用路径。

## 已确认的长期事实

- 已归档的 `documentation-governance-and-refresh` 只作为历史证据保留，不再作为默认 `boot` 入口。
- `Godot` family 当前核心页面集包括：
  - `docs/zh-CN/godot/index.md`
  - `docs/zh-CN/godot/architecture.md`
  - `docs/zh-CN/godot/scene.md`
  - `docs/zh-CN/godot/ui.md`
  - `docs/zh-CN/godot/storage.md`
  - `docs/zh-CN/godot/setting.md`
  - `docs/zh-CN/godot/signal.md`
  - `docs/zh-CN/godot/extensions.md`
  - `docs/zh-CN/godot/logging.md`
  - `docs/zh-CN/tutorials/godot-integration.md`
- `Game` persistence docs surface 当前最值得优先复核的页面集包括：
  - `docs/zh-CN/game/data.md`
  - `docs/zh-CN/game/storage.md`
  - `docs/zh-CN/game/serialization.md`
  - `docs/zh-CN/game/setting.md`
- `GFramework.Cqrs` 在当前 WSL / dotnet 环境下仍会读取失效的 fallback package folder，并在标准 build 中触发
  `MSB4276` / `MSB4018`；这是已知环境阻塞，不是本 topic 当前文档回归。
- 当前 WSL 会话里 `git.exe` 可解析但不可执行，仓库默认 Git 策略应继续优先使用显式
  `--git-dir` / `--work-tree` 绑定。

## 关联归档

- 早期详细验证历史：`ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-validation-history-through-rp-007.md`
- 时间线归档：`ai-plan/public/documentation-full-coverage-governance/archive/traces/documentation-full-coverage-governance-trace-history-through-rp-016.md`
