using GFramework.Core.Abstractions.time;

namespace GFramework.Core.time;

/// <summary>
///     系统时间提供者，返回真实的系统时间
/// </summary>
public sealed class SystemTimeProvider : ITimeProvider
{
    /// <summary>
    ///     获取当前 UTC 时间
    /// </summary>
    public DateTime UtcNow => DateTime.UtcNow;
}