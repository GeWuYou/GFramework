// Copyright (c) 2025 GeWuYou
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace GFramework.Core.Functional;

/// <summary>
///     表示可能存在或不存在的值，用于替代 null 引用的函数式编程类型
/// </summary>
/// <typeparam name="T">值的类型</typeparam>
public readonly struct Option<T>
{
    private readonly T _value;
    private readonly bool _isSome;

    /// <summary>
    ///     私有构造函数，用于创建 Some 状态
    /// </summary>
    private Option(T value)
    {
        _value = value;
        _isSome = true;
    }

    /// <summary>
    ///     判断是否有值
    /// </summary>
    public bool IsSome => _isSome;

    /// <summary>
    ///     判断是否无值
    /// </summary>
    public bool IsNone => !_isSome;

    #region 工厂方法

    /// <summary>
    ///     创建包含值的 Option
    /// </summary>
    /// <param name="value">要包装的值</param>
    /// <returns>包含值的 Option</returns>
    /// <exception cref="ArgumentNullException">当 value 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// var option = Option&lt;int&gt;.Some(42);
    /// </code>
    /// </example>
    public static Option<T> Some(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Option<T>(value);
    }

    /// <summary>
    ///     表示无值的 Option
    /// </summary>
    public static Option<T> None => default;

    #endregion

    #region 取值

    /// <summary>
    ///     获取值，如果无值则返回默认值
    /// </summary>
    /// <param name="defaultValue">无值时返回的默认值</param>
    /// <returns>Option 中的值或默认值</returns>
    /// <example>
    /// <code>
    /// var value = option.GetOrElse(0); // 如果无值则返回 0
    /// </code>
    /// </example>
    public T GetOrElse(T defaultValue) => _isSome ? _value : defaultValue;

    /// <summary>
    ///     获取值，如果无值则通过工厂函数生成默认值
    /// </summary>
    /// <param name="factory">生成默认值的工厂函数</param>
    /// <returns>Option 中的值或工厂函数生成的值</returns>
    /// <exception cref="ArgumentNullException">当 factory 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// var value = option.GetOrElse(() => ExpensiveDefault());
    /// </code>
    /// </example>
    public T GetOrElse(Func<T> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return _isSome ? _value : factory();
    }

    #endregion

    #region 变换

    /// <summary>
    ///     映射值到新类型，如果无值则返回 None
    /// </summary>
    /// <typeparam name="TResult">映射后的类型</typeparam>
    /// <param name="mapper">映射函数</param>
    /// <returns>映射后的 Option</returns>
    /// <exception cref="ArgumentNullException">当 mapper 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// var option = Option&lt;int&gt;.Some(42);
    /// var mapped = option.Map(x => x.ToString()); // Option&lt;string&gt;.Some("42")
    /// </code>
    /// </example>
    public Option<TResult> Map<TResult>(Func<T, TResult> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        return _isSome ? Option<TResult>.Some(mapper(_value)) : Option<TResult>.None;
    }

    /// <summary>
    ///     单子绑定操作，将 Option 链式转换
    /// </summary>
    /// <typeparam name="TResult">绑定后的类型</typeparam>
    /// <param name="binder">绑定函数</param>
    /// <returns>绑定后的 Option</returns>
    /// <exception cref="ArgumentNullException">当 binder 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// var option = Option&lt;string&gt;.Some("42");
    /// var bound = option.Bind(s => int.TryParse(s, out var i)
    ///     ? Option&lt;int&gt;.Some(i)
    ///     : Option&lt;int&gt;.None);
    /// </code>
    /// </example>
    public Option<TResult> Bind<TResult>(Func<T, Option<TResult>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        return _isSome ? binder(_value) : Option<TResult>.None;
    }

    #endregion

    #region 过滤

    /// <summary>
    ///     根据条件过滤值，不满足条件则返回 None
    /// </summary>
    /// <param name="predicate">过滤条件</param>
    /// <returns>满足条件的 Option 或 None</returns>
    /// <exception cref="ArgumentNullException">当 predicate 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// var option = Option&lt;int&gt;.Some(42);
    /// var filtered = option.Filter(x => x > 0); // Option&lt;int&gt;.Some(42)
    /// var filtered2 = option.Filter(x => x &lt; 0); // Option&lt;int&gt;.None
    /// </code>
    /// </example>
    public Option<T> Filter(Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _isSome && predicate(_value) ? this : None;
    }

    #endregion

    #region 模式匹配

    /// <summary>
    ///     模式匹配，根据是否有值执行不同的函数
    /// </summary>
    /// <typeparam name="TResult">返回值类型</typeparam>
    /// <param name="some">有值时执行的函数</param>
    /// <param name="none">无值时执行的函数</param>
    /// <returns>匹配结果</returns>
    /// <exception cref="ArgumentNullException">当 some 或 none 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// var result = option.Match(
    ///     some: value => $"Value: {value}",
    ///     none: () => "No value"
    /// );
    /// </code>
    /// </example>
    public TResult Match<TResult>(Func<T, TResult> some, Func<TResult> none)
    {
        ArgumentNullException.ThrowIfNull(some);
        ArgumentNullException.ThrowIfNull(none);
        return _isSome ? some(_value) : none();
    }

    /// <summary>
    ///     模式匹配（副作用版本），根据是否有值执行不同的操作
    /// </summary>
    /// <param name="some">有值时执行的操作</param>
    /// <param name="none">无值时执行的操作</param>
    /// <exception cref="ArgumentNullException">当 some 或 none 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// option.Match(
    ///     some: value => Console.WriteLine($"Value: {value}"),
    ///     none: () => Console.WriteLine("No value")
    /// );
    /// </code>
    /// </example>
    public void Match(Action<T> some, Action none)
    {
        ArgumentNullException.ThrowIfNull(some);
        ArgumentNullException.ThrowIfNull(none);
        if (_isSome)
            some(_value);
        else
            none();
    }

    #endregion

    #region 转换

    /// <summary>
    ///     转换为 Result 类型
    /// </summary>
    /// <param name="errorMessage">无值时的错误消息</param>
    /// <returns>Result 类型</returns>
    /// <example>
    /// <code>
    /// var result = option.ToResult("Value not found");
    /// </code>
    /// </example>
    public Result<T> ToResult(string errorMessage = "Value is None")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        return _isSome
            ? Result<T>.Succeed(_value)
            : Result<T>.Fail(new InvalidOperationException(errorMessage));
    }

    /// <summary>
    ///     转换为可枚举集合（有值时包含一个元素，无值时为空集合）
    /// </summary>
    /// <returns>可枚举集合</returns>
    /// <example>
    /// <code>
    /// var items = option.ToEnumerable(); // 有值时: [value], 无值时: []
    /// </code>
    /// </example>
    public IEnumerable<T> ToEnumerable()
    {
        if (_isSome)
            yield return _value;
    }

    #endregion

    #region 隐式转换

    /// <summary>
    ///     从值隐式转换为 Option
    /// </summary>
    public static implicit operator Option<T>(T value) =>
        value is not null ? Some(value) : None;

    #endregion

    #region 相等性和比较

    /// <summary>
    ///     判断两个 Option 是否相等
    /// </summary>
    public bool Equals(Option<T> other)
    {
        if (_isSome != other._isSome)
            return false;

        return !_isSome || EqualityComparer<T>.Default.Equals(_value, other._value);
    }

    /// <summary>
    ///     判断与对象是否相等
    /// </summary>
    public override bool Equals(object? obj) =>
        obj is Option<T> other && Equals(other);

    /// <summary>
    ///     获取哈希码
    /// </summary>
    public override int GetHashCode() =>
        _isSome ? EqualityComparer<T>.Default.GetHashCode(_value!) : 0;

    /// <summary>
    ///     相等运算符
    /// </summary>
    public static bool operator ==(Option<T> left, Option<T> right) =>
        left.Equals(right);

    /// <summary>
    ///     不等运算符
    /// </summary>
    public static bool operator !=(Option<T> left, Option<T> right) =>
        !left.Equals(right);

    #endregion

    #region 字符串表示

    /// <summary>
    ///     获取字符串表示
    /// </summary>
    public override string ToString() =>
        _isSome ? $"Some({_value})" : "None";

    #endregion
}