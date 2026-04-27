---
title: AutoRegisterExportedCollections 生成器
description: 说明批量注册生成器当前会生成什么、可匹配哪些集合与注册器成员，以及 null-skip 与编译期诊断的边界。
---

# AutoRegisterExportedCollections 生成器

`[AutoRegisterExportedCollections]` 用来把“遍历一组配置并逐项调用 registry 方法”的启动样板收敛成一个生成方法。

它最常见的落点确实是 Godot Inspector 导出的数组，但当前生成器真正依赖的不是 `[Export]` 本身，而是：

- 宿主类型被标记了 `[AutoRegisterExportedCollections]`
- 某个实例字段或可读实例属性被标记了 `[RegisterExportedCollection(...)]`
- 该成员可枚举，且元素类型可在编译期推导
- 目标 registry 成员存在，并能找到兼容的单参数实例方法

## 当前包关系

- 特性来源：`GFramework.Godot.SourceGenerators.Abstractions.UI`
- 生成器实现：`GFramework.Godot.SourceGenerators`
- 典型消费者：Godot 启动入口、资源入口节点、配置引导节点

## 最小用法

```csharp
using System.Collections.Generic;
using GFramework.Godot.SourceGenerators.Abstractions.UI;
using Godot;

public interface IKeyValue<TKey, TValue>
{
}

public interface IRegistry<TKey, TValue>
{
    void Registry(IKeyValue<TKey, TValue> mapping);
}

public interface IAssetRegistry<TValue> : IRegistry<string, TValue>
{
}

public sealed class TextureConfig : Resource, IKeyValue<string, Texture2D>
{
}

[AutoRegisterExportedCollections]
public partial class GameEntryPoint : Node
{
    private IAssetRegistry<Texture2D>? _textureRegistry;

    [Export]
    [RegisterExportedCollection(nameof(_textureRegistry), nameof(IRegistry<string, Texture2D>.Registry))]
    private Godot.Collections.Array<TextureConfig>? _textureConfigs;

    public override void _Ready()
    {
        _textureRegistry ??= ResolveTextureRegistry();
        __RegisterExportedCollections_Generated();
    }

    private static IAssetRegistry<Texture2D> ResolveTextureRegistry()
    {
        throw new NotImplementedException();
    }
}
```

当前生成器不会自动调用 `__RegisterExportedCollections_Generated()`。你需要在 registry 成员和集合成员都准备好之后手动调用。

## 当前会生成什么

对于上面的成员，当前生成器会产出：

```csharp
private void __RegisterExportedCollections_Generated()
{
    if (this._textureConfigs is not null && this._textureRegistry is not null)
    {
        foreach (var __generatedItem in this._textureConfigs)
        {
            this._textureRegistry.Registry(__generatedItem);
        }
    }
}
```

最重要的运行时语义只有两条：

- 集合成员为 `null` 时，本次注册直接跳过
- registry 成员为 `null` 时，本次注册直接跳过

这里的“跳过”只针对运行时 `null` 情况；配置错误、方法不匹配、元素类型无法推导等问题都会在编译期直接给出诊断，而不是静默吞掉。

## 当前支持的成员形状

### 集合成员

`[RegisterExportedCollection]` 可以标在：

- 实例字段
- 可读、非索引器的实例属性

它们不必一定带 `[Export]`，但在 Godot 项目里通常会配合 `[Export]` 使用。

### registry 成员

`registryMemberName` 指向的目标也必须是：

- 实例字段，或
- 可读、非索引器的实例属性

静态字段、静态属性、只写属性都不受支持。

## 当前匹配规则

### 可枚举集合

集合成员必须实现 `System.Collections.IEnumerable`，并且生成器还要能推导出元素类型。

因此：

- `List<int>`、`Godot.Collections.Array<TextureConfig>` 这类泛型集合可以
- 非泛型 `IEnumerable` / `ArrayList` 这类只能枚举 `object` 的集合不可以

### 注册方法

当前会查找名称匹配、且满足以下条件的方法：

- 实例方法
- 只有一个参数
- 对宿主类型可访问
- 参数类型能接收集合元素类型

查找范围不只限于 registry 具体类型本身，还包括：

- 基类
- 直接实现的接口
- 继承链上的接口

所以像下面这种接口继承链是受支持的：

```csharp
[RegisterExportedCollection(nameof(_registry), "Registry")]
public List<IntConfig>? Values { get; } = new();
```

只要 `_registry` 的接口链上能找到兼容的 `Registry(...)` 即可。

### 明确不支持的情况

当前测试明确覆盖了这些边界：

- 只显式实现接口方法，未在具体类型上暴露可访问成员
- 注册方法存在，但对宿主类型不可访问
- 集合元素类型无法推导
- registry 成员不存在
- 注册方法名存在但签名不兼容

这些情况都会直接触发编译期诊断。

## 真实采用路径

`ai-libs/CoreGrid/global/GameEntryPoint.cs` 是当前最直接的消费者参考：

- `UiPageConfigs`
- `GameSceneConfigs`
- `PrefabSceneConfigs`
- `TextureConfigs`

这几个 `Array<T>` 成员都通过 `[RegisterExportedCollection(...)]` 声明 registry 目标，并在 `_Ready()` 里调用
`__RegisterExportedCollections_Generated()`。

这个例子说明两件事：

1. 这项能力适合“启动时集中接入一批静态配置”的节点
2. 生成器只负责循环调用，不负责 registry 的获取、生命周期或错误恢复

## 使用约束

当前最重要的约束有这些：

- 宿主类型必须是顶层 `partial class`
- 不支持嵌套类
- 生成器不会自动接入 `_Ready()` 或其他生命周期方法
- 宿主类型若已声明 `__RegisterExportedCollections_Generated()`，会触发命名冲突诊断
- 只有当至少一个成员成功通过验证时，才会生成方法

## 诊断速查

| 诊断 ID | 含义 |
| --- | --- |
| `GF_Common_Class_001` | 宿主类型不是 `partial class` |
| `GF_Common_Class_002` | 已手写 `__RegisterExportedCollections_Generated()`，与生成代码冲突 |
| `GF_AutoExport_001` | 不支持嵌套类 |
| `GF_AutoExport_002` | 指定的 registry 成员不存在 |
| `GF_AutoExport_003` | 找不到兼容且可访问的注册方法 |
| `GF_AutoExport_004` | 被标记成员不可枚举 |
| `GF_AutoExport_005` | 无法安全推导集合元素类型 |
| `GF_AutoExport_006` | 集合成员不是实例可读成员 |
| `GF_AutoExport_007` | registry 成员不是实例可读成员 |
| `GF_AutoExport_008` | `RegisterExportedCollectionAttribute` 构造参数无效 |

## 何时适合用它

适合：

- 启动入口里有多组“集合 -> registry”的重复注册代码
- 每个元素都只需要一次简单的单参数注册
- 你想把“注册到哪个 registry、调用哪个方法”直接挂在成员声明上

不适合：

- 注册流程需要排序、过滤、去重或事务式回滚
- 每个元素注册前后还要插入复杂副作用
- 注册规则依赖运行时动态上下文，而不是静态成员绑定

## 推荐阅读

1. [源码生成器总览](./index.md)
2. [配置系统](../game/config-system.md)
3. [Godot 项目生成器](./godot-project-generator.md)
4. [Godot 模块总览](../godot/index.md)
