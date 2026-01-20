using GFramework.Core.Abstractions.logging;
using GFramework.Core.extensions;
using GFramework.Core.logging;
using GFramework.Core.utility;
using GFramework.Game.Abstractions.enums;
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
    /// <summary>
    /// 缓存统计信息实现类
    /// </summary>
    private sealed class CacheStatisticsInfo : IUiCacheStatistics
    {
        public int CacheSize { get; set; }
        public int HitCount { get; set; }
        public int MissCount { get; set; }
        public double HitRate => HitCount + MissCount > 0 ? (double)HitCount / (HitCount + MissCount) : 0;
        public DateTime? LastAccessTime { get; set; }
    }
    
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
    /// 缓存配置：uiKey -> 配置
    /// </summary>
    private readonly Dictionary<string, UiCacheConfig> _cacheConfigs = new();
    
    /// <summary>
    /// 缓存统计：uiKey -> 统计信息
    /// </summary>
    private readonly Dictionary<string, CacheStatisticsInfo> _cacheStatistics = new();
    
    /// <summary>
    /// LRU访问时间队列：uiKey -> 按访问时间排序的实例列表
    /// </summary>
    private readonly Dictionary<string, List<(IUiPageBehavior instance, DateTime accessTime)>> _accessTimeQueue = new();
    
    /// <summary>
    /// LFU访问计数：instance -> 访问次数
    /// </summary>
    private readonly Dictionary<IUiPageBehavior, int> _accessCount = new();

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
        
        // 更新统计信息
        UpdateStatisticsOnRecycle(uiKey);
        
        // 更新访问追踪
        UpdateAccessTracking(uiKey, page);
        
        _cachedInstances[uiKey].Enqueue(page);
        Log.Debug("Recycled UI instance to pool: {0}, poolSize={1}", uiKey, _cachedInstances[uiKey].Count);
        
        // 检查是否需要淘汰
        CheckAndEvict(uiKey);
    }

    /// <summary>
    /// 获取UI的缓存配置
    /// </summary>
    public UiCacheConfig GetCacheConfig(string uiKey)
    {
        return _cacheConfigs.TryGetValue(uiKey, out var config) ? config : UiCacheConfig.Default;
    }
    
    /// <summary>
    /// 设置UI的缓存配置
    /// </summary>
    public void SetCacheConfig(string uiKey, UiCacheConfig config)
    {
        _cacheConfigs[uiKey] = config;
        Log.Debug("Set cache config for UI: {0}, MaxSize={1}, Policy={2}", uiKey, config.MaxCacheSize, config.EvictionPolicy);
        
        // 检查是否需要淘汰
        CheckAndEvict(uiKey);
    }
    
    /// <summary>
    /// 移除UI的缓存配置
    /// </summary>
    public void RemoveCacheConfig(string uiKey)
    {
        if (_cacheConfigs.Remove(uiKey))
        {
            Log.Debug("Removed cache config for UI: {0}", uiKey);
        }
    }
    
    /// <summary>
    /// 获取所有UI的缓存统计信息
    /// </summary>
    public IDictionary<string, IUiCacheStatistics> GetCacheStatistics()
    {
        var result = new Dictionary<string, IUiCacheStatistics>();
        foreach (var kvp in _cacheStatistics)
        {
            result[kvp.Key] = kvp.Value;
        }
        return result;
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
            
            // 更新统计：缓存命中
            UpdateStatisticsOnHit(uiKey);
            
            // 更新访问追踪
            UpdateAccessTracking(uiKey, cached);
            
            Log.Debug("Reused cached UI instance: {0}, remainingInPool={1}", uiKey, queue.Count);
            return cached;
        }

        // 没有缓存则创建新实例
        UpdateStatisticsOnMiss(uiKey);
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
        
        // 从访问追踪移除
        _accessCount.Remove(page);
        if (_accessTimeQueue.TryGetValue(uiKey, out var queue))
        {
            queue.RemoveAll(x => x.instance == page);
        }

        // 销毁Godot节点
        if (page.View is Node node)
        {
            node.QueueFreeX();
        }
    }
    
    /// <summary>
    /// 更新统计信息：回收
    /// </summary>
    private void UpdateStatisticsOnRecycle(string uiKey)
    {
        if (!_cacheStatistics.ContainsKey(uiKey))
            _cacheStatistics[uiKey] = new CacheStatisticsInfo();
        
        var stats = _cacheStatistics[uiKey];
        stats.CacheSize = _cachedInstances[uiKey].Count + 1;
        stats.LastAccessTime = DateTime.Now;
    }
    
    /// <summary>
    /// 更新统计信息：命中
    /// </summary>
    private void UpdateStatisticsOnHit(string uiKey)
    {
        if (!_cacheStatistics.ContainsKey(uiKey))
            _cacheStatistics[uiKey] = new CacheStatisticsInfo();
        
        var stats = _cacheStatistics[uiKey];
        stats.HitCount++;
        stats.CacheSize = _cachedInstances[uiKey].Count;
        stats.LastAccessTime = DateTime.Now;
    }
    
    /// <summary>
    /// 更新统计信息：未命中
    /// </summary>
    private void UpdateStatisticsOnMiss(string uiKey)
    {
        if (!_cacheStatistics.ContainsKey(uiKey))
            _cacheStatistics[uiKey] = new CacheStatisticsInfo();
        
        var stats = _cacheStatistics[uiKey];
        stats.MissCount++;
        stats.CacheSize = _cachedInstances.TryGetValue(uiKey, out var queue) ? queue.Count : 0;
    }
    
    /// <summary>
    /// 更新访问追踪
    /// </summary>
    private void UpdateAccessTracking(string uiKey, IUiPageBehavior instance)
    {
        var now = DateTime.Now;
        
        // LRU: 更新访问时间队列
        if (!_accessTimeQueue.ContainsKey(uiKey))
            _accessTimeQueue[uiKey] = new List<(IUiPageBehavior, DateTime)>();
        
        var timeQueue = _accessTimeQueue[uiKey];
        timeQueue.RemoveAll(x => x.instance == instance);
        timeQueue.Add((instance, now));
        
        // LFU: 更新访问计数
        _accessCount.TryGetValue(instance, out var count);
        _accessCount[instance] = count + 1;
    }
    
    /// <summary>
    /// 检查并执行淘汰
    /// </summary>
    private void CheckAndEvict(string uiKey)
    {
        var config = GetCacheConfig(uiKey);
        var currentSize = _cachedInstances.TryGetValue(uiKey, out var queue) ? queue.Count : 0;

        if (currentSize <= config.MaxCacheSize) return;
        var toEvict = currentSize - config.MaxCacheSize;
            
        for (var i = 0; i < toEvict; i++)
        {
            if (config.EvictionPolicy == CacheEvictionPolicy.Lru)
                EvictLru(uiKey);
            else
                EvictLfu(uiKey);
        }
            
        Log.Debug("Evicted {0} instances for UI: {1}", toEvict, uiKey);
    }
    
    /// <summary>
    /// LRU淘汰策略
    /// </summary>
    private void EvictLru(string uiKey)
    {
        if (!_accessTimeQueue.TryGetValue(uiKey, out var timeQueue) || timeQueue.Count == 0)
            return;
        
        var oldest = timeQueue.OrderBy(x => x.accessTime).First();
        
        // 从队列中移除
        if (_cachedInstances.TryGetValue(uiKey, out var queue))
        {
            var tempQueue = new Queue<IUiPageBehavior>();
            var removed = false;
            
            while (queue.Count > 0)
            {
                var item = queue.Dequeue();
                if (!removed && item == oldest.instance)
                {
                    DestroyInstance(item);
                    removed = true;
                }
                else
                {
                    tempQueue.Enqueue(item);
                }
            }
            
            // 重新填充队列
            while (tempQueue.Count > 0)
                queue.Enqueue(tempQueue.Dequeue());
        }
    }
    
    /// <summary>
    /// LFU淘汰策略
    /// </summary>
    private void EvictLfu(string uiKey)
    {
        if (!_cachedInstances.TryGetValue(uiKey, out var queue) || queue.Count == 0)
            return;
        
        // 找到访问次数最少的实例
        IUiPageBehavior? toRemove = null;
        var minCount = int.MaxValue;
        
        foreach (var instance in queue)
        {
            if (_accessCount.TryGetValue(instance, out var count) && count < minCount)
            {
                minCount = count;
                toRemove = instance;
            }
        }
        
        if (toRemove != null)
        {
            DestroyInstance(toRemove);
        }
    }

    #endregion
}