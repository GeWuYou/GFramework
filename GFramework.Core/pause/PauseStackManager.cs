using GFramework.Core.Abstractions.lifecycle;
using GFramework.Core.Abstractions.logging;
using GFramework.Core.Abstractions.pause;
using GFramework.Core.logging;
using GFramework.Core.utility;

namespace GFramework.Core.pause;

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

    /// <summary>
    /// 异步销毁方法，在组件销毁时调用。
    /// </summary>
    /// <returns>表示异步操作完成的任务。</returns>
    public ValueTask DestroyAsync()
    {
        _lock.Dispose();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 暂停状态变化事件，当暂停状态发生改变时触发。
    /// </summary>
    public event Action<PauseGroup, bool>? OnPauseStateChanged;

    /// <summary>
    /// 推入一个新的暂停请求到指定的暂停组中。
    /// </summary>
    /// <param name="reason">暂停的原因描述。</param>
    /// <param name="group">暂停组，默认为全局暂停组。</param>
    /// <returns>表示此次暂停请求的令牌。</returns>
    public PauseToken Push(string reason, PauseGroup group = PauseGroup.Global)
    {
        _lock.EnterWriteLock();
        try
        {
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

            // 状态变化检测：从未暂停 → 暂停
            if (!wasPaused)
            {
                NotifyHandlers(group, true);
            }

            return new PauseToken(entry.TokenId);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 弹出指定的暂停请求。
    /// </summary>
    /// <param name="token">要弹出的暂停令牌。</param>
    /// <returns>如果成功弹出则返回true，否则返回false。</returns>
    public bool Pop(PauseToken token)
    {
        if (!token.IsValid)
            return false;

        _lock.EnterWriteLock();
        try
        {
            if (!_tokenMap.TryGetValue(token.Id, out var entry))
            {
                _logger.Warn($"Attempted to pop invalid/expired token: {token.Id}");
                return false;
            }

            var group = entry.Group;
            var stack = _pauseStacks[group];
            var wasPaused = stack.Count > 0;

            // 从栈中移除
            var tempStack = new Stack<PauseEntry>();
            bool found = false;

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current.TokenId == token.Id)
                {
                    found = true;
                    break;
                }

                tempStack.Push(current);
            }

            // 恢复栈结构
            while (tempStack.Count > 0)
            {
                stack.Push(tempStack.Pop());
            }

            if (found)
            {
                _tokenMap.Remove(token.Id);
                _logger.Debug($"Pause popped: {entry.Reason} (Group: {group}, Remaining: {stack.Count})");

                // 状态变化检测：从暂停 → 未暂停
                if (wasPaused && stack.Count == 0)
                {
                    NotifyHandlers(group, false);
                }
            }

            return found;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
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
        return new PauseScope(this, reason, group);
    }

    /// <summary>
    /// 清空指定暂停组的所有暂停请求。
    /// </summary>
    /// <param name="group">要清空的暂停组。</param>
    public void ClearGroup(PauseGroup group)
    {
        _lock.EnterWriteLock();
        try
        {
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
            if (wasPaused)
            {
                NotifyHandlers(group, false);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 清空所有暂停组的所有暂停请求。
    /// </summary>
    public void ClearAll()
    {
        _lock.EnterWriteLock();
        try
        {
            var pausedGroups = _pauseStacks
                .Where(kvp => kvp.Value.Count > 0)
                .Select(kvp => kvp.Key)
                .ToList();

            _pauseStacks.Clear();
            _tokenMap.Clear();

            _logger.Warn("Cleared all pauses for all groups");

            // 通知所有之前暂停的组
            foreach (var group in pausedGroups)
            {
                NotifyHandlers(group, false);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
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

        // 按优先级排序后通知
        foreach (var handler in _handlers.OrderBy(h => h.Priority))
        {
            try
            {
                handler.OnPauseStateChanged(group, isPaused);
            }
            catch (Exception ex)
            {
                _logger.Error($"Handler {handler.GetType().Name} failed", ex);
            }
        }

        // 触发事件
        OnPauseStateChanged?.Invoke(group, isPaused);
    }

    /// <summary>
    /// 初始化方法，在组件初始化时调用。
    /// </summary>
    protected override void OnInit()
    {
    }
}