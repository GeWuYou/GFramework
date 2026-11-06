# 首批核心改造设计

本页是下一轮实施规格，尚未修改公共 API 或运行时。任务状态与执行证据只维护在[核心路线图](../roadmap/core-governance.md)。

## 目标与边界

消除[评估 C1/C2/C3/C6/C7](core-assessment.md)的默认全局耦合、两次单例构造与释放遗漏。第一批不拆包、不移除旧命令体系，不重写全部日志输出或游戏业务模块；必要的直接使用方随接口调整。

## 注册、构建与初始化分离

拟调整入口如下；这些签名不是当前可用 API：

| 入口 | 下一批语义 |
| --- | --- |
| Architecture.ConfigureServices(IServiceCollection services) | 替代 OnInitialize 中的注册职责；只记录描述和模块元数据，不解析服务、不执行加载 |
| RegisterSystem/Model/Utility 类型及实例入口 | 只在配置阶段合法；声明 DI 注册和显式生命周期参与者，不立刻创建对象 |
| Architecture.InitializeAsync(CancellationToken) | 配置完成后构建一次 provider，再解析和初始化参与者；完成后进入 Ready |
| Architecture.DestroyAsync() / IAsyncDisposable.DisposeAsync() | 同一终态清理路径；幂等，销毁完成后上下文拒绝服务解析 |
| IAsyncArchitectureLifecycleHook.OnPhaseAsync(phase, architecture, token) | 支持配置首载等真实异步阶段；优先执行异步 hook，不用 GetResult 桥接异步加载 |

现有 onCreated 回调调整为正式 provider 激活实例之后、组件初始化之前执行一次，禁止在回调中继续登记服务。

删除 ArchitectureComponentActivator、RegistrationServiceProvider 及其单例缓存。System/Model/Utility 的依赖均由同一正式 provider 解析。RegisterPlurality 别名复用同一实例，不隐式把所有实现接口变成需要初始化/释放的另一份组件。

构建使用标准 ServiceCollection；验证构造依赖和 scoped 捕获。第一批继续保留 Utility → Model → System 的初始化顺序，阶段通知可依赖已建 provider。删除 AllowLateRegistration 的公开开关和执行分支；Ready 后所有注册明确失败，不用不可用配置承诺热插拔。

同步 Initialize 只支持真实同步参与者；遇到只能异步完成的初始化明确报错并指向异步入口，不启动 Task.Run 掩盖线程要求。异步路径不得靠同步包装完成配置首载。

## 实例与资源责任

第一批 Architecture 使用自己持有的 ServiceCollection 构建并拥有根 provider；复用外部应用 provider 与 Generic Host 集成放到后续独立任务，避免一个批次同时引入两种根容器所有权。

| 对象 | 生命周期责任 |
| --- | --- |
| DI 通过类型或工厂创建的对象 | 同一 provider 拥有并释放；Architecture 先调用已启动组件的业务停止，再异步释放 provider |
| 调用者传入的实例 | 调用者拥有 IDisposable/IAsyncDisposable 释放责任；若显式登记为生命周期组件，Architecture 仍负责对应业务停止 |
| 同时实现自有 Destroy 与 IDisposable 的对象 | Destroy 只停止业务，不再次释放其 DI 拥有的依赖；资源释放由其所有者执行 |
| 模块在初始化前创建的资源 | 移到初始化或可释放的 DI 工厂；不能因未进入 Initialize 就跳过已创建资源的清理 |
| 多接口 alias | 同一资源只释放一次；不得让多个 owning factory 导致重复释放 |

销毁流程为：阻止新初始化/解析 → 逆序停止已经成功启动的参与者 → 释放本实例 provider → 进入终态。单个停止失败继续后续清理，最后报告聚合错误；不会吞掉错误并宣称成功。停止过程内部仍可访问清理所需服务，对外旧 Context 不能继续解析。

初始化失败或取消后清理部分启动组件及已创建资源，不进入 Ready，不允许原对象重试；调用方创建新架构。WaitUntilReadyAsync 必须以失败或取消完成。重复 Initialize 明确拒绝；并发 Destroy 等待同一清理任务，不提前返回“已完成”。

## 上下文、日志与宿主

删除核心启动/销毁中的 GameContext.Bind/Unbind，以及 ContextAwareBase 默认全局回退；未显式绑定的 ContextAware 访问给出清晰错误。GameContext 的兼容公共类型留到历史 API 清理批次，但核心不依赖它运行。

日志从本架构配置或构造参数接入实例级 ILoggerFactoryProvider；缺省实例工厂不写 LoggerFactoryResolver.Provider。这是第一批的隔离接口，下一日志批次再改用 Microsoft.Extensions.Logging 契约；本批不引入永久双向日志适配层。

ContextAwareGenerator 同步取消静态共享 provider 和默认 GameContextProvider。生成对象通过实例级显式上下文注入工作，未注入时失败；更新 snapshot、诊断说明和采用示例，不仅修改运行时基类。

- GameConfigModule：安装阶段声明配置服务；异步 BeforeUtilityInit 首载，失败后停止 watcher。保留直接 GameConfigBootstrap 路径。
- Godot AbstractArchitecture/Anchor：使用同一 provider 和实例上下文，由引擎驱动；涉及节点的动作回到 Godot 主线程。不额外运行 Generic Host。
- ArchEcsModule：配置阶段只描述 World 创建和系统；初始化后 Update，任何部分失败均由明确所有者释放 World。
- CQRS：本批保留原包依赖与调用签名；内部上下文注入指向本实例，不增加新的全局路径。

## 验收矩阵

| 场景 | 必须观察到 |
| --- | --- |
| 同一类型/工厂 Singleton 被组件构造与 Context 读取 | 相同引用、构造一次 |
| 普通 Utility、ContextUtility、Model、System | 使用同一 provider；各自初始化一次、顺序稳定 |
| Scoped/Transient 与 alias | scope 之间隔离，scope 内一致；无根级 scoped 捕获；资源恰好释放一次 |
| 成功销毁和重复销毁 | 同步及 async-only disposable 均释放一次，旧 Context 无法解析 |
| 外部实例 | 执行已登记业务停止，框架不释放其外部拥有资源 |
| 配置、构造、组件初始化、阶段 hook 各阶段失败/取消 | 不进入 Ready，已创建/已启动资源均清理，原异常可追溯 |
| 两架构同类型/不同类型并存 | 上下文、日志和服务互不污染，销毁一个不影响另一个 |
| 生成 ContextAware 与手写基类 | 均无全局 fallback；未注入时明确失败 |
| Game 配置、Godot 锚点、ECS World | 初始数据可用、停止释放闭合；原生宿主场景单独验收 |

实现时优先把本轮最小探针转成仓库行为回归，再覆盖失败、取消和别名场景。运行相关 Core/CQRS/Game/Generator 测试及受影响项目 Release 构建；Godot/ECS 真实宿主证据缺失必须单列，不能由编译通过替代。
