# GFramework.Cqrs.SourceGenerators

`GFramework.Cqrs.SourceGenerators` 用于在编译期为当前业务程序集生成 CQRS handler registry，减少运行时程序集扫描与反射注册成本。

## 模块定位

这个包是编译期生成器，不是运行时消息或处理器库。

生成器会分析当前业务程序集中的：

- `IRequestHandler<,>`
- `INotificationHandler<>`
- `IStreamRequestHandler<,>`

并生成：

- `ICqrsHandlerRegistry` 实现及直接创建入口 `CqrsRegistration.CreateRegistry()`
- 在运行时合同允许时，额外生成 request / stream invoker provider 与 descriptor 元数据
- 程序集级 `CqrsHandlerRegistryAttribute`
- 必要时的 `CqrsReflectionFallbackAttribute` 元数据

## 包关系

- 运行时：`GFramework.Cqrs`
- 契约层：`GFramework.Cqrs.Abstractions`
- 生成器：`GFramework.Cqrs.SourceGenerators`

不安装这个包也可以正常使用 CQRS；区别只在于运行时会更多依赖反射扫描注册 handlers。

## 当前代码入口

仓库内该包的主要实现位于：

- `Cqrs/CqrsHandlerRegistryGenerator.cs`

它会在可以安全生成静态注册器时前移注册工作；对无法由生成代码直接引用的 handler，则通过 reflection fallback 元数据让运行时做定向补扫，而不是整程序集盲扫。
当 fallback handler 本身仍可直接引用时，生成器会优先发射 `typeof(...)` 形式的 fallback 元数据；如果 runtime 允许同一程序集声明多个 fallback 特性实例，mixed 场景也会拆成 `Type` 元数据和字符串元数据两段，进一步减少运行时按类型名回查程序集的成本。
当 runtime 同时暴露 request / stream invoker provider 契约时，生成注册器还会为可直接静态表达的 `IRequestHandler<,>` 与
`IStreamRequestHandler<,>` 发射对应 descriptor 与开放静态 invoker 方法，让 runtime 在首次创建 request / stream binding 时优先消费这些编译期元数据；未命中时仍保持既有反射 binding 创建语义。

## 最小接入路径

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

同一业务程序集中的组合根直接登记生成实例：

```csharp
services.AddCqrsHandlers(GFramework.Generated.Cqrs.CqrsRegistration.CreateRegistry());
```

直接入口使用普通构造和编译好的强类型 invoker 委托，不进行生成注册器反射激活。若仍有 handler 需要 fallback，该入口明确拒绝；调用方显式选择 `AddCqrsHandlersFromAssembly` 进行定向回退。
如果当前 runtime 合同已经包含 request / stream invoker provider seam，generated registry 还会把这两类 invoker 元数据一并前移到编译期。

## 什么时候值得安装

- 你的业务程序集里 handler 数量较多
- 你希望缩小冷启动时的反射扫描范围
- 你需要把 handler 注册路径收束到编译期并保持可诊断

## 对应文档

- CQRS 栏目：[CQRS 文档](../docs/zh-CN/core/cqrs.md)
- 源码生成器总览：[源码生成器文档首页](../docs/zh-CN/source-generators/index.md)

runtime 与生成器应同时升级。生成 request / stream invoker 前会验证当前 `Create<TResponse>` 工厂契约；若选择的 runtime 缺少匹配工厂，生成器报告编译错误 `GF_Cqrs_002` 并停止生成注册器，不为旧包降级生成。
