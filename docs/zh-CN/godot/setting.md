---
title: Godot 设置系统
description: 以当前 GFramework.Godot 源码、测试与 CoreGrid 接线为准，说明 Godot settings applicator 的职责、注册方式和运行时边界。
---

# Godot 设置系统

`GFramework.Godot` 在设置这一层做的事情很克制：它没有重新发明一套设置模型，而是给
`GFramework.Game` 的 `ISettingsModel` 提供三个 Godot 宿主 applicator：

- `GodotAudioSettings`
- `GodotGraphicsSettings`
- `GodotLocalizationSettings`

这些类型的职责是“把已经存在的设置数据应用到 Godot 引擎和框架运行时”，不是负责设置 UI、设置持久化或设置迁移。

## 当前公开入口

### `GodotAudioSettings`

`GodotAudioSettings` 从 `ISettingsModel` 读取 `AudioSettings`，再按 `AudioBusMap` 中的总线名把音量写入
`AudioServer`。

当前行为有几个关键点：

- `Master`、`Bgm`、`Sfx` 三类音量都来自 `AudioSettings`
- 应用前会把线性音量限制在 `0.0001f ~ 1f`，再转换成分贝
- 如果找不到对应 bus，当前实现只会 `GD.PushWarning(...)`，不会抛异常中断整个设置流程

`AudioBusMap` 默认值是：

- `Master`
- `BGM`
- `SFX`

如果项目里的 Godot Audio Bus 命名不同，需要在注册 applicator 时替换映射，而不是改写 applicator 本身。

### `GodotGraphicsSettings`

`GodotGraphicsSettings` 从 `ISettingsModel` 读取 `GraphicsSettings`，并把结果同步到 `DisplayServer`：

- `Fullscreen = true` 时切到 `ExclusiveFullscreen`
- 同时把 `Borderless` flag 设为 `true`
- `Fullscreen = false` 时切回窗口模式，设置窗口尺寸，并按主屏尺寸重新居中

当前实现没有扩展到分辨率档位之外的图形质量、渲染后端或平台特定显示策略。本页不再把这些未实现能力写成既成事实。

### `GodotLocalizationSettings`

`GodotLocalizationSettings` 负责把 `LocalizationSettings.Language` 同时同步到：

- Godot `TranslationServer.SetLocale(...)`
- GFramework `ILocalizationManager.SetLanguage(...)`

这一步依赖 `LocalizationMap` 把“用户可见语言值”拆成两套目标值：

- Godot locale，例如 `zh_CN`
- 框架语言码，例如 `zhs`

当前默认映射是：

- `简体中文` -> Godot `zh_CN`，框架 `zhs`
- `English` -> Godot `en`，框架 `eng`

默认应用器从设置模型显式绑定的实例上下文解析 ILocalizationManager，不查找进程级 GameContext。不同模型绑定不同上下文时，语言变更互不影响。模型必须先完成上下文注入；当前实例未安装可选 ILocalizationManager 时，仍会设置 Godot locale。

需要测试或定制引擎接线时，可传入独立的 locale 应用动作，或显式的管理器 resolver。原生 TranslationServer 调用应由宿主主线程执行。

## 最小接入路径

在配置阶段声明模型及其 applicator。下面示例由调用者提供已配置的仓库，模块在当前架构配置阶段安装：

```csharp
using GFramework.Core.Abstractions.Architectures;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Abstractions.Setting;
using GFramework.Game.Setting;
using GFramework.Godot.Setting;
using GFramework.Godot.Setting.Data;

public sealed class SettingsModule(ISettingsDataRepository repository) : IArchitectureModule
{
    public void Install(IArchitecture architecture)
    {
        var model = new SettingsModel<ISettingsDataRepository>(
            new SettingDataLocationProvider(), repository);
        model.RegisterApplicator(new GodotAudioSettings(model, new AudioBusMap()))
            .RegisterApplicator(new GodotGraphicsSettings(model))
            .RegisterApplicator(new GodotLocalizationSettings(model, new LocalizationMap()));
        architecture.RegisterModel<ISettingsModel>(model);
    }
}
```

把模块安装放入 Godot 架构的 InstallModules，或通用 Architecture 的 ConfigureServices(IServiceCollection)。不要在配置阶段用 Context 解析仓库，也不要在架构就绪后追加 RegisterModel。传入的仓库和模型实例由创建者拥有最终资源释放责任。

这条接法说明了当前边界：

- 设置数据和生命周期由 `SettingsModel` 管
- `GodotAudioSettings` / `GodotGraphicsSettings` / `GodotLocalizationSettings` 只是 applicator
- 保存、加载和迁移仍然走 `ISettingsDataRepository`、`SettingsModel.InitializeAsync()`、`SaveAllAsync()` 等 `Game`
  模块入口

## 运行时使用方式

业务代码通常不会直接 new 一次 applicator 然后立即调用，而是通过 `ISettingsSystem` 或 `ISettingsModel` 触发应用：

```csharp
using GFramework.Game.Abstractions.Setting;
using GFramework.Godot.Setting;

var settingsModel = this.GetModel<ISettingsModel>();
var audioData = settingsModel.GetData<AudioSettings>();
audioData.MasterVolume = 0.8f;
audioData.BgmVolume = 0.6f;
audioData.SfxVolume = 0.9f;

var settingsSystem = this.GetSystem<ISettingsSystem>();
await settingsSystem.Apply<GodotAudioSettings>();
```

对图形和语言设置的调用方式相同，区别只是 applicator 类型不同。

## 当前边界

- 这三个类型都不是设置数据对象；它们读取的是 `AudioSettings`、`GraphicsSettings`、`LocalizationSettings`
- 它们不负责设置持久化；是否保存到文件由 `ISettingsDataRepository` 和存储层决定
- `ApplyAsync()` 当前都只是同步推进 Godot 引擎调用后返回 `Task.CompletedTask`，不会启动后台工作线程
- `GodotAudioSettings` 依赖项目里已经存在对应 bus 名称；缺失时只会警告，不会帮你自动创建总线
- `GodotGraphicsSettings` 当前只覆盖窗口模式、尺寸和居中，不等于一个完整的图形选项系统
- `GodotLocalizationSettings` 解决的是“用户语言值 -> Godot locale / 框架语言码”双向对齐，不负责翻译资源本身的组织方式

## 什么时候应该改看别的入口

### 宿主无关设置契约

`ISettingsData`、`IResetApplyAbleSettings`、`SettingsModel`、`SettingsSystem` 与设置迁移的宿主无关说明，可继续阅读
[Game 设置系统](../game/setting.md)。

### 相关持久化文档

如果你关注统一设置文件、备份、数据位置和底层存储实现，可继续阅读：

- [Game 存储系统](../game/storage.md)
- [Godot 存储系统](./storage.md)

本页只补 Godot 宿主如何“应用”设置，不重复维护一份完整设置系统手册。

## 继续阅读

1. [Godot 运行时集成](./index.md)
2. [Game 设置系统](../game/setting.md)
3. [Godot 存储系统](./storage.md)
4. [Godot 集成教程](../tutorials/godot-integration.md)
