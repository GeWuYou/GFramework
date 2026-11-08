# 当前架构

核对日期：2026-11-08。本页随核心治理实现更新，当前任务与验证见[路线图](../roadmap/core-governance.md)。后续方向见[架构决策](adr-001-standard-dotnet-foundation.md)。

## 包依赖与责任

下表箭头表示左侧项目引用右侧项目。依据是各项目的 `ProjectReference`。

| 层 | 当前依赖与责任 | 源码 |
| --- | --- | --- |
| 聚合包 | 直接聚合 Core 与 Game；不包含 CQRS、Godot 或生成器 | [聚合项目](../../GFramework.csproj) |
| Core | 依赖 Core.Abstractions；承载架构、容器适配及通用子系统，不含 CQRS 引用 | [Core 项目](../../GFramework.Core/GFramework.Core.csproj) |
| Core.Abstractions | 依赖 Microsoft DI 抽象；上下文与容器契约不再包含 CQRS 能力 | [契约项目](../../GFramework.Core.Abstractions/GFramework.Core.Abstractions.csproj) |
| CQRS | Runtime 依赖 Core.Abstractions 与 Cqrs.Abstractions，运行在标准 DI 上；消息契约包可单独引用 | [CQRS 项目](../../GFramework.Cqrs/GFramework.Cqrs.csproj)、[运行时工厂](../../GFramework.Cqrs/CqrsRuntimeFactory.cs) |
| Game | 依赖 Core 与 Game.Abstractions；配置、设置、存储、Scene/UI 等游戏能力 | [Game 项目](../../GFramework.Game/GFramework.Game.csproj) |
| Game.Harmony | 可选依赖 Game 与 Lib.Harmony；共享补丁引擎、提供模组 owner 作用域，不由 Game/Godot 传递引入 | [补丁项目](../../GFramework.Game.Harmony/GFramework.Game.Harmony.csproj) |
| Godot | 在 Game/Core 之上接入节点、主线程与场景树生命周期 | [Godot 项目](../../GFramework.Godot/GFramework.Godot.csproj)、[宿主架构](../../GFramework.Godot/Architectures/AbstractArchitecture.cs) |
| Arch ECS | 依赖 Core 与自身抽象；服务模块创建 World、解析系统并负责更新与销毁 | [ECS 模块](../../GFramework.Ecs.Arch/ArchEcsModule.cs) |
| Source Generators | 编译消费项目，生成注册表、上下文与宿主样板；运行时包不反向引用生成器 | [边界校验器](../../scripts/validate-runtime-generator-boundaries.py) |

这些引用没有形成项目引用环。Core-only 消费方没有 CQRS 传递依赖；需要消息模型的项目显式安装 `GFramework.Cqrs` 并安装 runtime 模块。契约包仍可引用其实际需要的其他领域契约。

## 启动与依赖解析

[Architecture](../../GFramework.Core/Architectures/Architecture.cs) 创建当前实例配置、容器访问窗口和上下文。日志契约是 Microsoft.Extensions.Logging：工厂来自 `LoggerProperties.LoggerFactory`，缺省时实例自建控制台工厂并在销毁时释放；实例工厂注册进 DI 容器，没有进程级日志覆盖。同一容器不能同时属于两个架构。

1. [Bootstrapper](../../GFramework.Core/Architectures/ArchitectureBootstrapper.cs) 登记环境、实例上下文与实例服务模块；CQRS 基础设施通过显式安装的 `CqrsRuntimeModule`（`architecture.UseCqrsRuntime(...)` 或 `RegisterServiceModule(new CqrsRuntimeModule(...))`）进入同一列表，不在默认模块列表中。
2. 执行 Configurator 与 ConfigureServices，只记录服务描述、组件元数据、实例服务模块和阶段钩子。
3. [容器](../../GFramework.Core/Ioc/MicrosoftDiContainer.cs) Freeze 一次，启用标准作用域与构造依赖验证；之后禁止注册。
4. [组件注册器](../../GFramework.Core/Architectures/ArchitectureComponentRegistry.cs) 从同一 provider 激活组件、显式注入上下文并执行 onCreated。
5. 服务模块按稳定优先级启动，组件按 Utility → Model → System 初始化；异步阶段 hook 接收取消令牌。
6. OnStarted/OnStartedAsync 完成宿主准备后进入 Ready。同步启动拒绝异步组件、钩子与宿主。

