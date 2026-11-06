# 核心设计评估

评估日期：2026-11-07。源码基线：`5b727587f8860761a0c132203cb4d7e0cdae30af`。本报告保留治理前基线的设计评估与诊断，不代表后续提交仍有同样缺陷。当前实现见[当前架构](current-architecture.md)，修正与验收结果由[核心路线图](../roadmap/core-governance.md)维护。

## 结论

评估基线有可保留的模块划分、宿主适配与生成器边界。主要问题是注册、实例激活、初始化、全局上下文和资源释放由多套机制共同决定，调用方难以推导完整行为。继续拆分内部 helper 无法单独解决这些所有权和时序问题。

推荐[标准 .NET 基础与可选编排](adr-001-standard-dotnet-foundation.md)。先纠正实例和生命周期，再收敛包与调用入口；不因代码曾由 AI 编写而推定全部需要重写。

## 证据层级与范围

包依赖、启动、销毁和宿主路径见[当前架构](current-architecture.md)。已读 Core/CQRS 运行时、契约、注册生成器和相关测试，以及 Game 配置模块、Godot 架构与 ECS 服务模块。

“已观察”指本轮对该基线执行的最小纯托管探针；“静态事实”可直接从调用路径确定；“待验证风险”尚未通过相应宿主或故障场景验证。已有测试在本轮只是契约证据，未全部重新运行，不代表完整回归或性能验收。

## 治理前优先问题

