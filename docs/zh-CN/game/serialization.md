---
title: "JSON 序列化"
description: "在存储和数据服务中使用默认 JSON 序列化器。"
---

# JSON 序列化 {#overview}

`GFramework.Game.Serializer.JsonSerializer` 基于 Newtonsoft.Json。为静态类型调用使用 `ISerializer`，在类型只能在运行时取得时使用 `IRuntimeTypeSerializer`。在组合根将同一序列化策略交给需要协作的存储和仓库。

用于存档时，由[存档仓库](../game/data.md)管理版本化文档。小型文件读写示例见[文件存储](../game/storage.md)。
