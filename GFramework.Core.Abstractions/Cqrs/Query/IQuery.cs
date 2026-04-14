namespace GFramework.Core.Abstractions.Cqrs.Query;

/// <summary>
///     表示一个 CQRS 查询。
///     查询用于读取数据，不应产生副作用。
/// </summary>
/// <typeparam name="TResponse">查询响应类型。</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>
{
}

/// <summary>
///     表示一个流式 CQRS 查询。
/// </summary>
/// <typeparam name="TResponse">流式响应元素类型。</typeparam>
public interface IStreamQuery<out TResponse> : IStreamRequest<TResponse>
{
}
