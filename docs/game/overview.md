# Game 游戏模块概览

GFramework.Game 为游戏开发提供专门的功能模块，包括资产管理、存储系统、序列化等核心游戏功能。

## 核心特性

### 🎮 游戏专用模块

- **架构模块系统** - 可插拔的游戏功能模块
- **资产管理** - 统一的资源注册和查询系统
- **存储系统** - 分层的数据持久化方案
- **序列化** - 高性能的数据序列化支持

### 🏗️ 模块化设计

```csharp
public class AudioModule : AbstractModule
{
    public override void Install(IArchitecture architecture)
    {
        architecture.RegisterSystem(new AudioSystem());
        architecture.RegisterUtility(new AudioUtility());
    }
}
```

## 核心组件

### 模块系统

- **AbstractModule** - 模块基类
- **ArchitectureModule** - 架构模块接口
- **ModuleManager** - 模块管理器

### 存储系统

- **[ScopedStorage](/game/storage/scoped-storage)** - 分层存储
- **IStorage** - 存储接口
- **FileStorage** - 文件存储实现

### 资源管理

- **[AbstractAssetCatalogUtility](/game/assets/asset-catalog)** - 资源目录
- **AssetFactory** - 资源工厂
- **ResourceLoader** - 资源加载器

### 序列化

- **[JsonSerializer](/game/serialization/json-serializer)** - JSON 序列化
- **BinarySerializer** - 二进制序列化
- **CustomSerializer** - 自定义序列化

## 使用场景

Game 模块专为游戏开发设计：

- **2D/3D 游戏** - 支持各种类型的游戏项目
- **存档系统** - 完善的存档和读档功能
- **资源配置** - 集中的资源管理和加载
- **数据持久化** - 游戏数据的保存和恢复

## 安装

```bash
# 需要先安装 Core 模块
dotnet add package GeWuYou.GFramework.Core
dotnet add package GeWuYou.GFramework.Game
dotnet add package GeWuYou.GFramework.Game.Abstractions
```

## 快速示例

```csharp
using GFramework.Game.storage;
using GFramework.Game.assets;

// 存储系统使用
public class GameDataManager
{
    private IStorage _playerStorage;
    
    public GameDataManager(IStorage rootStorage)
    {
        _playerStorage = new ScopedStorage(rootStorage, "player");
    }
    
    public void SavePlayerData(PlayerData data)
    {
        _playerStorage.Write("profile", data);
        _playerStorage.Write("inventory", data.Inventory);
    }
}

// 资源管理使用
public class GameAssetCatalog : AbstractAssetCatalogUtility
{
    public override void Initialize()
    {
        RegisterSceneUnit("Player", "res://scenes/Player.tscn");
        RegisterAsset<Texture2D>("PlayerTexture", "res://textures/player.png");
    }
}
```

## 学习路径

建议按以下顺序学习 Game 模块：

1. **[模块系统](/game/modules/architecture-modules)** - 了解模块化架构
2. **[存储系统](/game/storage/scoped-storage)** - 掌握数据持久化
3. **[资源管理](/game/assets/asset-catalog)** - 学习资源处理
4. **[序列化](/game/serialization/json-serializer)** - 理解数据序列化

## 与 Core 的关系

```
GFramework.Core (提供基础架构)
    ↓ 扩展
GFramework.Game (提供游戏功能)
    ↓ 集成
GFramework.Godot (提供引擎支持)
```

Game 模块建立在 Core 模块之上，为游戏开发提供专门的功能支持。

## 性能优化

- **缓存机制** - 智能缓存减少重复加载
- **批量操作** - 支持批量数据处理
- **异步支持** - 非阻塞的 I/O 操作
- **内存管理** - 自动资源回收和管理

## 下一步

- [深入了解模块系统](/game/modules/architecture-modules)
- [查看存储系统文档](/game/storage/scoped-storage)
- [学习资源管理](/game/assets/asset-catalog)
- [查看 API 参考](/api-reference/game-api)