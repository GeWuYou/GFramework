---
title: 函数式工具
description: Option、Result 与函数组合的默认值、异常和取消契约。
---

# 函数式工具

`GFramework.Core.Functional` 提供可独立使用的 Option、Result 和函数组合能力，普通业务类无需继承框架基类。

## Option：显式缺席

`Option<T>.Some(value)` 表示存在值，`None` 与默认值表示缺席。Some 拒绝 null，隐式传入空引用则得到 None。

```csharp
using GFramework.Core.Functional;

Option<string> nickname = Option<string>.Some("Player");
string label = nickname.Match(name => name, () => "Guest");
Option<int> length = nickname.Map(name => name.Length);
```

Map 通过 Some 构造结果，因此映射到 null 会抛出异常；可能缺席的映射使用 Bind 并明确返回 Some/None。Option 不携带失败原因。

## Result：成功或失败

`Result.Success()` 表示无值成功；`Result<T>.Success(value)` 表示带值成功。两类使用一致的 `Failure(error)`、`IsSuccess`、`IsFailure` 和 `Error`。

```csharp
using System;
using GFramework.Core.Functional;

Result<int> count = Result<int>.Success(3);
Result<int> doubled = count.Map(value => value * 2);
string text = doubled.Match(value => value.ToString(), error => error.Message);
Result<int> failure = Result<int>.Failure(new InvalidOperationException("Unavailable"));
```

可空成功值必须用相应类型明确表达，例如 `Result<string?>.Success(null)`。T 是 Exception 时，Success 仍表示成功值，Failure 明确表示失败；没有值/异常二义性构造器。

`default(Result)` 与 `default(Result<T>)` 均为失败，Error 是共享的未初始化标记，不会每次读取都创建异常。成功结果的 Error 访问会抛出 InvalidOperationException，应先分支或使用 Match。

Map、Bind、异步版本和 Combine 原样传播既有失败，跳过成功委托。Combine 保持顺序；空序列成功得到空列表，默认结果属于失败，不能被忽略。

## 异常与取消

Map/Bind 的同步和异步版本都传播委托异常。只有明确调用 Try/TryAsync 才将普通异常转换为 Failure：

```csharp
using System.IO;
using GFramework.Core.Functional;

Result<string> read = Result<string>.Try(() => File.ReadAllText("settings.json"));
Result<string> readAsync = await Result<string>.TryAsync(() => File.ReadAllTextAsync("settings.json"));
```

OperationCanceledException 及其派生类型始终传播，不转换为普通失败。只在确有边界转换或恢复需求时使用 Try，不需要为每个方法套一层 Result。

OnSuccess/OnFailure 观察结果并返回原值，Ensure 验证成功值并保留既有失败。ToNullable 将失败转为 null，会丢失原因。

## 组合工具与成本

Pipe、Compose、AndThen、Curry 等只在使数据流更易读时使用。MemoizeUnbounded 不提供容量回收，适用于可以接受持续保留键值的纯函数。Once 和 Defer 用于有明确所有权的延迟计算。

成功结果按值比较；失败按异常类型和序数消息比较，并保持一致哈希。不提供结果排序。异常、Task、委托与闭包仍可能分配，Result 不构成性能保证。

参见[函数式实践](../tutorials/functional-programming.md)和[错误处理](../best-practices/error-handling.md)。
