// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Abstractions.Pause;
using GFramework.Core.Logging;
using GFramework.Core.Utility;

namespace GFramework.Core.Pause;

/// <summary>
/// 暂停栈管理器实现，用于管理游戏中的暂停状态。
/// 支持多组暂停、嵌套暂停、以及暂停状态的通知机制。
/// </summary>
public class PauseStackManager : AbstractContextUtility, IPauseStackManager, IAsyncDestroyable
{
    private readonly List<IPauseHandler> _handlers = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly ILogger _logger = LoggerFactoryResolver.Provider.CreateLogger(nameof(PauseStackManager));
    private readonly Dictionary<PauseGroup, Stack<PauseEntry>> _pauseStacks = new();
    private readonly Dictionary<Guid, PauseEntry> _tokenMap = new();
    private volatile bool _disposed;

    /// <summary>
    /// 异步销毁方法，在组件销毁时调用。
    /// </summary>
    /// <returns>表示异步操作完成的任务。</returns>
    public ValueTask DestroyAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        var destroySnapshot = TryBeginDestroy();
        if (destroySnapshot == null)
        {
            return ValueTask.CompletedTask;
        }

        NotifyDestroyedGroups(destroySnapshot.Value);
        _lock.Dispose();

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 暂停状态变化事件，当暂停状态发生改变时触发。
    /// </summary>
    public event EventHandler<PauseStateChangedEventArgs>? OnPauseStateChanged;

