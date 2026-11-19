---
title: "组合可失败的计算"
description: "用 Option 或 Result 表达一条可读的业务路径。"
---

# 组合可失败的计算 {#overview}

需要读取可选昵称时用 `Option<string>`，在展示阶段用 `Match` 决定占位文本。需要解释保存失败时，返回 `Result` 并携带错误。多个可能失败的步骤通过 `Bind` 顺序组合，最后在服务边界处理分支。

最小用法见[Option 与 Result](../core/functional.md)。
