using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Command;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     为 <see cref="ArchitectureContextTests" /> 提供的测试命令桩。
/// </summary>
public sealed class TestCommandV2 : ICommand
{
    private IArchitectureContext _context = null!;

    /// <summary>
    ///     获取命令是否已经执行。
    /// </summary>
    public bool Executed { get; private set; }

    /// <summary>
    ///     执行测试命令，并记录执行状态。
    /// </summary>
    public void Execute()
    {
        Executed = true;
    }

    /// <summary>
    ///     关联当前命令所属的架构上下文。
    /// </summary>
    /// <param name="context">要保存的架构上下文。</param>
    public void SetContext(IArchitectureContext context)
    {
        _context = context;
    }

    /// <summary>
    ///     获取当前命令已绑定的架构上下文。
    /// </summary>
    /// <returns>测试期间保存的架构上下文。</returns>
    public IArchitectureContext GetContext()
    {
        return _context;
    }
}
