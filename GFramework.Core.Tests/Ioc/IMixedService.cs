namespace GFramework.Core.Tests.Ioc;

/// <summary>
///     混合服务接口（用于测试优先级和非优先级混合）
/// </summary>
public interface IMixedService
{
    string? Name { get; set; }
}
