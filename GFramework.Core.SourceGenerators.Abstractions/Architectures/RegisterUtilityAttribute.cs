namespace GFramework.Core.SourceGenerators.Abstractions.Architectures;

/// <summary>
///     声明架构模块需要自动注册的工具类型。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class RegisterUtilityAttribute(Type utilityType) : Attribute
{
    /// <summary>
    ///     获取要注册的工具类型。
    /// </summary>
    public Type UtilityType { get; } = utilityType;
}
