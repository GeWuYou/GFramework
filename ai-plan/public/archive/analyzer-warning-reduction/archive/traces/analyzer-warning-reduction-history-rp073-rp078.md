# Analyzer Warning Reduction 追踪归档（RP073-RP078）

## 2026-04-27 — RP-078

### 阶段：完成第三轮 Core.Tests 低风险 slice 并在 30 files 处收口

- 触发背景：
  - 第二轮结束后，`GFramework.Game` 低风险单文件 warning 已基本耗尽，继续推进更适合转向测试项目
  - 第三轮选择的 `Core.Tests` slice 仍保持单文件、低耦合，且不会明显放大 branch diff
- 已接受的 delegated scope 与结果：
  - worker-A：`GFramework.Core.Tests/Concurrency/AsyncKeyLockManagerTests.cs`
    - 结果：与 `PauseStackManagerTests.cs` 一并落在提交 `650618b`，修复该文件的 `MA0004`
  - worker-B：`GFramework.Core.Tests/Pause/PauseStackManagerTests.cs`
    - 结果：与 `AsyncKeyLockManagerTests.cs` 一并落在提交 `650618b`，修复该文件的 `MA0158`
  - worker-C：`GFramework.Core.Tests/Extensions/AsyncExtensionsTests.cs`、`GFramework.Core.Tests/Architectures/ArchitectureModulesBehaviorTests.cs`
    - 结果：提交 `e19e60e`，修复 `MA0015` / `MA0004`
- 主线程验证里程碑：
  - 提权 `dotnet clean`
    - 结果：成功
  - 提权 `dotnet build`
    - 结果：成功；warning 从上一轮的 `405` 降到 `397`
  - 提权 `dotnet build GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release`
    - 结果：成功；`0 Warning(s)`、`0 Error(s)`
  - `git diff --name-only refs/remotes/origin/main...HEAD | wc -l`
    - 结果：`30`
  - `git diff --numstat refs/remotes/origin/main...HEAD`
    - 结果：`642` changed lines
- 当前结论：
  - 当前分支在 `30 / 50` files 时仍保持可审阅性，且已经连续三轮拿到了实质 warning 降幅
  - 继续推进的剩余候选主要是 `YamlConfig*` 高耦合热点与 `MA0048` 批量拆分，不再符合本轮的低风险边界
  - 默认建议在这里收口当前波次，把下一波次留给更明确的热点专项

## 2026-04-27 — RP-077

### 阶段：完成第二轮 Game 侧低风险 slice 验证并转向测试项目候选

- 触发背景：
  - 第二轮 worker 已分别完成 `SettingsModel.cs`、`RouterBase.cs`+`UiInteractionProfiles.cs`、`GameConfigBootstrap.cs`
  - 主线程在复验时发现 `SettingsModel.cs` 与 `GameConfigBootstrap.cs` 又各暴露一个 touched-file `MA0158`，已在主线程补齐
- 已接受的 delegated scope 与结果：
  - worker-A：`GFramework.Game/Setting/SettingsModel.cs`
    - 结果：提交 `c106e53`，修复 `MA0004`；主线程随后补齐同文件 `MA0158`
  - worker-B：`GFramework.Game/Routing/RouterBase.cs`、`GFramework.Game/UI/UiInteractionProfiles.cs`
    - 结果：提交 `9deafac`，修复 `MA0006` / `MA0099`
  - worker-C：`GFramework.Game/Config/GameConfigBootstrap.cs`
    - 结果：提交 `9ce634e`，拆分热重载启动流程以修复 `MA0051`；主线程随后补齐同文件 `MA0158`
  - explorer：重新审视 `GFramework.Game` 排除热点后的剩余候选
    - 结果：确认 `Game` 侧低风险单文件 warning 基本耗尽，继续推进应转向其他项目
- 主线程验证里程碑：
  - 提权 `dotnet clean`
    - 结果：成功
  - 提权 `dotnet build`
    - 结果：成功；warning 从上一轮的 `430` 继续降到 `405`
  - 提权 `dotnet build GFramework.Game/GFramework.Game.csproj -c Release`
    - 结果：成功；warning 从上一轮的 `147` 降到 `122`
  - `git diff --name-only refs/remotes/origin/main...HEAD | wc -l`
    - 结果：`26`
  - `git diff --numstat refs/remotes/origin/main...HEAD`
    - 结果：`483` changed lines
