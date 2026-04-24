---
title: 资源管理系统
description: 资源管理系统提供了统一的资源加载、缓存和卸载机制，支持引用计数和多种释放策略。
---

# 资源管理系统

## 概述

资源管理系统是 GFramework 中用于管理游戏资源（如纹理、音频、模型等）的核心组件。它提供了统一的资源加载接口，自动缓存机制，以及灵活的资源释放策略，帮助你高效管理游戏资源的生命周期。

通过资源管理器，你可以避免重复加载相同资源，使用引用计数自动管理资源生命周期，并根据需求选择合适的释放策略。

**主要特性**：

- 统一的资源加载接口（同步/异步）
- 自动资源缓存和去重
- 引用计数管理
- 可插拔的资源加载器
- 灵活的释放策略（手动/自动）
- 线程安全操作

## 核心概念

### 资源管理器

`ResourceManager` 是资源管理的核心类，负责加载、缓存和卸载资源：

```csharp
using GFramework.Core.Abstractions.Resource;

// 获取资源管理器（通常通过架构获取）
var resourceManager = this.GetUtility<IResourceManager>();

// 加载资源
var texture = resourceManager.Load<Texture>("textures/player.png");
```

### 资源句柄

`IResourceHandle<T>` 用于管理资源的引用计数，确保资源在使用期间不被释放：

```csharp
// 获取资源句柄（自动增加引用计数）
using var handle = resourceManager.GetHandle<Texture>("textures/player.png");

// 使用资源
var texture = handle.Resource;

// 离开作用域时自动减少引用计数
```

### 资源加载器

`IResourceLoader<T>` 定义了如何加载特定类型的资源：

```csharp
public interface IResourceLoader<T> where T : class
{
    T Load(string path);
    Task<T> LoadAsync(string path);
    void Unload(T resource);
}
```

### 释放策略

`IResourceReleaseStrategy` 决定何时释放资源：

- **手动释放**（`ManualReleaseStrategy`）：引用计数为 0 时不自动释放，需要手动调用 `Unload`
- **自动释放**（`AutoReleaseStrategy`）：引用计数为 0 时自动释放资源

## 基本用法

### 注册资源加载器

首先需要为每种资源类型注册加载器：

```csharp
using GFramework.Core.Abstractions.Resource;

// 实现纹理加载器
public class TextureLoader : IResourceLoader<Texture>
{
    public Texture Load(string path)
    {
        // 同步加载纹理
        return LoadTextureFromFile(path);
    }

    public async Task<Texture> LoadAsync(string path)
    {
        // 异步加载纹理
        return await LoadTextureFromFileAsync(path);
    }

    public void Unload(Texture resource)
    {
        // 释放纹理资源
        resource?.Dispose();
    }
}

// 在架构中注册加载器
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        var resourceManager = new ResourceManager();
        resourceManager.RegisterLoader(new TextureLoader());
        RegisterUtility<IResourceManager>(resourceManager);
    }
}
```

### 同步加载资源

```csharp
// 加载资源
var texture = resourceManager.Load<Texture>("textures/player.png");

if (texture != null)
{
    // 使用纹理
    sprite.Texture = texture;
}
```

### 异步加载资源

```csharp
// 异步加载资源
var texture = await resourceManager.LoadAsync<Texture>("textures/player.png");

if (texture != null)
{
    sprite.Texture = texture;
}
```

### 使用资源句柄

```csharp
public class PlayerController
{
    private IResourceHandle<Texture>? _textureHandle;

    public void LoadTexture()
    {
        var resourceManager = this.GetUtility<IResourceManager>();

        // 获取句柄（增加引用计数）
        _textureHandle = resourceManager.GetHandle<Texture>("textures/player.png");

        if (_textureHandle?.Resource != null)
        {
            sprite.Texture = _textureHandle.Resource;
        }
    }

    public void UnloadTexture()
    {
        // 释放句柄（减少引用计数）
        _textureHandle?.Dispose();
        _textureHandle = null;
    }
}
```

## 高级用法

### 预加载资源

在游戏启动或场景切换时预加载资源：

```csharp
public async Task PreloadGameAssets()
{
    var resourceManager = this.GetUtility<IResourceManager>();

    // 预加载多个资源
    await Task.WhenAll(
        resourceManager.PreloadAsync<Texture>("textures/player.png"),
        resourceManager.PreloadAsync<Texture>("textures/enemy.png"),
        resourceManager.PreloadAsync<AudioClip>("audio/bgm.mp3")
    );

    Console.WriteLine("资源预加载完成");
}
```

