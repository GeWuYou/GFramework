using GFramework.Core.Abstractions.rule;

namespace GFramework.Core.Abstractions.system;

/// <summary>
///     系统接口，定义了系统的基本行为和功能
///     该接口继承了多个框架相关的接口，提供了系统初始化和销毁能力
/// </summary>
public interface ISystem : IContextAware
{
    /// <summary>
    ///     初始化系统
    ///     在系统被创建后调用，用于执行系统的初始化逻辑
    /// </summary>
    void Init();

    /// <summary>
    ///     销毁系统
    ///     在系统被销毁前调用，用于执行系统的资源清理逻辑
    /// </summary>
    void Destroy();
}