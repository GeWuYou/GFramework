namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     记录模拟扩展程序集通知处理器的执行次数。
/// </summary>
public static class AdditionalAssemblyNotificationHandlerState
{
    private static int _invocationCount;

    /// <summary>
    ///     获取当前测试进程中该处理器的执行次数。
    /// </summary>
    /// <remarks>
    ///     该计数器通过原子读写维护，以支持 NUnit 并行执行环境中的并发访问。
    /// </remarks>
    public static int InvocationCount => Volatile.Read(ref _invocationCount);

    /// <summary>
    ///     记录一次通知处理，供测试断言显式程序集接入后的运行时行为。
    /// </summary>
    public static void RecordInvocation()
    {
        Interlocked.Increment(ref _invocationCount);
    }

    /// <summary>
    ///     清理共享计数器，避免测试间相互污染。
    /// </summary>
    public static void Reset()
    {
        Interlocked.Exchange(ref _invocationCount, 0);
    }
}
