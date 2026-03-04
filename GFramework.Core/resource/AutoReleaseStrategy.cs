using GFramework.Core.Abstractions.resource;

namespace GFramework.Core.resource;

/// <summary>
///     自动释放策略
///     当资源引用计数降为 0 时自动卸载资源
/// </summary>
public class AutoReleaseStrategy : IResourceReleaseStrategy
{
    /// <summary>
    ///     判断是否应该释放资源
    ///     当引用计数降为 0 时返回 true
    /// </summary>
    public bool ShouldRelease(string path, int refCount)
    {
        return refCount <= 0;
    }
}