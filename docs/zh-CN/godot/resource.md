---
title: Godot 资源仓储系统
description: Godot 资源仓储系统提供了 Godot Resource 的集中管理和高效加载功能。
---

# Godot 资源仓储系统

## 概述

Godot 资源仓储系统是 GFramework.Godot 中用于管理 Godot Resource
资源的核心组件。它提供了基于键值对的资源存储、批量加载、路径扫描等功能，让你可以高效地组织和访问游戏中的各类资源。

通过资源仓储系统，你可以将 Godot 的 `.tres` 和 `.res` 资源文件集中管理，支持按键快速查找、批量预加载、递归扫描目录等功能，简化资源管理流程。

**主要特性**：

- 基于键值对的资源管理
- 支持 Godot Resource 类型
- 路径扫描和批量加载
- 递归目录遍历
- 类型安全的资源访问
- 与 GFramework 架构集成

## 核心概念

### 资源仓储接口

`IResourceRepository<TKey, TResource>` 定义了资源仓储的基本操作：

```csharp
public interface IResourceRepository<in TKey, TResource> : IRepository<TKey, TResource>
    where TResource : Resource
{
    void LoadFromPath(IEnumerable<string> paths);
    void LoadFromPath(params string[] paths);
    void LoadFromPathRecursive(IEnumerable<string> paths);
    void LoadFromPathRecursive(params string[] paths);
}
```

### 资源仓储实现

`GodotResourceRepository<TKey, TResource>` 提供了完整的实现：

| 操作 | 结果与错误 |
| --- | --- |
| `Add(key, resource)` | 新增索引，重复键抛出 InvalidOperationException |
| `Get(key)`、`TryGet(key, out value)` | Get 在缺失时抛出 KeyNotFoundException，TryGet 返回 false |
| `Remove(key)`、`Clear()` | Remove 在缺失时抛出 KeyNotFoundException，Clear 清空索引 |
| `Contains(key)`、`GetAll()` | 检查键或取得当前资源集合 |

### 资源键接口

资源必须实现 `IHasKey<TKey>` 接口：

```csharp
public interface IHasKey<out TKey>
{
    TKey Key { get; }
}
```

## 显式资源登记

固定资源可由宿主通过 `[Export]` 数组和场景引用提供，在主线程启动阶段逐项 Add，再把仓储传给消费节点。完整的可运行接入见[Godot 接入教程](../tutorials/godot-integration.md)。仓储提供按键索引，资源原生引用由场景或其他明确拥有者保持。

显式登记可以在启动阶段集中处理重复键与缺失引用。目录扫描适合发现目录中的资源，扫描中的缺失目录、加载失败和重复键通过日志报告；需要完整内容集合的项目还应检查必需键。

## 基本用法

### 定义资源类型

```csharp
using Godot;
using GFramework.Core.Abstractions.Bases;

// 定义资源数据类
[GlobalClass]
public partial class ItemData : Resource, IHasKey<string>
{
    [Export]
    public string Id { get; set; } = string.Empty;

    [Export]
    public string Name { get; set; } = string.Empty;

    [Export]
    public string Description { get; set; } = string.Empty;

    [Export]
    public Texture2D? Icon { get; set; }

    [Export]
    public int MaxStack { get; set; } = 99;

    // 实现 IHasKey 接口
    public string Key => Id;
}
```

### 创建资源仓储

```csharp
using GFramework.Godot.Data;

public class ItemRepository : GodotResourceRepository<string, ItemData>
{
    public ItemRepository()
    {
        // 从指定路径加载所有物品资源
        LoadFromPath("res://data/items");
    }
}
```

### 注册到架构

```csharp
using GFramework.Godot.Architectures;
using GFramework.Godot.Data;
using Microsoft.Extensions.DependencyInjection;

public class GameArchitecture : AbstractArchitecture
{
    protected override void InstallModules(IServiceCollection services)
    {
        services.AddSingleton<IResourceRepository<string, ItemData>, ItemRepository>();
    }
}
```

### 使用资源仓储

