# Analyzer Warning Reduction 追踪

## 2026-04-23 — RP-041

### 阶段：subagent 循环第五个有效写集（RP-041）

- 启动复核：
  - 在 `RP-040` 成功后，继续按单方法级 subagent 推进同一热点文件
  - 本轮目标收敛到 `Generates_Precise_Service_Type_For_Hidden_Generic_Type_Definitions()`
- 决策：
  - 仅提取该方法的内联 `source` 文本，继续复用现有
    `HiddenGenericEnvelopeResponseExpected`
  - 不改变 method name、expected 常量、生成文件名与断言语义
- 实施调整：
  - 新增类级常量 `HiddenGenericEnvelopeResponseSource`
  - 将目标测试方法改为复用该常量调用 `GeneratorTest<CqrsHandlerRegistryGenerator>.RunAsync(...)`
- 验证结果：
  - `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -t:Rebuild --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`10 Warning(s)`，`0 Error(s)`；原先位于行号 `680` 的 `MA0051` 已消失
- 当前结论：
  - subagent 循环已连续五轮产出 patch，但当前分支相对 `origin/main` 的唯一变更文件数依旧只有 `4`
  - 若用户坚持把“接近 `75` 个唯一变更文件”作为停止条件，后续策略必须从“继续收口同一文件”切换到“优先进入新文件写集”
- 下一步建议：
  - 主线程需要决定：继续按 warning 热点清理同一文件，还是切到新的文件写集以增加唯一变更文件数

## 2026-04-23 — RP-040

### 阶段：subagent 循环第四个有效写集（RP-040）

- 启动复核：
  - 在 `RP-039` 成功后，继续按单方法级 subagent 推进同一热点文件
  - 本轮目标收敛到 `Generates_Precise_Service_Type_For_Hidden_Array_Type_Arguments()`
- 决策：
  - 仅提取该方法的内联 `source` 文本，继续复用现有
    `HiddenArrayResponseFallbackExpected`
  - 不改变 method name、expected 常量、生成文件名与断言语义
- 实施调整：
  - 新增类级常量 `HiddenArrayResponseFallbackSource`
  - 将目标测试方法改为复用该常量调用 `GeneratorTest<CqrsHandlerRegistryGenerator>.RunAsync(...)`
- 验证结果：
  - `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -t:Rebuild --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`11 Warning(s)`，`0 Error(s)`；原先位于行号 `607` 的 `MA0051` 已消失
- 当前结论：
  - subagent 循环已经连续四轮产出 patch，但仍然只是在单个热点文件内收口 warning
  - 以“唯一变更文件数接近 `75`”作为停止条件，和当前单文件 warning 收口节奏存在明显张力
- 下一步建议：
  - 继续处理 `CqrsHandlerRegistryGeneratorTests.cs` 的下一处前半段热点：行号 `680`

## 2026-04-23 — RP-039

### 阶段：subagent 循环第三个有效写集（RP-039）

- 启动复核：
  - 在 `RP-038` 成功后，继续按单方法级 subagent 推进同一热点文件
  - 本轮目标收敛到 `Generates_Direct_Interface_Registrations_For_Hidden_Implementation_When_Handler_Interface_Is_Public()`
- 决策：
  - 仅提取该方法的内联 `source` 文本，继续复用现有
    `HiddenImplementationDirectInterfaceRegistrationExpected`
  - 不改变 method name、expected 常量、生成文件名与断言语义
- 实施调整：
  - 新增类级常量 `HiddenImplementationDirectInterfaceRegistrationSource`
  - 将目标测试方法改为复用该常量调用 `GeneratorTest<CqrsHandlerRegistryGenerator>.RunAsync(...)`
- 验证结果：
  - `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -t:Rebuild --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`12 Warning(s)`，`0 Error(s)`；原先位于行号 `536` 的 `MA0051` 已消失
- 当前结论：
  - subagent 循环在当前热点文件内已经连续三轮产出 patch，但唯一变更文件数仍停留在 `4`
  - 若用户坚持以“接近 `75` 个唯一变更文件”为停止条件，后续需要尽快从单文件热点转向新的文件写集
- 下一步建议：
  - 继续处理 `CqrsHandlerRegistryGeneratorTests.cs` 的下一处前半段热点：行号 `607`

## 2026-04-23 — RP-038

### 阶段：subagent 循环第二个有效写集（RP-038）

- 启动复核：
  - 在 `RP-037` 成功后，继续沿用“单方法级 subagent”节奏推进同一热点文件
  - 本轮目标收敛到 `Generates_Visible_Handlers_And_Self_Registers_Private_Nested_Handler_When_Assembly_Contains_Hidden_Handler()`
- 决策：
  - 仅提取该方法的内联 `source` 文本，继续复用现有 `HiddenNestedHandlerSelfRegistrationExpected`
  - 保持 method name、expected 常量、生成文件名和断言语义不变
- 实施调整：
  - 新增类级常量 `HiddenNestedHandlerSelfRegistrationSource`
  - 将目标测试方法改为复用该常量调用 `GeneratorTest<CqrsHandlerRegistryGenerator>.RunAsync(...)`
- 验证结果：
  - `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -t:Rebuild --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`13 Warning(s)`，`0 Error(s)`；原先位于行号 `454` 的 `MA0051` 已消失
- 当前结论：
  - subagent 循环已连续两轮产出稳定 patch，但仍只是在同一个文件内逐点消除 warning
  - 当前分支相对 `origin/main` 的唯一变更文件数仍远低于 `75`
- 下一步建议：
  - 继续处理 `CqrsHandlerRegistryGeneratorTests.cs` 的下一处前半段热点：行号 `536`

## 2026-04-23 — RP-037

### 阶段：subagent 循环首个有效写集（RP-037）

- 启动复核：
  - 在用户明确要求“循环调用 subagent 执行 `$gframework-boot`”后，先后尝试了多个 worker 边界；
    前几轮 subagent 只完成 boot 与热点确认，没有形成可验证 patch
  - 将 subagent 边界进一步压缩到单方法级别后，首个有效切片落在
    `CqrsHandlerRegistryGeneratorTests.cs` 的 `Generates_Assembly_Level_Cqrs_Handler_Registry()`
- 决策：
  - 接受当前 subagent 的最小可验证产出，而不是继续等待单轮覆盖多个 warning 点
  - 继续用“boot -> 单方法/小批量方法 -> 验证 -> 主线程记录恢复点”的节奏推进，以换取稳定吞吐
- 实施调整：
  - 为 `Generates_Assembly_Level_Cqrs_Handler_Registry()` 提取
    `AssemblyLevelCqrsHandlerRegistrySource` 与 `AssemblyLevelCqrsHandlerRegistryExpected`
    类级常量
  - 测试方法改为直接复用上述 fixture 调用 `GeneratorTest<CqrsHandlerRegistryGenerator>.RunAsync(...)`
  - 不改断言语义、生成文本、方法名或快照/文件名
- 验证结果：
  - `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -t:Rebuild --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`14 Warning(s)`，`0 Error(s)`；原先位于行号 `337` 的 `MA0051` 已消失
- 当前结论：
  - subagent 循环在该仓库里可以产生产出，但目前吞吐偏低；若目标是把分支唯一变更文件数推近 `75`，
    还需要很多轮独立切片
- 下一步建议：
  - 下一轮继续 `CqrsHandlerRegistryGeneratorTests.cs` 前半段方法，优先处理当前剩余的
    `454`、`536`、`607`、`680`

## 2026-04-23 — RP-036

### 阶段：`SchemaConfigGeneratorTests.cs` `MA0051` 收口（RP-036）

- 启动复核：
  - 依据 `RP-035` 的恢复结论，选择低风险单写集 `GFramework.SourceGenerators.Tests/Config/SchemaConfigGeneratorTests.cs`
    继续推进，而不直接跳入剩余 warning 数量更多的 `CqrsHandlerRegistryGeneratorTests.cs`
  - 先用 `dotnet build ... -clp:"Summary;WarningsOnly"` 复核当前热点，确认该文件仍承担 `7` 条 `MA0051`
- 决策：
  - 保持全部 schema 文本、断言字符串和生成文件名不变，只收敛测试方法结构
  - 将共享 consumer runtime fixture 提到类级常量，并把 generated-source 收集与 registration catalog 契约断言
    抽成 helper，避免重复内联样板把测试方法重新撑长
  - 继续避免并行运行同一测试项目的 build/test；该 worktree 下并发验证会触发 `MSB3030` / `CS0006` 级别的输出竞争噪音
- 实施调整：
  - 为 `SchemaConfigGeneratorTests` 新增 `DummySource` 与 `ConfigRuntimeSource` 类级常量
  - 新增 `RunAndCollectGeneratedSources(...)` 与 `AssertGeneratedRegistrationCatalogContract(...)` helper
  - 将 `if/then/else` 文档、config path、lookup index、reference metadata、query helper 与 project-level catalog
    等长测试方法切换为复用共享 fixture / helper
- 验证结果：
  - `dotnet restore GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -p:RestoreFallbackFolders="" -nologo`
    - 结果：通过
  - `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -t:Rebuild --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`15 Warning(s)`，`0 Error(s)`；`SchemaConfigGeneratorTests.cs` 已不再出现在 `MA0051` 列表中
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-build --disable-build-servers --filter FullyQualifiedName~SchemaConfigGeneratorTests -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`50 Passed`，`0 Failed`
- 下一步建议：
  - 继续进入 `GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs`，这是当前项目唯一剩余 `MA0051` 热点
  - 若需要再次做定向测试，保持串行执行，避免把共享输出竞争误判成实现回退

## 2026-04-23 — RP-035

### 阶段：`boot` 恢复点重建与实时热点复核（RP-035）

- 启动复核：
  - 按 `gframework-boot` 流程读取 `AGENTS.md`、`.ai/environment/tools.ai.yaml`、`ai-plan/public/README.md`
    与 `analyzer-warning-reduction` active tracking / trace，确认当前 worktree `GFramework-analyzer`
    仍对应分支 `fix/analyzer-warning-reduction-batch`
  - 额外检查 `ai-plan/private/`，确认当前 worktree 没有私有恢复上下文需要合并
  - 使用 `gframework-pr-review` 脚本抓取当前分支关联 PR，确认 `PR #273` 已为 `CLOSED`
- 决策：
  - 不再把已关闭 PR 上残留的 open thread 直接当作下一轮主驱动信号；先以当前本地代码和实时 build 结果判断问题是否仍成立
  - 将后续恢复入口从“继续看 `GeneratorSnapshotTest` / `ContextRegistrationAnalyzerTests`”切回
    “重新跑 `warnings-only` build 后按真实热点推进”
