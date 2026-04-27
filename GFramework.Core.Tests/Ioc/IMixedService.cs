namespace GFramework.Core.Tests.Ioc;

/// <summary>
///     混合服务接口（用于测试优先级和非优先级混合）
/// </summary>
public interface IMixedService
{
    /// <summary>
    ///     获取或设置服务名称。
    /// </summary>
    string? Name { get; set; }
}
