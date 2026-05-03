# Godot Logging Core Sink Trace

## 2026-05-03

### RP-001 启动

- 新建 active topic：`godot-logging-core-sink`
- 当前分支：`feat/godot-logging-core-sink`
- 启动背景：
  - `godot-logging-compliance-polish` 已随 PR #314 合并并归档
  - 用户明确要求归档收尾不要作为独立分支推进，而是跟下一 active topic 一起提交
  - 本分支因此同时包含归档索引收口和新 topic 启动入口

### 初始边界

- 本主题要评估 Godot 输出是否应进入 Core appender / sink 模型
- 不把 `Microsoft.Extensions.Logging` 生态原样搬入 GFramework
- 不新增第二套业务日志 API；`GodotLog` 应保持为 Godot 宿主便利入口
- 不在已归档的 `godot-logging-compliance-polish` topic 中继续扩张新需求

### RP-001 验证

- `dotnet build GFramework.sln -c Release`
  - 结果：通过，`0 warning / 0 error`
  - 备注：本次 build 在创建 active topic 前执行，用于验证归档维护对解决方案无影响；实现阶段需要重新跑受影响项目验证

### RP-001 下一步

1. 只读盘点 Core logging 抽象与 Godot logger/provider 的耦合点
2. 记录候选设计，明确哪些能力进入 Core，哪些保留在 Godot 宿主层
3. 确认方案后进入实现与文档更新

### RP-002 Godot Appender 最小实现

- 盘点结论：
  - Core 已有 `ILogAppender`、`LogEntry`、`CompositeLogger`、filter、formatter 与 async appender
  - Godot 侧主要耦合点是 `GodotLogger` 直接持有模板渲染和 `GD.*` 输出逻辑
  - 不需要先新增 Core sink 抽象；把 Godot 输出沉淀为 Godot 包内的 `ILogAppender` 已能复用 Core 管线
- 已实施：
  - 新增 `GFramework.Godot.Logging.GodotLogAppender`
  - `GodotLogger` 保留原有 public API，并把输出委托给 `GodotLogAppender`
  - 新增 `GFramework.Godot.Tests/Logging/GodotLogAppenderTests.cs`
  - 更新 `GFramework.Godot/README.md`、`docs/zh-CN/core/logging.md`、`docs/zh-CN/godot/index.md`、
    `docs/zh-CN/godot/logging.md`
- 采用的兼容边界：
  - `GodotLog`、`GodotLoggerFactory`、`GodotLoggerFactoryProvider` 不改用户调用方式
  - Godot 输出可作为 Core appender 被自定义 factory / `CompositeLogger` 组合
  - 文件、JSON、namespace filter、async 等仍由 Core logging 组件负责，Godot 包只提供宿主控制台落点

### RP-002 验证

- `dotnet test GFramework.Godot.Tests -c Release`
  - 结果：通过，`75 passed / 0 failed / 0 skipped`
- `dotnet build GFramework.Godot -c Release`
  - 结果：通过，`0 warning / 0 error`

### RP-002 下一步

1. 提交当前 appender 实现与文档更新
2. 若继续推进本主题，优先补充组合示例或归档 topic，不新增第二套日志 API

### RP-003 PR Review Follow-up

- 使用 `$gframework-pr-review` 抓取 PR #315 最新 review payload：
  - CodeRabbit：3 个 open thread，分别指向 appender test 顺序依赖、默认 boot index 包含 archived topic、trace 重复 heading
  - Greptile：1 个 open thread，指出 `GodotLogger.FormatProperties` 为 dead private wrapper
  - Gemini Code Assist：无 open thread
  - GitHub Test Reporter：`2264 passed / 0 failed`
  - MegaLinter：`dotnet-format` 报 restore failure；本地进一步验证时发现 solution-wide format 还有既有 repo-wide 诊断
- 已实施：
  - `GodotLogAppenderTests` 改为验证固定前缀与结构化属性集合内容，不再依赖 `Dictionary` 枚举顺序
  - 移除 `GodotLogger.FormatProperties` private wrapper，并把既有结构化属性测试改为验证生产路径使用的 `ToPropertiesDictionary` 与 `GodotLogAppender.FormatProperties`
  - 从 `ai-plan/public/README.md` 移除 archived topics 区块，默认 boot index 只保留 active topic 与 worktree map
  - 将 trace 中重复的 `### 验证` / `### 下一步` 改为 `RP-001` 与 `RP-002` 前缀，避免 MD024 anchor 冲突

