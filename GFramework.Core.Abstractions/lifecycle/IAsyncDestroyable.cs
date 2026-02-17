namespace GFramework.Core.Abstractions.lifecycle;

/// <summary>
///     定义异步销毁接口，用于需要异步清理资源的组件
/// </summary>
public interface IAsyncDestroyable
{
    /// <summary>
    ///     异步销毁方法，在组件关闭时调用
    /// </summary>
    /// <returns>表示异步销毁操作的任务</returns>
    ValueTask DestroyAsync();
}