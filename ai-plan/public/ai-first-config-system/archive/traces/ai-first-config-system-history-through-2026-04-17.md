# AI-First Config System 执行 Trace

## 2026-04-19

### 阶段

- 主线：把当前 worktree 的旧本地恢复文档收口到 `ai-plan/public/ai-first-config-system/`
- 子任务：补齐公共索引映射，并把可提交文档中的本地环境痕迹清洗为治理允许的公共表述
- 当前恢复点：`ai-plan-public-migration`

### 关键决策

- 当前分支 `feat/ai-first-config` 的恢复文档应进入公共主题目录，而不是继续停留在 worktree 根下的旧本地恢复目录
- `ai-plan/public/README.md` 只登记当前 worktree 实际需要恢复的功能主题，因此本 worktree 只映射到 `ai-first-config-system`
- 原本地恢复文档中的绝对路径与宿主环境细节不再直接迁入公共区，统一改写为仓库相对路径或抽象环境描述

### 实施记录

- 复制旧 tracking / next / trace 文档到 `ai-plan/public/ai-first-config-system/` 对应目录
- 在公共 tracking 中补充迁移说明，并把旧目录路径引用改写为新的 `ai-plan/public/ai-first-config-system/` 路径
- 清洗公共 tracking 中的宿主 Windows NuGet fallback 绝对路径和 `GIT_DIR` / `GIT_WORK_TREE` 形式的本地命令细节
- 在公共索引中新增 `ai-first-config-system` 活跃主题，并将当前分支登记到对应 worktree 映射

### 下一步

- 后续继续在 `ai-plan/public/ai-first-config-system/` 下维护 C# 主线恢复点，不再回写到 worktree 根目录
- 下一轮功能恢复时，优先评估 `if` / `then` / `else` 等仍不改变生成形状的关键字

## 2026-04-17

### 阶段

- 主线：`C# Runtime + Source Generator + Consumer DX`
- 子任务：补齐不改变生成类型形状的下一批 JSON Schema 共享关键字
- 当前恢复点：`allOf-object-focused`

### 关键决策

- 本轮选择 `allOf`，但收敛为 object-focused 版本：只接受 object 节点上的 object-typed inline schema 数组。
- `allOf` 条目按 focused constraint block 语义叠加到当前对象，不做属性合并，不引入联合类型，也不改变生成代码形状。
- Runtime、Generator、Tooling 三端都统一拒绝非 object 节点声明 `allOf`，避免三端接受范围漂移。
- object-focused `allOf` 进一步约束为“只能引用父对象已声明字段”；由于当前不会做属性合并，因此把新字段只写进 `allOf` 会被视为不可满足 schema 并在解析阶段直接拒绝。
- 深层 `allOf` 诊断路径统一采用运行时格式 `reward[allOf[0]]`，避免 Runtime / Generator / Tooling 排错信息割裂。

### 实施记录

- Runtime：
  - `GFramework.Game/Config/YamlConfigSchemaValidator.cs` 新增 `allOf` 解析、对象约束匹配与引用递归采集。
  - `allOf` 匹配路径复用现有试匹配逻辑，并沿用 allow-unknown focused block 语义。
  - review 修正后，`allOf` 条目的 `required` / `properties` 还会复用父对象字段白名单，提前拒绝不可满足 schema。
  - review 继续收紧后，`allOf.properties` 若存在则必须是对象映射，`allOf.required` 若存在则必须是数组；Generator 不再静默跳过坏形状，Runtime 也会在 schema 解析阶段直接失败。
  - 本轮继续把对象关键字解析/校验抽到 `GFramework.Game/Config/YamlConfigSchemaValidator.ObjectKeywords.cs`，并把 `allOf.required` 的非字符串 / 空白项从“跳过”改成 `SchemaUnsupported`。
- Generator：
  - `GFramework.Game.SourceGenerators/Config/SchemaConfigGenerator.cs` 新增 `allOf` 递归元数据校验与 XML 文档摘要输出。
  - `GFramework.Game.SourceGenerators/Diagnostics/ConfigSchemaDiagnostics.cs` 新增 `GF_ConfigSchema_012`。
  - review 修正后，生成器会拒绝 `allOf` 引用父对象未声明字段，并统一深层 `allOf` 路径格式。
  - 本轮同步把 `allOf.required` 的非字符串 / 空白项改成 `GF_ConfigSchema_012`，避免生成器比 Runtime 更宽松。
