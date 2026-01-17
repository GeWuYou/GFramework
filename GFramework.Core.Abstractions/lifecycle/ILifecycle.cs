namespace GFramework.Core.Abstractions.lifecycle;

/// <summary>
///     完整生命周期接口，组合了初始化和销毁能力
/// </summary>
public interface ILifecycle : IInitializable, IDisposable;