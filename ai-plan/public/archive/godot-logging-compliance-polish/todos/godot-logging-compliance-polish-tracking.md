# Godot Logging Compliance Polish 跟踪

## 目标

继续把 `GFramework.Godot.Logging` 从“基础可用的 Godot 输出适配”收敛成“对齐 `GodotLogger` 优点、但保持
GFramework 自身日志抽象不分叉”的稳定宿主层，并为后续 Godot / Core 日志统一留下清晰恢复点。

## 完成状态

- 恢复点编号：`GODOT-LOGGING-COMPLIANCE-POLISH-RP-003`
- 当前阶段：`已完成并归档`
- 完成结论：
  - 已补齐 `GodotLog` 静态入口、延迟 logger 解析、配置自动发现与热重载
  - 已让 `GodotLoggerFactoryProvider` 对已缓存 logger 生效动态配置，而不是只在新建 logger 时读快照
  - 已让 `GodotLogger` 支持 `{properties}` 占位符，并把 `IStructuredLogger` / `LogContext` 属性落到 Godot 输出
  - 已兼容 `GodotLogger` 风格配置值，如 `Information` / `Critical`
  - 已处理 PR #314 最新 AI review 中仍适用的 XML docs、热路径分配、结构化属性兜底、文档示例和 tracking 精简问题
  - PR #314 已合并到 `origin/main`，当前主题从默认 boot 路径移入归档

## 当前状态摘要

- `GFramework.Core` 仍是主日志框架；`GFramework.Godot` 没有引入第二套业务日志 API
- `GFramework.Godot.Logging` 现在已经补上原 `GodotLogger` 项目最有价值的宿主便利层：
  - `GodotLog`
  - `DeferredLogger`
  - 配置文件自动发现
  - 文件热重载
  - 结构化属性渲染
- 本轮没有把 `Microsoft.Extensions.Logging` 的 `ILoggingBuilder` / `ILoggerProvider` 生态原样搬入 GFramework
- `AbstractLogger` 已支持动态最小级别提供器，为 Godot 配置热更新生效打通基础能力

## 当前活跃事实

- 本主题归档前由分支 `feat/godot-logging-compliance-polish` 驱动，PR #314 合并后已从
  `ai-plan/public/README.md` 的 active topic 映射移除
- `ai-libs/GodotLogger` 的 MIT 许可证已复制到 `third-party-licenses/GodotLogger/LICENSE`
- `GodotLog` 当前的配置发现顺序为：
  - `GODOT_LOGGER_CONFIG`
  - 可执行目录 `appsettings.json`
  - `res://appsettings.json`
- `GodotLog.Configure(...)` 仍要求在首次 materialize provider 前调用；延迟 logger 会避免 `static readonly` 字段过早锁死配置
- `GodotLoggerFactoryProvider` 当前按 logger 名称缓存实例，但每次判定级别和写日志都会读取最新 `GodotLoggerSettings`
- Godot 模板默认已扩展为包含 `{properties}`，因此结构化属性和 `LogContext` 会进入渲染结果
- 配置加载兼容两套级别命名：
  - GFramework 风格：`Info` / `Fatal`
  - `GodotLogger` 风格：`Information` / `Critical`
- 现有设计仍保留 UTC 时间戳语义，没有为了对齐原项目而默认切回本地时间
- `GodotLog.ConfigurationPath` 现在不会提前 materialize 全局配置源；`GodotLog.Shutdown()` 可释放已创建配置源的 watcher
- 配置 JSON 会先归一化模板和颜色字典，并拒绝未定义的数字 `LogLevel`
- `GodotLogTemplate` 的模板缓存和分类格式缓存已改为有界并发缓存，避免热重载或动态 category 长期单向增长
- `refactor-scripts/update-namespaces.py` 已移除本机绝对路径默认值，并会把文件处理失败汇总成非零退出码
- PR #314 最新 review 中，GodotLog watcher 释放、热重载回调阻塞、配置归一化、数字 `LogLevel` 校验、
  模板缓存和脚本健壮性问题已在当前 head 验证为已处理；本轮只继续修仍适用的问题
- PR #314 最新 follow-up 中，`DeferredLogger` 格式化重载现在委托给 inner logger，`GodotLogger` 默认 options
  provider 已改为构造时缓存，结构化属性会跳过空白 key 并使用 trimmed key

## 收尾风险

- 双入口生命周期风险：如果同一宿主同时混用 `LoggerFactoryResolver.Provider` 与 `GodotLog`，需要明确谁是最终默认 provider
  - 缓解措施：当前文档与实现都保留 `GodotLog.UseAsDefaultProvider()`，并继续把 `ArchitectureConfiguration` 方式写成默认推荐路径
- Core / Godot 管线分离风险：Godot 侧虽然已有热重载与配置发现，但还没有变成 Core 可组合 appender
  - 缓解措施：若后续重启本方向，应新建独立 topic 评估“Godot sink / appender 化”，不要在已归档主题继续扩张独立的 Godot logging 面
- 配置热重载的宿主差异风险：Godot 编辑器、导出包和测试宿主的文件系统语义不完全一致
  - 缓解措施：active 入口先锁定 discovery / reload 语义，后续若遇到平台差异，再用定向回归和文档补充收口
- `GodotLog.ConfigurationPath` 的“不会 materialize”语义没有加入自动化测试
  - 缓解措施：直接调用会触碰 Godot project path resolver，在普通 test host 中可能崩溃；当前以实现和文档约束记录，后续若增加 Godot 宿主集成测试再覆盖

## 活跃文档

- 当前跟踪：[godot-logging-compliance-polish-tracking.md](./godot-logging-compliance-polish-tracking.md)
- 当前 trace：[godot-logging-compliance-polish-trace.md](../traces/godot-logging-compliance-polish-trace.md)

## 验证说明

- `dotnet test GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release --filter FullyQualifiedName~GodotLog -nologo`
  - 结果：通过
  - 备注：RP-003 follow-up 后 Godot logging 定向测试共 `15` 项通过
- `dotnet test GFramework.Godot.Tests/GFramework.Godot.Tests.csproj -c Release -nologo`
  - 结果：通过
  - 备注：RP-003 follow-up 后 Godot 测试项目共 `73` 项通过
- `dotnet format GFramework.Godot.Tests/GFramework.Godot.Tests.csproj --verify-no-changes --no-restore --include ...`
  - 结果：通过
  - 备注：include 范围为本轮修改的 C# 文件；全项目 format 仍命中既有行尾 / 编码问题，详见 trace
- `dotnet build GFramework.sln -c Release`
  - 结果：通过，`0 warning / 0 error`
  - 备注：2026-05-03 在归档维护分支补跑仓库级 Release build，验证归档改动不会影响解决方案构建
- 历史验证明细已保留在 [执行 trace](../traces/godot-logging-compliance-polish-trace.md) 的 `RP-001 验证` 与
  `RP-002 验证` 小节，active tracking 入口只保留当前恢复点相关结果

## 归档说明

1. 本主题已随 PR #314 合并到 `origin/main`
2. 默认 boot 索引不再指向本主题
3. 后续若继续做 Godot logging 与 Core appender / sink 的统一设计，应建立新的 active topic