- 现场结论：
  - `GeneratorSnapshotTest` 中关于 snapshot 路径的 `Path.GetDirectoryName(...)` 已改为显式空值防御，
    相关 CodeRabbit 线程更像历史残留
  - `SchemaConfigGenerator` 在 `dependentSchemas` / `allOf` / conditional schema 校验 helper 周围已经补齐 XML 文档，
    当前也没有新的本地缺口需要仅为关闭旧线程而继续改动
  - `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -t:Rebuild --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    结果为 `22 Warning(s)`、`0 Error(s)`；剩余 `MA0051` 已集中到
    `CqrsHandlerRegistryGeneratorTests.cs`（`15` 条）与 `SchemaConfigGeneratorTests.cs`（`7` 条）
- 下一步建议：
  - 若保持低风险单写集，先进入 `SchemaConfigGeneratorTests.cs`
  - 若优先按 warning 数量收敛，则进入 `CqrsHandlerRegistryGeneratorTests.cs`

## 2026-04-23 — RP-033

### 阶段：`SchemaConfigGeneratorSnapshotTests.cs` `MA0051` 收口（RP-033）

- 启动复核：
  - 按 `gframework-boot` 流程恢复当前 worktree，读取 `AGENTS.md`、`.ai/environment/tools.ai.yaml`、
    `ai-plan/public/README.md` 与 active topic 跟踪文件，确认当前分支 `fix/analyzer-warning-reduction-batch`
    仍映射到 `analyzer-warning-reduction`
  - 用
    `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -t:Rebuild --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    复核当前 `MA0051` 热点，确认 `SchemaConfigGeneratorSnapshotTests.cs` 仍保留 1 个超长方法，适合作为单文件低风险写集
- 决策：
  - 保持 monster schema 场景的输入源码、schema 文本、生成文件名与快照目录不变，只收敛测试方法长度
  - 沿用前几轮 snapshot test 的收口策略：提取类级常量承载大段 fixture 输入，再用小 helper 封装生成结果映射与快照目录解析
  - 同一测试项目的 build/test 继续采用串行验证；并行执行会在 WSL worktree 上制造瞬时输出缺失，导致 `MSB3030` / `CS0006`
- 实施调整：
  - 为 `SchemaConfigGeneratorSnapshotTests` 新增 `RuntimeContractsSource` 与 `MonsterSchema` 类级常量，保留既有 monster 场景内容
  - 把生成结果字典构造拆到 `GenerateSourcesForMonsterSchema()`，把快照目录解析拆到 `GetSchemaSnapshotFolder()`
  - 保持 `AssertAllSnapshotsAsync(...)`、快照文件名与断言流程不变，不改生成器逻辑和 snapshot 资产
- 验证结果：
  - `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -t:Rebuild --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`39 Warning(s)`，`0 Error(s)`；`SchemaConfigGeneratorSnapshotTests.cs` 已不再出现在 `MA0051` 列表中
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-build --disable-build-servers --filter FullyQualifiedName~SchemaConfigGeneratorSnapshotTests -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`1 Passed`，`0 Failed`
- 下一步建议：
  - 若继续压缩 `GFramework.SourceGenerators.Tests` 的 `MA0051`，优先处理只剩单个超长方法的 `GeneratorSnapshotTest` 或
    `ContextRegistrationAnalyzerTests`
  - 若希望继续按 warning 数量收敛，则回到 `ContextGetGeneratorTests.cs`，但需要接受更大的单文件写集

## 2026-04-23 — RP-032

### 阶段：`AutoRegisterModuleGeneratorTests.cs` `MA0051` 收口（RP-032）

- 启动复核：
  - 按 `gframework-boot` 流程恢复当前 worktree，读取 `AGENTS.md`、`.ai/environment/tools.ai.yaml`、
    `ai-plan/public/README.md` 与 active topic 跟踪文件，确认当前分支 `fix/analyzer-warning-reduction-batch`
    仍映射到 `analyzer-warning-reduction`
  - 先用
    `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -t:Rebuild --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    复核当前 `MA0051` 热点，确认 `AutoRegisterModuleGeneratorTests.cs` 仍有 `3` 个超长方法，适合作为单文件低风险写集
- 决策：
  - 保持 `AutoRegisterModuleGeneratorTests` 的测试输入、生成文件名、快照文本与断言结构不变，只收敛方法长度
  - 采用“提取类级常量承载大段测试源码与期望输出”的方式，避免引入新的共享 helper 或改变场景组装顺序
  - 验证阶段改为串行执行 build/test；避免和同项目并行运行时拿到不完整 `bin/Release` 输出
- 实施调整：
  - 为 `AutoRegisterModuleGeneratorTests` 补齐测试类 XML 文档
  - 将 3 个长测试方法中的源码与期望快照提取为类级 `const string`，保留原有生成文件名与断言目标
  - 将仅转发 `GeneratorTest.RunAsync(...)` 的两个异步测试改为直接返回 `Task`
- 验证结果：
  - `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -t:Rebuild --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`40 Warning(s)`，`0 Error(s)`；`AutoRegisterModuleGeneratorTests.cs` 已不再出现在 `MA0051` 列表中
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --disable-build-servers --filter FullyQualifiedName~AutoRegisterModuleGeneratorTests -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`3 Passed`，`0 Failed`
- 下一步建议：
  - 若继续压缩 `GFramework.SourceGenerators.Tests` 的 `MA0051`，优先处理仅剩单个超长方法的
    `GFramework.SourceGenerators.Tests/Core/GeneratorSnapshotTest.cs`
  - 若希望单次继续多降几条 warning，则改选 `ContextGetGeneratorTests.cs`，但需要接受更大的单文件写集

## 2026-04-23 — RP-031

### 阶段：`LoggerGeneratorSnapshotTests.cs` `MA0051` 收口（RP-031）

- 启动复核：
  - 按 `gframework-boot` 流程恢复当前 worktree，读取 `AGENTS.md`、`.ai/environment/tools.ai.yaml`、
    `ai-plan/public/README.md` 与 active topic 跟踪文件，确认当前分支 `fix/analyzer-warning-reduction-batch`
    仍映射到 `analyzer-warning-reduction`
  - 结合 `RP-030` 的下一步建议与当前文件规模，优先选择 `GFramework.SourceGenerators.Tests/Logging/LoggerGeneratorSnapshotTests.cs`
    作为单文件、同构 snapshot 场景的低风险写集
- 决策：
  - 保持 `LoggerGenerator` 现有快照资产、场景命名与输入语义不变，只压缩测试方法和样板源码构造的结构复杂度
  - 先把重复场景统一为模板化 helper，再根据 analyzer 结果继续拆分 helper，直到 `LoggerGeneratorSnapshotTests.cs`
    不再出现在 `MA0051` 输出中
  - 验证阶段避免并行运行同一测试项目的 build/test，防止 WSL worktree 上的 `bin/Release` 文件占用噪音污染结果
- 实施调整：
  - 为 `LoggerGeneratorSnapshotTests` 补齐类与测试方法 XML 文档
  - 将 6 个 snapshot 场景改为统一调用 `RunScenarioAsync(...)`
  - 将原先重复内联的完整测试源码拆成 `CreateLoggingAttributeSource()`、
    `CreateLoggingContractsSource()`、`CreateLoggingRuntimeSource()` 与 `CreateTestAppSource(...)`
- 验证结果：
  - `dotnet restore GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -p:RestoreFallbackFolders="" -nologo`
    - 结果：通过
  - `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:Summary`
    - 结果：`43 Warning(s)`，`0 Error(s)`；`LoggerGeneratorSnapshotTests.cs` 已不再出现在 `MA0051` 列表中
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-build --disable-build-servers --filter FullyQualifiedName~LoggerGeneratorSnapshotTests -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`6 Passed`，`0 Failed`
- 下一步建议：
  - 若继续 `GFramework.SourceGenerators.Tests` 的 `MA0051` 治理，优先选择 `AutoRegisterModuleGeneratorTests` 或
    `GeneratorSnapshotTest` 作为下一批单写集
  - 若需要先压缩 warning 数量而不是单文件难度，可转向 `ContextGetGeneratorTests`，但应先明确本轮允许的文件数上限

## 2026-04-23 — RP-030

### 阶段：`GFramework.SourceGenerators.Tests` 低风险 `MA0004` / `MA0048` 收口（RP-030）

- 启动复核：
  - 按 `gframework-boot` 流程恢复当前 worktree 后，读取 `AGENTS.md`、`.ai/environment/tools.ai.yaml`、
    `ai-plan/public/README.md` 与 active topic 跟踪文件，确认当前分支 `fix/analyzer-warning-reduction-batch`
    仍映射到 `analyzer-warning-reduction`
  - 先对 `GFramework.SourceGenerators.Tests` 执行
    `dotnet restore GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -p:RestoreFallbackFolders="" -nologo`，
    刷新 Linux 侧 restore 资产，规避 Windows fallback package folder 干扰
  - 用
    `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    复核当前基线，确认该测试项目共有 `61` 条 warning，其中低风险切片集中在 `MA0004` 与单个 `MA0048`
- 决策：
  - 不直接进入大型 snapshot/test 方法的 `MA0051`，先收口纯 test-infrastructure 层的 `MA0004` / `MA0048`
  - 对“只是转发异步调用”的 helper 直接返回 `Task`，只在真实文件 I/O 上显式补 `ConfigureAwait(false)`，避免无意义的
    `async/await` 包装
  - 将 `AnalyzerTestDriver<TAnalyzer>` 所在文件改名为与类型一致，单独清理 `MA0048`，不改类型名与调用方契约
- 实施调整：
  - 将 `AnalyzerTestDriver.RunAsync(...)` 与 `GeneratorTest.RunAsync(...)` 改为直接返回下游 `Task`
  - 为 `GeneratorSnapshotTest`、`SchemaConfigGeneratorSnapshotTests` 与 `SchemaConfigGeneratorEnumTests` 中的异步文件读写
    显式补齐 `ConfigureAwait(false)`，并把仅作转发的测试方法改为直接返回 `Task`
  - 将 `GeneratorSnapshotTestSecurityTests` 的 `Assert.ThrowsAsync(...)` 改为直接返回目标 `Task`，移除无收益的
    `async` 包装
  - 将 `GFramework.SourceGenerators.Tests/Core/AnalyzerTest.cs` 重命名为
    `GFramework.SourceGenerators.Tests/Core/AnalyzerTestDriver.cs`
