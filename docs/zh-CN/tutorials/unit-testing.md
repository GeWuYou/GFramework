---
title: "测试游戏规则与用例"
description: "在纯 C# 测试中验证规则，在 Godot 中验证原生接线。"
---

# 测试游戏规则与用例 {#overview}

从应用用例的可观察结果写断言。例如模拟持久化失败，确认当前局面保持可用；加载合法快照时确认调用了世界恢复端口。使用明确的小型测试替身传入依赖。

```bash
dotnet test Samples/DodgeTheCreeps.Advanced.Tests -c Release
```

原生节点、信号和文件路径用[Godot 自检](../tutorials/godot-integration.md#smoke)覆盖。
