# Analyzer Warning Reduction 追踪

## 2026-04-24 — RP-050

### 阶段：clean-build 基线修正与 `GFramework.Godot.SourceGenerators` 切片清零

- 触发背景：
  - 用户确认之前的 `0 Warning(s)` 来自增量构建假阴性；只有先 `dotnet clean` 再 `dotnet build`，warning 才会重新出现
  - 用户给出 clean solution build 的真实结果：`Build succeeded with 1193 warning(s)`
- 主线程实施：
  - 纠正当前 topic 的 active todo / trace，把 clean build 作为新的 warning 检查真值
  - 在 `BindNodeSignalGenerator.cs`、`GetNodeGenerator.cs`、`GodotProjectMetadataGenerator.cs` 中完成分阶段方法抽取与字符串比较修正
  - 在 `Registration/AutoRegisterExportedCollectionsGenerator.cs` 中拆分 `TryCreateRegistration`，清除最后一个 `MA0051`
  - 更新 `AGENTS.md`，明确 warning 检查必须先 `dotnet clean` 再 `dotnet build`
- 验证里程碑：
  - `dotnet clean GFramework.Godot.SourceGenerators/GFramework.Godot.SourceGenerators.csproj -c Release`
    - 结果：成功；`0 Warning(s)`、`0 Error(s)`
  - `dotnet build GFramework.Godot.SourceGenerators/GFramework.Godot.SourceGenerators.csproj -c Release`
    - 首次验证：成功；`1 Warning(s)`，剩余 `Registration/AutoRegisterExportedCollectionsGenerator.cs(182,25)` `MA0051`
    - 修复后复验：成功；`0 Warning(s)`、`0 Error(s)`
- 当前结论：
  - `GFramework.Godot.SourceGenerators` 已在 clean `Release` build 下从 9 个 warning 降到 0 个 warning
  - 整仓库 warning 基线仍以用户确认的 clean solution build `1193 warning(s)` 为准
  - 下一轮应继续从 clean solution build 输出中选择新的低风险热点

## Archive Context

- 当前轮次归档：
  - [analyzer-warning-reduction-history-rp042-rp048.md](../archive/todos/analyzer-warning-reduction-history-rp042-rp048.md)
  - [analyzer-warning-reduction-history-rp042-rp048.md](../archive/traces/analyzer-warning-reduction-history-rp042-rp048.md)
- 历史跟踪归档：
  - [analyzer-warning-reduction-history-rp001.md](../archive/todos/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/todos/analyzer-warning-reduction-history-rp002-rp041.md)
- 历史 trace 归档：
  - [analyzer-warning-reduction-history-rp001.md](../archive/traces/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/traces/analyzer-warning-reduction-history-rp002-rp041.md)
