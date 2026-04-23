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

### 当前恢复点：RP-003

- 完成 `Ecs.Arch` 波次的运行时文档刷新：
  - `docs/zh-CN/ecs/index.md`
  - `docs/zh-CN/ecs/arch.md`
  - `GFramework.Ecs.Arch/README.md`
- 为 `Ecs.Arch.Abstractions` 补齐与运行时页同粒度的 XML inventory：
  - `GFramework.Ecs.Arch.Abstractions/README.md`
  - `docs/zh-CN/abstractions/ecs-arch-abstractions.md`
- 明确记录一个关键采用事实：
  - `UseArch(...)` 必须早于 `Initialize()` 调用
  - 该结论以 `ArchExtensions` 的模块注册方式和 `ExplicitRegistrationTests` 为证据
- 将 `Ecs.Arch` family 从“入口存在但失真”推进到“README / landing / abstractions / XML inventory 已对齐源码与测试”

### 当前决策（RP-003）

- `Ecs` 波次继续采用与 `Core` 相同的治理粒度：
  - 模块 README 承担仓库入口
  - `docs/zh-CN/ecs/index.md` 承担模块族 landing
  - `docs/zh-CN/ecs/arch.md` 承担运行时默认实现专题页
  - `docs/zh-CN/abstractions/ecs-arch-abstractions.md` 承担契约边界专题页
- `EnableStatistics` 当前仅保留在公开配置面上；文档不再把它写成已验证的运行时行为
- 下一恢复点切换到 `Cqrs` 波次，优先解决入口分散和 API / XML 阅读链路不统一的问题

### 当前验证（RP-003）

- 文档校验：
  - `validate-all.sh docs/zh-CN/ecs/index.md`：通过
  - `validate-all.sh docs/zh-CN/ecs/arch.md`：通过
  - `validate-all.sh docs/zh-CN/abstractions/ecs-arch-abstractions.md`：通过
- 构建校验：
  - `cd docs && bun run build`：通过；仅保留 VitePress 大 chunk warning，无构建失败

### 下一步

1. 在 `Cqrs` 波次核对模块 README、`docs/zh-CN/core/cqrs.md` 与 `docs/zh-CN/source-generators/**` 的真实 owner
2. 决定 `Cqrs` family 是补 dedicated landing 还是拆分现有入口页

### 当前恢复点：RP-004

- 完成 `Cqrs` 波次的模块族入口刷新：
  - 重写 `docs/zh-CN/core/cqrs.md`
  - 新建 `docs/zh-CN/source-generators/cqrs-handler-registry-generator.md`
  - 更新 `docs/zh-CN/source-generators/index.md`
  - 更新 `docs/zh-CN/api-reference/index.md`
  - 更新 `docs/.vitepress/config.mts`
