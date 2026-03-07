---
title: 移动平台优化指南
description: 针对移动平台的性能优化、内存管理和电池优化最佳实践
---

# 移动平台优化指南

## 概述

移动平台游戏开发面临着独特的挑战：有限的内存、较弱的处理器、电池续航限制、触摸输入、多样的屏幕尺寸等。本指南将帮助你使用
GFramework 开发高性能的移动游戏，提供针对性的优化策略和最佳实践。

**移动平台的主要限制**：

- **内存限制**：移动设备内存通常在 2-8GB，远低于 PC
- **CPU 性能**：移动 CPU 性能较弱，且受热量限制
- **GPU 性能**：移动 GPU 功能有限，填充率和带宽受限
- **电池续航**：高性能运行会快速消耗电池
- **存储空间**：应用包大小受限，用户存储空间有限
- **网络环境**：移动网络不稳定，延迟较高

**优化目标**：

- 减少内存占用（目标：&lt;200MB）
- 降低 CPU 使用率（目标：&lt;30%）
- 优化 GPU 渲染（目标：60 FPS）
- 延长电池续航（目标：3+ 小时）
- 减小包体大小（目标：&lt;100MB）

## 核心概念

### 1. 内存管理

移动设备内存有限，需要精细管理：

```csharp
// 监控内存使用
public class MemoryMonitor : AbstractSystem
{
    private const long MemoryWarningThreshold = 150 * 1024 * 1024; // 150MB
    private const long MemoryCriticalThreshold = 200 * 1024 * 1024; // 200MB

    protected override void OnInit()
    {
        this.RegisterEvent&lt;GameUpdateEvent&gt;(OnUpdate);
    }

    private void OnUpdate(GameUpdateEvent e)
    {
        // 每 5 秒检查一次内存
        if (e.TotalTime % 5.0 &lt; e.DeltaTime)
        {
            CheckMemoryUsage();
        }
    }

    private void CheckMemoryUsage()
    {
        var memoryUsage = GC.GetTotalMemory(false);

        if (memoryUsage &gt; MemoryCriticalThreshold)
        {
            // 内存严重不足，强制清理
            SendEvent(new MemoryCriticalEvent());
            ForceMemoryCleanup();
        }
        else if (memoryUsage &gt; MemoryWarningThreshold)
        {
            // 内存警告，温和清理
            SendEvent(new MemoryWarningEvent());
            SoftMemoryCleanup();
        }
    }

    private void ForceMemoryCleanup()
    {
        // 卸载不必要的资源
        var resourceManager = this.GetUtility&lt;IResourceManager&gt;();
        resourceManager.UnloadUnusedResources();

        // 清理对象池
        var poolSystem = this.GetSystem&lt;ObjectPoolSystem&gt;();
        poolSystem.TrimPools();

        // 强制 GC
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private void SoftMemoryCleanup()
    {
        // 温和清理：只清理明确不需要的资源
        var resourceManager = this.GetUtility&lt;IResourceManager&gt;();
        resourceManager.UnloadUnusedResources();
    }
}
```

### 2. 性能分析

使用性能分析工具识别瓶颈：

