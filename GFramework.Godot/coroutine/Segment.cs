namespace GFramework.Godot.coroutine;

/// <summary>
///     定义协程执行的不同时间段枚举
/// </summary>
public enum Segment
{
    /// <summary>
    ///     普通处理阶段，在每一帧的常规处理过程中执行（默认用于游戏级协程）
    /// </summary>
    Process,

    /// <summary>
    ///     在暂停状态下仍然执行的处理阶段，适合暂停菜单等需要继续更新的UI级协程
    /// </summary>
    ProcessIgnorePause,

    /// <summary>
    ///     物理处理阶段，在物理更新循环中执行，通常用于需要与物理引擎同步的操作
    /// </summary>
    PhysicsProcess,

    /// <summary>
    ///     延迟处理阶段，在当前帧结束后延迟执行，通常用于需要等待当前帧完成后再执行的操作
    /// </summary>
    DeferredProcess
}
