using GFramework.Core.Abstractions.ioc;
using GFramework.Core.Abstractions.properties;

namespace GFramework.Core.Abstractions.architecture;

public interface IServiceModuleManager
{
    void RegisterModule(IServiceModule module);

    void RegisterBuiltInModules(IIocContainer container, ArchitectureProperties properties);

    IReadOnlyList<IServiceModule> GetModules();

    Task InitializeAllAsync(bool asyncMode);

    ValueTask DestroyAllAsync();
}