namespace GFramework.Core.Abstractions.architecture;

/// <summary>
///     定义异步初始化接口，用于需要异步初始化的组件或服务
/// </summary>
public interface IAsyncInitializable
{
    /// <summary>
    ///     异步初始化方法，用于执行组件或服务的异步初始化逻辑
    /// </summary>
    /// <returns>表示异步初始化操作的Task</returns>
    Task InitializeAsync();
}