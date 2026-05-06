# GFramework.Game.SourceGenerators

`GFramework.Game.SourceGenerators` 负责把 `schemas/**/*.schema.json` 转成游戏内容配置类型和表包装代码。

它服务的核心场景是：让 `YAML` 配置、`JSON Schema`、运行时加载器和工具链共享一套结构定义。

## 模块定位

这个包是编译期生成器，不是运行时库。

它会在编译期读取 schema，并生成：

- 配置数据类型
- 对应的表包装类型
- 与 `GFramework.Game.Config` 运行时协作的访问辅助代码

这里要先明确一条采用边界：`GFramework.Game.SourceGenerators` 服务的是当前与 `GFramework.Game`
Runtime 对齐的共享 schema 子集，而不是任意 `JSON Schema` 的全量实现。它的目标是让配置生成、运行时校验和工具链维持同一份可落地契约，而不是把所有 schema 组合能力都映射成生成类型。

## 包关系

- 运行时：`GFramework.Game`
- 生成器：`GFramework.Game.SourceGenerators`
- 公共生成器支撑：`GFramework.SourceGenerators.Common`

如果你的项目还会使用 `[Log]`、`[ContextAware]` 或 Core 侧上下文注入特性，还需要同时安装
`GFramework.Core.SourceGenerators`。

## 目录与输入约定

当前项目结构显示该生成器主要围绕以下代码组织：

- `Config/SchemaConfigGenerator.cs`
- `Diagnostics/ConfigSchemaDiagnostics.cs`
- `GeWuYou.GFramework.Game.SourceGenerators.targets`

消费者项目的推荐目录约定：

```text
GameProject/
├─ config/
│  └─ monster/
│     └─ slime.yaml
└─ schemas/
   └─ monster.schema.json
```

默认情况下，打包产物会通过 `targets` 把 `schemas/**/*.schema.json` 纳入 `AdditionalFiles`。

## XML 阅读入口

下面这份目录视图用于帮助你定位 `GFramework.Game.SourceGenerators` 的生成器入口；具体诊断消息、生成输出和兼容性语义仍建议回到源码与测试继续核对。

| 阅读主题 | 代表类型 | 阅读重点 |
| --- | --- | --- |
| 配置生成入口 | `SchemaConfigGenerator` | 看 schema 到配置类型 / 表包装 / 注册辅助代码的生成入口 |
| 诊断与失败边界 | `ConfigSchemaDiagnostics` | 看生成器会抛出的诊断类别与失败边界 |

## 最小接入路径

```xml
<ItemGroup>
  <PackageReference Include="GeWuYou.GFramework.Game" Version="x.y.z" />
  <PackageReference Include="GeWuYou.GFramework.Game.SourceGenerators"
                    Version="x.y.z"
                    PrivateAssets="all"
                    ExcludeAssets="runtime" />
</ItemGroup>
```

如果你在仓库内用 `ProjectReference` 调试，仍需要把对应 `targets` 接进消费者项目。

## 什么时候使用它

- 你想把静态游戏内容维护成 `YAML`
- 你希望在编译期拿到强类型配置访问入口
- 你希望运行时加载、schema 校验和编辑工具链共用同一份结构定义

如果你的 schema 设计依赖下面这些场景，就不属于当前默认采用路径：

- `oneOf`
- `anyOf`
- 非 `false` 的 `additionalProperties`
- `patternProperties`
- `propertyNames`
- `unevaluatedProperties`
- 其他依赖开放对象形状、联合分支或属性合并的复杂组合约束

遇到这些情况时，建议先回到 [配置系统文档](../docs/zh-CN/game/config-system.md) 和原始 schema / YAML 设计本体，确认是否需要调整配置建模方式，而不是默认期待生成器直接支持完整 `JSON Schema` 语义。

## 对应文档

- 配置系统：[配置系统文档](../docs/zh-CN/game/config-system.md)
- 源码生成器总览：[源码生成器文档首页](../docs/zh-CN/source-generators/index.md)
