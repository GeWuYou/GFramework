---
title: "玩家设置"
description: "加载、应用和保存音频、界面与其他玩家偏好。"
---

# 玩家设置 {#overview}

1. 通过 `SettingsModel<TRepository>` 登记设置类型、位置、适配器和版本迁移。
2. 用 `GetData<T>` 获取模型持有的数据实例，让 applicator 使用同一实例。
3. 等待 `InitializeAsync` 加载现有数据，再通过 `SettingsSystem.ApplyAllAsync` 应用。
4. 在玩家确认时调用 `SaveAllAsync`。加载失败时保留原文件，并报告可操作的错误。

多个分区可用 `UnifiedSettingsDataRepository` 聚合到一个文件。需要切换线程的应用器通过宿主 `IGameDispatcher` 运行。参见[Godot 设置](../godot/setting.md)。
