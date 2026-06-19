<!--
Copyright (c) 2025-2026 GeWuYou
SPDX-License-Identifier: Apache-2.0
-->

# Godot CQRS 主线程保护追踪

## 当前恢复摘要

- 当前恢复点：`GODOT-CQRS-GUARD-RP-001`
- 当前日期：`2026-06-19`
- 当前分支：`fix/godot-cqrs-main-thread-guard`
- 当前 PR：`PR #366（OPEN）`
- 当前目标：
  - 用 `$gframework-pr-review` 获取当前 PR 的最新 AI review 真值
  - 只修复本地仍成立的文档、守卫提示与测试问题
  - 在不扩大写面的前提下完成最小验证并形成可恢复入口

## 2026-06-19

### 阶段：PR #366 review triage（GODOT-CQRS-GUARD-RP-001）

- 先按技能流程执行 PR review 抓取脚本，确认当前分支对应 `PR #366`。
- 当前抓取结果：
  - latest-head open threads：`6`
  - failed checks：`Docstring Coverage` warning
  - MegaLinter：`Success with warnings`
  - 测试汇总：`2299 passed / 0 failed`
- 本地复核后认定仍然有效的问题：
  - `AbstractArchitecture` 构造函数缺少参数与行为说明
  - `GodotArchitectureContext` 公开接口实现缺少 XML 文档
  - `GuardSyncCqrs` 对 `SendQuery(...)` 的异步替代方案提示不完整
  - `AbstractArchitectureModuleInstallationTests` 中公开测试方法缺少 XML 注释，且存在冗余 `Is.Not.Null` 断言
- 本轮计划写面：
  - `GFramework.Godot/Architectures/AbstractArchitecture.cs`
  - `GFramework.Godot/Architectures/GodotArchitectureContext.cs`
  - `GFramework.Godot.Tests/Architectures/AbstractArchitectureModuleInstallationTests.cs`
  - 当前 topic 的 `ai-plan/public/**` 恢复文档
- 本轮已完成修复：
  - 为 `AbstractArchitecture` 构造函数补齐参数与容器共享语义说明
  - 为 `GodotArchitectureContext` 的公开接口实现补齐 `inheritdoc` / 行为说明
  - 让同步守卫提示按 `SendCommand` / `SendQuery` / `SendRequest` 返回对应异步替代方案
  - 为 `AbstractArchitectureModuleInstallationTests` 补 XML 文档，移除冗余非空断言，并新增 `SendQueryAsync(...)` 提示断言
- 本轮验证摘要：
  - `license-header --check`：通过
  - `git diff --check`：通过
  - `dotnet build GFramework.Godot/GFramework.Godot.csproj -c Release`：通过，`0 warning / 0 error`
  - `dotnet build GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release`：通过，`0 warning / 0 error`
  - 初次 `dotnet test ... AbstractArchitectureModuleInstallationTests`：失败，`Test host process crashed`
  - 复核根因：`AbstractArchitecture_ShouldUseGodotContextWrapper_ByDefault` 在无 Godot 宿主环境里调用 `Initialize()`，执行 `Engine.GetMainLoop()` 时直接打崩 test host
  - 修复后 `dotnet test ... AbstractArchitecture_ShouldUseGodotContextWrapper_ByDefault`：通过
  - 修复后 `dotnet test GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release`：通过，`83 passed / 0 failed`
