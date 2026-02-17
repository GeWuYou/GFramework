using GFramework.Core.Abstractions.rule;
using GFramework.Core.coroutine;
using GFramework.Core.coroutine.extensions;
using GFramework.Core.extensions;
using Mediator;

namespace GFramework.Godot.coroutine;

/// <summary>
///     提供协程相关的扩展方法，用于简化协程的启动和管理。
/// </summary>
public static class ContextAwareCoroutineExtensions
{
    /// <summary>
    /// 发送命令并直接以协程方式运行（无返回值）
    /// </summary>
    /// <param name="contextAware">上下文感知对象，用于发送命令</param>
    /// <param name="command">要发送的命令对象</param>
    /// <param name="segment">协程运行的时间段，默认为 Process</param>
    /// <param name="tag">协程的标签，可用于标识或分组协程</param>
    /// <param name="cancellationToken">用于取消操作的令牌</param>
    /// <returns>返回协程的句柄，可用于后续操作（如停止协程）</returns>
    public static CoroutineHandle RunCommandCoroutine(
        this IContextAware contextAware,
        ICommand command,
        Segment segment = Segment.Process,
        string? tag = null,
        CancellationToken cancellationToken = default)
    {
        return contextAware
            .SendCommandAsync(command, cancellationToken)
            .AsTask()
            .ToCoroutineEnumerator()
            .RunCoroutine(segment, tag);
    }

    /// <summary>
    /// 发送命令并直接以协程方式运行（带返回值）
    /// </summary>
    /// <typeparam name="TResponse">命令返回值的类型</typeparam>
    /// <param name="contextAware">上下文感知对象，用于发送命令</param>
    /// <param name="command">要发送的命令对象</param>
    /// <param name="segment">协程运行的时间段，默认为 Process</param>
    /// <param name="tag">协程的标签，可用于标识或分组协程</param>
    /// <param name="cancellationToken">用于取消操作的令牌</param>
    /// <returns>返回协程的句柄，可用于后续操作（如停止协程）</returns>
    public static CoroutineHandle RunCommandCoroutine<TResponse>(
        this IContextAware contextAware,
        ICommand<TResponse> command,
        Segment segment = Segment.Process,
        string? tag = null,
        CancellationToken cancellationToken = default)
    {
        return contextAware
            .SendCommandAsync(command, cancellationToken)
            .AsTask()
            .ToCoroutineEnumerator()
            .RunCoroutine(segment, tag);
    }

    /// <summary>
    /// 发送查询并直接以协程方式运行（带返回值）
    /// </summary>
    /// <typeparam name="TResponse">查询返回值的类型</typeparam>
    /// <param name="contextAware">上下文感知对象，用于发送查询</param>
    /// <param name="query">要发送的查询对象</param>
    /// <param name="segment">协程运行的时间段，默认为 Process</param>
    /// <param name="tag">协程的标签，可用于标识或分组协程</param>
    /// <param name="cancellationToken">用于取消操作的令牌</param>
    /// <returns>返回协程的句柄，可用于后续操作（如停止协程）</returns>
    public static CoroutineHandle RunQueryCoroutine<TResponse>(
        this IContextAware contextAware,
        IQuery<TResponse> query,
        Segment segment = Segment.Process,
        string? tag = null,
        CancellationToken cancellationToken = default)
    {
        return contextAware
            .SendQueryAsync(query, cancellationToken)
            .AsTask()
            .ToCoroutineEnumerator()
            .RunCoroutine(segment, tag);
    }

    /// <summary>
    /// 发布通知并直接以协程方式运行
    /// </summary>
    /// <param name="contextAware">上下文感知对象，用于发布通知</param>
    /// <param name="notification">要发布的通知对象</param>
    /// <param name="segment">协程运行的时间段，默认为 Process</param>
    /// <param name="tag">协程的标签，可用于标识或分组协程</param>
    /// <param name="cancellationToken">用于取消操作的令牌</param>
    /// <returns>返回协程的句柄，可用于后续操作（如停止协程）</returns>
    public static CoroutineHandle RunPublishCoroutine(
        this IContextAware contextAware,
        INotification notification,
        Segment segment = Segment.Process,
        string? tag = null,
        CancellationToken cancellationToken = default)
    {
        return contextAware
            .PublishAsync(notification, cancellationToken)
            .AsTask()
            .ToCoroutineEnumerator()
            .RunCoroutine(segment, tag);
    }
}