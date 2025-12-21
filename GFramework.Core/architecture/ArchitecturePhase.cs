namespace GFramework.Core.architecture;

/// <summary>
/// 架构阶段枚举，定义了系统架构初始化和运行过程中的各个关键阶段
/// </summary>
/// <remarks>
/// 该枚举用于标记和控制系统架构组件的生命周期，确保在正确的时机执行相应的初始化和处理逻辑。
/// 各个阶段按照时间顺序排列，从创建到准备就绪的完整流程。
/// </remarks>
public enum ArchitecturePhase
{
    
    /// <summary>
    /// 对象创建阶段，对应 new T() 操作完成后的状态
    /// </summary>
    Created,
    
    /// <summary>
    /// 初始化之前阶段，在 Init() 方法调用之前的状态
    /// </summary>
    BeforeInit,
    
    /// <summary>
    /// 初始化之后阶段，在 Init() 方法调用之后的状态
    /// </summary>
    AfterInit,
    
    /// <summary>
    /// 模型初始化之前阶段
    /// </summary>
    BeforeModelInit,
    
    /// <summary>
    /// 模型初始化之后阶段
    /// </summary>
    AfterModelInit,
    
    /// <summary>
    /// 系统初始化之前阶段
    /// </summary>
    BeforeSystemInit,
    
    /// <summary>
    /// 系统初始化之后阶段
    /// </summary>
    AfterSystemInit,
    
    /// <summary>
    /// 就绪阶段，完成冻结和事件处理后的最终状态
    /// </summary>
    Ready,
    /// <summary>
    /// 正在销毁中 暂时不使用
    /// </summary>
    Destroying,
    /// <summary>
    /// 已销毁 暂时不使用
    /// </summary>
    Destroyed

}
