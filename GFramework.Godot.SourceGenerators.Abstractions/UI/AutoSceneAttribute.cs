namespace GFramework.Godot.SourceGenerators.Abstractions.UI;

/// <summary>
///     标记场景根节点类型，Source Generator 会生成场景行为样板代码。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AutoSceneAttribute(string key) : Attribute
{
    /// <summary>
    ///     获取场景键。
    /// </summary>
    public string Key { get; } = key;
}
