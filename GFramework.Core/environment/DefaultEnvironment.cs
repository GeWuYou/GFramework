namespace GFramework.Core.environment;

/// <summary>
/// 默认环境实现类，继承自EnvironmentBase
/// </summary>
public class DefaultEnvironment : EnvironmentBase
{
    /// <summary>
    /// 获取环境名称
    /// </summary>
    public override string Name { get; } = "Default";

    /// <summary>
    /// 初始化环境
    /// </summary>
    public override void Initialize()
    {
    }
}