```csharp
using Godot;
using GFramework.Godot.Extensions;

public partial class InventoryController : Node
{
    private IResourceRepository<string, ItemData> _itemRepo = null!;

    // 由父节点或组合根在 AddChild 前提供实际依赖。
    public void Configure(IResourceRepository<string, ItemData> repository)
    {
        _itemRepo = repository;
    }

    public override void _Ready()
    {

        // 使用资源
        ShowItemInfo("sword_001");
    }

    private void ShowItemInfo(string itemId)
    {
        // 获取物品数据
        if (_itemRepo.TryGet(itemId, out var itemData))
        {
            GD.Print($"物品: {itemData.Name}");
            GD.Print($"描述: {itemData.Description}");
            GD.Print($"最大堆叠: {itemData.MaxStack}");
        }
        else
        {
            GD.Print($"物品 {itemId} 不存在");
        }
    }
}
```

## 仓储与资源租约

本页的 GodotResourceRepository 按业务键索引 Godot Resource；它不负责 Core ResourceManager 的引用计数。仓储中的同步目录扫描需要在合适的 Godot 线程阶段执行，不能在 ConfigureServices 中主动构造并加载。

使用架构时，组合根在启动后解析仓储，再调用节点的 Configure 后入树；普通服务通过构造函数接收相同的仓储。配置结束后不再注册服务。

需要按类型与路径动态加载、共享在途请求及控制卸载时，使用 Core ResourceManager 的租约 API，并提供遵守 Godot 线程要求的加载器：

```csharp
manager.RegisterLoader<Texture2D>(textureLoader);
using var lease = await manager.AcquireAsync<Texture2D>("res://art/player.png", cancellationToken);
Texture2D texture = lease.Resource;
using var retained = lease.Retain(); // 独立租约，分别释放。
```

`Unload<Texture2D>`(path) 在存在活跃租约时拒绝卸载。不要在释放租约之后继续使用其中的资源；原始加载器的释放能力必须存活到所有租约归还之后。该管理器不会自动把加载和释放调度到 Godot 主线程。

## 高级用法

### 递归加载资源

```csharp
public class AssetRepository : GodotResourceRepository<string, AssetData>
{
    public AssetRepository()
    {
        // 递归加载所有子目录中的资源
        LoadFromPathRecursive("res://assets");
    }
}
```

### 多路径加载

```csharp
public class ConfigRepository : GodotResourceRepository<string, ConfigData>
{
    public ConfigRepository()
    {
        // 从多个路径加载资源
        LoadFromPath(
            "res://config/gameplay",
            "res://config/ui",
            "res://config/audio"
        );
    }
}
```

### 动态添加资源

```csharp
public partial class ResourceManager : Node
{
    private IResourceRepository<string, ItemData> _itemRepo = null!;

    // 由父节点或组合根在 AddChild 前提供实际依赖。
    public void Configure(IResourceRepository<string, ItemData> repository)
    {
        _itemRepo = repository;
    }

    public void AddCustomItem(ItemData item)
    {
        // 动态添加资源
        _itemRepo.Add(item.Id, item);
        GD.Print($"添加物品: {item.Name}");
    }

    public void RemoveItem(string itemId)
    {
        // 移除资源
        if (_itemRepo.Contains(itemId))
        {
            _itemRepo.Remove(itemId);
            GD.Print($"移除物品: {itemId}");
        }
    }
}
```

### 获取所有资源

```csharp
public partial class ItemListUI : Control
{
    private IResourceRepository<string, ItemData> _itemRepo = null!;

    // 由父节点或组合根在 AddChild 前提供实际依赖。
    public void Configure(IResourceRepository<string, ItemData> repository)
    {
        _itemRepo = repository;
    }

    public override void _Ready()
    {
        DisplayAllItems();
    }

    private void DisplayAllItems()
    {
        // 获取所有物品
        var allItems = _itemRepo.GetAll();

        GD.Print($"共有 {allItems.Count} 个物品:");
        foreach (var item in allItems)
        {
            GD.Print($"- {item.Name} ({item.Id})");
        }
    }
}
```

### 资源预加载

