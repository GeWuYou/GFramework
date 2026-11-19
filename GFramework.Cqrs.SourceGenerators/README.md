# Cqrs.SourceGenerators

[简体中文](#简体中文) · [English](#english)

## 简体中文

在包含处理器的消费项目中生成 CQRS 注册表。构建后用 CqrsRegistration.CreateRegistry() 取得注册表并交给运行时的 DI 登记。

[查看使用说明](https://gewuyou.github.io/GFramework/zh-CN/source-generators/cqrs-handler-registry-generator.html)

## English

Generate a CQRS registry in the project containing handlers. After the build, call CqrsRegistration.CreateRegistry() and pass the result to the runtime's DI registration.

[Read the guide](https://gewuyou.github.io/GFramework/source-generators/cqrs-handler-registry-generator.html)

## 安装 · Install

```bash
dotnet add package GeWuYou.GFramework.Cqrs.SourceGenerators
```
