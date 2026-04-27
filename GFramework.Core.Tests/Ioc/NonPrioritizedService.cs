namespace GFramework.Core.Tests.Ioc;

/// <summary>
///     不实现优先级的服务
/// </summary>
public sealed class NonPrioritizedService : IMixedService
{
    /// <summary>
    ///     获取或设置服务名称
    /// </summary>
    public string? Name { get; set; }
}
