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
| RegisterPlurality(instance) | 以借用实例暴露具体类型及全部接口；调用者仍拥有释放责任 |
| ExecuteServicesHook(configurator) | 在冻结前配置标准 IServiceCollection |
| Freeze() | 构建一次根 ServiceProvider，验证构造依赖及 scoped 捕获，禁止后续注册 |
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

该片段需要在配置阶段注册 IOperation 及其实现。作用域中的标准 IDisposable 服务由 scope 释放；若服务仅支持 IAsyncDisposable，应使用标准异步 scope，并等待 DisposeAsync。根容器实现 IDisposable 和 IAsyncDisposable；包含 async-only 资源时必须等待 DisposeAsync，不能使用同步 Dispose。

## 资源所有权

- 由正式 DI provider 通过类型或工厂创建的对象，由其 scope 或根 provider 释放。
- 直接传入的实例由创建者拥有。RegisterPlurality(instance) 的具体类型和全部接口都使用实例描述符，解析别名不会转移释放责任。
- RegisterPlurality&lt;T&gt;() 只接受不实现 IDisposable/IAsyncDisposable 的类型；这类普通单例的多个接口仍共享同一实例。
- 可释放类型应使用一个规范拥有绑定，例如 RegisterSingleton&lt;IConnection, Connection&gt;()。标准 DI 会跟踪工厂返回的 IDisposable，多个 owning factory aliases 可能重复捕获同一对象，因此框架拒绝隐式生成这种资源别名。
- DisposeAsync 等待标准 provider 释放其同步和异步资源；并发释放调用等待同一任务，重复调用不重复清理。释放后注册、解析、查询和创建 scope 均拒绝继续访问。
- Clear 只用于尚未冻结的描述集合，不能代替 Dispose 或 DisposeAsync。

Architecture 的类型注册会对可释放组件使用单一具体类型拥有键，生命周期参与关系单独登记；这类组件不会自动暴露所有接口别名。若组件需要被其他服务按接口依赖，应在组合根明确选择一个资源拥有服务键，避免另加返回同一资源的 owning factory。

## 在 Architecture 中使用

在 ConfigureServices(IServiceCollection services) 中登记组件、模块和标准 DI 服务；Configurator 也只能描述服务。配置结束后 Architecture 会调用一次 Freeze，再统一激活组件、注入上下文、执行创建回调并初始化。调用方不应再次构建 provider。

System、Model、Utility 的构造依赖和 Ready 后解析使用同一正式容器，Singleton 身份保持一致。GetService 不额外缓存普通服务：Transient 每次解析、Scoped 在明确 scope 内复用。根上下文不能解析 Scoped 服务，Contains 也不是无副作用的激活能力检查；只查询注册应使用 HasRegistration。

配置阶段不能通过 Context 获取服务，也不能在 onCreated、初始化或 OnStarted 回调里追加注册。原 AllowLateRegistration 配置已经移除。

等待 DestroyAsync 或 DisposeAsync 会先停止业务组件及模块，再异步释放当前 provider。停止开始后外部旧 Context 拒绝新操作；框架的清理调用链仍可访问停止所需依赖。业务 Destroy 不应重复释放 DI 拥有的依赖；传入的外部实例仍由调用者最终 Dispose/DisposeAsync。

## 相关主题

- [架构与生命周期](./architecture.md)
- [上下文访问](./context.md)
- [模型](./model.md)
- [系统](./system.md)
- [工具](./utility.md)
