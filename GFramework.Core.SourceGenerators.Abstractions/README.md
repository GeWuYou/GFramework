# GFramework.Core.SourceGenerators.Abstractions

此内部项目定义 GenerateEnumExtensions 和 Priority 等编译期输入特性，配合 Core.SourceGenerators 使用，不提供运行时能力。

Enums 定义枚举扩展输入；Bases 定义优先级输入。NuGet 消费项目安装生成器包，由该包携带所需特性源码；仓库内项目引用可显式引用本项目；框架运行时及其抽象项目不引用生成器或使用这些特性。

采用方式见 [源码生成器](../docs/zh-CN/source-generators/index.md)、[枚举扩展](../docs/zh-CN/source-generators/enum-generator.md) 与 [优先级](../docs/zh-CN/source-generators/priority-generator.md)。
