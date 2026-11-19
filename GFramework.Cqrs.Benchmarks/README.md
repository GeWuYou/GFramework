# CQRS benchmarks

[简体中文](#简体中文) · [English](#english)

## 简体中文

用 BenchmarkDotNet 测量请求与通知等 CQRS 路径。比较结果时保留运行时、硬件与构建配置。

[查看使用说明](https://gewuyou.github.io/GFramework/zh-CN/core/cqrs.html)

## English

Measure CQRS paths such as request and notification dispatch with BenchmarkDotNet. Record the runtime, hardware, and build configuration when comparing results.

[Read the guide](https://gewuyou.github.io/GFramework/core/cqrs.html)

## 运行 · Run

```bash
dotnet run --project GFramework.Cqrs.Benchmarks -c Release
```
