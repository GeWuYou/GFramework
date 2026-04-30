using System.Threading;
using GFramework.Core.Abstractions.Architectures;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     记录 stream dispatch binding 缓存回归中每次分发实际使用的上下文与实例身份。
/// </summary>
internal static class DispatcherStreamContextRefreshState
{
    private static readonly Lock _syncRoot = new();
    private static int _nextHandlerInstanceId;
    private static readonly List<DispatcherPipelineContextSnapshot> _handlerSnapshots = [];

    /// <summary>
    ///     获取每次建流时记录的快照副本。
    /// </summary>
    /// <returns>当前已记录的 handler 上下文快照副本。</returns>
    /// <remarks>共享状态通过 <c>_syncRoot</c> 串行化，避免并行测试写入抖动。</remarks>
    public static IReadOnlyList<DispatcherPipelineContextSnapshot> HandlerSnapshots
    {
        get
        {
            lock (_syncRoot)
            {
                return _handlerSnapshots.ToArray();
            }
        }
    }

    /// <summary>
    ///     为新的 handler 测试实例分配稳定编号。
    /// </summary>
    /// <returns>单调递增的 handler 实例编号。</returns>
    public static int AllocateHandlerInstanceId()
    {
        return Interlocked.Increment(ref _nextHandlerInstanceId);
    }

    /// <summary>
    ///     记录 handler 在当前建流中观察到的上下文。
    /// </summary>
    /// <param name="dispatchId">触发本次记录的稳定分发标识。</param>
    /// <param name="instanceId">观察到该上下文的 handler 实例编号。</param>
    /// <param name="context">当前分发注入到 handler 的架构上下文。</param>
    /// <remarks>写入过程通过 <c>_syncRoot</c> 串行化，确保快照列表保持稳定顺序。</remarks>
    public static void Record(string dispatchId, int instanceId, IArchitectureContext context)
    {
        lock (_syncRoot)
        {
            _handlerSnapshots.Add(new DispatcherPipelineContextSnapshot(dispatchId, instanceId, context));
        }
    }

    /// <summary>
    ///     清空历史记录与实例编号，避免跨测试污染断言。
    /// </summary>
    /// <remarks>重置过程通过 <c>_syncRoot</c> 串行化，避免读取端观察到半清理状态。</remarks>
    public static void Reset()
    {
        lock (_syncRoot)
        {
            _nextHandlerInstanceId = 0;
            _handlerSnapshots.Clear();
        }
    }
}
