namespace GFramework.Core.Abstractions.Cqrs;

/// <summary>
///     声明程序集内可供运行时直接调用的 CQRS 处理器注册器类型。
/// </summary>
/// <remarks>
///     该特性通常由源码生成器自动添加到消费端程序集。
///     运行时读取到该特性后，会优先实例化对应的 <see cref="ICqrsHandlerRegistry" />，
///     以常量时间获取处理器注册映射，而不是遍历程序集中的全部类型。
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class CqrsHandlerRegistryAttribute(Type registryType) : Attribute
{
    /// <summary>
    ///     获取承载 CQRS 处理器注册逻辑的注册器类型。
    /// </summary>
    public Type RegistryType { get; } = registryType ?? throw new ArgumentNullException(nameof(registryType));
}
