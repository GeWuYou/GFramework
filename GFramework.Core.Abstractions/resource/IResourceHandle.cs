namespace GFramework.Core.Abstractions.resource;

/// <summary>
///     资源句柄接口，用于管理资源的生命周期和引用
/// </summary>
/// <typeparam name="T">资源类型</typeparam>
public interface IResourceHandle<out T> : IDisposable where T : class
{
    /// <summary>
    ///     获取资源实例
    /// </summary>
    T? Resource { get; }

    /// <summary>
    ///     获取资源路径
    /// </summary>
    string Path { get; }

    /// <summary>
    ///     获取资源是否有效（未被释放）
    /// </summary>
    bool IsValid { get; }

    /// <summary>
    ///     获取当前引用计数
    /// </summary>
    int ReferenceCount { get; }

    /// <summary>
    ///     增加引用计数
    /// </summary>
    void AddReference();

    /// <summary>
    ///     减少引用计数
    /// </summary>
    void RemoveReference();
}