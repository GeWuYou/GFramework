using GFramework.Core.Abstractions.command;

namespace GFramework.Core.command;

/// <summary>
/// 空命令输入类，用于表示一个不包含任何输入参数的命令
/// </summary>
/// <remarks>
/// 该类实现了ICommandInput接口，作为命令模式中的输入参数载体
/// 通常用于不需要额外输入参数的简单命令操作
/// </remarks>
public sealed class EmptyCommandInput : ICommandInput;