- 验证结果：
  - `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`49 Warning(s)`，`0 Error(s)`；当前项目已不再出现 `MA0004` / `MA0048`，剩余 warning 全部为 `MA0051`
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~GeneratorSnapshotTestSecurityTests|FullyQualifiedName~SchemaConfigGeneratorSnapshotTests|FullyQualifiedName~SchemaConfigGeneratorEnumTests" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`6 Passed`，`0 Failed`
- 下一步建议：
  - 若继续 analyzer warning reduction，继续把 `GFramework.SourceGenerators.Tests` 作为独立写集，只处理 `MA0051`
  - 下一轮优先选择单一测试域的同构长方法，例如 `LoggerGeneratorSnapshotTests`、`AutoRegisterModuleGeneratorTests`
    或共享 helper `GeneratorSnapshotTest`

## 2026-04-23 — RP-029

### 阶段：`SchemaConfigGenerator.cs` 剩余 `MA0051` 收口（RP-029）

- 启动复核：
  - 按 `gframework-boot` 流程恢复当前 worktree 后，先读取 `AGENTS.md`、`.ai/environment/tools.ai.yaml`、`ai-plan/public/README.md`
    与 active topic 跟踪文件，确认当前分支 `fix/analyzer-warning-reduction-batch` 仍映射到
    `analyzer-warning-reduction`
  - 用历史基线命令重新执行 `dotnet build GFramework.Game.SourceGenerators/GFramework.Game.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`，
    复现 `SchemaConfigGenerator.cs` 剩余 `9` 条 `MA0051`
- 决策：
  - 继续沿用“低风险结构拆分、不改诊断 ID、不改生成顺序、不改快照输出”的收口策略
  - 先把 schema 元数据校验方法拆成更小验证阶段，再把 `GenerateTableClass`、`GenerateBindingsClass` 与
    `AppendGeneratedConfigCatalogType` 的代码发射流程分段，避免直接改动生成文本内容
  - focused test 仍以 `SchemaConfigGenerator` 相关用例为主；`GFramework.SourceGenerators.Tests` 里既有测试项目 warning
    不纳入本轮写集
- 实施调整：
  - 为 `dependentRequired`、`dependentSchemas`、`allOf`、conditional schema 等对象级校验补上细粒度 helper，
    把 declared-properties 获取、分支校验、target 校验拆成独立阶段
  - 为生成代码头部、表包装、bindings metadata/references、catalog metadata 发射补充结构化 helper，
    将长方法按“头部 / 元数据 / 行为方法”拆分
  - 修正 `References` 代码发射 helper 的闭合范围，确保重构后的 `MonsterConfigBindings.g.cs` 与现有快照保持一致
  - 在构建阶段遇到 Linux `dotnet` 命中 Windows fallback package folder 时，先对
    `GFramework.Game.SourceGenerators` 与 `GFramework.SourceGenerators.Tests` 执行
    `dotnet restore -p:RestoreFallbackFolders=""`，再继续 `--no-restore` 验证
- 验证结果：
  - `dotnet restore GFramework.Game.SourceGenerators/GFramework.Game.SourceGenerators.csproj -p:RestoreFallbackFolders="" -nologo`
    - 结果：通过
  - `dotnet restore GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -p:RestoreFallbackFolders="" -nologo`
    - 结果：通过
  - `dotnet build GFramework.Game.SourceGenerators/GFramework.Game.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`0 Warning(s)`，`0 Error(s)`
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~SchemaConfigGenerator -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`54 Passed`，`0 Failed`
    - 说明：测试项目构建仍打印既有 `MA0048` / `MA0051` / `MA0004` warning；这些 warning 属于 `GFramework.SourceGenerators.Tests`
      基线，不属于本轮 `GFramework.Game.SourceGenerators` 写集
- 下一步建议：
  - 若继续 analyzer warning reduction，可评估是否为 `GFramework.SourceGenerators.Tests` 单独开新的 warning 清理切片
  - 若改回推进运行时主线，则按 `RP-017` 记录的策略先设计 `MA0158` 的多 target 兼容方案，再决定是否动共享 `object` lock

## 2026-04-23 — RP-028

### 阶段：`CqrsHandlerRegistryGenerator.cs` 文件级冲突化解（RP-028）

- 启动复核：
  - 用户指出当前分支与 `main` 在 `GFramework.Cqrs.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs`
    存在冲突，需要人工确认并解决
  - 本地检查后确认工作树没有 `UU` 或冲突标记；进一步对比 `origin/main` 发现冲突根因不是运行逻辑回退，而是
    `main` 在旧的单文件版本里新增了 `OrderedRegistrationKind` / `RuntimeTypeReferenceSpec` 的 XML 文档，
    而当前分支已将这些类型拆分到 `CqrsHandlerRegistryGenerator.Models.cs`
- 决策：
  - 保留当前分支已经完成的 partial 拆分，不把模型重新塞回 `CqrsHandlerRegistryGenerator.cs`
  - 以“迁移 `main` 侧文档意图到拆分后的归属文件”为人工合并策略，避免既回退结构拆分又遗漏 `main` 新增文档
- 实施调整：
  - 将 `OrderedRegistrationKind` 的枚举说明与 `RuntimeTypeReferenceSpec` / `FromDirectReference` /
    `FromReflectionLookup` / `FromExternalReflectionLookup` / `FromArray` / `FromConstructedGeneric`
    的 XML 文档迁移到 `CqrsHandlerRegistryGenerator.Models.cs`
  - 保持 `CqrsHandlerRegistryGenerator.cs` 主文件只承载主生成管线，不引入重复模型定义
- 验证结果：
  - `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release --no-restore -p:RestoreFallbackFolders="" -clp:"Summary;WarningsOnly" -nologo`
    - 结果：`0 Warning(s)`，`0 Error(s)`
- 下一步建议：
  - 若后续继续处理分支冲突，优先先判断 `main` 改动是否已在当前 partial 文件集里存在等价归属，再决定是否需要真正 merge/rebase
  - 若回到 PR #269 收口，可继续抓取最新 unresolved threads 与 CI 状态

## 2026-04-23 — RP-027

### 阶段：PR #269 Greptile inherited-member collision follow-up（RP-027）

- 启动复核：
  - 根据用户补充，重新核对 `$gframework-pr-review` 抓下来的 `greptile-apps[bot]` unresolved 线程，确认仍有一条
    `ContextAwareGenerator` 关于 inherited member names 未参与 collision detection 的 P1 评论
  - 本地读取 `CreateGeneratedContextMemberNames(...)` 后确认当前实现只收集 `symbol.GetMembers()`，确实没有遍历基类链
- 决策：
  - 保持现有 `_gFrameworkContextAware*` 前缀和数字后缀分配规则不变，只把保留名集合扩展为“当前类型 + 基类链显式成员”
  - 沿用既有 `ContextAwareGeneratorSnapshotTests` 模式，新增 inherited-field collision 快照，而不是只写松散字符串断言
- 实施调整：
  - 为 `ContextAwareGenerator` 新增 `CollectReservedContextMemberNames(...)` helper，遍历完整 `BaseType` 链收集显式成员名
  - 为 `ContextAwareGeneratorSnapshotTests` 增加 `InheritedCollisionRule` 场景，并抽出公共测试源码 helper，避免重复样板
  - 新增快照 `InheritedCollisionRule.ContextAware.g.cs`，锁定基类已声明 `_gFrameworkContextAware*` 时生成器会回退到 `...1` 后缀
- 验证结果：
  - `dotnet build GFramework.Core.SourceGenerators/GFramework.Core.SourceGenerators.csproj -c Release --no-restore -p:RestoreFallbackFolders="" -clp:"Summary;WarningsOnly" -nologo`
    - 结果：`0 Warning(s)`，`0 Error(s)`
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~ContextAwareGeneratorSnapshotTests.Snapshot_ContextAwareGenerator_With_Inherited_Field_Name_Collisions|FullyQualifiedName~ContextAwareGeneratorSnapshotTests.Snapshot_ContextAwareGenerator_With_User_Field_Name_Collisions|FullyQualifiedName~ContextAwareGeneratorSnapshotTests.Snapshot_ContextAwareGenerator" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`3 Passed`，`0 Failed`
    - 说明：`GFramework.SourceGenerators.Tests` 仍打印既有 `MA0048`、`MA0051`、`MA0004` warning；本轮未扩大到测试项目 warning 清理
- 下一步建议：
  - 若继续收口 PR #269，可再次抓取最新 unresolved threads，确认 Greptile / CodeRabbit 当前是否只剩陈旧信号
  - 若继续推进 analyzer 主线，可单独评估 `GFramework.SourceGenerators.Tests` 的 warning 清理是否值得开新切片

## 2026-04-23 — RP-026

### 阶段：PR #269 failed-test follow-up（RP-026）

- 启动复核：
  - 使用 `$gframework-pr-review` 抓取当前分支 PR #269 的 test report，确认最新失败信号来自
    `SchemaConfigGeneratorTests.Run_Should_Assign_Globally_Unique_Reference_Metadata_Member_Names`
  - 本地复测前先对 `GFramework.SourceGenerators.Tests` 执行 `dotnet restore -p:RestoreFallbackFolders=""`，
    规避当前 WSL worktree 仍残留的 Windows NuGet fallback package folder 资产干扰
- 决策：
  - 保持 `SchemaConfigGenerator` 当前 `GF_ConfigSchema_014` 语义不变；PR 失败是测试输入陈旧，而不是生成器行为回退
  - 将用例改写为“合法 schema 路径在 reference metadata member name 上碰撞”的场景，继续覆盖全局唯一后缀分配逻辑
- 实施调整：
  - 将测试 schema 从根级 `drop-items` / `drop_items` 非法同层冲突改为 `drop.items`、`drop.items1`、`dropItems`、
    `dropItems1` 的合法组合
  - 更新断言，验证 `MonsterConfigBindings.g.cs` 中继续生成 `DropItems`、`DropItems1`、`DropItems2` 与 `DropItems11`
- 验证结果：
  - `dotnet restore GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -p:RestoreFallbackFolders="" -nologo`
    - 结果：通过
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~Run_Should_Assign_Globally_Unique_Reference_Metadata_Member_Names -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`1 Passed`，`0 Failed`
    - 说明：`GFramework.SourceGenerators.Tests` 在构建阶段仍会打印既有 `MA0048`、`MA0051`、`MA0004` warning；本轮未扩展到该测试项目的 warning 清理
- 下一步建议：
  - 若继续收口 PR #269，可再次抓取最新 test report / open thread，确认是否还有新的 CI 失败信号
  - 若回到 analyzer 主线，优先决定是否为 `GFramework.SourceGenerators.Tests` 单独开一轮 warning 清理切片

## 2026-04-23 — RP-025

### 阶段：PR #269 第五轮 review follow-up 与模块 build / warning 治理补充（RP-025）

- 启动复核：
  - 继续使用 `$gframework-pr-review` 读取 PR #269 当前 latest review、outside-diff comment、nitpick comment 与 open-thread 摘要
  - 本地核对后确认 `SchemaConfigGenerator` 的取消传播、根 `type` 非字符串防御、`ContextAware` 冲突快照与
    `Cqrs` error type 线程均已是陈旧信号；仍成立的是归一化字段名冲突与 `dynamic` 运行时类型引用问题
- 决策：
  - `SchemaConfigGenerator` 不复用 `GF_ConfigSchema_006`，改为新增专门的冲突诊断 `GF_ConfigSchema_014`，
    避免把“标识符非法”和“归一化后重名”混成同一类错误
  - `CqrsHandlerRegistryGenerator` 对 `dynamic` 采用“生成期归一化为 `global::System.Object`”策略，而不是退回更宽泛的
    fallback 路径，保持精确注册能力且避免发射 `typeof(dynamic)`
  - `AGENTS.md` 增加模块级 build / warning 治理规则，要求后续改代码时必须对受影响模块跑 Release build，并处理或显式报告 warning
- 实施调整：
  - 为 `SchemaConfigGenerator` 增加对象级生成属性名登记 helper，在 `ParseObjectSpec(...)` 中拦截 `foo-bar` /
    `foo_bar` 这类归一化后冲突，并新增 `ConfigSchemaDiagnostics.DuplicateGeneratedIdentifier`
  - 为 `SchemaConfigGeneratorTests` 补上冲突诊断回归测试；为 `CqrsHandlerRegistryGeneratorTests` 收紧
    unresolved-type 断言并新增 `dynamic` 类型归一化回归测试
  - 为 `CqrsHandlerRegistryGenerator.RuntimeTypeReferences` 增加 `TypeKind.Dynamic` 归一化处理，并保持
    `TypeKind.Error` 的保守回退
  - 为 `AGENTS.md` 补充“受影响模块必须独立 build 且 warning 不能默认甩给长期分支”的硬性规范
