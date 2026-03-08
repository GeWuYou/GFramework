namespace GFramework.Core.Abstractions.controller;

/// <summary>
///     控制器标记接口，用于标识控制器组件
/// </summary>
/// <remarks>
///     <para>
///     IController 是一个标记接口（Marker Interface），不包含任何方法或属性。
///     它的作用是标识一个类是控制器，用于协调 Model、System 和 UI 之间的交互。
///     </para>
///     <para>
///     架构访问 ：控制器通常需要访问架构上下文。使用 [ContextAware] 特性
///     自动生成上下文访问能力：
///     </para>
///     <code>
///     using GFramework.SourceGenerators.Abstractions.rule;
///
///     [ContextAware]
///     public partial class PlayerController : IController
///     {
///         public void Initialize()
///         {
///             // [ContextAware] 实现 IContextAware 接口，可使用扩展方法
///             var playerModel = this.GetModel&lt;PlayerModel&gt;();
///             var gameSystem = this.GetSystem&lt;GameSystem&gt;();
///         }
///     }
///     </code>
///     <para>
///     注意：
///     </para>
///     <list type="bullet">
///         <item>必须添加 partial 关键字</item>
///         <item>[ContextAware] 特性会自动实现 IContextAware 接口</item>
///         <item>可使用 this.GetModel()、this.GetSystem() 等扩展方法访问架构</item>
///     </list>
/// </remarks>
public interface IController;