### 使用自动释放策略

```csharp
using GFramework.Core.Resource;

// 设置自动释放策略
var resourceManager = this.GetUtility<IResourceManager>();
resourceManager.SetReleaseStrategy(new AutoReleaseStrategy());

// 使用资源句柄
using (var handle = resourceManager.GetHandle<Texture>("textures/temp.png"))
{
    // 使用资源
    var texture = handle.Resource;
}
// 离开作用域后，引用计数为 0，资源自动释放
```

### 批量卸载资源

```csharp
// 卸载特定资源
resourceManager.Unload("textures/old_texture.png");

// 卸载所有资源
resourceManager.UnloadAll();
```

### 查询资源状态

```csharp
// 检查资源是否已加载
if (resourceManager.IsLoaded("textures/player.png"))
{
    Console.WriteLine("资源已在缓存中");
}

// 获取已加载资源数量
Console.WriteLine($"已加载 {resourceManager.LoadedResourceCount} 个资源");

// 获取所有已加载资源的路径
foreach (var path in resourceManager.GetLoadedResourcePaths())
{
    Console.WriteLine($"已加载: {path}");
}
```

### 自定义释放策略

```csharp
using GFramework.Core.Abstractions.Resource;

// 实现基于时间的释放策略
public class TimeBasedReleaseStrategy : IResourceReleaseStrategy
{
    private readonly Dictionary<string, DateTime> _lastAccessTime = new();
    private readonly TimeSpan _timeout = TimeSpan.FromMinutes(5);

    public bool ShouldRelease(string path, int refCount)
    {
        // 引用计数为 0 且超过 5 分钟未访问
        if (refCount > 0)
            return false;

        if (!_lastAccessTime.TryGetValue(path, out var lastAccess))
            return false;

        return DateTime.Now - lastAccess > _timeout;
    }

    public void UpdateAccessTime(string path)
    {
        _lastAccessTime[path] = DateTime.Now;
    }
}

// 使用自定义策略
resourceManager.SetReleaseStrategy(new TimeBasedReleaseStrategy());
```

### 资源池模式

结合对象池实现资源复用：

```csharp
public class BulletPool
{
    private readonly IResourceManager _resourceManager;
    private readonly Queue<Bullet> _pool = new();
    private IResourceHandle<Texture>? _textureHandle;

    public BulletPool(IResourceManager resourceManager)
    {
        _resourceManager = resourceManager;
        // 加载并持有纹理句柄
        _textureHandle = _resourceManager.GetHandle<Texture>("textures/bullet.png");
    }

    public Bullet Get()
    {
        if (_pool.Count > 0)
        {
            return _pool.Dequeue();
        }

        // 创建新子弹，使用缓存的纹理
        var bullet = new Bullet();
        bullet.Texture = _textureHandle?.Resource;
        return bullet;
    }

    public void Return(Bullet bullet)
    {
        bullet.Reset();
        _pool.Enqueue(bullet);
    }

    public void Dispose()
    {
        // 释放纹理句柄
        _textureHandle?.Dispose();
        _textureHandle = null;
    }
}
```

### 资源依赖管理

```csharp
public class MaterialLoader : IResourceLoader<Material>
{
    private readonly IResourceManager _resourceManager;

    public MaterialLoader(IResourceManager resourceManager)
    {
        _resourceManager = resourceManager;
    }

    public Material Load(string path)
    {
        var material = new Material();

        // 加载材质依赖的纹理
        material.DiffuseTexture = _resourceManager.Load<Texture>($"{path}/diffuse.png");
        material.NormalTexture = _resourceManager.Load<Texture>($"{path}/normal.png");

        return material;
    }

    public async Task<Material> LoadAsync(string path)
    {
        var material = new Material();

        // 并行加载依赖资源
        var tasks = new[]
        {
            _resourceManager.LoadAsync<Texture>($"{path}/diffuse.png"),
            _resourceManager.LoadAsync<Texture>($"{path}/normal.png")
        };

        var results = await Task.WhenAll(tasks);
        material.DiffuseTexture = results[0];
        material.NormalTexture = results[1];

        return material;
    }

    public void Unload(Material resource)
    {
        // 材质卸载时，纹理由资源管理器自动管理
        resource?.Dispose();
    }
}
```