```csharp
public class PerformanceProfiler : AbstractSystem
{
    private readonly Dictionary&lt;string, PerformanceMetrics&gt; _metrics = new();

    public IDisposable Profile(string name)
    {
        return new ProfileScope(name, this);
    }

    private void RecordMetric(string name, double duration)
    {
        if (!_metrics.TryGetValue(name, out var metrics))
        {
            metrics = new PerformanceMetrics();
            _metrics[name] = metrics;
        }

        metrics.AddSample(duration);
    }

    public void PrintReport()
    {
        Console.WriteLine("\n=== 性能报告 ===");
        foreach (var (name, metrics) in _metrics.OrderByDescending(x =&gt; x.Value.AverageMs))
        {
            Console.WriteLine($"{name}:");
            Console.WriteLine($"  平均: {metrics.AverageMs:F2}ms");
            Console.WriteLine($"  最大: {metrics.MaxMs:F2}ms");
            Console.WriteLine($"  最小: {metrics.MinMs:F2}ms");
            Console.WriteLine($"  调用次数: {metrics.SampleCount}");
        }
    }

    private class ProfileScope : IDisposable
    {
        private readonly string _name;
        private readonly PerformanceProfiler _profiler;
        private readonly Stopwatch _stopwatch;

        public ProfileScope(string name, PerformanceProfiler profiler)
        {
            _name = name;
            _profiler = profiler;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _profiler.RecordMetric(_name, _stopwatch.Elapsed.TotalMilliseconds);
        }
    }
}

// 使用示例
public class GameSystem : AbstractSystem
{
    private PerformanceProfiler _profiler;

    protected override void OnInit()
    {
        _profiler = this.GetSystem&lt;PerformanceProfiler&gt;();
    }

    private void UpdateGame()
    {
        using (_profiler.Profile("GameUpdate"))
        {
            // 游戏更新逻辑
        }
    }
}
```

### 3. 电池优化

减少不必要的计算和渲染：

```csharp
public class PowerSavingSystem : AbstractSystem
{
    private bool _isPowerSavingMode;
    private int _targetFrameRate = 60;

    protected override void OnInit()
    {
        this.RegisterEvent&lt;BatteryLowEvent&gt;(OnBatteryLow);
        this.RegisterEvent&lt;BatteryNormalEvent&gt;(OnBatteryNormal);
    }

    private void OnBatteryLow(BatteryLowEvent e)
    {
        EnablePowerSavingMode();
    }

    private void OnBatteryNormal(BatteryNormalEvent e)
    {
        DisablePowerSavingMode();
    }

    private void EnablePowerSavingMode()
    {
        _isPowerSavingMode = true;

        // 降低帧率
        _targetFrameRate = 30;
        Application.targetFrameRate = _targetFrameRate;

        // 降低渲染质量
        QualitySettings.SetQualityLevel(0);

        // 减少粒子效果
        SendEvent(new ReduceEffectsEvent());

        // 暂停非关键系统
        PauseNonCriticalSystems();

        Console.WriteLine("省电模式已启用");
    }

    private void DisablePowerSavingMode()
    {
        _isPowerSavingMode = false;

        // 恢复帧率
        _targetFrameRate = 60;
        Application.targetFrameRate = _targetFrameRate;

        // 恢复渲染质量
        QualitySettings.SetQualityLevel(2);

        // 恢复粒子效果
        SendEvent(new RestoreEffectsEvent());

        // 恢复非关键系统
        ResumeNonCriticalSystems();

        Console.WriteLine("省电模式已禁用");
    }

    private void PauseNonCriticalSystems()
    {
        // 暂停动画系统
        var animationSystem = this.GetSystem&lt;AnimationSystem&gt;();
        animationSystem?.Pause();

        // 暂停音效系统（保留音乐）
        var audioSystem = this.GetSystem&lt;AudioSystem&gt;();
        audioSystem?.PauseSoundEffects();
    }

    private void ResumeNonCriticalSystems()
    {
        var animationSystem = this.GetSystem&lt;AnimationSystem&gt;();
        animationSystem?.Resume();

        var audioSystem = this.GetSystem&lt;AudioSystem&gt;();
        audioSystem?.ResumeSoundEffects();
    }
}
```

## 内存优化

### 1. 资源管理策略

实现智能资源加载和卸载：

