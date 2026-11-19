---
title: "配置生成参考"
description: "查找配置生成器的输入、输出和共享 schema 约束。"
---

# 配置生成参考 {#overview}

## 输入与输出 {#inputs}

在消费项目安装与 Game 同版本的 `GeWuYou.GFramework.Game.SourceGenerators`。将 `schemas/**/*.schema.json` 加入构建输入。每份 schema 生成配置类型、表包装和注册辅助；`GeneratedConfigCatalog` 汇总当前项目的表，`RegisterAllGeneratedConfigTables` 在首载前登记。

## Schema 关键字 {#keywords}

| 数据 | 支持的约束示例 |
| --- | --- |
| 值 | `enum`、`const`、`not` |
| 数字 | `minimum`、`maximum`、`exclusiveMinimum`、`exclusiveMaximum`、`multipleOf` |
| 字符串 | `minLength`、`maxLength`、`pattern`、`format` |
| 数组 | 单个对象形态的 `items` schema、`minItems`、`maxItems`、`uniqueItems`、`contains` |
| 对象 | `additionalProperties: false`、`required`、`minProperties`、`maxProperties`、`dependentRequired`、`dependentSchemas` |
| 对象组合 | `allOf`、对象条件 `if` / `then` / `else` |

日期、时间、时长、邮箱、URI 和 UUID 可按支持的 `format` 值约束。设计变体时使用确定的对象属性和条件规则，保持生成的 CLR 类型形状明确。出现诊断时先修复指出的 schema 路径，再重新构建。

目录结构与 YAML 示例见[类型化游戏配置](../game/config-system.md)。
