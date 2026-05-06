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

### 阶段：PR #262 review follow-up 与分支同步

- 已使用 `gframework-pr-review` 复核 PR #262，并确认 latest CodeRabbit review body 的第一行下方存在 folded `🧹 Nitpick comments (5)`
- 已修复 `fetch_current_pr_review.py` 的 follow-up 盲区：
  - 不再只依赖 issue comments，而会解析 latest review body 中的 folded nitpick cards
  - `parse_comment_cards` 现已覆盖 `.js/.ts` 等工具文件路径
  - text 输出会同时显示 declared / parsed 数量，避免 future drift 时静默少报
- 已按 5 条 nitpick 收口代码：
  - VS Code tooling 的 `ifElse` hint 现会显示 `condition`
  - `extension.js` 已抽出可复用的 `InlineObjectSchemaHint` typedef
  - `configValidation.js` 已抽取共享 target reference 校验 helper
  - Source Generator tests 已补齐对称分支覆盖
  - Runtime test cleanup 已从 `catch (Exception)` 收窄到 IO / 权限异常
- 已处理本地分支与远端分支差异：
  - 本地 `feat/ai-first-config` 已 rebase 到 `origin/feat/ai-first-config`
  - rebase 过程中 Git 跳过了远端已具备的 commit `76488dc`
  - 当前分支已不再 behind 远端，仅保留本地领先提交

### PR `#262` review follow-up 验证

- 2026-04-20：`python3 .codex/skills/gframework-pr-review/scripts/fetch_current_pr_review.py`
  - 结果：通过
  - 备注：输出 `CodeRabbit actionable comments: 2`、`CodeRabbit nitpick comments: 2 declared, 1 parsed`，并暴露剩余 review follow-up
- 2026-04-20：skill parser follow-up
  - 结果：已补齐
  - 备注：`gframework-pr-review` 现可解析 latest review body 中的 `Outside diff range comments`，并且不再遗漏 `.codex/.../*.py` nitpick cards
- 2026-04-20：`python3 .codex/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --pr 262 --format json`
  - 结果：通过
  - 备注：输出 `CodeRabbit outside-diff comments: 1 declared, 1 parsed`、`CodeRabbit nitpick comments: 2 declared, 2 parsed`，parser warning 清零
- 2026-04-20：运行时条件分支 follow-up
  - 结果：已补齐
  - 备注：`YamlConfigSchemaValidator` 现对非 object 的 `if` / `then` / `else` 使用分支级诊断路径；运行时测试新增 `else` 缺失 `if` 回归
- 2026-04-20：`bun run test`（`tools/gframework-config-tool`）
  - 结果：通过（122 tests）
  - 备注：新增条件分支坏形状回归后，tooling 现在会拒绝缺失 `type: "object"`、坏形状 `properties`、坏形状 `required` 与空白 required 成员
- 2026-04-20：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~SchemaConfigGeneratorTests"`
  - 结果：通过（46 tests）
- 2026-04-20：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~YamlConfigLoaderIfThenElseTests"`
  - 结果：通过（8 tests）
  - 备注：新增 `LoadAsync_Should_Throw_When_Else_Is_Declared_Without_If` 后，运行时回归覆盖保持对称
- 2026-04-20：`dotnet build GFramework.sln -c Release`
  - 结果：通过（历史记录）
  - 备注：存在仓库既有 analyzer warning，但无新增错误；本轮只需重新验证受影响测试切片

### 下一步

1. 跳过 `oneOf` / `anyOf`，优先筛选下一个仍不改变生成类型形状、且不需要属性合并或联合分支生成的共享关键字
2. 若继续扩共享关键字，先在 Runtime / Generator / Tooling 三端同时定义一致边界，再进入实现
3. 继续把 active 入口保持精简，只记录当前恢复点、验证与下一步

## 2026-04-30

### 阶段：组合关键字边界收口（AI-FIRST-CONFIG-RP-003）

- 已在 Runtime、Source Generator 与 VS Code Tooling 三端显式拒绝 `oneOf` / `anyOf`
- 本轮结论不是继续做 object-focused 子集，而是先收紧共享边界：
  - `oneOf` / `anyOf` 更容易引入联合分支、属性合并或生成类型形状漂移
  - 当前配置系统主线仍优先保证 `C# Runtime + Source Generator + Consumer DX` 的稳定契约
  - 因此三端统一改为在 schema 解析 / 生成阶段直接失败，避免静默忽略同一份 schema
- active tracking 也已同步更新，不再把 `oneOf` / `anyOf` 作为下一批默认候选

### 验证

- 2026-04-30：`bun run test`（`tools/gframework-config-tool`）
  - 目标：验证工具端会拒绝 `oneOf`
