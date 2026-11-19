---
title: "类型化游戏配置"
description: "用 YAML 和 JSON Schema 定义、加载并查询静态游戏内容。"
---

# 类型化游戏配置 {#overview}

## 目录与项目接线 {#project}

1. 安装 `GeWuYou.GFramework.Game` 和 `GeWuYou.GFramework.Game.SourceGenerators`。
2. 将 schema 放在 `schemas/` 下，按 `x-gframework-config-path` 的目录存放 YAML，每个文件定义一条记录。
3. 使用 NuGet 生成器时，包会将 `schemas/**/*.schema.json` 加入编译输入。使用项目引用时，在消费项目中显式配置 `AdditionalFiles`。
4. 在构建后从 `GFramework.Game.Config.Generated` 命名空间使用生成的表与注册辅助。

## Schema 与数据 {#schema}

GodotIntegration 的示例给每条配置定义整数 ID 和速度范围：

<<< ../../../Samples/GodotIntegration/schemas/gameplay.schema.json{json}

<<< ../../../Samples/GodotIntegration/config/gameplay/default.yaml{yaml}

对象显式设置 `additionalProperties: false`。为数组声明单个 `items` schema。用明确的对象字段表达变体，将条件校验写成共享子集支持的对象规则。有关关键字和生成诊断，见[配置生成参考](../source-generators/schema-config-generator.md)。

## 首载与访问 {#load}

```csharp
using GFramework.Game.Config;
using GFramework.Game.Config.Generated;

var registry = new ConfigRegistry();
var loader = new YamlConfigLoader("config-root")
    .RegisterAllGeneratedConfigTables();
await loader.LoadAsync(registry);
```

`config-root` 指向同时包含 `config/` 与 `schemas/` 的目录。加载成功后，从 registry 取得生成的类型化表，用记录 ID 查询。以实例服务向消费者提供 registry。需要统一管理首载和重载句柄时使用 `GameConfigBootstrap`。

## Godot 导出与重载 {#godot}

用 `GodotYamlConfigLoader` 读取 `res://` 内的 schema 和 YAML。将相应的原始文本加入导出预设。本地开发时，对可访问的文件系统路径启用热重载。修改失败时记录诊断并保留上一份表。结束会话时释放重载句柄。

需要组合本体与扩展数据时，继续[内容包](../game/content-packs.md)。编辑配置见[VS Code 配置工具](../game/config-tool.md)。
