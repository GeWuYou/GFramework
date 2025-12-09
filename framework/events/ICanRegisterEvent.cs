using GFramework.framework.rule;

namespace GFramework.framework.events;

/// <summary>
/// 定义一个可以注册事件的接口，继承自IBelongToArchitecture接口。
/// 该接口用于标识那些能够注册事件的对象，通常在框架的事件系统中使用。
/// 实现此接口的类型表明它属于某个架构组件，并具备事件注册的能力。
/// </summary>
public interface ICanRegisterEvent:IBelongToArchitecture;
