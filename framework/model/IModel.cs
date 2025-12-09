using GWFramework.framework.events;
using GWFramework.framework.rule;
using GWFramework.framework.utility;

namespace GWFramework.framework.model;

/// <summary>
/// 模型接口，定义了模型的基本行为和功能
/// </summary>
public interface IModel : ICanSetArchitecture, ICanGetUtility, ICanSendEvent
{
    /// <summary>
    /// 初始化模型
    /// </summary>
    void Init();
}