- 2026-04-30：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~SchemaConfigGeneratorTests"`
  - 目标：验证生成器新增 `GF_ConfigSchema_015`
- 2026-04-30：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~YamlConfigLoaderAllOfTests"`
  - 目标：验证运行时会拒绝对象节点上的 `oneOf`

### 下一步

1. 若本轮定向验证通过，继续盘点下一批真正低风险、且不改变生成类型形状的共享关键字
2. 不再重复评估 `oneOf` / `anyOf` 的 object-focused 子集，除非未来主线明确接受联合形状生成
3. 若后续关键字需要新诊断编号或文档边界说明，继续保持 Runtime / Generator / Tooling 同步收口

### 阶段：Tooling lane 收口整理（AI-FIRST-CONFIG-RP-003）

- 已把 Tooling / Docs 后续动作从 active 入口的主线叙述中剥离，改成 backlog 文件里的非阻塞并行 lane
- 当前 active tracking / trace 只继续承担三件事：
  - 给 `boot` 提供当前恢复点
  - 记录最近一次验证或计划性验证占位
  - 指向真正承载并行批次细节的 backlog 文件
- 本轮不新增代码范围、测试范围或文档范围，只整理 public `ai-plan/**` 的恢复入口表达，避免把治理噪音带回 reader-facing docs

### 关键决定

- `C# Runtime + Source Generator + Consumer DX` 仍是默认恢复主线
- Tooling / Docs 可以并发推进，但后续 batch 应直接以 `ai-first-config-system-csharp-experience-next.md` 为入口，而不是继续扩写 active tracking / trace
- public docs 后续只承接接入 guidance、能力边界和回退方式；批次编排、lane 风险和治理说明继续留在 `ai-plan/**`

### 验证

- 2026-04-30：`wc -l ai-plan/public/ai-first-config-system/todos/ai-first-config-system-tracking.md ai-plan/public/ai-first-config-system/todos/ai-first-config-system-csharp-experience-next.md ai-plan/public/ai-first-config-system/traces/ai-first-config-system-trace.md`
  - 结果：通过
  - 备注：确认本轮仍把 active 入口控制在精简范围，并把 lane 细节下沉到 backlog 文件

### 下一步

1. 若继续做主线代码批次，直接回到共享关键字盘点，不让 Tooling / Docs 成为阻塞条件
2. 若另开 Tooling / Docs batch，先读取 `ai-first-config-system-csharp-experience-next.md` 的并行 lane，再把结果摘要写回 active tracking / trace
3. 继续保持 active 入口精简，不在默认恢复文件中追加 UI 细节、治理台账或面向读者的文档草稿

### 阶段：Tooling / Docs reader-facing 边界补齐（AI-FIRST-CONFIG-RP-003）

- 已在 `config-tool.md`、`config-system.md` 和 `tools/gframework-config-tool/README.md` 明确 reader-facing 能力边界
- 本轮重点不是新增能力，而是把当前分支已经落地的结论写清楚：
  - `contains` / `minContains` / `maxContains`
  - `dependentRequired`、`dependentSchemas`、`allOf`
  - object-focused `if` / `then` / `else`
  - `additionalProperties: false`
  - `oneOf` / `anyOf` rejection
- 同时补充了两个采用原则：
  - VS Code 工具是辅助层，不定义 Runtime 契约
  - 复杂 shape 或超出共享子集的 schema，应回退到 raw YAML 与 schema 文件本体处理

### 验证

- 2026-04-30：`git diff --check -- docs/zh-CN/game/config-tool.md docs/zh-CN/game/config-system.md tools/gframework-config-tool/README.md ai-plan/public/ai-first-config-system/todos/ai-first-config-system-tracking.md ai-plan/public/ai-first-config-system/traces/ai-first-config-system-trace.md`
  - 结果：通过

### 下一步

1. Tooling / Docs 后续若继续推进，优先补真实采用示例，而不是重复扩写边界清单
2. 主线代码批次继续以 Runtime / Generator / Tooling 三端共享关键字收口为中心

### 阶段：Tooling parser 坏形状拒绝收紧（AI-FIRST-CONFIG-RP-003）

- 已在 `tools/gframework-config-tool/src/configValidation.js` 收紧工具侧 schema parser 边界
- 本轮不是扩 JSON Schema 能力，而是避免工具侧比 Runtime / Generator 更宽松：
  - `additionalProperties` 现在只接受 `false`
  - 数组 `items` 必须是 object-shaped 且显式带 `type`
  - 数组 `contains` 若声明，也必须是 object-shaped 且显式带 `type`
