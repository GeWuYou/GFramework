---
title: 生命周期
---

# 生命周期

生命周期表达显式的业务启动与停止，与 Model、System、Utility 命名无关。

| 能力 | 入口 |
| --- | --- |
| 同步初始化 | IInitializable.Initialize |
| 异步初始化 | IAsyncInitializable.InitializeAsync |
| 同步业务停止 | IDestroyable.Destroy |
| 异步业务停止 | IAsyncDestroyable.DestroyAsync |
| 资源释放 | IDisposable / IAsyncDisposable，由创建对象的拥有者执行 |

组件通过 AddComponent 登记，普通 AddSingleton 等注册不自动参与。异步能力优先，按 order 和登记顺序启动，逆实际启动顺序停止。业务停止与依赖资源释放分开；不要在组件里释放容器拥有的注入依赖。

启动失败与停止错误的完整规则见 [架构生命周期](./architecture.md)。
