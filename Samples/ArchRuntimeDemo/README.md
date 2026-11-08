# 原生 Arch 运行时示例

直接构造 `ArchRuntime`，用普通 `BaseSystem<World, float>` 展示实体移动、Before／Update／After 阶段、统计快照与关闭。无需 Architecture、生成器或 Godot。

```bash
dotnet run --project Samples/ArchRuntimeDemo -c Release
dotnet run --project Samples/ArchRuntimeDemo -c Release -- --benchmark
```

正常运行输出 `ARCH_RUNTIME_PASS`。基准选项测量本机空系统的统计开关成本，不代表实际游戏性能。计时循环不请求快照，以单独观察帧推进成本。

直接创建的系统和 World 由调用方释放；先停止 runtime，再释放系统，最后释放 World。使用 DI 的采用路径见 [Arch ECS 指南](../../docs/zh-CN/ecs/arch.md)；Godot 帧循环示例见 [Godot 集成工程](../GodotIntegration/README.md)。
