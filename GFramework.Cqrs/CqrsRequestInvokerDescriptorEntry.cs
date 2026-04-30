namespace GFramework.Cqrs;

/// <summary>
///     描述单个 request/response 类型对与其 generated invoker 元数据之间的映射条目。
/// </summary>
/// <param name="RequestType">请求运行时类型。</param>
/// <param name="ResponseType">响应运行时类型。</param>
/// <param name="Descriptor">对应的 generated request invoker 描述符。</param>
public sealed record CqrsRequestInvokerDescriptorEntry(
    Type RequestType,
    Type ResponseType,
    CqrsRequestInvokerDescriptor Descriptor);
