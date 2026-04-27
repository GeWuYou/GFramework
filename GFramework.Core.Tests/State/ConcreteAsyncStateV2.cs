using GFramework.Core.Abstractions.State;

namespace GFramework.Core.Tests.State;

/// <summary>
///     异步具体状态实现类V2版本，用于测试异步状态的基本功能。
/// </summary>
public sealed class ConcreteAsyncStateV2 : IState, IAsyncState
{
    /// <summary>
    ///     获取或设置是否允许转换。
    /// </summary>
    public bool AllowTransitions { get; set; } = true;

    /// <summary>
    ///     获取进入状态是否被调用的标志。
    /// </summary>
    public bool EnterCalled { get; private set; }

    /// <summary>
    ///     获取退出状态是否被调用的标志。
    /// </summary>
    public bool ExitCalled { get; private set; }

    /// <summary>
    ///     获取进入状态被调用的次数。
    /// </summary>
    public int EnterCallCount { get; private set; }

    /// <summary>
    ///     获取退出状态被调用的次数。
    /// </summary>
    public int ExitCallCount { get; private set; }

    /// <summary>
    ///     获取进入此状态的来源状态。
    /// </summary>
    public IState? EnterFrom { get; private set; }

    /// <summary>
    ///     获取从此状态退出的目标状态。
    /// </summary>
    public IState? ExitTo { get; private set; }

    /// <summary>
    ///     获取或设置转换到目标状态时执行的动作。
    /// </summary>
    public Action<IState>? CanTransitionToAsyncAction { get; set; }

    /// <summary>
    ///     异步进入当前状态时调用的方法。
    /// </summary>
    /// <param name="from">从哪个状态进入。</param>
    public async Task OnEnterAsync(IState? from)
    {
        await Task.Delay(1).ConfigureAwait(false);
        EnterCalled = true;
        EnterCallCount++;
        EnterFrom = from;
    }

    /// <summary>
    ///     异步退出当前状态时调用的方法。
    /// </summary>
    /// <param name="to">退出到哪个状态。</param>
    public async Task OnExitAsync(IState? to)
    {
        await Task.Delay(1).ConfigureAwait(false);
        ExitCalled = true;
        ExitCallCount++;
        ExitTo = to;
    }

    /// <summary>
    ///     异步判断是否可以转换到目标状态。
    /// </summary>
    /// <param name="target">目标状态。</param>
    /// <returns>如果可以转换则返回 <see langword="true" />，否则返回 <see langword="false" />。</returns>
    public async Task<bool> CanTransitionToAsync(IState target)
    {
        await Task.Delay(1).ConfigureAwait(false);
        CanTransitionToAsyncAction?.Invoke(target);
        return AllowTransitions;
    }

    /// <summary>
    ///     进入当前状态时调用的方法（同步版本，抛出异常表示不应被调用）。
    /// </summary>
    /// <param name="from">从哪个状态进入。</param>
    /// <exception cref="InvalidOperationException">总是抛出，表示异步状态不应走同步入口。</exception>
    public void OnEnter(IState? from)
    {
        throw new InvalidOperationException("Sync OnEnter should not be called for async state");
    }

    /// <summary>
    ///     退出当前状态时调用的方法（同步版本，抛出异常表示不应被调用）。
    /// </summary>
    /// <param name="to">退出到哪个状态。</param>
    /// <exception cref="InvalidOperationException">总是抛出，表示异步状态不应走同步入口。</exception>
    public void OnExit(IState? to)
    {
        throw new InvalidOperationException("Sync OnExit should not be called for async state");
    }

    /// <summary>
    ///     判断是否可以转换到目标状态（同步版本，抛出异常表示不应被调用）。
    /// </summary>
    /// <param name="target">目标状态。</param>
    /// <returns>此方法不会正常返回。</returns>
    /// <exception cref="InvalidOperationException">总是抛出，表示异步状态不应走同步入口。</exception>
    public bool CanTransitionTo(IState target)
    {
        throw new InvalidOperationException("Sync CanTransitionTo should not be called for async state");
    }
}