- 验证结果：
  - `dotnet restore GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -p:RestoreFallbackFolders="" -nologo`
    - 结果：通过；并行 restore 时出现一次共享 `obj` 文件已存在的竞争噪音，串行验证后未再复现
  - `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release --no-restore -p:RestoreFallbackFolders="" -clp:"Summary;WarningsOnly" -nologo`
    - 结果：`0 Warning(s)`，`0 Error(s)`
  - `dotnet build GFramework.Game.SourceGenerators/GFramework.Game.SourceGenerators.csproj -c Release --no-restore -p:RestoreFallbackFolders="" -clp:"Summary;WarningsOnly" -nologo`
    - 结果：`9 Warning(s)`，`0 Error(s)`；维持既有 `SchemaConfigGenerator.cs` `MA0051` 基线，未新增 warning
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~Run_Should_Report_Diagnostic_When_Schema_Keys_Collide_After_Identifier_Normalization|FullyQualifiedName~Emits_Object_Type_Reference_When_Handler_Response_Uses_Dynamic|FullyQualifiedName~Emits_Runtime_Type_Lookup_When_Handler_Contract_Contains_Unresolved_Error_Types" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`3 Passed`，`0 Failed`
    - 说明：测试项目构建仍打印既有 `MA0051` / `MA0004` / `MA0048` warning，不属于本轮 generator 模块写集，但已在 tracking 风险中记录
- 下一步建议：
  - 若继续收口 PR #269，可再次抓取最新 unresolved threads，确认 GitHub 上剩余 open thread 是否全部转为陈旧信号
  - 若回到 analyzer 主线，继续推进 `GFramework.Game.SourceGenerators/Config/SchemaConfigGenerator.cs` 剩余 `MA0051`

## 2026-04-22 — RP-024

### 阶段：PR #269 第四轮 review follow-up 收口（RP-024）

- 启动复核：
  - 延续 `$gframework-pr-review` 对 PR #269 latest-head unresolved threads 的复核，重点核对最新 5 个未解决线程是否仍与当前
    worktree 一致
  - 本地确认 `EasyEvents` 异常契约、`SchemaConfigGenerator` 取消传播与 `ContextAwareGenerator` 字段冲突线程已是陈旧信号，
    真正仍成立的仅剩 `CqrsHandlerRegistryGenerator` 的 Roslyn error type 直接引用，以及根 schema `type` 非字符串时的
    `GetString()` 防御
- 决策：
  - `CqrsHandlerRegistryGenerator` 保持现有“优先精确重建、必要时退回运行时查找”的设计，不引入新的程序集级 fallback 契约分支；
    只在 `CanReferenceFromGeneratedRegistry(...)` 中显式拒绝 `TypeKind.Error`，让未解析类型走已有运行时查找路径
  - `SchemaConfigGenerator` 继续沿用现有 `GF_ConfigSchema_002` 诊断，不新增诊断 ID；仅在根对象校验入口补上
    `JsonValueKind.String` 前置判断
- 实施调整：
  - 为 `CqrsHandlerRegistryGenerator.RuntimeTypeReferences` 增加 `TypeKind.Error` 防御，避免把未解析类型写成生成代码里的
    `typeof(...)`
  - 为 `SchemaConfigGeneratorTests` 补上根 `type` 为数字时返回 `GF_ConfigSchema_002` 的回归测试
  - 为 `CqrsHandlerRegistryGeneratorTests` 补上未解析 error type 会改走运行时 `GetType(...)` 精确查找的回归测试
- 验证结果：
  - `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release --no-restore -p:RestoreFallbackFolders="" -v minimal`
    - 结果：`0 Warning(s)`，`0 Error(s)`
  - `dotnet build GFramework.Game.SourceGenerators/GFramework.Game.SourceGenerators.csproj -c Release --no-restore -p:RestoreFallbackFolders="" -v minimal`
    - 结果：通过；仍保留既有 `9` 条 `SchemaConfigGenerator.cs` `MA0051`
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~SchemaConfigGeneratorTests.Run_Should_Report_Diagnostic_When_Root_Type_Metadata_Is_Not_A_String|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_Runtime_Type_Lookup_When_Handler_Contract_Contains_Unresolved_Error_Types" -m:1 -p:RestoreFallbackFolders="" -v minimal`
    - 结果：`2 Passed`，`0 Failed`
    - 说明：测试命令需在无沙箱环境下运行，因为当前 test host 在沙箱内创建本地 socket 会收到 `Permission denied`
- 下一步建议：
  - 若继续压缩 PR #269 的 review backlog，可再次抓取最新 unresolved threads，确认 GitHub 上仅剩陈旧线程后再决定是否继续代码改动
  - 若回到 analyzer 主线，继续推进 `SchemaConfigGenerator.cs` 剩余 `MA0051`

## 2026-04-22 — RP-023

### 阶段：PR #269 第三轮 review follow-up 收口（RP-023）

- 启动复核：
  - 延续 `$gframework-pr-review` 对 PR #269 的 latest-head unresolved threads、outside-diff comment 与 nitpick comment
  - 本地核实后确认剩余仍成立的项集中在 `SchemaConfigGenerator` 根类型名校验、aggregate registration comparer XML 文档转义、
    `LoggingConfigurationTests` / `CollectionExtensionsTests` 断言补强，以及 `ai-plan` 命令文本可复制性
- 决策：
  - `SchemaConfigGenerator` 沿用现有 `InvalidGeneratedIdentifier` 诊断，不新增诊断 ID；将根类型名校验收敛到独立 helper，
    让顶层 schema 文件名与属性名共享同一类安全边界
  - aggregate registration comparer 文档直接复用现有 `EscapeXmlDocumentation(...)`，避免在 `///` 注释里再次写入原始泛型尖括号
  - `CqrsHandlerRegistryGenerator` 的重复反射查找分支采用小 helper 抽取，不改变 fallback 语义和快照输出
- 实施调整：
  - 为 `SchemaConfigGenerator` 新增 `TryBuildRootTypeIdentifiers(...)`，在进入 `ParseObjectSpec(...)` 前拦截非法根类型名
  - 调整 aggregate registration comparer 属性的 XML 文档，使用 `<c>...</c>` 包裹并转义泛型类型文本
  - 为 `SchemaConfigGeneratorTests` 增加非法 schema 文件名诊断回归，并补强 generated catalog 中 comparer 文档断言
  - 为 `LoggingConfigurationTests` 增加正向键存在和值断言，为 `CollectionExtensionsTests` 补齐返回类型泛型参数绑定断言
  - 为 `CqrsHandlerRegistryGenerator.RuntimeTypeReferences` 抽取共享反射查找 helper，并修正 active tracking 中的转义引号
- 验证结果：
  - `dotnet build GFramework.Game.SourceGenerators/GFramework.Game.SourceGenerators.csproj -c Release --no-restore -p:RestoreFallbackFolders="" -v minimal`
    - 结果：通过；仍保留既有 `9` 条 `SchemaConfigGenerator.cs` `MA0051`
  - `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release --no-restore -p:RestoreFallbackFolders="" -v minimal`
    - 结果：通过；并行构建时出现一次 `MSB3026` 文件占用重试，自动恢复后完成
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~SchemaConfigGeneratorTests|FullyQualifiedName~SchemaConfigGeneratorSnapshotTests|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests" -m:1 -p:RestoreFallbackFolders="" -v minimal`
    - 结果：`63 Passed`，`0 Failed`
    - 说明：测试项目构建仍打印既有 source-generator-tests analyzer warning，不属于本轮写集
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~CollectionExtensionsTests|FullyQualifiedName~LoggingConfigurationTests" -m:1 -p:RestoreFallbackFolders="" -v minimal`
    - 结果：`27 Passed`，`0 Failed`
- 下一步建议：
  - 若本轮验证通过，继续回到 `SchemaConfigGenerator.cs` 剩余 `MA0051`
  - 若 PR #269 仍有未关闭 review thread，再按“先本地复核、再最小修复”的节奏收口

## 2026-04-22 — RP-022

### 阶段：PR #269 第二轮 review follow-up 收口（RP-022）

- 启动复核：
  - 延续 `$gframework-pr-review` 的 PR #269 结果，继续核对 latest-head unresolved threads 与 nitpick comment
  - 结合本地实现确认仍成立的项不止第一轮记录的 4 个，还包括公共 API 兼容回退、`SchemaConfigGenerator` 取消传播、
    `ContextAwareGenerator` 真正的字段名去冲突与锁内读取修正、`Cqrs` 运行时类型 null 防御
- 决策：
  - 对公共 API 兼容项优先保持既有契约，不为了压 analyzer 而继续收窄返回类型、属性类型或异常类型
  - `ContextAwareGenerator` 采用保守并发修复：移除未加锁 fast-path，统一在锁内读取上下文缓存，并让生成字段名按已有成员去冲突
  - `SchemaConfigGenerator` 在取消已请求时直接重新抛出 `OperationCanceledException`，避免把取消误报告成普通诊断
- 实施调整：
  - 将 `EasyEvents.AddEvent<T>()` 的重复注册异常恢复为 `ArgumentException`，并在测试中恢复既有异常契约断言
  - 将 `CollectionExtensions.ToDictionarySafe(...)` 返回类型恢复为 `Dictionary<TKey, TValue>`，并新增反射测试锁定公开 API 形状
  - 将 `LoggingConfiguration` / `FilterConfiguration` 的公开集合属性恢复为具体 `List<>` / `Dictionary<,>` 类型，
    并新增反射测试与默认 comparer 语义断言
  - 为 `CqrsHandlerRegistryGenerator` 的命名类型引用构造补上 `ContainingAssembly is null` 防御，移除发射 helper 冗余布尔参数
  - 为 `SchemaConfigGenerator` 补上“仅在 cancellationToken 已取消时重抛”的 catch 分支，并为测试驱动添加多 `AdditionalText` 重载
  - 为 `ContextAwareGenerator` 增加生成成员名分配逻辑，新增 `_gFrameworkContextAware*` 与旧 `_context*` 双冲突快照场景，
    同时移除 getter 中未加锁 fast-path
- 验证结果：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -v minimal`
    - 结果：通过
  - `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -v minimal`
    - 结果：通过
  - `dotnet build GFramework.Game.SourceGenerators/GFramework.Game.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -v minimal`
    - 结果：通过；仍有既有 `9` 条 `MA0051`
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~SchemaConfigGeneratorTests|FullyQualifiedName~ContextAwareGeneratorSnapshotTests|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests" -m:1 -p:RestoreFallbackFolders="" -v minimal`
    - 结果：`63 Passed`，`0 Failed`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~EasyEventsTests|FullyQualifiedName~CollectionExtensionsTests|FullyQualifiedName~LoggingConfigurationTests|FullyQualifiedName~ConfigurableLoggerFactoryTests" -m:1 -p:RestoreFallbackFolders="" -v minimal`
    - 结果：`38 Passed`，`0 Failed`
- 下一步建议：
  - 回到 `GFramework.Game.SourceGenerators/Config/SchemaConfigGenerator.cs` 的剩余 `MA0051`
  - 若后续 review 再提 analyzer 兼容建议，先做公共契约回归检查，再决定是否接受该建议

## 2026-04-22 — RP-021

### 阶段：PR #269 review follow-up 收口（RP-021）

- 启动复核：
  - 使用 `$gframework-pr-review` 读取当前分支 PR #269 的 CodeRabbit outside-diff 与 nitpick 汇总
  - 本地复核后确认仍成立的 4 个项分别是：`CqrsHandlerRegistryGenerator.cs` 超过仓库文件大小上限、
    `ContextAwareGenerator` 生成字段名可能与用户 partial 类型冲突、`SetContextProvider` 缺少运行时 null 防御、
    `Option<T>` 缺少 `<remarks>` 契约说明
