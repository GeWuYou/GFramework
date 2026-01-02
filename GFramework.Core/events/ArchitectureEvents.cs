namespace GFramework.Core.events;

/// <summary>
///     架构事件定义类，包含应用程序架构生命周期相关的事件结构体
/// </summary>
public static class ArchitectureEvents
{
    /// <summary>
    ///     架构生命周期准备就绪事件
    ///     当架构完成初始化并准备就绪时触发此事件
    /// </summary>
    public readonly struct ArchitectureLifecycleReadyEvent;

    /// <summary>
    ///     架构销毁中事件
    ///     当架构开始销毁过程时触发此事件，表示系统正在关闭
    /// </summary>
    public readonly struct ArchitectureDestroyingEvent;

    /// <summary>
    ///     架构已销毁事件
    ///     当架构完全销毁完成后触发此事件，表示系统已关闭
    /// </summary>
    public readonly struct ArchitectureDestroyedEvent;

    /// <summary>
    ///     架构初始化失败事件
    ///     当架构初始化过程中发生错误时触发此事件
    /// </summary>
    public readonly struct ArchitectureFailedInitializationEvent;
}