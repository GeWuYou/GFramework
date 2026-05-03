# Godot Logging Core Sink 跟踪

## 目标

在 `GFramework.Godot.Logging` 已完成宿主便利层收口后，评估并推进 Godot 输出与 `GFramework.Core` 日志扩展点的统一。
本主题优先判断是否应把 Godot 输出沉淀为 Core 可组合的 appender / sink，而不是继续扩张 Godot-only logging 管线。

## 当前恢复点

- 恢复点编号：`GODOT-LOGGING-CORE-SINK-RP-001`
- 当前阶段：`启动与边界确认`
- 当前焦点：
  - 复查 `GFramework.Core` 当前日志抽象、provider、appender / sink 能力与扩展边界
  - 对照已归档的 `godot-logging-compliance-polish` 结论，确认哪些能力应迁移为 Core 通用扩展，哪些能力应保留在 Godot 宿主层
  - 形成最小实现路径，避免同时引入第二套日志 API 或破坏现有 `GodotLog` 入口

## 已知输入

- `godot-logging-compliance-polish` 已归档，PR #314 已合并到 `origin/main`
- 归档主题确认：
  - `GFramework.Core` 仍是主日志框架
  - `GFramework.Godot.Logging` 已补齐 `GodotLog`、延迟 logger、配置发现、热重载和结构化属性渲染
  - 下一阶段应新建 topic 评估 Godot sink / appender 化，而不是继续在归档主题内扩张
- 当前分支同时承载归档收尾与本 active topic 启动，避免为纯归档维护单独开 PR

## 待办

1. 盘点 `GFramework.Core` 日志扩展点与 Godot 侧 logger/provider 的实际耦合点
2. 判断 Core appender / sink 抽象是否已足够承载 Godot 输出，还是需要先补齐抽象层能力
3. 制定兼容路径：保留 `GodotLog` 用户入口，同时让底层输出走 Core 可组合管线
4. 为选定方案补充 targeted tests 与 `docs/zh-CN/` adoption guidance

## 验证

- `dotnet build GFramework.sln -c Release`
  - 结果：通过，`0 warning / 0 error`
  - 备注：2026-05-03 在创建本 active topic 前已验证归档收尾分支；后续实现改动需要按受影响项目重新验证

## 下一步

1. 从 `GFramework.Core` 与 `GFramework.Godot.Logging` 源码开始做只读盘点
2. 在 trace 中记录候选设计和不采用的扩张路径
3. 确认实现边界后再修改代码与文档
