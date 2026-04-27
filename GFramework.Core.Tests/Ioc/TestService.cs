namespace GFramework.Core.Tests.Ioc;

/// <summary>
///     测试服务类，实现 IService 接口
/// </summary>
public sealed class TestService : IService
{
    /// <summary>
    ///     获取或设置优先级
    /// </summary>
    public int Priority { get; set; }
}
