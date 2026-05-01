# Documentation Full Coverage Governance 跟踪

## 目标

持续治理 `GFramework` 的 README、`docs/zh-CN`、站点导航、XML 文档和 API 参考链路，避免阶段性刷新完成后再次回漂。

- 用源码、测试、`*.csproj` 和必要的 `ai-libs/` 证据校正文档
- 以模块族为单位闭环 README、landing page、专题页、教程入口和 API 参考链路
- 把 XML 文档缺口与 reader-facing 采用路径持续纳入同一主题治理

## 当前恢复点

- 恢复点编号：`DOCUMENTATION-FULL-COVERAGE-GOV-RP-055`
- 当前阶段：`Phase 5 - Governance Maintenance`
- 当前焦点：
  - 处理 PR `#308` 当前 latest-head review 与 outside-diff review 中经本地复核仍成立的 reader-facing 文档与 active `ai-plan` 漂移问题
- 当前事实：
  - `2026-05-01` 重新抓取 `$gframework-pr-review` 后确认：PR `#308` 处于 `OPEN`，latest reviewed commit 已前进到 `00ecf6fb1083e9039c9dc544a3265c38e1ba9117`
  - 当前 `CodeRabbit` 仍有 `5` 条 latest-head open threads；其中 `2` 条是本轮 latest review 针对 active tracking 与 `schema-config-generator.md` 措辞提出的新 actionable comments，另有 `3` 条为已被当前文件内容满足但尚未由远端重新计算关闭的 stale 线程
  - `Greptile` / `Gemini Code Assist` 当前无 open thread，CodeRabbit latest review 状态为 `CHANGES_REQUESTED`
  - 本地复核后，最新 review 中真正仍成立的 `2` 条问题分别是：active tracking 的 RP-055 验证引用仍指向 `RP-049` 到 `RP-052` 归档，以及 `schema-config-generator.md` 的“更稳妥地回退”措辞未同步
  - `docs/zh-CN/source-generators/schema-config-generator.md` 已包含独立的“迁移与兼容性”章节，且运行时示例已补齐 `configRootPath` 定义，因此对应的 major / minor open threads 现阶段应视为 stale，等待提交推送后再由远端重新计算
  - GitHub Test Reporter 汇总为 `2247 passed / 0 failed`
  - `Title check` 仍为 `Inconclusive`，属于 PR 元数据问题，不是仓库文件内可直接修复的阻塞项
  - 本地修复尚未推送前，不会改变远端 latest reviewed commit 与 open-thread 统计
- 当前风险：
  - 如果 active tracking / trace 继续保留旧的 commit SHA 和 review 归因，会让后续恢复点重复处理已经本地闭环的问题
  - 在变更推送前，PR 页面仍会继续展示 latest reviewed commit `00ecf6fb...` 下的 open-thread 数量，容易把 stale 线程误判为新的本地缺陷

## 当前状态摘要

- `Core`、`Ecs.Arch`、`Cqrs`、`Game`、`Godot` 五个模块族当前都已有 README / landing / topic / API 参考层级的已验证入口。
- `source-generators` 栏目已经补出 `Schema 配置生成器` 专题页，并把 `Game.SourceGenerators` 接回 landing、API 入口与侧栏。
- `Cqrs.SourceGenerators` 的 fallback 精度、`GF_Cqrs_001` 诊断边界与共享支撑层阅读路线，当前已经回收进现有专题页与入口页，而不是继续扩成新的维护者导向页面。
- `RP-049` 到 `RP-052` 的阶段细节、逐命令验证和批次决策已迁入归档；active 文档只保留当前恢复事实、风险、验证结果与下一步。

## 归档指针

- 详细验证历史（`RP-001` 到 `RP-007`）：
  `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-validation-history-through-rp-007.md`
- 阶段状态归档（`RP-001` 到 `RP-016`）：
  `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-status-history-through-rp-016.md`
- 阶段状态归档（`RP-023` 到 `RP-025`）：
  `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-status-history-rp-023-to-rp-025-2026-04-24.md`
- 阶段状态归档（`RP-049` 到 `RP-052`）：
  `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-status-history-rp-049-to-rp-052-2026-05-01.md`
- 时间线归档（`RP-001` 到 `RP-016`）：
  `ai-plan/public/documentation-full-coverage-governance/archive/traces/documentation-full-coverage-governance-trace-history-through-rp-016.md`
- 时间线归档（`RP-023` 到 `RP-025`）：
  `ai-plan/public/documentation-full-coverage-governance/archive/traces/documentation-full-coverage-governance-trace-history-rp-023-to-rp-025-2026-04-24.md`
- 时间线归档（`RP-041` 到 `RP-048`）：
  `ai-plan/public/documentation-full-coverage-governance/archive/traces/documentation-full-coverage-governance-trace-history-rp-041-to-rp-048-2026-04-28.md`
- 时间线归档（`RP-049` 到 `RP-052`）：
  `ai-plan/public/documentation-full-coverage-governance/archive/traces/documentation-full-coverage-governance-trace-history-rp-049-to-rp-052-2026-05-01.md`
- 验证历史归档（`RP-041` 到 `RP-048`）：
  `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-validation-history-rp-041-to-rp-048-2026-04-28.md`
- 验证历史归档（`RP-049` 到 `RP-052`）：
  `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-validation-history-rp-049-to-rp-052-2026-05-01.md`

## 最新验证

- `2026-05-01` 本轮关键验证结论：`$gframework-pr-review` 重新抓取通过，确认 PR `#308` 的 latest reviewed commit 为 `00ecf6fb1083e9039c9dc544a3265c38e1ba9117`，`CodeRabbit` 当前仍有 `5` 条 latest-head open threads；其中最新 review 的 `2` 条 actionable comments 已在当前工作树完成修复，其余 `3` 条因当前文件内容已满足而应视为 stale。两篇 source-generators 页面校验通过，`docs/` 站点构建通过并仅保留既有大 chunk warning。
- 本轮逐条命令与详细结果见 active trace 的 `RP-055` 验证段：
  - `ai-plan/public/documentation-full-coverage-governance/traces/documentation-full-coverage-governance-trace.md`

## 下一步

1. 提交并推送本轮 source-generators 文档与 active `ai-plan` follow-up，然后重新抓取 `$gframework-pr-review`。
2. 若 remote open threads 只剩 stale 的“迁移与兼容性”线程或 `Title check` 这类 metadata 项，则停止本轮仓库内修复。
3. 若 review 线程清空或只剩 metadata 项，再按 `$gframework-batch-boot 50` 继续挑选新的 coverage 切片，避免在同一轮混入无关改稿。