```csharp
public class MobileResourceManager : AbstractSystem
{
    private readonly IResourceManager _resourceManager;
    private readonly Dictionary&lt;string, ResourcePriority&gt; _resourcePriorities = new();
    private readonly HashSet&lt;string&gt; _loadedResources = new();

    public MobileResourceManager(IResourceManager resourceManager)
    {
        _resourceManager = resourceManager;
    }

    protected override void OnInit()
    {
        // 配置资源优先级
        ConfigureResourcePriorities();

        // 监听场景切换事件
        this.RegisterEvent&lt;SceneChangedEvent&gt;(OnSceneChanged);

        // 监听内存警告
        this.RegisterEvent&lt;MemoryWarningEvent&gt;(OnMemoryWarning);
    }

    private void ConfigureResourcePriorities()
    {
        // 高优先级：UI、玩家资源
        _resourcePriorities["ui/"] = ResourcePriority.High;
        _resourcePriorities["player/"] = ResourcePriority.High;

        // 中优先级：敌人、道具
        _resourcePriorities["enemy/"] = ResourcePriority.Medium;
        _resourcePriorities["item/"] = ResourcePriority.Medium;

        // 低优先级：特效、装饰
        _resourcePriorities["effect/"] = ResourcePriority.Low;
        _resourcePriorities["decoration/"] = ResourcePriority.Low;
    }

    public async Task&lt;T&gt; LoadResourceAsync&lt;T&gt;(string path) where T : class
    {
        // 检查内存
        if (IsMemoryLow())
        {
            // 内存不足，先清理低优先级资源
            UnloadLowPriorityResources();
        }

        var resource = await _resourceManager.LoadAsync&lt;T&gt;(path);
        _loadedResources.Add(path);

        return resource;
    }

    private void OnSceneChanged(SceneChangedEvent e)
    {
        // 场景切换时，卸载旧场景资源
        UnloadSceneResources(e.PreviousScene);

        // 预加载新场景资源
        PreloadSceneResources(e.NewScene);
    }

    private void OnMemoryWarning(MemoryWarningEvent e)
    {
        // 内存警告，卸载低优先级资源
        UnloadLowPriorityResources();
    }

    private void UnloadLowPriorityResources()
    {
        var resourcesToUnload = _loadedResources
            .Where(path =&gt; GetResourcePriority(path) == ResourcePriority.Low)
            .ToList();

        foreach (var path in resourcesToUnload)
        {
            _resourceManager.Unload(path);
            _loadedResources.Remove(path);
        }

        Console.WriteLine($"卸载了 {resourcesToUnload.Count} 个低优先级资源");
    }

    private ResourcePriority GetResourcePriority(string path)
    {
        foreach (var (prefix, priority) in _resourcePriorities)
        {
            if (path.StartsWith(prefix))
                return priority;
        }

        return ResourcePriority.Medium;
    }

    private bool IsMemoryLow()
    {
        var memoryUsage = GC.GetTotalMemory(false);
        return memoryUsage &gt; 150 * 1024 * 1024; // 150MB
    }
}

public enum ResourcePriority
{
    Low,
    Medium,
    High
}
```

### 2. 纹理压缩和优化

使用合适的纹理格式和压缩：

```csharp
public class TextureOptimizer
{
    public static TextureSettings GetOptimalSettings(string platform)
    {
        return platform switch
        {
            "iOS" => new TextureSettings
            {
                Format = TextureFormat.PVRTC_RGB4,
                MaxSize = 2048,
                MipmapEnabled = true,
                Compression = TextureCompression.High
            },
            "Android" => new TextureSettings
            {
                Format = TextureFormat.ETC2_RGB,
                MaxSize = 2048,
                MipmapEnabled = true,
                Compression = TextureCompression.High
            },
            _ => new TextureSettings
            {
                Format = TextureFormat.RGB24,
                MaxSize = 4096,
                MipmapEnabled = true,
                Compression = TextureCompression.Normal
            }
        };
    }
}
```

### 3. 对象池优化

针对移动平台优化对象池，限制池大小并定期清理：

```csharp
public class MobileObjectPool&lt;T&gt; : AbstractObjectPoolSystem&lt;string, T&gt;
    where T : IPoolableObject
{
    private const int MaxPoolSize = 50;

    public new void Release(string key, T obj)
    {
        if (Pools.TryGetValue(key, out var pool) && pool.Count >= MaxPoolSize)
        {
            obj.OnPoolDestroy();
            return;
        }
        base.Release(key, obj);
    }

    protected override T Create(string key)
    {
        throw new NotImplementedException();
    }
}
```