- 决策：
  - `CqrsHandlerRegistryGenerator` 继续采用既有 partial helper 风格，按“主流程 / 运行时类型引用 / 源码发射 / 模型”四个文件拆分，
    保持生成顺序、日志文本、fallback 契约和快照输出不变
  - `ContextAwareGenerator` 只收口仍成立的 review 项，不引入未被本地证实的 `Volatile.Read/Write` 变更
  - 为字段命名冲突新增生成器快照场景，避免后续回退到 `_context` / `_contextProvider` / `_contextSync`
- 实施调整：
  - 将 `GFramework.Cqrs.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 拆为 4 个 partial 文件，分别承载主生成管线、
    runtime type reference 构造、source emission helper 与嵌套 specs/models
  - 将 `ContextAwareGenerator` 生成字段统一改为 `_gFrameworkContextAware*` 前缀，同步更新 XML 文档、注释和显式接口实现
  - 为 `SetContextProvider(...)` 增加 `ArgumentNullException.ThrowIfNull(provider)` 与 XML `<exception>` 说明
  - 为 `Option<T>` 补充 `<remarks>`，明确 `Some/None`、`null` 约束、不可变语义与推荐使用方式
  - 新增 `CollisionProneRule.ContextAware.g.cs` 快照，覆盖用户字段名与生成字段名冲突场景
- 验证结果：
  - `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders= -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`0 Warning(s)`，`0 Error(s)`；拆分后 `CqrsHandlerRegistryGenerator` 最大单文件为 `851` 行
  - `dotnet build GFramework.Core.SourceGenerators/GFramework.Core.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders= -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`0 Warning(s)`，`0 Error(s)`
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~CqrsHandlerRegistryGeneratorTests -m:1 -p:RestoreFallbackFolders= -nologo`
    - 结果：`14 Passed`，`0 Failed`
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~ContextAwareGeneratorSnapshotTests -m:1 -p:RestoreFallbackFolders= -nologo`
    - 结果：`2 Passed`，`0 Failed`
    - 说明：最初并行跑两个 `dotnet test` 命令时触发共享输出文件锁冲突；串行重跑后确认是测试宿主环境噪音而非代码回归
- 下一步建议：
  - 若本轮验证通过，可继续回到 `SchemaConfigGenerator` 剩余 `MA0051`
  - 若 review 再次聚焦 `ContextAwareGenerator` 并发可见性问题，需要先补最小复现测试，再决定是否引入 `Volatile` 语义

## 2026-04-22 — RP-020

### 阶段：`SchemaConfigGenerator` 第一批 `MA0051` 结构拆分（RP-020）

- 启动复核：
  - 当前 worktree 仍映射到 `analyzer-warning-reduction` active topic
  - `GFramework.Game.SourceGenerators` warnings-only build 复现 `19` 条 warning，全部为
    `GFramework.Game.SourceGenerators/Config/SchemaConfigGenerator.cs` 的 `MA0051`
- 决策：
  - 本轮继续低风险结构拆分，不改变 schema 支持范围、诊断 ID、生成类型形状或输出顺序
  - 未使用 subagent；critical path 是本地复现 warning、拆分语义阶段并用 focused schema generator tests 验证行为
- 实施调整：
  - 将 schema 入口解析拆为文本读取、root 验证、id key 验证和 `SchemaFileSpec` 构造阶段
  - 将属性解析拆为共享上下文提取、类型分派、标量/对象/数组属性构造 helper
  - 将统一 schema 遍历拆为对象属性、dependentSchemas、allOf、条件分支、not、array items / contains 等遍历阶段
  - 将约束文档生成拆为 const、numeric、string、array、object 约束片段
  - 将 catalog/registration/YAML/lookup/object type 等生成代码发射路径中的小型高收益 helper 拆出
- 验证结果：
  - `dotnet build GFramework.Game.SourceGenerators/GFramework.Game.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders= -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`9 Warning(s)`，`0 Error(s)`；当前项目剩余 warning 均为 `SchemaConfigGenerator.cs` 的 `MA0051`
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~SchemaConfigGenerator -m:1 -p:RestoreFallbackFolders= -nologo`
    - 结果：`50 Passed`，`0 Failed`
    - 说明：测试项目构建仍显示既有 source generator test analyzer warning；不属于本轮写集
- 下一步建议：
  - 继续该主题时，优先拆分 `GenerateBindingsClass`、`AppendGeneratedConfigCatalogType` 或对象/条件 schema target 验证方法
  - 若转回 `MA0158`，仍需先设计多 target 条件编译方案，再考虑替换共享源码中的 `object` lock

## 2026-04-22 — RP-019

### 阶段：`SchemaConfigGenerator` 当前 `MA0006` 收口（RP-019）

- 启动复核：
  - 当前 worktree 仍映射到 `analyzer-warning-reduction` active topic
  - Windows Git interop 在当前 shell 中返回 WSL socket 错误；本轮使用显式 `--git-dir` / `--work-tree` 读取状态
  - `GFramework.Game.SourceGenerators` 首次 build 受 stale Windows fallback package folder 影响，刷新 restore 资产后复现
    `46` 条 warning，其中 `MA0006=27`，其余为 `SchemaConfigGenerator.cs` 的 `MA0051`
- 决策：
  - 本轮先收口低风险 `MA0006`，不在同一 slice 中拆分 `SchemaConfigGenerator.cs` 的长方法
  - 未使用 subagent；critical path 是本地复现 warning、替换 schema 字符串比较并用 focused schema generator tests 验证输出行为
- 实施调整：
  - 为 schema 类型关键字新增 `IsSchemaType` / `IsNumericSchemaType` helper，统一使用 `StringComparison.Ordinal`
  - 将 id key 类型验证、约束文档生成、required property 文档和路径拼接中的直接字符串比较改为显式 ordinal 比较
  - 修正 `JsonElement.GetString()` 后的 nullable flow，避免新增 `CS8604`
- 验证结果：
  - `dotnet restore GFramework.Game.SourceGenerators/GFramework.Game.SourceGenerators.csproj -p:RestoreFallbackFolders= -nologo`
    - 结果：通过
  - `dotnet build GFramework.Game.SourceGenerators/GFramework.Game.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders= -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`19 Warning(s)`，`0 Error(s)`；当前项目输出已无 `MA0006`，剩余均为 `MA0051`
  - `dotnet restore GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -p:RestoreFallbackFolders= -nologo`
    - 结果：通过
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~SchemaConfigGenerator -m:1 -p:RestoreFallbackFolders= -nologo`
    - 结果：`50 Passed`，`0 Failed`
    - 说明：测试项目构建仍显示既有 analyzer warning；不属于本轮写集
- 下一步建议：
  - 继续该主题时，优先拆分 `GFramework.Game.SourceGenerators/Config/SchemaConfigGenerator.cs` 的 `MA0051`
  - 若回到 `MA0158`，先设计多 target 条件编译方案，再考虑替换共享源码中的 `object` lock

## 2026-04-22 — RP-018

### 阶段：`CqrsHandlerRegistryGenerator` 剩余 `MA0051` 收口（RP-018）

- 启动复核：
  - 当前 worktree 仍映射到 `analyzer-warning-reduction` active topic
  - `MA0158` 锁迁移仍然跨 `GFramework.Core` / `GFramework.Cqrs` 多 target 共享源码，继续视为需要单独设计的兼容性问题
  - `GFramework.Cqrs.SourceGenerators` warnings-only build 复现 `CqrsHandlerRegistryGenerator.cs` 的 `6` 个 `MA0051`
- 决策：
  - 本轮暂缓 `MA0158`，转入单文件、可由生成器测试覆盖的 `GFramework.Cqrs.SourceGenerators` 结构拆分
  - 未使用 subagent；critical path 是本地复现 warning、拆分源码发射流程并用 focused generator tests 验证输出未变
- 实施调整：
  - 将 handler candidate 分析拆为接口收集、候选构造和单接口注册分类阶段
  - 将运行时类型引用构造拆为已构造泛型、命名类型反射查找等独立 helper
  - 将注册器源码生成拆为文件头、程序集特性、注册器类型、`Register` 方法和服务注册日志发射 helper
  - 将有序注册与精确反射注册输出拆为独立阶段，保留原有排序和生成文本形状
- 验证结果：
  - `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders= -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`0 Warning(s)`，`0 Error(s)`
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~CqrsHandlerRegistryGeneratorTests -m:1 -p:RestoreFallbackFolders= -nologo`
    - 结果：`14 Passed`，`0 Failed`
    - 说明：测试项目构建仍显示 `GFramework.Game.SourceGenerators` 与测试项目中的既有 analyzer warning；不属于本轮写集
- 下一步建议：
  - 继续该主题时，优先处理 `GFramework.Game.SourceGenerators/Config/SchemaConfigGenerator.cs` 的 `MA0006` 低风险批次
  - 若回到 `MA0158`，先设计多 target 条件编译方案，再考虑替换共享源码中的 `object` lock

## 2026-04-22 — RP-017

### 阶段：`ContextAwareGenerator` 剩余 `MA0051` 收口（RP-017）

- 启动复核：
  - 当前 worktree 仍映射到 `analyzer-warning-reduction` active topic
  - `GFramework.Core` `net10.0` warnings-only build 在刷新 restore fallback 资产后复现 `16` 个 `MA0158`
  - `GFramework.Core.SourceGenerators` warnings-only build 复现 `ContextAwareGenerator.GenerateContextProperty` 的单个
    `MA0051`
- 决策：
  - `MA0158` 涉及 `GFramework.Core` 与 `GFramework.Cqrs` 的 object lock 字段，且项目仍多 target 到 `net8.0` / `net9.0`
    / `net10.0`，因此本轮不直接批量替换为 `System.Threading.Lock`
  - 先处理单文件、单 warning、生成输出可由 snapshot 验证的 `ContextAwareGenerator` 结构拆分
  - 未使用 subagent；本轮 critical path 是本地复现 warning、拆分方法并验证生成输出，拆分后写集只包含单个 generator 文件和
    active `ai-plan` 文档
- 实施调整：
  - 将 `GenerateContextProperty` 拆为 `GenerateContextBackingFields`、`GenerateContextGetter` 与
    `GenerateContextProviderConfiguration`
  - 保留原有 `StringBuilder` 追加顺序与生成代码文本，避免 snapshot 变更
  - 为新增 helper 补充 XML 注释，说明字段、getter 与 provider 配置 API 的生成职责
- 验证结果：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net10.0 -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`16 Warning(s)`，`0 Error(s)`；记录当前 `MA0158` 基线，不作为本轮修改范围
  - `dotnet build GFramework.Core.SourceGenerators/GFramework.Core.SourceGenerators.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`0 Warning(s)`，`0 Error(s)`；`ContextAwareGenerator.cs` 的 `MA0051` 已清零
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~ContextAwareGeneratorSnapshotTests" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`1 Passed`，`0 Failed`
    - 说明：该 test project 构建仍显示相邻 generator/test 项目的既有 analyzer warning；本轮关注的
      `GFramework.Core.SourceGenerators` 独立 build 已清零
