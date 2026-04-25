# Analyzer Warning Reduction 追踪

# Analyzer Warning Reduction 追踪

## 2026-04-25 — RP-060

### 阶段：并行子批次推进到 `9 files / 480 lines`

- 触发背景：
  - 用户明确要求“循环继续下一轮，直到 75 阈值”，并允许委派 subagent
  - `RP-059` 结束后，相对 `origin/main` 的累计 branch diff 仅 `3` 个文件，继续只做单文件深挖无法有效推进主停止条件
  - 主线程因此将任务拆成 4 个互不重叠的写集：1 个 `YamlConfigLoaderTests.cs` `MA0051` 批次、2 个 runtime `MA0004` 文件、3 个配置测试文件、1 个 Godot 测试文件
- 主线程实施：
  - 本地复核 `YamlConfigLoaderTests.cs` 的 `MA0051` 热点边界，确认前四个纯加载测试比热重载测试更适合作为 helper 抽取起点
  - 用 explorer 只读排序下一批候选，再把 4 个 disjoint 写集交给 worker 并持续复核主工作树状态
  - 接受并保留以下提交进入当前分支：
    - `877d1f3` `fix(godot-tests): 清理模块安装测试异步断言包装`
    - `1dae0b1` `test(game-tests): 清理配置测试中的机械型 MA0004 包装`
    - `27f5a2f` `fix(game): 清理切换管道中的低风险 MA0004`
    - `b27bcb5` `refactor(game-tests): 清理指定加载测试的 MA0051`
- 验证里程碑：
  - `dotnet build GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release`
    - 结果：成功；`0 Warning(s)`、`0 Error(s)`
  - `dotnet test GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~InstallGodotModuleAsync_ShouldThrowBeforeInvokingModuleInstall_WhenAnchorIsMissing"`
    - 结果：成功；`Passed: 1`
  - `dotnet build GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --no-incremental`
    - 结果：成功；`189 Warning(s)`、`0 Error(s)`
    - 结论：`YamlConfigLoaderTests.cs` 的四个纯加载 `MA0051` 热点已清零，剩余热点集中到热重载测试
  - `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~YamlConfigTextValidatorTests|FullyQualifiedName~GameConfigBootstrapTests|FullyQualifiedName~YamlConfigLoaderDependentRequiredTests"`
    - 结果：成功；`Passed: 15`
  - `dotnet build GFramework.Game/GFramework.Game.csproj -c Release`
    - 结果：成功；`519 Warning(s)`、`0 Error(s)`
- 当前结论：
  - 本轮并行批次把相对 `origin/main` 的累计 branch diff 推进到 `9` 个文件、`480` 行
  - 当前剩余最自然的高上下文热点仍在 `YamlConfigLoaderTests.cs` 的热重载 `MA0051`，但从 branch-size 目标看，下一轮更应该优先挑新的单文件热点
  - 只要低风险新文件仍存在，就应继续沿“新文件优先、同文件深挖次之”的策略推进 toward `75 files`

## 2026-04-25 — RP-059

### 阶段：`YamlConfigLoaderTests.cs` 单文件 `MA0004` 清理

- 触发背景：
  - 用户要求继续按 `$gframework-batch-boot 75` 自动推进 warning reduction，需要先按 skill 重新确认基线与 stop-condition
  - 当前 `HEAD` 与本地现有 `origin/main` 都是 `9964962`，因此已提交 branch diff 为 `0` 个文件，仍有充分批次空间
  - `GFramework.Game.Tests/Config/YamlConfigLoaderTests.cs` 已是剩余 warning 的主要热点，但其中 `MA0004` 仍属于机械且低风险的单文件切片
- 主线程实施：
  - 运行 `dotnet build GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --no-incremental` 重新建立观测值，确认 `YamlConfigLoaderTests.cs` 主要由 `Assert.ThrowsAsync(... async () => await ...)` 与 `WaitForTaskWithinAsync` 触发 `MA0004`
  - 将文件内 `44` 处 `Assert.ThrowsAsync<ConfigLoadException>(async () => await loader.LoadAsync(registry))` 统一改为 `Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry))`
  - 在 `WaitForTaskWithinAsync` 中为 `Task.WhenAny` 与最终 `task` await 补齐 `.ConfigureAwait(false)`，避免文件监听测试 helper 继续触发 analyzer
  - 更新 active tracking / trace，明确本轮停止原因是剩余切片已转为 `MA0051` 长方法重构，不再属于同等级低风险清理
