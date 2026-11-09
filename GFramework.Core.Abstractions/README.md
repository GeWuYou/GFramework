# GFramework.Core.Abstractions

此包定义 Core 能力的接口、枚举和值对象，不提供运行时实现。依赖标准 DI 与日志契约，不依赖 CQRS、Godot 或生成器。直接使用框架功能时安装 Core 即可传递取得本包。

## 契约地图

- Architectures / Lifecycle：IArchitecture、IArchitectureModule、显式启动与停止能力。
- Events / Property / State / StateManagement：事件、属性、状态机、Store / reducer / middleware。
- Coroutine / Time / Pause / Concurrency：协程阶段、失败策略、时钟、暂停、异步锁。
- Resource / Pool / Logging / Localization：资源租约、加载器、池、日志输出、本地化。
- Data / Serializer / Storage / Versioning：数据装载、序列化、存储、版本。
- Utility / Bases / Registries / Enums / Properties：数值格式、优先级、注册表及架构参数。

IObjectPoolSystem 的对象类型限定为引用类型；归还必须属于同一池及原始键且尚未归还。实现应在拒绝归还时保留对象、回调与池统计不变，见[对象池契约](../docs/zh-CN/core/pool.md)。

## 采用

适配器或独立契约层可以只引用本包，为实际需要的能力提供实现。普通领域类不需要标记接口；应用服务通过构造函数接收所需能力。

生命周期与资源所有权分开：Architecture 停止显式登记的业务组件，DI 释放它创建的资源，外部实例由调用者释放。事件与属性订阅返回标准 IDisposable。

异步组合根通过 `IAsyncArchitectureHost.StartHostAsync` 声明必要宿主接线；该能力只面向宿主，不要求普通业务对象实现。Services 的停止检查仅约束根访问入口，已有标准 DI Scope 仍由调用方管理。

## 文档

- [抽象层指南](../docs/zh-CN/abstractions/core-abstractions.md)
- [Core 能力](../docs/zh-CN/core/index.md)
- [实现包](../GFramework.Core/README.md)
