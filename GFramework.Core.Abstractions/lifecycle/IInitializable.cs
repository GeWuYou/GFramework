namespace GFramework.Core.Abstractions.lifecycle;

/// <summary>
///     可初始化接口，为需要初始化的组件提供标准初始化能力
/// </summary>
public interface IInitializable
{
    /// <summary>
    ///     初始化组件
    /// </summary>
    void Init();
}