- 将 `Cqrs` family 从“README 已存在但 generator 入口分散”推进到“runtime / abstractions / source generator 都有明确站内入口”
- 为 `GFramework.Cqrs/Internal/CqrsHandlerRegistrar.cs` 与
  `GFramework.Cqrs.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 中缺失的内部类型补齐 XML 注释
- 基于轻量扫描确认：
  - `GFramework.Cqrs.Abstractions/Cqrs/` 当前类型声明级 XML 覆盖为 `20/20`
  - `GFramework.Cqrs` 根入口与 `Internal/` 已补到 `19/19`
  - `GFramework.Cqrs.SourceGenerators/Cqrs/` 当前类型声明级 XML 覆盖为 `3/3`

### 当前决策（RP-004）

- `docs/zh-CN/core/cqrs.md` 继续保留在 `Core` 栏目，但其角色调整为 `Cqrs` family landing，而不再只是 runtime 简介页
- `Cqrs.SourceGenerators` 不单独新建一级导航栏目，而是在 `source-generators` 栏目内补一个专用专题页，保持站点 taxonomy 稳定
- generator 入口以“专题页 + API reference 链接 + sidebar”三点联动，而不是只在 `source-generators/index.md` 留一个段落链接
- XML inventory 仍维持“类型声明级基线”口径，不在本轮扩展成成员级 `param/returns/exception` 细审

### 当前验证（RP-004）

- 文档校验：
  - `validate-all.sh docs/zh-CN/core/cqrs.md`：通过
  - `validate-all.sh docs/zh-CN/source-generators/cqrs-handler-registry-generator.md`：通过
- 轻量 XML inventory：
  - `GFramework.Cqrs/Internal/`：`14/14`
  - `GFramework.Cqrs.Abstractions/Cqrs/`：`20/20`
  - `GFramework.Cqrs.SourceGenerators/Cqrs/`：`3/3`
- 构建校验：
  - `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release -p:RestoreFallbackFolders=`：通过
  - `cd docs && bun run build`：通过；仅保留 VitePress 大 chunk warning，无构建失败
  - `dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release`：失败；当前 WSL / dotnet 环境仍引用失效的 Windows fallback package folder，并在多目标 inner build 阶段触发 `MSB4276` / `MSB4018`

### 下一步

1. 切换到 `Game` family 波次，按 `Core` / `Ecs` / `Cqrs` 已验证模板继续补 XML inventory 与教程链路
2. 把 `GFramework.Cqrs` 的本地构建阻塞留给后续环境治理或构建脚本清理，不在本 topic 内扩张为环境修复任务

### 当前恢复点：RP-005

- 完成 `Game` 波次的模块族入口刷新：
  - 更新 `GFramework.Game/README.md`
  - 更新 `GFramework.Game.Abstractions/README.md`
  - 更新 `GFramework.Game.SourceGenerators/README.md`
  - 更新 `docs/zh-CN/game/index.md`
  - 重写 `docs/zh-CN/abstractions/game-abstractions.md`
- 将 `Game` family 从“README / 页面存在但缺少可审计 XML 入口，且 abstractions 页失真”推进到“runtime / abstractions / source generator 都有声明级 XML inventory 与真实采用边界”
- 基于轻量扫描确认：
  - `GFramework.Game` 当前类型声明级 XML 覆盖为 `56/56`
  - `GFramework.Game.Abstractions` 当前类型声明级 XML 覆盖为 `80/80`
  - `GFramework.Game.SourceGenerators` 当前类型声明级 XML 覆盖为 `2/2`

### 当前决策（RP-005）

- `docs/zh-CN/abstractions/game-abstractions.md` 不再维护虚构接口摘录，而是与源码中的 `Config` / `Data` / `Setting` / `Scene` / `UI` / `Routing` 契约分组保持一致
- `Game.SourceGenerators` 继续以 `README + docs/zh-CN/game/config-system.md + docs/zh-CN/source-generators/index.md` 组成入口，不额外新增只为凑数量的专题页
- `docs/zh-CN/game/index.md` 补 frontmatter，并承担 `Game` family 的 XML 基线入口；更细的类型族说明继续留在模块 README 与 abstractions 页

### 当前验证（RP-005）

- 文档校验：
  - `validate-all.sh docs/zh-CN/abstractions/game-abstractions.md`：通过
  - `validate-all.sh docs/zh-CN/game/index.md`：通过
- 轻量 XML inventory：
  - `GFramework.Game`：`56/56`
  - `GFramework.Game.Abstractions`：`80/80`
  - `GFramework.Game.SourceGenerators`：`2/2`
- 构建校验：
  - `cd docs && bun run build`：通过；仅保留 VitePress 大 chunk warning，无构建失败

### 下一步

1. 进入 `Game` family 巡检，优先检查 `config-system.md`、`scene.md`、`ui.md` 与 `source-generators/index.md` 的交叉引用是否回漂
2. 评估是否需要把 `Godot` family 的关键 XML inventory 摘要迁回 active topic，减少对 archive 的依赖

### 当前恢复点：RP-006

- 更新 `AGENTS.md` 的 WSL Git 规则：
  - 将显式 `git --git-dir=<...> --work-tree=<...>` 绑定提升为高于 `git.exe` 的默认优先级
  - 明确 plain Linux `git` 命中 worktree 路径翻译错误时，应先切到显式绑定而不是直接改用 `git.exe`
  - 明确 `git.exe` 只有在当前会话可执行时才作为次级 fallback
- 记录本次恢复任务的环境偏差：
  - `git.exe` 在当前 WSL 会话中可解析，但执行会触发 `Exec format error`
  - plain `git` 会把 worktree 元数据路径翻译错并报“not a git repository”
  - 显式 `--git-dir` / `--work-tree` 绑定是本次已验证可用的 Git 操作方式

### 当前决策（RP-006）

- 把 Git 回退顺序写进 `AGENTS.md`，而不是只留在一次性的聊天上下文里
- 不额外扩张 `gframework-boot` skill，因为它本身不内嵌 Git 选择逻辑，继续由 `AGENTS.md` 作为唯一准则
- 继续把 `git.exe` 保留为 fallback，而不是完全删除，避免在可执行的 WSL 会话里丢掉可用路径

### 当前验证（RP-006）

- 构建校验：
  - `cd docs && bun run build`：通过；仅保留 VitePress 大 chunk warning，无构建失败

### 下一步

1. 继续 `Game` family 巡检，优先检查 `config-system.md`、`scene.md`、`ui.md` 与 `source-generators/index.md` 的交叉引用是否回漂
2. 评估是否需要把 `Godot` family 的关键 XML inventory 摘要迁回 active topic，减少对 archive 的依赖

### 当前恢复点：RP-007

- 完成 `Game` family 巡检：
  - 复核 `docs/zh-CN/game/config-system.md`
  - 复核 `docs/zh-CN/game/scene.md`
  - 复核 `docs/zh-CN/game/ui.md`
  - 复核 `docs/zh-CN/source-generators/index.md`
- 对照 `GFramework.Game`、`GFramework.Game.Abstractions`、`GFramework.Game.SourceGenerators` README 与相关源码 / 测试后，未发现需要立刻修正的采用语义回漂
- 重点确认的真实语义包括：
  - `GameConfigBootstrap` / `RegisterAllGeneratedConfigTables(...)` / `GFrameworkConfigSchemaDirectory` 的配置入口仍与文档示例一致
  - `SceneRouterBase` 仍通过 `SemaphoreSlim` 串行化切换，并拒绝重复 `sceneKey` 入栈
  - `UiRouterBase` 仍将 `Page` 层与 `Overlay` / `Modal` / `Toast` / `Topmost` 分为两套入口，且 `Show(..., UiLayer.Page)` 会直接拒绝

### 当前决策（RP-007）

- 本轮不为“巡检通过”硬造文档改动，先把结论写回 active topic，保持恢复点准确
- `Game` family 暂时转入稳定巡检，不在没有源码变化的情况下重复改写 landing page
- 默认下一步切到 `Godot` family 摘要是否回迁，减少长期治理对 archive topic 的依赖

### 当前验证（RP-007）

- 构建校验：
  - `cd docs && bun run build`：通过；仅保留 VitePress 大 chunk warning，无构建失败

### 下一步

1. 评估是否需要把 `Godot` family 的关键 XML inventory 摘要迁回 active topic
2. 若不需要迁回，则继续抽查 README / landing page / API reference 之间的 cross-link 是否出现新的漂移

### 当前恢复点：RP-008

- 使用 `$gframework-pr-review` 抓取当前分支 PR `#271` 后，确认 latest head review threads 仍有 `4` 条 open：
  - `docs/zh-CN/source-generators/cqrs-handler-registry-generator.md` 的 marker 类型约定说明缺口
  - `docs/zh-CN/ecs/index.md` 的边界说明语序问题
  - `docs/zh-CN/abstractions/ecs-arch-abstractions.md` 误放的 source-generator 内部模块提醒
  - `ai-plan/public/documentation-full-coverage-governance/todos/documentation-full-coverage-governance-tracking.md` 的验证历史过长，以及
    `ai-plan/public/archive/documentation-governance-and-refresh/traces/documentation-governance-and-refresh-trace.md` 缺少显式结果态