CQRS runtime 使用标准服务解析：`services.AddCqrsRuntime()` 创建单例 runtime，handler、pipeline 行为与通知发布策略通过 `IServiceCollection` 扩展登记。旧注册期解析器与单例缓存已移除，Context 不再把普通 Transient/Scoped 服务缓存为单例。

## 停止、失败与资源责任

销毁立即关闭外部上下文访问，取消并等待启动，然后依次等待 OnStoppingAsync 关闭接入、组件业务停止、服务模块停止、OnStoppedAsync 分离宿主、根 provider 异步释放。宿主根节点在业务停止期间保持存活，允许路由异步回调完成；Godot 在 OnStoppedAsync 才分离模块和排队释放锚点。参与者停止按实际开始的逆序进行；单个停止错误不跳过后续动作，最终聚合报告。

启动失败/取消执行同一宿主和 provider 清理，并在清理后完成启动与 Ready 等待者的失败/取消任务。原对象不能重试；重复销毁等待同一成功或失败结果。linked-token 注册随启动完成或移交给销毁操作后释放。

类型/工厂创建的资源由标准 provider 拥有；传入实例仍由调用者拥有。外部实例 alias 不转移所有权。泛型自动 alias 拒绝可释放类型，后者使用一个明确拥有注册，避免多 factory alias 重复跟踪。业务 Destroy 不再次释放 DI 依赖。

生命周期门禁属于本架构拥有的 MicrosoftDiContainer，默认及 Godot 包装上下文都遵守同一配置/停止窗口；自定义容器负责自己的访问和释放契约。通用外部 provider/Generic Host 集成尚不在本批范围。

## 调用、宿主与生成代码

- [ArchitectureContext](../../GFramework.Core/Architectures/ArchitectureContext.cs) 只承载组件获取、事件与环境访问，不含任何 CQRS 成员。request/notification/stream 分发通过 `GFramework.Cqrs.Extensions` 的异步上下文扩展（解析容器中的单例 `ICqrsRuntime` 并绑定架构上下文）或直接解析 `ICqrsRuntime` 完成；未安装 `CqrsRuntimeModule` 的架构没有分发能力。
- [ContextAwareBase](../../GFramework.Core/Rule/ContextAwareBase.cs) 和[生成上下文](../../GFramework.Core.SourceGenerators/Rule/ContextAwareGenerator.cs)只接受实例绑定，无默认 GameContext/provider 回退。兼容的显式全局工具仍在，但核心启动不使用它们。
- [GameConfigModule](../../GFramework.Game/Config/GameConfigModule.cs) 以异步 BeforeUtilityInit hook 首载，类型登记的 lifetime utility 由 provider 拥有，失败也释放 watcher/bootstrap；[独立 bootstrap](../../GFramework.Game/Config/GameConfigBootstrap.cs)路径保留。
- Godot 配置期声明模块，异步启动通过[主线程调度器](../../GFramework.Godot/Architectures/GodotMainThreadDispatcher.cs)等待锚点和模块附加，清理回到同一主线程并使用根停止流程；QueueFree 是引擎排队操作，不声称原生内存已经同步回收。
- [Arch ECS](../../GFramework.Ecs.Arch/ArchEcsModule.cs) 通过实例 RegisterServiceModule 接入；World 工厂在正式 provider 激活后创建，系统业务停止和资源 Dispose 分开。

日志家族已改用 Microsoft.Extensions.Logging 契约：自有 `ILogger`/`ILoggerFactoryProvider`、`LoggerFactoryResolver` 与 `GodotLog.UseAsDefaultProvider()` 已移除，Core 输出引擎（appender/formatter/filter）经 `AppenderLoggerProvider` 继续使用。其他通用工具子系统保留已有实现。原生宿主、编辑工具、Native AOT 和性能按各自采用目标验证。


