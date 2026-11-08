---
title: 异步初始化
---

# 异步初始化

有文件首载或异步资源准备的组件实现 IAsyncInitializable，并通过 AddComponent 登记。调用者等待 Architecture.InitializeAsync，成功后再开放用户操作。

取消令牌必须传递到实际等待。启动失败、取消与正常退出共享资源清理规则；已开始但尚未完成初始化的组件也会尝试业务停止。创建资源的对象仍按 DI 所有权释放资源。

不要把异步工作放进构造函数，不在初始化回调里等待本架构的销毁操作。同步 Initialize 不能用于异步组件。

完整时序见 [装配与生命周期](./architecture.md)。
