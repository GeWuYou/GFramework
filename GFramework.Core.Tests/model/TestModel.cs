using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.enums;
using GFramework.Core.Abstractions.model;

namespace GFramework.Core.Tests.model;

/// <summary>
/// 测试模型类，用于框架测试目的
/// </summary>
public sealed class TestModel : IModel
{
    private IArchitectureContext _context = null!;

    /// <summary>
    /// 获取模型是否已初始化的状态
    /// </summary>
    public bool Initialized { get; private set; }

    /// <summary>
    /// 初始化模型
    /// </summary>
    public void Init()
    {
        Initialized = true;
    }

    public void SetContext(IArchitectureContext context)
    {
        _context = context;
    }

    public IArchitectureContext GetContext()
    {
        return _context;
    }

    public void OnArchitecturePhase(ArchitecturePhase phase)
    {
    }
}