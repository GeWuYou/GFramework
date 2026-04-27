using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     用于验证额外程序集接入是否成功的测试通知。
/// </summary>
public sealed record AdditionalAssemblyNotification : INotification;
