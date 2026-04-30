---
title: 序列化系统
description: 以当前 GFramework.Game.JsonSerializer 与 JsonSerializerTests 为准，说明 JSON 序列化器的配置生命周期和使用边界。
---

# 序列化系统

`GFramework.Game` 当前在序列化这一层的默认公开入口只有 `JsonSerializer`。

它实现的是：

- `ISerializer`
- `IRuntimeTypeSerializer`

它不负责：

- schema 驱动配置生成
- 存档槽位管理
- 文件路径或目录布局

这些能力分别属于 source generator、repository 和 storage。

## 当前公开入口

### `JsonSerializer`

`JsonSerializer` 基于 `Newtonsoft.Json`，既支持泛型 API，也支持运行时类型 API：

```csharp
using GFramework.Core.Abstractions.Serializer;
using GFramework.Game.Serializer;

ISerializer serializer = new JsonSerializer();
IRuntimeTypeSerializer runtimeSerializer = new JsonSerializer();
```

当前测试覆盖的核心行为包括：

- 普通对象可正常 round-trip
- 注入的 `JsonSerializerSettings` 会直接生效
- `Settings` 与 `Converters` 暴露的是同一个活动配置实例
- 运行时类型序列化 / 反序列化可处理 `object + Type`
- 非法 JSON 会抛出带目标类型上下文的 `InvalidOperationException`
- 非法参数（例如空字符串）会保留 `ArgumentException`
- 运行时类型序列化允许 `null`，输出 `"null"`

## 配置生命周期

这里最需要先确认的是序列化器的配置生命周期。

`JsonSerializer` 不会复制你传入的 `JsonSerializerSettings`。它会直接持有并复用这份实例，以及里面的 `Converters` 集合。

这意味着推荐模式是：

1. 在组合根创建序列化器
2. 一次性完成 settings / converters 配置
3. 再把同一个实例注册给存储、repository 或 architecture

推荐写法：

```csharp
using Newtonsoft.Json;

var settings = new JsonSerializerSettings
{
    Formatting = Formatting.Indented,
    NullValueHandling = NullValueHandling.Ignore
};

settings.Converters.Add(new CoordinateConverter());

var serializer = new JsonSerializer(settings);
```

不推荐写法：

```csharp
var serializer = architecture.GetUtility<IRuntimeTypeSerializer>();

// 序列化器已经被多个组件共享后，再继续改 converter，容易让并发调用看到不稳定配置。
((JsonSerializer)serializer).Converters.Add(new LateBoundConverter());
```

## 最小接入路径

### 作为底层 serializer 注册

当前更常见的采用方式不是“业务代码直接到处调 serializer”，而是把它注册给存储和 repository 复用：

```csharp
using GFramework.Core.Abstractions.Serializer;
using GFramework.Game.Serializer;

var serializer = new JsonSerializer();

architecture.RegisterUtility<ISerializer>(serializer);
architecture.RegisterUtility<IRuntimeTypeSerializer>(serializer);
```

然后由：

- `FileStorage`
- `UnifiedSettingsDataRepository`
- 其他依赖 `ISerializer` / `IRuntimeTypeSerializer` 的组件

统一复用这一份实例。

### 直接处理运行时类型

当业务层拿到的是 `object + Type` 组合，而不是静态泛型类型时，再使用运行时 API：

```csharp
var serializer = new JsonSerializer();

object data = new PlayerState
{
    Name = "Runtime",
    Level = 11
};

var json = serializer.Serialize(data, data.GetType());
var restored = serializer.Deserialize(json, data.GetType());
```

## 与存储系统的关系

`FileStorage` 已经会调用注入的 `ISerializer` 自己完成对象读写，因此当前默认接法里：

- 你可以直接 `storage.WriteAsync("profile/player", profile)`
- 不需要先手工 `serializer.Serialize(profile)` 再把字符串写回存储

手工显式调用 `Serialize(...)` 更适合这些场景：

- 需要把 JSON 发到网络或日志
- 需要和外部文本格式做中转
- 需要直接调试序列化输出内容

如果目标只是本地持久化，优先让 `IStorage` / repository 复用 serializer。

## 与配置系统的关系

不要把 `JsonSerializer` 和 `Game` 的 YAML 配置系统混在一起：

- `JsonSerializer`
  - 负责运行时对象 JSON 序列化
- `Game.SourceGenerators + YamlConfigLoader`
  - 负责 schema 驱动的配置表生成与 YAML 读取

如果你的目标是静态内容配置表，而不是运行时持久化对象，请改看 [配置系统](./config-system.md)。

如果你在配置系统里进一步碰到更复杂的 schema shape，也要尽快回到配置系统主文档和 raw YAML / schema 本体继续设计。当前默认采用路径面向的是与 `GFramework.Game` Runtime 和 `GFramework.Game.SourceGenerators` 对齐的共享 schema 子集，不是任意 `JSON Schema` 的全量支持。

## 当前边界

- 当前公开默认实现只有 JSON，没有内建 MessagePack、Binary 或 ProtoBuf 实现
- `JsonSerializer` 负责序列化，不负责对象版本迁移；版本迁移属于 `SettingsModel<TRepository>` 或 `SaveRepository<TSaveData>`
- 序列化器共享后应视为只读配置对象，避免在运行期继续修改 settings / converters
- 如果配置设计依赖 `oneOf`、`anyOf`、非 `false` 的 `additionalProperties`（例如省略或 `true`），或其他需要开放对象形状与联合分支的复杂约束，请直接按配置系统主文档回到 raw YAML / schema 方案处理，而不是把这些场景归到序列化层

## 继续阅读

1. [存储系统](./storage.md)
2. [数据与存档系统](./data.md)
3. [配置系统](./config-system.md)
4. [Game 入口](./index.md)
