---
title: 快速开始
---

# 快速开始

先运行仓库中可编译的最小应用，观察标准 DI 与可选 Architecture 的两种接入方式。

```bash
dotnet run --project Samples/MinimalApp -c Release
```

## 应用入口

<<< ../../../Samples/MinimalApp/Program.cs

## 服务与装配

<<< ../../../Samples/MinimalApp/OrderService.cs

<<< ../../../Samples/MinimalApp/OrderArchitecture.cs

服务通过构造函数声明依赖。组合根注册并取得服务；普通服务不需要角色、上下文或生命周期接口。需要首载和停止的组件显式登记，完成后等待 provider 或 Architecture 的异步释放。

独立能力见 [Core](../core/index.md)，游戏采用见 [Godot 完整教程](../tutorials/godot-complete-project.md)，推荐组织方式见 [进阶游戏教程](../tutorials/godot-advanced-project.md)。
