namespace GFramework.Core.architecture;

/// <summary>
///     定义架构阶段感知接口，用于在架构的不同阶段执行相应的逻辑
/// </summary>
public interface IArchitecturePhaseAware
{
    /// <summary>
    ///     当架构进入指定阶段时触发的回调方法
    /// </summary>
    /// <param name="phase">架构阶段枚举值，表示当前所处的架构阶段</param>
    void OnArchitecturePhase(ArchitecturePhase phase);
}