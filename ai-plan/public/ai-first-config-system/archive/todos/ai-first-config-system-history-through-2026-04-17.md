# AI-First Config System 执行追踪

## 0. 迁移说明

### 2026-04-19

- 已按 `ai-plan` 治理规范把当前 worktree 的旧本地恢复文档迁移到 `ai-plan/public/ai-first-config-system/`
- 当前分支 `feat/ai-first-config` 已登记到 `ai-plan/public/README.md`，后续 `boot` 可直接命中本主题
- 迁移后的公共文档已清洗绝对路径与机器相关信息，避免把本地环境细节继续带入可提交恢复文档

## 1. 目标

基于当前 `GFramework` 设计结论，逐步落地 AI-First 游戏配置系统。

当前已确认的产品与架构决策：

- 配置系统定位为游戏静态内容配置，不是 `GFramework.Core.Configuration` 的扩展
- 配置系统位于 `GFramework.Game` 侧
- 运行时、生成器、工具层分离
- 当前主线优先级调整为 `C# Runtime + Source Generator + Consumer DX`
- VS Code Extension 保持可选工具位，不再阻塞 C# 首发可用版本
- 独立 `Config Studio` 桌面版暂不进入首发范围

## 2. 阶段拆分

### Phase 1: Runtime MVP

目标：

- 建立只读配置表抽象
- 建立配置注册表抽象
- 提供内存实现
- 补齐基础测试

状态：已完成

### Phase 2: YAML Loader MVP

目标：

- 支持从配置目录加载文本配置
- 建立最小加载流程
- 明确主键与错误处理策略

状态：已完成

### Phase 3: Source Generator MVP

目标：

- 从 schema 生成配置类型
- 生成表访问包装
- 建立快照测试

状态：已完成

### Phase 4: VS Code Extension MVP

目标：

- 配置树浏览
- schema 校验
- 表单编辑入口
- Raw 编辑入口

状态：已完成（首版骨架，后续作为可选增强项维护）

## 3. 本轮执行项

- [x] 重写设计文档，收敛为当前仓库边界可执行版本
- [x] 将工具层默认方案切换为 VS Code Extension
- [x] 创建执行追踪文件
- [x] 新增配置抽象接口
- [x] 新增内存表与注册表实现
- [x] 增加基础行为测试
- [x] 运行定向测试并记录结果
- [x] 新增 YAML 配置目录加载器
- [x] 验证目录扫描、失败回滚与反序列化错误路径
- [x] 新增 schema 到配置类型/表包装的 Source Generator
- [x] 增加生成快照测试与基础错误诊断测试
- [x] 新增 VS Code 插件最小骨架
- [x] 提供配置树、raw/schema 打开、基础校验和轻量表单入口
- [x] 将追踪主线从“工具优先”切回 `C# Runtime + Generator + Consumer DX`
- [x] 为生成器新增项目级聚合注册入口 `RegisterAllGeneratedConfigTables()`
- [x] 为生成器新增 `GeneratedConfigCatalog`，统一暴露当前消费者项目内的生成表目录
- [x] 将消费者端到端测试与 `Architecture` 集成测试切换到聚合注册入口

## 4. 面向正式可用的 TODO

### P0: 必须完成后才建议用于正式游戏项目

- [x] Runtime 接入 JSON Schema 驱动校验
- [x] 运行时拒绝缺失必填字段、未知字段和类型不匹配数据
- [x] 为运行时 schema 校验补齐回归测试
- [x] 让 Source Generator 在消费项目中自动拾取 `schemas/**/*.schema.json`
- [x] 提供最小可复制的消费者接入示例与说明
- [x] 增加一个端到端消费者集成测试

### P1: 强烈建议尽快补齐

- [x] 扩展最有价值的一批 schema 关键字到 Runtime + Generator + Tooling 的共享子集
- [x] 开发期热重载：文件监听、局部重载、诊断通知
- [x] VS Code 插件增加基本自动化测试
- [x] VS Code 插件支持比“顶层标量字段”更完整的 schema 表单能力
- [x] VS Code 插件校验逻辑与运行时校验逻辑对齐

### P2: 后续增强

- [x] 跨表引用校验
- [x] 批量编辑能力
- [x] 更丰富的 schema 元数据支持
- [x] 评估是否需要独立 `Config Studio`

### 当前已知剩余缺口（按当前 C# 优先级排序）

- [x] 继续降低多表项目启动样板，例如支持按配置域过滤或分组注册，而不只是“全部注册”
- [x] 提供比文档模板更正式的 C# 启动帮助器，收敛 `ConfigRegistry + YamlConfigLoader + 热重载` 生命周期
- [ ] 扩展 JSON Schema 支持范围，在已支持 `const`、`not`、`pattern`、`minimum`、`maximum`、`minLength`、`maxLength`、`minItems`、`maxItems`、`exclusiveMinimum`、`exclusiveMaximum`、`multipleOf`、`uniqueItems`、`minProperties`、`maxProperties`、`dependentRequired`、`dependentSchemas`、`allOf` 的基础上补齐下一批关键字与约束映射
- [x] 评估是否需要为高频查询字段生成可选只读索引，而不破坏当前轻量线性扫描契约
- [ ] VS Code 表单继续支持更深层对象数组嵌套，减少 raw YAML 回退
- [ ] 为复杂结构提供比“顶层标量 / 标量数组”更强的批量编辑能力
- [ ] 在真实 VS Code 宿主中完成对象数组编辑与复杂 schema 的交互式手工验证

## 5. 当前实现范围约束

当前阶段额外暂不做：

- 为了工具侧继续扩深层编辑器能力而阻塞 C# 首发可用版本
- 运行时可写配置或在线编辑工作流
- 独立桌面 `Config Studio`

## 6. 最近更新

### 2026-04-17

- Runtime / Generator / Tooling 共享新增 object-focused `allOf` 支持；当前只接受 object 节点上的 object-typed inline schema 数组，并按 focused constraint block 语义叠加约束，不做属性合并，也不改变生成类型形状
- `GFramework.Game/Config/YamlConfigSchemaValidator.cs` 新增 `allOf` 解析、运行时匹配与引用递归采集；非 object 节点声明 `allOf` 会在 schema 解析阶段直接拒绝，`allOf` 匹配成功分支的 ref-table 也继续走结构化去重
- `GFramework.Game.Tests/Config/YamlConfigLoaderAllOfTests.cs` 覆盖 `allOf` 满足 / 不满足、非数组、非 object 值、非 object-typed 条目与非 object 节点声明回归；`GFramework.Game.Tests/Config/YamlConfigSchemaValidatorTests.cs` 新增 `allOf` 引用去重回归
- `GFramework.Game.SourceGenerators/Config/SchemaConfigGenerator.cs`、`ConfigSchemaDiagnostics` 与 `AnalyzerReleases.Unshipped.md` 新增 `GF_ConfigSchema_012`，递归校验 `allOf` 形状并将约束摘要写入 XML 文档
- `GFramework.SourceGenerators.Tests/Config/SchemaConfigGeneratorTests.cs` 新增 `allOf` 文档输出、非 object 节点、非数组与非 object-typed 条目诊断回归
- `tools/gframework-config-tool/src/configValidation.js`、`extension.js`、`localization.js` 与 `localizationKeys.js` 现支持 `allOf` 解析、校验、本地化与对象 section hint；`configValidation.test.js` 与 `localization.test.js` 已补齐对应回归
- `docs/zh-CN/game/config-system.md` 与 `ai-plan/public/ai-first-config-system/todos/ai-first-config-system-csharp-experience-next.md` 已更新共享关键字清单、`allOf` 语义和工具提示范围；`ai-plan/public/ai-first-config-system/traces/` 已补建本轮执行 trace
- 根据 review 继续收紧 `allOf`：Runtime / Generator / Tooling 现都会拒绝在 `allOf` focused block 中引用父对象未声明字段的不可满足 schema，避免“主链路拒绝 unknown property、allOf 又要求该字段”的死锁形状
- 根据 review 继续收紧 `allOf` 关键字形状校验：Runtime / Generator 不再静默放过 `allOf.properties` 非对象或 `allOf.required` 非数组的坏 schema，而是直接给出编译期 / 运行时失败
- 根据 review 继续收紧 `allOf.required` 条目校验：Runtime / Generator 不再静默跳过非字符串或空白项，而是直接报 schema 元数据错误
- `GFramework.Game/Config/YamlConfigSchemaValidator.cs` 的对象关键字解析/校验已拆到新的 partial 文件 `YamlConfigSchemaValidator.ObjectKeywords.cs`，把 `dependentRequired`、`dependentSchemas`、`allOf` 与对象属性数量约束从近 5k 行主文件中移出
- `GFramework.Game.SourceGenerators/Config/SchemaConfigGenerator.cs` 修正 `TryTraverseSchemaRecursively()` 注释缩进，并把深层 `allOf` 递归路径统一为运行时约定的 `reward[allOf[0]]` 形式；`tools/gframework-config-tool/src/configValidation.js` 同步改为相同路径格式
- `GFramework.SourceGenerators.Tests/Config/SchemaConfigGeneratorTests.cs` 新增 `allOf.properties` / `allOf.required` 形状错误回归；`GFramework.Game.Tests/Config/YamlConfigLoaderAllOfTests.cs` 同步补齐运行时坏 schema 回归
- `docs/zh-CN/game/config-system.md` 补充 `allOf` 最小 schema/YAML 示例与兼容性说明，明确“父对象先声明字段，再用 allOf 叠加 required/约束”，且 `allOf` 不做属性合并
- 已执行：`node --check tools/gframework-config-tool/src/configValidation.js`
- 已执行：`node --test tools/gframework-config-tool/test/configValidation.test.js`
- 已执行：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release -p:RestoreFallbackFolders= --filter "FullyQualifiedName~YamlConfigLoaderAllOfTests"`
- 已执行：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -p:RestoreFallbackFolders= --filter "FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Write_AllOf_Constraint_Into_Generated_Documentation|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_NonObject_Schema_Declares_AllOf|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_AllOf_Is_Not_An_Array|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_AllOf_Entry_Is_Not_Object_Valued|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_AllOf_Entry_Is_Not_Object_Typed|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_AllOf_Entry_Targets_Undeclared_Parent_Property|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_With_Runtime_Aligned_Path_When_AllOf_Inner_Schema_Is_Invalid"`
- 结果：review 补丁后的 JS / Runtime / Generator 定向回归均已通过；`GFramework.Game.Tests` 仍保留既有 `GF_ContextRegistration_003` 告警，但与本轮 `allOf` 修正无关
- 已执行：`node --check tools/gframework-config-tool/src/configValidation.js`
- 已执行：`node --check tools/gframework-config-tool/src/extension.js`
- 已执行：`node --test tools/gframework-config-tool/test/configValidation.test.js`
- 已执行：`node --test tools/gframework-config-tool/test/localization.test.js`
- 已执行：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release -p:RestoreFallbackFolders= --filter "FullyQualifiedName~YamlConfigLoaderAllOfTests|FullyQualifiedName~YamlConfigSchemaValidatorTests"`
- 已执行：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -p:RestoreFallbackFolders= --filter "FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Write_AllOf_Constraint_Into_Generated_Documentation|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_NonObject_Schema_Declares_AllOf|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_AllOf_Is_Not_An_Array|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_AllOf_Entry_Is_Not_Object_Typed"`
- 结果：本轮 JS 与 C# 定向回归均已通过；`GFramework.Game.Tests` 仍保留既有 `GF_ContextRegistration_003` 告警，但与本轮 `allOf` 改动无关

