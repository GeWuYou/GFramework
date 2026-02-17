using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.enums;
using GFramework.Core.Abstractions.lifecycle;
using GFramework.Core.Abstractions.system;

namespace GFramework.Core.Tests.system;

/// <summary>
///     异步测试系统，实现 ISystem 和 IAsyncInitializable
/// </summary>
public sealed class AsyncTestSystem : ISystem, IAsyncInitializable
{
    private IArchitectureContext _context = null!;
    public bool Initialized { get; private set; }
    public bool DestroyCalled { get; private set; }

    public async Task InitializeAsync()
    {
        await Task.Delay(10);
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

    public void Init()
    {
        // 同步 Init 不应该被调用
        throw new InvalidOperationException("Sync Init should not be called");
    }

    public void Destroy()
    {
        DestroyCalled = true;
    }

    public void OnArchitecturePhase(ArchitecturePhase phase)
    {
    }
}