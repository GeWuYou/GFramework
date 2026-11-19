# CQRS Pipeline Demo

[简体中文](#简体中文) · [English](#english)

## 简体中文

用生成注册表、开放泛型行为和 scoped 依赖分发请求，释放调用方作用域后输出 CQRS_PIPELINE_PASS。

[查看教程](https://gewuyou.github.io/GFramework/zh-CN/core/cqrs.html)

## English

Dispatch a request through a generated registry and open-generic behaviors with a scoped dependency. The sample prints CQRS_PIPELINE_PASS after the caller scope releases its dependency.

[Read the walkthrough](https://gewuyou.github.io/GFramework/core/cqrs.html)

## 运行 · Run

```bash
dotnet run --project Samples/CqrsPipelineDemo -c Release
```
