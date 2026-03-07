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

