---
title: 领域模型
---

# 领域模型

Model 表示具有明确业务含义的状态与规则。它是普通 C# 类型，不依赖 Architecture、容器、引擎节点或框架基类。

状态通过行为方法修改，对外提供只读属性或快照。把需要同时成立的约束放在同一个对象内维护，例如一局游戏的开始、计分、结束和是否允许继续。

```csharp
public sealed class CounterModel
{
    public int Value { get; private set; }
    public void Increment() => Value = checked(Value + 1);
}
```

存档 DTO 负责序列化，视图状态负责展示；它们可由领域快照转换，不必与领域对象共用可变实例。简单数据可直接使用 record。

跨存储、内容系统及 UI 的流程放在 [应用服务](./system.md)。
