using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Abstractions.Model;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     为 <see cref="ArchitectureContextTests" /> 提供的测试模型桩。
/// </summary>
public sealed class TestModelV2 : IModel
{
    private IArchitectureContext _context = null!;

    /// <summary>
    ///     获取或设置测试用标识。
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    ///     关联当前模型所属的架构上下文。
    /// </summary>
    /// <param name="context">要保存的架构上下文。</param>
    public void SetContext(IArchitectureContext context)
    {
        _context = context;
    }

    /// <summary>
    ///     获取当前模型已绑定的架构上下文。
    /// </summary>
    /// <returns>测试期间保存的架构上下文。</returns>
    public IArchitectureContext GetContext()
    {
        return _context;
    }

    /// <summary>
    ///     初始化测试模型。
    /// </summary>
    public void Initialize()
    {
    }

    /// <summary>
    ///     接收架构阶段切换通知。
    /// </summary>
    /// <param name="phase">当前架构阶段。</param>
    public void OnArchitecturePhase(ArchitecturePhase phase)
    {
    }

    /// <summary>
    ///     销毁测试模型。
    /// </summary>
    public void Destroy()
    {
    }
}