- 在当前 WSL 会话里，`gframework-pr-review` 脚本先命中了 `git.exe` 的 `Exec format error`
- 已将 `.agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py` 改为优先使用 Linux `git` 的显式
  `--git-dir` / `--work-tree` 绑定，并仅在无法建立该绑定时回退到旧的可执行解析逻辑
- 已同步更新 `.agents/skills/gframework-pr-review/SKILL.md`，使其 Git 策略与命令示例都与当前仓库状态一致
- 已把 `DOCUMENTATION-FULL-COVERAGE-GOV-RP-001` 到 `RP-007` 的详细验证历史迁入
  `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-validation-history-through-rp-007.md`

### 当前决策（RP-008）

- 继续把 latest-head unresolved threads 作为主信号，只修仍在本地成立的评论，不为已失效的历史 summary 做无意义回写
- active tracking 只保留最新验证摘要与恢复点；详细验证历史留在 topic 自己的 archive，而不是继续堆在默认 boot 路径
- `gframework-pr-review` 的脚本行为、技能文案与 `AGENTS.md` 必须保持同一套 WSL Git 策略，避免再次出现“文档说法正确但工具实现仍跑偏”的情况

### 当前验证（RP-008）

- PR review 抓取：
  - `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output /tmp/gframework-current-pr-review.json`：通过
