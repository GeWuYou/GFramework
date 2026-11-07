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

- Scene/UI：使用默认 SceneRouter 和 IUiService，通过显式工厂、GodotUiRoot 与强类型路由接入原生节点。
- 配置：schema 在编译期生成 `GameplayConfig`，Godot 加载器在编辑器与导出态读取相同 YAML。
- 暂停与输入：路由定义集中声明暂停、模态与输入策略，嵌套设置窗口独立释放暂停令牌；默认 GodotUiInputBridge 处理返回，重绑定使用引擎无关的绑定快照。
- 协程：世界计时器使用节点归属协程，暂停时停止计时，退树时停止运行。
- 设置与存档：使用现有设置仓库和槽位存档仓库，数据存放在 `user://runtime_sample/`。新游戏只有在明确保存后才覆盖旧槽位。

这个工程在一处注册普通 Control 工厂，界面通过实例上下文关闭自己。项目也可以用 RegisterGodotScene 注册 PackedScene，继续使用相同路由定义。GameRuntimeModule 通过 AddGamePause、AddGameUi 和 AddGameScenes 接入可选 Architecture；普通服务在就绪后使用 GetService 解析。窗口正常关闭会保留退出请求，等待当前操作与启动收尾，再等待架构停止；强制终止进程不自动保存。

## 导出与持久化

项目提供 Windows Desktop 导出预设，需要匹配版本的导出模板。预设显式包含 `config/gameplay/default.yaml` 与 `schemas/gameplay.schema.json`；这两类文本是导出态配置加载的必要资源。新增配置文件时应同步更新预设，确保它们进入导出包。

启动导出的程序，保存一次进度和设置后退出，再启动并继续。操作与目录说明见[示例使用说明](https://github.com/GeWuYou/GFramework/tree/main/Samples/GodotIntegration)。示例提供隔离数据的 --smoke 自检和独立进程 --smoke-continue 续测，覆盖原生场景、输入、暂停和持久化流程。

## 相关主题

- [场景管理](./scene.md)
- [UI 管理](./ui.md)
- [输入绑定](./input.md)
- [暂停控制](./pause.md)
- [文件存储](./storage.md)
