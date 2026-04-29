---
title: Godot 存储系统
description: 以当前 GFramework.Godot 源码与 CoreGrid 接线为准，说明 GodotFileStorage 的职责、路径边界和最小接入方式。
---

# Godot 存储系统

`GFramework.Godot` 在存储这一层提供的核心入口只有 `GodotFileStorage`。

它实现 `GFramework.Game` 侧统一的 `IStorage` 契约，负责把序列化后的读写、目录列举和路径处理接到 Godot 的
`res://`、`user://` 和普通文件系统路径上，而不是另外提供一套独立的“Godot 专属存档框架”。

## 当前公开入口

### `GodotFileStorage`

`GodotFileStorage` 的当前职责比较集中：

- 对外暴露 `IStorage` 约定的 `Read`、`Write`、`Exists`、`Delete`、目录列举与目录创建能力
- 识别并保留 Godot 虚拟路径：`res://`、`user://`
- 对普通文件系统路径做段级清理，并拒绝包含 `..` 的非法 key
- 使用 `IAsyncKeyLockManager` 对“绝对路径 / Godot 路径”做按 key 细粒度串行化

构造函数默认会在未注入锁管理器时创建内部 `AsyncKeyLockManager`。这意味着：

- 同一个 `GodotFileStorage` 实例内，不同文件可以并发访问
- 同一个目标路径的读写 / 删除会被串行化
- 锁作用域只限当前进程内的当前实例，不是跨进程文件锁

## 路径语义

### `res://`

`res://` 更适合作为只读资源或配置源目录。

当前实现不会阻止你把它传给 `ReadAsync`、`ExistsAsync` 之类的方法，但在导出后的 Godot 项目里，`res://`
通常不应被当作用户可写存储根目录。存档、设置和运行时缓存应优先落到 `user://`。

### `user://`

`user://` 是当前推荐的可写路径：

- 用户设置
- 存档
- 运行时缓存
- 导出后仍需要读写的 JSON / YAML / 二进制数据

如果调用 `ListDirectoriesAsync()` 或 `ListFilesAsync()` 时传入空字符串，当前实现会默认从 `user://` 根开始列举。

### 普通文件系统路径

当 key 不是 Godot 路径时，`GodotFileStorage` 会：

1. 把 `\` 统一成 `/`
2. 拒绝包含 `..` 的 key
3. 按路径段清理非法文件名字符
4. 在写入或建目录前自动补父目录

这条路径更适合测试、桌面工具链或显式指定外部目录的宿主环境，不建议在项目业务层自己重新拼装一套路径清理逻辑。

## 最小接入路径

当前更常见的接法，是先注册同一个序列化器和存储实例，再让设置仓库、存档仓库等上层组件复用它：

```csharp
using GFramework.Core.Abstractions.Serializer;
using GFramework.Core.Abstractions.Storage;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Storage;
using GFramework.Godot.Storage;
using Godot;

var jsonSerializer = new JsonSerializer();
architecture.RegisterUtility<ISerializer>(jsonSerializer);

var storage = new GodotFileStorage(jsonSerializer);
architecture.RegisterUtility<IStorage>(storage);

architecture.RegisterUtility(new UnifiedSettingsDataRepository(
    storage,
    jsonSerializer,
    new DataRepositoryOptions
    {
        BasePath = ProjectSettings.GetSetting("application/config/save/setting_path").AsString(),
        AutoBackup = true
    }));

architecture.RegisterUtility<ISaveRepository<GameSaveData>>(new SaveRepository<GameSaveData>(
    storage,
    new SaveConfiguration
    {
        SaveRoot = ProjectSettings.GetSetting("application/config/save/save_path").AsString(),
        SaveSlotPrefix = ProjectSettings.GetSetting("application/config/save/save_slot_prefix").AsString(),
        SaveFileName = ProjectSettings.GetSetting("application/config/save/save_file_name").AsString()
    }));
```

这里的分工是：

- `GodotFileStorage` 负责底层 key -> 文件读写
- `UnifiedSettingsDataRepository` 负责设置节聚合与持久化
- `SaveRepository<TSaveData>` 负责存档结构和保存槽位语义

不要把 `GodotFileStorage` 本身写成“设置系统”或“存档系统”的 owner。

## 什么时候应该改看别的入口

### 配置 YAML / schema 文本加载

如果你的目标是读取 `res://` 下的 YAML 配置，并在导出态同步到运行时缓存，可继续阅读
[Game 配置系统](../game/config-system.md) 中 `GodotYamlConfigLoader` 的接法。

这类场景的重点不是通用键值存储，而是：

- `res://` 与 `user://` 缓存切换
- 生成器表元数据
- 热重载可用性边界

### 通用存储契约

宿主无关的 `IStorage`、`ScopedStorage`、`FileStorage` 和统一数据仓库语义，可继续阅读
[Game 存储系统](../game/storage.md)。

本页只补 Godot 宿主差异，不重复维护一份跨宿主 API 手册。

## 当前边界

- 同步 `Read` / `Write` / `Delete` / `Exists` 只是对异步方法的阻塞包装；在带同步上下文的宿主里，优先使用异步 API
- `GodotFileStorage` 不负责文件扩展名约定、作用域前缀或保存槽位策略，这些属于上层 repository / scoped storage
- 路径安全只覆盖当前 key 的格式校验与路径段清理，不代替业务层的目录规划
- 当前实现支持目录列举与目录创建，但没有额外的“监视目录变化”或“自动迁移目录结构”能力

## 继续阅读

1. [Godot 运行时集成](./index.md)
2. [Game 存储系统](../game/storage.md)
3. [Game 配置系统](../game/config-system.md)
4. [Godot 集成教程](../tutorials/godot-integration.md)
