# GFramework.Cqrs.SourceGenerators

`GFramework.Cqrs.SourceGenerators` 用于在编译期为当前业务程序集生成 CQRS handler registry，减少运行时程序集扫描与反射注册成本。

## 模块定位

这个包是编译期生成器，不是运行时消息或处理器库。

生成器会分析当前业务程序集中的：

- `IRequestHandler<,>`
- `INotificationHandler<>`
- `IStreamRequestHandler<,>`

并生成：

- `ICqrsHandlerRegistry` 实现
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

运行时侧仍按正常方式注册程序集：

```csharp
RegisterCqrsHandlersFromAssembly(typeof(GameArchitecture).Assembly);
```

安装生成器后，运行时会优先走生成的 registry；无法静态表达的部分再走定向回退。

## 什么时候值得安装

- 你的业务程序集里 handler 数量较多
- 你希望缩小冷启动时的反射扫描范围
- 你需要把 handler 注册路径收束到编译期并保持可诊断

## 对应文档

- CQRS 栏目：[`../docs/zh-CN/core/cqrs.md`](../docs/zh-CN/core/cqrs.md)
- 源码生成器总览：[`../docs/zh-CN/source-generators/index.md`](../docs/zh-CN/source-generators/index.md)
