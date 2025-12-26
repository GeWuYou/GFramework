using System;

namespace GFramework.SourceGenerators.Attributes.rule;

/// <summary>
/// 标记该类需要自动实现 IContextAware
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ContextAwareAttribute : Attribute
{
}