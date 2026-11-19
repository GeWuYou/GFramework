---
title: "故障排查"
description: "根据症状检查 GFramework 项目的注册、资源与宿主接线。"
---

# 故障排查 {#overview}

| 症状 | 首先检查 |
| --- | --- |
| 无法解析服务 | 核对服务键和生命周期；scoped 服务使用操作作用域。[依赖注入](./core/ioc.md) |
| 首载失败 | 检查最先抛出的异常、组件注册和取消信号。[异步初始化](./core/async-initialization.md) |
| 事件未收到 | 检查是否为同一 EventBus 实例、通知类型和订阅寿命。[同步事件](./core/events.md) |
| 场景或 UI 访问线程错误 | 在 Godot 主线程建立调度器，并用它装配宿主。[宿主装配](./godot/architecture.md) |
| 导出后配置缺失 | 检查预设是否包含 YAML 和 schema 文件。[导出配置](./game/config-system.md#godot) |
| 旧存档需要转换 | 先准备候选数据，检查确认要求和迁移路径。[准备读取](./game/data.md#load) |
| 生成的字段或方法缺失 | 核对分析器包、输入和编译诊断。[源码生成器](./source-generators/index.md) |