- Tooling：
  - `tools/gframework-config-tool/src/configValidation.js` 新增 `allOf` 解析与校验。
  - `tools/gframework-config-tool/src/extension.js`、`localization.js`、`localizationKeys.js` 新增 `allOf` hint 和本地化诊断。
  - review 修正后，工具端也会拒绝 `allOf` 引入父对象未声明字段，并改为输出 `reward[allOf[0]]` 路径。
- Tests：
  - 新增 `GFramework.Game.Tests/Config/YamlConfigLoaderAllOfTests.cs`
  - 扩展 `GFramework.Game.Tests/Config/YamlConfigSchemaValidatorTests.cs`
  - 扩展 `GFramework.SourceGenerators.Tests/Config/SchemaConfigGeneratorTests.cs`
  - 扩展 `tools/gframework-config-tool/test/configValidation.test.js`
  - 扩展 `tools/gframework-config-tool/test/localization.test.js`
  - 本轮追加扩展 `GFramework.Game.Tests/Config/YamlConfigLoaderAllOfTests.cs` 与 `GFramework.SourceGenerators.Tests/Config/SchemaConfigGeneratorTests.cs`，锁住 `allOf.properties` / `allOf.required` 的坏形状、非字符串条目与空白字段名分支

### 验证

- `node --check tools/gframework-config-tool/src/configValidation.js`
- `node --check tools/gframework-config-tool/src/extension.js`
- `node --test tools/gframework-config-tool/test/configValidation.test.js`
- `node --test tools/gframework-config-tool/test/localization.test.js`
- `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release -p:RestoreFallbackFolders= --filter "FullyQualifiedName~YamlConfigLoaderAllOfTests|FullyQualifiedName~YamlConfigSchemaValidatorTests"`
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -p:RestoreFallbackFolders= --filter "FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Write_AllOf_Constraint_Into_Generated_Documentation|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_NonObject_Schema_Declares_AllOf|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_AllOf_Is_Not_An_Array|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_AllOf_Entry_Is_Not_Object_Typed"`
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -p:RestoreFallbackFolders= --filter "FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_AllOf_Entry_Is_Not_Object_Valued|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_AllOf_Entry_Targets_Undeclared_Parent_Property|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_With_Runtime_Aligned_Path_When_AllOf_Inner_Schema_Is_Invalid"`
- `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release -p:RestoreFallbackFolders= --filter "FullyQualifiedName~YamlConfigLoaderAllOfTests"`
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -p:RestoreFallbackFolders= --filter "FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Write_AllOf_Constraint_Into_Generated_Documentation|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_NonObject_Schema_Declares_AllOf|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_AllOf_Is_Not_An_Array|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_AllOf_Entry_Is_Not_Object_Valued|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_AllOf_Entry_Is_Not_Object_Typed|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_AllOf_Entry_Properties_Is_Not_Object_Valued|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_AllOf_Entry_Required_Is_Not_An_Array|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_AllOf_Entry_Targets_Undeclared_Parent_Property|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_With_Runtime_Aligned_Path_When_AllOf_Inner_Schema_Is_Invalid"`
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -p:RestoreFallbackFolders= --filter "FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Write_AllOf_Constraint_Into_Generated_Documentation|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_NonObject_Schema_Declares_AllOf|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_AllOf_Is_Not_An_Array|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_AllOf_Entry_Is_Not_Object_Valued|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_AllOf_Entry_Is_Not_Object_Typed|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_AllOf_Entry_Properties_Is_Not_Object_Valued|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_AllOf_Entry_Required_Is_Not_An_Array|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_AllOf_Entry_Required_Item_Is_Not_A_String|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_AllOf_Entry_Required_Item_Is_Blank|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_AllOf_Entry_Targets_Undeclared_Parent_Property|FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_With_Runtime_Aligned_Path_When_AllOf_Inner_Schema_Is_Invalid"`

### 当前状态

- `allOf-object-focused` 已完成并通过定向回归。
- 已知剩余噪音：`GFramework.Game.Tests` 仍存在既有 `GF_ContextRegistration_003` 告警，与本轮无关。
- 本轮 review 修复已验证通过：`YamlConfigLoaderAllOfTests` 9/9 通过，`SchemaConfigGeneratorTests` 定向 `allOf` 回归 9/9 通过。
- 本轮继续收紧与拆分后已验证通过：`YamlConfigLoaderAllOfTests` 11/11 通过，`SchemaConfigGeneratorTests` 定向 `allOf` 回归 11/11 通过。

### 下一步

- 继续评估下一批仍不改变生成类型形状的组合关键字，优先看 `if` / `then` / `else`。
- 如果切回工具体验，应聚焦复杂对象数组表单与批量编辑，而不是回头扩大运行时语义。
