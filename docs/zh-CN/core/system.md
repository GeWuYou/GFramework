---
title: 应用服务
---

# 应用服务

System 可以作为应用服务的命名习惯。服务通过构造函数接收领域对象及实际使用的能力，方法对应用户用例，例如开始游戏、保存进度或提交设置。

```csharp
public sealed class CounterService(CounterModel model)
{
    public int Increment()
    {
        model.Increment();
        return model.Value;
    }
}
```

调用方需要结果时直接调用方法。某个事实需要被多个独立观察者感知时，发布 [事件](./events.md)。统一校验、日志或处理管线有实际收益时，再使用 [CQRS](./cqrs.md)。

需要首载和停止的服务显式登记为生命周期组件；普通服务无需添加空初始化方法。每帧逻辑由所属宿主或明确的调度器驱动，生命周期编排不自动提供游戏循环。
