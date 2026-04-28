# Documentation Full Coverage Governance Trace History (RP-041 to RP-048)

## Scope

- 该归档记录 `RP-041` 到 `RP-048` 从 active trace 迁出的阶段性时间线，保留每轮恢复点、主要决策与停止条件。
- 逐命令验证明细继续保存在：
  `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-validation-history-rp-041-to-rp-048-2026-04-28.md`

## 2026-04-28 / RP-048

- 通过 `$gframework-pr-review` 重新抓取 PR `#299` 后确认：latest head review 只剩 `1` 条 `CodeRabbit` open thread 与 `1` 条 nitpick，且都指向 active tracking 文档，而非公开文档页面本身。
- 本地复核确认此前对抽象层入口语义化链接、生命周期入口示例与教程 / 排障页的修正已不再构成当前 remote latest-head review 的剩余阻塞。
- 因此本轮收敛为 active tracking 收口：补齐 `RP-048` 下一步，并将 `RP-045` 到 `RP-047` 的逐命令历史从 active trace 下沉到 archive。

## 2026-04-27 / RP-047

- 第 `2` 批次提交 `2d2cd0c` 后，branch diff 相对 `origin/main` 为 `13` files / `124` lines，仍明显低于 `$gframework-batch-boot 50` 的 stop condition。
- 最后一批集中收口 `core/query.md`、`core/command.md`、`core/context.md`、`game/scene.md`、`godot/ui.md`、`source-generators/priority-generator.md`、`core/lifecycle.md`、`game/ui.md`、`godot/scene.md`、`source-generators/context-aware-generator.md` 中依赖“旧文档 / 旧入口”对比的句式。
- 在这里停止，不是因为达到阈值，而是因为剩余命中已转成需要人工判断是否保留迁移边界或 README 交叉引用的问题。

## 2026-04-27 / RP-046

- 第 `1` 批次提交 `c56260b` 后，branch diff 相对 `origin/main` 为 `7` files / `68` lines，仍远低于 stop condition。
- 本轮继续处理 `docs/zh-CN/game/ui.md`、`godot/signal.md` 以及 `4` 个 source-generators 专题页，把直接暴露 `ai-libs/CoreGrid` 路径的公开说明改写为项目侧常见实现或典型入口组织方式。
- 决策上继续保持单页文字级边界，不把 `ai-libs/**` 当成公开导航或消费者说明，并在每个稳定批次校验通过后立即提交，以便精确计量 branch diff。

## 2026-04-27 / RP-045

- 通过 `$gframework-batch-boot 50` 从零 diff 状态重新进入，沿用显式 `--git-dir` / `--work-tree` 绑定，确认当前分支仍为 `docs/sdk-update-documentation`，baseline 固定到最新本地 `origin/main` `7cfdd2c`。
- 第 `1` 批次先处理 `docs/zh-CN/source-generators/index.md`、`game/index.md`、`api-reference/index.md`、`godot/setting.md`、`abstractions/index.md` 的 reader-facing 标题、描述与导航措辞，不改示例代码，也不扩栏目结构。
- 当轮下一候选批次被限定为继续清理 `ai-libs/CoreGrid` 与“旧文档”式指向表达，保持风险最低且适合批处理。

## 2026-04-27 / RP-044

- 本轮从 `$gframework-pr-review` 重新进入，抓取 PR `#296` 后确认 latest reviewed commit 为 `5778782df05e22dd24dc95189dd768458afb8537`，共有 `4` 条 open thread。
- 接受的仍成立问题集中在 `GFramework.Game.SourceGenerators/README.md` 的表头语义、`GFramework.Game/README.md` 的重复 `storage.md` 链接，以及 `docs/zh-CN/tutorials/godot-integration.md`、`docs/zh-CN/godot/extensions.md` 的 reader-facing 措辞收口。
- 唯一 failed check `Title check` 被明确排除在仓库文件修复范围之外，本轮只做 review 指向文件的最小修正并同步 tracking / trace。

## 2026-04-27 / RP-043

- 在提交 `docs(reader-facing): 统一站内入口与公开术语` 后，branch diff 已到 `46` changed files，接近 `$gframework-batch-boot 50` 的停止线。
- 因此最后只接受 `10` 个还没进入 branch diff 的文件，包括 `tutorials/godot-integration.md`、`game/setting.md`、`game/serialization.md`、`godot/index.md`、`godot/architecture.md`、`godot/storage.md`、`godot/logging.md`、`godot/setting.md`、`godot/extensions.md`、`core/architecture.md`。
- 这些修正统一把 `旧文档`、`ai-libs`、`.Wait()`、`family` 等维护 / 内部口吻改成当前采用指导，并在验证通过后立即停止扩批。

## 2026-04-27 / RP-042

- 用户明确允许在 stop condition 内循环推进，并接受使用 subagent 降低主线程上下文压力；主线程保留实现与验证，把热点识别交给 `3` 个 explorer。
- 接受的结论主要是：入口页应统一为 reader-facing 骨架；GitHub blob README 不应继续作为公开主导航；多个 README 与 Godot 页面还残留 `ai-libs`、`family`、`seam`、`ReadMe.md` 等对外不友好的表述。
- 基于这些结论，连续落地了入口页改写、README / Godot 页面去内部口吻、以及 GitHub blob README 外链回归站内入口等 `3` 组低风险切片。

## 2026-04-27 / RP-041

- 这一轮用于提交后重算 branch diff，并确认在继续补一批新文件后，工作树已经来到 `46` changed files，足够触发“接近 stop condition 时收口”的策略。
- 因此最后批次刻意限制在 `10` 个尚未进入 branch diff 的文件，并保持全部为 reader-facing 文案修正，不扩大到新结构或示例体系重写。
- 验证通过后，将后续恢复建议切换为“下一轮从 PR review 或剩余未触达细页重新开一轮”，而不是在同一轮继续堆文件数。
