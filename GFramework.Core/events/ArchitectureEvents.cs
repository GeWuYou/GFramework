
namespace GFramework.Core.events;

public static class ArchitectureEvents
{
    /// <summary>
    /// 架构初始化完成事件
    /// 在所有 Model / System Init 执行完毕后派发
    /// </summary>
    public readonly struct ArchitectureInitializedEvent { }
}