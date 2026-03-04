namespace GFramework.Core.Abstractions.resource;

/// <summary>
///     资源释放策略接口
///     定义当资源引用计数变化时的处理行为
/// </summary>
public interface IResourceReleaseStrategy
{
    /// <summary>
    ///     判断是否应该释放资源
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <param name="refCount">当前引用计数</param>
    /// <returns>如果应该释放返回 true，否则返回 false</returns>
    bool ShouldRelease(string path, int refCount);
}