### 4. 避免内存泄漏

确保正确释放资源和取消事件订阅：

```csharp
public class LeakFreeSystem : AbstractSystem
{
    private IResourceHandle&lt;Texture&gt;? _textureHandle;
    private IUnRegister? _eventUnregister;

    protected override void OnInit()
    {
        _eventUnregister = this.RegisterEvent&lt;GameEvent&gt;(OnGameEvent);
    }

    protected override void OnDestroy()
    {
        // 释放资源句柄
        _textureHandle?.Dispose();
        _textureHandle = null;

        // 取消事件订阅
        _eventUnregister?.UnRegister();
        _eventUnregister = null;

        base.OnDestroy();
    }

    private void OnGameEvent(GameEvent e)
    {
        // 处理事件
    }
}
```

## 性能优化

### 1. CPU 优化

减少 CPU 计算负担：

```csharp
public class CPUOptimizer : AbstractSystem
{
    private const int UpdateInterval = 5; // 每 5 帧更新一次
    private int _frameCount;

    protected override void OnInit()
    {
        this.RegisterEvent&lt;GameUpdateEvent&gt;(OnUpdate);
    }

    private void OnUpdate(GameUpdateEvent e)
    {
        _frameCount++;

        // 降低更新频率
        if (_frameCount % UpdateInterval == 0)
        {
            UpdateNonCriticalSystems();
        }

        // 关键系统每帧更新
        UpdateCriticalSystems();
    }

    private void UpdateCriticalSystems()
    {
        // 玩家输入、物理等关键系统
    }

    private void UpdateNonCriticalSystems()
    {
        // AI、动画等非关键系统
    }
}
```

### 2. 批量处理

使用批量操作减少函数调用：

```csharp
public class BatchProcessor : AbstractSystem
{
    private readonly List&lt;Entity&gt; _entitiesToUpdate = new();

    public void ProcessEntities()
    {
        // 批量处理，减少函数调用开销
        for (int i = 0; i < _entitiesToUpdate.Count; i++)
        {
            var entity = _entitiesToUpdate[i];
            entity.Update();
        }
    }
}
```

### 3. 缓存计算结果

避免重复计算：

```csharp
public class CachedCalculator
{
    private readonly Dictionary&lt;string, float&gt; _cache = new();

    public float GetDistance(Vector3 a, Vector3 b)
    {
        var key = $"{a}_{b}";

        if (_cache.TryGetValue(key, out var distance))
        {
            return distance;
        }

        distance = Vector3.Distance(a, b);
        _cache[key] = distance;

        return distance;
    }

    public void ClearCache()
    {
        _cache.Clear();
    }
}
```

## 电池优化

### 1. 动态帧率调整

根据场景复杂度调整帧率：

```csharp
public class DynamicFrameRateSystem : AbstractSystem
{
    private int _targetFrameRate = 60;

    public void AdjustFrameRate(SceneComplexity complexity)
    {
        _targetFrameRate = complexity switch
        {
            SceneComplexity.Low => 60,
            SceneComplexity.Medium => 45,
            SceneComplexity.High => 30,
            _ => 60
        };

        Application.targetFrameRate = _targetFrameRate;
    }
}

public enum SceneComplexity
{
    Low,
    Medium,
    High
}
```

### 2. 后台优化

应用进入后台时降低性能消耗：

