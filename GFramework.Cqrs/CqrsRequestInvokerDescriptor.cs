using System.Reflection;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs;

/// <summary>
///     描述单个 request/response 类型对在运行时分发时需要复用的元数据。
/// </summary>
/// <param name="handlerType">当前请求处理器在容器中的服务类型。</param>
/// <param name="invokerMethod">
///     执行单个请求处理器的开放静态方法。
///     dispatcher 会在首次创建 request binding 时，把该方法绑定成内部使用的强类型委托。
/// </param>
/// <remarks>
///     dispatcher 会继续自行构造 pipeline behavior 服务类型并负责上下文注入；
///     该描述符只前移请求处理器服务类型与直接调用方法元数据。
/// </remarks>
public sealed class CqrsRequestInvokerDescriptor(
    Type handlerType,
    MethodInfo invokerMethod)
{
    /// <summary>
    ///     获取请求处理器在容器中的服务类型。
    /// </summary>
    public Type HandlerType { get; } = handlerType ?? throw new ArgumentNullException(nameof(handlerType));

    /// <summary>
    ///     获取执行请求处理器的开放静态方法。
    /// </summary>
    public MethodInfo InvokerMethod { get; } = invokerMethod ?? throw new ArgumentNullException(nameof(invokerMethod));
}
