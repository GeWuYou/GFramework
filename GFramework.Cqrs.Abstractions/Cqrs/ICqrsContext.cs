namespace GFramework.Cqrs.Abstractions.Cqrs;

/// <summary>
///     定义 CQRS runtime 在分发期间携带的最小上下文标记。
/// </summary>
/// <remarks>
///     该接口当前刻意保持为轻量 marker seam，只用于让 <see cref="ICqrsRuntime" /> 从
///     <c>GFramework.Core.Abstractions</c> 的 <c>IArchitectureContext</c> 解耦。
///     运行时实现仍可在需要时识别更具体的上下文类型，并对现有 <c>IContextAware</c> 处理器执行兼容注入。
/// </remarks>
public interface ICqrsContext
{
}