- 验证里程碑：
  - `dotnet build GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --no-incremental`
    - 修复前：成功；`249 Warning(s)`、`0 Error(s)`
    - 修复后：成功；`203 Warning(s)`、`0 Error(s)`
    - 结论：`YamlConfigLoaderTests.cs` 不再出现在 `MA0004` warning 输出中，仅剩同文件 `MA0051`
  - `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~YamlConfigLoaderTests"`
    - 结果：成功；`Passed: 74`、`Failed: 0`
- 当前结论：
  - 这轮以单文件单 warning-family 的边界完成了 `YamlConfigLoaderTests.cs` 的 `MA0004` 清理
  - 当前工作树投影相对 `origin/main` 为 `1` 个文件、`92` 行，远低于 `$gframework-batch-boot 75`
  - 下一候选若继续留在同文件，将进入 `MA0051` 长方法拆分，风险高于本轮，适合作为新的独立批次而不是立即连做

## 2026-04-24 — RP-058

### 阶段：PR #286 latest-head review 格式跟进

- 触发背景：
  - 用户要求执行 `$gframework-pr-review`，需要以当前分支 PR 页面而不是本地记忆为准，重新核对 CodeRabbit、MegaLinter 和测试状态
  - 抓取脚本当前解析到的 PR 是 `#286`，最新 reviewed commit 为 `2b707343577193fc9904517e6078149653e95698`
  - 最新 head 上真正未解决的代码线程只剩 `GFramework.Godot/Scene/SceneBehaviorBase.cs:148` 的缩进问题；其余 nitpick 为可选建议或已留待后续批次
- 主线程实施：
  - 运行 `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --json-output /tmp/current-pr-review.json`，确认 PR `OPEN`、测试 `2156/2156` 通过、MegaLinter 仅剩 `dotnet-format` 警告
  - 复核 `SceneBehaviorBase.cs` 后确认 `OnPauseAsync` 的方法签名与方法体缩进异常仍存在于本地源码；同段的 `OnResumeAsync`、`OnUnloadAsync` 也有同类偏差
  - 在不改变行为的前提下统一修正三个方法的缩进，保持现有 XML 注释、`ConfigureAwait(true)` 语义与 Godot 主线程说明不变
  - 更新 active tracking / trace，记录当前 PR review follow-up 已完成，本地剩余外部信号只剩 PR 标题检查
- 验证里程碑：
  - `dotnet build GFramework.Godot/GFramework.Godot.csproj -c Release`
    - 结果：成功；`565 Warning(s)`、`0 Error(s)`
    - 结论：当前格式修复未引入编译错误；模块既有 warning 基线仍存在，但不属于本次 PR review 跟进范围
  - `dotnet format GFramework.Godot/GFramework.Godot.csproj --verify-no-changes --no-restore --include GFramework.Godot/Scene/SceneBehaviorBase.cs`
    - 首次运行：失败；sandbox 环境下在 build host / pipe 建立阶段报错，未进入真实格式比较
    - 提权复验：成功；仅提示 workspace load warning，无格式差异
- 当前结论：
  - PR #286 当前 latest-head 上唯一未解决的实质代码 review thread 已在本地修复
  - MegaLinter 暴露的 `dotnet-format` 问题已被本地 `verify-no-changes` 复验覆盖
  - `Title check: Inconclusive` 仍然存在，但属于 GitHub PR 标题元数据问题，不能通过本地代码提交直接消除

## 2026-04-24 — RP-057

### 阶段：清理 `PersistenceTests.cs` 残余 `MA0004`

- 触发背景：
  - `RP-056` 提交后重新做非增量热点排序时，`GFramework.Game.Tests` 的剩余测试项目 warning 已明显收敛，只剩 `PersistenceTests.cs` 少量 `MA0004` 与 `YamlConfigLoaderTests.cs` 大量 warning
  - 为避免在同一轮直接进入 `YamlConfigLoaderTests.cs` 的大文件高上下文批次，先吃掉 `PersistenceTests.cs` 这个独立小切片
- 主线程实施：
  - 在 `PersistenceTests.cs` 中为统一设置仓库失败缓存一致性相关测试补齐剩余 `.ConfigureAwait(false)`
  - 覆盖保存失败与删除失败两个测试场景中的缓存读取、存在性检查、后续保存和最终验证读取
  - 更新 active tracking / trace，明确下一批若继续推进应单独进入 `YamlConfigLoaderTests.cs`
