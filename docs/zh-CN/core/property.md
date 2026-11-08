---
title: 可绑定属性
description: 用字段级响应值连接状态与展示，并管理订阅生命周期。
---

# 可绑定属性

`BindableProperty<T>` 表达单个可观察字段。对外暴露 `IReadonlyBindableProperty<T>` 可以限制视图写入，但不自动约束整个业务对象的可变性。复杂聚合状态需要 reducer、选择器或历史时，参见[状态管理](./state-management.md)。

## 最小使用

```csharp
using System;
using GFramework.Core.Property;

var score = new BindableProperty<int>(0);
using var subscription = score.RegisterWithInitValue(value => Console.WriteLine(value));
score.Value = 10;
```

Register 只观察后续变化，RegisterWithInitValue 先通知当前值。二者返回 IDisposable，视图或绑定拥有者退出时 Dispose；相同字段更新不会默认产生变化通知。SetValueWithoutEvent 更新字段而不通知订阅者。

## 展示状态与业务规则

```csharp
using GFramework.Core.Abstractions.Property;
using GFramework.Core.Property;

public sealed class ScoreViewState
{
    private readonly BindableProperty<int> _score = new(0);
    public IReadonlyBindableProperty<int> Score => _score;
    public void Present(int score) => _score.Value = score;
}
```

展示层读取字段并管理订阅；用例调用领域方法修改权威状态，再更新展示快照。不要让视图绕过领域规则直接修改所有公共属性。

## 线程与比较

字段存取与监听器列表有内部同步，回调在赋值线程执行，不自动回到 Godot 主线程。调用者应串行化初值订阅与跨线程更新，不能把 RegisterWithInitValue 视为跨线程事务。

WithComparer 设置的是同一个闭合 T 类型共享的比较策略，会影响该 T 的其他 BindableProperty 实例。只需要局部比较规则时，可使用有独立 comparer 的 Store 选择器。

参见[事件订阅](./events.md)和[架构组织](../best-practices/architecture-patterns.md)。
