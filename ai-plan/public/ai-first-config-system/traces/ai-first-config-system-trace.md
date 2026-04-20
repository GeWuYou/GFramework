# AI-First Config System 执行 Trace

## 2026-04-19

### 阶段：active 入口归档收口（AI-FIRST-CONFIG-RP-002）

- 已将截至 `2026-04-17` 的详细实现历史从默认 trace 入口移到主题内归档
- active trace 现在只保留当前恢复点和下一步，避免 `boot` 每次恢复都重新读取已完成的长历史
- 当前功能主线不变，仍是：
  - `C# Runtime + Source Generator + Consumer DX`
  - 下一批共享 JSON Schema 关键字评估
  - 优先看 `if` / `then` / `else`

### Archive Context

- 历史跟踪归档：
  - `ai-plan/public/ai-first-config-system/archive/todos/ai-first-config-system-history-through-2026-04-17.md`
- 历史 trace 归档：
  - `ai-plan/public/ai-first-config-system/archive/traces/ai-first-config-system-history-through-2026-04-17.md`

### 验证

- 2026-04-19：入口归档收口验证
  - 执行命令：`wc -l ai-plan/public/ai-first-config-system/todos/ai-first-config-system-tracking.md ai-plan/public/ai-first-config-system/traces/ai-first-config-system-trace.md`
  - 结果：通过
  - 备注：active 入口文件行数显著减少，已完成阶段详细历史已移至归档
- 2026-04-17 之前：详细实现与定向验证命令
  - 参考：`ai-plan/public/ai-first-config-system/archive/todos/ai-first-config-system-history-through-2026-04-17.md`
  - 备注：包含 Runtime / Generator / Tooling 三端同步落地的每日验证记录与具体测试命令

### 下一步

1. 从 `ai-first-config-system-csharp-experience-next.md` 读取当前 backlog，而不是继续翻已完成历史
2. 先判断 `if` / `then` / `else` 是否满足“三端一致且不改变生成形状”的前提
3. 若不满足，直接回退到下一批收益更明确的共享关键字评估

## 2026-04-20

### 阶段：object-focused `if` / `then` / `else` 收口（AI-FIRST-CONFIG-RP-003）

- 已在 Runtime、Source Generator 与 VS Code Tooling 三端落地 object-focused `if` / `then` / `else`
- 本轮采用的约束边界：
  - 仅允许 object 节点上的 object-typed inline schema
  - `if` 必填，且必须至少存在 `then` 或 `else` 之一
  - `then` / `else` 只能约束父对象已声明字段，不做属性合并
  - 条件匹配沿用 `dependentSchemas` / `allOf` 的 focused matcher 语义，允许未在条件块中声明的额外同级字段继续存在
- 生成器新增 `GF_ConfigSchema_013`，在生成阶段提前拒绝坏形状的条件元数据，并把条件摘要写入 XML 文档
- VS Code 工具同步补齐 schema 解析、校验消息、本地化文本与表单 hint 元数据显示

### 验证

- 2026-04-20：`bun run test`（`tools/gframework-config-tool`）
  - 结果：通过
- 2026-04-20：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~SchemaConfigGeneratorTests"`
  - 结果：通过
- 2026-04-20：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~YamlConfigLoaderIfThenElseTests"`
  - 结果：通过
  - 备注：修正断言路径后，运行时诊断显示路径与 `reward[if]` / `reward[then]` 的约定保持一致
- 2026-04-20：`dotnet build GFramework.sln -c Release`
  - 结果：通过
  - 备注：解决方案构建成功；输出包含仓库既有 analyzer warning，但无新增错误

### 下一步

1. 评估 `oneOf` / `anyOf` 是否值得继续沿用 object-focused 子集；若仍会造成生成形状漂移，就直接跳过
2. 若继续扩共享关键字，先在 Runtime / Generator / Tooling 三端同时定义一致边界，再进入实现
3. 继续把 active 入口保持精简，只记录当前恢复点、验证与下一步
