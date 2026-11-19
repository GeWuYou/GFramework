---
title: "应用配置"
description: "在启动时加载、验证并注入应用参数。"
---

# 应用配置 {#overview}

在组合根以 `Microsoft.Extensions.Configuration` 读取启动参数，用类型化 Options 或参数对象传给使用方。在启动前验证必填值与取值范围。

游戏内容表使用[类型化配置](../game/config-system.md)。玩家偏好使用[设置服务](../game/setting.md)。两种数据各自管理加载时机和持久化。
