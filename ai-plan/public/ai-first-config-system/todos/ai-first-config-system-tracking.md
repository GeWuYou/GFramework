# AI-First Config System 跟踪

## 目标

基于当前 `GFramework` 设计结论，继续推进 AI-First 游戏配置系统，并把主线保持在
`C# Runtime + Source Generator + Consumer DX`。

## 当前恢复点

- 恢复点编号：`AI-FIRST-CONFIG-RP-003`
- 当前阶段：`C# Runtime + Source Generator + Consumer DX`
- 当前焦点：
  - 已完成 object-focused `if` / `then` / `else`，继续评估下一批仍不改变生成类型形状的共享关键字
  - 已完成 PR #262 的 CodeRabbit follow-up，补齐 latest review body 中 folded `Nitpick comments` 的 skill 解析并按建议收口 Tooling / Tests
  - 先以 Runtime / Generator / Tooling 三端一致语义为前提筛选下一项，而不是盲目扩全量 JSON Schema
  - 继续把 VS Code 工具能力视为非阻塞项，不让复杂 UI 编辑器需求反过来拖慢 C# 主线

### 已知风险

- 组合关键字扩展风险：下一批候选关键字可能像标准 `oneOf` / `anyOf` 一样更容易引入生成类型形状漂移
  - 缓解措施：延续 object-focused / focused matcher 约束，只接受三端都能稳定解释且不需要属性合并的子集
- 工具链验证风险：VS Code 与 CI / 发布管道验证覆盖不足
  - 缓解措施：继续为新增共享关键字补齐三端测试覆盖，优先保证 C# Runtime 与 Generator 回归通过，并记录 JS 测试与构建验证
- PR review 信号漂移风险：CodeRabbit 可能把建议折叠在 latest review body，而不是 issue comments
  - 缓解措施：`gframework-pr-review` 现已同时解析 latest review body，并输出 declared / parsed 数量以便快速识别解析缺口
- PR follow-up 残留风险：PR `#262` 最新 review thread 仍有少量 open comments，且 nitpick body 解析仍存在 declared / parsed 缺口
  - 缓解措施：先以 latest unresolved thread 为准逐条本地核验；已确认并补齐运行时诊断路径与 `else without if` 回归测试，skill 现已补齐 `.py` nitpick 与 outside-diff comment 解析，剩余项只需等待本地修复推送后再复抓确认
- 非阻塞项回退风险：将 VS Code 功能标为非阻塞但导致主线回退的风险
  - 缓解措施：C# 主线补齐新关键字时仍需在 `configValidation.js` 与 `extension.js` 中同步落地，只是不让复杂表单控件阻塞发布

## 当前状态

- 已完成 Runtime、YAML Loader、Source Generator 与 VS Code Extension 的首轮可用版本
- 已落地项目级聚合注册入口、`GeneratedConfigCatalog`、`GameConfigBootstrap`、`GameConfigModule`
- 已补齐一批共享 JSON Schema 子集，包括：
  - `enum`、`const`、`not`、`pattern`
  - `format` 稳定子集：`date`、`date-time`、`duration`、`email`、`time`、`uri`、`uuid`
  - `minItems`、`maxItems`、`exclusiveMinimum`、`exclusiveMaximum`、`multipleOf`、`uniqueItems`
  - `minProperties`、`maxProperties`、`dependentRequired`、`dependentSchemas`、`allOf`、object-focused `if` / `then` / `else`
- `if` / `then` / `else` 已按“不改变生成类型形状”的边界落地：
  - 只允许 object 节点上的 object-typed inline schema
  - `if` 必填，且必须至少伴随 `then` 或 `else` 之一
  - 分支只能引用父对象已声明字段，不做属性合并
  - 条件匹配沿用 `dependentSchemas` / `allOf` 的 focused matcher 语义
- 相关实现与验证入口：
  - Runtime：`GFramework.Game/Config/YamlConfigSchemaValidator.cs`、`GFramework.Game/Config/YamlConfigSchemaValidator.ObjectKeywords.cs`
  - Generator：`GFramework.Game.SourceGenerators/Config/SchemaConfigGenerator.cs`
  - Tooling：`tools/gframework-config-tool/src/configValidation.js`、`tools/gframework-config-tool/src/extension.js`
  - Tests：`GFramework.Game.Tests/Config/YamlConfigLoaderIfThenElseTests.cs`、`GFramework.SourceGenerators.Tests/Config/SchemaConfigGeneratorTests.cs`、`tools/gframework-config-tool/test/configValidation.test.js`
- PR review follow-up 收口：
  - `gframework-pr-review` 现已解析 latest CodeRabbit review body 中 folded `Nitpick comments`
  - text 输出会显示 `CodeRabbit nitpick comments: X declared, Y parsed`，避免再次静默遗漏
  - 已按 5 条 nitpick 更新 VS Code tool hints、shared validation helper，以及对称分支测试覆盖
- PR `#262` 最新 follow-up：
  - 最新抓取结果显示 latest review body 里有 2 条 nitpick 与 1 条 outside-diff actionable comment
  - `SchemaConfigGenerator` 的分支级诊断定位已在当前分支，无需重复修改
  - `YamlConfigSchemaValidator` 已补齐 `conditionalSchemaPath` 诊断路径，避免 `reward[then]` / `reward[else]` 坏形状误报到父路径
  - `YamlConfigLoaderIfThenElseTests` 已新增运行时 `else` 缺失 `if` 回归，避免 Runtime / Generator 覆盖漂移
  - active trace 已将重复的 `### 验证` 标题改为专用 PR follow-up 标题，消除 `MD024`
  - `gframework-pr-review` 现已在 latest review body 中同时解析 `Outside diff range comments` 与 `Nitpick comments`
  - `parse_comment_cards` 已不再遗漏 `.codex/.../*.py` 这类 skill 文件评论卡片
  - `tools/gframework-config-tool/src/configValidation.js` 已按 outside-diff 建议收紧条件分支坏形状拒绝规则，并补齐 JS 回归测试
- 分支同步状态：
  - `feat/ai-first-config` 已 rebase 到 `origin/feat/ai-first-config`
  - 当前已解决“ahead / behind 同时存在”的分支差异，不再 behind 远端
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
- `2026-04-20` 当前恢复点验证：
  - `python3 .codex/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --pr 262 --format json`：通过（`CodeRabbit outside-diff comments: 1 declared, 1 parsed`，`CodeRabbit nitpick comments: 2 declared, 2 parsed`）
  - `bun run test`（`tools/gframework-config-tool`）：通过（122 tests；包含条件分支坏形状回归）
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~SchemaConfigGeneratorTests"`：通过
  - `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~YamlConfigLoaderIfThenElseTests"`：通过（8 tests；新增 `else without if` 运行时回归）
  - `dotnet build GFramework.sln -c Release`：通过（存在仓库既有 analyzer warning，无新增错误）

## 下一步

1. 提交并推送当前 PR `#262` follow-up 修复后，重新抓取一次 PR review，确认 outside-diff comment 与 open thread 是否都已收口
2. 若 PR review 已收口，再回到 `GFramework.Game/Config/YamlConfigSchemaValidator.cs`、`GFramework.Game.SourceGenerators/Config/SchemaConfigGenerator.cs`、`tools/gframework-config-tool/src/configValidation.js` 盘点下一批候选关键字
3. 优先判断 `oneOf` / `anyOf` 是否存在可接受的 object-focused 子集；若仍会引入生成类型形状漂移，就直接跳过
