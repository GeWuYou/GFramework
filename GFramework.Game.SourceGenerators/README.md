# Game.SourceGenerators

[简体中文](#简体中文) · [English](#english)

## 简体中文

将 JSON Schema 编译为配置类型、表和注册辅助。把 schema 放入消费项目的 `schemas/` 目录。

`GFramework.Game.SourceGenerators` 负责生成类型及注册辅助，由 `GFramework.Game` 在运行时加载 YAML。先在消费项目加入这两个包和 schema，再通过生成的 `RegisterAllGeneratedConfigTables` 注册数据表。

[查看使用说明](https://gewuyou.github.io/GFramework/zh-CN/source-generators/schema-config-generator.html)

## English

Compile JSON Schema into configuration types, tables, and registration helpers. Put schemas in the consuming project’s `schemas/` directory.

`GFramework.Game.SourceGenerators` produces types and registration helpers. `GFramework.Game` loads the YAML at runtime. Add both packages and the schema to the consuming project, then use the generated `RegisterAllGeneratedConfigTables` to register tables.

[Read the guide](https://gewuyou.github.io/GFramework/source-generators/schema-config-generator.html)

## 安装 · Install

```bash
dotnet add package GeWuYou.GFramework.Game.SourceGenerators
```
