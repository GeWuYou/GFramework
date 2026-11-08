# CQRS 开放泛型管道示例

标准 DI 的 Scoped runtime、开放泛型行为、Scoped 依赖与生成处理器注册表共同运行。示例在退出调用方 scope 后验证依赖已释放。

```bash
dotnet run --project Samples/CqrsPipelineDemo -c Release
```

成功输出 `CQRS_PIPELINE_PASS`。不使用 Architecture，不隐式创建 scope；普通应用仍可直接调用业务服务。采用说明见 [CQRS 运行时](../../docs/zh-CN/core/cqrs.md)。
