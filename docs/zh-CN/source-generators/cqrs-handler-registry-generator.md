---
title: CQRS Handler Registry 生成器
description: 为消费端程序集生成 CQRS handler registry，并在需要时附带精确 reflection fallback 元数据。
---

# CQRS Handler Registry 生成器

`GFramework.Cqrs.SourceGenerators` 会在编译期为当前业务程序集生成 `ICqrsHandlerRegistry`，让 `GFramework.Cqrs`
runtime 在注册 handlers 时优先走静态注册表；当运行时合同允许时，也会把 request / stream 分发可直接复用的 invoker
元数据前移到编译期，而不是总是先扫描整个程序集或在首次分发时再走反射绑定。

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

当运行时暴露对应合同、且当前 handler 可被安全静态表达时，生成注册器还可以继续暴露：

- generated request invoker provider / descriptor
- generated stream invoker provider / descriptor

当某些 handler 不能被生成代码安全地直接引用时，还会补发：

- 程序集级 `CqrsReflectionFallbackAttribute`

这意味着运行时会先使用生成注册器完成可静态表达的映射；对 request 与 stream 分发来说，也会优先消费 generated invoker
descriptor。只有当前类型对没有 generated metadata，或 registry / fallback 无法覆盖时，才继续回到既有反射 binding 或补扫路径，而不是退回整程序集盲扫。
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
3. 若生成注册器同时提供 request invoker provider / descriptor，registrar 会把这些 request invoker 元数据预先登记到 dispatcher 缓存
4. 若生成注册器同时提供 stream invoker provider / descriptor，runtime 也会优先消费对应的 generated stream invoker 元数据；未命中时仍回退到既有反射 stream binding
5. 若生成元数据损坏、registry 不可激活，记录告警并回退到反射路径
6. 若存在 `CqrsReflectionFallbackAttribute`，优先按其中携带的 `Type` 或类型名补扫剩余 handler；若元数据为空或只保留 marker 语义，则退回整程序集补扫
7. 同一程序集按稳定键去重，避免重复注册

这个行为由
[运行时注册流程测试](https://github.com/GeWuYou/GFramework/blob/main/GFramework.Cqrs.Tests/Cqrs/CqrsHandlerRegistrarTests.cs)
和
[生成器行为测试](https://github.com/GeWuYou/GFramework/blob/main/GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs)
共同覆盖。

## 什么时候值得安装

推荐安装：

- 业务程序集内 handler 数量较多
- 想把 handler 注册路径前移到编译期
- 想把 request / stream 分发里可静态确定的 invoker metadata 一并前移到编译期
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

## 生成策略层级

把这个生成器理解成“静态注册 or 整程序集扫描”的二选一，会低估它的收益。当前策略实际上分成四层：

1. 直接静态注册
   - handler 接口和实现类型都能被生成代码安全引用
2. 实现类型定向反射查找
   - handler 接口还能精确表达，但实现类型只能在运行时按具体类型名恢复
3. service type 精确运行时查找
   - handler 接口本身也需要运行时构造，但仍能把查找范围收窄到具体 service type
4. 程序集级 fallback 元数据
   - 只有前面几层都无法覆盖的剩余 handler，才交给 `CqrsReflectionFallbackAttribute`

这意味着安装生成器后，并不要求“所有 handler 都可直接引用”才有收益。很多只能部分静态表达的项目，仍然可以把大部分注册路径前移到编译期，再对少数复杂类型做定向补扫。

## 哪些场景通常不会直接退回整程序集扫描

下列类型形态经常仍然能保留精细化注册，而不是立刻退回整程序集盲扫：

| 场景 | 常见结果 |
| --- | --- |
| 私有嵌套 handler，但对外 handler 接口仍可直接引用 | 生成器改为按实现类型定向反射查找 |
| 响应或参数里包含需要运行时恢复的隐藏类型 | 生成器改为精确 service type runtime lookup |
| mixed 场景里同时存在可直接引用和仅能按名称恢复的 fallback handlers | 生成器拆分 `Type` 元数据和字符串元数据，减少后续字符串回查 |
| 响应类型写成 `dynamic` | 生成器会按 `System.Object` 归一化，而不是发射非法的 `typeof(dynamic)` |

相反，pointer、function-pointer 这类无法安全重建的类型形态，不属于这里承诺的精确生成边界。

如果当前编译环境缺少这个 fallback 合同，而某些 handler 又必须依赖它，生成器会报：

- `GF_Cqrs_001`

这条诊断的含义不是“某个 handler 写错了”，而是“当前 runtime 合同不足以安全承载这轮生成结果”。
遇到它时，优先按这个顺序判断：

1. 当前消费端是否已经引用支持 `CqrsReflectionFallbackAttribute` 的 `GFramework.Cqrs` runtime
2. 当前项目里是否存在只能部分静态表达的 handler 类型
3. 如果确实不想引入 fallback 合同，是否需要把这类 handler 改成更容易被生成器直接引用的公开形态

`CqrsReflectionFallbackAttribute` 出现也不等于“运行时一定回到整程序集扫描”。只有 fallback 元数据为空、或旧版只保留 marker 语义时，runtime 才会退回整程序集补扫；当元数据里已经带了具体 `Type` 或类型名时，runtime 会优先按这些剩余 handler 做定向补扫。

## 源码与 API 阅读入口

如果你要核对生成器对外暴露的契约，优先看这些类型：

- `GFramework.Cqrs.ICqrsHandlerRegistry`
- `GFramework.Cqrs.CqrsHandlerRegistryAttribute`
- `GFramework.Cqrs.CqrsReflectionFallbackAttribute`
- `GFramework.Cqrs.ICqrsRequestInvokerProvider`
- `GFramework.Cqrs.ICqrsStreamInvokerProvider`
- `GFramework.Cqrs.SourceGenerators.Cqrs.CqrsHandlerRegistryGenerator`

模块族入口见：

- [CQRS 运行时](../core/cqrs.md)
- [源码生成器总览](./index.md)
