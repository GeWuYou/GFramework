using GFramework.Core.rule;

namespace GFramework.Core.system;

/// <summary>
///     定义一个接口，表示可以获取系统的对象。
///     该接口继承自IBelongToArchitecture接口，用于标识那些属于系统架构并能够获取系统实例的类型。
/// </summary>
public interface ICanGetSystem : IBelongToArchitecture;