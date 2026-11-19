---
title: "运行 Arch ECS 系统"
description: "装配 World、驱动原生系统并检查示例结果。"
---

# 运行 Arch ECS 系统 {#overview}

## 直接装配 {#direct}

<<< ../../../Samples/ArchRuntimeDemo/Program.cs#native-runtime

```bash
dotnet run --project Samples/ArchRuntimeDemo -c Release
```

看到 `ARCH_RUNTIME_PASS` 表示原生系统已执行且实体移动结果正确。运行时借用传入的 World 和系统。调用方先停止运行时，再释放系统，最后释放 World。

## 使用 DI {#di}

在组合根调用 `AddArch` 配置运行时及选项，并登记系统。在宿主循环按实际时间步调用 `Update`。开启 `EnableStatistics` 后可读取实例指标；启用前按自己的场景测量花费。
