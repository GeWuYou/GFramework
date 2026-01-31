using GFramework.Core.extensions;
using GFramework.Core.functional.pipe;
using NUnit.Framework;

namespace GFramework.Core.Tests.functional.pipe;

/// <summary>
/// PipeExtensions扩展方法测试类，用于验证函数式编程扩展方法的正确性
/// 包括管道操作、函数组合、柯里化、模式匹配等多种函数式编程功能的测试
/// </summary>
[TestFixture]
public class PipeExtensionsTests
{
    #region Pipe Tests
    
    /// <summary>
    /// 测试Pipe方法 - 验证值能够正确传递给函数并返回结果
    /// </summary>
    [Test]
    public void Pipe_Should_Execute_Function_And_Return_Result()
    {
        // Arrange
        var value = 5;
        
        // Act
        var result = value.Pipe(x => x * 2);
        
        // Assert
        Assert.That(result, Is.EqualTo(10));
    }

    #endregion

    #region Then Tests
    
    /// <summary>
    /// 测试Then方法 - 验证两个函数能够正确组合执行
    /// </summary>
    [Test]
    public void Then_Should_Compose_Two_Functions()
    {
        // Arrange
        Func<int, int> addTwo = x => x + 2;

        // Act
        var composed = addTwo.Then((Func<int, int>)MultiplyByThree);
        var result = composed(5);
        
        // Assert
        Assert.That(result, Is.EqualTo(21)); // (5+2)*3 = 21
        return;

        int MultiplyByThree(int x) => x * 3;
    }

    #endregion

    #region After Tests
    
    /// <summary>
    /// 测试After方法 - 验证反向函数组合的正确性
    /// </summary>
    [Test]
    public void After_Should_Compose_Functions_In_Reversed_Order()
    {
        // Arrange
        Func<int, int> multiplyByThree = x => x * 3;

        // Act
        var composed = multiplyByThree.After((Func<int, int>)AddTwo);
        var result = composed(5);
        
        // Assert
        Assert.That(result, Is.EqualTo(21)); // (5+2)*3 = 21
        return;

        int AddTwo(int x) => x + 2;
    }

    #endregion

    #region Tap Tests
    
    /// <summary>
    /// 测试Tap方法 - 验证副作用操作执行后返回原值
    /// </summary>
    [Test]
    public void Tap_Should_Execute_Action_And_Return_Original_Value()
    {
        // Arrange
        var value = 42;
        var capturedValue = 0;
        
        // Act
        var result = value.Tap(x => capturedValue = x);
        
        // Assert
        Assert.That(result, Is.EqualTo(42));
        Assert.That(capturedValue, Is.EqualTo(42));
    }

    #endregion

    #region Map Tests
    
    /// <summary>
    /// 测试Map方法 - 验证集合中的每个元素都能被正确转换
    /// </summary>
    [Test]
    public void Map_Should_Transform_Each_Element_In_Collection()
    {
        // Arrange
        var numbers = new[] {1, 2, 3, 4};
        
        // Act
        var result = numbers.Map(x => x * x).ToArray();
        
        // Assert
        Assert.That(result, Is.EquivalentTo([1, 4, 9, 16]));
    }

    #endregion

    #region Filter Tests
    
    /// <summary>
    /// 测试Filter方法 - 验证集合能够根据条件正确过滤
    /// </summary>
    [Test]
    public void Filter_Should_Filter_Elements_Based_On_Predicate()
    {
        // Arrange
        var numbers = new[] {1, 2, 3, 4, 5, 6};
        
        // Act
        var result = numbers.Filter(x => x % 2 == 0).ToArray();
        
        // Assert
        Assert.That(result, Is.EquivalentTo([2, 4, 6]));
    }

    #endregion

    #region Reduce Tests
    
    /// <summary>
    /// 测试Reduce方法 - 验证集合能够正确归约为单个值
    /// </summary>
    [Test]
    public void Reduce_Should_Reduce_Collection_To_Single_Value()
    {
        // Arrange
        var numbers = new[] {1, 2, 3, 4};
        
        // Act
        var result = numbers.Reduce(0, (acc, x) => acc + x);
        
        // Assert
        Assert.That(result, Is.EqualTo(10));
    }

