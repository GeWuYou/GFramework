using GFramework.Core.Abstractions.Bases;

namespace GFramework.Core.Tests.Ioc;

/// <summary>
///     优先级服务接口
/// </summary>
public interface IPrioritizedService : IPrioritized
{
    string? Name { get; set; }
}
