---
title: Game 存储系统
description: 以当前 GFramework.Game 源码与持久化测试为准，说明 FileStorage 与 ScopedStorage 的职责、路径语义和复用方式。
---

# Game 存储系统

`GFramework.Game` 在存储这一层只提供宿主无关的 `IStorage` 默认实现和作用域包装器。

当前真正对外需要理解的入口只有两个：

- `FileStorage`
  - 负责 `key -> 文件路径 -> 序列化内容` 的落盘读写
- `ScopedStorage`
  - 负责给同一份底层存储加前缀作用域，避免不同子系统直接拼字符串抢同一片键空间

它们不负责：

- 设置 section 的聚合语义
- 存档槽位目录约定
- 业务数据迁移

这些都属于上层 repository。

## 当前公开入口

### `FileStorage`

`FileStorage` 是 `IStorage` 的默认文件系统实现。按当前源码，它的职责比较集中：

- 把业务 key 映射到根目录下的层级文件路径
- 通过构造函数注入的 `ISerializer` 负责对象序列化和反序列化
- 对同一目标路径使用 `IAsyncKeyLockManager` 做细粒度串行化
- 写入时先落 `.tmp` 临时文件，再原子替换目标文件
- 自动创建父目录
- 拒绝包含 `..` 的非法 key，并清理路径段中的非法文件名字符

默认文件扩展名是 `.dat`，也可以在构造时改成 `.json` 或其他后缀：

```csharp
using GFramework.Core.Abstractions.Storage;
using GFramework.Game.Serializer;
using GFramework.Game.Storage;

var serializer = new JsonSerializer();
IStorage storage = new FileStorage("GameData", serializer, ".json");
```

### `ScopedStorage`

`ScopedStorage` 不额外实现一套落盘逻辑，只是给底层 `IStorage` 包一层前缀。

它适合做的是：

- 把 `settings/`、`profiles/`、`runtime-cache/` 这类键空间隔离开
- 让多个 repository 或 utility 共用同一份根存储
- 避免项目层到处手写 `"settings/xxx"`、`"save/slot_1/xxx"` 之类的字符串拼接

当前实现还支持继续嵌套：

```csharp
var rootStorage = new FileStorage("GameData", new JsonSerializer(), ".json");
var settingsStorage = new ScopedStorage(rootStorage, "settings");
var audioStorage = settingsStorage.Scope("audio");

await audioStorage.WriteAsync("master", 0.8f);
```

最终实际写入的 key 会是 `settings/audio/master`。

## 路径语义

### key 到文件路径的映射

`FileStorage` 会把 key 中的 `/` 当成目录分隔符，把最后一段作为文件名，并自动附加扩展名。

例如：

```text
key: profile/player
root: GameData
extension: .json
```

会落到：

```text
GameData/profile/player.json
```

这意味着 key 的语义应该保持“逻辑路径”，而不是“完整文件名”。不要在业务层再自己补一遍 `.json`，否则会得到双重后缀。

### 安全边界

当前实现会：

1. 把 `\` 统一成 `/`
2. 拒绝包含 `..` 的 key
3. 清理每个路径段中的非法文件名字符

这套规则能挡住明显的路径逃逸和非法文件名问题，但它不代替业务层做目录规划。哪些 key 属于设置、存档还是缓存，仍应由上层模块统一约定。

### 同步与异步 API

`Read`、`Write`、`Exists`、`Delete` 这些同步方法只是对异步 API 的阻塞包装。

在 UI 线程或带同步上下文的宿主中，优先使用：

- `ReadAsync<T>()`
- `WriteAsync<T>()`
- `ExistsAsync()`
- `DeleteAsync()`

只有在无法继续异步传播时，再退回同步封装。

## 最小接入路径

如果你只想先拿到一个可复用的本地持久化底座，最短路径如下：

```csharp
using GFramework.Core.Abstractions.Serializer;
using GFramework.Core.Abstractions.Storage;
using GFramework.Game.Serializer;
using GFramework.Game.Storage;

var serializer = new JsonSerializer();
IStorage storage = new FileStorage("GameData", serializer, ".json");

await storage.WriteAsync("profiles/player", new Dictionary<string, int>
{
    ["level"] = 12
});

var loaded = await storage.ReadAsync<Dictionary<string, int>>("profiles/player");
```

如果项目里同时有设置、存档和运行时缓存，推荐先在组合根把作用域拆开：

```csharp
var serializer = new JsonSerializer();
var rootStorage = new FileStorage("GameData", serializer, ".json");

var settingsStorage = new ScopedStorage(rootStorage, "settings");
var saveStorage = new ScopedStorage(rootStorage, "saves");
var cacheStorage = new ScopedStorage(rootStorage, "runtime-cache");
```

不过在默认仓库接法里，项目通常不需要直接创建 `saveStorage` 这种 scoped instance，因为 `SaveRepository<TSaveData>`
会再根据 `SaveConfiguration` 自己组织槽位目录。

## 与上层 repository 的关系

`FileStorage` / `ScopedStorage` 是持久化最底层，不是最终采用入口。当前更常见的实际分工是：

- `DataRepository`
  - 每个 `IDataLocation` 对应一份独立持久化对象
- `UnifiedSettingsDataRepository`
  - 把多个设置 section 聚合到同一个统一文件里保存
- `SaveRepository<TSaveData>`
  - 负责存档槽位、文件名和迁移链

也就是说：

- 业务层如果想保存一份独立数据，优先看 [`data.md`](./data.md)
- 业务层如果想保存设置，优先看 [`setting.md`](./setting.md)
- 业务层如果只是需要底层存储实现，才直接依赖 `IStorage`

## 当前边界

- `FileStorage` 已经会通过注入的 `ISerializer` 自动序列化对象；默认接法不需要先手工 `Serialize(...)` 再把字符串写回
- `FileStorage` 负责目录列举与目录创建，但不负责“列出所有存档槽位”的业务语义
- `ScopedStorage` 只做 key 前缀，不做权限、事务或迁移控制
- 锁粒度是“当前实例内的目标路径”，不是跨进程文件锁
- 原子写入只覆盖单文件替换，不等于多文件事务

## 继续阅读

1. [数据与存档系统](./data.md)
2. [设置系统](./setting.md)
3. [序列化系统](./serialization.md)
4. [Game 入口](./index.md)
