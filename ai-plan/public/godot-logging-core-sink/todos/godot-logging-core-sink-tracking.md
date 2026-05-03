# Godot Logging Core Sink 跟踪

## 目标

在 `GFramework.Godot.Logging` 已完成宿主便利层收口后，评估并推进 Godot 输出与 `GFramework.Core` 日志扩展点的统一。
本主题优先判断是否应把 Godot 输出沉淀为 Core 可组合的 appender / sink，而不是继续扩张 Godot-only logging 管线。

## 当前恢复点

- 恢复点编号：`GODOT-LOGGING-CORE-SINK-RP-004`
- 当前阶段：`PR review follow-up 复查已验证`
- 当前焦点：
  - `GFramework.Godot.Logging.GodotLogAppender` 已作为 Core `ILogAppender` 的 Godot 宿主落点落地
  - `GodotLogger` 保留原有 `ILogger` 入口，但底层输出委托给 appender
  - Godot / Core logging 文档已说明 provider 与 appender 的组合边界
  - PR #315 最新 AI review 中仍适用的测试稳定性、dead private wrapper、boot index 与 trace heading 问题已处理
  - 最新 CodeRabbit outside-diff 复查指出的反射测试诊断不清晰问题已处理

## 已知输入

- `godot-logging-compliance-polish` 已归档，PR #314 已合并到 `origin/main`
- 归档主题确认：
  - `GFramework.Core` 仍是主日志框架
  - `GFramework.Godot.Logging` 已补齐 `GodotLog`、延迟 logger、配置发现、热重载和结构化属性渲染
  - 下一阶段应新建 topic 评估 Godot sink / appender 化，而不是继续在归档主题内扩张
- 当前分支同时承载归档收尾与本 active topic 启动，避免为纯归档维护单独开 PR

## 待办

1. 已完成：盘点 `GFramework.Core` 日志扩展点与 Godot 侧 logger/provider 的实际耦合点
2. 已完成：确认现有 Core `ILogAppender` 足够承载 Godot 输出，无需新增第二套 sink API
3. 已完成：保留 `GodotLog` / `GodotLoggerFactoryProvider` 入口，并让 `GodotLogger` 底层走 `GodotLogAppender`
4. 已完成：补充 `GodotLogAppender` targeted tests 与 `docs/zh-CN/` adoption guidance
5. 已完成：处理 PR #315 最新 review follow-up，移除默认 boot index 的 archived topics 区块并消除 trace 重复 heading
6. 已完成：处理最新 CodeRabbit outside-diff 反馈，显式断言反射目标与返回类型以改善测试失败定位
7. 待确认：是否还需要在后续阶段补一个配置化 factory 示例，把 `GodotLogAppender` 与文件 / async appender 显式组合

## 验证

- `dotnet build GFramework.sln -c Release`
  - 结果：通过，`0 warning / 0 error`
  - 备注：2026-05-03 在创建本 active topic 前已验证归档收尾分支；后续实现改动需要按受影响项目重新验证
- `dotnet test GFramework.Godot.Tests -c Release`
  - 结果：通过，`75 passed / 0 failed / 0 skipped`
  - 备注：覆盖 `GodotLogAppender` 渲染、动态 options provider、既有 Godot logging tests
- `dotnet build GFramework.Godot -c Release`
  - 结果：通过，`0 warning / 0 error`
  - 备注：验证受影响运行时项目
- `dotnet format GFramework.Godot --verify-no-changes --no-restore --include GFramework.Godot/Logging/GodotLogger.cs`
  - 结果：通过
- `dotnet format GFramework.Godot.Tests --verify-no-changes --no-restore --include GFramework.Godot.Tests/Logging/GodotLogAppenderTests.cs GFramework.Godot.Tests/Logging/GodotLoggerSettingsLoaderTests.cs`
  - 结果：通过
- `dotnet test GFramework.Godot.Tests -c Release`
  - 结果：通过，`75 passed / 0 failed / 0 skipped`
  - 备注：2026-05-03 最新 PR review outside-diff 复查后重新验证
- `dotnet format GFramework.Godot.Tests --verify-no-changes --no-restore --include GFramework.Godot.Tests/Logging/GodotLoggerSettingsLoaderTests.cs`
  - 结果：通过
  - 备注：覆盖最新改动的测试文件
- `dotnet format GFramework.sln --verify-no-changes --no-restore`
  - 结果：失败
  - 备注：失败集中在仓库既有的 whitespace、final newline 与 charset 诊断，跨 `GFramework.Core`、`GFramework.Cqrs`、`GFramework.Game.Abstractions` 等未触碰项目；本轮改动用 scoped format 验证

## 下一步

1. 提交最新 PR review follow-up
2. 等待 PR #315 复查并确认 CodeRabbit outside-diff 反馈是否关闭
3. 如继续扩展本主题，优先评估是否需要示例化 `CompositeLogger + GodotLogAppender + FileAppender`，而不是新增 API
