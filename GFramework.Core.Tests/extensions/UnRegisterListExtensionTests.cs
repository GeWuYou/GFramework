using GFramework.Core.Abstractions.events;
using GFramework.Core.events;
using GFramework.Core.extensions;
using NUnit.Framework;

namespace GFramework.Core.Tests.extensions;

/// <summary>
/// 测试UnRegisterList扩展方法的功能
/// </summary>
[TestFixture]
public class UnRegisterListExtensionTests
{
    /// <summary>
    /// 在每个测试方法执行前初始化测试环境
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _unRegisterList = new TestUnRegisterList();
    }

    private TestUnRegisterList _unRegisterList = null!;

    /// <summary>
    /// 验证AddToUnregisterList方法能够正确将元素添加到列表中
    /// </summary>
    [Test]
    public void AddToUnregisterList_Should_Add_To_List()
    {
        var unRegister = new DefaultUnRegister(() => { });

        unRegister.AddToUnregisterList(_unRegisterList);

        Assert.That(_unRegisterList.UnregisterList.Count, Is.EqualTo(1));
    }

    /// <summary>
    /// 验证AddToUnregisterList方法能够正确添加多个元素到列表中
    /// </summary>
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

    /// <summary>
    /// 验证UnRegisterAll方法能够正确注销所有元素
    /// </summary>
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

        // 执行注销操作
        _unRegisterList.UnRegisterAll();

        Assert.That(invoked1, Is.True);
        Assert.That(invoked2, Is.True);
        Assert.That(invoked3, Is.True);
    }

    /// <summary>
    /// 验证UnRegisterAll方法在执行后会清空列表
    /// </summary>
    [Test]
    public void UnRegisterAll_Should_Clear_List()
    {
        var unRegister = new DefaultUnRegister(() => { });
        unRegister.AddToUnregisterList(_unRegisterList);

        // 执行注销操作
        _unRegisterList.UnRegisterAll();

        Assert.That(_unRegisterList.UnregisterList.Count, Is.EqualTo(0));
    }

    /// <summary>
    /// 验证UnRegisterAll方法在空列表情况下不会抛出异常
    /// </summary>
    [Test]
    public void UnRegisterAll_Should_Not_Throw_When_Empty()
    {
        Assert.DoesNotThrow(() => _unRegisterList.UnRegisterAll());
    }

    /// <summary>
    /// 验证UnRegisterAll方法对每个元素只调用一次注销操作
    /// </summary>
    [Test]
    public void UnRegisterAll_Should_Invoke_Once_Per_Element()
    {
        var callCount = 0;
        var unRegister = new DefaultUnRegister(() => { callCount++; });

        unRegister.AddToUnregisterList(_unRegisterList);

        // 执行注销操作
        _unRegisterList.UnRegisterAll();

        Assert.That(callCount, Is.EqualTo(1));
    }
}

/// <summary>
/// 测试用的UnRegisterList实现类，用于验证扩展方法功能
/// </summary>
public class TestUnRegisterList : IUnRegisterList
{
    /// <summary>
    /// 获取或设置注销列表
    /// </summary>
    public IList<IUnRegister> UnregisterList { get; } = new List<IUnRegister>();
}