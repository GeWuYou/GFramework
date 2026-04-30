---
title: VS Code 配置工具
description: 说明 GFramework AI-First 配置工作流对应的 VS Code 工具入口、工作区约定、常用命令与使用边界。
---

# VS Code 配置工具

`GFramework Config Tool` 是面向 AI-First 配置工作流的 VS Code 扩展。它不是新的运行时模块，而是把
`config/`、`schemas/`、轻量校验、表单预览和批量编辑收敛到编辑器侧的一条辅助工作流。

如果你正在维护 `GFramework.Game` 的 YAML + JSON Schema 配置，这个工具通常比纯手写 YAML 更适合做日常浏览、
校验和批量修改；如果你要做复杂嵌套结构或超出当前支持子集的 schema 设计，仍然应该回到原始 YAML 和 schema 文件。

## 适合什么时候用

- 你已经采用 `config/**/*.yaml` + `schemas/**/*.schema.json`
- 你希望在 VS Code 里快速浏览配置域和对应 schema
- 你需要批量修改同一配置域的顶层标量或标量数组字段
- 你想先走表单预览，再决定是否回到 raw YAML

不适合：

- 项目不使用 `GFramework.Game` 的配置工作流
- 需要完整 JSON Schema 编辑器，而不是当前仓库落地的稳定子集
- 需要在编辑器里处理更深层对象数组嵌套，且不接受回退到 raw YAML

## 工作区约定

默认目录约定是：

```text
GameProject/
├─ config/
│  ├─ monster/
│  │  ├─ slime.yaml
│  │  └─ goblin.yaml
│  └─ item/
│     └─ potion.yaml
└─ schemas/
   ├─ monster.schema.json
   └─ item.schema.json
```

扩展默认会把：

- `config/` 视为配置根目录
- `schemas/` 视为 schema 根目录

如果你的项目用了不同目录，可以通过工作区设置覆盖。

## 扩展当前提供什么

### Explorer 视图

扩展会在 VS Code Explorer 中提供一个独立视图，用来浏览配置域和配置文件。

### 常用命令

当前命令面向这几类操作：

- 刷新配置树
- 打开 raw YAML
- 打开对应 schema
- 打开轻量表单预览
- 对单个配置域做批量编辑
- 运行全量校验

如果你更关心“当前 schema 和 YAML 是否仍一致”，优先使用全量校验；如果你只是定位单个字段或注释，优先使用
Explorer + 表单预览。

### 当前能力范围

仓库中的 `tools/gframework-config-tool` 当前提供以下能力：

- 浏览 `config/` 目录
- 打开 raw YAML 文件
- 打开匹配的 schema 文件
- 根据 VS Code 当前界面语言在英文和简体中文之间切换主要工具界面文本
- 对嵌套对象中的必填字段、未知字段、基础标量类型、标量数组和对象数组元素做轻量校验
- 对嵌套对象字段、对象数组、顶层标量字段和顶层标量数组提供轻量表单入口
- 在表单中渲染已有 YAML 注释，并允许直接编辑字段级 YAML 注释
- 对带 `x-gframework-ref-table` 的字段提供引用 schema / 配置域 / 引用文件跳转入口
- 对空配置文件提供基于 schema 的示例 YAML 初始化入口
- 对同一配置域内的多份 YAML 文件执行批量字段更新
- 在表单入口中显示 `title / description / default / const / enum / x-gframework-ref-table（UI 中显示为 ref-table） / multipleOf / pattern / format / uniqueItems / contains / minContains / maxContains / minProperties / maxProperties / dependentRequired / dependentSchemas / allOf / if / then / else` 元数据；批量编辑入口当前只暴露顶层可批量改写字段所需的基础信息
- 对 `additionalProperties: false` 提供闭合对象边界校验，并在遇到 `oneOf` / `anyOf` 或其他当前未收口的组合形状时明确提示该 schema 不属于当前工具支持子集

当前表单入口适合编辑嵌套对象中的标量字段、标量数组，以及对象数组中的对象项。

对象数组编辑器当前支持：