- 当前结论：
  - 第二轮 Game 侧 warning 清理已完成验证，且 warning 数继续实质下降
  - 当前分支距离 `$gframework-batch-boot 50` 仍有空间，但继续推进不应再硬碰 `YamlConfigSchemaValidator*` / `YamlConfigLoader.cs`
  - 若继续下一轮，优先切向 `Core.Tests` 等测试项目里的单文件 `MA0004` / `MA0015` / `MA0158`

## 2026-04-27 — RP-076

### 阶段：首轮收口提交后进入第二轮低风险 Game warning slice

- 触发背景：
  - 首轮并行清理已经以 `fb0a55f` 收口，当前分支相对 `origin/main` 的累计改动文件数来到 `22 / 50`
  - 用户要求继续采用“先拿构建 warning，再分批交给 subagent”模式，因此当前仍有继续推进的 branch 预算
- 主线程当前真值：
  - 当前基线：`refs/remotes/origin/main` = `617e0bf`
  - 当前 `HEAD` stop metric：
    - files：`22`
    - changed lines：`378`
  - 最近权威验证仍为：
    - `dotnet build`：`430 Warning(s)`、`0 Error(s)`
    - `dotnet build GFramework.sln -c Release`：`147 Warning(s)`、`0 Error(s)`
- 本轮拟下发的 delegated scope：
  - worker-A：`GFramework.Game/Setting/SettingsModel.cs`
    - 目标：修复 `MA0004`，仅在不改变设置模型生命周期语义的前提下补全 `ConfigureAwait(false)`
  - worker-B：`GFramework.Game/Routing/RouterBase.cs` 与 `GFramework.Game/UI/UiInteractionProfiles.cs`
    - 目标：修复 `MA0006` / `MA0099`，保持现有路由比较语义与 UI 动作位掩码语义不变
  - worker-C：`GFramework.Game/Config/GameConfigBootstrap.cs`
    - 目标：评估并尽量修复 `MA0051`；若单文件安全提取不可低风险完成，应明确放弃并说明阻塞点
- 当前结论：
  - 第二轮继续严格限制在低风险单文件 slice，避免直接进入 `YamlConfigSchemaValidator*` 与 `YamlConfigLoader.cs` 这种高耦合热点
  - 本轮完成后应重新评估 branch diff 是否仍适合继续在同一分支上批量推进

## 2026-04-27 — RP-075

### 阶段：完成 `$gframework-batch-boot 50` 第一轮并行 warning 清理集成

- 触发背景：
  - 用户要求先以权威构建输出建立 warning 基线，再把低风险 warning family 按文件边界拆给不同 subagent 并行清理
  - 当前批次已完成首轮 worker 集成，但第二组锁迁移、主线程补修与 `ai-plan` 同步仍在工作树，需先收口提交再进入下一轮
- 已接受的 delegated scope 与结果：
  - worker-1：`GFramework.Core` 事件 / 状态 / 属性 / 协程统计中的 `MA0158`
    - 结果：已提交 `8f2d959`，采用 `#if NET9_0_OR_GREATER` + `System.Threading.Lock` / `object` 双分支兼容模式
  - worker-2：`GFramework.Core` / `GFramework.Cqrs` 资源、日志、配置缓存中的 `MA0158`
    - 结果：改动已集成到工作树，待主线程与本轮 `ai-plan` 一并提交
  - worker-3：`GFramework.Game/Data` 与 `SceneRouterBase.cs`
    - 结果：已提交 `e3eec54`，主线程随后补修 `SceneRouterBase.Contains` 与 `SaveRepository._migrationsLock` 的 touched-file 残留 warning
  - worker-4：`GFramework.Game/UI/UiRouterBase.cs`
    - 结果：已提交 `7e13752`
- 主线程验证里程碑：
  - 提权 `dotnet clean`
    - 结果：成功
  - 提权 `dotnet build`
    - 结果：成功；warning 从本轮批次建立时的 `639` 降到 `430`
  - 提权 `dotnet build GFramework.sln -c Release`
    - 结果：成功；`147 Warning(s)`、`0 Error(s)`
  - `git diff --name-only refs/remotes/origin/main...HEAD | wc -l`
    - 结果：`12`
  - `git diff --numstat refs/remotes/origin/main...HEAD`
    - 结果：`192` changed lines
- 当前结论：
  - 第一轮并行 warning 清理已经完成验证，且 warning 总量出现明显下降，可以继续按 batch 模式推进
  - 当前 stop-condition 仍远低于 `$gframework-batch-boot 50`；但在派发下一轮之前，应该先提交当前工作树里的第二组锁迁移与恢复文档同步
  - 下一轮优先目标保持“低风险、单文件、避免高耦合热点”，候选包括 `SettingsModel.cs`、`RouterBase.cs`、`UiInteractionProfiles.cs`