## 协程调度与流程所有权

[Core 调度器](../../GFramework.Core/Coroutine/CoroutineScheduler.cs)在单一驱动线程管理多阶段活跃队列。槽位按并发峰值复用，句柄包含调度器身份及代数；遍历快照保留句柄身份，避免回调清理或启动任务后误推进复用槽位。热推进复用存储，Task 完成源仅在调用方请求观察时创建，终结历史有固定上限。

[作用域](../../GFramework.Core/Coroutine/CoroutineScope.cs)区分独立容器与父子流程：根容器及 Godot 节点默认 Supervisor，单次业务流程默认 FailFast。上下文显式启动拥有的子任务，父任务完成前等待子任务收尾；取消与暂停沿所有权向下传播。Join/Await 仅建立等待依赖，不转移目标所有权。资源清理、结果发布与观察回调统一收尾，清理错误不会跳过其余资源。

[Godot Timing](../../GFramework.Godot/Coroutine/Timing.cs)使用一个调度器驱动 Process、Physics 与 Deferred；场景暂停策略不随阶段切换改变，节点退树关闭其全部作用域。Deferred 只表示引擎延迟调用阶段，不是渲染完成信号。通用层不依赖原生节点，其他引擎通过阶段推进与时间增量接入。

[性能样例](../../Samples/CoroutinePerformance/README.md)提供不同活跃量与固定峰值创建销毁的可重复测量。统计中的执行耗时与存活时间分开，千级空业务调度预算不等于具体游戏、设备或渲染性能保证。

## 游戏服务与宿主边界

[UiService](../../GFramework.Game/UI/UiService.cs)、[SceneRouter](../../GFramework.Game/Scene/SceneRouter.cs)、[设置模型](../../GFramework.Game/Setting/SettingsModel.cs)、[数据仓库](../../GFramework.Game/Data/DataRepository.cs)、[存档仓库](../../GFramework.Game/Data/SaveRepository.cs) 和 [暂停管理器](../../GFramework.Core/Pause/PauseStackManager.cs) 通过构造依赖独立采用。对应契约不继承 Model／System／Utility；普通服务不解析全局上下文。

[标准注册扩展](../../GFramework.Game/Runtime/GameServiceCollectionExtensions.cs) 提供 AddGamePause、AddGameUi 和 AddGameScenes。[GameRuntimeModule](../../GFramework.Game/Runtime/GameRuntimeModule.cs) 把相同服务接到 Architecture：配置回调只登记服务，provider 构建后解析实例，停止时依次关闭 UI、Scene 和暂停。模块要求显式 dispatcher，使原生暂停处理器也在宿主线程停止；provider 最后释放拥有对象，传入 root 和 dispatcher 保持调用方所有权。

[IGameDispatcher](../../GFramework.Game.Abstractions/Runtime/IGameDispatcher.cs) 是宿主回调边界。纯托管实现直接执行；同步上下文实现将回调派发到显式提供的宿主上下文，取消尚未开始的工作，等待已经开始的工作完成。Scene 与 UI 各自管理业务状态和串行队列。

## UI 状态与原生视图

[强类型路由](../../GFramework.Game.Abstractions/UI/UiRoute.cs) 和 [UiRegistry](../../GFramework.Game/UI/UiRegistry.cs) 在组合根定义参数与工厂；业务调用复用同一定义实例。展示类型、显示层、模态、输入阻断、暂停和多实例独立声明。窗口默认单实例，关闭默认释放；页面历史与独立窗口各自保留。

所有修改操作、动作分发、对话框完成与停止由同一操作链串行执行。对话框等待用户选择时不持有该链。工厂、生命周期、挂载、焦点和释放经过宿主 dispatcher。只允许动作处理器通过显式上下文关闭自身，生命周期中的嵌套导航被拒绝。

IUiView 是服务拥有的租约，IUiHost 负责挂载与焦点。打开在挂载隐藏态准备，Visible 与 Active 分别管理。失败清理目标并尽可能恢复已有状态；停止关闭接纳、等待在途工作，再绕过守卫清理全部实例。状态快照独立于原生对象，可供业务和诊断读取。