```csharp
public partial class GameInitializer : Node
{
    public override void _Ready()
    {
        // 这是无容器宿主的组合根；构造函数执行示例中的同步目录预加载。
        var items = new ItemRepository();
        var skills = new SkillRepository();
        var enemies = new EnemyRepository();

        var inventory = new InventoryController();
        inventory.Configure(items);
        AddChild(inventory);
        GD.Print("资源已准备完成。");
    }
}
```

### 资源缓存管理

```csharp
public class CachedResourceRepository<TKey, TResource>
    where TResource : Resource, IHasKey<TKey>
    where TKey : notnull
{
    private readonly GodotResourceRepository<TKey, TResource> _repository;
    private readonly Dictionary<TKey, TResource> _cache = new();

    public CachedResourceRepository(params string[] paths)
    {
        _repository = new GodotResourceRepository<TKey, TResource>();
        _repository.LoadFromPath(paths);
    }

    public TResource Get(TKey key)
    {
        // 先从缓存获取
        if (_cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        // 从仓储获取并缓存
        var resource = _repository.Get(key);
        _cache[key] = resource;
        return resource;
    }

    public void ClearCache()
    {
        _cache.Clear();
        GD.Print("资源缓存已清空");
    }
}
```

### 资源版本管理

```csharp
[GlobalClass]
public partial class VersionedItemData : Resource, IHasKey<string>
{
    [Export]
    public string Id { get; set; } = string.Empty;

    [Export]
    public string Name { get; set; } = string.Empty;

    [Export]
    public int Version { get; set; } = 1;

    public string Key => Id;
}

public class VersionedItemRepository : GodotResourceRepository<string, VersionedItemData>
{
    public VersionedItemRepository()
    {
        LoadFromPath("res://data/items");
        ValidateVersions();
    }

    private void ValidateVersions()
    {
        var allItems = GetAll();
        foreach (var item in allItems)
        {
            if (item.Version < 2)
            {
                GD.PrintErr($"物品 {item.Id} 版本过旧: v{item.Version}");
            }
        }
    }
}
```

### 多类型资源管理

```csharp
// 武器资源
[GlobalClass]
public partial class WeaponData : Resource, IHasKey<string>
{
    [Export] public string Id { get; set; } = string.Empty;
    [Export] public string Name { get; set; } = string.Empty;
    [Export] public int Damage { get; set; }
    public string Key => Id;
}

// 护甲资源
[GlobalClass]
public partial class ArmorData : Resource, IHasKey<string>
{
    [Export] public string Id { get; set; } = string.Empty;
    [Export] public string Name { get; set; } = string.Empty;
    [Export] public int Defense { get; set; }
    public string Key => Id;
}

// 统一管理
public class EquipmentManager
{
    private readonly IResourceRepository<string, WeaponData> _weaponRepo;
    private readonly IResourceRepository<string, ArmorData> _armorRepo;

    public EquipmentManager(
        IResourceRepository<string, WeaponData> weaponRepo,
        IResourceRepository<string, ArmorData> armorRepo)
    {
        _weaponRepo = weaponRepo;
        _armorRepo = armorRepo;
    }

    public void ShowAllEquipment()
    {
        GD.Print("=== 武器 ===");
        foreach (var weapon in _weaponRepo.GetAll())
        {
            GD.Print($"{weapon.Name}: 伤害 {weapon.Damage}");
        }

        GD.Print("=== 护甲 ===");
        foreach (var armor in _armorRepo.GetAll())
        {
            GD.Print($"{armor.Name}: 防御 {armor.Defense}");
        }
    }
}
```

### 资源热重载

```csharp
public partial class HotReloadManager : Node
{
    private IResourceRepository<string, ItemData> _itemRepo = null!;

    // 由父节点或组合根在 AddChild 前提供实际依赖。
    public void Configure(IResourceRepository<string, ItemData> repository)
    {
        _itemRepo = repository;
    }

    public void ReloadResources()
    {
        // 清空现有资源
        _itemRepo.Clear();

        // 重新加载
        var repo = _itemRepo as GodotResourceRepository<string, ItemData>;
        repo?.LoadFromPath("res://data/items");

        GD.Print("资源已重新加载");
    }

    public override void _Input(InputEvent @event)
    {
        // 按 F5 热重载
        if (@event is InputEventKey keyEvent &&
            keyEvent.Pressed &&
            keyEvent.Keycode == Key.F5)
        {
            ReloadResources();
        }
    }
}
```

