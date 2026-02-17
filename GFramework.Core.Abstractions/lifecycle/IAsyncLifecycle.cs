namespace GFramework.Core.Abstractions.lifecycle;

/// <summary>
///     定义异步生命周期接口，组合了异步初始化和异步销毁
/// </summary>
public interface IAsyncLifecycle : IAsyncInitializable, IAsyncDestroyable
{
}