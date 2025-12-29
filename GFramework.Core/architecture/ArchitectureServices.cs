using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.events;
using GFramework.Core.Abstractions.ioc;
using GFramework.Core.events;
using GFramework.Core.ioc;

namespace GFramework.Core.architecture;

public class ArchitectureServices : IArchitectureServices
{
    private IArchitectureContext _context = null!;
    public IIocContainer Container { get; } = new IocContainer();
    public ITypeEventSystem TypeEventSystem { get; } = new TypeEventSystem();

    public void SetContext(IArchitectureContext context)
    {
        _context = context;
        Container.SetContext(context);
    }

    public IArchitectureContext GetContext()
    {
        return _context;
    }
}