## 最佳实践

1. **使用资源句柄管理生命周期**：优先使用句柄而不是直接加载
   ```csharp
   ✓ using var handle = resourceManager.GetHandle<Texture>(path);
   ✗ var texture = resourceManager.Load<Texture>(path); // 需要手动管理
   ```

2. **选择合适的释放策略**：根据游戏需求选择策略
    - 手动释放：适合长期使用的资源（如 UI 纹理）
    - 自动释放：适合临时资源（如特效纹理）

3. **预加载关键资源**：避免游戏中途加载导致卡顿
   ```csharp
   // 在场景加载时预加载
   await PreloadSceneAssets();
   ```

4. **避免重复加载**：使用 `IsLoaded` 检查缓存
   ```csharp
   if (!resourceManager.IsLoaded(path))
   {
       await resourceManager.LoadAsync<Texture>(path);
   }
   ```

5. **及时释放不用的资源**：避免内存泄漏
   ```csharp
   // 场景切换时卸载旧场景资源
   foreach (var path in oldSceneResources)
   {
       resourceManager.Unload(path);
   }
   ```

6. **使用 using 语句管理句柄**：确保引用计数正确
   ```csharp
   ✓ using (var handle = resourceManager.GetHandle<Texture>(path))
   {
       // 使用资源
   } // 自动释放

   ✗ var handle = resourceManager.GetHandle<Texture>(path);
   // 忘记调用 Dispose()
   ```

## 常见问题

### 问题：资源加载失败怎么办？

**解答**：
`Load` 和 `LoadAsync` 方法在失败时返回 `null`，应该检查返回值：

```csharp
var texture = resourceManager.Load<Texture>(path);
if (texture == null)
{
    Logger.Error($"Failed to load texture: {path}");
    // 使用默认纹理
    texture = defaultTexture;
}
```

### 问题：如何避免重复加载相同资源？

**解答**：
资源管理器自动缓存已加载的资源，多次加载相同路径只会返回缓存的实例：

```csharp
var texture1 = resourceManager.Load<Texture>("player.png");
var texture2 = resourceManager.Load<Texture>("player.png");
// texture1 和 texture2 是同一个实例
```

### 问题：什么时候使用手动释放 vs 自动释放？

**解答**：

- **手动释放**：适合长期使用的资源，如 UI、角色模型
- **自动释放**：适合临时资源，如特效、临时纹理

```csharp
// 手动释放：UI 资源长期使用
resourceManager.SetReleaseStrategy(new ManualReleaseStrategy());

// 自动释放：特效资源用完即释放
resourceManager.SetReleaseStrategy(new AutoReleaseStrategy());
```

### 问题：资源句柄的引用计数如何工作？

**解答**：

- `GetHandle` 增加引用计数
- `Dispose` 减少引用计数
- 引用计数为 0 时，根据释放策略决定是否卸载

```csharp
// 引用计数: 0
var handle1 = resourceManager.GetHandle<Texture>(path); // 引用计数: 1
var handle2 = resourceManager.GetHandle<Texture>(path); // 引用计数: 2

handle1.Dispose(); // 引用计数: 1
handle2.Dispose(); // 引用计数: 0（可能被释放）
```

### 问题：如何实现资源热重载？

**解答**：
卸载旧资源后重新加载：

```csharp
public void ReloadResource(string path)
{
    // 卸载旧资源
    resourceManager.Unload(path);

    // 重新加载
    var newResource = resourceManager.Load<Texture>(path);
}
```

### 问题：资源管理器是线程安全的吗？

**解答**：
是的，所有公共方法都是线程安全的，可以在多线程环境中使用：

```csharp
// 在多个线程中并行加载
Parallel.For(0, 10, i =>
{
    var texture = resourceManager.Load<Texture>($"texture_{i}.png");
});
```

## 相关文档

- [对象池系统](/zh-CN/core/pool.md) - 结合对象池复用资源
- [协程系统](/zh-CN/core/coroutine.md) - 异步加载资源
- [Godot 扩展](/zh-CN/godot/extensions.md) - Godot 引擎的资源管理
- [资源管理最佳实践](/zh-CN/tutorials/resource-management.md) - 详细教程
