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

### 验证

- `dotnet build GFramework.sln -c Release`
  - 结果：通过，`0 warning / 0 error`
  - 备注：本次 build 在创建 active topic 前执行，用于验证归档维护对解决方案无影响；实现阶段需要重新跑受影响项目验证

### 下一步

1. 只读盘点 Core logging 抽象与 Godot logger/provider 的耦合点
2. 记录候选设计，明确哪些能力进入 Core，哪些保留在 Godot 宿主层
3. 确认方案后进入实现与文档更新
