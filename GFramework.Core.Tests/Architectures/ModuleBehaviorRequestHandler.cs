using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     处理测试请求的处理器。
/// </summary>
public sealed class ModuleBehaviorRequestHandler : IRequestHandler<ModuleBehaviorRequest, string>
{
    /// <summary>
    ///     返回固定结果，便于聚焦验证管道行为是否执行。
    /// </summary>
    /// <param name="request">请求实例。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>固定响应内容。</returns>
    public ValueTask<string> Handle(ModuleBehaviorRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult("handled");
    }
}