- 根据 review 修正 `tools/gframework-config-tool/src/configValidation.js` 的 `dependentSchemas` 触发判断复用路径，`validateParsedConfig()` 与 `matchesSchemaNodeInternal()` 现在共享同一 helper，避免对象条件匹配语义后续漂移
- `GFramework.Game/Config/YamlConfigSchemaValidator.cs` 的 `ValidateObjectConstraints()` XML 注释已同步到“数量约束 + dependentRequired + dependentSchemas”新职责，并把条件子 schema 成功匹配时的跨表引用回写改为结构化去重，避免同一字段被重复记录
- `GFramework.Game.SourceGenerators/Config/SchemaConfigGenerator.cs` 现在会拒绝非 object 节点上的 `dependentSchemas`，补齐 `TryBuildInlineSchemaSummary(..., includeRequiredProperties)` 的 XML 参数注释，并把误登记在 `GFramework.Core.SourceGenerators/AnalyzerReleases.Unshipped.md` 的 `GF_ConfigSchema_001` 到 `GF_ConfigSchema_011` 清理回 `GFramework.Game.SourceGenerators/AnalyzerReleases.Unshipped.md` 所属项目
- `GFramework.Game.Tests/Config/YamlConfigSchemaValidatorTests.cs` 新增条件子 schema 引用去重回归；`GFramework.SourceGenerators.Tests/Config/SchemaConfigGeneratorTests.cs` 新增“非 object 节点声明 dependentSchemas”诊断回归；`GFramework.Game.Tests/Config/YamlConfigLoaderDependentSchemasTests.cs` 现补齐“trigger 未在同级 properties 中声明时拒绝”分支，并与 `GFramework.Game.Tests/Config/YamlConfigSchemaValidatorTests.cs` 同步去掉 `null!` 测试根目录初始化
- 已执行：`node --check tools/gframework-config-tool/src/configValidation.js`
- 已执行：`node --test tools/gframework-config-tool/test/configValidation.test.js`
- 已执行：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release -p:RestoreFallbackFolders= --filter "FullyQualifiedName~YamlConfigLoaderDependentSchemasTests|FullyQualifiedName~YamlConfigSchemaValidatorTests"`
- 已执行：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release -p:RestoreFallbackFolders= --filter "FullyQualifiedName~YamlConfigLoaderDependentSchemasTests"`
- 已执行：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -p:RestoreFallbackFolders= --filter "FullyQualifiedName~Run_Should_Write_DependentSchemas_Constraint_Into_Generated_Documentation|FullyQualifiedName~Run_Should_Report_Diagnostic_When_DependentSchemas_Schema_Is_Not_Object_Typed|FullyQualifiedName~Run_Should_Report_Diagnostic_When_NonObject_Schema_Declares_DependentSchemas|FullyQualifiedName~Run_Should_Report_Diagnostic_When_DependentSchemas_Schema_Uses_Format_On_Non_String_Node"`
- 结果：本轮 JS 与 C# 定向回归均已通过；`GF_ConfigSchema_*` 误登记导致的 `RS2002` 警告已消失，当前仅保留既有的 `GF_ContextRegistration_003` 测试项目告警
- Runtime / Generator / Tooling 共享新增 `dependentSchemas` 关键字支持，用于表达“当对象内某个字段出现时，当前对象还必须额外满足哪个 object 子 schema”，且不改变生成类型形状
- `GFramework.Game/Config/YamlConfigSchemaValidator.cs` 现会解析对象级 `dependentSchemas` 映射，并在运行时复用现有递归 matcher 执行条件校验；当前条件子 schema 按 focused constraint block 语义允许未声明的额外同级字段继续存在
- 新增 `GFramework.Game.Tests/Config/YamlConfigLoaderDependentSchemasTests.cs`，覆盖条件 schema 未满足拒绝、触发字段缺席通过、条件满足且保留额外 sibling 通过，以及坏 schema 拒绝路径
- `GFramework.SourceGenerators/Config/SchemaConfigGenerator.cs` 与 `ConfigSchemaDiagnostics` 新增 `dependentSchemas` 递归元数据校验和 XML 文档输出；`GF_ConfigSchema_011` 会拒绝非 object-typed 或坏形状的 `dependentSchemas`
- `GFramework.SourceGenerators.Tests/Config/SchemaConfigGeneratorTests.cs` 新增 `dependentSchemas` 文档输出、坏 schema 诊断，以及子 schema 内非法 `format` 递归诊断回归；`GFramework.SourceGenerators/AnalyzerReleases.Unshipped.md` 已登记新规则
- `tools/gframework-config-tool/src/configValidation.js`、`extension.js`、`localization.js` 与 `localizationKeys.js` 现会解析 `dependentSchemas`、给出中英文校验诊断，并在对象 section hint 中展示条件子 schema 摘要
- `tools/gframework-config-tool/test/configValidation.test.js` 与 `localization.test.js` 已新增 `dependentSchemas` 的解析 / 校验 / 本地化回归
- `docs/zh-CN/game/config-system.md` 与 `ai-plan/public/ai-first-config-system/todos/ai-first-config-system-csharp-experience-next.md` 已更新共享关键字清单，补充 `dependentSchemas` 的语义与工具提示范围
- 已执行：`node --check tools/gframework-config-tool/src/configValidation.js`
- 已执行：`node --check tools/gframework-config-tool/src/extension.js`
- 已执行：`node --test tools/gframework-config-tool/test/configValidation.test.js`
- 已执行：`node --test tools/gframework-config-tool/test/localization.test.js`
- 已执行：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release -p:RestoreFallbackFolders= --filter "FullyQualifiedName~YamlConfigLoaderDependentSchemasTests"`
- 已执行：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -p:RestoreFallbackFolders= --filter "FullyQualifiedName~Run_Should_Write_DependentSchemas_Constraint_Into_Generated_Documentation|FullyQualifiedName~Run_Should_Report_Diagnostic_When_DependentSchemas_Schema_Is_Not_Object_Typed|FullyQualifiedName~Run_Should_Report_Diagnostic_When_DependentSchemas_Schema_Uses_Format_On_Non_String_Node"`
- 结果：JS 与 C# 定向回归均已通过；本轮继续串行执行 `.NET` 验证，避免同一 worktree 的 `obj` 文件竞争
### 2026-04-16

- Runtime / Generator / Tooling 共享新增 `dependentRequired` 关键字支持，用于表达“当对象内某个字段出现时，还必须同时声明哪些同级字段”，且不改变生成类型形状
- `GFramework.Game/Config/YamlConfigSchemaValidator.cs` 现会解析对象级 `dependentRequired` 映射，并在运行时与 `contains` / `not` 试匹配路径上统一复用同一套 sibling 依赖语义
- 新增 `GFramework.Game.Tests/Config/YamlConfigLoaderDependentRequiredTests.cs`，覆盖依赖字段缺失拒绝、触发字段缺席通过、依赖满足通过，以及坏 schema 拒绝路径
- `GFramework.SourceGenerators/Config/SchemaConfigGenerator.cs` 与 `ConfigSchemaDiagnostics` 新增 `dependentRequired` 递归元数据校验和 XML 文档输出；`GF_ConfigSchema_010` 会拒绝引用未声明 sibling 字段的坏 schema
- `GFramework.SourceGenerators.Tests/Config/SchemaConfigGeneratorTests.cs` 新增 `dependentRequired` 文档输出与坏 schema 诊断回归；`GFramework.SourceGenerators/AnalyzerReleases.Unshipped.md` 已登记新规则
- `tools/gframework-config-tool/src/configValidation.js`、`localization.js`、`localizationKeys.js` 与 `extension.js` 现会解析 `dependentRequired`、给出中英文校验诊断，并在对象 section hint 中展示 sibling 依赖关系
- `tools/gframework-config-tool/test/configValidation.test.js` 与 `localization.test.js` 已新增 `dependentRequired` 的解析 / 校验 / 本地化回归
- `docs/zh-CN/game/config-system.md` 与 `ai-plan/public/ai-first-config-system/todos/ai-first-config-system-csharp-experience-next.md` 已更新共享关键字清单，补充 `dependentRequired` 的语义与工具提示范围
- 已执行：`node --check tools/gframework-config-tool/src/configValidation.js`
- 已执行：`node --check tools/gframework-config-tool/src/extension.js`
- 已执行：`node --test tools/gframework-config-tool/test/configValidation.test.js`
- 已执行：`node --test tools/gframework-config-tool/test/localization.test.js`
- 已执行：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release -p:RestoreFallbackFolders= --filter "FullyQualifiedName~YamlConfigLoaderDependentRequiredTests"`
- 已执行：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -p:RestoreFallbackFolders= --filter "FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Write_DependentRequired_Constraint_Into_Generated_Documentation|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_DependentRequired_Target_Is_Not_Declared"`
- 结果：JS 与 C# 定向回归均已通过；中途确认并避开了并行 `dotnet test` 导致的同一 worktree `obj` 文件竞争问题，后续应继续串行执行相关 .NET 验证

- Runtime / Generator / Tooling 共享把 `enum` 从“标量与标量数组元素”扩到“标量、对象、数组节点”；当前对象 `enum` 会忽略字段顺序比较，数组 `enum` 保留元素顺序
- `GFramework.Game/Config/YamlConfigSchemaValidator.cs` 现会把 `enum` 候选值预归一化为与 `const` 相同的稳定比较键，并在对象 / 数组 / 标量主校验链上统一执行匹配
- 新增 `GFramework.Game.Tests/Config/YamlConfigLoaderEnumTests.cs`，覆盖对象 `enum` 的顺序无关匹配、对象值未命中拒绝，以及数组 `enum` 的顺序敏感拒绝
- `GFramework.SourceGenerators/Config/SchemaConfigGenerator.cs` 现会把对象 / 数组 `enum` 以原始 JSON 文本写入生成代码 XML 文档；新增 `GFramework.SourceGenerators.Tests/Config/SchemaConfigGeneratorEnumTests.cs` 覆盖对象 / 数组文档输出
- `tools/gframework-config-tool/src/configValidation.js` 现会为对象 / 数组 `enum` 保存显示文本与 comparable key，并在 VS Code 校验中复用与运行时一致的比较语义；新增 `tools/gframework-config-tool/test/configValidation.enum.test.js` 覆盖元数据解析与顺序语义
- `docs/zh-CN/game/config-system.md` 与 `ai-plan/public/ai-first-config-system/todos/ai-first-config-system-csharp-experience-next.md` 已更新共享关键字清单，明确对象 / 数组 `enum` 当前主要参与校验与 XML 文档输出
- 已执行：`node --check tools/gframework-config-tool/src/configValidation.js`
- 已执行：`node --test tools/gframework-config-tool/test/configValidation.test.js`
- 已执行：`node --test tools/gframework-config-tool/test/configValidation.enum.test.js`
- 已执行：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release -p:RestoreFallbackFolders= --filter "FullyQualifiedName~YamlConfigLoaderEnumTests"`
- 已执行：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -p:RestoreFallbackFolders= --filter "FullyQualifiedName~SchemaConfigGeneratorEnumTests|FullyQualifiedName~SchemaConfigGeneratorTests"`
- 结果：JS 与 C# 定向回归均已通过；本轮继续沿用 `-p:RestoreFallbackFolders=` 规避旧的 Windows NuGet fallback 目录干扰

- 根据 review 修正 `GFramework.SourceGenerators/Config/SchemaConfigGenerator.cs` 的 `not` 递归 `format` 校验顺序，`not` 子 schema 不再被非数组节点的早退逻辑跳过
- `GFramework.SourceGenerators.Tests/Config/SchemaConfigGeneratorTests.cs` 里的 `GF_ConfigSchema_009` 回归现在对应到真实接线路径，目标是稳定覆盖 `hp[not]` 这类非数组节点诊断
- 新增 `GFramework.Game.Tests/Config/YamlConfigLoaderNegationTests.cs`，把 `not` 运行时回归从 3800 行主 fixture 中独立出来，并补齐“未命中 not 时允许通过”“对象完整命中时拒绝”“对象仅命中属性子集时允许通过”的对照覆盖
- `tools/gframework-config-tool/test/configValidation.test.js` 新增对象 `not` 的完整命中失败用例，避免对象分支整体失效时仍被现有子集匹配回归漏过
- 已执行：`node --test tools/gframework-config-tool/test/configValidation.test.js`
- 已执行：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -p:RestoreFallbackFolders= --filter "FullyQualifiedName~SchemaConfigGeneratorTests"`
- 已执行：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release -p:RestoreFallbackFolders= --filter "FullyQualifiedName~YamlConfigLoaderTests|FullyQualifiedName~YamlConfigLoaderNegationTests"`
- 结果：两条 C# 定向测试均已通过；本轮在命令行显式传入 `-p:RestoreFallbackFolders=` 后，`ResolvePackageAssets` 不再指向旧的 Windows fallback 目录

- Runtime / Generator / Tooling 共享新增 `not` 关键字支持，可在不改变生成类型形状的前提下表达“当前值不得匹配某个内联子 schema”的负约束
- `GFramework.Game/Config/YamlConfigSchemaValidator.cs` 现会解析 `not` 子 schema、在运行时按主校验链的严格对象语义执行 negated match，并在命中禁用分支时抛出结构化 `ConstraintViolation`
- `GFramework.Game.Tests/Config/YamlConfigLoaderTests.cs` 新增 `not` 命中拒绝与坏 schema（`not` 不是对象）回归，覆盖运行时行为与 schema 解析失败路径
- `GFramework.SourceGenerators/Config/SchemaConfigGenerator.cs` 现会把 `not` 写入生成 XML 文档，并把 `not` 子树纳入递归 `format` 元数据校验，避免生成器比运行时 / tooling 更宽松
- `GFramework.SourceGenerators.Tests/Config/SchemaConfigGeneratorTests.cs` 新增 `not` 文档输出与 `not` 子 schema 非法 `format` 诊断回归
- `tools/gframework-config-tool/src/configValidation.js` 新增 `not` 子 schema 解析、严格对象匹配语义与校验诊断；`localization.js` / `localizationKeys.js` 新增中英文 `not` 诊断文本
- `tools/gframework-config-tool/test/configValidation.test.js` 与 `localization.test.js` 已新增 `not` 解析 / 校验 / 本地化回归，并覆盖“对象 not 不采用 contains 式子集匹配”的边界
- `docs/zh-CN/game/config-system.md` 与 `ai-plan/public/ai-first-config-system/todos/ai-first-config-system-csharp-experience-next.md` 已更新共享关键字清单，明确 `not` 的当前语义边界
- 已执行：`node --test tools/gframework-config-tool/test/configValidation.test.js`
- 已执行：`node --test tools/gframework-config-tool/test/localization.test.js`
- 已执行：`node --check tools/gframework-config-tool/src/configValidation.js`
- 尝试执行：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~YamlConfigLoaderTests.LoadAsync_Should_Throw_When_Value_Matches_Not_Schema|FullyQualifiedName~YamlConfigLoaderTests.LoadAsync_Should_Throw_When_Not_Is_Not_An_Object"`
- 尝试执行：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Write_Not_Constraint_Into_Generated_Documentation|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_Not_Schema_Uses_Format_On_Non_String_Node"`
- 结果：两条 `dotnet test` 在当前 WSL 环境下都被同一 NuGet fallback 目录错误阻塞，`ResolvePackageAssets` 继续指向某个不存在的宿主 Windows NuGet fallback 目录

