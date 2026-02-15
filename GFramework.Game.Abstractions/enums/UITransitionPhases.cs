namespace GFramework.Game.Abstractions.enums;

/// <summary>
///     UI切换阶段枚举，定义UI切换过程中的不同阶段
/// </summary>
[Flags]
public enum UiTransitionPhases
{
    /// <summary>
    ///     UI切换前阶段，在此阶段执行的Handler可以阻塞UI切换流程
    ///     适用于：淡入淡出动画、用户确认对话框、数据预加载等需要等待完成的操作
    /// </summary>
    BeforeChange = 1,

    /// <summary>
    ///     UI切换后阶段，在此阶段执行的Handler不阻塞UI切换流程
    ///     适用于：播放音效、日志记录、统计数据收集等后台操作
    /// </summary>
    AfterChange = 2,

    /// <summary>
    ///     中间件阶段，支持包裹整个变更过程的逻辑（阻塞执行）
    ///     适用于：性能监控、事务管理、权限验证、日志记录开始/结束等需要控制流程的操作
    ///     Around 处理器在变更前后都会执行，可以决定是否继续执行后续逻辑
    /// </summary>
    Around = 4,

    /// <summary>
    ///     所有阶段，Handler将在BeforeChange、AfterChange和Around阶段都执行
    /// </summary>
    All = BeforeChange | AfterChange | Around
}