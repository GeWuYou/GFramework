# 当前架构

源码、项目文件和行为测试定义当前实现。本页说明稳定边界；框架采用说明见[Core 指南](../../docs/zh-CN/core/index.md)。

## 依赖方向

Core.Abstractions 定义能力契约，Core 提供实现。Core 不引用 CQRS、Game、Godot 或生成器。CQRS runtime 独立使用标准 DI；Game 提供引擎无关的配置、内容、Mod、存档、UI 与场景服务；Godot 和 ECS 提供各自宿主适配。

编译期能力仅进入消费项目，运行时、契约和聚合包不引用生成器。Core 生成器保留枚举与优先级，CQRS、Game 和 Godot 生成器负责各自输入。

## 组合根与生命周期

普通业务对象使用构造注入，Model/System/Utility 是职责约定。容器仅在组合根和宿主接线访问。标准 IServiceCollection 描述依赖；Architecture 构建并拥有唯一 provider，通过受阶段检查的 Services 暴露标准解析接口。

普通服务不自动启动。AddComponent 登记服务键与生命周期元数据，按 order 升序、同值登记顺序启动，逆实际开始顺序停止；失败的初始化尝试也参与清理。组件键在最终配置完成后检查唯一的非 keyed Singleton 绑定，再构建 provider；后续模块不能悄然覆盖该约束。配置型模块只组织 IServiceCollection，不拥有第二套容器或进程级注册表。

启动失败、取消和正常停止均闭合业务停止、宿主分离与 provider 释放。停止调用共享同一结果；单个清理失败不跳过其余清理。外部实例由调用者拥有，DI 创建的资源由 provider 拥有。避免工厂别名将同一资源重复登记为拥有对象。

日志使用实例级标准 ILoggerFactory/ILogger&lt;T&gt;。业务不通过全局上下文、角色基类或通用服务定位器取依赖。

## 运行能力

- EventBus 每个类型一个同步通道，优先级与普通订阅统一快照；订阅返回独立 IDisposable。
- StateMachine 独立于容器和事件系统，串行切换、回退与终态清理，失败后只允许停止。
- ResourceManager 合并同键加载并返回独立租约；调用者取消不取消其他等待者，关闭等待加载与已开始的释放回调，关闭后已有租约有效至归还。
- Option/Result 是可选的显式返回工具；取消按异步取消语义传播。
- 协程、Store、属性、暂停和其他能力可独立采用，其所有权与线程契约由各接口说明。

## 宿主与线程

Godot 保留其场景树和主循环，拥有者负责装配节点依赖。原生访问通过宿主 dispatcher 进入拥有线程。DI 解析安全不等于服务或引擎安全；标准 provider 的异步释放也不保证延续在调用线程。持有原生对象的适配器在自身清理边界调度。

资源加载器及其释放能力必须存活到最后租约归还。管理器关闭完成与全部外部租约归还是不同条件；应用停止应先收回任务、订阅和租约，再释放所属基础设施。

## 示例与维护

MinimalApp 展示普通 DI 和可选编排。GodotIntegration 展示宿主接线。DodgeTheCreeps 提供完整直接采用路径；DodgeTheCreeps.Advanced 用相同功能展示纯 C# 规则/用例与宿主适配，拥有独立数据目录。

新增功能优先放在所属功能内，以实际依赖决定接口与程序集边界。行为测试验证规则、生命周期和错误语义；原生测试验证场景树、输入、资源、导出与跨进程数据。构建、托管测试和原生运行分别提供各自范围的证据。

历史评估与阶段执行报告通过 Git 历史查阅，不作为当前架构或待办依据。

## 可选消息与原生 ECS 边界

Godot 不引用 CQRS，消息分发由消费工程显式选择。CQRS 管道登记使用标准 ServiceLifetime，默认 Transient，开放泛型映射在登记时校验；Scoped runtime 和行为共享调用方作用域，流的作用域覆盖完整枚举。

ArchEcsModule 只配置服务，ArchRuntime 管理借用 World／系统的业务生命周期。宿主驱动原生 ISystem 的三阶段更新；稳定优先级排序，失败后封闭更新，逆序停止已尝试启动的 IDestroyable 系统。provider 或直接创建者分别负责最终释放。Group 是一个顶层拥有对象，不重复登记内部子系统。

ECS 的统计策略在构造时固定，默认关闭。启用后复用帧测量存储，读取时生成不可变快照；未执行阶段与失败阶段明确区分。runtime 限定宿主拥有线程，快照可跨线程分享。采用和关闭的可执行路径见[Arch ECS 接入](../../docs/zh-CN/ecs/arch.md)。

Godot 等具有线程约束的宿主应显式提供 `AddArch(..., hostContext: SynchronizationContext.Current)`，并用异步 Architecture 启动／停止。该路径把 World、系统和 runtime 的激活、初始化及业务停止派发回拥有上下文，避免前置异步组件导致线程切换；普通同步宿主省略此参数。最终资源释放仍由对象拥有者负责，持有原生对象的系统应在自己的释放边界进行宿主调度。