- 验证里程碑：
  - `dotnet build GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --no-incremental`
    - 热点重排前：成功；`253 Warning(s)`、`0 Error(s)`
    - 修复后：成功；`249 Warning(s)`、`0 Error(s)`
    - 结论：`PersistenceTests.cs` 不再出现在 warning 输出中
  - `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~UnifiedSettingsDataRepository_SaveAsync_When_Persist_Fails_Should_Keep_Cache_Consistent|FullyQualifiedName~UnifiedSettingsDataRepository_DeleteAsync_When_Persist_Fails_Should_Keep_Cache_Consistent"`
    - 结果：成功；`Passed: 2`、`Failed: 0`
- 当前结论：
  - `PersistenceTests.cs` 的残余 warning 已清零，`GFramework.Game.Tests` 剩余热点几乎全部压缩到了 `YamlConfigLoaderTests.cs`
  - 当前工作树投影下，分支体积为 `27` 个文件、`991` 行，仍低于 `$gframework-batch-boot 75`
  - 按 batch skill 的低风险边界，这一轮应在提交后收口；下一轮再把 `YamlConfigLoaderTests.cs` 作为单独批次处理

## 2026-04-24 — RP-056

### 阶段：修复 `GeneratedConfigConsumerIntegrationTests` 编译错误并清零该文件 warning

- 触发背景：
  - `RP-055` 继续推进时，`GeneratedConfigConsumerIntegrationTests.cs` 在 raw string `invalidYaml` 段落附近出现 `CS8999`，导致 `GFramework.Game.Tests` 暂时无法编译
  - 该文件同时仍是项目内少数残留 warning 热点之一，因此适合作为同一批次中的单文件收尾
- 主线程实施：
  - 修复 `GeneratedConfigConsumerIntegrationTests.cs` 中损坏的 `CreateMonsterFiles` raw string 与方法边界，恢复文件可编译状态
  - 保留并整理上一轮已开始的 `.ConfigureAwait(false)` 与断言 helper 抽取
  - 继续将 `AssertGeneratedBindingsLoadResults` 再拆分为 catalog / monster / item 三个辅助方法，清除该文件剩余 `MA0051`
  - 更新 active tracking / trace，沿用 `merge-base(origin/main, HEAD)` 作为 `$gframework-batch-boot 75` 的唯一 stop-condition 口径
- 验证里程碑：
  - `dotnet build GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release`
    - 结果：成功；`59 Warning(s)`、`0 Error(s)`
    - 结论：`GeneratedConfigConsumerIntegrationTests.cs` 不再出现在 warning 输出中
  - `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~GeneratedConfigConsumerIntegrationTests"`
    - 结果：成功；`Passed: 4`、`Failed: 0`
- 当前结论：
  - `GFramework.Game.Tests` 已从 `RP-055` 收尾时的 `63 warning(s)` 进一步收敛到 `59 warning(s)`
  - 当前工作树投影下，分支体积为 `27` 个文件、`943` 行，仍低于 `$gframework-batch-boot 75`
  - 后续若继续自动推进，最自然的下一批将进入 `YamlConfigLoaderTests.cs` 这类高上下文大文件

## 2026-04-24 — RP-055

### 阶段：修正 stop-condition 口径并继续 `GFramework.Game.Tests` 小热点

- 触发背景：
  - `RP-054` 之后复核 batch stop-condition 时，发现之前一度把工作树 diff 错当成了 skill 要求的 branch diff
  - 按正确口径 `merge-base(origin/main, HEAD)` 计算，`RP-054` 提交后的真实分支体积是 `23` 个文件、`603` 行，因此仍可继续下一批
  - 当前剩余 warning 里，`ArchitectureConfigIntegrationTests`、`GameConfigBootstrapTests`、`JsonSerializerTests` 属于独立且低风险的小切片
