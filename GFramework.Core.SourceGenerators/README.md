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

`[GetAll]` 会跳过 `const`、`static` 和 `readonly` 字段。若某个字段本来会被 `[GetAll]` 推断为
`Model`、`System` 或 `Utility` 绑定，但因为是不可赋值的 `static` 或 `readonly` 字段而被跳过，生成器会发出警告提示该字段不会参与生成。

## 注册分析器

包现在同时包含一个注册可见性分析器，用于检查 `Model`、`System`、`Utility` 的使用点是否能在所属架构中找到静态可见注册。

- 覆盖字段特性注入：`[GetModel]`、`[GetModels]`、`[GetSystem]`、`[GetSystems]`、`[GetUtility]`、`[GetUtilities]`
- 覆盖手写调用：`GetModel<T>()`、`GetModels<T>()`、`GetSystem<T>()`、`GetSystems<T>()`、`GetUtility<T>()`、`GetUtilities<T>()`
- 默认报告 `Warning`
- 当前只分析静态可见的注册路径，例如 `OnInitialize()`、`InstallModules()`、`InstallModule(new Module())`

对于反射、运行时条件分支、外部程序集动态注册等路径，分析器不会强行推断；当无法唯一确定组件所属架构时，也会选择不报，优先降低误报。
