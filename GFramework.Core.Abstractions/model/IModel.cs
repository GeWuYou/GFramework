using GFramework.Core.Abstractions.rule;

namespace GFramework.Core.Abstractions.model;

/// <summary>
///     模型接口，定义了模型的基本行为和功能
/// </summary>
public interface IModel : IContextAware
{
    /// <summary>
    ///     初始化模型
    /// </summary>
    void Init();
}