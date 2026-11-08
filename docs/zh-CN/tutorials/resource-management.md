---
title: 资源管理实践
description: 使用独立租约管理预加载、取消与场景退出。
---

# 资源管理实践

先确定资源类型与路径约定，实现 `IResourceLoader<T>` 的 Load、LoadAsync、CanLoad 和 Unload。加载器负责完整释放，包括原生引用与 IDisposable；管理器不会重复 Dispose。

## 场景取得租约

```csharp
using System.Threading;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Resource;

static async Task<IResourceHandle<T>> LoadForSceneAsync<T>(
    IResourceManager resources, string path, CancellationToken lifetime) where T : class
{
    return await resources.AcquireAsync<T>(path, lifetime);
}
```

场景保存返回的租约并在退出时释放。子流程需要独立持有时调用 Retain，由该流程负责 Dispose。不要只向异步工作传递 Resource 而丢失对应所有权。

## 预加载与策略

`await resources.PreloadAsync<T>(path, token)` 固定缓存，后续 Acquire 不重复加载。预加载固定的条目由显式 Unload 或管理器关闭释放。

AutoReleaseStrategy 适合最后一次使用后立即释放的临时资源；默认 ManualReleaseStrategy 适合跨场景保留缓存，在明确加载边界 UnloadAll。存在租约的条目保持有效。

## 取消与关闭

同标识异步获取共享一次加载，取消只结束本次等待。底层失败保留异常，后续获取可重试。同步 Acquire 不阻塞已进行的异步加载，而是要求使用 AcquireAsync。

退出先停止新请求，结束资源使用，归还租约，再 await DisposeAsync。管理器等待已开始的加载收尾；关闭后的获取与 Retain 失败。仍未归还的已有租约保持资源有效，直到该租约释放。原始加载器及释放能力必须一直存活；因此先归还租约，再释放拥有加载器的 DI provider。

加载器负责宿主线程，不在后台直接修改活动场景树。纯托管单元测试不能证明原生资源已按预期释放。

## 验证场景

使用可控 TaskCompletionSource 验证共享加载、多份租约、取消隔离、全体取消后的回收、加载失败重试、关闭与加载竞争、原始加载器释放一次及租约重复 Dispose。完整接口与线程约束见[资源租约](../core/resource.md)。