- 主线程实施：
  - 在 `ArchitectureConfigIntegrationTests.cs` 中补齐异步架构初始化 / 销毁和异常断言的 `.ConfigureAwait(false)`
  - 在 `GameConfigBootstrapTests.cs` 中补齐启动流程、并发初始化断言与 `WaitForTaskWithinAsync` 的 `.ConfigureAwait(false)`
  - 在 `JsonSerializerTests.cs` 中将坐标解析改为 `CultureInfo.InvariantCulture`
  - 顺手清理 `YamlConfigLoaderAllOfTests.cs` 与 `PersistenceTests.cs` 中上一批遗漏的字段态状态检查和异步等待 warning
  - 纠正 active tracking：明确 stop-condition 必须使用 `origin/main...HEAD` 的 merge-base 分支 diff，而不是工作树 diff
- 验证里程碑：
  - `dotnet build GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release`
    - 并行误用 build/test 时：出现 `MSB3026` / `CS2012` 文件占用噪声，不计入代码结论
    - 串行复验：成功；`63 Warning(s)`、`0 Error(s)`
  - `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~ArchitectureConfigIntegrationTests|FullyQualifiedName~GameConfigBootstrapTests|FullyQualifiedName~JsonSerializerTests"`
    - 结果：成功；`Passed: 19`、`Failed: 0`
- 当前结论：
  - `GFramework.Game.Tests` 已从上一批收尾时的 `71 warning(s)` 进一步降到 `63 warning(s)`
  - 这次提交后的分支体积投影为 `26` 个文件、`691` 行，仍低于 `$gframework-batch-boot 75`
  - 剩余热点越来越集中到 `YamlConfigLoaderTests.cs` 与 `GeneratedConfigConsumerIntegrationTests.cs`，后续继续时应把它们视为高上下文批次

## 2026-04-24 — RP-054

### 阶段：`GFramework.Game.Tests` 低风险测试 warning 批次（触发文件数停止阈值）

- 触发背景：
  - 用户要求“直接进入下一批”，继续沿 `$gframework-batch-boot 75` 自动推进 warning reduction
  - 以 `origin/main` 为基线时，上一批提交后分支累计 diff 仍只有 `8` 个文件，足够再落一个独立批次
  - 重新执行 `dotnet clean GFramework.sln -c Release` 仍停在 `ValidateSolutionConfiguration`，因此继续以直接 `dotnet build GFramework.sln -c Release` 的输出挑选低风险热点
- 主线程实施：
  - 从整仓 `Release build` 的 `116 warning(s)` 入口观测值中，选择 `GFramework.Game.Tests` 的小型测试文件和 `PersistenceTestUtilities.cs` 作为当前批次，刻意避开 `YamlConfigLoaderTests.cs` 这类高上下文大文件
  - 在 `YamlConfigLoaderIfThenElseTests.cs`、`YamlConfigLoaderDependentSchemasTests.cs`、`YamlConfigLoaderDependentRequiredTests.cs`、`YamlConfigLoaderNegationTests.cs`、`YamlConfigLoaderAllOfTests.cs`、`YamlConfigLoaderEnumTests.cs`、`YamlConfigTextValidatorTests.cs`、`PersistenceTests.cs` 中补齐 `.ConfigureAwait(false)`，并把字段态 `_rootPath` 的 `ThrowIfNull` 改为显式 `InvalidOperationException`
  - 将 `PersistenceTestUtilities.cs` 拆分为 `TestDataLocation.cs`、`TestSaveData.cs`、`TestVersionedSaveData.cs`、`TestSimpleData.cs`、`TestNamedData.cs`，消除 `MA0048` 并对齐仓库的一文件一主类型风格
  - 在 `YamlConfigSchemaValidatorTests.cs` 中把字段态 `_rootPath` 的校验改成显式状态异常，避免继续触发 `MA0015`
- 验证里程碑：
  - `dotnet clean GFramework.sln -c Release`
    - 结果：失败；停在 `ValidateSolutionConfiguration`，`0 Warning(s)`、`0 Error(s)`
  - `dotnet build GFramework.sln -c Release`
    - 结果：成功；`116 Warning(s)`、`0 Error(s)`
  - `dotnet clean GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release`
    - 结果：失败；clean 阶段提前结束，`0 Warning(s)`、`0 Error(s)`
  - `dotnet build GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release`
    - 第一轮批次后：成功；`80 Warning(s)`、`0 Error(s)`
    - 收尾修正后：成功；`71 Warning(s)`、`0 Error(s)`
  - `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~YamlConfigLoaderIfThenElseTests|FullyQualifiedName~YamlConfigLoaderDependentSchemasTests|FullyQualifiedName~YamlConfigLoaderDependentRequiredTests|FullyQualifiedName~YamlConfigLoaderNegationTests|FullyQualifiedName~YamlConfigLoaderAllOfTests|FullyQualifiedName~YamlConfigLoaderEnumTests|FullyQualifiedName~YamlConfigTextValidatorTests|FullyQualifiedName~YamlConfigSchemaValidatorTests|FullyQualifiedName~PersistenceTests"`
    - 结果：成功；`Passed: 63`、`Failed: 0`