```csharp
public class BackgroundOptimizer : AbstractSystem
{
    protected override void OnInit()
    {
        Application.focusChanged += OnFocusChanged;
    }

    private void OnFocusChanged(bool hasFocus)
    {
        if (hasFocus)
        {
            OnApplicationForeground();
        }
        else
        {
            OnApplicationBackground();
        }
    }

    private void OnApplicationBackground()
    {
        // 降低帧率
        Application.targetFrameRate = 10;

        // 暂停音频
        AudioListener.pause = true;

        // 暂停非关键系统
        PauseNonCriticalSystems();
    }

    private void OnApplicationForeground()
    {
        // 恢复帧率
        Application.targetFrameRate = 60;

        // 恢复音频
        AudioListener.pause = false;

        // 恢复系统
        ResumeNonCriticalSystems();
    }

    private void PauseNonCriticalSystems()
    {
        SendEvent(new PauseNonCriticalSystemsEvent());
    }

    private void ResumeNonCriticalSystems()
    {
        SendEvent(new ResumeNonCriticalSystemsEvent());
    }

    protected override void OnDestroy()
    {
        Application.focusChanged -= OnFocusChanged;
        base.OnDestroy();
    }
}
```

## UI 优化

### 1. 触摸优化

优化触摸输入处理：

```csharp
public class TouchInputSystem : AbstractSystem
{
    private const float TouchThreshold = 10f; // 最小移动距离
    private Vector2 _lastTouchPosition;

    protected override void OnInit()
    {
        this.RegisterEvent&lt;TouchEvent&gt;(OnTouch);
    }

    private void OnTouch(TouchEvent e)
    {
        switch (e.Phase)
        {
            case TouchPhase.Began:
                _lastTouchPosition = e.Position;
                break;

            case TouchPhase.Moved:
                var delta = e.Position - _lastTouchPosition;
                if (delta.magnitude > TouchThreshold)
                {
                    ProcessTouchMove(delta);
                    _lastTouchPosition = e.Position;
                }
                break;

            case TouchPhase.Ended:
                ProcessTouchEnd(e.Position);
                break;
        }
    }

    private void ProcessTouchMove(Vector2 delta)
    {
        SendEvent(new TouchMoveEvent { Delta = delta });
    }

    private void ProcessTouchEnd(Vector2 position)
    {
        SendEvent(new TouchEndEvent { Position = position });
    }
}
```

### 2. UI 元素池化

复用 UI 元素减少创建开销：

```csharp
public class UIElementPool : AbstractObjectPoolSystem&lt;string, UIElement&gt;
{
    protected override UIElement Create(string key)
    {
        return new UIElement(key);
    }

    public UIElement GetButton()
    {
        return Acquire("button");
    }

    public void ReturnButton(UIElement button)
    {
        Release("button", button);
    }
}

public class UIElement : IPoolableObject
{
    public string Type { get; }

    public UIElement(string type)
    {
        Type = type;
    }

    public void OnAcquire()
    {
        // 激活 UI 元素
    }

    public void OnRelease()
    {
        // 重置状态
    }

    public void OnPoolDestroy()
    {
        // 清理资源
    }
}
```

## 平台适配

### iOS 优化

```csharp
public class iOSOptimizer
{
    public static void ApplyOptimizations()
    {
        // 注意：图形 API 和多线程渲染需要在 Player Settings 中配置
        // 在 Unity Editor 中：Edit > Project Settings > Player > Other Settings
        // - Graphics APIs: 选择 Metal
        // - Multithreaded Rendering: 启用

        // 运行时可以调整的设置：

        // 优化纹理质量（0 = 最高质量）
        QualitySettings.masterTextureLimit = 0;

        // 设置目标帧率
        Application.targetFrameRate = 60;

        // 启用 VSync（0 = 关闭，1 = 每帧同步，2 = 每两帧同步）
        QualitySettings.vSyncCount = 1;

        // 优化阴影质量
        QualitySettings.shadowDistance = 50f;
        QualitySettings.shadowResolution = ShadowResolution.Medium;
    }
}
```

### Android 优化