## 2026-04-27 — RP-074

### 阶段：按 `$gframework-batch-boot 50` 建立并行 warning 清理批次

- 触发背景：
  - 用户明确要求在拿到构建 warning 后分批指派给不同 subagent，以控制主线程上下文长度并提高 warning 清理效率
  - 当前 worktree 映射到 `analyzer-warning-reduction` 主题，且该任务符合 batch candidate 条件：重复、可切片、可按文件边界独立验证
- 基线与停止条件：
  - 当前基线采用 `refs/remotes/origin/main`
  - `origin/main` 与 `HEAD` 当前同为 `617e0bf`（`2026-04-26T12:17:15+08:00`）
  - 主 stop condition 为 branch diff files 接近 `50`；当前为 `0 / 50`
- 主线程实施：
  - 先读取 `AGENTS.md`、`.ai/environment/tools.ai.yaml`、`ai-plan/public/README.md` 以及当前 topic 的 active todo/trace，确认批处理流程与 topic 上下文
  - 先在沙箱内执行仓库根 `dotnet clean` / `dotnet build`；其中 `dotnet clean` 因缺失 Windows fallback package folder 失败，判定为环境噪音
  - 按仓库规则提权重跑直接命令，确认权威基线为 `dotnet clean` 成功、`dotnet build` 成功且 `639 Warning(s)`、`0 Error(s)`
  - 基于当前 warning 输出，预划分以下互不重叠的 subagent ownership：
    - `GFramework.Core` / `GFramework.Cqrs` 的 `MA0158` 专用锁迁移
    - `GFramework.Game/Data` 的 `MA0004` 与局部 `MA0002`
    - `GFramework.Game/Scene/SceneRouterBase.cs`、`GFramework.Game/UI/UiRouterBase.cs` 的显式上下文 / 参数名 / 比较器修正
- 验证里程碑：
  - `dotnet clean`
    - 结果：提权后成功；作为本轮 clean 真值
  - `dotnet build`
    - 结果：提权后成功；`639 Warning(s)`、`0 Error(s)`
  - `git diff --name-only refs/remotes/origin/main...HEAD | wc -l`
    - 结果：`0`
  - `git diff --numstat refs/remotes/origin/main...HEAD`
    - 结果：空输出
- 当前结论：
  - 本轮已经完成 batch boot 所需的权威警告基线建立，可以安全进入并行 worker 阶段
  - 当前优先级应继续保持在低风险、少文件、可独立验证的 warning family 上，不直接扩展到 `YamlConfigSchemaValidator` 这类高耦合热点
  - 下一步默认由主线程下发 disjoint worker 任务并在集成后重新计算 branch diff 与 warning 结果

## 2026-04-26 — RP-073

### 阶段：脱敏 analyzer-warning-reduction 文档中的绝对路径记录

- 触发背景：
  - 用户再次显式要求执行 `$gframework-pr-review`，当前分支仍对应 PR `#291`
  - 最新抓取结果确认 latest-head 还剩 `2` 条 open review thread，分别指向 active todo 与 archive trace 中记录的绝对路径
  - active trace 当前也保留了同类 `/tmp` 路径记录；虽然这次 review 没直接点名，但继续保留会留下同一类治理缺口
- 主线程实施：
  - 将 active todo 与 active trace 中的 PR review 输出路径改写为 `--json-output <current-pr-review-json>`
  - 将 [analyzer-warning-reduction-history-rp062-rp071.md](analyzer-warning-reduction-history-rp062-rp071.md) 里的临时 `dotnet` home、PR review 输出路径和失效 Windows fallback package folder 改写为仓库安全占位符
  - 同步刷新 active todo 中的 review 真值，把当前恢复点更新到 `RP-073`
- 验证里程碑：
  - `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output <current-pr-review-json>`
    - 结果：成功；确认 PR `#291` latest-head open review thread 为 `2`，两者都指向 `ai-plan` 文档中的绝对路径记录
  - `dotnet build`
    - 结果：成功；`639 Warning(s)`、`0 Error(s)`；与当前权威仓库根基线一致
- 当前结论：
  - 本轮只吸收当前仍成立的 PR review 文档项，不扩展到新的 warning 清理切片
  - 当前仓库根 warning 权威基线仍保持 `639 Warning(s)`、`0 Error(s)`；本轮目标是让 analyzer-warning-reduction 主题下当前入口不再记录绝对路径
  - 下一轮默认先推送本轮同步并重新执行 `$gframework-pr-review`，确认 PR `#291` 的 open thread 是否已自动收口
