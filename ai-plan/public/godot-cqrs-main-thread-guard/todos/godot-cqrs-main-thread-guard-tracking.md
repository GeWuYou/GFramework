<!--
Copyright (c) 2025-2026 GeWuYou
SPDX-License-Identifier: Apache-2.0
-->

# Godot CQRS 主线程保护跟踪

## 目标

围绕 `fix/godot-cqrs-main-thread-guard` 当前 PR 的 AI review 结果，完成仍然有效的文档、提示文案与回归测试修复。

## 当前恢复点

- 恢复点编号：`GODOT-CQRS-GUARD-RP-001`
- 当前阶段：`PR review triage`
- 当前 PR 锚点：`PR #366（OPEN，2026-06-19 复核）`
- 当前结论：
  - `$gframework-pr-review` 已确认当前 PR 最新 head 上仍有 `6` 条 AI review open threads。
  - 本地复核后仍然有效的问题集中在 `GFramework.Godot` 与 `GFramework.Godot.Tests`，不涉及新的 CI 失败测试。
  - 需要修复的有效问题包括：
    - `AbstractArchitecture` 受保护构造函数缺少完整 XML 契约说明
    - `GodotArchitectureContext` 的公开接口实现缺少 XML 文档
    - 同步查询守卫提示缺少 `SendQueryAsync(...)`
    - 测试公开入口缺少 XML 注释，且存在 `Assert.Throws*` 返回值非空的冗余断言

## 当前活跃事实

- 当前分支：`fix/godot-cqrs-main-thread-guard`
- 当前工作面：
  - `GFramework.Godot/Architectures/AbstractArchitecture.cs`
  - `GFramework.Godot/Architectures/GodotArchitectureContext.cs`
  - `GFramework.Godot.Tests/Architectures/AbstractArchitectureModuleInstallationTests.cs`
- 当前 review 真值：
  - CodeRabbit open threads：`4`
  - Greptile open threads：`2`
  - failed checks：`Docstring Coverage` warning、`MegaLinter` success with warnings
  - 测试报告：`2299 passed / 0 failed`

## 当前风险

- 若只补接口文档而不修正守卫文案，`SendQuery(...)` 的调用方仍会收到不完整迁移指引。
- 若测试继续保留冗余断言而不覆盖同步查询提示，review 线程即使重新跑也可能继续成立。

## 最近权威验证

- `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output /tmp/gframework-current-pr-review.json`
  - 结果：通过
  - 备注：确认当前分支对应 `PR #366`，并抓到 `6` 条 latest-head open threads
- `python3 scripts/license-header.py --check --paths ai-plan/public/README.md ai-plan/public/godot-cqrs-main-thread-guard/todos/godot-cqrs-main-thread-guard-tracking.md ai-plan/public/godot-cqrs-main-thread-guard/traces/godot-cqrs-main-thread-guard-trace.md GFramework.Godot/Architectures/AbstractArchitecture.cs GFramework.Godot/Architectures/GodotArchitectureContext.cs GFramework.Godot.Tests/Architectures/AbstractArchitectureModuleInstallationTests.cs`
  - 结果：通过
  - 备注：本轮新增与修改文件均保留 Apache-2.0 头
- `git diff --check`
  - 结果：通过
  - 备注：当前 patch 未引入 whitespace 或 patch 格式问题
- `dotnet build GFramework.Godot/GFramework.Godot.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`
  - 备注：覆盖本轮运行时代码改动
- `dotnet build GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`
  - 备注：覆盖本轮测试代码改动
- `dotnet test GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release --filter "FullyQualifiedName~AbstractArchitectureModuleInstallationTests" -m:1`
  - 结果：失败
  - 备注：`VSTest` 在 `net10.0` 下直接报告 `Test host process crashed`，属于当前测试宿主运行时问题，尚未拿到用例级失败输出

## 下一推荐步骤

1. 提交本轮已验证的 runtime / test 文档修复。
2. 推送后重新执行 `$gframework-pr-review`，确认当前 open threads 是否只剩 stale 信号。
3. 若仍需测试级证明，再单独排查 `GFramework.Godot.Tests` 的 `net10.0` test host 崩溃原因。