    #endregion

    #region Apply Tests
    
    /// <summary>
    /// 测试Apply方法 - 验证函数能够正确应用到参数上
    /// </summary>
    [Test]
    public void Apply_Should_Apply_Function_To_Argument()
    {
        // Arrange
        Func<int, int> multiplyByTwo = x => x * 2;
        
        // Act
        var result = multiplyByTwo.Apply(5);
        
        // Assert
        Assert.That(result, Is.EqualTo(10));
    }

    #endregion

    #region Curry Tests
    
    /// <summary>
    /// 测试Curry方法 - 验证二参数函数能够正确柯里化
    /// </summary>
    [Test]
    public void Curry_Should_Convert_Binary_Function_To_Curried_Form()
    {
        // Arrange
        Func<int, int, int> add = (x, y) => x + y;
        
        // Act
        var curriedAdd = add.Curry();
        var addFive = curriedAdd(5);
        var result = addFive(3);
        
        // Assert
        Assert.That(result, Is.EqualTo(8));
    }

    #endregion

    #region Uncurry Tests
    
    /// <summary>
    /// 测试Uncurry方法 - 验证柯里化函数能够正确还原为二参数函数
    /// </summary>
    [Test]
    public void Uncurry_Should_Convert_Curried_Function_Back_To_Binary_Form()
    {
        // Arrange
        Func<int, int, int> originalAdd = (x, y) => x + y;
        var curriedAdd = originalAdd.Curry();
        
        // Act
        var uncurriedAdd = curriedAdd.Uncurry();
        var result = uncurriedAdd(5, 3);
        
        // Assert
        Assert.That(result, Is.EqualTo(8));
    }

    #endregion

    #region Partial Tests
    
    /// <summary>
    /// 测试Partial方法 - 验证部分应用函数功能
    /// </summary>
    [Test]
    public void Partial_Should_Fix_First_Argument_Of_Binary_Function()
    {
        // Arrange
        Func<int, int, int> multiply = (x, y) => x * y;
        
        // Act
        var doubleFunction = multiply.Partial(2);
        var result = doubleFunction(5);
        
        // Assert
        Assert.That(result, Is.EqualTo(10));
    }

    #endregion

    #region Match Tests
    
    /// <summary>
    /// 测试Match方法 - 验证模式匹配功能
    /// </summary>
    [Test]
    public void Match_Should_Execute_Matching_Case()
    {
        // Arrange
        var value = 5;
        
        // Act
        var result = value.Match(
            (x => x < 0, _ => "negative"),
            (x => x > 0, _ => "positive"),
            (x => x == 0, _ => "zero")
        );
        
        // Assert
        Assert.That(result, Is.EqualTo("positive"));
    }
    