- 脚本语法校验：
  - `python3 -B -c "from pathlib import Path; compile(Path('.agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py').read_text(encoding='utf-8'), '.agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py', 'exec')"`：通过
- 文档校验：
  - `validate-all.sh docs/zh-CN/source-generators/cqrs-handler-registry-generator.md`：通过
  - `validate-all.sh docs/zh-CN/ecs/index.md`：通过
  - `validate-all.sh docs/zh-CN/abstractions/ecs-arch-abstractions.md`：通过
- 构建校验：
  - `cd docs && bun run build`：通过；仅保留既有 VitePress 大 chunk warning，无构建失败

### 下一步

1. 提交本轮 PR review follow-up
2. 推送当前分支后重新执行 `$gframework-pr-review`，观察 PR #271 的 open threads 是否收敛

### 当前恢复点：RP-009

- 按 `boot` 恢复 `documentation-full-coverage-governance` 主题
- 重新读取 `AGENTS.md`、`.ai/environment/tools.ai.yaml`、`ai-plan/public/README.md` 与当前 topic 的 active todo / trace 后，确认当前 worktree `docs/sdk-update-documentation` 仍映射到本 topic
- 当前 worktree Git 状态干净，且不存在 `ai-plan/private/` 的 worktree 私有恢复材料
- 重新执行 `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output /tmp/gframework-current-pr-review.json`
- 抓取结果显示 PR `#271` 已关闭，latest reviewed commit 仍为 `df91d3706ba9db71737e803ef2f40f4841ecbbf1`
- 当前 latest commit 仍显示 `2` 条 open thread，但两条都落在 `ai-plan` 文件上，且本地文件已经满足评论要求：
  - `ai-plan/public/archive/documentation-governance-and-refresh/traces/documentation-governance-and-refresh-trace.md` 已包含显式 `结果：通过`
  - `ai-plan/public/documentation-full-coverage-governance/todos/documentation-full-coverage-governance-tracking.md` 已将 RP-001 至 RP-007 的详细验证明细迁入 archive
- 因此本轮将 PR #271 follow-up 视为已完成，后续不再为 closed PR 上未自动收敛的陈旧 thread 状态追加仓库改动

### 当前决策（RP-009）

- `closed PR + stale open thread` 不再作为需要继续修改仓库内容的信号；除非后续 review 抓取显示新的 latest-head finding
- `documentation-full-coverage-governance` 的默认下一步切回治理 backlog，优先判断是否把 `Godot` family 的关键 XML inventory 摘要迁回 active topic
- 本轮 `boot` 不引入 subagent；关键恢复信号都能通过本地读取和单次 PR review 抓取直接确认

### 当前验证（RP-009）

