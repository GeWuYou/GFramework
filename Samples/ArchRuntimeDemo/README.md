# Arch Runtime Demo

[简体中文](#简体中文) · [English](#english)

## 简体中文

使用原生 Arch 系统更新实体并输出 ARCH_RUNTIME_PASS。传入的 World 和系统由调用方释放。加 -- --benchmark 可测量空系统的统计开关成本。

[查看教程](https://gewuyou.github.io/GFramework/zh-CN/ecs/arch.html)

## English

Update an entity with a native Arch system and look for ARCH_RUNTIME_PASS. The caller owns its World and systems. Add -- --benchmark to measure the cost of enabling statistics for an empty system.

[Read the walkthrough](https://gewuyou.github.io/GFramework/ecs/arch.html)

## 运行 · Run

```bash
dotnet run --project Samples/ArchRuntimeDemo -c Release
```
