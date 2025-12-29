using GFramework.Core.architecture;
using GFramework.Core.events;
using GFramework.Core.Tests.model;
using GFramework.Core.Tests.system;

namespace GFramework.Core.Tests.architecture;

public sealed class TestArchitecture : Architecture
{
    public bool ReadyEventFired { get; private set; }
    public bool InitCalled { get; private set; }

    protected override void Init()
    {
        InitCalled = true;

        RegisterModel(new TestModel());
        RegisterSystem(new TestSystem());
        Context.RegisterEvent<ArchitectureEvents.ArchitectureLifecycleReadyEvent>(_ => { ReadyEventFired = true; });
    }
}