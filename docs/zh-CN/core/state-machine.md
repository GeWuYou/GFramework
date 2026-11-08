---
title: 状态机
---

# 状态机

StateMachine 是可独立构造的异步状态机，不要求 Architecture、Context 或事件总线。

```csharp
using GFramework.Core.State;

await using var machine = new StateMachine(maxHistorySize: 10);
machine.Register(new MenuState()).Register(new PlayingState());
await machine.ChangeToAsync<MenuState>();
await machine.ChangeToAsync<PlayingState>();

public sealed class MenuState : StateBase;
public sealed class PlayingState : StateBase;
```

状态通过构造函数接收依赖。同步回调可实现 IState 或继承 StateBase；异步回调可实现 IAsyncState 或继承 AsyncStateBase，并传递取消令牌。

## 切换契约

- 具体状态类型只注册一次，首次切换后冻结注册。Unregister 仅用于配置阶段。
- ChangeToAsync、GoBackAsync 和 StopAsync 共用串行操作边界。等待可取消，取消一个等待者不取消其他操作。
- 切换先检查当前状态的退出条件，再退出旧状态、进入新状态；成功后提交历史并发出实例 StateChanged。
- 相同状态返回 false，不重复回调。回退同样经过退出条件，拒绝时保留历史。
- 回调失败后 Fault 保存原异常，后续切换被拒绝。无法自动撤销已产生的副作用；停止完成后创建新机器。
- 回调不得等待本机器的另一次操作，重入会明确报错。

## 线程与清理

机器保留操作调用者的同步上下文；Godot 接入须将所有操作调度到宿主线程。注册表安全不意味着状态对象可跨线程使用。

StopAsync 等待当前切换完成，退出当前状态，逆序尝试已开始状态的业务清理。并发和重复停止等待同一结果，异常聚合报告。状态实例属于调用者或 DI，状态机不调用其 Dispose。

历史最多保留 maxHistorySize 条成功切换记录，读取顺序为最近优先；0 关闭历史，负值拒绝。
