---
title: CQRS Handler Registry 生成器
description: 为消费端程序集生成 CQRS handler registry，并在需要时附带精确 reflection fallback 元数据。
---

# CQRS Handler Registry 生成器

`GFramework.Cqrs.SourceGenerators` 会在编译期为当前业务程序集生成 `ICqrsHandlerRegistry`，让 `GFramework.Cqrs`
runtime 在注册 handlers 时优先走静态注册表，而不是先扫描整个程序集。

它服务的是 `Cqrs` 家族，不是独立运行时：

- 契约层：`GeWuYou.GFramework.Cqrs.Abstractions`
- 默认 runtime：`GeWuYou.GFramework.Cqrs`
- 编译期生成器：`GeWuYou.GFramework.Cqrs.SourceGenerators`

## 生成什么

当前生成器会分析消费端程序集中的：

- `IRequestHandler<,>`
- `INotificationHandler<>`
- `IStreamRequestHandler<,>`

然后输出两类结果：

1. 一个实现 `ICqrsHandlerRegistry` 的内部注册器类型
2. 程序集级 `CqrsHandlerRegistryAttribute`

当某些 handler 不能被生成代码安全地直接引用时，还会补发：

- 程序集级 `CqrsReflectionFallbackAttribute`

这意味着运行时会先使用生成注册器完成可静态表达的映射，再只对剩余类型做补扫，而不是退回整程序集盲扫。
如果这些 fallback handlers 本身仍可直接引用，生成器会优先发射 `typeof(...)` 形式的 fallback 元数据；当 runtime 允许同一程序集声明多个 fallback 特性实例时，mixed 场景也会拆成 `Type` 元数据和字符串元数据两段，进一步减少 runtime 再做字符串类型名回查的成本。

## 最小接入路径

安装方式保持 runtime 包与生成器包版本一致，并把生成器作为编译期依赖引入：

```xml
<ItemGroup>
  <PackageReference Include="GeWuYou.GFramework.Cqrs" Version="x.y.z" />
  <PackageReference Include="GeWuYou.GFramework.Cqrs.Abstractions" Version="x.y.z" />
  <PackageReference Include="GeWuYou.GFramework.Cqrs.SourceGenerators"
                    Version="x.y.z"
                    PrivateAssets="all"
                    ExcludeAssets="runtime" />
</ItemGroup>
```

运行时侧仍然按 `Core` 的标准入口注册程序集：

```csharp
protected override void OnInitialize()
{
    RegisterCqrsHandlersFromAssembly(typeof(GameArchitecture).Assembly);
}
```

如果你的 handlers 分布在多个业务程序集里，则改用：

```csharp
RegisterCqrsHandlersFromAssemblies(
[
    typeof(InventoryCqrsMarker).Assembly,
    typeof(BattleCqrsMarker).Assembly
]);
```

文档示例统一用 marker 类型承载程序集引用。框架本身不要求固定目录或固定命名，但团队实践里可以把这类空 marker
集中放在每个业务程序集自己的 `Application/Markers` 或等价目录，并采用 `InventoryCqrsMarker` 这类能直接看出来源
的名字，避免多人协作时拿无关业务类型充当程序集定位锚点。

## 运行时如何消费生成结果

`Cqrs` runtime 当前的注册顺序是：

1. 先读取程序集上的 `CqrsHandlerRegistryAttribute`
2. 优先激活生成的 `ICqrsHandlerRegistry`
3. 若生成元数据损坏、registry 不可激活，记录告警并回退到反射路径
4. 若存在 `CqrsReflectionFallbackAttribute`，只补扫剩余 handler
5. 同一程序集按稳定键去重，避免重复注册

这个行为由 `GFramework.Cqrs.Tests/Cqrs/CqrsHandlerRegistrarTests.cs` 和
`GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs` 共同覆盖。

## 什么时候值得安装

推荐安装：

- 业务程序集内 handler 数量较多
- 想把 handler 注册路径前移到编译期
- 希望冷启动阶段减少整程序集反射扫描
- 需要更明确地观察“哪些 handler 走静态注册，哪些只能走 fallback”

可以先不装：

- 项目体量很小，handler 很少
- 当前只做原型，尚不关心注册成本
- 你还没稳定到 `Cqrs` runtime 的最终接入边界

## fallback 边界

生成器并不会承诺“所有 handler 都能被静态表达”。

当前实现遵循一个保守原则：

- 能直接引用的 handler，生成直接注册语句
- 实现类型不能直接引用、但服务接口还能精确表达时，生成反射实现类型查找
- 服务接口本身也需要运行时解析时，生成精确 type lookup
- 当 fallback handlers 全部可直接引用且 runtime 暴露 `params Type[]` 合同时，优先发射直接 `Type` 元数据
- 当 mixed 场景同时包含可直接引用与仅能按名称恢复的 handlers，且 runtime 允许多个 fallback 特性实例时，拆分发射 `Type` 元数据和字符串元数据
- 其余场景统一回退到字符串元数据，避免 mixed 场景漏注册
- 只有在 runtime 提供 `CqrsReflectionFallbackAttribute` 合同时，才允许发射依赖 fallback 的结果

如果当前编译环境缺少这个 fallback 合同，而某些 handler 又必须依赖它，生成器会报：

- `GF_Cqrs_001`

这条诊断的含义不是“某个 handler 写错了”，而是“当前 runtime 合同不足以安全承载这轮生成结果”。

## 源码与 API 阅读入口

如果你要核对生成器对外暴露的契约，优先看这些类型：

- `GFramework.Cqrs.ICqrsHandlerRegistry`
- `GFramework.Cqrs.CqrsHandlerRegistryAttribute`
- `GFramework.Cqrs.CqrsReflectionFallbackAttribute`
- `GFramework.Cqrs.SourceGenerators.Cqrs.CqrsHandlerRegistryGenerator`

模块族入口见：

- [CQRS 运行时](../core/cqrs.md)
- [源码生成器总览](./index.md)
