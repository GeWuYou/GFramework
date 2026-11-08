# GFramework.Core.SourceGenerators

此包为编译期工具，提供枚举扩展、Priority 实现及相关分析器。它不参与运行期依赖解析。最低编译器版本以项目 Roslyn 依赖和打包配置为准。

## 包与目录

- Core.SourceGenerators.Abstractions：消费端使用的特性契约。
- SourceGenerators.Common：公共生成器支撑。
- Enums：枚举扩展生成。
- Bases / Analyzers / Diagnostics：优先级生成及编译诊断。

## 采用

消费项目显式安装生成器包并使用其随附的特性源码，无需单独安装内部的 Abstractions 项目。普通业务依赖通过构造函数声明，日志显式注入 ILogger&lt;T&gt;，模块在组合根调用 AddModule。

运行时和契约项目不引用生成器，也不使用生成器特性。Game schema、CQRS handler registry 和 Godot 节点接线属于各自包。

## 文档

- [生成器指南](../docs/zh-CN/source-generators/index.md)
- [枚举扩展](../docs/zh-CN/source-generators/enum-generator.md)
- [优先级](../docs/zh-CN/source-generators/priority-generator.md)