- 这样 tuple-array `items: []`、缺失 `type` 的 `contains` 子 schema，以及其他会误导用户以为“工具支持但运行时不支持”的坏形状，会在工具解析阶段直接失败

### 验证

- 2026-04-30：`bun run test`（`tools/gframework-config-tool`）
  - 结果：通过
  - 备注：新增 JS 回归覆盖 `additionalProperties`、tuple-array `items` 与缺失 `type` 的 `contains`

### 下一步

1. 继续盘点 Runtime / Generator / Tooling 三端是否还有类似“工具宽松吞掉、主线不支持”的 schema 形状
2. 若继续做 Tooling lane，优先补 reader-facing 示例或采用路径，而不是继续堆积边界清单

### 阶段：PR #306 open threads 收口（AI-FIRST-CONFIG-RP-003）

- 已重新抓取 PR `#306` 的 latest open review threads，并按“本地仍成立 / 已被当前分支吸收”重新核验
- 本轮收口重点不是继续扩能力，而是把 open threads 中仍成立的三类问题一次性补齐：
  - Generator：补齐 `GF_ConfigSchema_015` 的 `anyOf` 对称负例，避免组合关键字只覆盖 `oneOf`
  - Tooling：拒绝未知显式 `type`、收窄 object-array 只遍历当前 editor 直属 items、统一 `contains` hint 文案
  - Docs：把 `additionalProperties: false` 的“必须显式设置为 false”写清，并为工具补最小接入示例、迁移提示与更准确的 raw YAML 回退条件
- 本轮同时更新了 JS / .NET 回归测试与 active tracking，避免只修 review comment 不保留恢复点

### 验证

- 2026-04-30：`bun run test`（`tools/gframework-config-tool`）
  - 结果：通过（132 tests）
  - 备注：新增未知 schema `type` 拒绝、嵌套 object-array 不串层，以及 `contains` hint 文案回归
- 2026-04-30：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~SchemaConfigGeneratorTests"`
  - 结果：通过（54 tests）
  - 备注：补齐 `Run_Should_Report_Diagnostic_When_Object_Schema_Declares_Unsupported_AnyOf`
- 2026-04-30：`git diff --check`
  - 结果：通过
  - 备注：本轮代码与文档改动未引入空白或冲突标记问题

### 下一步

1. 推送本轮修复后，重新抓取 PR `#306` review 状态，确认哪些 open threads 会被 GitHub 自动折叠或仍需人工回复
2. 若还有残留 open threads，优先区分“远端未刷新 / 已过时评论 / 仍成立问题”，不要再把 review body 摘要和 latest open threads 混在一起处理

## 2026-05-06

### 阶段：开放对象关键字边界收口（AI-FIRST-CONFIG-RP-003）

- 已在 Runtime、Source Generator 与 VS Code Tooling 三端统一收紧开放对象关键字边界
- 本轮不是扩 JSON Schema 能力，而是避免某一端静默接受会重新打开对象形状的 schema：
  - 当前继续接受 `additionalProperties: false` 作为显式闭合对象提醒
  - `patternProperties`、`propertyNames`、`unevaluatedProperties` 当前改为三端直接失败
- reader-facing docs 也已同步更新，避免采用文档继续把这类关键字描述成“也许工具没做但运行时可能支持”的灰区

### 关键决定

- `additionalProperties: false` 仍是唯一共享支持的开放对象相关关键字形状
- 任何会重新引入动态字段集的开放对象关键字，都视为当前主线之外的设计，而不是后续工具增强项
- 本轮继续保持主线为 `C# Runtime + Source Generator + Consumer DX`，没有把工作重心切回复杂表单或宿主验证

### Stop Condition

- Batch baseline：`origin/main` (`a8c6c11e`, `2026-05-05 13:14:24 +0800`)
- Primary metric：branch diff vs `origin/main` changed files，阈值 `50`
- 本轮执行时的 branch diff 指标仍为 `0`，说明当前批次尚未把 `HEAD` 推进到接近阈值；reviewability headroom 充足

### 验证

- 2026-05-06：`bun run test`（`tools/gframework-config-tool`）
  - 结果：通过（133 tests）
  - 备注：新增 JS 回归覆盖 `patternProperties`、`propertyNames` 与 `unevaluatedProperties` 的显式拒绝
- 2026-05-06：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~YamlConfigLoaderAllOfTests"`
  - 结果：通过（18 tests）
  - 备注：运行时新增开放对象关键字拒绝回归，继续沿用 `SchemaUnsupported` + `reward` 诊断路径
- 2026-05-06：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~SchemaConfigGeneratorTests"`
  - 结果：通过（57 tests）
  - 备注：生成器新增 `GF_ConfigSchema_016` 对称回归，覆盖 3 类开放对象关键字
