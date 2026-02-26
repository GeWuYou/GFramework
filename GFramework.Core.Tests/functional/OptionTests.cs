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

using GFramework.Core.Functional;
using NUnit.Framework;

namespace GFramework.Core.Tests.Functional;

/// <summary>
///     Option&lt;T&gt; 类型测试类
/// </summary>
[TestFixture]
public class OptionTests
{
    [Test]
    public void Some_WithValue_Should_Create_Some_Option()
    {
        var option = Option<int>.Some(42);
        Assert.That(option.IsSome, Is.True);
        Assert.That(option.IsNone, Is.False);
    }

    [Test]
    public void Some_WithNull_Should_Throw_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Option<string>.Some(null!));
    }

    [Test]
    public void None_Should_Create_None_Option()
    {
        var option = Option<int>.None;
        Assert.That(option.IsSome, Is.False);
        Assert.That(option.IsNone, Is.True);
    }

    [Test]
    public void GetOrElse_WithSome_Should_Return_Value()
    {
        var option = Option<int>.Some(42);
        var result = option.GetOrElse(0);
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void GetOrElse_WithNone_Should_Return_Default_Value()
    {
        var option = Option<int>.None;
        var result = option.GetOrElse(99);
        Assert.That(result, Is.EqualTo(99));
    }

    [Test]
    public void GetOrElse_WithFactory_WithSome_Should_Return_Value_Without_Calling_Factory()
    {
        var option = Option<int>.Some(42);
        var factoryCalled = false;

        var result = option.GetOrElse(() =>
        {
            factoryCalled = true;
            return 99;
        });

        Assert.That(result, Is.EqualTo(42));
        Assert.That(factoryCalled, Is.False);
    }

    [Test]
    public void GetOrElse_WithFactory_WithNone_Should_Call_Factory()
    {
        var option = Option<int>.None;
        var result = option.GetOrElse(() => 99);
        Assert.That(result, Is.EqualTo(99));
    }

    [Test]
    public void GetOrElse_WithNullFactory_Should_Throw_ArgumentNullException()
    {
        var option = Option<int>.None;
        Assert.Throws<ArgumentNullException>(() => option.GetOrElse(null!));
    }

    [Test]
    public void Map_WithSome_Should_Map_Value()
    {
        var option = Option<int>.Some(42);
        var mapped = option.Map(x => x.ToString());
        Assert.That(mapped.IsSome, Is.True);
        Assert.That(mapped.GetOrElse(""), Is.EqualTo("42"));
    }

    [Test]
    public void Map_WithNone_Should_Return_None()
    {
        var option = Option<int>.None;
        var mapped = option.Map(x => x.ToString());
        Assert.That(mapped.IsNone, Is.True);
    }

    [Test]
    public void Map_WithNullMapper_Should_Throw_ArgumentNullException()
    {
        var option = Option<int>.Some(42);
        Assert.Throws<ArgumentNullException>(() => option.Map<string>(null!));
    }

    [Test]
    public void Bind_WithSome_Should_Bind_Value()
    {
        var option = Option<string>.Some("42");
        var bound = option.Bind(s => int.TryParse(s, out var i)
            ? Option<int>.Some(i)
            : Option<int>.None);

        Assert.That(bound.IsSome, Is.True);
        Assert.That(bound.GetOrElse(0), Is.EqualTo(42));
    }

    [Test]
    public void Bind_WithSome_Can_Return_None()
    {
        var option = Option<string>.Some("invalid");
        var bound = option.Bind(s => int.TryParse(s, out var i)
            ? Option<int>.Some(i)
            : Option<int>.None);

        Assert.That(bound.IsNone, Is.True);
    }

    [Test]
    public void Bind_WithNone_Should_Return_None()
    {
        var option = Option<string>.None;
        var bound = option.Bind(s => Option<int>.Some(42));
        Assert.That(bound.IsNone, Is.True);
    }

    [Test]
    public void Bind_WithNullBinder_Should_Throw_ArgumentNullException()
    {
        var option = Option<int>.Some(42);
        Assert.Throws<ArgumentNullException>(() => option.Bind<string>(null!));
    }

    [Test]
    public void Filter_WithSome_PredicateTrue_Should_Return_Some()
    {
        var option = Option<int>.Some(42);
        var filtered = option.Filter(x => x > 0);
        Assert.That(filtered.IsSome, Is.True);
        Assert.That(filtered.GetOrElse(0), Is.EqualTo(42));
    }

    [Test]
    public void Filter_WithSome_PredicateFalse_Should_Return_None()
    {
        var option = Option<int>.Some(42);
        var filtered = option.Filter(x => x < 0);
        Assert.That(filtered.IsNone, Is.True);
    }

    [Test]
    public void Filter_WithNone_Should_Return_None()
    {
        var option = Option<int>.None;
        var filtered = option.Filter(x => true);
        Assert.That(filtered.IsNone, Is.True);
    }

    [Test]
    public void Filter_WithNullPredicate_Should_Throw_ArgumentNullException()
    {
        var option = Option<int>.Some(42);
        Assert.Throws<ArgumentNullException>(() => option.Filter(null!));
    }

    [Test]
    public void Match_WithSome_Should_Call_Some_Function()
    {
        var option = Option<int>.Some(42);
        var result = option.Match(
            some: value => $"Value: {value}",
            none: () => "No value"
        );
        Assert.That(result, Is.EqualTo("Value: 42"));
    }

    [Test]
    public void Match_WithNone_Should_Call_None_Function()
    {
        var option = Option<int>.None;
        var result = option.Match(
            some: value => $"Value: {value}",
            none: () => "No value"
        );
        Assert.That(result, Is.EqualTo("No value"));
    }

    [Test]
    public void Match_WithNullSomeFunction_Should_Throw_ArgumentNullException()
    {
        var option = Option<int>.Some(42);
        Assert.Throws<ArgumentNullException>(() =>
            option.Match<string>(null!, () => ""));
    }

    [Test]
    public void Match_WithNullNoneFunction_Should_Throw_ArgumentNullException()
    {
        var option = Option<int>.Some(42);
        Assert.Throws<ArgumentNullException>(() =>
            option.Match(value => "", null!));
    }

    [Test]
    public void Match_Action_WithSome_Should_Call_Some_Action()
    {
        var option = Option<int>.Some(42);
        var someCalled = false;
        var noneCalled = false;

        option.Match(
            some: _ => someCalled = true,
            none: () => noneCalled = true
        );

        Assert.That(someCalled, Is.True);
        Assert.That(noneCalled, Is.False);
    }

    [Test]
    public void Match_Action_WithNone_Should_Call_None_Action()
    {
        var option = Option<int>.None;
        var someCalled = false;
        var noneCalled = false;

        option.Match(
            some: _ => someCalled = true,
            none: () => noneCalled = true
        );

        Assert.That(someCalled, Is.False);
        Assert.That(noneCalled, Is.True);
    }

    [Test]
    public void ToResult_WithSome_Should_Return_Success_Result()
    {
        var option = Option<int>.Some(42);
        var result = option.ToResult("Error");
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Match(succ: v => v, fail: _ => 0), Is.EqualTo(42));
    }

    [Test]
    public void ToResult_WithNone_Should_Return_Failure_Result()
    {
        var option = Option<int>.None;
        var result = option.ToResult("Value not found");
        Assert.That(result.IsFaulted, Is.True);
        Assert.That(result.Exception, Is.TypeOf<InvalidOperationException>());
        Assert.That(result.Exception.Message, Is.EqualTo("Value not found"));
    }

    [Test]
    public void ToResult_WithNullOrWhiteSpaceMessage_Should_Throw_ArgumentException()
    {
        var option = Option<int>.None;
        Assert.Throws<ArgumentException>(() => option.ToResult(""));
        Assert.Throws<ArgumentException>(() => option.ToResult("   "));
    }

    [Test]
    public void ToEnumerable_WithSome_Should_Return_Sequence_With_One_Element()
    {
        var option = Option<int>.Some(42);
        var enumerable = option.ToEnumerable().ToList();
        Assert.That(enumerable, Has.Count.EqualTo(1));
        Assert.That(enumerable[0], Is.EqualTo(42));
    }

    [Test]
    public void ToEnumerable_WithNone_Should_Return_Empty_Sequence()
    {
        var option = Option<int>.None;
        var enumerable = option.ToEnumerable().ToList();
        Assert.That(enumerable, Is.Empty);
    }

    [Test]
    public void ImplicitConversion_FromValue_Should_Create_Some_Option()
    {
        Option<int> option = 42;
        Assert.That(option.IsSome, Is.True);
        Assert.That(option.GetOrElse(0), Is.EqualTo(42));
    }

    [Test]
    public void ImplicitConversion_FromNull_Should_Create_None_Option()
    {
        Option<string> option = null!;
        Assert.That(option.IsNone, Is.True);
    }

    [Test]
    public void Equals_TwoSomeWithSameValue_Should_Return_True()
    {
        var option1 = Option<int>.Some(42);
        var option2 = Option<int>.Some(42);
        Assert.That(option1.Equals(option2), Is.True);
        Assert.That(option1 == option2, Is.True);
        Assert.That(option1 != option2, Is.False);
    }

    [Test]
    public void Equals_TwoSomeWithDifferentValue_Should_Return_False()
    {
        var option1 = Option<int>.Some(42);
        var option2 = Option<int>.Some(99);
        Assert.That(option1.Equals(option2), Is.False);
        Assert.That(option1 == option2, Is.False);
        Assert.That(option1 != option2, Is.True);
    }

    [Test]
    public void Equals_TwoNone_Should_Return_True()
    {
        var option1 = Option<int>.None;
        var option2 = Option<int>.None;
        Assert.That(option1.Equals(option2), Is.True);
        Assert.That(option1 == option2, Is.True);
        Assert.That(option1 != option2, Is.False);
    }

    [Test]
    public void Equals_SomeAndNone_Should_Return_False()
    {
        var option1 = Option<int>.Some(42);
        var option2 = Option<int>.None;
        Assert.That(option1.Equals(option2), Is.False);
        Assert.That(option1 == option2, Is.False);
        Assert.That(option1 != option2, Is.True);
    }

    [Test]
    public void GetHashCode_TwoSomeWithSameValue_Should_Return_Same_HashCode()
    {
        var option1 = Option<int>.Some(42);
        var option2 = Option<int>.Some(42);
        Assert.That(option1.GetHashCode(), Is.EqualTo(option2.GetHashCode()));
    }

    [Test]
    public void GetHashCode_TwoNone_Should_Return_Same_HashCode()
    {
        var option1 = Option<int>.None;
        var option2 = Option<int>.None;
        Assert.That(option1.GetHashCode(), Is.EqualTo(option2.GetHashCode()));
    }

    [Test]
    public void ToString_WithSome_Should_Return_Formatted_String()
    {
        var option = Option<int>.Some(42);
        var result = option.ToString();
        Assert.That(result, Is.EqualTo("Some(42)"));
    }

    [Test]
    public void ToString_WithNone_Should_Return_None()
    {
        var option = Option<int>.None;
        var result = option.ToString();
        Assert.That(result, Is.EqualTo("None"));
    }
}