- Runtime / Generator / Tooling 共享新增稳定字符串 `format: duration` 支持；当前统一支持 `date`、`date-time`、`duration`、`email`、`time`、`uri` 与 `uuid`
- `GFramework.Game/Config/YamlConfigSchemaValidator.cs` 新增 `duration` 格式映射、day-time duration 正则与运行时校验；当前只接受 `P[n]D`、`PT[n]H[n]M[n]S` 及其组合，秒允许小数，并明确拒绝 `Y` / `M(月)` / `W` 等日历语义片段
- `GFramework.Game.Tests/Config/YamlConfigLoaderTests.cs` 将 `duration` 纳入共享 format 成功 / 失败参数化回归，并校验未支持格式诊断文本同步包含新白名单成员
- `GFramework.SourceGenerators/Config/SchemaConfigGenerator.cs` 将 `duration` 纳入生成阶段共享白名单；`SchemaConfigGeneratorTests` 现在验证 `format = 'duration'` XML 文档输出与未支持格式诊断文本更新
- `tools/gframework-config-tool/src/configValidation.js` 新增 `duration` 解析白名单与 day-time duration 校验；`configValidation.test.js` 同步覆盖接受 / 拒绝、schema 元数据提取和未支持格式提示文本
- `docs/zh-CN/game/config-system.md` 与 `ai-plan/public/ai-first-config-system/todos/ai-first-config-system-csharp-experience-next.md` 已更新稳定 `format` 子集说明，并明确 `duration` 只支持 day-time 子集
- 已执行：`node --test tools/gframework-config-tool/test/configValidation.test.js`
- 已执行：`node --check tools/gframework-config-tool/src/configValidation.js`
- 尝试执行：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~YamlConfigLoaderTests.LoadAsync_Should_Accept_Supported_String_Format|FullyQualifiedName~YamlConfigLoaderTests.LoadAsync_Should_Throw_When_String_Does_Not_Match_Supported_Format|FullyQualifiedName~YamlConfigLoaderTests.LoadAsync_Should_Throw_When_String_Format_Is_Not_Supported"`
- 尝试执行：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Write_Supported_Duration_Format_Into_Generated_Documentation|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_String_Format_Is_Not_Supported"`
- 结果：两条 `dotnet test` 在当前 WSL 环境下仍被同一 NuGet fallback 目录错误阻塞，`ResolvePackageAssets` 解析到某个不存在的宿主 Windows NuGet fallback 目录；本轮依旧无法完成 C# 定向测试

