namespace GFramework.Core.SourceGenerators.Abstractions.Rule;

/// <summary>
///     标记字段需要自动注入系统集合。
/// </summary>
/// <remarks>
///     Source Generator 会为标记字段生成从当前架构上下文收集系统实例的注入代码，用于避免在组件内部重复书写
///     <c>GetSystems()</c> 一类的样板访问逻辑。
///     被标记字段应声明为可承载多个系统实例的类型，例如 <c>IEnumerable&lt;ISystem&gt;</c> 或兼容集合接口。
/// </remarks>
/// <example>
/// <code>
/// public partial class CombatPanel : IContextAware
/// {
///     [GetSystems]
///     private IEnumerable&lt;ISystem&gt; _systems = Array.Empty&lt;ISystem&gt;();
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class GetSystemsAttribute : Attribute
{
}