- PR review 抓取：
  - `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output /tmp/gframework-current-pr-review.json`：通过；PR `#271` 已关闭，latest reviewed commit 为 `df91d3706ba9db71737e803ef2f40f4841ecbbf1`，当前 `2` 条 open thread 都是已被本地文件满足的陈旧信号
- 构建校验：
  - `cd docs && bun run build`：通过；仅保留既有 VitePress 大 chunk warning，无构建失败

### 下一步

1. 评估是否需要把 `Godot` family 的关键 XML inventory 摘要迁回 active topic
2. 若不迁回，则在 active todo / trace 保留足够的 archive 指针，并继续抽查 README / landing page / API reference 的 cross-link 是否出现新的漂移

### 当前恢复点：RP-010

- 按 `boot` 恢复当前 topic 后，重新读取：
  - `AGENTS.md`
  - `.ai/environment/tools.ai.yaml`
  - `ai-plan/public/README.md`
  - `ai-plan/public/documentation-full-coverage-governance/todos/documentation-full-coverage-governance-tracking.md`
  - `ai-plan/public/documentation-full-coverage-governance/traces/documentation-full-coverage-governance-trace.md`
- 确认当前任务状态属于 `resume`：
  - 当前分支仍为 `docs/sdk-update-documentation`
  - `ai-plan/public/README.md` 继续把本 worktree 映射到 `documentation-full-coverage-governance`
  - 当前 worktree 没有 `ai-plan/private/` 私有恢复材料
- 为判断 `Godot` family 是否需要回填恢复摘要，补读归档主题：
  - `ai-plan/public/archive/documentation-governance-and-refresh/todos/documentation-governance-and-refresh-tracking.md`
  - `ai-plan/public/archive/documentation-governance-and-refresh/traces/documentation-governance-and-refresh-trace.md`
  - `ai-plan/public/archive/documentation-governance-and-refresh/archive/todos/documentation-governance-and-refresh-history-through-2026-04-22.md`
- 归档材料表明，`Godot` family 的可恢复关键信号已经稳定，且足以压缩成 active topic 里的最小摘要：
  - 核心页面集为 `docs/zh-CN/godot/index.md`、`architecture.md`、`scene.md`、`ui.md`、`signal.md`、`extensions.md`、`logging.md` 与 `docs/zh-CN/tutorials/godot-integration.md`
  - `GFramework.Godot.SourceGenerators` 继续作为 `[GetNode]`、`[BindNodeSignal]`、`AutoLoads`、`InputActions` 的 owner
  - `GFramework.Godot.SourceGenerators.Abstractions` 继续按 `IsPackable=false` 的内部支撑模块处理
  - `GodotSceneFactory` 在 provider 缺失时回退到 `SceneBehaviorFactory`，而 `GodotUiFactory` 仍要求 `IUiPageBehaviorProvider`
- 因此本轮决定：
  - 不把整段 `documentation-governance-and-refresh` 历史重新迁回 active 路径
  - 只把足够让未来 `boot` 快速恢复的 `Godot` family 摘要写回 active todo
  - 继续把阶段级细节留在 archive，保持默认恢复入口轻量

### 当前决策（RP-010）

- `Godot` family 的“最小恢复摘要”应当留在 active topic，因为它已经属于长期治理 backlog 的默认上下文，而不仅仅是已完成项目的历史注脚
- active topic 只保留对后续判断有用的事实：
  - 页面范围
  - generator owner
  - Scene / UI 真实运行时边界
  - archive 指针
- `documentation-governance-and-refresh` archive 继续作为阶段级历史证据，不重新回到 `boot` 默认扫描路径
- 下一步从“是否回填摘要”切换回“继续巡检 cross-link 漂移”，避免治理入口停留在已经完成的元问题上

### 当前验证（RP-010）

- 归档恢复检查：
  - `sed -n '1,260p' ai-plan/public/archive/documentation-governance-and-refresh/todos/documentation-governance-and-refresh-tracking.md`：通过
  - `sed -n '1,260p' ai-plan/public/archive/documentation-governance-and-refresh/traces/documentation-governance-and-refresh-trace.md`：通过
  - `sed -n '1,240p' ai-plan/public/archive/documentation-governance-and-refresh/archive/todos/documentation-governance-and-refresh-history-through-2026-04-22.md`：通过