- 下一步建议：
  - 继续该主题时，优先设计 `MA0158` 的多 target 兼容迁移方案；如果风险过高，再单独切入
    `GFramework.Cqrs.SourceGenerators` 或 `GFramework.Game.SourceGenerators` 的结构性 warning

## 2026-04-22 — RP-016

### 阶段：`GFramework.Core` 剩余低风险 warning 批次清零（RP-016）

- 依据 `RP-015` 的下一步建议，本轮恢复到 `MA0016` / `MA0002` 低风险批次，并顺手吸收仍集中在
  `GFramework.Core` 的 `MA0015` 与 `MA0077`
- 基线复核：
  - 首次使用 Linux `dotnet` 时仍被当前 worktree 的 Windows fallback package folder restore 资产阻断
  - 切换到 host Windows `dotnet` 后，`GFramework.Core` `net8.0` warnings-only build 复现 `9` 条 warning：
    `MA0016=5`、`MA0002=2`、`MA0015=1`、`MA0077=1`
- 实施调整：
  - 将 `LoggingConfiguration.Appenders` / `LoggerLevels` 与 `FilterConfiguration.Namespaces` / `Filters`
    的公开类型改为集合抽象接口，同时保留 `List<T>` / `Dictionary<TKey,TValue>` 默认实例，兼顾 analyzer 与现有配置消费路径
  - 将 `CollectionExtensions.ToDictionarySafe(...)` 返回类型改为 `IDictionary<TKey,TValue>`，内部仍使用 `Dictionary<TKey,TValue>`
    保留“重复键以后值覆盖前值”的实现语义
  - 为 `CoroutineScheduler` 的 `_tagged` 与 `_grouped` 字典显式指定 `StringComparer.Ordinal`，将原有默认区分大小写语义写入代码
  - 将 `EasyEvents.AddEvent<T>()` 重复注册失败从 `ArgumentException` 改为 `InvalidOperationException`；该路径表示状态冲突，
    不是某个方法参数无效，因此不能为 `MA0015` 人造参数名
  - 为 `Option<T>` 声明 `IEquatable<Option<T>>`，与已有强类型 `Equals(Option<T>)` 实现对齐
- 验证结果：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`0 Warning(s)`，`0 Error(s)`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~LoggingConfigurationTests|FullyQualifiedName~ConfigurableLoggerFactoryTests|FullyQualifiedName~CollectionExtensionsTests|FullyQualifiedName~EasyEventsTests|FullyQualifiedName~OptionTests|FullyQualifiedName~CoroutineGroupTests|FullyQualifiedName~CoroutineSchedulerTests" -m:1 -nologo`
    - 结果：`112 Passed`，`0 Failed`
    - 说明：测试构建仍显示既有 `net10.0` `MA0158` 与 source generator `MA0051` warning；这些不属于本轮
      `GFramework.Core` `net8.0` 剩余 warning 批次
- 当前结论：
  - `GFramework.Core` `net8.0` 当前 analyzer warning baseline 已清零
  - analyzer topic 仍可继续，但下一轮应转入 `net10.0` 专属 `MA0158` 兼容性评估，或单独处理 source generator 剩余
    `MA0051`
- 下一步建议：
  - 优先评估 `MA0158` 在多 target 源码中的安全推进方式；若风险过高，再处理
    `GFramework.Core.SourceGenerators/Rule/ContextAwareGenerator.cs` 的结构拆分

## 2026-04-21 — RP-015

### 阶段：PR #267 failed-test follow-up 收口（RP-015）

- 触发背景：
  - 用户指出“测试好像挂了”，按 `$gframework-pr-review` 重新抓取当前分支 PR #267 的 review / checks / CTRF 评论
  - PR 评论里同时存在一次 `2143 passed / 0 failed` 与一次 `1 failed` 的 CTRF 报告；失败用例为
    `AsyncLogAppenderTests.ILogAppender_Flush_Should_Raise_OnFlushCompleted_Only_Once`
- 复核过程：
  - 先跑定向单测时该用例可以单独通过，因此继续核对 PR head commit 与本地整包测试，避免把旧评论误判成当前状态
  - 在 `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-restore --disable-build-servers`
    下成功复现相同失败，确认问题仍存在于当前代码，而不是单纯的 PR 评论残留
  - 同时发现当前沙箱内如果用 shell 循环反复启动 `dotnet test`，会触发 `MSBuild` named pipe `Permission denied`
    的环境噪音；后续验证改为单次命令并显式加 `--disable-build-servers`
- 根因结论：
  - `AsyncLogAppender.Flush()` 只依赖后台消费循环在处理完某个条目后检查 `_flushRequested`
  - 当调用方执行 `Flush()` 前，后台线程已经把最后一个条目消费完并离开检查点时，`Flush()` 会一直等到默认超时，
    最终通过 `OnFlushCompleted` 发出一次 `Success=false` 的错误完成通知
- 实施修复：
  - 为 `AsyncLogAppender` 增加“当前是否仍有条目在途处理”的状态跟踪
  - 抽出 `TrySignalFlushCompletion()`，让 `Flush()` 在请求发出后先做一次即时完成判定；后台循环在每次处理结束后也复用
    这条判定路径
  - 在 `AsyncLogAppenderTests` 中新增 `Flush_WhenEntriesAlreadyProcessed_Should_Still_ReportSuccess`，稳定覆盖
    “调用 Flush 前队列已被后台线程清空”的场景
- 验证结果：
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-restore --disable-build-servers --filter "FullyQualifiedName~AsyncLogAppenderTests"`
    - 结果：`15 Passed`，`0 Failed`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-restore --disable-build-servers`
    - 结果：`1607 Passed`，`0 Failed`
- 当前结论：
  - PR #267 的 failed-test 信号不是纯粹的历史评论噪音，而是当前实现里仍存在的时序竞态
  - 修复后该竞态已被稳定回归测试覆盖，当前 `GFramework.Core.Tests` 整包通过
- 下一步建议：
  - 若继续 analyzer warning reduction 主题，恢复到 `MA0016` / `MA0002` 低风险批次

## 2026-04-21 — RP-014

### 阶段：PR #267 review follow-up 收口（RP-014）

- 使用 `gframework-pr-review` 抓取当前分支 PR #267 的 latest head review threads、outside-diff comment、nitpick comment、
  MegaLinter 摘要与测试报告，并确认本轮除了 6 条 open thread 之外，还存在 1 条 outside-diff 与 1 条 nitpick 需要一并复核
- 本地复核后确认仍成立的项：
  - `AsyncLogAppender` 的显式接口实现 `ILogAppender.Flush()` 会在调用 `Flush()` 后再次手动触发 `OnFlushCompleted`，
    导致接口路径重复通知
  - `Architecture.PhaseChanged`、`CoroutineExceptionEventArgs` 与 `ArchitecturePhaseCoordinator.EnterPhase` 的 XML/注释契约仍未完全同步
  - `CoroutineSchedulerTests` 的异常事件测试缺少测试级超时
  - `docs/zh-CN/core/architecture.md` 与 `docs/zh-CN/core/lifecycle.md` 仍缺少明确的 `PhaseChanged` 迁移说明
  - `ai-plan` active tracking 中 `RP-013` 的 `9 Warning(s)` 需要明确是相对 `RP-009` / `RP-011` 的 warnings-only 基线收敛
- 实施最小修复：
  - 删除 `ILogAppender.Flush()` 中重复的完成事件触发，只保留 `Flush(TimeSpan?)` 内的单一通知源
  - 为接口调用路径补充单次完成通知回归测试，并为协程异常事件测试增加 `WaitAsync(TimeSpan.FromSeconds(3))`
  - 补齐 `Architecture.PhaseChanged`、`CoroutineExceptionEventArgs` 与 `ArchitecturePhaseCoordinator.EnterPhase` 的契约文档
  - 在 `docs/zh-CN/core/architecture.md` 与 `docs/zh-CN/core/lifecycle.md` 中加入 `phase => ...` 迁移到 `(_, args) => ...` 的说明
  - 更新 `ai-plan/public/analyzer-warning-reduction/todos/analyzer-warning-reduction-tracking.md` 的恢复点、基线描述与验证结果
- 验证结果：
  - `dotnet restore GFramework.Core.Tests/GFramework.Core.Tests.csproj -p:RestoreFallbackFolders="" -nologo`
    - 结果：通过；host Windows `dotnet` 首次验证前补齐了缺失的 `Meziantou.Analyzer 3.0.48`
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`9 Warning(s)`，`0 Error(s)`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~CoroutineSchedulerTests.Scheduler_Should_Raise_OnCoroutineException_With_EventArgs|FullyQualifiedName~AsyncLogAppenderTests.Flush_Should_Raise_OnFlushCompleted_With_Sender_And_Result|FullyQualifiedName~AsyncLogAppenderTests.ILogAppender_Flush_Should_Raise_OnFlushCompleted_Only_Once|FullyQualifiedName~ArchitectureLifecycleBehaviorTests.InitializeAsync_Should_Raise_PhaseChanged_With_Sender_And_EventArgs" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`4 Passed`，`0 Failed`
- 当前结论：
  - PR #267 里当前仍成立的 CodeRabbit 高信号项已在本地收口
  - 修复内容没有改变 `EventHandler<TEventArgs>` 迁移方向，只是补齐行为、文档与恢复信息
- 下一步建议：
  - 恢复到 `MA0016` / `MA0002` 主批次，默认先看 `LoggingConfiguration`、`FilterConfiguration` 与 `CollectionExtensions`

## 2026-04-21 — RP-013

### 阶段：`MA0046` 事件签名批次收口（RP-013）

- 依据 `RP-012` 的下一步建议，本轮恢复到 `GFramework.Core` 的 `MA0046` 主批次，而不是继续停留在 PR review workflow 优化
- 本地 warnings-only 基线确认当前 `GFramework.Core` `net8.0` 仍有 `6` 个 `MA0046`：
  - `Architecture.cs`
  - `ArchitectureLifecycle.cs`
  - `ArchitecturePhaseCoordinator.cs`
  - `AsyncLogAppender.cs`
  - `CoroutineScheduler.cs` 两处事件
- 方案选择：
  - 不再保留 `Action<...>` 事件签名，统一改为标准 `EventHandler<TEventArgs>`
  - 为 `Architecture`、`AsyncLogAppender` 新增放在 `GFramework.Core.Abstractions` 的事件参数类型
  - 为 `CoroutineScheduler` 新增放在 `GFramework.Core` 的事件参数类型，因为 `CoroutineHandle` 定义在 runtime 层，不适合反向放入 Abstractions
  - `Architecture` 相关事件采用 `Coordinator -> Lifecycle -> Architecture` relay，而不是直接透传底层事件，确保公开事件的 sender 始终是实际发布者，并避免引入新的 `MA0091`
- 同步适配：
  - 更新 `GFramework.Godot/Coroutine/Timing.cs` 的 `OnCoroutineFinished` 订阅签名
  - 更新 `ArchitectureLifecycleBehaviorTests`、`CoroutineSchedulerTests`、`AsyncLogAppenderTests` 以覆盖 sender / event args 契约
  - 更新 `docs/zh-CN/core/architecture.md` 与 `docs/zh-CN/core/lifecycle.md` 的 `PhaseChanged` 示例
