using System.Reflection;

namespace GFramework.Cqrs.Abstractions.Cqrs;

/// <summary>
///     定义 CQRS 处理器程序集接入的 runtime seam。
///     该抽象负责承接“生成注册器优先、反射扫描回退”的处理器注册流程，
///     让容器与架构启动链不再直接依赖固定的注册实现类型。
/// </summary>
public interface ICqrsHandlerRegistrar
{
    /// <summary>
    ///     扫描并注册指定程序集集合中的 CQRS 处理器。
    /// </summary>
    /// <param name="assemblies">要接入的程序集集合。</param>
    void RegisterHandlers(IEnumerable<Assembly> assemblies);
}
