namespace GFramework.Core.architecture;

/// <summary>
/// 架构选项配置类，用于定义架构行为的相关配置选项
/// </summary>
public sealed class ArchitectureOptions(
    bool strictPhaseValidation = true,
    bool allowLateRegistration = false
)
{
    /// <summary>
    /// 严格阶段验证开关，当设置为true时启用严格的阶段验证机制
    /// </summary>
    public bool StrictPhaseValidation = strictPhaseValidation;

    /// <summary>
    /// 允许延迟注册开关，当设置为true时允许在初始化完成后进行组件注册
    /// </summary>
    public bool AllowLateRegistration = allowLateRegistration;
}