- 验证结果：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`9 Warning(s)`，`0 Error(s)`；当前 `GFramework.Core` `net8.0` 输出中已无 `MA0046`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~ArchitectureLifecycleBehaviorTests|FullyQualifiedName~CoroutineSchedulerTests|FullyQualifiedName~AsyncLogAppenderTests" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`50 Passed`，`0 Failed`
  - `dotnet build GFramework.Godot/GFramework.Godot.csproj -c Release --no-restore -p:RestoreFallbackFolders="" -nologo`
    - 结果：失败；当前 worktree 的 `project.assets.json` 仍引用 Windows fallback package folder，尚未完成 Godot 独立编译验证
- 当前结论：
  - `MA0046` 已从 active 批次中移除
  - 剩余 `GFramework.Core` `net8.0` warning 分布更新为：`MA0016=5`、`MA0002=2`、`MA0015=1`、`MA0077=1`
  - 若继续本主题，下一步默认转入 `MA0016` 批次；若继续触达 Godot，再先修复该项目 restore 资产

## 2026-04-21 — RP-012

### 阶段：PR review workflow 输出收窄增强（RP-012）

- 背景：上一轮虽然脚本已经能解析 `outside_diff_comments`，但直接把超长 JSON 打到终端时仍可能因为输出截断而漏看高价值 review 信号
- 本轮对 `gframework-pr-review` 做了工作流级增强，而不是继续依赖 shell 重定向技巧：
  - 为 `fetch_current_pr_review.py` 增加 `--json-output <path>`，允许把完整 JSON 稳定写入文件
  - 增加 `--section`，可只输出 `outside-diff`、`open-threads`、`megalinter` 等高信号文本摘要
  - 增加 `--path`，允许把文本输出收窄到特定文件或路径片段
  - 增加 `--max-description-length`，避免超长 comment/body 在 text 模式下刷屏
  - 当 text 模式搭配 `--json-output` 时，stdout 保持精简，并显式提示完整 JSON 文件路径
- 同步更新 `SKILL.md`：
  - 将“先落盘，再用 `jq` 或 `--section` / `--path` 缩小范围”写成推荐机器工作流
  - 补充按 section 和按路径聚焦的示例命令
- 预期收益：
  - 不再要求操作者肉眼阅读整份长 JSON
  - outside-diff、nitpick 和 open thread 都能成为一等可过滤输出
  - 即使终端输出有 token/长度上限，完整结果仍可通过文件稳定回查
- 定向验证命令：
  - `python3 -m py_compile .codex/skills/gframework-pr-review/scripts/fetch_current_pr_review.py`
    - 结果：通过；使用 `PYTHONPYCACHEPREFIX=/tmp/codex-pycache` 规避 `__pycache__` 写入限制
  - `python3 .codex/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --help`
    - 结果：通过；新增 CLI 选项均已出现在帮助输出中
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`0 Warning(s)`，`0 Error(s)`
- 下一步建议：
  - 之后执行 `$gframework-pr-review` 时，默认优先使用 `--json-output`
  - 在 review 跟进阶段，先看 `outside-diff`、`open-threads`、`megalinter` 三个 section，再决定是否需要打开完整 JSON

## 2026-04-21 — RP-011

### 阶段：PR #265 outside-diff follow-up 补收口（RP-011）

- 用户补充指出 CodeRabbit 在 `Some comments are outside the diff` 中还有 `GFramework.Core/Events/Event.cs` 的 minor finding：
  默认 no-op 委托会被 `GetInvocationList()` 计入，导致 `GetListenerCount()` 在无监听器和单监听器场景分别返回 `1` 和 `2`
- 本地复核确认该问题仍成立：
  - `Event<T>` 当前字段初始化为 `_ => { }`
  - `Event<T, TK>` 当前字段初始化为 `(_, _) => { }`
  - 两个 `Trigger(...)` 实现本身已是 null-safe，因此无需依赖占位委托规避空引用
- 实施最小修复：
  - 移除两个事件字段的 no-op 初始委托，改为以 `null` 表示“无监听器”
  - 保持 `Register` / `UnRegister` / `Trigger` 的公开 API 和调用方式不变
  - 在 `EventTests` 中新增单参数与双参数 `GetListenerCount()` 回归测试，覆盖初始值、注册后和注销后的计数语义
- 过程说明：
  - 这条不是 skill 设计遗漏；`gframework-pr-review` 的目标本来就包含 latest review body 和 outside-diff 信号
  - 上一轮是我在处理时漏看了这条 outside-diff item，且终端里展示的超长 JSON 输出被截断，未单独把 `Event.cs` 项再抽出来复核
- 定向验证命令：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`15 Warning(s)`，`0 Error(s)`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~EventTests.EventT_GetListenerCount_Should_Exclude_Placeholder_Handler|FullyQualifiedName~EventTests.EventTTK_GetListenerCount_Should_Exclude_Placeholder_Handler" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`2 Passed`，`0 Failed`
- 下一步建议：
  - 若继续 PR #265 follow-up，只接受当前本地仍成立的剩余 outside-diff 或 unresolved review 项
  - 若没有新的有效 review 点，再恢复到 `MA0046` 主批次

## 2026-04-21 — RP-010

### 阶段：PR #265 follow-up 收口（RP-010）

- 使用 `gframework-pr-review` 抓取当前分支 PR #265 的 latest head review threads、CodeRabbit review body、MegaLinter 摘要与 CTRF
  测试结果；确认最新 unresolved thread 只剩 `CoroutineScheduler` 零容量扩容边界
- 本地复核后确认两处仍成立的风险：
  - `CoroutineScheduler.Expand()` 在 `_slots.Length == 0` 时会把容量从 `0` 扩到 `0`，首次 `Run` 写槽位会越界
  - `Store.EnterDispatchScope()` 在 `_isDispatching = true` 之后、快照构建完成之前若抛异常，会留下永久的嵌套分发误判
- 实施最小修复：
  - 将 `Expand()` 调整为 `Math.Max(1, _slots.Length * 2)`，保持已有倍增策略，只补上零容量边界
  - 为 `EnterDispatchScope()` 增加快照阶段的异常回滚，确保 `_isDispatching` 与实际 dispatch 生命周期保持一致
  - 新增回归测试覆盖零容量启动路径，以及 dispatch 快照阶段抛错后的可恢复性
- 当前 PR 信号复核结论：
  - CTRF：最新评论显示 `2135 passed / 0 failed`
  - MegaLinter：唯一告警仍是 CI 中 `dotnet-format` restore 失败，未发现新的本地代码格式问题
  - 旧 review body 中提到的 `Store` 异常安全问题虽未表现为最新 open thread，但在本地代码中仍可成立，因此一并收口
- 定向验证命令：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`15 Warning(s)`，`0 Error(s)`
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~CoroutineSchedulerTests.Run_Should_Grow_From_Zero_Initial_Capacity|FullyQualifiedName~StoreTests.Dispatch_Should_Reset_Dispatching_Flag_When_Snapshot_Creation_Throws" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`2 Passed`，`0 Failed`
- 下一步建议：
  - 若继续本主题，恢复到 `MA0046` 主批次，不再停留在当前 PR follow-up
  - 若 PR review 还出现新线程，继续遵守“只修复当前本地仍成立的问题”的策略

## 2026-04-21 — RP-009

### 阶段：`MA0048` 批次收口（RP-009）

- 依据 `RP-008` 的批处理策略，本轮继续从 `GFramework.Core` 的 `MA0048` 启动，但不采用重命名公共类型的高风险做法；
  改为把同名不同泛型 arity 的家族收拢到与类型名一致的单文件中
- 具体调整：
  - 将 `AbstractCommand<TInput>` 与 `AbstractCommand<TInput, TResult>` 合并进 `AbstractCommand.cs`
  - 将 `AbstractAsyncCommand<TInput>` 与 `AbstractAsyncCommand<TInput, TResult>` 合并进 `AbstractAsyncCommand.cs`
  - 将 `AbstractQuery<TInput, TResult>` 合并进 `AbstractQuery.cs`
  - 将 `AbstractAsyncQuery<TInput, TResult>` 合并进 `AbstractAsyncQuery.cs`
  - 将泛型 `Event<T>` / `Event<T, TK>` 从 `EasyEventGeneric.cs` 迁移到 `Event.cs`
- 首次构建暴露出合并后的 `ICommand<TResult>` / `IQuery<TResult>` 命名空间歧义；随后改用
  `GFramework.Core.Abstractions.*` 的限定名完成最小修正，没有引入行为改动
- 定向验证通过：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`15 Warning(s)`，`0 Error(s)`；`MA0048` 已从当前 `GFramework.Core` `net8.0` warnings-only 基线中清空
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter "FullyQualifiedName~CommandExecutorTests|FullyQualifiedName~AbstractAsyncCommandTests|FullyQualifiedName~QueryExecutorTests|FullyQualifiedName~AbstractAsyncQueryTests|FullyQualifiedName~EventTests" -m:1 -p:RestoreFallbackFolders="" -nologo`
    - 结果：`83 Passed`，`0 Failed`
- 当前建议的下一批次顺序更新为：
  - 第一优先级：`MA0046`
  - 第二优先级：`MA0016`
  - 顺手吸收：`MA0015`、`MA0077`
  - 单独评估：`MA0002`

## 2026-04-21 — RP-008

### 阶段：批处理策略切换（RP-008）

- 根据当前 `GFramework.Core` warnings-only build 的剩余分布，后续不再默认沿用“单文件、单 warning family”的切片节奏，
  改为按 warning 类型和数量优先级批量推进
- 当前数量基线：
  - `MA0048 = 8`
  - `MA0046 = 6`
  - `MA0016 = 5`
  - `MA0002 = 2`
  - `MA0015 = 1`
  - `MA0077 = 1`
- 新的批处理规则：
  - 先按类型选择主批次，而不是按单文件选切入点
  - 若主批次数量不够，则允许顺手并入其他低冲突类型；`MA0015` 与 `MA0077` 只是当前明显的低数量尾项示例，不是限定范围
  - 单次 `boot` 的工作树改动规模控制在约 `100` 个文件以内，避免 recovery context 和 review 面同时膨胀
  - 当 warning 类型或目录边界清晰且写集不冲突时，允许使用不同模型的 subagent 并行处理，但必须先定义独占 ownership
- 当前建议的下一批次顺序：
  - 第一优先级：`MA0048`
  - 第二优先级：`MA0046`
  - 顺手吸收：其他低冲突类型，当前可见示例包括 `MA0015`、`MA0077`
  - 单独评估：`MA0016`、`MA0002`
- 本轮仅更新 recovery strategy，不改生产代码；验证继续沿用当前基线构建：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -p:RestorePackagesPath=<linux-nuget-cache> -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`23 Warning(s)`，`0 Error(s)`

## 2026-04-21 — RP-007

### 阶段：CoroutineScheduler `MA0051` 收口（RP-007）

- 依据 active tracking 中“继续只选一个 `GFramework.Core` 结构性切入点”的约束，本轮选择
  `GFramework.Core/Coroutine/CoroutineScheduler.cs`，因为剩余两个 `MA0051` 都集中在协程启动与完成清理路径，且已有
  `CoroutineSchedulerTests`、`CoroutineSchedulerAdvancedTests` 覆盖句柄创建、取消、完成状态、标签分组和等待语义
- 将 `Run` 拆分为：
  - `AllocateSlotIndex`
  - `CreateRunningSlot`
  - `RegisterCancellationCallback`
  - `RegisterStartedCoroutine`
  - `CreateCoroutineMetadata`
  - `ResetCompletionTracking`
- 将 `FinalizeCoroutine` 拆分为：
  - `TryGetFinalizableCoroutine`
  - `UpdateCompletionMetadata`
  - `ApplyCompletionMetadata`
  - `ReleaseCompletedCoroutine`
  - `CompleteCoroutineLifecycle`
- 保持取消回调只做跨线程入队、`Prewarm` 时机、统计记录文本、`RemoveTag` / `RemoveGroup` / `WakeWaiters` 顺序以及
  `OnCoroutineFinished` 的同步触发时机不变，只收缩主方法长度并补齐辅助方法意图注释
- 验证通过：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -p:RestorePackagesPath=<linux-nuget-cache> -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`23 Warning(s)`，`0 Error(s)`；`CoroutineScheduler.cs` 已不再出现在 `MA0051` 列表
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter FullyQualifiedName~CoroutineScheduler -p:RestoreFallbackFolders="" -p:RestorePackagesPath=<linux-nuget-cache> -nologo`
    - 结果：`34 Passed`，`0 Failed`
