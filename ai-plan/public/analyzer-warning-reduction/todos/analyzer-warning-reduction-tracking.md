# Analyzer Warning Reduction 跟踪

## 目标

继续以“优先低风险、保持行为兼容”为原则收敛当前仓库的 Meziantou analyzer warnings，并确保 active recovery 入口保持精简、可恢复。

## 当前恢复点

- 恢复点编号：`ANALYZER-WARNING-REDUCTION-RP-046`
- 当前阶段：`Phase 46`
- 当前焦点：
  - 已按用户更正后的要求执行前台 `dotnet build GFramework.sln -c Release`，收集当前工作树的 solution 级 warning 基线，并将结果回写 active plan
  - 当前前台 solution Release build 已能稳定完成，结果为 `891 Warning(s)` / `0 Error(s)` / `Time Elapsed 00:00:18.57`
  - 当前 baseline 仍为 `origin/main` (`e692ed3`, `2026-04-24 09:36:17 +0800`)；分支仍映射到 `fix/analyzer-warning-reduction-batch` / `GFramework-analyzer`
  - 当前直接观察到的热点 warning 规则集中在 `MA0051`、`MA0158`、`MA0004`，并伴随一部分 `MA0006`、`MA0002`、`MA0009`
  - 当前直接观察到的热点模块集中在 `GFramework.Godot.SourceGenerators`、`GFramework.Godot.SourceGenerators.Tests`、`GFramework.Core`、`GFramework.Game`、`GFramework.Cqrs`、`GFramework.Godot`
  - 非前台形态的同命令当前不稳定：重定向到文件、`script` 分配 TTY、以及若干 logger 组合都曾快速返回 `Build FAILED / 0 Warning(s) / 0 Error(s)` 或 `Restore failed`

## 当前状态摘要

- 已通过 explorer `Rawls` 盘点出本轮最适合并行推进的低风险切片：
  - `GFramework.Game/Data/UnifiedSettingsFile.cs` 的 `MA0016`
  - `GFramework.Godot/Setting/Data/LocalizationMap.cs` 的 `MA0016`
  - `GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs` 的 `MA0051`
- 已通过 explorer `Arendt` 收敛过旧构建根因：当前离线缓存只有 `Meziantou.Polyfill 1.0.110`，缺少项目请求的 `1.0.116`，导致 restore 失败并使 source-generator 相关 `netstandard2.0` 项目缺失有效引用图
- worker `Aquinas` 已独立完成 `LocalizationMap` 收口并生成提交 `a0ce04b`（`fix(godot): 收紧本地化映射集合暴露`）
- worker `Boyle` 已完成 `UnifiedSettingsFile` / `UnifiedSettingsDataRepository` 的最小 API 形状修正，并同步更新 active todo / trace 草稿
- 主线程本轮重新执行前台 `dotnet build GFramework.sln -c Release`，构建成功并暴露出完整 warning 面，说明当前工作树至少在交互式前台构建路径上已经恢复到可收集 analyzer baseline 的状态
- 从实时输出可直接确认的热点包括：
  - `GFramework.Godot.SourceGenerators/GodotProjectMetadataGenerator.cs`、`BindNodeSignalGenerator.cs`、`GetNodeGenerator.cs`、`Registration/AutoRegisterExportedCollectionsGenerator.cs`
  - `GFramework.Godot.SourceGenerators.Tests/**` 多个 generator 测试文件
  - `GFramework.Cqrs/Internal/WeakKeyCache.cs`
  - `GFramework.Core/**` 多个锁相关组件，如 `SamplingFilter.cs`、`BindableProperty.cs`、`FileAppender.cs`、`ResourceHandle.cs`
  - `GFramework.Game/Config/YamlConfigSchemaValidator*.cs`、`GFramework.Game/UI/UiRouterBase.cs`、`GFramework.Game/Storage/FileStorage.cs`
- 同一轮中，多次尝试把相同命令切换到“重定向日志”“TTY 包裹”或“附加 logger 输出到文件”的形态时，构建又会退化成 `Build FAILED / 0 Warning(s) / 0 Error(s)` 或 `Restore failed`
- 这说明当前环境不只是“能否构建”的问题，还存在“前台构建”和“非前台采集”结果不一致的执行形态漂移

## 当前活跃事实

- 当前主题仍保持 active，因为 analyzer warning reduction 主任务尚未结束，而且本轮新增了可直接消费的 solution warning baseline
- `CqrsHandlerRegistryGeneratorTests.cs` 当前已不再保留方法内 `const string source = """..."""` 型大型 fixture；后续只需回到真实 warning 热点继续收敛
- `UnifiedSettingsFile.Sections` 改为 `IDictionary<string, string>` 后，`CloneFile` 仍会在底层是 `Dictionary` 时保留 comparer，从而避免改变现有 key 比较行为
- `LocalizationMap` 现在通过私有只读字典与 `IReadOnlyDictionary<string, string>` 暴露映射，消费者 `GodotLocalizationSettings` 仍只按只读方式使用这些映射
- 当前 worktree 的推荐构建入口仍是 `bash scripts/dotnet-wsl.sh <dotnet-command> ...`
- 当前前台 `dotnet build GFramework.sln -c Release` 的结果是 `891` 条 warning、`0` 条 error，且已确认不是“0 warning 的假成功”
- 当前 warning 规则的已观察集合包括 `MA0051`、`MA0158`、`MA0004`、`MA0006`、`MA0002`、`MA0009`
- 当前“非前台采集”路径仍不可信：把输出重定向到文件、追加 file logger 或经 `script` 分配 TTY 时，都未稳定复现前台结果
- 先前已识别的 `--no-restore` 资产文件漂移、`Meziantou.Polyfill 1.0.116` 缺失、以及 `NU1301` 风险仍保留，但本轮用户要求的 warning 基线以成功的前台 `dotnet build` 结果为准

