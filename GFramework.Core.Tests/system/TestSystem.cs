using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.enums;
using GFramework.Core.Abstractions.system;

namespace GFramework.Core.Tests.system;

/// <summary>
///     测试系统类，实现了ISystem接口
/// </summary>
public sealed class TestSystem : ISystem
{
    /// <summary>
    ///     架构上下文对象
    /// </summary>
    private IArchitectureContext _context = null!;

    /// <summary>
    ///     获取系统是否已初始化的状态
    /// </summary>
    public bool Initialized { get; private set; }

    /// <summary>
    ///     获取系统是否已销毁的状态
    /// </summary>
    public bool DestroyCalled { get; private set; }

    /// <summary>
    ///     设置架构上下文
    /// </summary>
    /// <param name="context">架构上下文对象</param>
    public void SetContext(IArchitectureContext context)
    {
        _context = context;
    }

    /// <summary>
    ///     获取架构上下文
    /// </summary>
    /// <returns>架构上下文对象</returns>
    public IArchitectureContext GetContext()
    {
        return _context;
    }

    /// <summary>
    ///     初始化系统
    /// </summary>
    public void Init()
    {
        Initialized = true;
    }

    /// <summary>
    ///     销毁系统
    /// </summary>
    public void Destroy()
    {
        DestroyCalled = true;
    }

    public void OnArchitecturePhase(ArchitecturePhase phase)
    {
    }
}