using GFramework.Core.Abstractions.events;
using GFramework.Core.events;
using GFramework.Core.extensions;
using NUnit.Framework;

namespace GFramework.Core.Tests.extensions;

[TestFixture]
public class UnRegisterListExtensionTests
{
    [SetUp]
    public void SetUp()
    {
        _unRegisterList = new TestUnRegisterList();
    }

    private TestUnRegisterList _unRegisterList = null!;

    [Test]
    public void AddToUnregisterList_Should_Add_To_List()
    {
        var unRegister = new DefaultUnRegister(() => { });

        unRegister.AddToUnregisterList(_unRegisterList);

        Assert.That(_unRegisterList.UnregisterList.Count, Is.EqualTo(1));
    }

    [Test]
    public void AddToUnregisterList_Should_Add_Multiple_Elements()
    {
        var unRegister1 = new DefaultUnRegister(() => { });
        var unRegister2 = new DefaultUnRegister(() => { });
        var unRegister3 = new DefaultUnRegister(() => { });

        unRegister1.AddToUnregisterList(_unRegisterList);
        unRegister2.AddToUnregisterList(_unRegisterList);
        unRegister3.AddToUnregisterList(_unRegisterList);

        Assert.That(_unRegisterList.UnregisterList.Count, Is.EqualTo(3));
    }

    [Test]
    public void UnRegisterAll_Should_UnRegister_All_Elements()
    {
        var invoked1 = false;
        var invoked2 = false;
        var invoked3 = false;

        var unRegister1 = new DefaultUnRegister(() => { invoked1 = true; });
        var unRegister2 = new DefaultUnRegister(() => { invoked2 = true; });
        var unRegister3 = new DefaultUnRegister(() => { invoked3 = true; });

        unRegister1.AddToUnregisterList(_unRegisterList);
        unRegister2.AddToUnregisterList(_unRegisterList);
        unRegister3.AddToUnregisterList(_unRegisterList);

        _unRegisterList.UnRegisterAll();

        Assert.That(invoked1, Is.True);
        Assert.That(invoked2, Is.True);
        Assert.That(invoked3, Is.True);
    }

    [Test]
    public void UnRegisterAll_Should_Clear_List()
    {
        var unRegister = new DefaultUnRegister(() => { });
        unRegister.AddToUnregisterList(_unRegisterList);

        _unRegisterList.UnRegisterAll();

        Assert.That(_unRegisterList.UnregisterList.Count, Is.EqualTo(0));
    }

    [Test]
    public void UnRegisterAll_Should_Not_Throw_When_Empty()
    {
        Assert.DoesNotThrow(() => _unRegisterList.UnRegisterAll());
    }

    [Test]
    public void UnRegisterAll_Should_Invoke_Once_Per_Element()
    {
        var callCount = 0;
        var unRegister = new DefaultUnRegister(() => { callCount++; });

        unRegister.AddToUnregisterList(_unRegisterList);

        _unRegisterList.UnRegisterAll();

        Assert.That(callCount, Is.EqualTo(1));
    }
}

public class TestUnRegisterList : IUnRegisterList
{
    public IList<IUnRegister> UnregisterList { get; } = new List<IUnRegister>();
}