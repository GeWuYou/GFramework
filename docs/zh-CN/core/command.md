---
title: 写操作请求
---

# 写操作请求

写操作可直接调用应用服务方法。当需要统一分发与管线时，定义 IRequest&lt;TResponse&gt;，在 IRequestHandler 中执行用例并返回结果。

处理器通过构造函数取得依赖，调用 ICqrsRuntime.SendAsync 时传递取消令牌。验证失败、取消和意外错误应由接口契约分别表达，不能把失败转换成正常成功。

写操作在所属领域对象中维护约束；完成后可发布事实通知。采用和注册入口见 [CQRS runtime](./cqrs.md)。