- 当前结论：
  - `GFramework.Game.Tests` 本轮入口热点已从 `116 warning(s)` 收敛到 `71 warning(s)`，且本轮 touched files 不再出现在 warning 输出中
  - 当前工作树相对 `origin/main` 的累计 diff 已达到 `76` 个文件、`986` 行，超过 `$gframework-batch-boot 75` 的主停止阈值
  - 按批处理技能规则，本轮必须在提交当前批次后停止；剩余候选应在新一轮里单独评估，尤其是 `YamlConfigLoaderTests.cs`

## 2026-04-24 — RP-053

### 阶段：`GFramework.Godot` / `GFramework.Godot.Tests` 小批次 warning 清理

- 触发背景：
  - 用户以 `$gframework-batch-boot 75` 要求继续按批次推进 analyzer warning reduction，并以 `origin/main` 作为累计分支 diff 基线
  - 当前 worktree `fix/analyzer-warning-reduction-batch` 相对 `origin/main` 的已提交分支 diff 为 `0` 个文件，具备继续落一个低风险 warning batch 的空间
  - solution-level `dotnet clean GFramework.sln -c Release` 仍在 `ValidateSolutionConfiguration` 阶段失败，因此本轮继续用直接 `dotnet build GFramework.sln -c Release` 建立热点观察值
- 主线程实施：
  - 运行 `dotnet build GFramework.sln -c Release`，确认当前整仓观测值为 `1122 warning(s)`，并从输出中挑选 `GFramework.Godot` 的小范围热点作为本轮批次
  - 在 `GodotYamlConfigEnvironment.cs` 中按“普通文件系统 / Godot 路径”拆分目录枚举 helper，消除 `MA0051`
  - 在 `AbstractArchitecture.cs` 与 `SceneBehaviorBase.cs` 中将必须保留 Godot 主线程上下文的 await 显式改为 `.ConfigureAwait(true)`，清理 `MA0004` 并把线程意图写入注释
  - 在 `GFramework.Godot.Tests` 中补齐异步断言的 `.ConfigureAwait(false)`，并让 `RichTextMarkupTests` 的测试字典显式指定 `StringComparer.Ordinal`
- 验证里程碑：
  - `dotnet clean GFramework.sln -c Release`
    - 结果：失败；停在 `ValidateSolutionConfiguration`，`0 Warning(s)`、`0 Error(s)`
  - `dotnet build GFramework.sln -c Release`
    - 结果：成功；`1122 Warning(s)`、`0 Error(s)`
  - `dotnet build GFramework.Godot/GFramework.Godot.csproj -c Release`
    - 第一轮修复后：成功；`12 Warning(s)`、`0 Error(s)`，仅剩 `MA0004`
    - 第二轮修复后：成功；`0 Warning(s)`、`0 Error(s)`
  - `dotnet test GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release --filter "FullyQualifiedName~AbstractArchitectureModuleInstallationTests|FullyQualifiedName~GodotYamlConfigLoaderTests|FullyQualifiedName~RichTextMarkupTests"`
    - 结果：成功；`Passed: 15`、`Failed: 0`
  - `dotnet build GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release`
    - 并行验证时：成功；`1 Warning(s)`、`0 Error(s)`；`MSB3026` 为与并行 `dotnet test` 竞争输出 DLL 的文件占用
    - 串行复验：成功；`0 Warning(s)`、`0 Error(s)`
- 当前结论：
  - `GFramework.Godot` 与 `GFramework.Godot.Tests` 本轮直接涉及的 warning 已全部清零
  - 当前待提交代码批次相对 `origin/main` 的源码 diff 为 `6` 个文件、`107` 行，距离 `$gframework-batch-boot 75` 主停止阈值仍有充足余量
  - 继续推进的下一批候选将主要落在 `GFramework.Game` 等高 warning 基线模块，已不再属于当前同等级低风险切片，因此本轮在这里收口并进入提交