- 2026-05-06：`dotnet build GFramework.Game/GFramework.Game.csproj -c Release`
  - 结果：通过（0 warnings, 0 errors）
- 2026-05-06：`dotnet build GFramework.Game.SourceGenerators/GFramework.Game.SourceGenerators.csproj -c Release`
  - 结果：通过（0 warnings, 0 errors）
- 2026-05-06：`python3 scripts/license-header.py --check --paths ...`
  - 结果：通过
  - 备注：仓库脚本默认 `git ls-files` 在当前 WSL worktree 绑定下无法直接解析仓库上下文，因此本轮改为对受影响文件执行 targeted check
- 2026-05-06：`git diff --check`
  - 结果：通过

### 下一步

1. 继续盘点下一批不会改变生成类型形状、也不会重新打开对象形状的共享关键字
2. Tooling / Docs 如继续并发推进，优先补真实采用示例，不再重复扩写开放对象边界清单
3. 若后续 batch 再触碰 schema contract，继续保持 Runtime / Generator / Tooling 三端同步失败语义与 reader-facing docs 一致

### 阶段：PR #325 latest review follow-up 收口（AI-FIRST-CONFIG-RP-003）

- 已使用 `gframework-pr-review` 抓取并复核 PR `#325` 的 latest review body、未解决 latest-head 线程、MegaLinter 摘要与测试报告
- 本轮按“仅修复本地仍成立项”收口 5 条 review 信号：
  - Runtime / Generator / Tooling 三端均移除开放对象关键字校验中的不可达 `additionalProperties: false` 放行分支
  - Tooling 测试补齐 `additionalProperties: false` 的正向回归，避免共享允许边界后续回退
  - `docs/zh-CN/game/index.md` 将开放对象边界说明拆成并列语句，避免把 `patternProperties` / `propertyNames` / `unevaluatedProperties` 误读成 `additionalProperties` 的变体
- 本轮没有跟进 stale 信号：
  - PR 当前 failed checks 为 `0`
  - latest test report 为 `2280 passed / 0 failed`
  - MegaLinter 仅保留 `dotnet-format` 摘要噪音，未提供需要额外修复的新代码格式差异

### 验证

- 2026-05-06：`python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output /tmp/gframework-current-pr-review.json`
  - 结果：通过
  - 备注：确认 PR `#325` 仍有 3 条 CodeRabbit nitpick 与 2 条 Greptile open threads，需要本地核验
- 2026-05-06：`node --test ./test/*.test.js`（`tools/gframework-config-tool`）
  - 结果：通过（134 tests）
  - 备注：新增 `additionalProperties: false` 正向回归后，工具端继续显式接受唯一共享闭合对象入口
- 2026-05-06：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~YamlConfigLoaderAllOfTests"`
  - 结果：通过（18 tests）
  - 备注：运行时开放对象关键字回归保持通过，未引入额外诊断路径漂移
- 2026-05-06：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~SchemaConfigGeneratorTests"`
  - 结果：通过（57 tests）
  - 备注：生成器开放对象关键字诊断回归保持通过，移除不可达分支未影响既有诊断契约
- 2026-05-06：`dotnet build GFramework.Game/GFramework.Game.csproj -c Release`
  - 结果：通过（0 warnings, 0 errors）
- 2026-05-06：`dotnet build GFramework.Game.SourceGenerators/GFramework.Game.SourceGenerators.csproj -c Release`
  - 结果：通过（0 warnings, 0 errors）
- 2026-05-06：`python3 scripts/license-header.py --check`
  - 结果：环境受限
  - 备注：仓库脚本默认通过 `git ls-files` 枚举文件，在当前 WSL worktree 绑定下返回 `128`；已改为对受影响文件执行 targeted check 并通过
- 2026-05-06：`python3 scripts/license-header.py --check --paths GFramework.Game/Config/YamlConfigSchemaValidator.cs GFramework.Game.SourceGenerators/Config/SchemaConfigGenerator.cs tools/gframework-config-tool/src/configValidation.js tools/gframework-config-tool/test/configValidation.test.js`
  - 结果：通过
- 2026-05-06：`git diff --check`
  - 结果：通过

### 下一步

1. 执行本轮受影响 Tooling / Runtime / Generator 定向验证，并确认没有新增 warning 或格式漂移
2. 若验证通过，重新抓取 PR `#325` review 状态，区分哪些 open threads 会随推送自动折叠
3. 继续把 PR review follow-up 约束在“latest unresolved thread + 本地仍成立问题”，不回头追旧 summary 噪音
