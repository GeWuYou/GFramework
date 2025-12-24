
using GFramework.Core.events;
using GFramework.Core.ioc;

namespace GFramework.Core.architecture;

public class ArchitectureServices: IArchitectureServices
{
    public IIocContainer Container { get; } = new IocContainer();
    public ITypeEventSystem TypeEventSystem { get; } = new TypeEventSystem();
}