- Runtime / Generator / Tooling 共享新增稳定字符串 `format: time` 支持；当前统一支持 `date`、`date-time`、`email`、`time`、`uri` 与 `uuid`
- `GFramework.Game/Config/YamlConfigSchemaValidator.cs` 新增 `time` 格式映射、显式时区偏移正则与运行时校验；当前只接受 `HH:mm:ss[.fraction](Z|±HH:mm)`，避免 time-only 文本在不同宿主上隐式补默认日期或本地时区
- `GFramework.Game.Tests/Config/YamlConfigLoaderTests.cs` 将 `time` 纳入共享 format 成功 / 失败参数化回归，并校验未支持格式诊断文本同步包含新白名单成员
- `GFramework.SourceGenerators/Config/SchemaConfigGenerator.cs` 将 `time` 纳入生成阶段共享白名单；`SchemaConfigGeneratorTests` 现在验证 `format = 'time'` XML 文档输出与未支持格式诊断文本更新
- `tools/gframework-config-tool/src/configValidation.js` 新增 `time` 解析白名单与 RFC 3339 full-time 校验；`configValidation.test.js` 同步覆盖接受 / 拒绝、schema 元数据提取和未支持格式提示文本
- `docs/zh-CN/game/config-system.md` 与 `ai-plan/public/ai-first-config-system/todos/ai-first-config-system-csharp-experience-next.md` 已更新稳定 `format` 子集说明，并明确 `time` 需要显式时区偏移
- 已执行：`node --test tools/gframework-config-tool/test/configValidation.test.js`
- 已执行：`node --check tools/gframework-config-tool/src/configValidation.js`
- 尝试执行：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~YamlConfigLoaderTests.LoadAsync_Should_Accept_Supported_String_Format|FullyQualifiedName~YamlConfigLoaderTests.LoadAsync_Should_Throw_When_String_Does_Not_Match_Supported_Format|FullyQualifiedName~YamlConfigLoaderTests.LoadAsync_Should_Throw_When_String_Format_Is_Not_Supported"`
- 尝试执行：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Write_Supported_Time_Format_Into_Generated_Documentation|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_String_Format_Is_Not_Supported"`
- 结果：两条 `dotnet test` 在当前 WSL 环境下都被同一 NuGet fallback 目录错误阻塞，`ResolvePackageAssets` 解析到某个不存在的宿主 Windows NuGet fallback 目录；即使额外尝试 `-p:RestoreFallbackFolders=` 也未绕过该环境问题，本轮无法完成 C# 定向测试

### 2026-04-11

- Runtime / Generator / Tooling 共享新增稳定字符串 `format` 子集支持：当前统一支持 `date`、`date-time`、`email`、`uri` 与 `uuid`
- `GFramework.Game/Config/YamlConfigSchemaValidator.cs` 现在会在 schema 解析阶段拒绝不支持的 `format`，并在运行时对字符串值执行跨端一致的格式校验；`uri` 额外要求显式 scheme，避免把普通路径误判成绝对 URI
- `GFramework.Game.Tests/Config/YamlConfigLoaderTests.cs` 新增 `format` 成功加载、格式违规拒绝与坏 schema 拒绝回归，覆盖当前共享子集的接受 / 失败路径
- `GFramework.SourceGenerators/Config/SchemaConfigGenerator.cs` 与 `ConfigSchemaDiagnostics` 新增 `format` 元数据校验和 XML 文档输出；`GF_ConfigSchema_009` 会在生成阶段拒绝未纳入共享子集的字符串格式
- `GFramework.SourceGenerators.Tests/Config/SchemaConfigGeneratorTests.cs` 新增 `format` 文档输出与坏 schema 诊断回归；`AnalyzerReleases.Unshipped.md` 已登记新规则，避免 Roslyn release tracking 警告
- `tools/gframework-config-tool/src/configValidation.js`、`extension.js`、`localization.js` 与 `localizationKeys.js` 同步补齐 `format` 解析、诊断、本地化文案和表单 hint；`configValidation.test.js` 新增校验 / 元数据回归
- `docs/zh-CN/game/config-system.md` 已更新共享关键字清单、运行时拒绝路径和表单元数据说明，明确当前 `format` 只支持稳定子集
- 补齐 `format` 剩余 schema 级分支回归：`YamlConfigLoaderTests` 新增“非字符串节点声明 format”与“format 不是字符串值”的 `SchemaUnsupported` 测试，避免后续重构时静默放宽 schema 约束
- `SchemaConfigGenerator` 现补上根节点与数组 `contains` 子 schema 的 `format` 校验，防止同一份 schema 在生成器与运行时/工具侧出现接受范围漂移；`SchemaConfigGeneratorTests` 新增对应的 `GF_ConfigSchema_009` 诊断回归
- `tools/gframework-config-tool/src/configValidation.js` 现在会在任意非字符串 schema 节点声明 `format` 时直接抛错，不再静默忽略；`configValidation.test.js` 同步新增“非字符串节点”和“非字符串 format 值”回归
- `tools/gframework-config-tool/src/configValidation.js` 的日期校验不再依赖 `Date.UTC(...)`，改为显式年份边界、闰年和每月天数判断，修复 `0000-xx-xx` 与低位年份在 JavaScript 里的特殊年份归一化偏差；`configValidation.test.js` 同步补上 `0000-01-01` 的 `date` 回归
- `docs/zh-CN/game/config-system.md` 补充 `x-gframework-ref-table` 与 UI 展示名 `ref-table` 的对应说明，消除文档关键字命名歧义
- 已执行：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~YamlConfigLoaderTests.LoadAsync_Should_Accept_Supported_String_Format|FullyQualifiedName~YamlConfigLoaderTests.LoadAsync_Should_Throw_When_String_Does_Not_Match_Supported_Format|FullyQualifiedName~YamlConfigLoaderTests.LoadAsync_Should_Throw_When_String_Format_Is_Not_Supported"`
- 已执行：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Write_Supported_String_Format_Into_Generated_Documentation|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_String_Format_Is_Not_Supported"`
- 已执行：`node --test tools/gframework-config-tool/test/configValidation.test.js`
- 已执行：`node --test tools/gframework-config-tool/test/localization.test.js`
- 已执行：`node --check tools/gframework-config-tool/src/extension.js`
- 已执行：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~YamlConfigLoaderTests.LoadAsync_Should_Throw_When_Format_Is_Used_On_Non_String_Property|FullyQualifiedName~YamlConfigLoaderTests.LoadAsync_Should_Throw_When_Format_Is_Not_A_String"`
- 已执行：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_Root_Node_Uses_Format|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_Contains_Schema_Uses_Format_On_Non_String_Node|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_String_Format_Is_Not_Supported"`
- 已执行：`node --check tools/gframework-config-tool/src/configValidation.js`
- 尝试执行：`bash scripts/validate-csharp-naming.sh`
- 结果：脚本在当前 WSL + Windows worktree 环境下仍命中 Git 路径翻译错误；本轮未完成该项校验，后续如需跑该脚本，应继续按仓库约定通过宿主 Windows Git 显式绑定脚本内的 `git` 调用

### 2026-04-10

- Runtime / Generator / Tooling 共享新增 `const` 关键字支持
- `GFramework.Game/Config/YamlConfigSchemaValidator.cs` 现在会在 schema 解析阶段预归一化 `const`，并对标量、对象、数组统一复用稳定比较键做运行时校验
- `YamlConfigLoaderTests` 新增标量、数组与嵌套对象 `const` 回归用例，覆盖固定值、固定序列和固定对象结构的拒绝路径
- `SchemaConfigGenerator` 现在会把 `const` 写入生成 XML 文档约束说明，`MonsterConfig.g.txt` 快照已更新
- `tools/gframework-config-tool/src/configValidation.js` 现在会解析 `const` 元数据，并在 VS Code 校验中按与运行时一致的对象 / 数组 / 标量比较语义给出诊断
- `tools/gframework-config-tool/src/extension.js` 与本地化文本已补齐 `const` hint 展示；标量字段在 YAML 缺值时会优先回填 schema 固定值
- `docs/zh-CN/game/config-system.md` 已更新共享关键字清单、运行时拒绝路径和表单元数据说明
- 已执行：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~YamlConfigLoaderTests"`
- 已执行：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~SchemaConfigGeneratorSnapshotTests"`
- 已执行：`cd tools/gframework-config-tool && bun run test`
- 下一恢复点：在 `const` 收敛后继续评估下一批共享关键字时，优先比较 `contains / minContains / maxContains` 和 `format` 的投入产出，而不是回到工具侧深层 UI 扩展
- Runtime / Generator / Tooling 对 `const` 的边界行为进一步对齐：运行时允许空对象 `const: {}`，生成器不再忽略空字符串 `const: ""`，VS Code 工具的字符串 `const` 诊断与 hint 改为保留 JSON 风格展示值
- `tools/gframework-config-tool/src/configValidation.js` 新增 ordinal 字符串比较辅助，替换对象 `const`、对象值 comparable key 以及批量字段列表中的 `localeCompare(...)`，避免与运行时 `string.CompareOrdinal(...)` 在非 ASCII 键名上出现排序语义漂移
- `tools/gframework-config-tool/src/extension.js` 改为对 `constValue` 使用 `??` / `!== undefined` 分支，避免空字符串固定值被错误回退到 `defaultValue` 或被 hint 渲染逻辑跳过
- `YamlConfigLoaderTests` 新增空对象 `const` 成功加载回归测试；`SchemaConfigGeneratorTests` 新增空字符串 `const` XML 文档回归测试；`tools/gframework-config-tool/test/configValidation.test.js` 新增 ordinal 排序、空字符串 `const` 原始值/展示值与示例 YAML 回归测试
- 已执行：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~YamlConfigLoaderTests.LoadAsync_Should_Accept_Empty_Object_Schema_Const|FullyQualifiedName~YamlConfigLoaderTests.LoadAsync_Should_Throw_When_Nested_Object_Does_Not_Match_Schema_Const|FullyQualifiedName~YamlConfigLoaderTests.LoadAsync_Should_Throw_When_Scalar_Value_Does_Not_Match_Schema_Const"`
- 已执行：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Preserve_Empty_String_Const_In_Generated_Documentation"`
- 已执行：`cd tools/gframework-config-tool && bun run test`
- 已执行：`node --check tools/gframework-config-tool/src/extension.js`
- 补齐 `contains` 剩余运行时契约分支：`YamlConfigLoaderTests` 新增 `contains` 非对象、嵌套数组 `contains` 的 `SchemaUnsupported` 回归，以及 `matchingCount == minContains == maxContains` 的成功边界回归
- 修正 `YamlConfigSchemaValidator` 在 `contains` 试匹配路径中遗漏引用收集的问题：匹配成功的 `contains` 子树现在会把 `x-gframework-ref-table` 使用量写回 collector，同时 `CollectReferencedTableNames(...)` 也会递归到 `ArrayConstraints.ContainsConstraints.ContainsNode`
- 新增 `contains + ref-table` 运行时回归：初次加载会拒绝仅声明在 `contains` 子 schema 里的缺失目标引用；热重载也会把这类目标表纳入依赖闭包并在依赖破坏时整体回滚
- `tools/gframework-config-tool/src/extension.js` 将 `contains` 摘要抽到 `containsSummary.js`，改为复用现有本地化 hint 文案，避免中文界面出现 `const / enum / pattern / ref / item` 的英文硬编码
- `tools/gframework-config-tool/test/containsSummary.test.js` 新增摘要本地化回归，覆盖中文摘要与空摘要回退文案
- `YamlConfigSchemaValidator` 的 `contains` 试匹配现在会在对象节点上允许当前 `contains` 子树未声明的额外字段，避免对象数组使用“声明属性子集”匹配时被 `UnknownProperty` 误判；主加载链仍保持未知字段即失败
- `YamlConfigLoaderTests` 新增对象数组 `contains` 子集匹配成功回归，覆盖 `{ id: 1, weight: 2 }` 这类对象在 `contains` 只声明 `id` 时仍会计入匹配数
- `tools/gframework-config-tool/src/containsSummary.js` 新增 `buildContainsHintLines(...)`，让表单 hint 在仅声明 `contains` 时也显式展示运行时默认语义 `minContains = 1`
- `tools/gframework-config-tool/src/configValidation.js` 补齐与 C# runtime 对齐的 schema 级拒绝：拒绝 nested-array `contains`，并在 `contains` 存在时按 `effectiveMinContains` 校验反转的 `minContains` / `maxContains`
- `tools/gframework-config-tool/test/configValidation.test.js` 与 `containsSummary.test.js` 新增默认 `minContains = 1`、nested-array `contains`、`maxContains: 0` 与显式反转边界的 Node 回归