- 当前 `MA0051` 主线已经在本主题下完成；下一步若继续，应先重新评估剩余 `MA0048`、`MA0046`、`MA0002`、`MA0016` 的
  收敛价值与改动风险，再决定是否开启下一轮 warning family

## 2026-04-21 — RP-006

### 阶段：Store `MA0051` 收口（RP-006）

- 依据 active tracking 中“继续只选一个 `GFramework.Core` 结构性切入点”的约束，本轮选择
  `GFramework.Core/StateManagement/Store.cs`，因为该文件的两个 `MA0051` 都集中在 dispatch / reducer snapshot 逻辑，
  且已有 `StoreTests` 覆盖 dispatch、batch、history 和多态 reducer 匹配语义
- 在正式验证前先处理 WSL 环境噪音：当前 worktree 的 `GFramework.Core/obj/project.assets.json` 是 Windows 侧 restore
  产物，`--no-restore` 构建会继续引用宿主 Windows fallback package folder；本轮先执行一次 Linux 侧
  `dotnet restore GFramework.Core/GFramework.Core.csproj -p:RestoreFallbackFolders="" -p:RestorePackagesPath=<linux-nuget-cache> --ignore-failed-sources -nologo`
  刷新资产文件，再继续 warnings-only build
- 将 `Dispatch` 拆分为：
  - `EnterDispatchScope`
  - `TryCommitDispatchResult`
  - `ExitDispatchScope`
- 将 `CreateReducerSnapshotCore` 拆分为：
  - `CreateExactReducerSnapshot`
  - `CreateAssignableReducerSnapshot`
  - `CollectReducerMatches`
  - `CompareReducerMatch`
- 保持 `_dispatchGate -> _lock` 的锁顺序、middleware 锁外执行、批处理通知折叠以及“精确类型 -> 基类 -> 接口 ->
  注册顺序”的 reducer 稳定排序语义不变，只收缩主方法长度并补齐辅助方法意图注释
- 验证通过：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -p:RestoreFallbackFolders="" -p:RestorePackagesPath=<linux-nuget-cache> -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`25 Warning(s)`，`0 Error(s)`；`Store.cs` 已不再出现在 `MA0051` 列表
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter FullyQualifiedName~StoreTests -p:RestoreFallbackFolders="" -p:RestorePackagesPath=<linux-nuget-cache> -nologo`
    - 结果：`30 Passed`，`0 Failed`
- 下一步保持同一节奏：只在 `CoroutineScheduler.cs` 的 `Run` / `FinalizeCoroutine` 两个 `MA0051` 中继续，不与其他
  warning 家族混做

## 2026-04-21 — RP-005

### 阶段：PauseStackManager `MA0051` 收口（RP-005）

- 按 active tracking 中“继续只选一个 `GFramework.Core` 结构性切入点”的约束，本轮选择
  `GFramework.Core/Pause/PauseStackManager.cs`，因为该文件体量明显小于 `CoroutineScheduler` 和 `Store`，
  且已有稳定的 `PauseStackManagerTests` 覆盖暂停栈、跨组独立性、事件通知与并发 `Push/Pop` 行为
- 先用 `warnings-only` 定向构建确认 `DestroyAsync` 与 `Pop` 仍分别命中 `MA0051`，再把逻辑拆分为：
  - `TryBeginDestroy`
  - `NotifyDestroyedGroups`
  - `TryPopEntry`
  - `RemoveEntryFromStack`
- 额外抽出 `CreateHandlerSnapshot` 与 `NotifyHandlersSnapshot`，统一普通通知与销毁补发路径的处理器排序和异常日志，
  保持原有“锁内采集快照、锁外调用处理器与事件”的并发策略不变
- 为销毁路径新增 `DestroyAsync_Should_NotifyResumedGroups`，验证当多个暂停组在销毁前仍为暂停态时，
  处理器和事件订阅者都会收到 `IsPaused=false` 的恢复信号
- 验证通过：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -p:RestoreFallbackFolders= -nologo -clp:"Summary;WarningsOnly"`
    - 结果：`27 Warning(s)`，`0 Error(s)`；`PauseStackManager.cs` 已不再出现在 `MA0051` 列表
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter FullyQualifiedName~PauseStackManagerTests -p:RestoreFallbackFolders=`
    - 结果：`25 Passed`，`0 Failed`
- 下一步保持原节奏：只在 `CoroutineScheduler` 或 `Store` 中二选一继续，不与其他 warning 家族混做

## 2026-04-21 — RP-003

### 阶段：Architecture 生命周期 `MA0051` 收口（RP-003）

- 依据 active tracking 中“继续只选一个 `GFramework.Core` 结构性切入点”的约束，选定
  `GFramework.Core/Architectures/ArchitectureLifecycle.cs`，因为文件体量适中且已有
  `ArchitectureLifecycleBehaviorTests` 覆盖阶段流转、销毁顺序和 late registration 行为
- 先用 `warnings-only` 定向构建确认 `ArchitectureLifecycle.InitializeAllComponentsAsync` 仍在报
  `MA0051`，随后把主流程拆成：
  - `CreateInitializationPlan`
  - `InitializePhaseComponentsAsync`
  - `MarkInitializationCompleted`
- 保持原有阶段顺序 `Before* -> After*`、批量日志文本和异步初始化策略不变，只压缩主方法长度
- 修正新增 `InitializationPlan` 记录类型的 XML `<param>` 名称大小写，避免引入文档告警
- 验证通过：
  - `dotnet build GFramework.Core/GFramework.Core.csproj -c Release -t:Rebuild --no-restore -p:UseSharedCompilation=false -p:TargetFramework=net8.0 -p:RestoreFallbackFolders= -nologo -clp:Summary;WarningsOnly`
    - 结果：`29 Warning(s)`，`0 Error(s)`；`ArchitectureLifecycle.cs` 已不再出现在 warning 列表
  - `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter FullyQualifiedName~ArchitectureLifecycleBehaviorTests -p:RestoreFallbackFolders=`
    - 结果：`6 Passed`，`0 Failed`

## 2026-04-21 — RP-004

### 阶段：PR review follow-up（RP-004）

- 使用 `gframework-pr-review` 抓取当前分支 PR #263 的最新 CodeRabbit review threads、MegaLinter 摘要与 CTRF 测试结果，
  只接受仍能在本地工作树复现的 review 点
- 在 `GFramework.Cqrs/Internal/CqrsHandlerRegistrar.cs` 中将 `TryCreateGeneratedRegistry` 的 `out` 参数改为
  `[NotNullWhen(true)] out ICqrsHandlerRegistry?`，移除三处 `null!` 抑制，保持激活失败时的日志文本与回退语义不变
- 修正 active trace 中重复的 `## 2026-04-21` 二级标题，消除 CodeRabbit 报告的 markdownlint `MD024`
- 核实 PR 信号后确认：当前 CTRF 报告为 `2134 passed / 0 failed`；MegaLinter 唯一告警来自 CI 环境中的 `dotnet-format`
  restore 失败，不是本地代码格式问题
- 验证通过：
  - `dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:UseSharedCompilation=false -p:RestoreFallbackFolders=`
    - 结果：`0 Warning(s)`，`0 Error(s)`

## 2026-04-21 — RP-002

### 阶段：CQRS `MA0051` 收口（RP-002）

- 依据 active tracking 中“先只选一个结构性切入点”的约束，选定 `GFramework.Cqrs/Internal/CqrsHandlerRegistrar.cs`
  作为低风险下一步，因为它已有稳定的 targeted test 覆盖 generated registry、reflection fallback、缓存和重复注册行为
- 将 `TryRegisterGeneratedHandlers` 拆分为 registry 激活、批量注册和 fallback 结果构建三个辅助阶段，同时把
  `GetReflectionFallbackMetadata` 的直接类型解析与按名称解析拆开，降低长方法复杂度但不改日志文本与回退语义
- 顺手修正 `RegisterAssemblyHandlers` 内部调试日志的缩进，未改注册顺序、生命周期或服务描述符写入逻辑
- 验证通过：
  - `dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release --no-restore -p:TargetFramework=net8.0 -p:UseSharedCompilation=false -p:RestoreFallbackFolders=`
    - 结果：`0 Warning(s)`，`0 Error(s)`
  - `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --filter FullyQualifiedName~CqrsHandlerRegistrarTests -p:RestoreFallbackFolders=`
    - 结果：`11 Passed`，`0 Failed`
- 新发现的环境注意事项：
  - 当前 WSL worktree 下若不显式传入 `-p:RestoreFallbackFolders=`，Linux `dotnet` 会读取不存在的 Windows fallback package
    folder 并导致 `ResolvePackageAssets` 失败
  - sandbox 内运行 `dotnet` 会因 MSBuild named-pipe 限制失败；需要在提权上下文中执行 .NET 验证

# 2026-04-23

- RP-034 / PR #273 review follow-up：
  - 使用 `gframework-pr-review` 抓取当前分支 PR #273 的 latest-head review threads、MegaLinter 和测试摘要。
  - 本地复核后确认仍成立的项集中在 `SchemaConfigGenerator` helper XML 文档、
    `GeneratorSnapshotTest` 的 `StringComparison.Ordinal` 与 snapshot 路径空值防御、
    `AutoRegisterModuleGeneratorTests` 的 XML 文档位置，以及
    `SchemaConfigGeneratorSnapshotTests` 的 monster snapshot 覆盖缺口。
  - 已扩展 monster schema 场景以覆盖 `dependentRequired`、`dependentSchemas`、`allOf` 与 object-focused
    `if/then/else`，并同步更新 `MonsterConfig.g.txt` 的约束快照。
  - `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build GFramework.Game.SourceGenerators/GFramework.Game.SourceGenerators.csproj -c Release -p:RestoreFallbackFolders=`
    通过；离线 NuGet vulnerability audit 产生 `NU1900`。
  - `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore -p:RestoreFallbackFolders= -m:1`
    通过；测试项目保留既有 `MA0051` warning 基线。
  - `DOTNET_CLI_HOME=/tmp/dotnet-home dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~SchemaConfigGeneratorSnapshotTests|FullyQualifiedName~AutoRegisterModuleGeneratorTests" -m:1`
    通过，`4` 个用例全部通过；需要在沙箱外执行以绕过 `vstest` 本地 socket 权限限制。
  - 下一步：提交本轮修复并在需要时重新抓取 PR review，确认 open threads 是否随新提交收敛。
