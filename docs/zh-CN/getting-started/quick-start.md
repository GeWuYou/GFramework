---
title: "快速开始"
description: "运行最小 C# 示例，观察标准 DI 与 Architecture 的接入。"
---

# 快速开始 {#overview}

在仓库根目录运行以下命令。构建所需 SDK 见[环境](../getting-started/installation.md#requirements)。

```bash
dotnet run --project Samples/MinimalApp -c Release
```

输出先显示独立服务的问候日志，再记录架构启动与示例订单的处理。

## 运行入口 {#program}

<<< ../../../Samples/MinimalApp/Program.cs

## 注册业务服务 {#services}

`OrderArchitecture` 在 `ConfigureServices` 登记服务。宿主完成启动后调用服务，退出时等待 `DestroyAsync` 完成。

<<< ../../../Samples/MinimalApp/OrderArchitecture.cs

在自己的项目中，按[安装指南](../getting-started/installation.md)添加 Core，再把业务类和构造函数依赖登记到组合根。接入引擎时，继续[运行 Godot 示例](../tutorials/godot-integration.md)。
