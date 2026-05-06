# AI-First Config System 跟踪

## 目标

基于当前 `GFramework` 设计结论，继续推进 AI-First 游戏配置系统，并把主线保持在
`C# Runtime + Source Generator + Consumer DX`。

## 当前恢复点

- 恢复点编号：`AI-FIRST-CONFIG-RP-003`
- 当前阶段：`C# Runtime + Source Generator + Consumer DX`
- 当前焦点：
  - 已完成 object-focused `if` / `then` / `else`，继续评估下一批仍不改变生成类型形状的共享关键字
  - 已明确将 `oneOf` / `anyOf` 归类为当前不支持的组合关键字，并在 Runtime / Generator / Tooling 三端显式拒绝，避免静默接受导致形状漂移
  - 已把开放对象关键字边界收紧为只接受 `additionalProperties: false`，并在 Runtime / Generator / Tooling 三端显式拒绝 `patternProperties`、`propertyNames`、`unevaluatedProperties`
  - 已完成 PR #262 的 CodeRabbit follow-up，补齐 latest review body 中 folded `Nitpick comments` 的 skill 解析并按建议收口 Tooling / Tests
  - 先以 Runtime / Generator / Tooling 三端一致语义为前提筛选下一项，而不是盲目扩全量 JSON Schema
  - Tooling / Docs 后续改为非阻塞并行 lane；active 入口只保留主线恢复点，把批处理细节下沉到 backlog 文件

### 已知风险

- 组合关键字扩展风险：下一批候选关键字可能像标准 `oneOf` / `anyOf` 一样更容易引入生成类型形状漂移
  - 缓解措施：`oneOf` / `anyOf` 已改为三端显式拒绝；后续仅继续评估不会引入联合形状、属性合并或分支生成漂移的关键字子集
- 开放对象形状风险：如果某一端静默接受 `patternProperties`、`propertyNames`、`unevaluatedProperties` 等关键字，会重新打开对象形状并造成契约漂移
  - 缓解措施：当前三端已统一把开放对象边界收紧为只接受 `additionalProperties: false`，其余开放对象关键字直接报错
- 工具链验证风险：VS Code 与 CI / 发布管道验证覆盖不足
  - 缓解措施：继续为新增共享关键字补齐三端测试覆盖，优先保证 C# Runtime 与 Generator 回归通过，并记录 JS 测试与构建验证
- PR review 信号漂移风险：CodeRabbit 可能把建议折叠在 latest review body，而不是 issue comments
  - 缓解措施：`gframework-pr-review` 现已同时解析 latest review body，并输出 declared / parsed 数量以便快速识别解析缺口
- PR follow-up 残留风险：PR `#262` 最新 review thread 仍有少量 open comments，且 nitpick body 解析仍存在 declared / parsed 缺口
  - 缓解措施：先以 latest unresolved thread 为准逐条本地核验；已确认并补齐运行时诊断路径与 `else without if` 回归测试，skill 现已补齐 `.py` nitpick 与 outside-diff comment 解析，剩余项只需等待本地修复推送后再复抓确认
- 并行 lane 漂移风险：Tooling / Docs 作为并行项后，后续 batch 可能重新把治理说明写回 active 入口或 public docs
  - 缓解措施：active tracking / trace 只保留恢复点、验证和 lane 指针；reader-facing 文档只写接入信息，治理说明继续留在 `ai-plan/**`

## 当前状态

- 已完成 Runtime、YAML Loader、Source Generator 与 VS Code Extension 的首轮可用版本
- 已落地项目级聚合注册入口、`GeneratedConfigCatalog`、`GameConfigBootstrap`、`GameConfigModule`
- 已补齐一批共享 JSON Schema 子集，包括：
  - `enum`、`const`、`not`、`pattern`
  - `format` 稳定子集：`date`、`date-time`、`duration`、`email`、`time`、`uri`、`uuid`
  - `minLength`、`maxLength`、`minItems`、`maxItems`、`contains`、`minContains`、`maxContains`、`exclusiveMinimum`、`exclusiveMaximum`、`multipleOf`、`uniqueItems`
  - `minProperties`、`maxProperties`、`dependentRequired`、`dependentSchemas`、`allOf`、object-focused `if` / `then` / `else`
- 已明确拒绝会改变生成类型形状的组合关键字：
  - `oneOf`、`anyOf` 当前会在 Runtime / Generator / Tooling 三端直接报错，而不是静默忽略
- 已明确拒绝会重新打开对象形状的开放对象关键字：
  - 当前只接受 `additionalProperties: false`
  - `patternProperties`、`propertyNames`、`unevaluatedProperties` 当前会在 Runtime / Generator / Tooling 三端直接报错，而不是静默忽略
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
- Tooling / Docs 并行 lane 仍需推进复杂表单、交互式宿主验证和后续接入文档，但这些事项不再阻塞当前恢复点

## 活跃文档

- 当前 backlog：[ai-first-config-system-csharp-experience-next.md](./ai-first-config-system-csharp-experience-next.md)
- 历史跟踪归档：[ai-first-config-system-history-through-2026-04-17.md](../archive/todos/ai-first-config-system-history-through-2026-04-17.md)
- 历史 trace 归档：[ai-first-config-system-history-through-2026-04-17.md](../archive/traces/ai-first-config-system-history-through-2026-04-17.md)

## 验证说明

- `2026-04-17` 之前的详细实现记录与定向验证命令已归档到历史 tracking / trace
- active 跟踪文件只保留当前恢复点、当前状态和下一步，不再重复堆积已完成阶段的完整历史
- 最近验证摘要：`2026-04-30` 已完成 Tooling / Docs reader-facing 收口与工具 parser 边界收紧，详细命令、批次背景与验证结果保留在 trace 的 `2026-04-30` 分阶段记录中
- 最近验证摘要：`2026-05-06` 已完成开放对象关键字边界收口；Runtime / Generator / Tooling 现统一拒绝 `patternProperties`、`propertyNames`、`unevaluatedProperties`，并保留 `additionalProperties: false` 作为唯一共享闭合对象入口；详细命令与批次背景保留在 trace 的 `2026-05-06` 记录中
- 最近验证摘要：`2026-05-06` 已按 PR `#325` latest review follow-up 移除三端开放对象校验中的不可达 `additionalProperties: false` 放行分支，补齐 Tooling 正向回归，并同步拆分 reader-facing docs 对开放对象边界的表述；细节与验证命令保留在 trace 的 `2026-05-06` 追加记录中
- PR `#306` follow-up 摘要：已按 latest open review threads 补齐 Generator `anyOf` 对称回归、Tooling schema type 白名单、object-array 直系收集边界，以及 reader-facing docs 的显式 `additionalProperties: false` / adoption guidance 说明；细节和验证命令保留在 trace 的 `2026-04-30` 新增阶段记录中
- PR review 跟进指针：当前分支的 latest review follow-up 与后续本地核验结论以 `ai-first-config-system-trace.md` 为准，active tracking 不再重复展开逐条命令历史

## 下一步

1. 主线继续回到 `YamlConfigSchemaValidator.cs`、`SchemaConfigGenerator.cs` 与 `configValidation.js` 的共享关键字盘点，默认跳过 `oneOf` / `anyOf` 以及开放对象关键字扩展
2. Tooling / Docs 若要并发推进，优先补 reader-facing 示例或采用路径，不再重复扩写能力边界说明
3. 保持 active tracking / trace 精简，只记录当前恢复点、最近验证和下一步恢复指针