## 当前风险

- warning 采集形态漂移风险：同一个 `dotnet build GFramework.sln -c Release` 在前台可成功输出 `891` warnings，但一旦切到日志重定向、TTY 包裹或特定 logger 组合，就可能在约 `1` 秒内快速失败
  - 缓解措施：短期内把“前台普通构建”作为 warning 基线真值来源；若需要自动化统计，再单独排查 stdout/TTY/logger 相关环境差异
- SDK / workload resolver 环境漂移风险：历史诊断样本里 Linux .NET SDK `10.0.106` 在 solution restore 图阶段报告过 `Microsoft.NET.SDK.WorkloadAutoImportPropsLocator` 缺失
  - 缓解措施：后续若要恢复脚本化或 `--no-restore` 验证，再重新检查当前 WSL `dotnet --info`、workload 解析器状态与仓库推荐构建入口是否一致
- 资产文件环境漂移风险：历史样本中的根项目 `project.assets.json` 明显来自 Windows restore，上游记录的 fallback package folder 在当前 WSL 会话不存在
  - 缓解措施：需要重新启用 `--no-restore` 验证时，先用与 WSL 兼容的 NuGet / restore 配置重建根项目资产文件
- 构建环境可达性风险：`Meziantou.Polyfill 1.0.116` 缺失与 `NU1301` 在旧样本中仍是有效线索
  - 缓解措施：后续若再次命中 restore 阻塞，优先核查本地缓存版本与 NuGet 可达性
- reviewability 风险：当前分支只提交了 `LocalizationMap` 一个 warning 切片，而工作树仍保留 `ai-plan` 等未提交变更
  - 缓解措施：后续 warning reduction 继续按模块或规则切片提交，不把多个热点混成单个大提交

## 活跃文档

- 历史跟踪归档：
  - [analyzer-warning-reduction-history-rp001.md](../archive/todos/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/todos/analyzer-warning-reduction-history-rp002-rp041.md)
- 历史 trace 归档：
  - [analyzer-warning-reduction-history-rp001.md](../archive/traces/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/traces/analyzer-warning-reduction-history-rp002-rp041.md)

## 验证说明

- `dotnet build GFramework.sln -c Release`
  - 结果：成功；`891 Warning(s)`、`0 Error(s)`、`Time Elapsed 00:00:18.57`
  - 补充：当前 warning 流可直接从前台交互式构建观察到；热点规则以 `MA0051`、`MA0158`、`MA0004` 为主
- `dotnet build GFramework.sln -c Release > /tmp/gframework-build-warnings.log 2>&1`
  - 结果：失败；约 `0.78s` 结束，摘要仅显示 `Build FAILED / 0 Warning(s) / 0 Error(s)`
- `dotnet build GFramework.sln -c Release '/flp:logfile=/tmp/gframework-build-warnings.log;verbosity=normal'`
  - 结果：成功；但日志文件只保留构建摘要，没有留下可消费的 warning 行
- `dotnet build GFramework.sln -c Release '/flp1:logfile=/tmp/gframework-build-warnings-only.log;warningsonly'`
  - 结果：成功；但 warning-only 日志文件为空
- `script -q -c "dotnet build GFramework.sln -c Release" /tmp/gframework-build-full-typescript.log`
  - 结果：失败；TTY 形态下 restore 于约 `0.8s` 退出
- `git --git-dir=/mnt/f/gewuyou/System/Documents/WorkSpace/GameDev/GFramework/.git/worktrees/GFramework-analyzer --work-tree=/mnt/f/gewuyou/System/Documents/WorkSpace/GameDev/GFramework-WorkTree/GFramework-analyzer branch --show-current`
  - 结果：成功；当前分支为 `fix/analyzer-warning-reduction-batch`

## 下一步建议

1. 先以本轮前台 `dotnet build GFramework.sln -c Release` 的 `891` warning baseline 为起点，优先从 `MA0051` / `MA0158` 最密集的模块切分下一批低风险整改
2. 并行保留环境核查：检查为什么相同命令在重定向日志、TTY 包裹或 logger 组合下会快速失败，避免后续 warning 统计自动化再次失真
3. 在需要重新启用 `--no-restore` 或脚本化验证时，再回到根 `GFramework.csproj` 资产文件漂移、SDK workload resolver、以及 `Meziantou.Polyfill 1.0.116` / `NU1301` 这几条环境线索
