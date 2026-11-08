---
title: 装配与生命周期
---

# 装配与生命周期

Architecture 在一个实例中持有并释放唯一的 Microsoft DI provider。服务通过构造函数取得依赖。

## 注册和启动

覆盖 ConfigureServices(IServiceCollection)，只登记描述。普通服务使用 AddSingleton / AddScoped / AddTransient；需要启动与停止的对象使用 AddComponent。后者同时登记一个服务键和生命周期参与关系，解析与启动使用同一实例。

```csharp
using GFramework.Core.Architectures;
using GFramework.Core.Abstractions.Lifecycle;
using Microsoft.Extensions.DependencyInjection;

public sealed class Application : Architecture
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddComponent<CacheWarmup>(order: -100);
    }
}

public sealed class CacheWarmup : IInitializable, IDestroyable
{
    public void Initialize() { /* Prepare owned business work. */ }
    public void Destroy() { /* Stop work; DI disposes owned resources. */ }
}
```

AddComponent&lt;TService, TImplementation&gt; 只使用一个拥有键。外部实例使用实例重载；架构负责其已登记业务停止，调用者仍拥有 Dispose 责任。不要用多个工厂别名重复登记同一个可释放对象。所有模块配置完成后，架构会在构建 provider 前检查每个组件键必须且只能有一个非 keyed 的 Singleton 描述；后续模块追加同键服务、移除绑定或改成 Scoped / Transient 都会导致启动失败，避免消费者获得未启动的实例。

## 生命周期

1. 配置依赖，构建并验证一次 provider。
2. 按 order 升序启动组件，同 order 按登记顺序；异步初始化能力优先。
3. 完成 OnStarted / OnStartedAsync 宿主接线后进入 Ready。
4. 停止接入，逆序停止已尝试启动的组件，分离宿主，再释放 provider。

InitializeAsync 接收取消令牌；失败或取消先完成清理再向调用者报告。原实例不可重试，请创建新实例。DestroyAsync 与 DisposeAsync 共享一次停止结果。组件停止失败仍继续后续清理，最终报告聚合异常。

同步 Initialize 只接受真正同步的组件及宿主；异步能力使用 InitializeAsync。普通业务服务不需要实现生命周期接口。

## 模块与宿主

IArchitectureModule.ConfigureServices(IServiceCollection) 只组织注册。组合根通过 services.AddModule 安装模块，运行期工作登记为组件。没有进程级自动模块注册。

Services 是受生命周期检查的 IServiceProvider，供组合根与宿主接线使用；配置期间和停止后拒绝解析。业务类应直接取得所需依赖。Godot 保留自己的主循环，涉及节点的开始和停止由 Godot 适配调度。

[最小示例](../getting-started/quick-start.md) 展示独立 DI 与可选 Architecture 两种采用方式。

order 只控制生命周期回调，不保证构造激活顺序；构造函数不应读取其他组件的“已初始化”状态。停止接入后外部 Services 解析被拒绝，正在进行的清理调用链在 provider 释放前仍可解析必要服务。
