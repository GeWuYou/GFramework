namespace GFramework.Core.Tests.Ioc;

/// <summary>
///     同时实现多个别名接口的测试服务。
/// </summary>
public sealed class AliasAwareService : IPrimaryAliasService, ISecondaryAliasService
{
}
