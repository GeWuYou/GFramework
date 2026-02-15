namespace GFramework.Game.Abstractions.ui;

/// <summary>
/// UI切换中间件处理器接口，支持包裹整个变更过程的逻辑。
/// Around 处理器在变更前后都会执行，可以控制是否继续执行变更。
/// 适用于：性能监控、事务管理、权限验证、日志记录等横切关注点。
/// </summary>
public interface IUiAroundTransitionHandler
{
    /// <summary>
    /// 处理器优先级，数值越小越先执行（外层）。
    /// 建议范围：-1000 到 1000。
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// 判断是否应该处理当前事件。
    /// </summary>
    /// <param name="event">UI切换事件。</param>
    /// <returns>如果应该处理则返回 true，否则返回 false。</returns>
    bool ShouldHandle(UiTransitionEvent @event);

    /// <summary>
    /// 执行中间件逻辑。
    /// </summary>
    /// <param name="event">UI切换事件。</param>
    /// <param name="next">下一个中间件或实际操作的委托。调用此委托以继续执行流程。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    Task HandleAsync(
        UiTransitionEvent @event,
        Func<Task> next,
        CancellationToken cancellationToken);
}