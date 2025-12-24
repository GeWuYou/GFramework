using GFramework.Core.events;
using GFramework.Core.rule;
using GFramework.Core.utility;

namespace GFramework.Core.model;

/// <summary>
///     模型接口，定义了模型的基本行为和功能
/// </summary>
public interface IModel
{
    /// <summary>
    ///     初始化模型
    /// </summary>
    void Init();
}