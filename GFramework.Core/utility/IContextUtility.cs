using GFramework.Core.rule;

namespace GFramework.Core.utility;

/// <summary>
///     上下文工具接口，继承自IUtility和IContextAware接口
///     提供具有上下文感知能力的工具功能
/// </summary>
public interface IContextUtility : IUtility, IContextAware
{
    /// <summary>
    ///     初始化上下文工具
    /// </summary>
    void Init();
}