```csharp
public class AndroidOptimizer
{
    public static void ApplyOptimizations()
    {
        // 注意：图形 API 和多线程渲染需要在 Player Settings 中配置
        // 在 Unity Editor 中：Edit > Project Settings > Player > Other Settings
        // - Graphics APIs: 选择 Vulkan 或 OpenGL ES 3.0
        // - Multithreaded Rendering: 启用

        // 运行时可以调整的设置：

        // 优化纹理质量
        QualitySettings.masterTextureLimit = 0;

        // 设置目标帧率
        Application.targetFrameRate = 60;

        // 根据设备性能调整质量等级
        var devicePerformance = GetDevicePerformance();
        QualitySettings.SetQualityLevel(devicePerformance switch
        {
            DevicePerformance.Low => 0,
            DevicePerformance.Medium => 1,
            DevicePerformance.High => 2,
            _ => 1
        });

        // GC 优化建议：
        // 警告：完全禁用 GC (GarbageCollector.Mode.Disabled) 在内存受限设备上有风险
        // 仅在以下情况考虑使用：
        // 1. 高端设备（4GB+ RAM）
        // 2. 配合自定义内存管理策略
        // 3. 经过充分测试

        // 推荐的 GC 优化方式：
        // 1. 使用增量 GC 减少卡顿
        GarbageCollector.GCMode = GarbageCollector.Mode.Enabled;

        // 2. 调整 GC 增量时间片（微秒）
        GarbageCollector.incrementalTimeSliceNanoseconds = 2000000; // 2ms

        // 3. 定期在合适的时机手动触发 GC（如加载界面）
        // GC.Collect();
    }

    private static DevicePerformance GetDevicePerformance()
    {
        var memory = SystemInfo.systemMemorySize;
        var processorCount = SystemInfo.processorCount;

        if (memory < 2048 || processorCount < 4)
            return DevicePerformance.Low;
        else if (memory < 4096 || processorCount < 6)
            return DevicePerformance.Medium;
        else
            return DevicePerformance.High;
    }
}
```

## 最佳实践

### 1. 资源管理

- 使用资源优先级系统
- 及时卸载不用的资源
- 使用资源压缩
- 实现资源预加载

### 2. 内存管理

- 监控内存使用
- 限制对象池大小
- 避免内存泄漏
- 定期执行 GC

### 3. 性能优化

- 降低更新频率
- 使用批量处理
- 缓存计算结果
- 优化算法复杂度

### 4. 电池优化

- 动态调整帧率
- 后台降低性能
- 减少渲染开销
- 优化网络请求

### 5. UI 优化

- 优化触摸处理
- 池化 UI 元素
- 减少 UI 层级
- 使用异步加载

## 常见问题

### 问题：如何监控移动设备的内存使用？

**解答**：

使用 `GC.GetTotalMemory()` 监控托管内存，结合平台 API 监控总内存：

```csharp
var managedMemory = GC.GetTotalMemory(false);
Console.WriteLine($"托管内存: {managedMemory / 1024 / 1024}MB");
```

### 问题：如何优化移动游戏的启动时间？

**解答**：

- 延迟加载非关键资源
- 使用异步初始化
- 减少启动时的计算
- 优化资源包大小

### 问题：如何处理不同设备的性能差异？

**解答**：

实现设备性能分级系统：

```csharp
public enum DevicePerformance
{
    Low,
    Medium,
    High
}

public class DeviceProfiler
{
    public static DevicePerformance GetDevicePerformance()
    {
        var memory = SystemInfo.systemMemorySize;
        var processorCount = SystemInfo.processorCount;

        if (memory < 2048 || processorCount < 4)
            return DevicePerformance.Low;
        else if (memory < 4096 || processorCount < 6)
            return DevicePerformance.Medium;
        else
            return DevicePerformance.High;
    }
}
```

### 问题：如何优化移动游戏的网络性能？

**解答**：

- 使用数据压缩
- 批量发送请求
- 实现请求队列
- 处理网络中断

## 相关文档

- [资源管理系统](/zh-CN/core/resource) - 资源管理详细说明
- [对象池系统](/zh-CN/core/pool) - 对象池优化
- [协程系统](/zh-CN/core/coroutine) - 异步操作优化
- [架构模式最佳实践](/zh-CN/best-practices/architecture-patterns) - 架构设计

---

**文档版本**: 1.0.0
**更新日期**: 2026-03-07

