# AI-First Config System 跟踪

## 目标

基于当前 `GFramework` 设计结论，继续推进 AI-First 游戏配置系统，并把主线保持在
`C# Runtime + Source Generator + Consumer DX`。

## 当前恢复点

- 恢复点编号：`AI-FIRST-CONFIG-RP-002`
- 当前阶段：`C# Runtime + Source Generator + Consumer DX`
- 当前焦点：
  - 在当前稳定 `format` 子集与 object-focused `allOf` 之后，继续评估仍不改变生成类型形状的下一批组合关键字
  - 优先考察 `if` / `then` / `else` 是否能在 Runtime / Generator / Tooling 三端保持一致语义
  - 继续把 VS Code 工具能力视为非阻塞项，不让复杂 UI 编辑器需求反过来拖慢 C# 主线

### 已知风险

- 语义一致性风险：`if` / `then` / `else` 在 Runtime / Generator / Tooling 三端语义不一致的风险
  - 缓解措施：先验证是否能在不引入生成类型形状漂移的前提下落地，若否则选择下一批共享解释关键字
- 工具链验证风险：VS Code 与 CI / 发布管道验证覆盖不足
  - 缓解措施：继续为新增共享关键字补齐三端测试覆盖，优先保证 C# Runtime 与 Generator 回归通过
- 非阻塞项回退风险：将 VS Code 功能标为非阻塞但导致主线回退的风险
  - 缓解措施：C# 主线补齐新关键字时仍需在 `configValidation.js` 与 `extension.js` 中同步落地，只是不让复杂表单控件阻塞发布

## 当前状态

- 已完成 Runtime、YAML Loader、Source Generator 与 VS Code Extension 的首轮可用版本
- 已落地项目级聚合注册入口、`GeneratedConfigCatalog`、`GameConfigBootstrap`、`GameConfigModule`
- 已补齐一批共享 JSON Schema 子集，包括：
  - `enum`、`const`、`not`、`pattern`
  - `format` 稳定子集：`date`、`date-time`、`duration`、`email`、`time`、`uri`、`uuid`
  - `minItems`、`maxItems`、`exclusiveMinimum`、`exclusiveMaximum`、`multipleOf`、`uniqueItems`
  - `minProperties`、`maxProperties`、`dependentRequired`、`dependentSchemas`、`allOf`
- 当前最细粒度的下一阶段 backlog 保留在独立文件：
  - `ai-plan/public/ai-first-config-system/todos/ai-first-config-system-csharp-experience-next.md`

## 当前未完成项

- 继续扩展“不会改变生成类型形状”的共享关键字支持
- 继续降低复杂 schema 与多配置域项目的接入成本
- 让 VS Code 表单支持更深层对象数组嵌套，减少 raw YAML 回退
- 为复杂结构提供比“顶层标量 / 标量数组”更强的批量编辑能力
- 在真实 VS Code 宿主中完成对象数组编辑与复杂 schema 的交互式手工验证

## 活跃文档

- 当前 backlog：[ai-first-config-system-csharp-experience-next.md](./ai-first-config-system-csharp-experience-next.md)
- 历史跟踪归档：[ai-first-config-system-history-through-2026-04-17.md](../archive/todos/ai-first-config-system-history-through-2026-04-17.md)
- 历史 trace 归档：[ai-first-config-system-history-through-2026-04-17.md](../archive/traces/ai-first-config-system-history-through-2026-04-17.md)

## 验证说明

- `2026-04-17` 之前的详细实现记录与定向验证命令已归档到历史 tracking / trace
- active 跟踪文件只保留当前恢复点、当前状态和下一步，不再重复堆积已完成阶段的完整历史

## 下一步

1. 先检查 `GFramework.Game/Config/YamlConfigSchemaValidator.cs`、`GFramework.SourceGenerators/Config/SchemaConfigGenerator.cs`、`tools/gframework-config-tool/src/configValidation.js`
2. 评估 `if` / `then` / `else` 是否能在不引入生成类型形状漂移的前提下落地
3. 若结论是否定，再选择下一批仍能共享解释的关键字，而不是先回到工具 UI 深挖