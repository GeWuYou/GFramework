namespace GFramework.Core.architecture;

/// <summary>
/// 函数式架构选项实现，支持匿名实现
/// </summary>
public class FunctionalArchitectureOptions(Func<bool> strictPhaseValidation, Func<bool> allowLateRegistration)
    : IArchitectureOptions
{
    /// <summary>
    /// 初始化 FunctionalArchitectureOptions 类的新实例
    /// </summary>
    /// <param name="strictPhaseValidation">用于确定是否启用严格阶段验证的函数</param>
    /// <param name="allowLateRegistration">用于确定是否允许延迟注册的函数</param>
    private readonly Func<bool> _strictPhaseValidation = strictPhaseValidation ?? throw new ArgumentNullException(nameof(strictPhaseValidation));
    private readonly Func<bool> _allowLateRegistration = allowLateRegistration ?? throw new ArgumentNullException(nameof(allowLateRegistration));

    /// <summary>
    /// 获取一个值，该值指示是否启用严格阶段验证
    /// </summary>
    public bool StrictPhaseValidation => _strictPhaseValidation();

    /// <summary>
    /// 获取一个值，该值指示是否允许延迟注册
    /// </summary>
    public bool AllowLateRegistration => _allowLateRegistration();
}