### 2026-03-30

- 完成设计文档重写并收敛为 `Runtime / Generator / Tooling` 三层
- 确认工具层 MVP 采用 VS Code Extension
- 开始 Runtime MVP 实现
- 在 `GFramework.Game.Abstractions/Config` 中新增 `IConfigTable`、`IConfigRegistry`、`IConfigLoader`
- 在 `GFramework.Game/Config` 中新增 `InMemoryConfigTable` 与 `ConfigRegistry`
- 在 `GFramework.Game/Config` 中新增 `YamlConfigLoader`，支持按目录注册 YAML 配置表
- 在 `GFramework.Game/GFramework.Game.csproj` 中新增 `YamlDotNet` 依赖
- 在 `GFramework.Game.Tests/Config` 中新增基础行为测试与 YAML loader 测试，共通过 11 个测试
- 已执行：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~Config"`
- 在 `GFramework.SourceGenerators/Config` 中新增 `SchemaConfigGenerator`，支持从 `*.schema.json` 生成 `Config` 类型与 `Table` 包装
- 在 `GFramework.SourceGenerators/Diagnostics` 中新增 `ConfigSchemaDiagnostics`
- 在 `GFramework.SourceGenerators/GFramework.SourceGenerators.csproj` 中新增 `System.Text.Json` 依赖
- 在 `GFramework.SourceGenerators.Tests/Config` 中新增 schema 生成器测试驱动、快照测试和诊断测试，共通过 2 个测试
- 已执行：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~SchemaConfig"`
- 在 `tools/gframework-config-tool` 中新增 VS Code 插件骨架
- 在 `tools/gframework-config-tool/src/extension.js` 中实现配置树、打开 raw/schema、基础 schema 校验与顶层标量表单预览
- 已执行静态校验：`node --check tools/gframework-config-tool/src/extension.js`
- 已执行静态校验：`jq empty tools/gframework-config-tool/package.json`
- 尚未在真实 VS Code 宿主中做交互式手工验证

### 2026-03-31

- 开始补齐面向正式可用的第一批缺口
- 已将 P0 项拆分为运行时 schema 校验、生成器接入自动化和消费者接入说明三个方向
- 在 `GFramework.Game/Config` 中新增 `YamlConfigSchemaValidator`，为 `YamlConfigLoader` 提供最小运行时 schema 校验
- `YamlConfigLoader` 新增绑定 schema 的 `RegisterTable` 重载
- 补充缺失必填字段、未知字段、标量类型和数组元素类型的回归测试
- `GeWuYou.GFramework.SourceGenerators.targets` 默认收集 `schemas/**/*.schema.json`
- 新增 `docs/zh-CN/game/config-system.md` 作为最小接入说明
- `YamlConfigLoader` 新增 `EnableHotReload(...)` 开发期热重载入口
- 热重载支持监听配置目录与绑定 schema 文件，并按表粒度刷新注册表
- 补充配置变更成功重载与 schema 变更失败保留旧表的回归测试
- 将 VS Code 插件中的纯校验逻辑拆分到独立模块，便于在 Node 环境中直接回归测试
- 新增 `tools/gframework-config-tool/test/configValidation.test.js`
- VS Code 插件校验补齐未知字段、数组元素类型和顶层 YAML 结构检查
- VS Code 表单编辑补齐顶层标量数组支持，复杂对象仍回退到 raw YAML

### 2026-04-01

- 在 `GFramework.Game/Config` 中扩展运行时 schema 校验，支持通过 `x-gframework-ref-table` 声明跨表引用
- `YamlConfigLoader` 在初次加载与热重载时都会执行跨表引用校验，并在依赖表变更导致引用失效时整体回滚受影响表
- `IConfigRegistry` / `ConfigRegistry` 新增弱类型 `TryGetTable` 入口，供运行时跨表校验读取目标表元数据
- 在 `GFramework.Game.Tests/Config` 中补充跨表引用成功、缺失目标、数组引用与热重载回滚回归测试
- 更新 `docs/zh-CN/game/config-system.md`，补充跨表引用 schema 扩展和热重载行为说明
- `tools/gframework-config-tool` 新增按配置域批量编辑入口，可对多份 YAML 统一写入顶层标量字段和标量数组
- 在 `tools/gframework-config-tool/test` 中补充批量编辑辅助逻辑测试
- 运行时 schema 校验新增标量 `enum` 与数组元素 `enum` 约束
- VS Code 插件新增对 `title`、`description`、`default`、`enum` 与 `x-gframework-ref-table` 的元数据展示
- Source Generator 将 schema 元数据写入生成类型 XML 文档，并在可安全映射时把 `default` 转成属性初始值
- 完成独立 `Config Studio` 评估：当前阶段维持 `VS Code Extension` 为主，不建议额外启动桌面工具项目
- 运行时 schema 校验升级为递归模型，支持嵌套对象、对象数组和深层必填 / 未知字段 / enum 校验
- `SchemaConfigGenerator` 升级为生成嵌套配置类型，支持对象属性和对象数组项的强类型代码生成
- VS Code 插件校验升级为递归模型，支持嵌套对象和对象数组结构诊断
- VS Code 表单入口支持嵌套对象字段编辑
- VS Code 表单入口补齐对象数组编辑，支持新增 / 删除对象项，以及对象项中的标量、标量数组和嵌套对象字段写回
- 对象数组编辑新增结构化写回测试；更深层对象数组嵌套仍继续保持 raw YAML 回退
- 在 `GFramework.Game.Tests`、`GFramework.SourceGenerators.Tests` 和 `tools/gframework-config-tool/test` 中补充递归 schema 子集回归测试

### 2026-04-02

- 收敛 VS Code 插件内部的逻辑路径辅助，统一 `joinPropertyPath / joinArrayIndexPath / joinArrayTemplatePath` 的使用
- 为校验消息引入共享 `ValidationMessageKeys`，减少本地化 key 漂移
- 放宽 YAML 注释提取与最小 YAML 解析对复杂 key 的支持，覆盖带引号、短横线和空格的 key
- 中文本地化调整为：简体中文使用 `zh-CN` 字典，繁体中文语言环境暂回退英文，避免错误显示简体文本
- “从 schema 初始化” 改为覆盖前弹出确认，避免静默丢失尚未保存的表单修改
- 在 `tools/gframework-config-tool/test` 中补充复杂 key 与繁中回退回归测试
- 新增 `ai-plan/public/ai-first-config-system/todos/ai-first-config-system-csharp-experience-next.md`，作为下一阶段 C# 体验主清单

### 2026-04-03

