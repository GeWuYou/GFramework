namespace GFramework.Core.Coroutine;

/// <summary>
///     协程常量定义类，包含协程系统的常量值
/// </summary>
public static class CoroutineConstants
{
    /// <summary>
    ///     默认最大协程数量
    /// </summary>
    public const int DefaultMaxCoroutines = 1000;

    /// <summary>
    ///     默认协程超时时间（秒），0表示不限制
    /// </summary>
    public const float DefaultCoroutineTimeout = 0f;

    /// <summary>
    ///     协程名称的最大长度
    /// </summary>
    public const int MaxNameLength = 256;

    /// <summary>
    ///     默认协程检查间隔（秒），用于协程泄漏检测
    /// </summary>
    public const float DefaultLeakCheckInterval = 60f;

    /// <summary>
    ///     协程泄漏警告的持续时间阈值（秒）
    /// </summary>
    public const float LeakWarningDuration = 300f;
}