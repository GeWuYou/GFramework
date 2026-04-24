---
title: Property
description: 说明 GFramework.Core.Property 的可绑定属性模型、订阅方式与常见用法。
---

# Property

`GFramework.Core.Property` 负责字段级响应式值。它最适合“一个字段变化就足以驱动视图或局部业务逻辑”的场景；
如果你的状态已经是聚合状态树、需要 reducer / middleware / history，再切到
[state-management](./state-management.md)。

## 安装方式

```bash
dotnet add package GeWuYou.GFramework.Core
dotnet add package GeWuYou.GFramework.Core.Abstractions
```

## 最常用类型

当前最常见的公开类型是：

- `IReadonlyBindableProperty<T>`
- `IBindableProperty<T>`
- `BindableProperty<T>`

一般做法是：内部持有 `BindableProperty<T>`，对外只暴露 `IReadonlyBindableProperty<T>`。

## 最小示例

```csharp
using GFramework.Core.Property;
using GFramework.Core.Abstractions.Property;
using GFramework.Core.Model;

public sealed class PlayerModel : AbstractModel
{
    public BindableProperty<int> Health { get; } = new(100);

    public IReadonlyBindableProperty<int> ReadonlyHealth => Health;

    public void Damage(int amount)
    {
        Health.Value = Math.Max(0, Health.Value - amount);
    }
}
```

监听方式：

```csharp
var unRegister = playerModel.ReadonlyHealth.RegisterWithInitValue(health =>
{
    Console.WriteLine($"Current HP: {health}");
});
```

## 当前公开语义

- `Value`
  - 读写当前值；只有值被判定为“真的变化”时才会触发回调
- `Register(...)`
  - 订阅后续变化，不会立即回放当前值
- `RegisterWithInitValue(...)`
  - 先回放当前值，再继续订阅
- `SetValueWithoutEvent(...)`
  - 更新值但不触发通知
- `UnRegister(...)`
  - 显式移除某个处理器
- `WithComparer(...)`
  - 改写值变化判定逻辑

## 一个需要注意的兼容点

`BindableProperty<T>.Comparer` 是按闭合泛型 `T` 共享的静态比较器，`WithComparer(...)` 本质上会改写这一共享
比较器。也就是说，多个 `BindableProperty<int>` 实例会观察到同一比较规则；只有当你确定整个 `T` 族都要共享同一
判等语义时，再去改它。

## 什么时候继续用 Property

下面这些场景仍然优先使用 `BindableProperty<T>`：

- 单个字段变化就能驱动 UI
- 状态范围局限在单个 Model 或单个页面
- 不需要统一的 action / reducer 写入口
- 不需要撤销/重做、历史快照或中间件

## 什么时候该切到 Store

如果状态已经演化为下面这些形态，更适合用 `Store<TState>`：

- 多个字段必须作为一个原子状态一起演进
- 多个模块共享同一聚合状态
- 需要 reducer / middleware / 历史回放
- 需要从整棵状态树中复用局部选择逻辑

迁移时不必一次性抛弃旧绑定风格。当前已经提供：

- `store.Select(...)`
- `store.ToBindableProperty(...)`

这意味着你可以先把写路径统一到 `Store<TState>`，再渐进迁移现有 UI 或 Controller 的读取方式。
