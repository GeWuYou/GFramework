using GFramework.Core.Abstractions.utility;

namespace GFramework.Game.Abstractions.ui;

/// <summary>
/// UI工厂接口，用于创建UI页面实例
/// </summary>
public interface IUiFactory : IContextUtility
{
    /// <summary>
    /// 创建或获取UI页面实例
    /// </summary>
    /// <param name="uiKey">UI标识键</param>
    /// <param name="policy">实例管理策略</param>
    /// <returns>UI页面实例</returns>
    IUiPageBehavior GetOrCreate(string uiKey, UiInstancePolicy policy = UiInstancePolicy.AlwaysCreate);
    
    /// <summary>
    /// 仅创建新实例（不使用缓存）
    /// </summary>
    IUiPageBehavior Create(string uiKey);
    
    /// <summary>
    /// 预加载UI资源到缓存池
    /// </summary>
    /// <param name="uiKey">UI标识键</param>
    /// <param name="count">预加载数量，默认1个</param>
    void Preload(string uiKey, int count = 1);
    
    /// <summary>
    /// 批量预加载
    /// </summary>
    void PreloadBatch(params string[] uiKeys);
    
    /// <summary>
    /// 回收实例到缓存池
    /// </summary>
    /// <param name="page">要回收的页面实例</param>
    void Recycle(IUiPageBehavior page);
    
    /// <summary>
    /// 清理指定UI的缓存实例
    /// </summary>
    void ClearCache(string uiKey);
    
    /// <summary>
    /// 清理所有缓存
    /// </summary>
    void ClearAllCache();
    
    /// <summary>
    /// 检查是否有缓存的实例
    /// </summary>
    bool HasCached(string uiKey);
}