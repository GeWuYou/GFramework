---
title: IoC 容器
description: 说明 MicrosoftDiContainer 的注册、冻结、作用域与资源释放边界。
---

# IoC 容器

`MicrosoftDiContainer` 是 Microsoft DI 的框架适配层，实现 `IIocContainer`，额外提供组件别名、优先级查询和 CQRS 注册入口。它不是可随时修改的动态容器；预冻结查询与正式 provider 解析也有不同语义。

## 注册与冻结

直接使用容器时，从组合根创建并拥有它：

```csharp
using System;
using GFramework.Core.Ioc;

using var container = new MicrosoftDiContainer();
container.RegisterSingleton<IClock, SystemClock>();
container.Freeze();

var clock = container.GetRequired<IClock>();

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
```

当前主要入口：

| 方法 | 行为 |
| --- | --- |
| RegisterSingleton&lt;T&gt;(instance) | 绑定外部实例；该重载会检查同一服务类型是否已注册 |
| RegisterSingleton&lt;TService, TImpl&gt;() | 使用标准 DI 的 Singleton 描述；与实例重载的重复注册校验不同 |
| RegisterScoped&lt;TService, TImpl&gt;() | 在标准 scope 内复用实例 |
| RegisterTransient&lt;TService, TImpl&gt;() | 每次解析创建 |
| RegisterFactory&lt;TService&gt;(factory) | 当前注册的是 Singleton 工厂，不是 Transient 工厂 |
| RegisterPlurality(instance) | 暴露具体类型及实现接口；生命周期和资源归属需要单独确认 |
| ExecuteServicesHook(configurator) | 在冻结前配置标准 IServiceCollection |
| Freeze() | 构建根 ServiceProvider 并禁止后续注册 |
| Get&lt;T&gt;() / GetRequired&lt;T&gt;() | 获取可选/必需服务；预冻结时主要读取已注册实例 |
| CreateScope() | 从已冻结 provider 创建 Microsoft DI scope |

类型或工厂注册在冻结前不保证可以由 Get 完整激活。不要把 Contains 的注册存在检查当作对象已创建或初始化成功。

GetServicesUnsafe 暴露内部 IServiceCollection，调用者不应保留引用并在 Freeze 后修改它；这会绕过注册检查，也不会重建正式 provider。

## 作用域

在同一个 scope 内解析 Scoped 服务，离开 scope 时释放该 scope 创建并拥有的资源。不要从根 provider 解析本应属于请求或操作 scope 的服务，也不要让 Singleton 捕获 Scoped 依赖。

```csharp
using Microsoft.Extensions.DependencyInjection;

using var scope = container.CreateScope();
var operation = scope.ServiceProvider.GetRequiredService<IOperation>();
```

该片段需要在配置阶段注册 IOperation 及其实现。作用域中的标准 IDisposable 服务由 scope 释放；若服务仅支持 IAsyncDisposable，应使用标准异步 scope，并等待 DisposeAsync。根容器当前只实现同步 IDisposable，含 async-only 资源的根容器不能假定同步释放可用。

## 资源所有权

- 由正式 DI provider 创建的对象，按标准 DI 的生命周期由其 scope 或根 provider 负责释放。
- 直接传入的实例由创建者负责；RegisterPlurality 的工厂别名会影响 provider 的跟踪，不要仅凭实例注册重载推断所有 alias 都不参与释放。资源敏感对象优先用明确的服务绑定。
- 容器的 Dispose 会释放其拥有的根 provider；重复 Dispose 可调用。释放后注册、解析、查询和创建 scope 会抛出 ObjectDisposedException。
- Clear 在冻结后会直接返回，不能作为 Dispose 的替代。

当前 Architecture.DestroyAsync 会执行组件停止和服务模块销毁，但随后调用的是 Clear，未释放冻结 provider。若组合根显式传入并拥有 ArchitectureServices，应在组件停止后释放其 Container。仅持有 Architecture 时，没有公开的独立容器释放入口。注册期创建的构造依赖还可能不属于正式 provider，不能把显式 Dispose 当成所有生命周期缺口的通用补救。

## 在 Architecture 中使用

当前在 OnInitialize 注册组件；额外 DI 服务可通过 Configurator 配置。Architecture 自己在组件初始化之后冻结容器，调用方无需再次 Freeze。

按类型注册 System/Model 使用注册期激活路径，之后才构建正式 provider。因此它们收到的 Singleton 构造依赖可能与 Ready 后 GetService 得到的实例不同。需要稳定身份的现有调用方应显式保留并传入同一实例，同时承担相应资源责任。

初始化完成后的注册会被冻结容器拒绝，即使架构配置打开 AllowLateRegistration，也不能将其理解为完整的运行中注册支持。

## 相关主题

- [架构与生命周期](./architecture.md)
- [上下文访问](./context.md)
- [模型](./model.md)
- [系统](./system.md)
- [工具](./utility.md)
