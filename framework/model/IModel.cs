using GFramework.framework.events;
using GFramework.framework.rule;
using GFramework.framework.utility;

namespace GFramework.framework.model;

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
