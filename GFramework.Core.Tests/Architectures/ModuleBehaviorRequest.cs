using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     用于验证管道行为注册是否生效的测试请求。
/// </summary>
public sealed class ModuleBehaviorRequest : IRequest<string>
{
}
