using GFramework.Core.rule;

namespace GFramework.Core.events;

/// <summary>
///     定义一个可以发送事件的接口，继承自IBelongToArchitecture接口。
///     该接口用于标识那些具备发送事件能力的类型，通常作为架构中事件发送功能的基础接口。
/// </summary>
public interface ICanSendEvent : IBelongToArchitecture;