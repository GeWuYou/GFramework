---
title: 可运行示例
description: 运行一个包含场景、UI、暂停、配置、设置和存档的 Godot 单机工程
---

# 可运行示例

仓库内的 [Godot 运行示例](https://github.com/GeWuYou/GFramework/tree/main/Samples/GodotIntegration) 是一个完整工程，可在匹配的 Godot .NET 编辑器中打开。它使用现有框架接口完成以下流程：

**主菜单 → 新游戏或继续 → 移动与收集 → 暂停及设置 → 保存 → 返回菜单 → 重启后继续。**

## 启动工程

安装 Godot 4.7.2 .NET 版本和 .NET SDK，在仓库根目录执行：

```bash
dotnet build Samples/GodotIntegration/GodotIntegration.csproj -c Debug
godot --editor --path Samples/GodotIntegration
```

`godot` 应指向 Godot .NET 可执行文件。示例项目使用匹配版本的 `Godot.NET.Sdk`；框架运行时仍为普通库项目。打开后运行主场景，WASD 移动，Escape 打开暂停菜单。暂停键可在设置中重绑定。

## 能力与边界

- Scene/UI：继承 `SceneRouterBase` 和 `UiRouterBase`，接入项目根节点和 Godot behavior adapter。
- 配置：schema 在编译期生成 `GameplayConfig`，Godot 加载器在编辑器与导出态读取相同 YAML。
- 暂停与输入：页面交互配置持有暂停令牌，嵌套设置菜单独立释放；重绑定使用引擎无关的绑定快照。
- 协程：世界计时器使用节点归属协程，暂停时停止计时，退树时停止运行。
- 设置与存档：使用现有设置仓库和槽位存档仓库，数据存放在 `user://runtime_sample/`。新游戏只有在明确保存后才覆盖旧槽位。

这个工程使用程序化场景和 UI，便于定位接线。自己的项目可以把视图替换为 `PackedScene`，继续使用相同路由契约。窗口正常关闭会保留退出请求，等待当前操作与启动收尾，再等待架构停止；强制终止进程不自动保存。

## 导出与持久化

项目提供 Windows Desktop 导出预设，需要匹配版本的导出模板。预设显式包含 `config/gameplay/default.yaml` 与 `schemas/gameplay.schema.json`；这两类文本是导出态配置加载的必要资源。新增配置文件时应同步更新预设，确保它们进入导出包。

启动导出的程序，保存一次进度和设置后退出，再启动并继续。操作与目录说明见[示例使用说明](https://github.com/GeWuYou/GFramework/tree/main/Samples/GodotIntegration)。编译通过不等同于完成原生场景树、输入、暂停和导出运行验证。

## 相关主题

- [场景管理](./scene.md)
- [UI 管理](./ui.md)
- [输入绑定](./input.md)
- [暂停控制](./pause.md)
- [文件存储](./storage.md)
