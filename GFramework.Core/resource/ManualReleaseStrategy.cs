using GFramework.Core.Abstractions.resource;

namespace GFramework.Core.resource;

/// <summary>
///     手动释放策略
///     引用计数降为 0 时不自动卸载资源，需要手动调用 Unload
/// </summary>
public class ManualReleaseStrategy : IResourceReleaseStrategy
{
    /// <summary>
    ///     判断是否应该释放资源（始终返回 false）
    /// </summary>
    public bool ShouldRelease(string path, int refCount)
    {
        // 手动释放策略：永远不自动释放，由用户显式调用 Unload
        return false;
    }
}