    /// <summary>
    /// 推入一个新的暂停请求到指定的暂停组中。
    /// </summary>
    /// <param name="reason">暂停的原因描述。</param>
    /// <param name="group">暂停组，默认为全局暂停组。</param>
    /// <returns>表示此次暂停请求的令牌。</returns>
    public PauseToken Push(string reason, PauseGroup group = PauseGroup.Global)
    {
        PauseToken token;
        bool shouldNotify = false;

        _lock.EnterWriteLock();
        try
        {
            ThrowIfDisposed();

            var wasPaused = IsPausedInternal(group);

            var entry = new PauseEntry
            {
                TokenId = Guid.NewGuid(),
                Reason = reason,
                Group = group,
                Timestamp = DateTime.UtcNow
            };

            if (!_pauseStacks.TryGetValue(group, out var stack))
            {
                stack = new Stack<PauseEntry>();
                _pauseStacks[group] = stack;
            }

            stack.Push(entry);
            _tokenMap[entry.TokenId] = entry;

            _logger.Debug($"Pause pushed: {reason} (Group: {group}, Depth: {stack.Count})");

            token = new PauseToken(entry.TokenId);

            // 状态变化检测：从未暂停 → 暂停
            shouldNotify = !wasPaused;
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        // 在锁外通知处理器，避免死锁
        if (shouldNotify)
        {
            NotifyHandlers(group, true);
        }

        return token;
    }

    /// <summary>
    /// 弹出指定的暂停请求。
    /// </summary>
    /// <param name="token">要弹出的暂停令牌。</param>
    /// <returns>如果成功弹出则返回true，否则返回false。</returns>
    public bool Pop(PauseToken token)
    {
        if (!token.IsValid)
        {
            return false;
        }

        var result = TryPopEntry(token);
        if (result.ShouldNotify)
        {
            NotifyHandlers(result.NotifyGroup, false);
        }

        return result.Found;
    }

    /// <summary>
    /// 查询指定暂停组当前是否处于暂停状态。
    /// </summary>
    /// <param name="group">要查询的暂停组，默认为全局暂停组。</param>
    /// <returns>如果该组处于暂停状态则返回true，否则返回false。</returns>
    public bool IsPaused(PauseGroup group = PauseGroup.Global)
    {
        _lock.EnterReadLock();
        try
        {
            ThrowIfDisposed();
            return IsPausedInternal(group);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// 获取指定暂停组的暂停深度（即嵌套暂停的层数）。
    /// </summary>
    /// <param name="group">要查询的暂停组，默认为全局暂停组。</param>
    /// <returns>暂停深度，0表示未暂停。</returns>
    public int GetPauseDepth(PauseGroup group = PauseGroup.Global)
    {
        _lock.EnterReadLock();
        try
        {
            ThrowIfDisposed();
            return _pauseStacks.TryGetValue(group, out var stack) ? stack.Count : 0;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// 获取指定暂停组的所有暂停原因。
    /// </summary>
    /// <param name="group">要查询的暂停组，默认为全局暂停组。</param>
    /// <returns>包含所有暂停原因的只读列表。</returns>
    public IReadOnlyList<string> GetPauseReasons(PauseGroup group = PauseGroup.Global)
    {
        _lock.EnterReadLock();
        try
        {
            ThrowIfDisposed();

            if (!_pauseStacks.TryGetValue(group, out var stack))
                return Array.Empty<string>();

            return stack.Select(e => e.Reason).ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// 创建一个暂停作用域，支持 using 语法自动管理暂停生命周期。
    /// </summary>
    /// <param name="reason">暂停的原因描述。</param>
    /// <param name="group">暂停组，默认为全局暂停组。</param>
    /// <returns>表示暂停作用域的 IDisposable 对象。</returns>
    public IDisposable PauseScope(string reason, PauseGroup group = PauseGroup.Global)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PauseStackManager),
                "Cannot use PauseStackManager after it has been destroyed");

        return new PauseScope(this, reason, group);
    }

    /// <summary>
    /// 清空指定暂停组的所有暂停请求。
    /// </summary>
    /// <param name="group">要清空的暂停组。</param>
    public void ClearGroup(PauseGroup group)
    {
        bool shouldNotify = false;

        _lock.EnterWriteLock();
        try
        {
            ThrowIfDisposed();

            if (!_pauseStacks.TryGetValue(group, out var stack))
                return;

            var wasPaused = stack.Count > 0;

            // 移除所有令牌
            foreach (var entry in stack)
            {
                _tokenMap.Remove(entry.TokenId);
            }

            stack.Clear();

            _logger.Warn($"Cleared all pauses for group: {group}");

            // 状态变化检测
            shouldNotify = wasPaused;
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        // 在锁外通知处理器，避免死锁
        if (shouldNotify)
        {
            NotifyHandlers(group, false);
        }
    }

    /// <summary>
    /// 清空所有暂停组的所有暂停请求。
    /// </summary>
    public void ClearAll()
    {
        List<PauseGroup> pausedGroups;

        _lock.EnterWriteLock();
        try
        {
            ThrowIfDisposed();

            pausedGroups = _pauseStacks
                .Where(kvp => kvp.Value.Count > 0)
                .Select(kvp => kvp.Key)
                .ToList();

            _pauseStacks.Clear();
            _tokenMap.Clear();

            _logger.Warn("Cleared all pauses for all groups");
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        // 在锁外通知所有之前暂停的组，避免死锁
        foreach (var group in pausedGroups)
        {
            NotifyHandlers(group, false);
        }
    }

    /// <summary>
    /// 注册一个暂停处理器，用于监听暂停状态的变化。
    /// </summary>
    /// <param name="handler">要注册的暂停处理器。</param>
    public void RegisterHandler(IPauseHandler handler)
    {
        _lock.EnterWriteLock();
        try
        {
            ThrowIfDisposed();

            if (!_handlers.Contains(handler))
            {
                _handlers.Add(handler);
                _logger.Debug($"Registered pause handler: {handler.GetType().Name}");
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 注销一个已注册的暂停处理器。
    /// </summary>
    /// <param name="handler">要注销的暂停处理器。</param>
    public void UnregisterHandler(IPauseHandler handler)
    {
        _lock.EnterWriteLock();
        try
        {
            ThrowIfDisposed();

            if (_handlers.Remove(handler))
            {
                _logger.Debug($"Unregistered pause handler: {handler.GetType().Name}");
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 检查是否已销毁，如果已销毁则抛出异常
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PauseStackManager),
                "Cannot use PauseStackManager after it has been destroyed");
        }
    }

    /// <summary>
    /// 采集销毁所需的快照并清空内部状态。
    /// </summary>
    /// <returns>
    /// 成功进入销毁阶段时返回销毁快照；如果其他线程已先完成销毁，则返回 <see langword="null" />。
    /// </returns>
    /// <remarks>
    /// 该方法只负责锁内状态迁移，把外部回调与事件派发留到锁外执行，
    /// 以避免在生命周期结束阶段持锁调用用户代码。
    /// </remarks>
    private DestroySnapshot? TryBeginDestroy()
    {
        _lock.EnterWriteLock();
        try
        {
            if (_disposed)
            {
                return null;
            }

            _disposed = true;

            var pausedGroups = CollectPausedGroups();
            var handlersSnapshot = CreateHandlerSnapshot();

            _pauseStacks.Clear();
            _tokenMap.Clear();
            _handlers.Clear();

            _logger.Debug("PauseStackManager destroyed");

            return new DestroySnapshot(pausedGroups, handlersSnapshot);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 在销毁后向所有先前处于暂停状态的分组补发恢复通知。
    /// </summary>
    /// <param name="destroySnapshot">销毁阶段采集的分组与处理器快照。</param>
    private void NotifyDestroyedGroups(DestroySnapshot destroySnapshot)
    {
        foreach (var group in destroySnapshot.PausedGroups)
        {
            _logger.Debug($"Notifying handlers of destruction: Group={group}, IsPaused=false");

            NotifyHandlersSnapshot(group, false, destroySnapshot.HandlersSnapshot, isDestroying: true);
            RaiseDestroyStateChanged(group);
        }
    }

    /// <summary>
    /// 在锁内执行令牌移除，并返回锁外通知所需的信息。
    /// </summary>
    /// <param name="token">要移除的暂停令牌。</param>
    /// <returns>包含本次弹出结果和后续通知决策的快照。</returns>
    /// <remarks>
    /// Pop 支持移除非栈顶令牌，因此这里会先临时转移栈元素，再恢复原有顺序，
    /// 只在最后一个暂停请求被移除时触发恢复通知。
    /// </remarks>
    private PopResult TryPopEntry(PauseToken token)
    {
        _lock.EnterWriteLock();
        try
        {
            ThrowIfDisposed();

            if (!_tokenMap.TryGetValue(token.Id, out var entry))
            {
                _logger.Warn($"Attempted to pop invalid/expired token: {token.Id}");
                return PopResult.NotFound;
            }

            var stack = _pauseStacks[entry.Group];
            var wasPaused = stack.Count > 0;
            var found = RemoveEntryFromStack(stack, token.Id);
            if (!found)
            {
                return PopResult.NotFound;
            }

            _tokenMap.Remove(token.Id);
            _logger.Debug($"Pause popped: {entry.Reason} (Group: {entry.Group}, Remaining: {stack.Count})");

            return new PopResult(true, wasPaused && stack.Count == 0, entry.Group);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 从指定暂停栈中移除目标令牌，并保持其他暂停请求的原始顺序。
    /// </summary>
    /// <param name="stack">要修改的暂停栈。</param>
    /// <param name="tokenId">目标令牌标识。</param>
    /// <returns>如果找到了目标令牌则返回 <see langword="true" />。</returns>
    private static bool RemoveEntryFromStack(Stack<PauseEntry> stack, Guid tokenId)
    {
        var tempStack = new Stack<PauseEntry>();
        var found = false;

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current.TokenId == tokenId)
            {
                found = true;
                break;
            }

            tempStack.Push(current);
        }

        while (tempStack.Count > 0)
        {
            stack.Push(tempStack.Pop());
        }

        return found;
    }

    /// <summary>
    /// 收集当前仍处于暂停状态的分组列表。
    /// </summary>
    /// <returns>包含所有暂停中的分组的数组。</returns>
    private PauseGroup[] CollectPausedGroups()
    {
        return _pauseStacks
            .Where(kvp => kvp.Value.Count > 0)
            .Select(kvp => kvp.Key)
            .ToArray();
    }

    /// <summary>
    /// 按优先级创建处理器快照，确保锁外通知仍保持确定性顺序。
    /// </summary>
    /// <returns>已按优先级排序的处理器快照。</returns>
    private IPauseHandler[] CreateHandlerSnapshot()
    {
        return _handlers
            .OrderBy(handler => handler.Priority)
            .ToArray();
    }

    /// <summary>
    /// 统一使用给定的处理器快照派发暂停状态变化通知。
    /// </summary>
    /// <param name="group">发生状态变化的暂停组。</param>
    /// <param name="isPaused">新的暂停状态。</param>
    /// <param name="handlersSnapshot">要通知的处理器快照。</param>
    /// <param name="isDestroying">是否处于销毁补发路径。</param>
    private void NotifyHandlersSnapshot(
        PauseGroup group,
        bool isPaused,
        IReadOnlyList<IPauseHandler> handlersSnapshot,
        bool isDestroying)
    {
        foreach (var handler in handlersSnapshot)
        {
            try
            {
                handler.OnPauseStateChanged(group, isPaused);
            }
            catch (Exception ex)
            {
                var message = isDestroying
                    ? $"Handler {handler.GetType().Name} failed during destruction"
                    : $"Handler {handler.GetType().Name} failed";
                _logger.Error(message, ex);
            }
        }
    }

    /// <summary>
    /// 在销毁路径中独立保护事件通知，避免订阅方异常中断其他分组的恢复信号。
    /// </summary>
    /// <param name="group">需要补发恢复事件的暂停组。</param>
    private void RaiseDestroyStateChanged(PauseGroup group)
    {
        try
        {
            RaisePauseStateChanged(group, false);
        }
        catch (Exception ex)
        {
            _logger.Error($"Event subscriber failed during destruction for group {group}", ex);
        }
    }

    /// <summary>
    /// 内部查询暂停状态的方法，不加锁。
    /// </summary>
    /// <param name="group">要查询的暂停组。</param>
    /// <returns>如果该组处于暂停状态则返回true，否则返回false。</returns>
    private bool IsPausedInternal(PauseGroup group)
    {
        return _pauseStacks.TryGetValue(group, out var stack) && stack.Count > 0;
    }

    /// <summary>
    /// 通知所有已注册的处理器和事件订阅者暂停状态的变化。
    /// </summary>
    /// <param name="group">发生状态变化的暂停组。</param>
    /// <param name="isPaused">新的暂停状态。</param>
    private void NotifyHandlers(PauseGroup group, bool isPaused)
    {
        _logger.Debug($"Notifying handlers: Group={group}, IsPaused={isPaused}");

        // 在锁内获取处理器快照，避免并发修改异常
        IPauseHandler[] handlersSnapshot;
        _lock.EnterReadLock();
        try
        {
            handlersSnapshot = CreateHandlerSnapshot();
        }
        finally
        {
            _lock.ExitReadLock();
        }

        // 在锁外遍历快照并通知处理器
        NotifyHandlersSnapshot(group, isPaused, handlersSnapshot, isDestroying: false);

        // 触发事件
        RaisePauseStateChanged(group, isPaused);
    }

    /// <summary>
    ///     以标准事件模式发布暂停状态变化事件。
    ///     所有状态变更路径都通过该方法创建统一的事件参数，避免不同调用点出现不一致的载荷。
    /// </summary>
    /// <param name="group">发生状态变化的暂停组。</param>
    /// <param name="isPaused">暂停组变化后的新状态。</param>
    private void RaisePauseStateChanged(PauseGroup group, bool isPaused)
    {
        OnPauseStateChanged?.Invoke(this, new PauseStateChangedEventArgs(group, isPaused));
    }

    /// <summary>
    /// 初始化方法，在组件初始化时调用。
    /// </summary>
    protected override void OnInit()
    {
    }

    /// <summary>
    /// 锁内采集的销毁快照，供锁外补发恢复通知使用。
    /// </summary>
    /// <param name="PausedGroups">销毁前仍处于暂停状态的分组。</param>
    /// <param name="HandlersSnapshot">按优先级排序后的处理器快照。</param>
    private readonly record struct DestroySnapshot(PauseGroup[] PausedGroups, IPauseHandler[] HandlersSnapshot);

    /// <summary>
    /// Pop 操作的锁内结果快照。
    /// </summary>
    /// <param name="Found">是否成功移除了目标令牌。</param>
    /// <param name="ShouldNotify">是否需要在锁外发出恢复通知。</param>
    /// <param name="NotifyGroup">需要通知的暂停组。</param>
    private readonly record struct PopResult(bool Found, bool ShouldNotify, PauseGroup NotifyGroup)
    {
        /// <summary>
        /// 表示未找到目标令牌时的默认结果。
        /// </summary>
        public static PopResult NotFound { get; } = new(false, false, PauseGroup.Global);
    }
}