- 新增和删除对象项
- 编辑对象项中的标量字段
- 编辑对象项中的标量数组
- 编辑对象项中的嵌套对象字段
- 编辑对象项内部继续嵌套的对象数组，只要这些内层对象数组项仍然由对象、标量字段、标量数组和嵌套对象组成

如果对象数组中混入了标量项，或者更深层结构超出当前 schema 子集，表单入口会明确提示该路径需要回退到 raw YAML。

当前批量编辑入口仍刻意限制在“同域文件统一改动顶层标量字段和顶层标量数组”，避免复杂结构批量写回时破坏人工维护的 YAML 排版。

### 工具边界与 Runtime 契约

这个扩展是编辑器侧的辅助层，不定义 `GFramework.Game` 的 Runtime 契约。

- Runtime / Source Generator 是否接受某份 schema，决定了它是否属于当前配置系统的正式支持范围
- 工具里的表单、hint、校验和批量编辑，只是把这套已落地契约搬到 VS Code 中帮助你更快发现问题
- 如果工具界面暂时没有把某个 shape 做成可视化编辑入口，不代表 Runtime 会自动接受更宽松的 schema；同样，如果 Runtime / Generator 已明确拒绝某类关键字，工具也不会把它包装成可继续编辑的“可用能力”

日常采用时，建议把它理解为“优先用工具加速已支持子集的维护；遇到边界时立刻回到 schema + raw YAML 本体确认”。

### 最小接入示例与兼容 / 迁移说明

项目里至少需要准备三类内容：

- `config/<domain>/*.yaml`：实际配置文件
- `schemas/<domain>.schema.json`：与该配置域对应的 schema
- VS Code 工作区里的 `GFramework Config Tool` 扩展，以及与 schema 保持一致的 `x-gframework-ref-table` 引用约定

最小目录可以从下面这个形态起步：

```text
GameProject/
├─ config/
│  └─ monster/
│     └─ slime.yaml
└─ schemas/
   └─ monster.schema.json
```

最小 schema 示例：

```json
{
  "type": "object",
  "additionalProperties": false,
  "required": ["id", "name", "rarity", "dropItems"],
  "properties": {
    "id": {
      "type": "integer",
      "title": "Monster Id",
      "description": "Primary monster key.",
      "default": 1
    },
    "name": {
      "type": "string",
      "title": "Display Name",
      "minLength": 1
    },
    "rarity": {
      "type": "string",
      "enum": ["common", "elite", "boss"],
      "default": "common"
    },
    "spawnTime": {
      "type": "string",
      "format": "time"
    },
    "dropItems": {
      "type": "array",
      "uniqueItems": true,
      "items": {
        "type": "string"
      }
    },
    "rewardTableId": {
      "type": "string",
      "x-gframework-ref-table": "reward-table"
    }
  }
}
```

对应的 YAML 初始文件可以保持很小：

```yaml
id: 1
name: Slime
rarity: common
spawnTime: 08:30:00Z
dropItems:
  - potion
rewardTableId: starter-reward
```

推荐接入顺序：

1. 在 VS Code 中打开包含 `config/` 和 `schemas/` 的工作区
2. 如果目录不是默认值，先设置 `gframeworkConfig.configPath` 与 `gframeworkConfig.schemasPath`
3. 通过 Explorer 打开目标 YAML 或 schema，先跑一次全量校验
4. 对空 YAML 使用“基于 schema 的示例 YAML 初始化”，或直接从 raw YAML 开始录入
5. 需要统一改同域顶层标量字段时，再进入批量编辑

迁移自纯 raw YAML 工作流时，至少先检查下面几件事：

- `additionalProperties` 是否显式设置为 `false`；省略或 `true` 不属于当前共享支持子集
- schema 是否依赖 `oneOf` / `anyOf`；这些组合关键字会被 Runtime / Generator / Tooling 直接拒绝
- 对象数组里是否混入标量项，或是否存在更深、更异构的数组结构
- Runtime / Source Generator 是否已经接受这份 schema，而不是只有编辑器里“暂时看起来能写”