[GodotUiRoot](../../GFramework.Godot/UI/GodotUiRoot.cs)、[GodotUiView](../../GFramework.Godot/UI/GodotUiView.cs) 和注册扩展落地普通 Control 场景；可选业务接口只承担打开／关闭或交互通知。原生适配器保存并恢复 authored 输入设置，模态 root 负责遮罩与焦点。页面包装生成器、旧层级行为家族和旧 UI 路由体系已移除。

## 场景、设置与持久化

场景先验证守卫并准备目标，准备失败保留旧栈；已发生的业务副作用由业务负责补偿。准备和进入接收调用令牌，退出与卸载使用非取消令牌。停止共享同一结果，清理错误聚合报告。Godot 场景工厂通过构造函数接收注册表，并回收创建失败的节点。

设置初始化先独立准备全部加载与迁移结果，再更新现有实例；失败保留当前数据，IsInitialized 为 false，并向调用方返回聚合错误。每个设置应用器通过显式 dispatcher 执行，独立失败不跳过其他项，最终报告聚合错误。仓库和设置通知通过显式回调接入宿主事件系统。

FileStorage 和 GodotFileStorage 共用同目录临时文件替换逻辑；Godot 可变数据使用 user://，res:// 用于只读资源。取消控制操作接纳、锁等待和后续步骤；已开始的底层写入等待到完成。一般数据仓库与设置保持各自格式；槽位存档使用独立文档信封，结构版本与业务 DTO 解耦。资源缓存保留创建条目时的 loader 释放责任，活跃句柄阻止普通卸载。

## 内容会话与代码模组

[内容发现](../../GFramework.Game/Content/ContentPackDiscovery.cs)、[依赖解析](../../GFramework.Game/Content/ContentPackResolver.cs)和[目录合并](../../GFramework.Game/Content/ContentCatalogLoader.cs)创建一次启动会话。依赖先于玩家偏好，同字段后加载者覆盖；目录在 schema／跨表引用校验完成后发布，来源保留包、文件和操作位置。游戏持有类型、schema 与业务 API。

清单格式 v2 的 `code` 声明唯一 DLL 和入口类型及所需代码 API／能力，本身不提供信任。[GameModHost](../../GFramework.Game/Modding/GameModHost.cs)在任何模组代码执行前检查全部选择和精确包指纹授权，分别为每包准备加载上下文。框架契约、游戏 API 与扩展程序集明确共享，私有托管依赖留在包范围。宿主通过 admission 接入内容 loader，普通数据入口拒绝静默忽略代码。

模组不是根容器中的动态注册描述；根 provider 仍在启动前冻结。`IGameModContext.Services` 暴露宿主选定的 API 和模组本地作用域，不重新打开组合根。原生资源准备完成后按包顺序初始化与启动；失败与关闭逆序停止，即使初始化部分完成也执行清理。模组跟踪的资源、事件订阅和扩展作用域先于原生宿主释放。加载上下文保持进程生命周期，模组选集变化必须重启。

