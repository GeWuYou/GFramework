# Documentation Full Coverage Governance Status History (RP-049 to RP-052)

## Scope

- 该归档承接 active tracking 从 `RP-049` 到 `RP-052` 迁出的阶段状态、批次边界和恢复决策。
- 逐命令验证明细单独保存在：
  `ai-plan/public/documentation-full-coverage-governance/archive/todos/documentation-full-coverage-governance-validation-history-rp-049-to-rp-052-2026-05-01.md`

## 2026-04-29 / RP-049

- 重新进入时确认当前分支仍为 `docs/sdk-update-documentation`，但上游恢复路径不再依赖旧 PR 线程，而是改用 `origin/main` 作为 batch stop-condition baseline。
- 本轮工作树在 reader-facing 文案收口后相对 `origin/main` 来到 `13` files / `132` lines，仍明显低于 `$gframework-batch-boot 50` 的阈值。
- 主线程接受的变更范围限定在 `docs/zh-CN/godot/*`、`game/*` 和少量 README 标签，不扩展到导航重写或大型结构改稿。

## 2026-04-29 / RP-050

- 在 `RP-049` 的基础上继续做第 2 批低风险 reader-facing 收口，触达 `game/data.md`、`game/storage.md`、`godot/ui.md` 与 `GFramework.Cqrs.Abstractions/README.md`、`GFramework.SourceGenerators.Common/README.md`。
- 决策上只接受“改句子即可闭环”的问题，不把 README 子系统地图或结构级重写混入同一轮。
- 当轮工作树相对 `origin/main` 为 `18` files / `225` lines，仍保留充足余量。

## 2026-04-29 / RP-051

- 从与 `origin/main` 零 diff 的状态重新进入后，把批次目标从“低风险句子收口”提升为“补新的 docs coverage 入口”。
- 这一轮新增 `docs/zh-CN/source-generators/schema-config-generator.md`，并同步更新 `source-generators/index.md`、`api-reference/index.md`、`source-generators/cqrs-handler-registry-generator.md`、`core/cqrs.md` 与 `docs/.vitepress/config.mts`。
- 接受的核心结论是：`Game.SourceGenerators` 需要新的 reader-facing 专题页，而 `SourceGenerators.Common` 和各 `*.SourceGenerators.Abstractions` 更适合作为现有入口中的共享排障层阅读路线。

## 2026-04-30 / RP-052

- 该批次提交为 `f88f96c3`（`docs(source-generators): 补充生成器专题覆盖并更新进度`）。
- 提交后重新计算确认 committed branch diff vs `origin/main` 已回落到 `8` files / `337` lines，说明提交前的 `39` files / `2555` lines 只是临时工作树峰值，不应继续作为默认恢复指标。
- active topic 后续仍可以继续按 `$gframework-batch-boot 50` 推进，但应优先挑“已有 package README、但站内专题仍不足”的覆盖切片。