### RP-003 验证

- `dotnet test GFramework.Godot.Tests -c Release`
  - 结果：通过，`75 passed / 0 failed / 0 skipped`
- `dotnet build GFramework.Godot -c Release`
  - 结果：通过，`0 warning / 0 error`
- `dotnet format GFramework.Godot --verify-no-changes --no-restore --include GFramework.Godot/Logging/GodotLogger.cs`
  - 结果：通过
- `dotnet format GFramework.Godot.Tests --verify-no-changes --no-restore --include GFramework.Godot.Tests/Logging/GodotLogAppenderTests.cs GFramework.Godot.Tests/Logging/GodotLoggerSettingsLoaderTests.cs`
  - 结果：通过
- `dotnet format GFramework.sln --verify-no-changes --no-restore`
  - 结果：失败
  - 备注：失败为仓库既有的跨项目 whitespace、final newline 与 charset 诊断；本轮改动文件已通过 scoped format 验证

### RP-003 下一步

1. 提交 PR review follow-up
2. 等待 PR #315 复查，确认 CodeRabbit / Greptile open threads 是否关闭

### RP-004 PR Review Outside-Diff 复查

- 使用 `$gframework-pr-review` 重新抓取 PR #315 最新 review payload：
  - CodeRabbit：无 open thread，但 latest review body 仍有 1 条 outside-diff 反馈
  - Greptile：无 open thread
  - Gemini Code Assist：无 open thread
  - GitHub Test Reporter：最新 run 显示 `2264 passed / 0 failed`
  - MegaLinter：仍为 `dotnet-format` restore failure，未提供具体源文件格式诊断
- 已实施：
  - `GodotLoggerSettingsLoaderTests.StructuredProperties_Should_Skip_Blank_Keys_And_Trim_Valid_Keys` 在调用反射目标前显式断言 `ToPropertiesDictionary` 存在
  - 同一测试在交给 `GodotLogAppender.FormatProperties` 前显式断言反射返回值类型符合预期

### RP-004 验证

- `dotnet test GFramework.Godot.Tests -c Release`
  - 结果：通过，`75 passed / 0 failed / 0 skipped`
- `dotnet format GFramework.Godot.Tests --verify-no-changes --no-restore --include GFramework.Godot.Tests/Logging/GodotLoggerSettingsLoaderTests.cs`
  - 结果：通过

### RP-004 下一步

1. 提交最新 PR review follow-up
2. 等待 PR #315 复查，确认 CodeRabbit outside-diff 反馈是否关闭

### RP-005 组合示例与主题归档

- PR #315 已合并，当前短分支从 `origin/main` 创建，用于收口最后的文档示例与 active topic 归档
- 复核结论：
  - `ai-libs/GodotLogger` 的宿主便利层能力已由 `godot-logging-compliance-polish` 吸收并归档
  - 本主题已把 Godot 输出落到 Core `ILogAppender`，无需新增第二套 sink API
  - 仅剩有价值的补强是示例化 Core appender 组合，而不是继续扩展 Logger 框架代码
- 已实施：
  - 在 `docs/zh-CN/core/logging.md` 补充 `GodotLogAppender + AsyncLogAppender + FileAppender` 组合 provider 示例
  - 在 `docs/zh-CN/godot/logging.md` 补充 Godot 宿主下的组合接入路径，并说明 `user://` 路径需先转换为文件系统路径
  - 将 `godot-logging-core-sink` 移入 `ai-plan/public/archive/`
  - 从 `ai-plan/public/README.md` 移除本主题 active topic 与 `feat/godot-logging-core-sink` 分支映射

### RP-005 验证

- 首次并行运行 `dotnet build GFramework.Godot -c Release` 与 `dotnet test GFramework.Godot.Tests -c Release` 时，
  build 命中 MSBuild 输出文件锁竞争；随后改为串行重跑同样的 direct 命令作为最终结果
- `dotnet build GFramework.Godot -c Release`
  - 结果：通过，`0 warning / 0 error`
- `dotnet test GFramework.Godot.Tests -c Release`
  - 结果：通过，`75 passed / 0 failed / 0 skipped`

### RP-005 下一步

1. 提交短分支
