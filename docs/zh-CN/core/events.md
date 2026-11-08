---
title: 本地事件
---

# 本地事件

IEventBus 表达当前进程中的同步通知，按通知类型投递。业务请求需要返回结果时直接调用服务方法，或使用独立的 CQRS runtime。

```csharp
using GFramework.Core.Events;

var bus = new EventBus();
using var subscriptions = new SubscriptionScope();
subscriptions.Add(bus.Subscribe<int>(score => Console.WriteLine(score)));
subscriptions.Add(bus.Subscribe<int>(score => Console.WriteLine(score), priority: 10));
bus.Publish(42);
```

## 投递规则

普通与优先级订阅属于同一通道。数字越大越先执行，同值按订阅顺序。Publish 默认通知全部订阅者。EventPropagation.Highest 只通知最高优先级组；UntilHandled 在 SubscribeWithContext 回调调用 MarkAsHandled 后停止。

每次发布捕获订阅快照，过程中添加或移除的订阅在后续发布中生效。嵌套发布拥有独立传播上下文。回调异常立即向发布者传播，后续回调不再执行。

注册表可并发操作；回调在发布线程执行。通知不会自动排队或切换主线程，Godot 节点更新由宿主调度。不要在同步回调里使用 async void 执行需要等待的业务。

## 订阅所有权

Subscribe 返回幂等 IDisposable。每次重复订阅都有独立令牌，释放其中一个不影响其他订阅。SubscriptionScope 批量拥有令牌，逆序释放；单个清理失败仍尝试其他令牌。

WeakEvent 和 FilterableEvent 为独立的弱订阅、过滤能力。弱订阅不替代明确释放；局部 Event 和 BindableProperty 同样返回标准 IDisposable。

OrEvent 组合拥有其源订阅；释放单个监听者只移除该监听者，释放组合才断开全部源。enableStatistics 可开启实例统计，统计键为完整通知类型名。