## 2026-04-24 — RP-052

### 阶段：PR review follow-up（comparer 契约 + `ConfigureAwait(false)` 收尾）

- 触发背景：
  - 当前分支 PR #283 的最新 review 中，`greptile-apps[bot]` 仍有一个未解决线程，指出 `UnifiedSettingsDataRepository.CloneFile` fallback 会静默丢失原 comparer
  - CodeRabbit 另指出 `AutoRegisterExportedCollectionsGeneratorTests.cs` 中还残留 5 处 `await test.RunAsync();`，与同项目其他测试文件的 `.ConfigureAwait(false)` 风格不一致
- 主线程实施：
  - 复核 PR review JSON、`UnifiedSettingsDataRepository.cs`、`UnifiedSettingsFile.cs` 与 `AutoRegisterExportedCollectionsGeneratorTests.cs` 的当前代码，确认只有 comparer 契约线程仍属最新 head 上的实质问题
  - 将 `UnifiedSettingsFile.Sections` 的 XML 注释补充为显式 comparer 契约，并把默认字典初始化改为 `StringComparer.Ordinal`
  - 将 `CloneFile` fallback 从隐式默认 comparer 改为显式 `StringComparer.Ordinal`，并同步修正文档注释，避免继续暗含“保留原语义”的错误表述
  - 把 `AutoRegisterExportedCollectionsGeneratorTests` 中剩余的 5 处 `await test.RunAsync();` 统一为 `.ConfigureAwait(false)`，同时让 `VerifyDiagnosticsAsync` 内部也消费 `ConfigureAwait(false)`
- 验证里程碑：
  - `dotnet build GFramework.Game/GFramework.Game.csproj -c Release`
    - 结果：成功；`533 Warning(s)`、`0 Error(s)`；`GFramework.Game` 仍有既有 warning 基线，本轮 follow-up 仅处理 PR review 指向的 comparer 契约与测试异步等待一致性
  - `dotnet build GFramework.Godot.SourceGenerators.Tests/GFramework.Godot.SourceGenerators.Tests.csproj -c Release`
    - 结果：成功；`0 Warning(s)`、`0 Error(s)`
  - `dotnet test GFramework.Godot.SourceGenerators.Tests/GFramework.Godot.SourceGenerators.Tests.csproj -c Release --no-build`
    - 首次并行复验：失败；`FileNotFoundException`，原因是 `--no-build` 测试在 Release DLL 落盘前启动
    - 串行复验：成功；`Passed: 48`、`Failed: 0`
- 当前结论：
  - PR #283 当前仍打开的 comparer review thread 已在本地代码与 XML 注释层面得到对应修复
  - `AutoRegisterExportedCollectionsGeneratorTests` 的异步等待风格已与同项目其他测试保持一致
  - 当前改动已通过直接受影响测试项目的 Release build 与串行 Release test 复验，可进入提交阶段

## 2026-04-24 — RP-051

### 阶段：`GFramework.Godot.SourceGenerators.Tests` warning 清零

- 触发背景：
  - 用户要求直接运行 `dotnet clean`，不再添加额外 shell 包装；solution-level `dotnet clean` 仍然在 `ValidateSolutionConfiguration` 阶段失败
  - 直接执行仓库根目录 `dotnet build` 成功，并输出 `1184 warning(s)`，说明当前真实热点已从 `GFramework.Godot.SourceGenerators` 转移到对应测试项目
- 主线程实施：
  - 以 `GFramework.Godot.SourceGenerators.Tests` 为独立批次，先确认该项目本地基线为 `24 warning(s)`
  - 在 `BindNodeSignalGeneratorTests.cs`、`AutoSceneGeneratorTests.cs`、`AutoUiPageGeneratorTests.cs`、`GetNodeGeneratorTests.cs`、`AutoRegisterExportedCollectionsGeneratorTests.cs`、`GodotProjectMetadataGeneratorTests.cs` 中抽取共享 source / diagnostic helper，压缩重复长方法
  - 在 `Core/GeneratorTest.cs` 中补充 `ConfigureAwait(false)`，清除项目内唯一 `MA0004`
  - 把 `GFramework.Godot.SourceGenerators.Tests` 项目 warning 从 `24` 降到 `0`
