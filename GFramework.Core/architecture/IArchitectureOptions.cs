namespace GFramework.Core.architecture;

/// <summary>
/// 架构可配置选项接口
/// </summary>
public interface IArchitectureOptions
{
    /// <summary>
    /// 是否严格验证阶段转换
    /// </summary>
    bool StrictPhaseValidation { get; }

    /// <summary>
    /// 是否允许在 Ready 阶段后注册系统/模型
    /// </summary>
    bool AllowLateRegistration { get; }
}