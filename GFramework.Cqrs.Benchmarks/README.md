# GFramework.Cqrs.Benchmarks

该模块承载 `GFramework.Cqrs` 的独立性能基准工程，用于持续比较运行时 dispatch、publish、cold-start 与后续 generator / pipeline 收口的成本变化。

## 目的

- 为 `GFramework.Cqrs` 建立独立于 NUnit 集成测试的 BenchmarkDotNet 基线
- 参考 `ai-libs/Mediator/benchmarks` 的场景组织方式，逐步补齐 request、notification、stream 与初始化成本对比
- 为后续吸收 `Mediator` 的 dispatch 设计、fixture 组织和对比矩阵提供可重复验证入口

## 当前内容

- `Program.cs`
  - benchmark 命令行入口
- `Messaging/Fixture.cs`
  - 运行前输出并校验场景配置
- `Messaging/RequestBenchmarks.cs`
  - direct handler、`GFramework.Cqrs` runtime 与 `MediatR` 的 request steady-state dispatch 对比
- `Messaging/RequestPipelineBenchmarks.cs`
  - `0 / 1 / 4` 个 pipeline 行为下，direct handler、`GFramework.Cqrs` runtime 与 `MediatR` 的 request steady-state dispatch 对比
- `Messaging/RequestStartupBenchmarks.cs`
  - `Initialization` 与 `ColdStart` 两组 request startup 成本对比，补齐与 `Mediator` comparison benchmark 更接近的 startup 维度
- `Messaging/RequestInvokerBenchmarks.cs`
  - direct handler、`GFramework.Cqrs` reflection runtime、handwritten generated-invoker runtime 与 `MediatR` 的 request steady-state dispatch 对比
- `Messaging/StreamInvokerBenchmarks.cs`
  - direct handler、`GFramework.Cqrs` reflection runtime、handwritten generated-invoker runtime 与 `MediatR` 的 stream 完整枚举对比
- `Messaging/NotificationBenchmarks.cs`
  - `GFramework.Cqrs` runtime 与 `MediatR` 的单处理器 notification publish 对比
- `Messaging/StreamingBenchmarks.cs`
  - direct handler、`GFramework.Cqrs` runtime 与 `MediatR` 的 stream request 完整枚举对比

## 最小使用方式

```bash
dotnet run --project GFramework.Cqrs.Benchmarks/GFramework.Cqrs.Benchmarks.csproj -c Release
```

也可以通过 `BenchmarkDotNet` 过滤器只运行某一类场景。

## 后续扩展方向

- generated invoker provider 与纯反射 dispatch 对比
- generated stream invoker provider 与纯反射建流对比
- registration / service lifetime 矩阵
- request / stream 的真实 source-generator 产物与 handwritten generated provider 对照