- 验证里程碑：
  - `dotnet build`
    - 结果：成功；`1184 Warning(s)`、`0 Error(s)`
  - `dotnet build GFramework.Godot.SourceGenerators.Tests/GFramework.Godot.SourceGenerators.Tests.csproj`
    - 初始结果：成功；`24 Warning(s)`、`0 Error(s)`
    - 第一批（`BindNodeSignal` + `GeneratorTest`）后：`16 Warning(s)`
    - 第二批（`AutoScene` / `AutoUiPage` / `GetNode`）后：`8 Warning(s)`
    - 第三批（`Registration` / `Project`）后：`1 Warning(s)`
    - 收尾修复后：成功；`0 Warning(s)`、`0 Error(s)`
  - `dotnet build GFramework.Godot.SourceGenerators.Tests/GFramework.Godot.SourceGenerators.Tests.csproj -c Release`
    - 结果：成功；`0 Warning(s)`、`0 Error(s)`
  - `dotnet test GFramework.Godot.SourceGenerators.Tests/GFramework.Godot.SourceGenerators.Tests.csproj -c Release --no-build`
    - 结果：成功；`Passed: 48`、`Failed: 0`
- 当前结论：
  - `GFramework.Godot.SourceGenerators.Tests` 已在 `Debug` / `Release` 构建下达到 `0 warning(s)`
  - 按 `origin/main` merge-base 计算并只纳入当前暂存批次时，累计分支 diff 为 `23` 个文件，低于 `$gframework-batch-boot 75` 的主停止阈值
  - 仓库根目录 `dotnet clean` 仍无法稳定产出新的 clean 基线，需要在下一轮单独排查
  - 当前 worktree 已有与本批次无关的既有改动；提交时必须只暂存 analyzer warning reduction 相关文件

## 2026-04-24 — RP-050

### 阶段：clean-build 基线修正与 `GFramework.Godot.SourceGenerators` 切片清零

- 触发背景：
  - 用户确认之前的 `0 Warning(s)` 来自增量构建假阴性；只有先 `dotnet clean` 再 `dotnet build`，warning 才会重新出现
  - 用户给出 clean solution build 的真实结果：`Build succeeded with 1193 warning(s)`
- 主线程实施：
  - 纠正当前 topic 的 active todo / trace，把 clean build 作为新的 warning 检查真值
  - 在 `BindNodeSignalGenerator.cs`、`GetNodeGenerator.cs`、`GodotProjectMetadataGenerator.cs` 中完成分阶段方法抽取与字符串比较修正
  - 在 `Registration/AutoRegisterExportedCollectionsGenerator.cs` 中拆分 `TryCreateRegistration`，清除最后一个 `MA0051`
  - 更新 `AGENTS.md`，明确 warning 检查必须先 `dotnet clean` 再 `dotnet build`
- 验证里程碑：
  - `dotnet clean GFramework.Godot.SourceGenerators/GFramework.Godot.SourceGenerators.csproj -c Release`
    - 结果：成功；`0 Warning(s)`、`0 Error(s)`
  - `dotnet build GFramework.Godot.SourceGenerators/GFramework.Godot.SourceGenerators.csproj -c Release`
    - 首次验证：成功；`1 Warning(s)`，剩余 `Registration/AutoRegisterExportedCollectionsGenerator.cs(182,25)` `MA0051`
    - 修复后复验：成功；`0 Warning(s)`、`0 Error(s)`
- 当前结论：
  - `GFramework.Godot.SourceGenerators` 已在 clean `Release` build 下从 9 个 warning 降到 0 个 warning
  - 整仓库 warning 基线仍以用户确认的 clean solution build `1193 warning(s)` 为准
  - 下一轮应继续从 clean solution build 输出中选择新的低风险热点

## Archive Context

- 当前轮次归档：
  - [analyzer-warning-reduction-history-rp042-rp048.md](../archive/todos/analyzer-warning-reduction-history-rp042-rp048.md)
  - [analyzer-warning-reduction-history-rp042-rp048.md](../archive/traces/analyzer-warning-reduction-history-rp042-rp048.md)
- 历史跟踪归档：
  - [analyzer-warning-reduction-history-rp001.md](../archive/todos/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/todos/analyzer-warning-reduction-history-rp002-rp041.md)
- 历史 trace 归档：
  - [analyzer-warning-reduction-history-rp001.md](../archive/traces/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/traces/analyzer-warning-reduction-history-rp002-rp041.md)
