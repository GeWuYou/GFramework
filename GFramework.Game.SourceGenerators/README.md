# GFramework.Game.SourceGenerators

`GFramework.Game.SourceGenerators` 负责把 `schemas/**/*.schema.json` 转成游戏内容配置类型和表包装代码。

它服务的核心场景是：让 `YAML` 配置、`JSON Schema`、运行时加载器和工具链共享一套结构定义。

## 模块定位

这个包是编译期生成器，不是运行时库。

它会在编译期读取 schema，并生成：

- 配置数据类型
- 对应的表包装类型
- 与 `GFramework.Game.Config` 运行时协作的访问辅助代码

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

## XML 覆盖基线

下面这份 inventory 记录的是 `2026-04-23` 对 `GFramework.Game.SourceGenerators` 做的一轮轻量 XML 盘点结果：只统计公开类型声明是否带 XML 注释，用来建立生成器入口；具体诊断消息、生成输出和兼容性语义仍需要回到源码与测试继续核对。

| 类型族 | 基线状态 | 代表类型 | 阅读重点 |
| --- | --- | --- | --- |
| `Config/` | `1/1` 个类型声明已带 XML 注释 | `SchemaConfigGenerator` | 看 schema 到配置类型 / 表包装 / 注册辅助代码的生成入口 |
| `Diagnostics/` | `1/1` 个类型声明已带 XML 注释 | `ConfigSchemaDiagnostics` | 看生成器会抛出的诊断类别与失败边界 |

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

## 对应文档

- 配置系统：[配置系统文档](../docs/zh-CN/game/config-system.md)
- 源码生成器总览：[源码生成器文档首页](../docs/zh-CN/source-generators/index.md)
