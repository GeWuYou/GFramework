using Godot;

namespace GFramework.Godot.extensions.signal;

/// <summary>
///     为Godot Node节点提供信号相关的流畅API扩展方法
/// </summary>
public static class SignalFluentExtensions
{
    /// <summary>
    ///     为指定节点创建信号构建器
    /// </summary>
    /// <param name="object">要创建信号构建器的Godot对象</param>
    /// <param name="signal">信号名称</param>
    /// <returns>信号构建器实例</returns>
    public static SignalBuilder Signal(
        this GodotObject @object,
        StringName signal)
    {
        return new SignalBuilder(@object, signal);
    }
}