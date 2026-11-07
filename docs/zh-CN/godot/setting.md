---
title: Godot 设置系统
description: 通过普通设置服务和显式宿主调度应用音频、图形与本地化数据。
---

# Godot 设置系统

Godot 设置应用器读取通用 `ISettingsModel` 中的数据，并调用原生 API。应用流程通过共享 `IGameDispatcher` 回到宿主线程。

## 创建应用器

```csharp
using GFramework.Game.Data;
using GFramework.Game.Setting;
using GFramework.Godot.Setting;
using GFramework.Godot.Setting.Data;

var model = new SettingsModel<UnifiedSettingsDataRepository>(locations, repository, dispatcher: dispatcher);
model.RegisterApplicator(new GodotAudioSettings(model, new AudioBusMap()));
model.RegisterApplicator(new GodotGraphicsSettings(model));
model.RegisterApplicator(new GodotLocalizationSettings(model, new LocalizationMap(), localizationManager));
await model.InitializeAsync(cancellationToken);

var settings = new SettingsSystem(model, dispatcher: dispatcher);
await settings.ApplyAllAsync(cancellationToken);
```

`locations` 提供数据类型到 section 的映射，`repository` 负责持久化。`localizationManager` 是可选的框架本地化服务；Godot 语言应用始终通过显式应用器完成，不再读取模型上下文。

## 应用语义

- `GodotAudioSettings` 使用 `AudioBusMap` 中的 Master、BGM 和 SFX 总线名，将线性音量应用到音频服务器。
- `GodotGraphicsSettings` 应用分辨率、全屏、窗口位置和垂直同步等现有图形数据。
- `GodotLocalizationSettings` 使用 `LocalizationMap` 将选项映射到 Godot locale 和可选框架语言码。

每个 `ApplyAsync` 都接受末尾取消令牌。通过 `SettingsSystem.ApplyAllAsync` 执行时，每个应用器分别经过 dispatcher；一个独立应用器失败后继续尝试其他项，最终向调用方返回聚合错误。

## 加载和保存

先登记数据与应用器，再加载；全部 section 准备成功后才更新当前数据。损坏数据或不完整迁移向调用方报告，现有文件保留。保存由明确操作调用 `SaveAllAsync`，失败显示给用户，关闭流程等待已接纳的写入。

文件格式与通用系统一致，Godot 可变数据放在 `user://`。详细语义见 [设置系统](../game/setting.md) 和 [Godot 存储](./storage.md)。

## 相关主题

- [数据与存档](../game/data.md)
- [Godot UI](./ui.md)
- [运行时示例](./runtime-sample.md)
