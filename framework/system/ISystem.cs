using GFramework.framework.events;
using GFramework.framework.model;
using GFramework.framework.rule;
using GFramework.framework.utility;

namespace GFramework.framework.system;

/// <summary>
/// 系统接口，定义了系统的基本行为和功能
/// 该接口继承了多个框架相关的接口，提供了系统初始化能力
/// </summary>
public interface ISystem : ICanSetArchitecture, ICanGetModel, ICanGetUtility,
    ICanRegisterEvent, ICanSendEvent, ICanGetSystem
{
    /// <summary>
    /// 初始化系统
    /// 在系统被创建后调用，用于执行系统的初始化逻辑
    /// </summary>
    void Init();
}