- `SchemaConfigGenerator` 新增生成注册/访问辅助代码：为每个 schema 产出 `YamlConfigLoader` 注册扩展、`IConfigRegistry` 强类型访问扩展，以及表名 / 配置目录 / schema 路径常量
- `docs/zh-CN/game/config-system.md` 更新为优先使用生成辅助的接入方式，减少消费端手写字符串和 key selector
- `GFramework.SourceGenerators.Tests/Config` 快照测试新增 `MonsterConfigBindings.g.cs` 校验，并补充最小运行时 stub
- 已执行：`dotnet build GFramework.SourceGenerators/GFramework.SourceGenerators.csproj -c Release`
- 已执行：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~SchemaConfig"`
- `GFramework.Game.Abstractions/Config` 新增 `ConfigLoadFailureKind`、`ConfigLoadDiagnostic` 与 `ConfigLoadException`，为配置加载失败提供稳定的结构化诊断字段
- `YamlConfigLoader` 与 `YamlConfigSchemaValidator` 统一改为抛出 `ConfigLoadException`，覆盖目录缺失、schema 读取/解析、字段级 schema 校验、反序列化和跨表引用失败
- `YamlConfigLoaderTests` 新增对 `FailureKind / TableName / YamlPath / SchemaPath / DisplayPath / ReferencedTableName / RawValue` 的断言，热重载失败回调也改为验证结构化诊断
- `docs/zh-CN/game/config-system.md` 补充 `ConfigLoadException.Diagnostic` 的使用方式，说明热重载失败回调如何读取结构化字段
- 已执行：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~YamlConfigLoaderTests"`
- `GFramework.Game.Tests` 现在通过仓库内 `.targets` 自动拾取 `schemas/**/*.schema.json`，并新增真实消费者端到端测试，覆盖生成代码编译、`YamlConfigLoader` 注册辅助、运行时加载和 `IConfigRegistry` 强类型访问
- Runtime / Generator / Tooling 共享新增 `minimum`、`maximum`、`minLength`、`maxLength` 子集支持：运行时与 VS Code 校验会拒绝违反范围/长度约束的值，生成代码 XML 文档会同步暴露这些约束
- `SchemaConfigGenerator` 进一步将 `*ConfigBindings` 中的元数据收敛为 `Metadata` 容器，并补充 `ConfigDomain` 常量，同时保留顶层 `TableName / ConfigRelativePath / SchemaRelativePath` 兼容别名
- `GeneratedConfigConsumerIntegrationTests` 与 `SchemaConfigGenerator` 快照测试新增对 `Metadata.*` 与 `ConfigDomain` 的回归覆盖
- `docs/zh-CN/game/config-system.md` 补充统一读取 `MonsterConfigBindings.Metadata` 的推荐写法，减少后续消费者继续分散引用裸常量
- 已执行：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~SchemaConfigGeneratorSnapshotTests"`
- 已执行：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~GeneratedConfigConsumerIntegrationTests"`
- `SchemaConfigGenerator` 新增生成期跨表引用辅助：`*ConfigBindings` 现在会生成 `ReferenceMetadata`、`References.All`、按字段路径命名的引用元数据成员，以及 `TryGetByDisplayPath(...)`
- `SchemaConfigGenerator` 快照测试补充对象数组内引用字段覆盖，验证生成代码会暴露 `dropItems` 与 `phases[].monsterId` 这类引用路径
- `GeneratedConfigConsumerIntegrationTests` 增加对空引用集合与 `TryGetByDisplayPath(...)` 的编译期回归，确保没有 ref-table 的普通 schema 也具备稳定 API
- `docs/zh-CN/game/config-system.md` 补充如何通过 `MonsterConfigBindings.References` 读取引用目标表、值类型和是否为集合
- 已执行：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~SchemaConfigGeneratorSnapshotTests"`
- 已执行：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~GeneratedConfigConsumerIntegrationTests"`

### 2026-04-09

- 为 `GFramework.Game/Config/YamlConfigSchemaValidator.cs` 做 SonarQube 可维护性清理，拆分 `BuildComparableNodeValue(...)` 与 `ValidateScalarConstraints(...)` 的递归 / 约束校验分支，降低主方法认知复杂度
- `YamlConfigScalarConstraints` 改为聚合 `YamlConfigNumericConstraints` 与 `YamlConfigStringConstraints`，避免继续使用 9 参数构造函数
- `YamlConfigSchemaNode` 改为通过 `CreateObject(...)`、`CreateArray(...)` 与 `CreateScalar(...)` 命名工厂创建，避免继续通过多语义长参数构造函数拼装不同节点模式
- 更新 `AGENTS.md`：补充 SonarQube 复杂度 / 长参数处理规范，并记录在当前 WSL + Windows worktree 环境下优先使用 Windows `git`（如 `git.exe`）的仓库约定
- 修正 `multipleOf` 的判定实现：运行时与 JS 工具优先按十进制字面量做精确整倍数判断，仅在无法精确归一化时才退回浮点容差兜底，避免同时出现大数十进制步进误拒和大数量级非整倍数误放
- `YamlConfigLoaderTests` 新增大数十进制步进回归用例，覆盖 `10000000.2` 配合 `multipleOf: 0.1` 时运行时会接受应当合法的十进制整倍数
- `YamlConfigLoaderTests` 新增大数量级非整倍数回归用例，覆盖 `1000000000000.4` 配合 `multipleOf: 1` 时运行时仍会拒绝明显非法输入的场景
- `tools/gframework-config-tool/test/configValidation.test.js` 新增对应的 VS Code 工具回归用例，确保编辑器侧与运行时共享同一组大数 `multipleOf` 边界行为
- 更新 `AGENTS.md`：新增单文件默认应控制在约 800-1000 行内的规则；超过该范围时必须先检查是否应按职责拆分
- 已执行：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~YamlConfigLoaderTests"`
- 下一恢复点：继续沿清理后的 validator 结构扩展下一批 JSON Schema 关键字时，优先复用现有“按语义拆分 helper / 分组约束对象 / 命名工厂”的模式，避免再次累积 SonarQube 复杂度与长参数问题
- `docs/zh-CN/game/config-system.md` 新增“推荐接入模板”，补齐项目目录布局、仓库内 `csproj` 引用模板、`GFrameworkConfigSchemaDirectory` 覆盖方式、初始化入口、强类型读取入口和开发期热重载模板
- 文档中的热重载示例改为优先使用 `RegisterMonsterTable()`，避免推荐方案与示例重新回到手写字符串注册
- `GFramework.Game/Config` 新增 `YamlConfigTableRegistrationOptions<TKey, TValue>` 与 `YamlConfigHotReloadOptions`，为 `RegisterTable(...)` 和 `EnableHotReload(...)` 提供稳定的 options 入口
- `YamlConfigLoader` 现有重载全部委托到新的 options API，保留兼容调用方式，同时为未来新增加载开关预留统一扩展点
- `YamlConfigLoaderTests` 新增对 `RegisterTable(options)`、`EnableHotReload(options)` 和空 options 参数的回归覆盖
- `docs/zh-CN/game/config-system.md` 补充 `YamlConfigTableRegistrationOptions` 与 `YamlConfigHotReloadOptions` 的推荐用法
- 已执行：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~YamlConfigLoaderTests"`
- 已执行：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~GeneratedConfigConsumerIntegrationTests"`

### 2026-04-06

- `SchemaConfigGenerator` 为顶层非主键标量字段新增轻量查询辅助，生成 `FindBy*` 与 `TryFindFirstBy*` 入口，保持运行时仍基于 `All()` 线性扫描，不引入新索引契约
- `GFramework.SourceGenerators.Tests/Config` 补充查询辅助生成规则回归，明确断言主键、数组、对象和 ref-table 字段都不会生成 `FindBy*` / `TryFindFirstBy*`
- `GFramework.SourceGenerators.Tests/Config/snapshots/SchemaConfigGenerator/MonsterTable.g.txt` 更新为包含查询辅助的最新快照
- `GFramework.Game.Tests` 的消费者端到端 schema 新增 `faction` 字段，并补充生成查询辅助的运行时断言
- 新增 `ArchitectureConfigIntegrationTests`，验证在 `Architecture.OnInitialize()` 中注册 `ConfigRegistry`、执行 `YamlConfigLoader.LoadAsync(...)`，并通过生成的 `GetMonsterTable()` 读取配置
- `docs/zh-CN/game/config-system.md` 补充生成查询辅助说明，以及将 `ConfigRegistry + YamlConfigLoader + Register*Table()` 收敛为 `Architecture` 推荐接入模板
- 修正 `SchemaConfigGenerator` 中无参数字符串插值导致的分析器告警，避免生成器实现继续留下无意义的插值调用
- `ArchitectureConfigIntegrationTests` 的临时目录清理改为同时兜底 `IOException` 与 `UnauthorizedAccessException`，降低测试在不同文件系统环境下的偶发失败
- `SchemaConfigGenerator` 新增项目级聚合输出 `GeneratedConfigCatalog` 与 `RegisterAllGeneratedConfigTables()`，让消费者项目可以一行注册当前编译中全部生成表

### 2026-04-09（生命周期与同步桥接）

- `GameConfigModule` 现在会在安装前显式拒绝已离开 `ArchitecturePhase.None` 的架构，避免错过 `BeforeUtilityInit` 首载窗口
- `GameConfigModule.Install(...)` 现在会先完成无副作用阶段校验，再先注册 `IConfigRegistry` / 生命周期 utility、最后注册生命周期钩子；一旦进入注册阶段即把模块实例视为已消耗，避免任何不可回滚的部分安装失败后重复暴露 utility 或重复挂钩子
- `BootstrapInitializationHook` 的同步桥接改为 `InitializeAsync().ConfigureAwait(false).GetAwaiter().GetResult()`，并为 `GameConfigBootstrap.InitializeAsync()` 补充线程契约说明
- `GameConfigBootstrap`、`YamlConfigLoader` 与 `YamlConfigSchemaValidator` 的初始化异步链统一补充 `ConfigureAwait(false)`，降低在 Unity 主线程、UI 线程或自定义 `SynchronizationContext` 上的死锁风险
- `ArchitectureConfigIntegrationTests` 统一为 PascalCase 命名，并新增“阻塞同步上下文下通过真实架构生命周期桥接完成初始化”和“迟到安装失败不消耗模块实例”的回归测试
- 已执行：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~ArchitectureConfigIntegrationTests"`

### 2026-04-08（索引构建逻辑修复与聚合注册能力）

