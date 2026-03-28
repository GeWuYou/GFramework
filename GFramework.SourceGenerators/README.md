# GFramework.SourceGenerators

Core 侧通用源码生成器模块。

## Context Get 注入

当类本身是上下文感知类型时，可以通过字段特性生成一个手动调用的注入方法：

- `[GetService]`
- `[GetServices]`
- `[GetSystem]`
- `[GetSystems]`
- `[GetModel]`
- `[GetModels]`
- `[GetUtility]`
- `[GetUtilities]`
- `[GetAll]`

上下文感知类满足以下任一条件即可：

- 类上带有 `[ContextAware]`
- 继承 `ContextAwareBase`
- 实现 `IContextAware`

生成器会生成 `__InjectContextBindings_Generated()`，需要在合适的生命周期中手动调用。在 Godot 中通常放在 `_Ready()`：

```csharp
using GFramework.SourceGenerators.Abstractions.Rule;

[ContextAware]
public partial class InventoryPanel
{
    [GetModel]
    private IInventoryModel _inventory = null!;

    [GetServices]
    private IReadOnlyList<IInventoryStrategy> _strategies = null!;

    public override void _Ready()
    {
        __InjectContextBindings_Generated();
    }
}
```

`[GetAll]` 作用于类本身，会自动扫描字段并推断 `Model`、`System`、`Utility` 相关的 `GetX` 调用；已显式标记字段的优先级更高。

`Service` 和 `Services` 绑定不会在 `[GetAll]` 下自动推断。对于普通引用类型字段，请显式使用 `[GetService]` 或
`[GetServices]`，避免将非上下文服务字段误判为服务依赖。
