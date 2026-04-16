namespace GFramework.Core.SourceGenerators.Abstractions.Architectures;

/// <summary>
///     声明架构模块需要自动注册的模型类型。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class RegisterModelAttribute(Type modelType) : Attribute
{
    /// <summary>
    ///     获取要注册的模型类型。
    /// </summary>
    public Type ModelType { get; } = modelType;
}