### 下一步

1. 抽查 `Godot` 与 `Game` 相关 README / landing page / API reference 的 cross-link 是否出现新的漂移
2. 当后续分支修改相关 README / docs / 公共 API 时，回到对应 module family 追加 targeted 巡检与验证

### 当前恢复点：RP-011

- 继续按 `boot` 恢复后的默认下一步执行 `Godot` / `Game` cross-link 巡检，并额外补读：
  - `GFramework.Godot/README.md`
  - `GFramework.Godot.SourceGenerators/README.md`
  - `docs/zh-CN/api-reference/index.md`
  - `docs/zh-CN/godot/index.md`
  - `docs/zh-CN/source-generators/index.md`
- 结合 `GFramework.Godot.csproj`、`GFramework.Godot.SourceGenerators.csproj`、相关测试与 `scan_module_evidence.py` 输出，确认新的漂移点集中在入口 README：
  - `GFramework.Godot/README.md` 仍是旧版简略说明，没有记录当前包关系、子系统地图、最小接入路径与 `docs/zh-CN` 入口
  - `GFramework.Godot.SourceGenerators/README.md` 没有覆盖 `AutoScene`、`AutoUiPage`、`AutoRegisterExportedCollections` 这些当前已发布的生成器分组
  - `docs/zh-CN/api-reference/index.md` 的 `Godot` 映射仍只把生成器入口落到泛化总览页，恢复效率偏低
- 因此本轮执行最小修复集：
  - 重写 `GFramework.Godot/README.md`
  - 重写 `GFramework.Godot.SourceGenerators/README.md`
  - 更新 `docs/zh-CN/api-reference/index.md` 的 `Godot` 行

### 当前决策（RP-011）

- 这轮不改 `docs/zh-CN/godot/**` landing / topic 页面，因为站内页面本身没有发现新的事实漂移，问题集中在仓库 README 与 API 入口的回退
- `GFramework.Godot` README 必须和 `Game` / `Godot` 真实边界一致，明确它不是生成器 owner，也不引入虚构的 router 类型
- `GFramework.Godot.SourceGenerators` README 采用“元数据 / 节点注入与信号绑定 / 行为包装 / 批量注册”四段式入口，避免读者只看到旧的三项能力
- API 参考页对 `Godot` 生成器入口直接给出专题页链接，而不是仅要求读者再从总览页二次分流

### 当前验证（RP-011）

- 模块扫描：
  - `python3 .agents/skills/gframework-doc-refresh/scripts/scan_module_evidence.py Godot`：通过
  - `python3 .agents/skills/gframework-doc-refresh/scripts/scan_module_evidence.py Godot.SourceGenerators`：通过
