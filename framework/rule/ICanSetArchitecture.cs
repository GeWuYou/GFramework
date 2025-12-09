using GWFramework.framework.architecture;

namespace GWFramework.framework.rule;

/// <summary>
/// 定义一个接口，用于设置架构实例
/// </summary>
public interface ICanSetArchitecture
{
    /// <summary>
    /// 设置架构实例
    /// </summary>
    /// <param name="architecture">要设置的架构实例</param>
    void SetArchitecture(IArchitecture architecture);
}
