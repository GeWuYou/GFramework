using GFramework.Core.Abstractions.logging;
using GFramework.Core.extensions;
using GFramework.Core.logging;
using GFramework.Core.utility;
using GFramework.Game.Abstractions.ui;
using GFramework.Godot.extensions;
using Godot;

namespace GFramework.Godot.ui;

/// <summary>
/// Godot UI工厂类，用于创建UI页面实例
/// 继承自AbstractContextUtility并实现IUiFactory接口
/// </summary>
public class GodotUiFactory : AbstractContextUtility, IUiFactory
{
    private static readonly ILogger Log = LoggerFactoryResolver.Provider.CreateLogger("GodotUiFactory");
    
    private IGodotUiRegistry _registry = null!;
    
    /// <summary>
    /// 缓存池：uiKey -> 实例队列
    /// </summary>
    private readonly Dictionary<string, Queue<IUiPageBehavior>> _cachedInstances = new();
    
    /// <summary>
    /// 追踪所有创建的实例（用于清理）
    /// </summary>
    private readonly Dictionary<string, HashSet<IUiPageBehavior>> _allInstances = new();

    /// <summary>
    /// 创建或获取UI页面实例
    /// </summary>
    public IUiPageBehavior GetOrCreate(string uiKey, UiInstancePolicy policy = UiInstancePolicy.AlwaysCreate)
    {
        return policy switch
        {
            UiInstancePolicy.Reuse => GetCachedOrCreate(uiKey),
            UiInstancePolicy.Pooled => GetFromPoolOrCreate(uiKey),
            _ => Create(uiKey)
        };
    }

    /// <summary>
    /// 仅创建新实例
    /// </summary>
    public IUiPageBehavior Create(string uiKey)
    {
        var scene = _registry.Get(uiKey);
        var node = scene.Instantiate();

        if (node is not IUiPageBehaviorProvider provider)
            throw new InvalidCastException($"UI scene {uiKey} must implement IUiPageBehaviorProvider");

        var page = provider.GetPage();
        
        // 追踪实例
        if (!_allInstances.ContainsKey(uiKey))
            _allInstances[uiKey] = new HashSet<IUiPageBehavior>();
        _allInstances[uiKey].Add(page);
        
        Log.Debug("Created new UI instance: {0}", uiKey);
        return page;
    }

    /// <summary>
    /// 预加载UI资源
    /// </summary>
    public void Preload(string uiKey, int count = 1)
    {
        Log.Debug("Preloading UI: {0}, count={1}", uiKey, count);
        
        if (!_cachedInstances.ContainsKey(uiKey))
            _cachedInstances[uiKey] = new Queue<IUiPageBehavior>();

        var queue = _cachedInstances[uiKey];
        
        for (int i = 0; i < count; i++)
        {
            var instance = Create(uiKey);
            // 预加载的实例初始状态为隐藏
            instance.OnHide();
            queue.Enqueue(instance);
        }
        
        Log.Debug("Preloaded {0} instances of {1}", count, uiKey);
    }

    /// <summary>
    /// 批量预加载
    /// </summary>
    public void PreloadBatch(params string[] uiKeys)
    {
        foreach (var uiKey in uiKeys)
        {
            Preload(uiKey);
        }
    }

    /// <summary>
    /// 回收实例到缓存池
    /// </summary>
    public void Recycle(IUiPageBehavior page)
    {
        var uiKey = page.Key;
        
        if (!_cachedInstances.ContainsKey(uiKey))
            _cachedInstances[uiKey] = new Queue<IUiPageBehavior>();

        // 确保实例处于隐藏状态
        page.OnHide();
        
        _cachedInstances[uiKey].Enqueue(page);
        Log.Debug("Recycled UI instance to pool: {0}, poolSize={1}", uiKey, _cachedInstances[uiKey].Count);
    }

    /// <summary>
    /// 清理指定UI的缓存
    /// </summary>
    public void ClearCache(string uiKey)
    {
        if (!_cachedInstances.TryGetValue(uiKey, out var queue))
            return;

        int count = queue.Count;
        while (queue.Count > 0)
        {
            var instance = queue.Dequeue();
            DestroyInstance(instance);
        }

        _cachedInstances.Remove(uiKey);
        Log.Debug("Cleared cache for UI: {0}, destroyed {1} instances", uiKey, count);
    }

    /// <summary>
    /// 清理所有缓存
    /// </summary>
    public void ClearAllCache()
    {
        foreach (var uiKey in _cachedInstances.Keys)
        {
            ClearCache(uiKey);
        }
        
        Log.Debug("Cleared all UI caches");
    }

    /// <summary>
    /// 检查是否有缓存的实例
    /// </summary>
    public bool HasCached(string uiKey)
    {
        return _cachedInstances.TryGetValue(uiKey, out var queue) && queue.Count > 0;
    }

    protected override void OnInit()
    {
        _registry = this.GetUtility<IGodotUiRegistry>()!;
    }

    #region Private Methods

    /// <summary>
    /// 获取缓存实例或创建新实例（Reuse策略）
    /// </summary>
    private IUiPageBehavior GetCachedOrCreate(string uiKey)
    {
        // 优先从缓存池获取
        if (_cachedInstances.TryGetValue(uiKey, out var queue) && queue.Count > 0)
        {
            var cached = queue.Dequeue();
            Log.Debug("Reused cached UI instance: {0}, remainingInPool={1}", uiKey, queue.Count);
            return cached;
        }

        // 没有缓存则创建新实例
        Log.Debug("No cached instance, creating new: {0}", uiKey);
        return Create(uiKey);
    }

    /// <summary>
    /// 从池中获取或创建（Pooled策略）
    /// 如果池为空，自动创建并填充
    /// </summary>
    private IUiPageBehavior GetFromPoolOrCreate(string uiKey)
    {
        // 如果池为空，先预加载一个
        if (HasCached(uiKey)) return GetCachedOrCreate(uiKey);
        Log.Debug("Pool empty, preloading instance: {0}", uiKey);
        Preload(uiKey);

        return GetCachedOrCreate(uiKey);
    }

    /// <summary>
    /// 销毁实例
    /// </summary>
    private void DestroyInstance(IUiPageBehavior page)
    {
        var uiKey = page.Key;
        
        // 从追踪列表移除
        if (_allInstances.TryGetValue(uiKey, out var set))
        {
            set.Remove(page);
        }

        // 销毁Godot节点
        if (page.View is Node node)
        {
            node.QueueFreeX();
        }
    }

    #endregion
}