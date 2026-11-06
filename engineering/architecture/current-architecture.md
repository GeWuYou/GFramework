# 当前架构

核对日期：2026-11-07。运行时基线：`5b727587f8860761a0c132203cb4d7e0cdae30af`。本页描述已实现行为，未来方向见[架构决策](adr-001-standard-dotnet-foundation.md)。

## 包依赖与责任

下表箭头表示左侧项目引用右侧项目。依据是各项目的 `ProjectReference`，不代表每条调用均经过所有包。

| 层 | 当前依赖与责任 | 源码 |
| --- | --- | --- |
| 聚合包 | 直接聚合 Core 与 Game；通过 Core 间接包含 CQRS，不包含 Godot 或生成器 | [聚合项目](../../GFramework.csproj) |
| Core | 依赖 Core.Abstractions、Cqrs、Cqrs.Abstractions；承载架构、容器适配及通用子系统 | [Core 项目](../../GFramework.Core/GFramework.Core.csproj) |
| Core.Abstractions | 依赖 Cqrs.Abstractions 和 Microsoft DI 抽象；上下文与容器契约包含 CQRS 能力 | [契约项目](../../GFramework.Core.Abstractions/GFramework.Core.Abstractions.csproj) |
| CQRS | Runtime 依赖 Core.Abstractions 与 Cqrs.Abstractions；消息契约包可单独引用 | [CQRS 项目](../../GFramework.Cqrs/GFramework.Cqrs.csproj)、[运行时工厂](../../GFramework.Cqrs/CqrsRuntimeFactory.cs) |
| Game | 依赖 Core 与 Game.Abstractions；配置、设置、存储、Scene/UI 等游戏能力 | [Game 项目](../../GFramework.Game/GFramework.Game.csproj) |
| Godot | 在 Game/Core 之上接入节点、主线程与场景树生命周期 | [Godot 项目](../../GFramework.Godot/GFramework.Godot.csproj)、[宿主架构](../../GFramework.Godot/Architectures/AbstractArchitecture.cs) |
| Arch ECS | 依赖 Core 与自身抽象；服务模块创建 World、解析系统并负责更新与销毁 | [ECS 模块](../../GFramework.Ecs.Arch/ArchEcsModule.cs) |
| Source Generators | 编译消费项目，生成注册表、上下文与宿主样板；运行时包不反向引用生成器 | [边界校验器](../../scripts/validate-runtime-generator-boundaries.py) |

这些引用没有形成项目引用环，但 Core 与 CQRS 的采用边界并不独立。契约包拆分也不等于不含其他领域的契约依赖。

## 启动与依赖解析

[Architecture](../../GFramework.Core/Architectures/Architecture.cs) 构造默认服务包装、配置和日志，并写入进程级日志工厂。其启动顺序为：

1. [Bootstrapper](../../GFramework.Core/Architectures/ArchitectureBootstrapper.cs) 初始化环境，注册内置事件、旧执行器和 CQRS 服务模块。
2. 创建上下文并强制绑定全局 [GameContext](../../GFramework.Core/Architectures/GameContext.cs)，设置容器上下文，注册默认程序集 handlers，执行 `Configurator`。
3. 初始化服务模块，再调用用户的 `OnInitialize()`。
4. [组件注册器](../../GFramework.Core/Architectures/ArchitectureComponentRegistry.cs) 立即创建按类型注册的 System/Model；其构造依赖由[注册期解析器](../../GFramework.Core/Architectures/ArchitectureComponentActivator.cs)解析。
5. [生命周期](../../GFramework.Core/Architectures/ArchitectureLifecycle.cs) 按 ContextUtility → Model → System 初始化，随后 `Freeze()` 构建正式 DI provider，进入 Ready。

普通 Utility 的类型注册使用延迟工厂，与 System/Model 的立即激活不同。预冻结容器查询主要暴露已注册实例，不是完整 Microsoft DI 解析。晚注册配置与冻结容器的限制也不是同一套规则。

## 销毁与资源责任

[ArchitectureDisposer](../../GFramework.Core/Architectures/ArchitectureDisposer.cs) 逆序执行已登记组件的自有 Destroy 契约，隔离单个清理异常，销毁服务模块，广播 Destroyed，然后调用容器 `Clear()`。Architecture 最后按类型解除全局上下文绑定。

[MicrosoftDiContainer](../../GFramework.Core/Ioc/MicrosoftDiContainer.cs) 的 `Clear()` 在冻结后直接返回；只有独立的 `Dispose()` 路径会释放正式根 provider。接口只有同步 `IDisposable`。因此不能把“进入 Destroyed”理解成“所有 DI 资源已经释放”。诊断结果见[核心评估](core-assessment.md)。

## 调用、宿主与生成代码

- [ArchitectureContext](../../GFramework.Core/Architectures/ArchitectureContext.cs) 同时提供服务定位、事件、旧命令/查询及新版 request/notification/stream。同步包装和异步入口并存。
- [ContextAwareBase](../../GFramework.Core/Rule/ContextAwareBase.cs) 在未注入时回退全局上下文，并缓存引用；[生成实现](../../GFramework.Core.SourceGenerators/Rule/ContextAwareGenerator.cs)还包含按生成类型共享的静态 provider。仅修改基类不能改变生成路径。
- [GameConfigModule](../../GFramework.Game/Config/GameConfigModule.cs) 在 Utility 初始化前的同步阶段钩子中等待异步首载，并通过 Utility 停止热重载；[GameConfigBootstrap](../../GFramework.Game/Config/GameConfigBootstrap.cs) 也支持不使用 Architecture 的直接接入。
- Godot 通过 [ArchitectureAnchor](../../GFramework.Godot/Architectures/ArchitectureAnchor.cs) 接场景树销毁，并用 [GodotArchitectureContext](../../GFramework.Godot/Architectures/GodotArchitectureContext.cs) 防止危险的主线程同步 CQRS 调用；这不保证通用生命周期回调始终在 Godot 主线程。
- ECS 模块在 Register 创建 World，Initialize 解析适配系统。未来改变容器构建时机时，必须重新检查其初始化和失败清理窗口。

当前 Core 含很多独立价值的能力，但本轮没有逐一验证协程、状态管理、资源、池、日志、配置的全部运行时语义，也未证明 Native AOT 或引擎实机支持。
