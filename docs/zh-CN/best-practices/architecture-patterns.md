---
title: 架构组织与依赖边界
description: 使用标准 DI、功能模块与轻量分层组织普通 C# 应用。
---

# 架构组织与依赖边界

推荐标准 DI 装配普通 C# 对象，按功能组织代码，在领域、应用与宿主之间建立明确依赖方向。Architecture 用于需要统一启动、停止与宿主协作的应用。

## 职责与方向

| 职责 | 内容 | 约束 |
| --- | --- | --- |
| 领域 | 状态、不变量和业务规则 | 不读取容器、引擎节点和文件系统 |
| 应用 | 开局、保存、结算等用例 | 依赖领域和必要能力接口 |
| 适配 | 存档、资源、引擎和外部服务 | 实现所需接口，持有平台细节 |
| 表现 | 输入、界面、场景和动画 | 调用用例，读取快照，管理订阅 |
| 组合根 | 创建对象并装配关系，控制启停 | 可以引用所需具体实现 |

Model 可以维护自身不变量，而非任意写入的数据袋。System 可以表示组织用例的服务，Utility 表示复用能力；这些是职责命名，普通对象不继承角色基类。

## 按功能组织

小项目使用 `Features/Gameplay`、`Features/Progress`、`Features/Settings`，每个功能下按需要区分规则、用例和适配。需要编译保障时，将纯 C# 业务放入独立项目，由宿主引用它。

项目数量以真实边界为依据，不为了架构图建立只有转发的一层，也不让 Common 变成所有对象都可任意依赖的目录。

## 显式依赖

```csharp
using System;

public sealed class Health
{
    public Health(int maximum)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximum);
        Maximum = maximum;
        Current = maximum;
    }
    public int Maximum { get; }
    public int Current { get; private set; }
    public void Damage(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        Current = Math.Max(0, Current - amount);
    }
}

public sealed class CombatService(Health health)
{
    public void ApplyHit(int damage) => health.Damage(damage);
}
```

该例可以直接 new 并测试。业务对象不通过 IServiceProvider 查找任意服务。真正的替换边界，如存档存储和内容目录，才定义对应接口；领域对象可以直接依赖具体类型。

## 装配与生命周期

使用 IServiceCollection 登记服务。普通 DI 注册遵循其标准所有权；需要业务启动/停止的对象显式 AddComponent，按实际依赖声明顺序，停止逆实际开始顺序。配置阶段不做文件加载或解析服务。

可释放对象只有一个拥有者；外部传入实例由调用者释放。Godot 节点由引擎创建时，组合根在节点开始工作前显式接入所需能力。活动场景树操作留在宿主线程，纯业务服务无需了解节点路径。

## 调用与状态

有明确执行者的用例直接调用方法。事件表达已发生的事实，不通过事件链暗中完成必须按序成功的流程。CQRS 在统一管线有实际收益时采用。

BindableProperty 表达字段展示，Store 表达集中 reducer，StateMachine 表达流程切换；不要让它们同时成为同一份状态的权威。验证新增设置项或计分规则时，改动应落在相应功能与适配中。通过编译引用和边界测试保护纯业务项目。

参见[架构生命周期](../core/architecture.md)、[错误处理](./error-handling.md)与[单元测试](../tutorials/unit-testing.md)。
