namespace GFramework.Core.Abstractions.Cqrs.Command;

/// <summary>
///     表示一个 CQRS 命令。
///     命令通常用于修改系统状态。
/// </summary>
/// <typeparam name="TResponse">命令响应类型。</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>
{
}

/// <summary>
///     表示一个无显式返回值的 CQRS 命令。
/// </summary>
public interface ICommand : ICommand<Unit>
{
}

/// <summary>
///     表示一个流式 CQRS 命令。
/// </summary>
/// <typeparam name="TResponse">流式响应元素类型。</typeparam>
public interface IStreamCommand<out TResponse> : IStreamRequest<TResponse>
{
}
