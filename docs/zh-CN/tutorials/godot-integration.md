---
title: Godot 接入教程
---

# Godot 接入教程

Godot 提供场景树与主循环；框架能力由组合根接入。普通服务使用构造注入，引擎创建的节点由共同拥有者显式传入所需能力。

最小示例运行入口：

```bash
godot --path Samples/GodotIntegration --editor
```

架构装配：

<<< ../../../Samples/GodotIntegration/DemoArchitecture.cs

节点接线：

<<< ../../../Samples/GodotIntegration/DemoController.cs

开始与停止时等待框架生命周期，原生节点操作必须在 Godot 主线程执行。游戏数据与偏好使用隔离用户目录，场景退出后释放订阅、暂停句柄和应用资源。

完整玩法接入见 [完整项目](./godot-complete-project.md)，职责拆分见 [进阶游戏](./godot-advanced-project.md)。