当 schema 仍在共享支持子集内，但某段编辑路径已经超出轻量表单可视化边界时，优先回到 raw YAML；不要把“工具暂时没有表单入口”误判成“运行时契约已放宽”。

## 推荐工作流

### 1. 浏览配置与 schema

先从 Explorer 里进入目标配置文件，再根据需要：

- 打开 raw YAML
- 跳转到对应 schema
- 进入轻量表单预览

### 2. 先校验，再批量改

如果你准备改同一配置域下多份文件，推荐顺序是：

1. 先运行全量校验
2. 再进入配置域批量编辑
3. 批量修改完成后回到 raw YAML 或表单确认结果

### 3. 嵌套结构优先分层处理

当前工具支持：

- 顶层标量字段
- 顶层标量数组
- 嵌套对象字段
- 对象数组
- object-focused `if` / `then` / `else`、`dependentRequired`、`dependentSchemas`、`allOf`
- `contains` / `minContains` / `maxContains`
- `additionalProperties: false`

如果你进入更深层对象数组嵌套，当前更稳妥的做法通常是：

1. 用 Explorer 找到目标文件
2. 先看表单预览确认字段结构
3. 再回到 raw YAML 完成最终编辑

以下 shape 目前也建议直接回退到 raw YAML，并同时检查 schema 是否仍在当前共享支持子集内：

- 需要表达 `oneOf` / `anyOf` 这类会改变生成类型形状的组合关键字
- 需要 `additionalProperties` 的其他形态，而不是当前明确支持的 `additionalProperties: false`
- 需要在 `allOf`、`dependentSchemas`、`if` / `then` / `else` 中引入父对象未声明的新字段
- 需要比当前对象数组编辑器更深、更异构的数组结构

## 工作区设置

当前公开设置只有两个：

```json
{
  "gframeworkConfig.configPath": "config",
  "gframeworkConfig.schemasPath": "schemas"
}
```

- `gframeworkConfig.configPath`
  - 配置根目录，默认是 `config`
- `gframeworkConfig.schemasPath`
  - schema 根目录，默认是 `schemas`

## 当前边界

当前扩展重点覆盖的是仓库已经验证过的最小工作流：

- 工作区默认只取第一个 workspace folder
- 校验聚焦仓库当前支持的 schema 子集
- 表单预览支持对象数组，但更深的嵌套对象数组仍可能需要回退到 raw YAML
- 批量编辑当前聚焦顶层标量和顶层标量数组字段
- 共享约束里只支持闭合对象边界 `additionalProperties: false`；`oneOf` / `anyOf` 等改变生成形状的组合关键字会被明确拒绝

因此，最稳妥的理解方式是：

- 用它加速“浏览、定位、轻量校验、批量维护”
- 不把它当成完整替代 YAML / schema 编辑的唯一入口

## 适用范围

当前这套工具更适合已经定义好 schema、需要校验、轻量表单和批量改写能力的内容维护场景，尤其适合由开发者或技术策划主导的游戏项目配置工作流。

以下场景目前仍建议保留 raw YAML 编辑，或由项目补充专用工具：

- 需要更完整的 JSON Schema 支持
- 需要覆盖更复杂的数组结构和更深层 schema 关键字

## 工具形态建议

对当前仓库已经落地的工作流而言，`VS Code Extension` 形态已经可以覆盖 schema 校验、轻量表单、批量编辑和 raw YAML 回退这条采用路径。

如果你的团队出现以下需求，再评估独立 `Config Studio` 会更合适：

- 配置维护主要由非开发角色承担，希望进一步降低 VS Code 的安装和使用门槛
- 需要更重的表格视图、跨表可视化关系编辑、复杂审批流或离线发布流程
- 插件形态已经明显受限于 VS Code Webview / Extension API，而不是 schema 与工作流本身
- 已经沉淀出稳定的 schema 元数据约定，足以支撑单独工具的长期维护

## 继续阅读

- [游戏内容配置系统](./config-system.md)
- [Game 模块](./index.md)
- [安装配置](../getting-started/installation.md)