| 编号 | 发现与机制 | 证据 | 判断 |
| --- | --- | --- | --- |
| C1 | 启动始终 GameContext.Bind，表内拒绝不同上下文；实例级 ScopedContextProvider 无法绕过此启动步骤 | [Bootstrapper.EnsureContext](https://github.com/GeWuYou/GFramework/blob/5b727587f8860761a0c132203cb4d7e0cdae30af/GFramework.Core/Architectures/ArchitectureBootstrapper.cs)、[GameContext.Bind](https://github.com/GeWuYou/GFramework/blob/5b727587f8860761a0c132203cb4d7e0cdae30af/GFramework.Core/Architectures/GameContext.cs)、[provider](https://github.com/GeWuYou/GFramework/blob/5b727587f8860761a0c132203cb4d7e0cdae30af/GFramework.Core/Architectures/ScopedContextProvider.cs) | 静态事实且已观察；当前有意的单上下文约束不符合新目标 |
| C2 | System/Model 在正式 provider 构建前激活；依赖单例保存在独立缓存，Freeze 后不再复用该缓存 | [ComponentActivator](https://github.com/GeWuYou/GFramework/blob/5b727587f8860761a0c132203cb4d7e0cdae30af/GFramework.Core/Architectures/ArchitectureComponentActivator.cs)、[组件注册器](https://github.com/GeWuYou/GFramework/blob/5b727587f8860761a0c132203cb4d7e0cdae30af/GFramework.Core/Architectures/ArchitectureComponentRegistry.cs)、[Freeze](https://github.com/GeWuYou/GFramework/blob/5b727587f8860761a0c132203cb4d7e0cdae30af/GFramework.Core/Ioc/MicrosoftDiContainer.cs) | 静态事实且已观察；同一 Singleton 描述可能有两份实例 |
| C3 | 架构销毁调用 Clear；冻结容器的 Clear 直接返回，未调用已有 Dispose | [ArchitectureDisposer.DestroyAsync](https://github.com/GeWuYou/GFramework/blob/5b727587f8860761a0c132203cb4d7e0cdae30af/GFramework.Core/Architectures/ArchitectureDisposer.cs)、[容器 Clear/Dispose](https://github.com/GeWuYou/GFramework/blob/5b727587f8860761a0c132203cb4d7e0cdae30af/GFramework.Core/Ioc/MicrosoftDiContainer.cs) | 已观察：DI 资源未释放，旧上下文仍可解析已缓存服务 |
| C4 | Core 依赖 CQRS runtime，Core.Abstractions 继承并暴露其契约；CQRS runtime 又依赖 Core 契约 | [Core 项目](https://github.com/GeWuYou/GFramework/blob/5b727587f8860761a0c132203cb4d7e0cdae30af/GFramework.Core/GFramework.Core.csproj)、[上下文契约](https://github.com/GeWuYou/GFramework/blob/5b727587f8860761a0c132203cb4d7e0cdae30af/GFramework.Core.Abstractions/Architectures/IArchitectureContext.cs)、[工厂](https://github.com/GeWuYou/GFramework/blob/5b727587f8860761a0c132203cb4d7e0cdae30af/GFramework.Cqrs/CqrsRuntimeFactory.cs) | 静态事实；无项目引用环，但按需采用不成立 |
| C5 | 旧命令/查询、新 request/notification/stream、兼容 alias 和同步包装长期并存 | [上下文实现](https://github.com/GeWuYou/GFramework/blob/5b727587f8860761a0c132203cb4d7e0cdae30af/GFramework.Core/Architectures/ArchitectureContext.cs)、[内置模块](https://github.com/GeWuYou/GFramework/blob/5b727587f8860761a0c132203cb4d7e0cdae30af/GFramework.Core/Services/ServiceModuleManager.cs) | 静态事实；推荐异步 request 主路径，历史 API 在独立批次移除 |
| C6 | Architecture 构造写全局日志 provider；基类与生成上下文存在不同的缓存和静态 provider 规则 | [Architecture 构造函数](https://github.com/GeWuYou/GFramework/blob/5b727587f8860761a0c132203cb4d7e0cdae30af/GFramework.Core/Architectures/Architecture.cs)、[日志工厂](https://github.com/GeWuYou/GFramework/blob/5b727587f8860761a0c132203cb4d7e0cdae30af/GFramework.Core.Abstractions/Logging/LoggerFactoryResolver.cs)、[生成器](https://github.com/GeWuYou/GFramework/blob/5b727587f8860761a0c132203cb4d7e0cdae30af/GFramework.Core.SourceGenerators/Rule/ContextAwareGenerator.cs) | 静态事实；日志隔离和生成代码迁移必须一起考虑（评估时的状态。日志标准化批次已移除全局 provider 与 LoggerFactoryResolver，架构实例改用标准 `ILoggerFactory`，`[Log]` 生成代码经实例上下文解析工厂） |
| C7 | 配置允许晚注册，而 Freeze 禁止容器注册；Utility 延迟创建、System/Model 立即创建 | [注册器](https://github.com/GeWuYou/GFramework/blob/5b727587f8860761a0c132203cb4d7e0cdae30af/GFramework.Core/Architectures/ArchitectureComponentRegistry.cs)、[生命周期](https://github.com/GeWuYou/GFramework/blob/5b727587f8860761a0c132203cb4d7e0cdae30af/GFramework.Core/Architectures/ArchitectureLifecycle.cs)、[相关测试](https://github.com/GeWuYou/GFramework/blob/5b727587f8860761a0c132203cb4d7e0cdae30af/GFramework.Core.Tests/Architectures/ArchitectureLifecycleBehaviorTests.cs) | 静态事实；配置不能作为统一的热插拔保证 |

### 基线诊断结果

使用 .NET SDK 10.0.403、net10.0 Release 运行一次组合探针。探针只创建计数对象，不访问外部服务，不修改运行时源码；源码和原始日志保存在忽略目录 `.local/core-assessment/`。

复现步骤：在 Configurator 注册一个无实例参数的 Singleton 依赖及一个 IDisposable 服务；在 OnInitialize 按类型注册依赖它的 System；初始化后比较 System 构造依赖和 Context.GetService 的结果，再尝试启动第二架构，最后销毁首架构并检查释放计数。

| 观察 | 实际结果 |
| --- | --- |
| 构造依赖与就绪后解析的 Singleton 是否同一实例 | false；依赖构造了 2 次 |
| 第二个不同上下文的架构能否启动 | 抛出 InvalidOperationException，提示已有活动上下文 |
| DestroyAsync 后已解析的 IDisposable 服务释放次数 | 0 |
| DestroyAsync 后原 Context 是否仍能解析同一服务 | true |
| 随后显式 Container.Dispose 的释放次数 | 正式 provider 依赖与服务均为 1；注册期创建的依赖仍为 0 |

探针没有证明并发安全、所有 alias 的释放次数、初始化中途失败或仅实现 IAsyncDisposable 的对象均得到正确处理。未进行负载测试、真实 Godot/VS Code 交互或 Native AOT 验证。日志时间为 UTC，评估日期按维护者所在时区记录。

### 历史修复为何不足

先前容器释放工作增加了 Dispose 和独立容器测试，当前[生命周期测试](https://github.com/GeWuYou/GFramework/blob/5b727587f8860761a0c132203cb4d7e0cdae30af/GFramework.Core.Tests/Ioc/IocContainerLifetimeTests.cs)也验证直接释放容器。缺失条件是“通过正常 Architecture 销毁路径抵达同一资源所有者”；阶段通知及全局解绑测试并不覆盖根 provider 的释放。

单活动上下文曾用于消除“任意字典首项”的歧义，当前源码确实实现了这个规则。本轮目标已变为通用实例隔离，因此应撤销其默认全局绑定，而不是在全局表里再发明上下文选择策略。

## 保留、调整、移除与暂缓

| 处置 | 能力 | 理由和收益 | 影响面与成功证据 |
| --- | --- | --- | --- |
| 保留 | Runtime/Abstractions/Generator 分离 | 已有职责边界和包校验有价值 | [边界校验器](../../scripts/validate-runtime-generator-boundaries.py)、生成器与打包测试继续通过 |
| 保留 | 可选 Architecture/Module 编排 | Game 配置首载、宿主接线实际使用其顺序 | 最小应用可不用该层；使用时生命周期顺序可验证 |
| 保留 | Game 配置直接接入、Godot/ECS 适配 | 已有独立 bootstrap 与宿主职责 | 宿主采用路径仍可构建，实机边界单独验证 |
| 保留 | dispatcher 与生成注册表能力 | 有现成实现与覆盖，暂无收益证据支持替换第三方或重写 | [CQRS 测试](https://github.com/GeWuYou/GFramework/blob/5b727587f8860761a0c132203cb4d7e0cdae30af/GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj)与[注册生成测试](https://github.com/GeWuYou/GFramework/blob/5b727587f8860761a0c132203cb4d7e0cdae30af/GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs)保持语义 |
| 调整 | DI 注册与初始化窗口 | 一次构建后激活，统一实例身份 | System/Model/Utility、CQRS、Game/ECS 的构造与生命周期场景通过 |
| 调整 | 停止、销毁与 Dispose 责任 | 业务停止与内存/句柄释放分开，失败也能清理 | 同步/异步/alias/外部实例、失败中途清理均有断言 |
| 调整 | 日志和上下文依赖 | 显式实例依赖消除跨架构污染 | 两实例不同配置，销毁一个不影响另一个 |
| 后续移除 | 注册期解析器、单例缓存、默认全局 fallback | 不能保留两套互相补偿的容器语义 | 删除后用标准 DI 复现同一真实使用场景 |
| 后续移除 | 旧 Command/Query 与兼容别名 | 仅个人使用，无需长期双轨 | 仓库调用、生成快照及采用说明同步迁移 |
| 后续调整 | CQRS 从 Core 中独立、角色分类可选化 | 缩小采用耦合，避免所有应用接受游戏角色 | Core-only 和 CQRS-only 独立消费样例分别还原成功 |
| 暂缓 | 为每个工具子系统单独建包 | 拆包增加版本和接线成本，尚无实际采用证据 | 遇到真实依赖成本后再决定 |
| 暂缓 | 全量 schema、AOT、性能优化、完整宿主家族 | 当前需求和证据不足 | 分别需要真实消费者、兼容实验或测量依据 |

## 下游与测试缺口

- Game：首载同步 hook、热重载句柄和迁移 runner 必须保留语义；不能把安装描述阶段变成文件加载阶段。
- Godot：锚点、主线程限制和异步销毁需要宿主验证。纯托管测试不能证明节点退树、queue_free 或原生 InputMap 正确。
- ECS：World 在注册期创建，而 DestroyAsync 在未初始化时提前返回；若启动在两者之间失败，存在 World 清理缺口的静态风险，尚未通过原生资源诊断验证。
- Generators：ContextAware 的静态 provider、自动模块注册、CQRS registry/invoker 均是迁移使用方；生成器版本和运行时约定需要共同核对。
- 生命周期失败、并发初始化、取消与清理异常：当前阶段状态机和 ready task 不等于完整操作互斥与失败回滚，需要[首批设计](first-core-batch.md)列出的行为测试。

本轮结论支持有边界的核心治理，不支持宣称框架已经适合所有 C# 应用，也不支持无证据的性能优劣判断。
