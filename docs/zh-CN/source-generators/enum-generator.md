# 枚举扩展生成器

> 为枚举生成 `IsX()` 和 `IsIn(...)` 扩展方法。

## 概述

`EnumExtensionsGenerator` 位于 `GFramework.SourceGenerators.Enums.EnumExtensionsGenerator`，当前会为带
`[GenerateEnumExtensions]` 的枚举生成：

- 每个枚举成员对应的 `IsX()` 方法
- 一个 `IsIn(params T[])` 方法

## 基础使用

```csharp
using GFramework.SourceGenerators.Abstractions.Enums;

[GenerateEnumExtensions]
public enum GameState
{
    Normal,
    Paused,
    GameOver
}
```

生成结果的核心形态如下：

```csharp
public static partial class GameStateExtensions
{
    public static bool IsNormal(this GameState value) => value == GameState.Normal;

    public static bool IsPaused(this GameState value) => value == GameState.Paused;

    public static bool IsGameOver(this GameState value) => value == GameState.GameOver;

    public static bool IsIn(this GameState value, params GameState[] values)
    {
        if (values == null) return false;
        foreach (var candidate in values)
        {
            if (value == candidate) return true;
        }

        return false;
    }
}
```

## 使用示例

```csharp
var state = GameState.Paused;

if (state.IsPaused())
{
    Console.WriteLine("Game is paused.");
}

if (state.IsIn(GameState.Paused, GameState.GameOver))
{
    Console.WriteLine("Gameplay loop should stop.");
}
```

## 当前实现与属性定义的差异

`GenerateEnumExtensionsAttribute` 当前声明了两个开关：

- `GenerateIsMethods`
- `GenerateIsInMethod`

但当前生成器实现并不会读取这两个属性值。也就是说：

- 即使你显式把它们设为 `false`
- 当前生成器仍会生成 `IsX()` 和 `IsIn(...)`

因此本页不再把这些开关写成“当前可用配置项”，而只把它们视为尚未接入生成逻辑的预留接口。
