---
title: Core 抽象层
---

# Core 抽象层

Core.Abstractions 定义架构、生命周期、事件、属性、状态、资源、协程和基础设施契约。运行时实现由 Core 提供；普通业务对象无需继承框架基类。

接口用于隔离实际变化的能力。宿主负责注册实现、构造依赖和确定资源所有者。事件与属性订阅采用 IDisposable，状态与资源各自具有独立生命周期。

源码阅读从 IArchitecture、IArchitectureModule、IEventBus、IStateMachine、IResourceManager 开始，具体契约见其 XML 注释。[Core 使用指南](../core/index.md) 提供对应采用路径。
