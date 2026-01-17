namespace GFramework.Core.Abstractions.lifecycle;

/// <summary>
///     可销毁接口，为需要资源清理的组件提供标准销毁能力
/// </summary>
public interface IDisposable
{
    /// <summary>
    ///     销毁组件并释放资源
    /// </summary>
    void Destroy();
}