using GFramework.framework.rule;

namespace GFramework.framework.command;

/// <summary>
/// 定义一个可以发送命令的接口，继承自IBelongToArchitecture接口。
/// 该接口用于标识那些具备发送命令能力的架构组件。
/// </summary>
public interface ICanSendCommand : IBelongToArchitecture;
