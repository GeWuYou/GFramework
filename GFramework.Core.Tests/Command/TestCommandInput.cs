using GFramework.Cqrs.Abstractions.Cqrs.Command;

namespace GFramework.Core.Tests.Command;

/// <summary>
///     表示 <see cref="CommandExecutorTests" /> 使用的测试命令输入。
/// </summary>
public sealed class TestCommandInput : ICommandInput
{
    /// <summary>
    ///     获取或设置测试值。
    /// </summary>
    public int Value { get; init; }
}
