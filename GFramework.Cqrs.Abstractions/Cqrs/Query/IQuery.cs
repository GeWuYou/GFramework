namespace GFramework.Cqrs.Abstractions.Cqrs.Query;

/// <summary>
///     表示一个 CQRS 查询。
///     查询用于读取数据，不应产生副作用。
/// </summary>
/// <typeparam name="TResponse">查询响应类型。</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>;
