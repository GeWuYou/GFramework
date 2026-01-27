using GFramework.Core.extensions;
using NUnit.Framework;

namespace GFramework.Core.Tests.extensions;

/// <summary>
///     测试ObjectExtensions扩展方法的功能
/// </summary>
[TestFixture]
public class ObjectExtensionsTests
{
    /// <summary>
    ///     验证IfType方法在类型匹配时执行指定操作
    /// </summary>
    [Test]
    public void IfType_Should_Execute_Action_When_Type_Matches()
    {
        var obj = new TestClass();
        var executed = false;

        obj.IfType<TestClass>(_ => { executed = true; });

        Assert.That(executed, Is.True);
    }

    /// <summary>
    ///     验证IfType方法在类型不匹配时不执行指定操作
    /// </summary>
    [Test]
    public void IfType_Should_Not_Execute_Action_When_Type_Does_Not_Match()
    {
        var obj = new TestClass();
        var executed = false;

        obj.IfType<string>(_ => { executed = true; });

        Assert.That(executed, Is.False);
    }

    /// <summary>
    ///     验证IfType方法在类型匹配且谓词条件为真时执行指定操作
    /// </summary>
    [Test]
    public void IfType_WithPredicate_Should_Execute_When_Type_Matches_And_Predicate_True()
    {
        var obj = new TestClass { Value = 10 };
        var executed = false;

        obj.IfType<TestClass>(x => x.Value > 5, _ => { executed = true; });

        Assert.That(executed, Is.True);
    }

    /// <summary>
    ///     验证IfType方法在谓词条件为假时不执行指定操作
    /// </summary>
    [Test]
    public void IfType_WithPredicate_Should_Not_Execute_When_Predicate_False()
    {
        var obj = new TestClass { Value = 3 };
        var executed = false;

        obj.IfType<TestClass>(x => x.Value > 5, _ => { executed = true; });

        Assert.That(executed, Is.False);
    }

    /// <summary>
    ///     验证IfType方法在类型匹配时执行匹配操作，在类型不匹配时执行不匹配操作
    /// </summary>
    [Test]
    public void IfType_WithBoth_Actions_Should_Execute_Correct_Action()
    {
        var matchCount = 0;
        var noMatchCount = 0;

        var obj = new TestClass();
        obj.IfType<TestClass>(
            _ => { matchCount++; },
            _ => { noMatchCount++; }
        );

        Assert.That(matchCount, Is.EqualTo(1));
        Assert.That(noMatchCount, Is.EqualTo(0));
    }

    /// <summary>
    ///     验证IfType方法在类型匹配时返回转换结果
    /// </summary>
    [Test]
    public void IfType_WithResult_Should_Return_Value_When_Type_Matches()
    {
        var obj = new TestClass { Name = "Test" };

        var result = obj.IfType<TestClass, string>(x => x.Name);

        Assert.That(result, Is.EqualTo("Test"));
    }

    /// <summary>
    ///     验证IfType方法在类型不匹配时返回默认值
    /// </summary>
    [Test]
    public void IfType_WithResult_Should_Return_Default_When_Type_Does_Not_Match()
    {
        var obj = new TestClass();

        var result = obj.IfType<string, string>(x => x);

        Assert.That(result, Is.Null);
    }

    /// <summary>
    ///     验证As方法在类型匹配时返回实例
    /// </summary>
    [Test]
    public void As_Should_Return_Instance_When_Type_Matches()
    {
        var obj = new TestClass();

        var result = obj.As<TestClass>();

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(obj));
    }

    /// <summary>
    ///     验证As方法在类型不匹配时返回null
    /// </summary>
    [Test]
    public void As_Should_Return_Null_When_Type_Does_Not_Match()
    {
        var obj = new TestClass();

        var result = obj.As<string>();

        Assert.That(result, Is.Null);
    }

    /// <summary>
    ///     验证Do方法执行操作并返回对象本身
    /// </summary>
    [Test]
    public void Do_Should_Execute_Action_And_Return_Object()
    {
        var obj = new TestClass { Value = 5 };

        var result = obj.Do(x => x.Value = 10);

        Assert.That(result, Is.SameAs(obj));
        Assert.That(obj.Value, Is.EqualTo(10));
    }

    /// <summary>
    ///     验证Do方法支持链式调用
    /// </summary>
    [Test]
    public void Do_Should_Support_Chaining()
    {
        var obj = new TestClass { Value = 1, Name = "A" };

        obj.Do(x => x.Value = 2)
            .Do(x => x.Name = "B");

        Assert.That(obj.Value, Is.EqualTo(2));
        Assert.That(obj.Name, Is.EqualTo("B"));
    }

    /// <summary>
    ///     验证SwitchType方法执行匹配的处理器
    /// </summary>
    [Test]
    public void SwitchType_Should_Execute_Matching_Handler()
    {
        var obj = new TestClass();
        var executed = false;

        obj.SwitchType(
            (typeof(TestClass), _ => { executed = true; }),
            (typeof(string), _ => { Assert.Fail("Should not execute"); })
        );

        Assert.That(executed, Is.True);
    }

    /// <summary>
    ///     验证SwitchType方法只执行第一个匹配的处理器
    /// </summary>
    [Test]
    public void SwitchType_Should_Execute_First_Matching_Handler()
    {
        var obj = new TestClass();
        var count = 0;

        obj.SwitchType(
            (typeof(TestClass), _ => { count++; }),
            (typeof(TestClass), _ => { count++; })
        );

        Assert.That(count, Is.EqualTo(1));
    }

    /// <summary>
    ///     验证SwitchType方法在没有匹配项时不执行任何处理器
    /// </summary>
    [Test]
    public void SwitchType_Should_Not_Execute_When_No_Match()
    {
        var obj = new TestClass();
        var executed = false;

        obj.SwitchType(
            (typeof(string), _ => { executed = true; }),
            (typeof(int), _ => { executed = true; })
        );

        Assert.That(executed, Is.False);
    }
}

/// <summary>
///     测试用的简单类
/// </summary>
public class TestClass
{
    /// <summary>
    ///     获取或设置整数值
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    ///     获取或设置名称字符串
    /// </summary>
    public string Name { get; set; } = string.Empty;
}