[Harmony 扩展](../../GFramework.Game.Harmony/HarmonyModExtension.cs)是独立可选能力。补丁引擎由适配器加载／复用到 `AssemblyLoadContext.Default`，该确切的 `0Harmony` 程序集再共享给模组。这样 MonoMod 生成代理和补丁引擎使用同一类型身份，而不会在独立引擎加载上下文与默认上下文之间形成第二份引擎类型。背景见[上游类型身份问题](https://github.com/pardeike/Harmony/issues/642)。

[引擎桥接](../../GFramework.Game.Harmony/HarmonyEngine.cs)限定 `0Harmony 2.4.3.0` 身份。无参扩展优先复用默认上下文中匹配的现有引擎，否则按名称解析，再尝试应用根目录的 `0Harmony.dll`；自定义宿主可以通过构造参数提供宿主拥有的绝对引擎路径。缺失或不兼容引擎明确失败，不自动改写运行时内部状态。适配器通过已知公开 API 桥接，公开方法补丁描述使用 `ModPatchMethod` 与 BCL `MethodInfo`，不静态暴露另一加载上下文中的 `HarmonyLib` 类型；正常注解和 `PatchAll(Assembly)` 仍使用共享引擎。

补丁 owner 按游戏和模组身份创建，排序使用 Harmony 的 priority/before/after；作用域只撤销自身 owner，不代替业务冲突判定。DLL 具有进程权限；加载上下文与 API 选择不是安全沙箱。

Godot PCK 保持独立的宿主信任边界：挂载前校验实际哈希，关闭全局文件替换，挂载失败后需要重启；第三方资料使用受限解码入口。批准代码执行不等于把任意 PCK 认定为官方资源。

宿主共享契约包含其公开签名中的类型定义程序集，例如 `IServiceProvider` 的定义程序集。游戏宿主独立加载上下文持有的实际契约身份优先于默认上下文；这与补丁引擎固定默认上下文是两个独立边界。私有依赖和运行时包元数据的标准库回退不替代契约共享，也不将全部 BCL 强制共享。


## 文档存档与显式提交

[SaveDocument](../../GFramework.Game/Data/SaveDocument.cs)把框架格式、游戏身份、游戏 schema、内容快照及模组版本负载放在业务 DTO 外层。[文档 codec](../../GFramework.Game/Data/ISaveDocumentCodec.cs)负责信封和纯数据转换；默认实现使用 Newtonsoft JSON DOM 并关闭 CLR 类型元数据解释。存储只提供键空间、原始快照和原子替换，不拥有游戏 schema。

[迁移图](../../GFramework.Game/Data/SaveMigrationGraph.cs)捕获明确的有向转换边，分别实现升级和降级，选择唯一最短单调路径；歧义由宿主提供完整版本序列。游戏与每个模组拥有独立 schema，同一个执行器验证转换输出；不从当前 DTO 构造函数推导版本，也不自动生成逆向逻辑。

[准备与提交](../../GFramework.Game/Data/SaveRepository.Preparation.cs)分开。准备在独立文档中转换结构，再按当前 schema 检查并修复内容，最后创建和验证当前 DTO。引用枚举和恢复均接收完整 `SaveDocument`，游戏可以覆盖游戏／模组引用并明确修复或删除模组块；恢复不能改变游戏身份、信封格式或绕过当前 schema，框架最后附加目标内容快照。纯准备钩子不能修改世界或持久化。候选结果保存私有编码字节，公开 DTO、文档和模组 session 都是副本，调用方修改不能替换已确认结果。

普通加载不自动回写；需要变换时交给宿主展示路径、差异与损失说明。降级、有损转换和内容修复要求明确决定。提交校验仓库／槽位、源文件指纹和目标内容会话，保存原始字节备份并再次核对源文件，最终原子写入。复用槽位串行锁、快照及备份能力，不扩张 Core 的必需存储接口。该保护不是跨进程事务。

[ModSaveSession](../../GFramework.Game/Data/ModSaveSession.cs)保留全部历史数据块，只对当前活跃 owner 暴露匹配作用域；缺失模组的负载保持纯数据，不调用缺失代码。同一局的读档草稿携带缺失模组数据，新局空草稿不继承目标槽位旧状态；显式草稿是整份模组数据快照。游戏进度与模组草稿验证后写入同一文档；丢弃或补偿业务状态的决定属于游戏。模组启动不等于单局载入，小游戏在进入运行局面后才开放状态访问，初始化和无当前局时给出明确错误。

旧 typed-save 迁移和内容恢复分支被文档流程替代，同步调整仓库消费者与公开采用说明，不建立兼容双轨。设置迁移继续维护自己的加载和应用生命周期，不强制套用槽位存档流程。

## 采用工程

[独立 .NET 示例](../../Samples/MinimalApp/Program.cs) 展示标准 DI 与可选 Architecture；[官方素材小游戏](../../Samples/DodgeTheCreeps/README.md) 展示普通 Godot 宿主中的强类型 UI、场景、输入、暂停、设置、保存和继续流程。公开教程直接引用样例源文件，编译和原生操作分别验证各自契约。