- 文档校验：
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-links.sh GFramework.Godot/README.md`：通过
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-code-blocks.sh GFramework.Godot/README.md`：通过
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-links.sh GFramework.Godot.SourceGenerators/README.md`：通过
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-code-blocks.sh GFramework.Godot.SourceGenerators/README.md`：通过
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/api-reference/index.md`：通过
- 构建校验：
  - `cd docs && bun run build`：通过；仅保留既有 VitePress 大 chunk warning，无构建失败

### 下一步

1. 继续抽查根 `README.md`、`docs/zh-CN/source-generators/index.md` 与 `docs/zh-CN/tutorials/godot-integration.md` 是否仍把 `Godot` owner 写回旧边界
2. 当后续分支继续修改 `Game` / `Godot` family 入口时，沿用当前 README -> landing -> API reference 的最小修复顺序

### 当前恢复点：RP-012

- 继续按 `boot` 恢复后的默认下一步执行 `Game` / `Godot` 入口巡检，并重新读取：
  - `README.md`
  - `docs/zh-CN/source-generators/index.md`
  - `docs/zh-CN/tutorials/godot-integration.md`
  - `docs/zh-CN/api-reference/index.md`
  - `GFramework.Godot/README.md`
  - `GFramework.Godot.SourceGenerators/README.md`
- 巡检结果显示主体内容仍然稳定，但根入口摘要存在一处残留漂移：
  - 根 `README.md` 仍把 `GFramework.Godot.SourceGenerators` 写成“Godot 场景专用源码生成器”，与当前包实际覆盖的 `project.godot` 元数据、节点注入、信号绑定、Scene / UI 包装和导出集合注册职责不符
  - `docs/zh-CN/source-generators/index.md` 的选包描述同步缺少 Scene / UI 包装与导出集合注册辅助这组能力
- 因此本轮执行最小修复集：
  - 更新根 `README.md` 的 `GFramework.Godot.SourceGenerators` 模块描述
  - 更新 `docs/zh-CN/source-generators/index.md` 的 Godot 选包摘要

### 当前决策（RP-012）

- 继续维持“只修新发现的入口漂移，不重写稳定页面”的治理节奏；这轮不改 `docs/zh-CN/tutorials/godot-integration.md`，因为教程与 README / 生成器专题页仍使用同一套职责边界
- 根 `README.md` 作为仓库一级入口，必须与模块 README 保持同一粒度的职责摘要；如果根入口比模块 README 更旧，后续 `boot` 和人工恢复都会被误导
- `source-generators/index.md` 的选包段落需要覆盖当前真实能力分组，但不重复展开各专题页细节，避免重新长成第二份 README

### 当前验证（RP-012）

- 文档校验：
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-links.sh README.md`：通过
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-code-blocks.sh README.md`：通过
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators/index.md`：通过
- 构建校验：
  - `cd docs && bun run build`：通过；仅保留既有 VitePress 大 chunk warning，无构建失败

### 下一步

1. 继续抽查 `docs/zh-CN/tutorials/godot-integration.md`、`docs/zh-CN/godot/index.md` 与根 `README.md` 的职责摘要是否继续保持同一口径
2. 当后续分支继续修改 `Game` / `Godot` family 入口时，沿用当前 README -> landing -> API reference 的最小修复顺序

### 当前恢复点：RP-013

- 使用 `$gframework-boot` 恢复当前 worktree 后，按 `documentation-full-coverage-governance` 的默认下一步执行一次
  validation-only 巡检，并补读：
  - `README.md`
  - `docs/zh-CN/godot/index.md`
  - `docs/zh-CN/tutorials/godot-integration.md`
  - `docs/zh-CN/source-generators/index.md`
  - `docs/zh-CN/api-reference/index.md`
  - `GFramework.Godot/README.md`
  - `.agents/skills/gframework-doc-refresh/SKILL.md`
- 同时执行 `python3 .agents/skills/gframework-doc-refresh/scripts/scan_module_evidence.py Godot`，确认当前 `Godot`
  docs surface 除 `index.md`、`architecture.md`、`scene.md`、`ui.md`、`signal.md`、`extensions.md`、`logging.md`
  外，还应把 `storage.md` 与 `setting.md` 视为默认恢复集合的一部分
- 巡检结论：
  - 根 `README.md`、`docs/zh-CN/godot/index.md`、`docs/zh-CN/tutorials/godot-integration.md`、
    `docs/zh-CN/source-generators/index.md` 与 `docs/zh-CN/api-reference/index.md` 当前仍保持同一套 `Godot`
    owner / adoption path 叙述，没有发现新的入口漂移
  - 本轮不需要改动稳定的 README / docs 页面，只需要把 active topic 的最小恢复摘要补齐到当前 landing page
    实际覆盖的页集合
- 因此本轮执行的唯一修改是：
  - 更新 `ai-plan/public/documentation-full-coverage-governance/todos/documentation-full-coverage-governance-tracking.md`
    的恢复点、`Godot` 页面集合、稳定性巡检结论与下一步
  - 记录本条 `RP-013` trace，保证未来 `boot` 不会漏掉 `storage.md` / `setting.md`

### 当前决策（RP-013）

