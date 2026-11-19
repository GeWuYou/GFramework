---
title: "文件存储"
description: "把业务键映射到文件，用作用域隔离子系统数据。"
---

# 文件存储 {#overview}

```csharp
using GFramework.Game.Serializer;
using GFramework.Game.Storage;

using var files = new FileStorage("GameData", new JsonSerializer(), ".json");
var profile = new ScopedStorage(files, "profile");

await profile.WriteAsync("score", 42);
int score = await profile.ReadAsync<int>("score");
```

`FileStorage` 在根目录下用相对键组织文件。将键定义为业务标识，用 `ScopedStorage` 给同一存储中的各子系统分配前缀。由创建者释放底层存储。

保存进度和偏好时，在存储之上选择[数据仓库](../game/data.md)。原生路径映射见[Godot 存储](../godot/storage.md)。
