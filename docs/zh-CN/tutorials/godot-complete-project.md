---
title: Godot 完整项目
description: 运行使用官方教程资源和 GFramework 通用服务制作的 Dodge the Creeps 小游戏。
---

# Godot 完整项目

本教程以仓库中的 [Dodge the Creeps 工程](https://github.com/GeWuYou/GFramework/tree/main/Samples/DodgeTheCreeps) 为基础。玩法参考 [Godot 官方首个 2D 游戏教程](https://docs.godotengine.org/en/stable/getting_started/first_2d_game/index.html)，使用官方资源包，菜单和运行流程采用 GFramework 服务。

## 启动工程

使用与项目 SDK 一致的 Godot .NET 版本，在仓库根构建后打开工程：

```bash
dotnet build Samples/DodgeTheCreeps/DodgeTheCreeps.csproj -c Debug
godot --editor --path Samples/DodgeTheCreeps
```

`godot` 指已安装的 Godot .NET 可执行文件。首次打开会导入资源。版本、操作、导出和数据位置以 [工程说明](https://github.com/GeWuYou/GFramework/tree/main/Samples/DodgeTheCreeps) 为准。

## 项目分工

| 入口 | 责任 |
| --- | --- |
| `DodgeApp` | 组合根、异步业务动作、场景切换、存档与关闭 |
| `DodgeRoutes` | 强类型页面、窗口和对话框定义 |
| `DodgeMenus` | 普通原生 Control 工厂与视图业务动作 |
| `Gameplay/` | 角色、敌人、生成、碰撞与计分 |
| `DodgeProgress`、`DodgePreferences` | 可序列化的进度和偏好数据 |
| `art/`、`fonts/` | 官方教程素材和对应许可 |

框架负责 UI 状态、导航、暂停请求、视图清理、持久化基础与宿主调度。项目负责具体玩法、业务选择、视图布局和用户反馈。

## 路由和视图

路由和调用方复用相同定义：

<<< ../../../Samples/DodgeTheCreeps/DodgeRoutes.cs{csharp}

`DodgeMenus.Register` 将普通 Control 工厂登记到 `UiRegistry`。工厂收到强类型参数和实例上下文，关闭按钮调用上下文的 `CloseAsync`，确认按钮通过 `CompleteAsync` 返回业务值。

HUD 是独立窗口，不占用页面历史；暂停与设置使用模态窗口和分别持有的暂停请求；主菜单与结束画面使用页面。修改显示顺序只调整 `Layer`，输入、暂停和多实例由各自选项决定。

采用步骤见 [通用 UI](../game/ui.md) 和 [Godot UI](../godot/ui.md)。

## 游戏与保存

开始新游戏时创建内存进度；继续游戏先读取槽位，再创建世界和恢复角色位置与分数。玩家明确保存后才覆盖槽位。返回菜单的确认对话框通过强类型结果决定后续业务操作。

存档存储使用 `user://`，读取失败保持原文件并展示错误。关闭流程等待已接纳的保存、输入和导航工作，再释放服务和原生宿主。

## 扩展练习

1. 添加带参数的新页面：定义路由、登记工厂、发起导航。
2. 添加结果对话框：定义参数与返回值，在业务流程中等待结果。
3. 调整暂停菜单中的设置：维持嵌套暂停和返回规则。
4. 增加一个实时数据展示：打开时订阅，关闭时解绑。

每项练习完成后编译工程并实际操作对应流程。布局、字体、音频和游戏手感可在工程内直接调整，素材许可见 [资源说明](https://github.com/GeWuYou/GFramework/blob/main/Samples/DodgeTheCreeps/ASSETS.md)。

## 相关主题

- [Godot 运行时示例](../godot/runtime-sample.md)
- [存档教程](./save-system.md)
- [暂停实践](./pause-system.md)
- [设置系统](../game/setting.md)
