# 保留的后续事项

核对日期：2026-11-08。依据当前基线源码与旧主题的目标、风险和未勾选项筛选；不继承旧分支、恢复点、PR 状态或历史测试通过数。各项范围与当前验收状态分别在条目中维护。

## 配置系统与编辑工具

状态：deferred。启动条件：出现具体配置编辑需求，且收益超过共享语义维护成本。

- 保留 Runtime/Generator/Tooling 的闭合对象和单一 items 形状边界；oneOf/anyOf、开放对象和 tuple 形状不能单端静默接受。
- 当前已有 GameConfigBootstrap、GameConfigModule、GeneratedConfigCatalog 和只读索引支持，不能把旧计划里的“下一步”重复实现。
- 候选工作：更深对象数组编辑、批量编辑、真实 VS Code 宿主交互验证；先核对当前工具，不能把历史未勾选当作现在仍缺功能。
- 验收：三端相同输入得到一致约束结果，工具清楚提供 raw YAML 回退；不追求完整 JSON Schema 或运行时可写配置。

依据：[Schema Validator](../../GFramework.Game/Config/YamlConfigSchemaValidator.cs)、[生成器](../../GFramework.Game.SourceGenerators/Config/SchemaConfigGenerator.cs)、[编辑工具](../../tools/gframework-config-tool)、[现有采用说明](../../docs/zh-CN/game/config-system.md)。

下一步：如需恢复，先选一个真实输入/编辑场景，核对已有测试和宿主行为，再立项。

## 协程、输入与原生宿主

状态：done。采用范围：引擎无关的千级协程流程与 Godot 桌面宿主。

协程采用统一多阶段内核、可复用身份槽位及显式父子所有权，流程默认 FailFast，节点容器默认 Supervisor。阶段切换后保持目标阶段，场景暂停与真实时间分别管理；任务结果保留异常，终结路径负责枚举器、等待指令和订阅清理。同步子任务、失败／取消／清理重入、深层任务树、固定峰值长期创建销毁及原生生命周期均有行为覆盖。千级空业务推进达到本机时间预算且稳定阶段零分配，Windows/Linux 源工程、导出程序与独立进程续测通过；实际游戏业务和目标设备仍按采用项目测量。采用边界见[当前架构](../architecture/current-architecture.md#协程调度与流程所有权)和[性能样例](../../Samples/CoroutinePerformance/README.md)。

原生样例已验证配置加载、InputEvent/InputMap 重绑定和快照重载、嵌套暂停、节点退出与 QueueFree、归属协程停止、异步关闭。Windows 与 Linux 导出程序分别完成相同集成检查，并通过独立进程恢复设置与存档。

输入契约仍使用动作名、descriptor 和 snapshot，不引入原生事件到通用层。可视布局、手柄实机和具体业务操作随采用项目继续验收。

依据：[原生样例](../../Samples/GodotIntegration/README.md)、[Timing](../../GFramework.Godot/Coroutine/Timing.cs)、[输入契约](../../GFramework.Game.Abstractions/Input)、[Godot 后端](../../GFramework.Godot/Input/GodotInputMapBackend.cs)。

## 文档存档、双向迁移与代码模组

状态：done。采用范围：桌面 .NET 与 Godot 的可信 DLL 扩展、可选 Harmony 和可检查的存档转换。

实现边界：文档信封独立保存游戏身份、schema、内容快照与模组块，迁移及内容修复先于当前 DTO；纯准备和带原始备份的显式提交分离。游戏提供正反向转换和损失策略，框架不推测降级。同一局的缺失模组数据保持纯数据，新局不继承上一局状态，活跃模组按 ID 接收作用域。

代码扩展复用内容包发现、依赖与排序，执行前校验精确指纹授权及 API／能力；每包私有依赖隔离，契约与宿主扩展明确共享。Harmony 是独立可选包，由默认加载上下文持有单一引擎，通过中立调用边界登记并按 owner 清理补丁。启停和更新需要重启。

验收：结构改变导致无法直接转换为当前 DTO 的历史文档仍能先迁移；升降级、歧义、缺链、错误输出、过期确认及备份／提交失败保留原档；缺失模组数据可以保留并在重新启用后恢复；真实 DLL、多个补丁共存和独立撤销通过宿主验证。Godot 源工程与桌面导出分别验证跨进程结果。

依据：[存档仓库](../../GFramework.Game/Data/SaveRepository.cs)、[文档准备](../../GFramework.Game/Data/SaveRepository.Preparation.cs)、[模组宿主](../../GFramework.Game/Modding/GameModHost.cs)、[补丁扩展](../../GFramework.Game.Harmony/HarmonyModExtension.cs)、[当前架构](../architecture/current-architecture.md)。

设置模型的迁移与应用保持独立；压缩、加密与其他 codec 扩展仍在出现具体需要时评估，不因已有文档边界而默认增加处理层。

## 已落地约束与不再续作的旧事务

生成器隔离、可卸载程序集的弱缓存、Godot appender 复用、Conventional Commits 版本语义与安全示例仍按现有实现/规则维护。依据：[弱缓存](../../GFramework.Cqrs/Internal/WeakKeyCache.cs)、[Godot appender](../../GFramework.Godot/Logging/GodotLogAppender.cs)、[版本规则](../../.releaserc.json)、[贡献规则](../../AGENTS.md)。

旧文档覆盖、技能治理、PR review 和版本发布修复不再作为默认活跃任务。相关代码或说明已在当前基线，旧 PR 编号和 pending review 不能自动转成新待办；未来出现具体缺陷再按当前证据处理。当前组合根、生命周期和日志边界见[当前架构](../architecture/current-architecture.md)。


## 游戏运行可靠性与采用闭环

状态：done。通用独立服务、强类型 UI、默认 Godot 接入及官方素材小游戏完成实现与工程验收。

目标：普通服务通过显式依赖工作，agent 和开发者能从公开文档完成界面与游戏流程，不依赖隐藏的上下文、基类或生成器约定。

实现：UI 统一页面、窗口、返回和对话框操作契约，独立声明展示与交互策略，生命周期与原生释放责任明确；Scene、设置、存档与暂停独立采用，显式 dispatcher 保护宿主线程。标准 DI 与 GameRuntimeModule 复用同一实现。保留资源 loader 所有权、临时文件替换和版本迁移机制。

采用工程：[官方素材小游戏](../../Samples/DodgeTheCreeps/README.md) 覆盖真实玩法、强类型菜单、嵌套暂停、设置、确认结果、保存与恢复；[运行时集成示例](../../Samples/GodotIntegration/README.md) 保留配置、输入、协程等采用路径。

验收结论：独立宿主和 Architecture 路径、拒绝／失败／取消／重复停止、宿主线程和一次释放均有回归覆盖；原生集成工程与官方素材小游戏完成公开操作，Windows/Linux 导出程序和独立进程持久化恢复通过。场景守卫接收取消令牌，原生暂停恢复保留原有处理设置。公开 API、模块说明、教程、模板和导航同步，主要示例直接引用可编译源码。

最终可视操作与游戏体验由用户验收，后续采用反馈继续在相应能力内维护。
