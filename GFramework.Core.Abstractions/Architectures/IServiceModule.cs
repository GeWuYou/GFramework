using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Lifecycle;

namespace GFramework.Core.Abstractions.Architectures;

/// <summary>
///     服务模块接口，定义了服务模块的基本契约。
///     所有服务模块必须实现此接口，以支持注册、初始化和异步销毁功能。
/// </summary>
public interface IServiceModule : IInitializable, IAsyncDestroyable
{
    /// <summary>
    ///     获取模块的唯一名称。
    /// </summary>
    string ModuleName { get; }

    /// <summary>
    ///     获取模块的优先级，数值越小优先级越高。
    ///     用于控制模块的注册和初始化顺序。
    /// </summary>
    int Priority { get; }

    /// <summary>
    ///     获取模块的启用状态。
    ///     返回 true 表示模块已启用，false 表示模块被禁用。
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    ///     注册模块提供的服务到依赖注入容器中。
    /// </summary>
    /// <param name="container">依赖注入容器实例，用于注册服务。</param>
    void Register(IIocContainer container);
}