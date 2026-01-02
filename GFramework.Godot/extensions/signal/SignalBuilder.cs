using Godot;

namespace GFramework.Godot.extensions.signal;

/// <summary>
/// 信号连接构建器，用于以流畅的方式连接Godot信号
/// </summary>
/// <param name="target">要连接信号的目标节点</param>
/// <param name="signal">要连接的信号名称</param>
public sealed class SignalBuilder(Node target, StringName signal)
{
    private GodotObject.ConnectFlags? _flags;

    /// <summary>
    /// 设置连接标志
    /// </summary>
    /// <param name="flags">连接标志</param>
    /// <returns>当前构建器实例</returns>
    public SignalBuilder WithFlags(GodotObject.ConnectFlags flags)
    {
        _flags = flags;
        return this;
    }

    /// <summary>
    /// 连接信号到指定的可调用对象
    /// </summary>
    /// <param name="callable">要连接的可调用对象</param>
    /// <returns>当前构建器实例</returns>
    public SignalBuilder To(Callable callable)
    {
        // 根据是否设置了标志来决定连接方式
        if (_flags is null)
            target.Connect(signal, callable);
        else
            target.Connect(signal, callable, (uint)_flags);

        return this;
    }

    /// <summary>
    /// 显式结束，返回 Node
    /// </summary>
    /// <returns>目标节点</returns>
    public Node End() => target;
}