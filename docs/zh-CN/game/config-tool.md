---
title: "VS Code 配置工具"
description: "在 VS Code 浏览、编辑与校验 GFramework 游戏配置。"
---

# VS Code 配置工具 {#overview}

1. 安装 [GFramework Config Tool](https://marketplace.visualstudio.com/items?itemName=GeWuYou.gframework-config-tool)，在 VS Code 打开包含 `config/` 与 `schemas/` 的项目根目录。
2. 从配置树打开配置域。浏览记录，或打开对应 schema 检查字段约束。
3. 用表单编辑已呈现的字段。编辑嵌套值时可直接打开 YAML。批量编辑用于同配置域的顶层标量或标量数组字段。
4. 保存后根据诊断检查字段与引用，再运行项目加载流程验证最终结果。

运行时目录和 schema 写法见[类型化配置](../game/config-system.md)。
