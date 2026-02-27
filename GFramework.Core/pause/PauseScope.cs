using GFramework.Core.Abstractions.pause;

namespace GFramework.Core.pause;

/// <summary>
/// 暂停作用域，支持 using 语法自动管理暂停生命周期
/// </summary>
public class PauseScope : IDisposable
{
    private readonly IPauseStackManager _manager;
    private readonly PauseToken _token;
    private bool _disposed;

    /// <summary>
    /// 创建暂停作用域
    /// </summary>
    /// <param name="manager">暂停栈管理器</param>
    /// <param name="reason">暂停原因</param>
    /// <param name="group">暂停组</param>
    public PauseScope(IPauseStackManager manager, string reason, PauseGroup group = PauseGroup.Global)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        _token = _manager.Push(reason, group);
    }

    /// <summary>
    /// 释放作用域，自动恢复暂停
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    /// <param name="disposing">是否正在显式释放</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _manager.Pop(_token);
        }

        _disposed = true;
    }
}