- 当前 topic 继续保持“巡检优先、最小修复”的节奏；验证通过时不为凑改动而重写稳定页面
- `scan_module_evidence.py` 识别出的 docs surface 应优先反映到 active recovery artifact，而不是只留在一次性 chat
  上下文
- `Godot` family 的后续巡检重点从“根入口是否还残留旧描述”切换为“storage / setting 子页是否和 landing / README
  保持同一口径”

### 当前验证（RP-013）

- 模块扫描：
  - `python3 .agents/skills/gframework-doc-refresh/scripts/scan_module_evidence.py Godot`：通过
- 文档校验：
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/index.md`：通过
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/tutorials/godot-integration.md`：通过
- 构建校验：
  - `cd docs && bun run build`：通过；仅保留既有 VitePress 大 chunk warning，无构建失败

### 下一步

1. 若后续分支继续调整 `GFramework.Godot` 运行时入口，优先复核 `docs/zh-CN/godot/storage.md`、`setting.md` 与根
   `README.md` / landing page 是否仍保持同一套职责边界
2. 当后续分支再修改 `Godot` / `Game` family 的 README、docs 或公共 API 时，回到对应模块追加 targeted 巡检与验证

### 当前恢复点：RP-014

- 继续沿用 `RP-013` 的 `Godot` docs surface 巡检范围，补读：
  - `docs/zh-CN/godot/storage.md`
  - `docs/zh-CN/godot/setting.md`
  - `GFramework.Godot/Storage/GodotFileStorage.cs`
  - `GFramework.Godot/Setting/GodotAudioSettings.cs`
  - `GFramework.Godot/Setting/GodotGraphicsSettings.cs`
  - `GFramework.Godot/Setting/GodotLocalizationSettings.cs`
  - `GFramework.Godot/Setting/Data/AudioBusMap.cs`
  - `GFramework.Godot/Setting/Data/LocalizationMap.cs`
  - `GFramework.Game.Tests/Setting/GodotLocalizationSettingsTests.cs`
  - `ai-libs/CoreGrid/scripts/module/UtilityModule.cs`
  - `ai-libs/CoreGrid/scripts/module/ModelModule.cs`
- 巡检发现两处新的 topic 级漂移：
  - `docs/zh-CN/godot/storage.md` 仍按旧版 API 手册组织，缺少 frontmatter、当前 `IStorage` / repository 分工与
    `GodotYamlConfigLoader` 分流说明
  - `docs/zh-CN/godot/setting.md` 仍使用过时的“settings data 直接注入 applicator 构造函数”叙述，没有反映当前
    `ISettingsModel` + `RegisterApplicator(...)` 的真实接线方式
- 因此本轮执行最小修复集：
  - 重写 `docs/zh-CN/godot/storage.md`
  - 重写 `docs/zh-CN/godot/setting.md`
  - 更新 active tracking 的恢复点、巡检结论与验证结果

### 当前决策（RP-014）

- 继续遵循“README / landing 稳定时，不重写稳定入口；只修新发现的 topic 漂移”的治理节奏
- `storage.md` 应强调宿主路径语义与 repository 分工，而不是重复 `Game` 通用存储手册
- `setting.md` 应强调 applicator 注册和运行时边界，而不是重新维护一份过时的设置 API 摘要

### 当前验证（RP-014）

- 模块扫描：
  - `python3 .agents/skills/gframework-doc-refresh/scripts/scan_module_evidence.py Godot`：通过
- 文档校验：
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/storage.md`：通过
  - `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/setting.md`：通过
- 构建校验：
  - `cd docs && bun run build`：通过；仅保留既有 VitePress 大 chunk warning，无构建失败

### 下一步

1. 若后续分支继续调整 `GFramework.Godot` 运行时入口，优先复核 `docs/zh-CN/godot/storage.md`、`setting.md` 与根
   `README.md` / landing page 是否仍保持同一套职责边界
2. 当后续分支再修改 `Godot` / `Game` family 的 README、docs 或公共 API 时，回到对应模块追加 targeted 巡检与验证
