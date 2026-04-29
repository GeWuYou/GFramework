using System.Threading;
using GFramework.Core.Abstractions.Architectures;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     记录 stream dispatch binding 缓存回归中每次分发实际使用的上下文与实例身份。
/// </summary>
internal static class DispatcherStreamContextRefreshState
{
    private static int _nextHandlerInstanceId;

    /// <summary>
    ///     获取每次建流时记录的快照。
    /// </summary>
    public static List<DispatcherPipelineContextSnapshot> HandlerSnapshots { get; } = [];

    /// <summary>
    ///     为新的 handler 测试实例分配稳定编号。
    /// </summary>
    public static int AllocateHandlerInstanceId()
    {
        return Interlocked.Increment(ref _nextHandlerInstanceId);
    }

    /// <summary>
    ///     记录 handler 在当前建流中观察到的上下文。
    /// </summary>
    public static void Record(string dispatchId, int instanceId, IArchitectureContext context)
    {
        HandlerSnapshots.Add(new DispatcherPipelineContextSnapshot(dispatchId, instanceId, context));
    }

    /// <summary>
    ///     清空历史记录与实例编号，避免跨测试污染断言。
    /// </summary>
    public static void Reset()
    {
        _nextHandlerInstanceId = 0;
        HandlerSnapshots.Clear();
    }
}
