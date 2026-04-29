using GFramework.Core.Abstractions.Architectures;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     描述单次分发阶段记录下来的上下文与实例身份。
/// </summary>
/// <param name="DispatchId">触发本次记录的请求标识。</param>
/// <param name="InstanceId">当次 handler 或 behavior 实例编号。</param>
/// <param name="Context">当次分发注入的架构上下文。</param>
internal sealed record DispatcherPipelineContextSnapshot(
    string DispatchId,
    int InstanceId,
    IArchitectureContext Context);