    /// <summary>
    /// 测试Match方法 - 验证无匹配时抛出异常
    /// </summary>
    [Test]
    public void Match_Should_Throw_Exception_When_No_Case_Matches()
    {
        // Arrange
        var value = 10;
        
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            value.Match(
                (x => x < 0, _ => "negative"),
                (x => x > 10, _ => "large positive")
            )
        );
    }

    #endregion

    #region MatchOrDefault Tests
    
    /// <summary>
    /// 测试MatchOrDefault方法 - 验证模式匹配带默认值功能
    /// </summary>
    [Test]
    public void MatchOrDefault_Should_Execute_Matching_Case_Or_Return_Default()
    {
        // Arrange
        var value = 10;
        
        // Act
        var result = value.MatchOrDefault("unknown",
            (x => x < 0, _ => "negative"),
            (x => x > 10, _ => "large positive")
        );
        
        // Assert
        Assert.That(result, Is.EqualTo("unknown"));
    }

    #endregion

    #region If Tests
    
    /// <summary>
    /// 测试If方法 - 验证条件执行功能
    /// </summary>
    [Test]
    public void If_Should_Execute_ThenFunc_When_Condition_Is_True()
    {
        // Arrange
        var value = 5;
        
        // Act
        var result = value.If(x => x > 0, x => x * 2);
        
        // Assert
        Assert.That(result, Is.EqualTo(10));
    }
    
    /// <summary>
    /// 测试If方法 - 验证条件为假时不执行转换函数
    /// </summary>
    [Test]
    public void If_Should_Return_Original_Value_When_Condition_Is_False()
    {
        // Arrange
        var value = -5;
        
        // Act
        var result = value.If(x => x > 0, x => x * 2);
        
        // Assert
        Assert.That(result, Is.EqualTo(-5));
    }

    #endregion

    #region IfElse Tests
    
    /// <summary>
    /// 测试IfElse方法 - 验证条件分支功能
    /// </summary>
    [Test]
    public void IfElse_Should_Execute_ThenFunc_When_Condition_Is_True()
    {
        // Arrange
        var value = 5;
        
        // Act
        var result = value.IfElse(
            x => x > 0,
            x => x * 2,
            x => x * -1
        );
        
        // Assert
        Assert.That(result, Is.EqualTo(10));
    }
    
    /// <summary>
    /// 测试IfElse方法 - 验证条件为假时执行else分支
    /// </summary>
    [Test]
    public void IfElse_Should_Execute_ElseFunc_When_Condition_Is_False()
    {
        // Arrange
        var value = -5;
        
        // Act
        var result = value.IfElse(
            x => x > 0,
            x => x * 2,
            x => x * -1
        );
        
        // Assert
        Assert.That(result, Is.EqualTo(5));
    }

    #endregion

    #region As Tests
    
    /// <summary>
    /// 测试As方法 - 验证安全类型转换功能
    /// </summary>
    [Test]
    public void As_Should_Perform_Safe_Type_Cast()
    {
        // Arrange
        object obj = "Hello";
        
        // Act
        var result = obj.As<string>();
        
        // Assert
        Assert.That(result, Is.EqualTo("Hello"));
    }
    
    /// <summary>
    /// 测试As方法 - 验证不兼容类型转换返回null
    /// </summary>
    [Test]
    public void As_Should_Return_Null_For_Incompatible_Types()
    {
        // Arrange
        object obj = 42;
        
        // Act
        var result = obj.As<string>();
        
        // Assert
        Assert.That(result, Is.Null);
    }

    #endregion

    #region Cast Tests
    
    /// <summary>
    /// 测试Cast方法 - 验证强制类型转换功能
    /// </summary>
    [Test]
    public void Cast_Should_Perform_Forced_Type_Cast()
    {
        // Arrange
        object obj = "Hello";
        
        // Act
        var result = obj.Cast<string>();
        
        // Assert
        Assert.That(result, Is.EqualTo("Hello"));
    }

    #endregion

    #region Also Tests
    
    /// <summary>
    /// 测试Also方法 - 验证执行操作后返回原值功能
    /// </summary>
    [Test]
    public void Also_Should_Execute_Action_And_Return_Original_Value()
    {
        // Arrange
        var value = 42;
        var capturedValue = 0;
        
        // Act
        var result = value.Also(x => capturedValue = x);
        
        // Assert
        Assert.That(result, Is.EqualTo(42));
        Assert.That(capturedValue, Is.EqualTo(42));
    }

    #endregion

    #region Let Tests
    
    /// <summary>
    /// 测试Let方法 - 验证值转换功能
    /// </summary>
    [Test]
    public void Let_Should_Transform_Value()
    {
        // Arrange
        var value = 5;
        
        // Act
        var result = value.Let(x => x * 2);
        
        // Assert
        Assert.That(result, Is.EqualTo(10));
    }

    #endregion

    #region TakeIf Tests
    
    /// <summary>
    /// 测试TakeIf方法 - 验证条件为真时返回原值
    /// </summary>
    [Test]
    public void TakeIf_Should_Return_Value_When_Condition_Is_True()
    {
        // Arrange
        string str = "Hello";
        
        // Act
        var result = str.TakeIf(s => s.Length > 3);
        
        // Assert
        Assert.That(result, Is.EqualTo("Hello"));
    }
    
    /// <summary>
    /// 测试TakeIf方法 - 验证条件为假时返回null
    /// </summary>
    [Test]
    public void TakeIf_Should_Return_Null_When_Condition_Is_False()
    {
        // Arrange
        string str = "Hi";
        
        // Act
        var result = str.TakeIf(s => s.Length > 3);
        
        // Assert
        Assert.That(result, Is.Null);
    }

    #endregion

    #region TakeUnless Tests
    
    /// <summary>
    /// 测试TakeUnless方法 - 验证条件为假时返回原值
    /// </summary>
    [Test]
    public void TakeUnless_Should_Return_Value_When_Condition_Is_False()
    {
        // Arrange
        string str = "Hi";
        
        // Act
        var result = str.TakeUnless(s => s.Length > 3);
        
        // Assert
        Assert.That(result, Is.EqualTo("Hi"));
    }
    
    /// <summary>
    /// 测试TakeUnless方法 - 验证条件为真时返回null
    /// </summary>
    [Test]
    public void TakeUnless_Should_Return_Null_When_Condition_Is_True()
    {
        // Arrange
        string str = "Hello";
        
        // Act
        var result = str.TakeUnless(s => s.Length > 3);
        
        // Assert
        Assert.That(result, Is.Null);
    }

    #endregion

    #region Repeat Tests
    
    /// <summary>
    /// 测试Repeat方法 - 验证重复执行函数功能
    /// </summary>
    [Test]
    public void Repeat_Should_Execute_Function_N_Times()
    {
        // Arrange
        var initialValue = 2;
        
        // Act
        var result = initialValue.Repeat(3, x => x * 2);
        
        // Assert
        // 2 -> 4 -> 8 -> 16 (3次重复)
        Assert.That(result, Is.EqualTo(16));
    }

    #endregion

    #region Try Tests
    
    /// <summary>
    /// 测试Try方法 - 验证安全执行成功情况
    /// </summary>
    [Test]
    public void Try_Should_Return_Success_When_Function_Does_Not_Throw()
    {
        // Arrange
        var value = 10;
        
        // Act
        var (success, result, error) = value.Try(x => 100 / x);
        
        // Assert
        Assert.That(success, Is.True);
        Assert.That(result, Is.EqualTo(10));
        Assert.That(error, Is.Null);
    }
    
    /// <summary>
    /// 测试Try方法 - 验证安全执行异常情况
    /// </summary>
    [Test]
    public void Try_Should_Return_Failure_When_Function_Throws()
    {
        // Arrange
        var value = 0;
        
        // Act
        var (success, result, error) = value.Try(x => 100 / x);
        
        // Assert
        Assert.That(success, Is.False);
        Assert.That(result, Is.EqualTo(0)); // 对于int类型，默认值是0
        Assert.That(error, Is.Not.Null);
        Assert.That(error, Is.TypeOf<DivideByZeroException>());
    }

    #endregion

    #region Memoize Tests
    
    /// <summary>
    /// 测试Memoize方法 - 验证函数结果缓存功能
    /// </summary>
    [Test]
    public void Memoize_Should_Cache_Function_Results()
    {
        // Arrange
        var callCount = 0;
        Func<int, int> expensiveFunction = x =>
        {
            callCount++;
            return x * x;
        };
        var memoized = expensiveFunction.Memoize();
        
        // Act
        var result1 = memoized(5); // 第一次调用
        var result2 = memoized(5); // 第二次调用，应该使用缓存
        var result3 = memoized(3); // 新参数，应该调用函数
        var result4 = memoized(3); // 再次使用相同参数，应该使用缓存
        
        // Assert
        Assert.That(result1, Is.EqualTo(25));
        Assert.That(result2, Is.EqualTo(25));
        Assert.That(result3, Is.EqualTo(9));
        Assert.That(result4, Is.EqualTo(9));
        Assert.That(callCount, Is.EqualTo(2)); // 只应调用两次，而不是四次
    }

    #endregion
}