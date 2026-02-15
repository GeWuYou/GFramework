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
    ///     所有阶段，Handler将在BeforeChange和AfterChange阶段都执行
    /// </summary>
    All = BeforeChange | AfterChange
}