- 修复 `SchemaConfigGenerator` 生成的索引构建逻辑：`BuildLookupIndex<TProperty>` 现在会跳过运行时空 key，并在生成代码 XML 注释中说明这是为了避免 `Lazy<T>` 因格式错误配置而永久缓存异常
- 收紧生成器内部的索引查询空值守卫分类逻辑：不再依赖“只有 `string` 是引用类型”的静默假设，而是显式枚举当前支持的标量映射；未来新增标量若未同步分类，将在生成期直接失败
- 在 `GFramework.SourceGenerators.Tests/Config/SchemaConfigGeneratorTests.cs` 中新增定向回归，验证生成代码包含运行时空 key 防御逻辑与说明性注释
- 已执行：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~SchemaConfigGenerator"`
- 下一恢复点：如果后续扩展新的 schema 标量类型，需要同步更新 `RequiresIndexedLookupNullGuard(...)` 的分类分支，并补充对应生成回归测试
- `GeneratedConfigCatalog` 暴露 `Tables` 与 `TryGetByTableName(...)`，为启动、诊断和后续按域收敛提供稳定元数据目录
- 修正聚合注册入口遗漏自定义 comparer 的问题：新增 `GeneratedConfigRegistrationOptions`，让 `RegisterAllGeneratedConfigTables(...)` 可以按表转发 comparer，而不改变既有运行时查找语义
- `GeneratedConfigConsumerIntegrationTests` 去掉对 `GeneratedConfigCatalog.Tables.Count` 的全局数量锁定，改为只断言期望表项存在，避免随着测试项目新增 schema 而出现伪失败
- `GFramework.SourceGenerators.Tests/Config` 新增聚合注册目录回归测试，并新增 `GeneratedConfigCatalog.g.txt` 快照
- `GeneratedConfigConsumerIntegrationTests` 与 `ArchitectureConfigIntegrationTests` 改为优先走 `RegisterAllGeneratedConfigTables()`，验证聚合注册入口贯通真实消费者链路
- `docs/zh-CN/game/config-system.md` 更新为优先推荐 `RegisterAllGeneratedConfigTables()`，并补充 `GeneratedConfigCatalog` 的启动/诊断用法与 comparer 覆盖方式
- 已执行：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~SchemaConfig"`
- 已执行：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~GeneratedConfigConsumerIntegrationTests|FullyQualifiedName~ArchitectureConfigIntegrationTests"`
- `GeneratedConfigRegistrationOptions` 新增 `IncludedConfigDomains`、`IncludedTableNames` 与 `TableFilter`，让 `RegisterAllGeneratedConfigTables()` 可以按域、按表名或按调用方谓词筛选要注册的 schema 子集
- `GeneratedConfigRegistrationExtensions` 聚合注册流程改为先执行允许列表和谓词判断，再按表调用对应 `Register*Table(...)`，保持既有无筛选调用兼容
- `GFramework.Game.Tests/schemas` 新增第二张 `item` schema，用真实消费者项目验证“项目中存在多张生成表，但当前启动仅注册一部分”的场景
- `GeneratedConfigConsumerIntegrationTests` 新增按配置域、表名和谓词筛选聚合注册的回归测试，并补充多表项目的加载断言

### 2026-04-09（bootstrap 与 schema 子集）

- `GFramework.Game/Config` 新增正式运行时入口 `GameConfigBootstrap` 与 `GameConfigBootstrapOptions`，把 `ConfigRegistry`、`YamlConfigLoader`、初次 `LoadAsync` 和热重载句柄收敛到单个生命周期对象
- `GameConfigBootstrap` 提供 `InitializeAsync()`、`StartHotReload()`、`StopHotReload()` 与共享 `Registry` / `Loader` 访问，避免消费者继续复制文档模板实现
- `GFramework.Game.Tests/Config` 新增 `GameConfigBootstrapTests`，覆盖共享注册表复用、初始化加载和显式热重载回写链路
- `ArchitectureConfigIntegrationTests` 改为在 `Architecture.OnInitialize()` 中使用 `GameConfigBootstrap`，并通过 `IConfigRegistry` utility 验证官方启动入口可以直接嵌入架构生命周期
- `docs/zh-CN/game/config-system.md` 更新为优先推荐 `GameConfigBootstrap` / `GameConfigBootstrapOptions`，并补充 `Architecture` 与热重载接入示例
- 已执行：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~GameConfigBootstrapTests|FullyQualifiedName~ArchitectureConfigIntegrationTests|FullyQualifiedName~GeneratedConfigConsumerIntegrationTests"`
- 已执行：`bash scripts/validate-csharp-naming.sh`
- Runtime / Generator / Tooling 共享新增 `pattern`、`minItems`、`maxItems`、`exclusiveMinimum`、`exclusiveMaximum` 子集支持：运行时与 VS Code 校验会拒绝模式、数组长度和开区间边界违规值，生成代码 XML 文档与表单 hint 同步暴露这些约束
- `YamlConfigSchemaValidator` 新增数组元素数量约束模型与字符串正则/开区间数值边界校验，`YamlConfigLoaderTests` 补充三类回归测试
- `SchemaConfigGenerator` 扩展 XML 文档约束输出，`MonsterConfig.g.txt` 快照更新为包含 `pattern`、`minItems/maxItems` 与 `exclusive*` 说明
- `tools/gframework-config-tool` 扩展 schema 解析、本地化诊断与表单 hint；`configValidation.test.js` 新增对模式、数组元素数量和开区间数值边界的回归覆盖
- 补齐上下界对称回归：`YamlConfigLoaderTests` 新增 `exclusiveMaximum`、`minItems` 与 regex backreference 用例，`configValidation.test.js` 额外覆盖 `exclusiveMaximum` / `maxItems`
- `GameConfigBootstrap` 生命周期改为单锁保护的原子状态提交：初始化和热重载启动只会在整步成功后才发布 `_loader` / `_hotReload`，并为并发进入与热重载启动失败补充回归测试
- `YamlConfigSchemaValidator` 移除 `RegexOptions.ExplicitCapture`，让运行时 `pattern` 与 JS 工具保持一致的分组/反向引用语义
- `tools/gframework-config-tool` 现在会在 schema 解析阶段显式拒绝非法 `pattern`，不再静默丢弃约束；`.github/workflows/ci.yml` 也新增该工具的 `bun run test`
- `docs/zh-CN/game/config-system.md` 同步更新共享 schema 子集与运行时校验行为说明
- 修正文档示例命名冲突：目录模板和生命周期包装示例统一改为 `GameConfigHost.cs` / `GameConfigHost`，保留 `GameConfigRuntime.cs` 作为只读访问门面，并补充二者组合使用示例
- Runtime / Generator / Tooling 共享新增 `multipleOf` 与 `uniqueItems` 子集支持：运行时会拒绝非整倍数数值与重复数组元素，VS Code 校验 / 表单 hint / 本地化诊断同步对齐，生成代码 XML 文档也会暴露这两类约束
- `YamlConfigSchemaValidator` 为标量约束模型新增 `multipleOf`，为数组约束模型新增 `uniqueItems`，并按 schema 归一化结构比较对象数组重复项
- `YamlConfigLoaderTests` 新增 `multipleOf` 与 `uniqueItems` 运行时回归测试
- `SchemaConfigGeneratorSnapshotTests` 与 `MonsterConfig.g.txt` 快照更新为包含 `multipleOf` / `uniqueItems` 文档输出
- `tools/gframework-config-tool/test/configValidation.test.js` 新增 `multipleOf` 与 `uniqueItems` 校验 / 元数据回归测试，`extension.js` 与 `localization.js` 同步补齐表单 hint 和诊断文案
- 已执行：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~YamlConfigLoaderTests"`
- 已执行：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~SchemaConfigGeneratorSnapshotTests"`
- 已执行：`bun run test`（`tools/gframework-config-tool`）
- 已执行：`node --check tools/gframework-config-tool/src/extension.js`
- 已执行：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~SchemaConfigGenerator"`
- 已执行：`node --test ./test/configValidation.test.js`（`tools/gframework-config-tool`）

### 2026-04-08（索引能力）

- `SchemaConfigGenerator` 新增 schema 元数据 `x-gframework-index`，允许对"顶层、必填、非主键、非引用标量字段"生成可选只读精确匹配索引
- 生成的 `FindBy*` / `TryFindFirstBy*` 对显式声明索引的字段会改为惰性构建只读 bucket 字典；未声明字段继续保持 `All()` 线性扫描契约
- `ConfigSchemaDiagnostics` 新增 `GF_ConfigSchema_008`，在 `x-gframework-index` 类型错误或落到不支持字段时提供稳定诊断
- `GFramework.SourceGenerators.Tests/Config` 新增索引元数据诊断测试，并扩展查询辅助生成测试断言索引字段只为显式 opt-in 的属性生成
- `GFramework.SourceGenerators.Tests/Config/snapshots/SchemaConfigGenerator/MonsterTable.g.txt` 更新为包含索引字段、惰性索引构建和索引化查询实现的最新快照
- `GFramework.Game.Tests/schemas/monster.schema.json` 与 `GeneratedConfigConsumerIntegrationTests` 同步把 `name`、`faction` 标记为 `x-gframework-index: true`，验证真实消费者链路继续贯通
- `docs/zh-CN/game/config-system.md` 补充 `x-gframework-index` 用法、约束范围和“索引字段自动走惰性只读索引、其他字段仍线性扫描”的语义说明
- 已执行：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~SchemaConfig"`
- 已执行：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~GeneratedConfigConsumerIntegrationTests"`
- 已执行：`bash scripts/validate-csharp-naming.sh`

### 2026-04-09（目录辅助与模块化接入）

