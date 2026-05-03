# Godot Logging Compliance Polish 追踪

## 2026-05-02

### 阶段：GodotLogger 优点吸收与宿主层补齐（RP-001）

- 复核 `GodotLogger` 与 `GFramework.Godot.Logging` 后确认：
  - 模板、颜色、Debug/Release 双模式、类别缩写和 Godot 输出路由已基本吸收
  - 真正缺口主要在宿主接入与运行期配置层，而不是输出格式层
- 本轮新建 `godot-logging-compliance-polish` topic，并将当前分支
  `feat/godot-logging-compliance-polish` 映射到该主题
- 为 `GFramework.Godot.Logging` 新增：
  - `GodotLog`
  - `DeferredLogger`
  - `GodotLogConfigurationSource`
  - `GodotLoggerSettings`
  - `GodotLoggerSettingsLoader`
- 关键实现决策：
  - 不把 `Microsoft.Extensions.Logging` 的 builder / provider 生态整套移植进来
  - 保持 `LoggerFactoryResolver` 与 `ArchitectureConfiguration` 仍是主接线方式
  - 只吸收 `GodotLogger` 里对 GFramework 现有模型真正有价值的部分：
    - 配置自动发现
    - 热重载
    - 延迟 logger 初始化
    - 配置命名兼容
- 为让热重载作用于已缓存 logger，调整 `AbstractLogger` 支持动态最小级别提供器，并让
  `GodotLoggerFactoryProvider` / `GodotLogger` 在写入和级别判定时读取最新设置
- 为让结构化日志在 Godot 侧不再退化成纯字符串，扩展：
  - `GodotLogRenderContext`
  - `GodotLogTemplate`
  - `GodotLoggerOptions`
  - `GodotLogger`
  使默认模板支持 `{properties}`，并将 `IStructuredLogger` / `LogContext` 属性渲染到输出中
- 为兼容 `GodotLogger` 原项目配置习惯，在 `GodotLoggerSettingsLoader` 中补充枚举解析兼容：
  - `Info` / `Information`
  - `Fatal` / `Critical`
  - `Warn` / `Warning`
- 同步更新 `docs/zh-CN/godot/logging.md`，把文档结论从“只有薄适配层”刷新成“已具备宿主便利层和热重载语义”
- 已从 `ai-libs/GodotLogger` 复制 MIT 许可证到 `third-party-licenses/GodotLogger/LICENSE`

### RP-001 验证

- `dotnet test GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release --filter FullyQualifiedName~GodotLog -nologo`
  - 结果：通过（11/11）
- `dotnet test GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release -nologo`
  - 结果：通过（69/69）
- `dotnet test GFramework.Core.Tests/GFramework.Core.Tests.csproj -c Release --filter FullyQualifiedName~Logging -nologo`
  - 结果：通过（214/214）

### RP-001 下一步

1. 若继续推进本主题，优先评估 Godot 输出是否应变成 Core 可组合 appender / sink
2. 若出现后续 review 反馈，直接在本 topic 追加 RP-002，而不是重新开临时 local-plan
3. 若本主题阶段性完成，再把详细实现 history 迁入 `archive/`，active 入口只保留恢复点与风险

### 阶段：PR review hardening（RP-002）

- 使用 `$gframework-pr-review` 抓取 PR #314 最新 review payload，确认当前 head 上仍有 CodeRabbit 与 Greptile
  未解决线程
- 接受并处理仍适用的 review 结论：
  - `GodotLog.ConfigurationPath` 不应提前创建全局配置源，`Configure(...)` 需要在 provider 或配置源已创建后 fail-fast
  - 静态配置源需要可显式释放 watcher，因此新增 `GodotLog.Shutdown()`
  - `DeferredLogger` 首次解析改为 `Interlocked.CompareExchange` 发布，避免 `_inner ??=` 并发竞态
  - `GodotLogger` 结构化 `Log(...)` 覆写改为复用 `IsEnabled(level)`，删除重复的最小级别 provider 字段
  - JSON 配置输入需要归一化模板和颜色字典，并拒绝未定义的数字 `LogLevel`
  - `GodotLogTemplate` 模板缓存和分类缓存需要有界，避免热重载或动态 category 长期增长
  - `refactor-scripts/update-namespaces.py` 不能依赖本机绝对路径，也不能把文件处理异常吞成 0 次替换
