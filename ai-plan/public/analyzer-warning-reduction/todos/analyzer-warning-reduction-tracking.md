# Analyzer Warning Reduction 跟踪

## 目标

继续以“低风险、可审查、可恢复”为原则收敛 analyzer warning，并保持 active recovery 文档只保留当前真值。

## 当前恢复点

- 恢复点编号：`ANALYZER-WARNING-REDUCTION-RP-048`
- 当前阶段：`Phase 48`
- 当前焦点：
  - 当前 baseline 为 `origin/main`（`a8447a6`, `2026-04-24 12:53:39 +0800`），batch stop condition 仍为 branch diff 接近 `75` 个文件
  - 当前 branch diff 为 `6` 个文件、`1566` 行（相对 `origin/main...HEAD`），距离 stop condition 仍有空间
  - 当前 warning-reduction 代码切片已经提交到 `77e332f`（`fix(analyzer): 收口当前批次警告切片`）
  - 当前工作树除未跟踪的 `.codex` 目录外无活动代码修改

## 当前活跃事实

- `UnifiedSettingsFile.Sections` 已抽象为 `IDictionary<string, string>`；`UnifiedSettingsDataRepository.CloneFile` 会在底层仍是 `Dictionary` 时保留 comparer，避免改变既有键比较语义
- `LocalizationMap` 通过私有 `Dictionary` 字段配合 `IReadOnlyDictionary` 暴露映射，继续避免把可变集合直接暴露给调用方
- `CqrsHandlerRegistryGeneratorTests.cs` 已把一批大型 fixture 提升到类级常量，以更低噪音方式消化 `MA0051`
- 通过仓库根目录直接执行 `dotnet build` 已再次确认当前 solution 默认构建成功
- 本地当前没有新的低风险 warning hotspot；继续扩展 batch 会先增加 branch 体积，而不是继续降低 warning

## 当前风险

- active todo / trace 在 RP-047 之后一度滞后于仓库真值，曾错误保留“工作树仍有未提交切片”的描述
  - 缓解措施：已在 RP-048 回写提交状态、默认 build 验证结果与最新 baseline
- 当前 solution warning 已为 `0`，若继续按 warning-reduction 名义扩展分支，reviewability 收益会快速下降
  - 缓解措施：将本轮 batch 视为自然停点；仅在出现新的 warning hotspot 或明确新切片目标后再继续

## 活跃文档

- 历史跟踪归档：
  - [analyzer-warning-reduction-history-rp001.md](../archive/todos/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/todos/analyzer-warning-reduction-history-rp002-rp041.md)
- 历史 trace 归档：
  - [analyzer-warning-reduction-history-rp001.md](../archive/traces/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/traces/analyzer-warning-reduction-history-rp002-rp041.md)

## 验证说明

- `dotnet build`
  - 结果：成功；solution 默认构建通过，`Build succeeded in 16.2s`

## 下一步建议

1. 将当前 warning-reduction batch 视为已到自然停点，不再为了“凑批次”继续扩大 branch diff
2. 若后续仍要继续 warning work，先重新定位新的 warning hotspot 或回归来源，再开启下一轮批处理
3. 继续下一轮前优先保持与 `origin/main` 的基线同步，避免在已清零 warning 的前提下无收益扩分支
