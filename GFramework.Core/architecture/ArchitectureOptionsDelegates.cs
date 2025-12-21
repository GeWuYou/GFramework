
namespace GFramework.Core.architecture;

public class ArchitectureOptionsDelegates
{
    /// <summary>
    /// 架构可配置选项委托
    /// </summary>
    /// <returns>是否严格验证阶段转换</returns>
    public delegate bool StrictPhaseValidationDelegate();

    /// <summary>
    /// 架构可配置选项委托
    /// </summary>
    /// <returns>是否允许在 Ready 阶段后注册系统/模型</returns>
    public delegate bool AllowLateRegistrationDelegate();
}