- 同步补充 Godot logging 内部类型和关键方法 XML 文档，说明热重载、快照发布、分类匹配和模板缓存语义
- 同步更新 `docs/zh-CN/godot/logging.md`，记录 `ConfigurationPath` 的诊断语义和 `Shutdown()` teardown 用法

### RP-002 验证

- `dotnet test GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release --filter FullyQualifiedName~GodotLog -nologo`
  - 结果：通过（15/15）
- `dotnet test GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release -nologo`
  - 结果：通过（73/73）
- `python3 -B refactor-scripts/update-namespaces.py --help`
  - 结果：通过

### RP-002 下一步

1. 提交 RP-002 review hardening 改动
2. 刷新 PR review / CI，确认最新 head 是否关闭已处理线程
3. 若 CI 仍只有 MegaLinter `dotnet-format` restore 失败，优先定位 Actions restore 环境

## 2026-05-03

### 阶段：PR review follow-up（RP-003）

- 再次使用 `$gframework-pr-review` 抓取 PR #314 最新 review payload，确认当前 head 上仍有 CodeRabbit 与
  Greptile 未解决线程
- 本轮验证后接受并处理仍适用的 review 结论：
  - `GodotLoggerSettingsLoaderTests` 公开测试类型与公开测试方法需要 XML 文档
  - `DeferredLogger` 的公开接口成员需要 XML 文档，并且格式化 `Log(...)` 重载不应提前执行 `string.Format`
  - `GodotLogger` 默认构造器不应在每条日志上重新创建 options，结构化属性 key 需要跳过空白并做 trim
  - Godot logging 文档需要给出最小 `appsettings.json` 示例、放置约定和热重载覆盖说明
  - active tracking 不应同时保留 RP-001 与 RP-002 的详细验证计数，trace 重复标题需要消除
- 本轮验证后确认以下旧 review 结论在当前 head 已处理，无需重复改动：
  - `GodotLog.Shutdown()` 已可释放 materialized configuration source 的 watcher
  - hot-reload callback 已走无 `Thread.Sleep` 的 `LoadSettings()`，`Thread.Sleep` 只保留在 startup strict load retry
  - JSON options 归一化、数字 `LogLevel` 校验、GodotLogTemplate 缓存和 namespace 脚本健壮性已在当前 head 存在

### RP-003 验证

- `dotnet test GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release --filter FullyQualifiedName~GodotLog -nologo`
  - 结果：通过（14/14）
- `dotnet test GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release -nologo`
  - 结果：通过（72/72）
- `dotnet format GFramework.Godot.Tests/GFramework.Godot.Tests.csproj --verify-no-changes --no-restore --include ...`
  - 结果：通过，include 范围为本轮修改的三个 C# 文件
- `dotnet format GFramework.Godot.Tests/GFramework.Godot.Tests.csproj --verify-no-changes --no-restore`
  - 结果：未通过；命中既有 `GFramework.Godot.Tests/Coroutine/GodotTimeSourceTests.cs` 行尾与
    `GFramework.Godot.Tests/GlobalUsings.cs` 编码问题，本轮未把该历史格式清理并入 PR review follow-up

### RP-003 下一步

1. 提交 RP-003 review follow-up 改动
2. 刷新 PR review，确认 CodeRabbit / Greptile 线程是否关闭或 stale
3. 若 CI 仍只有 MegaLinter `dotnet-format` restore 失败，继续定位 Actions restore 环境而不是扩大本地格式清理范围

### 阶段：主题归档（RP-004）

- PR #314 已合并，当前分支 head 与 `origin/main` 同为 merge commit `918a61f3`
- 旧 upstream branch `origin/feat/godot-logging-compliance-polish` 已不存在
- 当前 batch stop condition 使用 `origin/main` 作为 baseline；归档前分支累计 diff 为 `0` 个文件
- 接受的收尾动作：
  - 将 `godot-logging-compliance-polish` 从默认 boot active topic 中移除
  - 将主题恢复文档移动到 `ai-plan/public/archive/godot-logging-compliance-polish/`
  - 在 public index 的 archived topics 中保留主题位置和合并结论

### RP-004 验证

- `dotnet build GFramework.sln -c Release`
  - 结果：通过，`0 warning / 0 error`
  - 备注：归档维护只触及 `ai-plan/public/**`，本次 build 用于满足仓库完成标准并确认解决方案仍可构建

### RP-004 下一步

1. 若继续推进 Godot logging 与 Core 的统一输出管线，建立新的 active topic
2. 当前归档维护已完成；后续只需提交并发布归档分支
