# Context Get 注入生成器

> 为上下文感知类型生成 `__InjectContextBindings_Generated()`。

## 概述

`ContextGetGenerator` 位于 `GFramework.SourceGenerators.Rule.ContextGetGenerator`，当前支持以下注入标记：

| 单值             | 集合               |
|----------------|------------------|
| `[GetModel]`   | `[GetModels]`    |
| `[GetSystem]`  | `[GetSystems]`   |
| `[GetUtility]` | `[GetUtilities]` |
| `[GetService]` | `[GetServices]`  |

此外还支持类型级别的 `[GetAll]`，用于自动推断可注入字段。

## 目标类型要求

目标类型必须满足以下任一条件：

- 标记了 `[ContextAware]`
- 实现 `IContextAware`
- 继承 `ContextAwareBase`

并且目标类型本身必须是 `partial class`。

## 基础使用

```csharp
using GFramework.SourceGenerators.Abstractions.Rule;

[ContextAware]
public partial class PlayerController
{
    [GetModel]
    private IPlayerModel _playerModel = null!;

    [GetSystem]
    private ICombatSystem _combatSystem = null!;

    [GetService]
    private IAudioService _audioService = null!;

    public void Initialize()
    {
        __InjectContextBindings_Generated();
        _combatSystem.Attack(_playerModel);
    }
}
```

生成器会产出一个私有注入方法：

```csharp
private void __InjectContextBindings_Generated()
{
    _playerModel = this.GetModel<global::IPlayerModel>();
    _combatSystem = this.GetSystem<global::ICombatSystem>();
    _audioService = this.GetService<global::IAudioService>();
}
```

## 当前行为边界

- 生成器只生成 `__InjectContextBindings_Generated()`，不会自动调用它。
- 推荐在上下文已就绪的生命周期入口里手动调用，例如 `Initialize()`、`OnEnter()` 或 Godot 的 `_Ready()`。
- `[GetAll]` 当前只会推断 Model / System / Utility 及其 `IReadOnlyList<T>` 集合形态，不会自动推断 Service。

## `[GetAll]` 示例

```csharp
using GFramework.SourceGenerators.Abstractions.Rule;

[ContextAware]
[GetAll]
public partial class GameController
{
    private IPlayerModel _playerModel = null!;
    private ICombatSystem _combatSystem = null!;
    private IReadOnlyList<IHudUtility> _hudUtilities = null!;

    [GetService]
    private IAudioService _audioService = null!;
}
```

## 配套分析器

当字段或手写 `GetModel<T>() / GetSystem<T>() / GetUtility<T>()` 调用在所属架构中找不到静态可见注册时，
`ContextRegistrationAnalyzer` 会给出警告。

详细诊断见 [分析器](/zh-CN/source-generators/analyzers)。
