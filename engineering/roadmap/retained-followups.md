# 保留的后续事项

核对日期：2026-11-07。依据当前基线源码与旧主题的目标、风险和未勾选项筛选；不继承旧分支、恢复点、PR 状态或历史测试通过数。以下均不在本轮实现范围。

## 配置系统与编辑工具

状态：deferred。启动条件：出现具体配置编辑需求，且收益超过共享语义维护成本。

- 保留 Runtime/Generator/Tooling 的闭合对象和单一 items 形状边界；oneOf/anyOf、开放对象和 tuple 形状不能单端静默接受。
- 当前已有 GameConfigBootstrap、GameConfigModule、GeneratedConfigCatalog 和只读索引支持，不能把旧计划里的“下一步”重复实现。
- 候选工作：更深对象数组编辑、批量编辑、真实 VS Code 宿主交互验证；先核对当前工具，不能把历史未勾选当作现在仍缺功能。
- 验收：三端相同输入得到一致约束结果，工具清楚提供 raw YAML 回退；不追求完整 JSON Schema 或运行时可写配置。

依据：[Schema Validator](../../GFramework.Game/Config/YamlConfigSchemaValidator.cs)、[生成器](../../GFramework.Game.SourceGenerators/Config/SchemaConfigGenerator.cs)、[编辑工具](../../tools/gframework-config-tool)、[现有采用说明](../../docs/zh-CN/game/config-system.md)。

下一步：如需恢复，先选一个真实输入/编辑场景，核对已有测试和宿主行为，再立项。

## 协程、输入与原生宿主

状态：deferred。启动条件：真实 Godot 项目需要这些操作，或改动触及宿主循环。

- 保留 managed Timing 对 pause/segment/stage-wait 的覆盖；这些测试不能替代真实场景树。
- 待核实：归属节点退出、queue_free、协程终止与取消的实际表现；输入 InputMap/InputEvent 原生读写与快照重置。
- 输入共享契约继续使用动作名、descriptor 和 snapshot，不把 Godot InputEvent 带入通用抽象。
- 验收：在真实 Godot 宿主复现启动、暂停、退出和重置；与纯托管测试分别记录。

依据：[Timing](../../GFramework.Godot/Coroutine/Timing.cs)、[Timing 测试](../../GFramework.Godot.Tests/Coroutine/TimingTests.cs)、[输入契约](../../GFramework.Game.Abstractions/Input)、[Godot 后端](../../GFramework.Godot/Input/GodotInputMapBackend.cs)。

下一步：先建立有明确场景的原生宿主验证，不扩张无需求的输入能力表或图标系统。

## 设置、序列化与持久化

状态：deferred。启动条件：真实存档/设置出现压缩、加密、备份或元数据需求。

- 迁移链已由 VersionedMigrationRunner 共享；继续复用现有校验，不重新创建同类统一抽象。
- 评估 Serializer、Storage、Repository 与 codec 的职责，只有现有接线无法满足实际需求时才新增 pipeline。
- 保留迁移失败不污染当前设置/持久化数据、加载使用迁移快照的约束；不向不可信存档开放无限制多态反序列化。
- 验收：有效迁移、缺链、错误版本、失败保留数据及并发注册快照均有行为证据。

依据：[迁移执行器](../../GFramework.Game/Internal/VersionedMigrationRunner.cs)、[设置模型](../../GFramework.Game/Setting/SettingsModel.cs)、[存档仓库](../../GFramework.Game/Data/SaveRepository.cs)。

下一步：先给出真实数据读写流程和现有重叠点；不以旧计划一句“评估 pipeline”启动新框架。

## 已落地约束与不再续作的旧事务

生成器隔离、可卸载程序集的弱缓存、Godot appender 复用、Conventional Commits 版本语义与安全示例仍按现有实现/规则维护。依据：[弱缓存](../../GFramework.Cqrs/Internal/WeakKeyCache.cs)、[Godot appender](../../GFramework.Godot/Logging/GodotLogAppender.cs)、[版本规则](../../.releaserc.json)、[贡献规则](../../AGENTS.md)。

旧文档覆盖、技能治理、PR review 和版本发布修复不再作为默认活跃任务。相关代码或说明已在当前基线，旧 PR 编号和 pending review 不能自动转成新待办；未来出现具体缺陷再按当前证据处理。历史默认单全局上下文与独占自有日志的决定由[新 ADR](../architecture/adr-001-standard-dotnet-foundation.md)替代。
