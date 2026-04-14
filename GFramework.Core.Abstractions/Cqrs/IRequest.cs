namespace GFramework.Core.Abstractions.Cqrs;

/// <summary>
///     表示一个有响应的 CQRS 请求。
///     该接口是命令、查询以及其他请求语义的统一基接口。
/// </summary>
/// <typeparam name="TResponse">请求响应类型。</typeparam>
public interface IRequest<out TResponse>
{
}
