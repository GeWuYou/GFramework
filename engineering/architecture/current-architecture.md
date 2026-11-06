# 当前架构

核对日期：2026-11-07。本页随核心治理实现更新，当前任务与验证见[路线图](../roadmap/core-governance.md)。后续方向见[架构决策](adr-001-standard-dotnet-foundation.md)。

## 包依赖与责任

下表箭头表示左侧项目引用右侧项目。依据是各项目的 `ProjectReference`，不代表每条调用均经过所有包。

| 层 | 当前依赖与责任 | 源码 |
| --- | --- | --- |
| 聚合包 | 直接聚合 Core 与 Game；不包含 CQRS、Godot 或生成器 | [聚合项目](../../GFramework.csproj) |
| Core | 依赖 Core.Abstractions；承载架构、容器适配及通用子系统，不含 CQRS 引用 | [Core 项目](../../GFramework.Core/GFramework.Core.csproj) |
| Core.Abstractions | 依赖 Microsoft DI 抽象；上下文与容器契约不再包含 CQRS 能力 | [契约项目](../../GFramework.Core.Abstractions/GFramework.Core.Abstractions.csproj) |
| CQRS | Runtime 依赖 Core.Abstractions 与 Cqrs.Abstractions，运行在标准 DI 上；消息契约包可单独引用 | [CQRS 项目](../../GFramework.Cqrs/GFramework.Cqrs.csproj)、[运行时工厂](../../GFramework.Cqrs/CqrsRuntimeFactory.cs) |
| Game | 依赖 Core 与 Game.Abstractions；配置、设置、存储、Scene/UI 等游戏能力 | [Game 项目](../../GFramework.Game/GFramework.Game.csproj) |
| Godot | 在 Game/Core 之上接入节点、主线程与场景树生命周期 | [Godot 项目](../../GFramework.Godot/GFramework.Godot.csproj)、[宿主架构](../../GFramework.Godot/Architectures/AbstractArchitecture.cs) |
| Arch ECS | 依赖 Core 与自身抽象；服务模块创建 World、解析系统并负责更新与销毁 | [ECS 模块](../../GFramework.Ecs.Arch/ArchEcsModule.cs) |
| Source Generators | 编译消费项目，生成注册表、上下文与宿主样板；运行时包不反向引用生成器 | [边界校验器](../../scripts/validate-runtime-generator-boundaries.py) |

这些引用没有形成项目引用环。Core-only 消费方没有 CQRS 传递依赖；需要消息模型的项目显式安装 `GFramework.Cqrs` 并安装 runtime 模块。契约包拆分也不等于不含其他领域的契约依赖。

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

销毁立即关闭外部上下文访问，取消并等待启动，然后依次等待 OnStoppingAsync、组件业务停止、服务模块停止、根 provider 异步释放。参与者停止按实际开始的逆序进行；单个停止错误不跳过后续动作，最终聚合报告。

启动失败/取消执行同一宿主和 provider 清理，并在清理后完成启动与 Ready 等待者的失败/取消任务。原对象不能重试；重复销毁等待同一成功或失败结果。linked-token 注册随启动完成或移交给销毁操作后释放。

类型/工厂创建的资源由标准 provider 拥有；传入实例仍由调用者拥有。外部实例 alias 不转移所有权。泛型自动 alias 拒绝可释放类型，后者使用一个明确拥有注册，避免多 factory alias 重复跟踪。业务 Destroy 不再次释放 DI 依赖。

生命周期门禁属于本架构拥有的 MicrosoftDiContainer，默认及 Godot 包装上下文都遵守同一配置/停止窗口；自定义容器负责自己的访问和释放契约。通用外部 provider/Generic Host 集成尚不在本批范围。

## 调用、宿主与生成代码

- [ArchitectureContext](../../GFramework.Core/Architectures/ArchitectureContext.cs) 只承载组件获取、事件与环境访问，不含任何 CQRS 成员。request/notification/stream 分发通过 `GFramework.Cqrs.Extensions` 的异步上下文扩展（解析容器中的单例 `ICqrsRuntime` 并绑定架构上下文）或直接解析 `ICqrsRuntime` 完成；未安装 `CqrsRuntimeModule` 的架构没有分发能力。
- [ContextAwareBase](../../GFramework.Core/Rule/ContextAwareBase.cs) 和[生成上下文](../../GFramework.Core.SourceGenerators/Rule/ContextAwareGenerator.cs)只接受实例绑定，无默认 GameContext/provider 回退。兼容的显式全局工具仍在，但核心启动不使用它们。
- [GameConfigModule](../../GFramework.Game/Config/GameConfigModule.cs) 以异步 BeforeUtilityInit hook 首载，类型登记的 lifetime utility 由 provider 拥有，失败也释放 watcher/bootstrap；[独立 bootstrap](../../GFramework.Game/Config/GameConfigBootstrap.cs)路径保留。
- Godot 配置期声明模块，异步启动通过[主线程调度器](../../GFramework.Godot/Architectures/GodotMainThreadDispatcher.cs)等待锚点和模块附加，清理回到同一主线程并使用根停止流程；QueueFree 是引擎排队操作，不声称原生内存已经同步回收。
- [Arch ECS](../../GFramework.Ecs.Arch/ArchEcsModule.cs) 通过实例 RegisterServiceModule 接入；World 工厂在正式 provider 激活后创建，系统业务停止和资源 Dispose 分开。

日志家族已改用 Microsoft.Extensions.Logging 契约：自有 `ILogger`/`ILoggerFactoryProvider`、`LoggerFactoryResolver` 与 `GodotLog.UseAsDefaultProvider()` 已移除，Core 输出引擎（appender/formatter/filter）经 `AppenderLoggerProvider` 继续使用。本批不改写全部工具子系统。原生 Godot/VS Code、Native AOT 和性能需各自独立证据；托管构建/测试不能代替这些验收。
