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

```csharp
public class GodotResourceRepository<TKey, TResource>
    : IResourceRepository<TKey, TResource>
    where TResource : Resource, IHasKey<TKey>
    where TKey : notnull
{
    public void Add(TKey key, TResource value);
    public TResource Get(TKey key);
    public bool TryGet(TKey key, out TResource value);
    public IReadOnlyCollection<TResource> GetAll();
    public bool Contains(TKey key);
    public void Remove(TKey key);
    public void Clear();
}
```

### 资源键接口

资源必须实现 `IHasKey<TKey>` 接口：

```csharp
public interface IHasKey<out TKey>
{
    TKey Key { get; }
}
```

## 基本用法

### 定义资源类型

```csharp
using Godot;
using GFramework.Core.Abstractions.bases;

// 定义资源数据类
[GlobalClass]
public partial class ItemData : Resource, IHasKey<string>
{
    [Export]
    public string Id { get; set; }

    [Export]
    public string Name { get; set; }

    [Export]
    public string Description { get; set; }

    [Export]
    public Texture2D Icon { get; set; }

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
using GFramework.Godot.Architecture;

public class GameArchitecture : AbstractArchitecture
{
    protected override void InstallModules()
    {
        // 注册物品仓储
        var itemRepo = new ItemRepository();
        RegisterUtility<IResourceRepository<string, ItemData>>(itemRepo);
    }
}
```

### 使用资源仓储

```csharp
using Godot;
using GFramework.Godot.Extensions;

public partial class InventoryController : Node
{
    private IResourceRepository<string, ItemData> _itemRepo;

    public override void _Ready()
    {
        // 获取资源仓储
        _itemRepo = this.GetUtility<IResourceRepository<string, ItemData>>();

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
    private IResourceRepository<string, ItemData> _itemRepo;

    public override void _Ready()
    {
        _itemRepo = this.GetUtility<IResourceRepository<string, ItemData>>();
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
    private IResourceRepository<string, ItemData> _itemRepo;

    public override void _Ready()
    {
        _itemRepo = this.GetUtility<IResourceRepository<string, ItemData>>();
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
    public override async void _Ready()
    {
        await PreloadAllResources();
        GD.Print("所有资源预加载完成");
    }

    private async Task PreloadAllResources()
    {
        // 预加载物品资源
        var itemRepo = new ItemRepository();
        this.RegisterUtility<IResourceRepository<string, ItemData>>(itemRepo);

        // 预加载技能资源
        var skillRepo = new SkillRepository();
        this.RegisterUtility<IResourceRepository<string, SkillData>>(skillRepo);

        // 预加载敌人资源
        var enemyRepo = new EnemyRepository();
        this.RegisterUtility<IResourceRepository<string, EnemyData>>(enemyRepo);

        await Task.CompletedTask;
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
    public string Id { get; set; }

    [Export]
    public string Name { get; set; }

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
    [Export] public string Id { get; set; }
    [Export] public string Name { get; set; }
    [Export] public int Damage { get; set; }
    public string Key => Id;
}

// 护甲资源
[GlobalClass]
public partial class ArmorData : Resource, IHasKey<string>
{
    [Export] public string Id { get; set; }
    [Export] public string Name { get; set; }
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
    private IResourceRepository<string, ItemData> _itemRepo;

    public override void _Ready()
    {
        _itemRepo = this.GetUtility<IResourceRepository<string, ItemData>>();
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

## 最佳实践

1. **资源实现 IHasKey 接口**：确保资源可以被仓储管理
   ```csharp
   ✓ public partial class ItemData : Resource, IHasKey<string> { }
   ✗ public partial class ItemData : Resource { } // 无法使用仓储
   ```

2. **使用有意义的键类型**：根据业务需求选择合适的键类型
   ```csharp
   ✓ IResourceRepository<string, ItemData>  // 字符串 ID
   ✓ IResourceRepository<int, LevelData>    // 整数关卡号
   ✓ IResourceRepository<Guid, SaveData>    // GUID 唯一标识
   ```

3. **在架构初始化时加载资源**：避免运行时加载卡顿
   ```csharp
   protected override void InstallModules()
   {
       var itemRepo = new ItemRepository();
       RegisterUtility<IResourceRepository<string, ItemData>>(itemRepo);
   }
   ```

4. **使用递归加载组织资源**：保持目录结构清晰
   ```csharp
   // 推荐的目录结构
   res://data/
   ├── items/
   │   ├── weapons/
   │   ├── armors/
   │   └── consumables/
   └── enemies/
       ├── bosses/
       └── minions/
   ```

5. **处理资源不存在的情况**：使用 TryGet 避免异常
   ```csharp
   ✓ if (_itemRepo.TryGet(itemId, out var item)) { }
   ✗ var item = _itemRepo.Get(itemId); // 可能抛出异常
   ```

6. **合理使用资源缓存**：平衡内存和性能
   ```csharp
   // 频繁访问的资源可以缓存
   private ItemData _cachedPlayerWeapon;

   public ItemData GetPlayerWeapon()
   {
       return _cachedPlayerWeapon ??= _itemRepo.Get("player_weapon");
   }
   ```

## 常见问题

### 问题：如何让资源支持仓储管理？

**解答**：
资源类必须实现 `IHasKey<TKey>` 接口：

```csharp
[GlobalClass]
public partial class MyResource : Resource, IHasKey<string>
{
    [Export]
    public string Id { get; set; }

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

```
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
    RegisterUtility(itemRepo);
}
```

### 问题：如何处理重复的资源键？

**解答**：
仓储会记录警告但不会抛出异常：

```csharp
// 日志会显示: "Duplicate key detected: item_001"
// 后加载的资源会被忽略
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
