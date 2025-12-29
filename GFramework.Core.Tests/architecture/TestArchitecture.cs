using GFramework.Core.architecture;
using GFramework.Core.Tests.model;
using GFramework.Core.Tests.system;

namespace GFramework.Core.Tests.architecture;

public sealed class TestArchitecture : Architecture
{
    public bool InitCalled { get; private set; }

    protected override void Init()
    {
        InitCalled = true;

        RegisterModel(new TestModel());
        RegisterSystem(new TestSystem());
    }
}