## 采用约定

- 使用业务稳定的键，并为资源实现 `IHasKey<TKey>`。场景、文件路径和业务键分别维护。
- 在 Godot 主线程的启动阶段验证资源引用并建立索引，再通过构造函数或 Configure 提供给消费方。
- 必需资源可在启动时使用 Get 并处理失败；可选资源通过 TryGet 分支处理缺失。
- 显式 Add 拒绝重复键，目录扫描保留先加载的资源并报告重复项。完整内容集由宿主核对必需键。
- 仓储管理索引和引用。原生资源的持有与释放由场景或明确的宿主拥有者负责，动态资源租约见[核心资源管理](../core/resource.md)。

## 常见问题

### 问题：如何让资源支持仓储管理？

**解答**：
资源类必须实现 `IHasKey<TKey>` 接口：

```csharp
[GlobalClass]
public partial class MyResource : Resource, IHasKey<string>
{
    [Export]
    public string Id { get; set; } = string.Empty;

    public string Key => Id;
}
```

### 问题：资源文件必须是什么格式？

**解答**：
资源仓储支持 Godot 的 `.tres` 和 `.res` 文件格式：

- `.tres`：文本格式，可读性好，适合版本控制
- `.res`：二进制格式，加载更快，适合发布版本

### 问题：如何组织资源目录结构？

**解答**：
推荐按类型和功能组织：

```text
res://data/
├── items/          # 物品资源
│   ├── weapons/
│   ├── armors/
│   └── consumables/
├── enemies/        # 敌人资源
├── skills/         # 技能资源
└── levels/         # 关卡资源
```

### 问题：资源加载会阻塞主线程吗？

**解答**：
`LoadFromPath` 是同步操作，建议在游戏初始化时加载：

```csharp
public override void _Ready()
{
    // 在游戏启动时加载
    var itemRepo = new ItemRepository();
    // 接收方通过 Configure(itemRepo) 或构造函数取得它。
}
```

### 问题：如何处理重复的资源键？

**解答**：
显式 `Add(key, resource)` 遇到重复键会抛出 `InvalidOperationException`，原有登记保留。启动时逐项登记可据此拒绝配置错误。

目录扫描 `LoadFromPath`／`LoadFromPathRecursive` 遇到重复资源键时记录告警，保留先加载的资源，忽略后续重复项：

```csharp
// 日志会显示: "Duplicate key detected: item_001"
// 目录扫描忽略后加载的重复资源
```

### 问题：可以动态添加和移除资源吗？

**解答**：
可以，使用 `Add` 和 `Remove` 方法：

```csharp
// 添加资源
_itemRepo.Add("new_item", newItemData);

// 移除资源
_itemRepo.Remove("old_item");

// 清空所有资源
_itemRepo.Clear();
```

### 问题：如何实现资源的延迟加载？

**解答**：
可以创建包装类实现延迟加载：

```csharp
public class LazyResourceRepository<TKey, TResource>
    where TKey : notnull
    where TResource : Resource, IHasKey<TKey>
    where TKey : notnull
{
    private GodotResourceRepository<TKey, TResource> _repository;
    private readonly string[] _paths;
    private bool _loaded;

    public LazyResourceRepository(params string[] paths)
    {
        _paths = paths;
    }

    private void EnsureLoaded()
    {
        if (_loaded) return;

        _repository = new GodotResourceRepository<TKey, TResource>();
        _repository.LoadFromPath(_paths);
        _loaded = true;
    }

    public TResource Get(TKey key)
    {
        EnsureLoaded();
        return _repository.Get(key);
    }
}
```

## 相关文档

- [数据与存档系统](/zh-CN/game/data.md) - 数据持久化
- [Godot 架构集成](/zh-CN/godot/architecture.md) - Godot 架构基础
- [Godot 场景系统](/zh-CN/godot/scene.md) - 场景资源管理
- [资源管理系统](/zh-CN/core/resource.md) - 核心资源管理
