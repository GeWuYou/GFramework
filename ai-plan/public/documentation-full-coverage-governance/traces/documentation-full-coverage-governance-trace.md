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
