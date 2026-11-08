---
title: Core 模块
---

# Core 模块

Core 提供可独立采用的事件、状态机、协程、资源、属性、日志和函数式工具。Architecture 是可选的初始化与停止编排，不是普通服务的必需基类。

## 采用方式

- 普通服务使用标准 Microsoft DI，领域对象可直接构造。
- 有异步首载、启动顺序或宿主停止需求时，使用 [Architecture](./architecture.md)。
- 业务对象采用显式依赖；Model、System、Utility 表达职责，不要求实现框架角色接口。
- CQRS 独立安装，见 [请求运行时](./cqrs.md)。

## 主题

- [装配与生命周期](./architecture.md)、[依赖注入](./ioc.md)、[依赖边界](./context.md)
- [领域模型](./model.md)、[应用服务](./system.md)、[通用能力](./utility.md)、[业务规则](./rule.md)
- [事件](./events.md)、[属性](./property.md)、[状态机](./state-machine.md)、[状态管理](./state-management.md)
- [资源](./resource.md)、[协程](./coroutine.md)、[暂停](./pause.md)、[对象池](./pool.md)
- [日志](./logging.md)、[配置](./configuration.md)、[运行环境](./environment.md)、[本地化](./localization.md)、[函数式工具](./functional.md)

Core.Abstractions 提供契约，Core 提供实现。Core 不传递依赖 CQRS、Godot 或源码生成器。安装 Core 即可传递取得对应契约包。

```bash
dotnet add package GeWuYou.GFramework.Core
```

完整应用接入见 [最小 C# 示例](../getting-started/quick-start.md) 和 [Godot 完整项目](../tutorials/godot-complete-project.md)。