- `GeneratedConfigCatalog` 继续补齐启动与诊断辅助，新增 `GetTablesInConfigDomain(...)`、`GetTablesForRegistration(...)` 与 `MatchesRegistrationOptions(...)`
- 聚合注册入口 `RegisterAllGeneratedConfigTables(...)` 改为复用 `GeneratedConfigCatalog.MatchesRegistrationOptions(...)`，让启动日志、诊断输出和真实注册路径共享同一套筛选逻辑
- `GFramework.Game.Tests/Config/GeneratedConfigConsumerIntegrationTests.cs` 新增目录筛选 / 启动诊断视图回归，验证按配置域枚举、按注册选项筛选和单条元数据匹配判断
- `GFramework.SourceGenerators.Tests/Config` 更新生成断言与 `GeneratedConfigCatalog.g.txt` 快照，覆盖新目录 API 和聚合注册实现细节
- `docs/zh-CN/game/config-system.md` 补充 `GeneratedConfigCatalog.GetTablesInConfigDomain(...)` 与 `GetTablesForRegistration(...)` 的推荐用法
- 根据 review 修正文档中的 `Architecture` 模板说明，明确 `.GetAwaiter().GetResult()` 只是同步桥接写法，并补充 `SynchronizationContext` 风险与适用前提
- `GeneratedConfigConsumerIntegrationTests` 追加 `TableFilter` 分支断言，确保目录诊断视图与真实聚合注册在谓词筛选路径上继续保持一致
- `GFramework.Game/Config` 新增 `GameConfigModule`，为 `Architecture` 宿主提供正式的配置模块入口
- `GameConfigModule` 安装时会注册 `IConfigRegistry` utility，并在 `BeforeUtilityInit` 通过 lifecycle hook 完成首次加载
- `GameConfigModule` 通过内部 context utility 跟随架构销毁自动释放 `GameConfigBootstrap` 与热重载句柄，同时保留 `StartHotReload(...)` / `StopHotReload()` 转发入口
- `ArchitectureConfigIntegrationTests` 改为优先验证 `GameConfigModule` 链路，并新增“依赖 utility 在初始化阶段直接读取配置”和“同一模块实例不可复用安装”的回归测试
- `docs/zh-CN/game/config-system.md` 更新为：`Architecture` 场景优先推荐 `GameConfigModule`，非 `Architecture` 场景继续直接使用 `GameConfigBootstrap`
- 已执行：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~SchemaConfigGenerator"`
- 已执行：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~GeneratedConfigConsumerIntegrationTests"`
- 已执行：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~ArchitectureConfigIntegrationTests|FullyQualifiedName~GameConfigBootstrapTests"`

### 2026-04-09（schema 校验对齐修正）

- `tools/gframework-config-tool/src/configValidation.js` 放宽 `number` 标量兼容判定，补齐科学计数法，并同步让 YAML 回写路径沿用同一组数值 / 布尔标量判定
- schema `pattern` 现在会在解析阶段以 Unicode `u` 模式编译并缓存，校验阶段直接复用已编译 `RegExp`，避免工具侧与运行时对 `\p{L}` 等 Unicode 模式语义漂移
- `uniqueItems` 校验改为跳过本轮已产生子项诊断的数组元素，避免 shape/type 错误再被额外误报成重复项；同时移除“首个重复即中断”的行为，一次返回全部重复诊断
- JS 与 C# 的 `uniqueItems` 比较键统一补齐长度前缀编码，避免对象值、数组元素或标量内容中包含分隔符时发生比较键碰撞
- `GFramework.Game/Config/YamlConfigSchemaValidator.cs` 新增共享私有入口 `ValidateCore(...)`，让 `Validate(...)` 不再为无引用收集场景额外分配临时 `List<YamlConfigReferenceUsage>`
- `GFramework.Game.Tests/Config/YamlConfigLoaderTests.cs` 新增科学计数法数值接受用例和 `uniqueItems` 比较键碰撞回归；`tools/gframework-config-tool/test/configValidation.test.js` 新增科学计数法、Unicode pattern、`uniqueItems` 跳过无效项、全量重复诊断与碰撞回归
- `docs/zh-CN/game/config-system.md` 收窄“批量编辑入口”能力描述，并补充 `pattern` 在 JS 工具侧按 Unicode `u` 模式解释的说明
- 已执行：`node --test tools/gframework-config-tool/test/configValidation.test.js`
- 已执行：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~YamlConfigLoaderTests"`

### 2026-04-10

- Runtime / Generator / Tooling 共享新增 `minProperties` 与 `maxProperties` 子集支持：运行时会拒绝对象属性数量越界，VS Code 校验与对象 section 表单 hint 同步对齐，生成代码 XML 文档也会暴露对象级约束
- `YamlConfigSchemaValidator` 新增对象属性数量约束模型与运行时校验路径，`YamlConfigLoaderTests` 补充嵌套对象 `minProperties` / `maxProperties` 回归测试
- `SchemaConfigGenerator` 为根配置类型与嵌套对象类型补充对象级约束文档输出，`MonsterConfig.g.txt` 快照更新为包含 `minProperties` / `maxProperties` 说明
- `tools/gframework-config-tool/src/configValidation.js`、`extension.js` 与 `localization.js` 同步补齐对象属性数量诊断、本地化文案和表单 hint；`configValidation.test.js` 新增校验 / 元数据回归
- `docs/zh-CN/game/config-system.md` 更新共享 schema 子集、运行时拒绝路径和表单元数据说明，记录对象级关键字的当前行为
- 根据 review 修正工具链诊断路由：`expectedObject` 继续保留专用格式化入口，`minProperties` / `maxProperties` 重新补齐到 `localization.js` 并优先通过 `localizer.t(...)` 分发，避免 validation key 与本地化字典脱节
- 根据 review 修正 `configValidation.js` 的对象属性数量统计，`minProperties` / `maxProperties` 现在按去重后的对象 key 计数，避免重复 YAML key 造成假阳性或假阴性
- 根据 review 修正 `SchemaConfigGenerator` 的 `required` 大小写语义为 `StringComparer.Ordinal`，与运行时 validator 及 JSON Schema 的大小写敏感约定保持一致
- `YamlConfigSchemaValidator` 针对对象级坏 schema 的诊断改为在根对象场景输出 `Root object ...`，避免出现 `Property ''` 之类的空路径文本；`YamlConfigLoaderTests` 额外补齐非法 `minProperties` / `maxProperties` 与倒置范围的坏 schema 回归
- 根据 review 补齐 `tools/gframework-config-tool/test/configValidation.test.js` 的 `const` 成功路径、对象键归一化 / 数组顺序、整数与布尔固定值，以及 `createSampleConfigYaml` 优先使用 `const` 的回归覆盖
- `AGENTS.md` 里的 WSL 宿主 Git 说明改为通用表述：当 Linux `git` 命中 worktree 路径翻译错误时，先让 shell 解析到宿主 Windows Git，再为当前会话显式绑定后续 Git 命令
- 已执行：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~YamlConfigLoaderTests"`
- 已执行：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~SchemaConfigGeneratorSnapshotTests"`
- 已执行：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~SchemaConfigGenerator"`
- 已执行：`node --test tools/gframework-config-tool/test/configValidation.test.js`
- 已执行：`node --test tools/gframework-config-tool/test/localization.test.js`
- 已执行：`node --check tools/gframework-config-tool/src/extension.js`
- Runtime / Generator / Tooling 共享新增 `contains`、`minContains` 与 `maxContains` 子集支持：运行时会按同一套递归 schema 规则统计匹配 `contains` 子 schema 的数组元素数量，VS Code 校验 / 表单 hint / 本地化诊断同步对齐，生成代码 XML 文档会输出紧凑的 contains 摘要
- `YamlConfigSchemaValidator` 新增数组 contains 约束模型与运行时校验路径，`YamlConfigLoaderTests` 补充默认 contains 最少 1 个匹配、显式 `minContains` / `maxContains`、缺失 `contains` 与倒置范围的回归测试
- `SchemaConfigGenerator` 为数组字段 XML 文档新增 `contains = ...`、`minContains` 与 `maxContains` 说明，`MonsterConfig.g.txt` 快照更新为包含数组 contains 约束摘要
- `tools/gframework-config-tool/src/configValidation.js`、`extension.js`、`localization.js` 与 `localizationKeys.js` 同步补齐数组 contains 匹配计数诊断、本地化文案和表单 hint；`configValidation.test.js` 与 `localization.test.js` 新增校验 / 元数据 / 本地化回归
- `docs/zh-CN/game/config-system.md` 更新共享 schema 子集、运行时拒绝路径和表单元数据说明，记录 `contains` / `minContains` / `maxContains` 的当前行为
- 已执行：`dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~YamlConfigLoaderTests"`
- 已执行：`dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~SchemaConfigGenerator"`
- 已执行：`node --test tools/gframework-config-tool/test/configValidation.test.js`
- 已执行：`node --test tools/gframework-config-tool/test/localization.test.js`
- 已执行：`node --check tools/gframework-config-tool/src/extension.js`
- 已执行：`bash scripts/validate-csharp-naming.sh`

### 下次恢复建议

- 当前 C# 主线已推进到：单表注册辅助、强类型访问、结构化诊断、查询辅助、`Architecture` 推荐接入路径、项目级聚合注册目录、按域/按表筛选的聚合注册、目录级启动诊断辅助、官方 `GameConfigBootstrap` 生命周期帮助器，以及 `Architecture` 宿主专用的 `GameConfigModule` 均已落地
- 本轮已补齐：`pattern`、`minItems`、`maxItems`、`exclusiveMinimum`、`exclusiveMaximum`
- 本轮额外补齐：可选只读索引 `x-gframework-index`、`GeneratedConfigCatalog` 的目录筛选 / 启动诊断辅助，以及 `GameConfigModule` 模块化接入入口
- 最新补齐：数组级 `contains` / `minContains` / `maxContains`
- 最新补齐：稳定字符串 `format` 子集 `date` / `date-time` / `email` / `uri` / `uuid`
- 下次优先项：在当前稳定 `format` 子集落地后，继续评估是否补 `time` / `duration` 等剩余 format 家族成员，或转向下一批非 format 关键字；仍然优先 Runtime / Generator / VS Code 校验三端共同收益，而不是先扩工具 UI
- 恢复时优先检查：
  - `GFramework.Game/Config/YamlConfigSchemaValidator.cs`
  - `GFramework.SourceGenerators/Config/SchemaConfigGenerator.cs`
  - `tools/gframework-config-tool/src/configValidation.js`
  - `GFramework.Game.Tests/Config/YamlConfigLoaderTests.cs`
  - `docs/zh-CN/game/config-system.md`