# GFramework.Core

Core 是引擎无关的 C# 基础能力库。普通服务采用标准 Microsoft DI 和 ILogger&lt;T&gt;；Architecture 按需提供组件启动、停止、失败清理及宿主接线。

## 包与能力

Core 依赖 Core.Abstractions 及标准 DI/日志包，不传递依赖 CQRS、Godot 或源码生成器。契约可独立引用。

| 目录 | 能力 |
| --- | --- |
| Architectures | 唯一 provider、显式组件与配置型模块、实例生命周期 |
| Events / Property | 同步通知、标准订阅令牌与可绑定属性 |
| State / StateManagement | 独立状态机与单向状态 Store |
| Coroutine / Time / Pause / Concurrency | 协程、时钟、暂停与异步键锁 |
| Resource / Pool | 资源租约、缓存与对象池 |
| Logging / Localization | 标准日志输出适配与本地化 |
| Functional / Utility / Extensions | Option、Result、数值显示与通用辅助 |

## 最小采用

安装 GeWuYou.GFramework.Core 后，可直接构造独立能力或通过 IServiceCollection 注册。普通对象不要求框架角色标记。需要生命周期时使用 Architecture.ConfigureServices、services.AddComponent 和 InitializeAsync；完成后等待 DisposeAsync。

业务状态由领域对象维护，用例由应用服务组织，构造函数显式声明依赖；容器解析集中在组合根。模块只配置 IServiceCollection，运行工作登记为组件。外部实例由调用者拥有，DI 创建的资源由 provider 释放。

## 文档与示例

- [Core 使用指南](../docs/zh-CN/core/index.md)
- [装配与生命周期](../docs/zh-CN/core/architecture.md)
- [依赖注入](../docs/zh-CN/core/ioc.md)
- [最小 C# 应用](../Samples/MinimalApp/README.md)
- [Godot 接入](../Samples/GodotIntegration/README.md)
- [完整游戏](../Samples/DodgeTheCreeps/README.md)
- [进阶游戏](../Samples/DodgeTheCreeps.Advanced/README.md)
