namespace GFramework.Core.Abstractions.Coroutine.Config;

/// <summary>
///     协程配置类，定义协程系统的行为参数
/// </summary>
/// <remarks>
///     协程配置可以在IArchitectureConfiguration中设置，影响整个协程系统的行为
/// </remarks>
public class CoroutineProperties
{
    /// <summary>
    ///     获取或设置默认错误处理器（架构级统一处理）
    /// </summary>
    /// <remarks>
    ///     当协程发生异常时，如果协程本身或作用域没有设置错误处理器，则使用此处理器
    /// </remarks>
    public ICoroutineErrorHandler? DefaultErrorHandler { get; set; } = null;

    /// <summary>
    ///     获取或设置最大协程数量限制
    /// </summary>
    /// <remarks>
    ///     当活跃协程数量超过此限制时，新的协程启动请求将被拒绝
    ///     设置为0表示不限制
    /// </remarks>
    public int MaxCoroutines { get; set; } = 1000;

    /// <summary>
    ///     获取或设置是否启用协程调试信息
    /// </summary>
    /// <remarks>
    ///     启用后会记录协程的启动、完成、取消等事件，增加开销
    /// </remarks>
    public bool EnableDebugInfo { get; set; } = false;

    /// <summary>
    ///     获取或设置协程超时时间（秒）
    /// </summary>
    /// <remarks>
    ///     当协程运行时间超过此限制时，会自动取消协程并记录警告
    ///     设置为0表示不限制
    /// </remarks>
    public float CoroutineTimeout { get; set; } = 0f;

    /// <summary>
    ///     获取或设置是否在异常时停止整个作用域
    /// </summary>
    /// <remarks>
    ///     设置为true时，当作用域内任意协程发生异常，整个作用域将被取消
    /// </remarks>
    public bool StopScopeOnError { get; set; } = false;

    /// <summary>
    ///     获取或设置是否启用协程泄漏检测
    /// </summary>
    /// <remarks>
    ///     启用后会定期检查长时间运行的协程，并记录警告
    /// </remarks>
    public bool EnableLeakDetection { get; set; } = true;

    /// <summary>
    ///     获取或设置协程泄漏检测的持续时间阈值（秒）
    /// </summary>
    /// <remarks>
    ///     当协程运行时间超过此限制时，会触发泄漏警告
    /// </remarks>
    public float LeakWarningDuration { get; set; } = 300f;
}