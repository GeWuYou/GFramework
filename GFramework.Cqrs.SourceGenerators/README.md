# Cqrs.SourceGenerators

[简体中文](#简体中文) · [English](#english)

## 简体中文

为声明处理器的项目生成 CQRS 注册表。在同一个项目中安装 `GFramework.Cqrs` 和 `GFramework.Cqrs.SourceGenerators`。运行时包提供生成所需的注册表类型，并带入处理器契约 `GFramework.Cqrs.Abstractions`。

宿主与处理器在同一个项目时，在组合根调用生成的 `CqrsRegistration.CreateRegistry()`。处理器放在独立库时，由库内的公开方法调用生成的工厂并注册处理器，再由宿主调用该方法。

[查看安装和注册示例](https://gewuyou.github.io/GFramework/zh-CN/source-generators/cqrs-handler-registry-generator.html)

## English

Generate a CQRS registry in the assembly that declares the handlers. Install `GFramework.Cqrs` and `GFramework.Cqrs.SourceGenerators` in that same project. The runtime package supplies the registry types and brings in the handler contracts from `GFramework.Cqrs.Abstractions`.

Call the generated `CqrsRegistration.CreateRegistry()` in the composition root when the host and handlers share a project. For a separate handler library, add a public method in the library that invokes the generated factory and registers its handlers, then call that method from the host.

[See installation and registration examples](https://gewuyou.github.io/GFramework/source-generators/cqrs-handler-registry-generator.html)

## 安装 · Install

```bash
dotnet add package GeWuYou.GFramework.Cqrs
dotnet add package GeWuYou.GFramework.Cqrs.SourceGenerators
```
