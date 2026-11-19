---
title: "可绑定属性"
description: "观察单个值并在界面退出时解除绑定。"
---

# 可绑定属性 {#overview}

```csharp
using GFramework.Core.Property;

var score = new BindableProperty<int>(0);
using var subscription = score.RegisterWithInitValue(Console.WriteLine);
score.Value = 10;
```

`RegisterWithInitValue` 先发出当前值，`Register` 观察后续变化。视图可接收 `IReadonlyBindableProperty<T>`，修改由用例完成。让绑定方释放订阅令牌。

回调在赋值线程执行。在引擎线程上更新需要驱动原生 UI 的属性。多个字段需要作为一个快照更新时，参见[集中状态](../core/state-management.md)。
