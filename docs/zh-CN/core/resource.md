---
title: 资源租约
description: 使用类型化缓存、独立租约和共享异步加载管理资源所有权。
---

# 资源租约

`ResourceManager` 以“请求类型 + 路径”为缓存标识。`IResourceLoader<T>` 创建资源并负责匹配的 `Unload`；管理器不额外调用资源 Dispose，也不接管外部加载器的释放责任。

## 获取与持有

安装 `GeWuYou.GFramework.Core`，登记加载器后通过一次操作加载并取得租约：

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Resource;
using GFramework.Core.Resource;

static async Task UseResourceAsync<T>(IResourceLoader<T> loader, string path, CancellationToken token)
    where T : class
{
    await using var resources = new ResourceManager();
    resources.RegisterLoader(loader);
    using var first = await resources.AcquireAsync<T>(path, token);
    using var second = first.Retain();
    Console.WriteLine(ReferenceEquals(first.Resource, second.Resource));
}
```

每个 `IResourceHandle<T>` 都拥有一个独立引用。`Retain()` 创建另一个租约；同一租约重复 Dispose 不减少其他租约的引用。已释放租约的 Resource 与 Retain 抛出 `ObjectDisposedException`。跨任务传递资源时，同时明确租约的持有者，不能只保留裸资源引用。

## 共享加载与取消

同类型同路径的并发异步请求只执行一次底层加载，各自获得租约。调用者令牌只取消自己的等待，不中止其他等待者或共享加载。加载异常原样传播，失败条目移出缓存，后续请求可以重试。

同步 Acquire 遇到同标识的共享加载尚未完成时明确要求使用 AcquireAsync，不阻塞宿主上下文。不同类型可以使用相同路径；加载器跨标识共享原生对象时，应为每次成功加载维持匹配的底层所有权。

## 缓存与释放

- 默认 `ManualReleaseStrategy` 在最后一个租约释放后保留缓存，通过 `Unload<T>(path)` 或 `UnloadAll()` 清理。
- `AutoReleaseStrategy` 在最后一个租约结束且没有待完成获取时释放资源。
- `PreloadAsync<T>` 成功后固定缓存，直到显式卸载或管理器关闭，不受自动策略驱逐。
- 有租约或待完成获取时，显式卸载不会移除资源。批量卸载尝试每个可释放条目并聚合失败。
- 替换或注销加载器只影响后续加载；已加载资源始终交回创建它的原始加载器。

## 关闭与宿主线程

优先 `await DisposeAsync()`：关闭拒绝新工作，等待底层加载以及已开始的资源卸载收尾，释放无租约资源。显式卸载、批量卸载或自动释放即使已移除缓存项，其正在执行的加载器回调仍会被关闭流程等待；这些回调在关闭期间失败时，关闭结果会报告聚合异常。仍有租约的资源继续有效，最后一个租约释放后再释放资源；关闭完成不代表消费者已归还所有租约。关闭后租约不能再 Retain。加载器及其释放能力必须存活到最后一个租约归还；如果加载器由 DI provider 拥有，先归还租约，再释放该 provider。

有加载或资源卸载未完成时，同步 Dispose 明确拒绝，请使用 DisposeAsync。场景退出先停止新操作、结束资源使用并归还租约，再关闭管理器。管理器只保护缓存与引用计数，不使资源本身线程安全；加载器负责引擎线程要求，回调不自动切换到 Godot 主线程。

参见[资源管理实践](../tutorials/resource-management.md)。
