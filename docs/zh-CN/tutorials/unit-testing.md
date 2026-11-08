---
title: 单元测试实践
description: 验证领域规则、用例、资源所有权与宿主边界的可观察行为。
---

# 单元测试实践

普通 C# 领域对象可以直接创建并测试。应用用例通过构造函数取得依赖，测试用有明确行为的内存实现替换外部存储或引擎适配。测试不需要全局架构或服务定位器。

## 创建项目

```bash
dotnet new classlib -n MyGame.Core
dotnet new nunit -n MyGame.Tests
dotnet add MyGame.Tests/MyGame.Tests.csproj reference MyGame.Core/MyGame.Core.csproj
dotnet add MyGame.Tests/MyGame.Tests.csproj package NUnit --version 5.0.0
dotnet add MyGame.Tests/MyGame.Tests.csproj package GeWuYou.GFramework.Core
```

测试目标应与业务项目支持的目标一致。仓库已有测试使用 NUnit 5、Microsoft.NET.Test.Sdk 与 NUnit3TestAdapter，其具体引用由测试项目文件维护。新消费项目选择并锁定自己的测试依赖。

## 测试领域规则

```csharp
using System;
using NUnit.Framework;

[TestFixture]
public sealed class CounterTests
{
    [Test]
    public void Add_PositiveAmount_UpdatesValue()
    {
        var counter = new Counter();
        counter.Add(3);
        Assert.That(counter.Value, Is.EqualTo(3));
    }

    [Test]
    public void Add_NegativeAmount_DoesNotMutateState()
    {
        var counter = new Counter();
        Assert.Throws<ArgumentOutOfRangeException>(() => counter.Add(-1));
        Assert.That(counter.Value, Is.Zero);
    }

    private sealed class Counter
    {
        public int Value { get; private set; }
        public void Add(int amount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(amount);
            Value += amount;
        }
    }
}
```

## 测试同步事件与订阅拥有者

```csharp
using GFramework.Core.Events;
using NUnit.Framework;

[TestFixture]
public sealed class EventTests
{
    [Test]
    public void DisposedSubscription_DoesNotReceiveLaterEvents()
    {
        var bus = new EventBus();
        var total = 0;
        var subscription = bus.Subscribe<int>(amount => total += amount);
        bus.Publish(2);
        subscription.Dispose();
        bus.Publish(3);
        Assert.That(total, Is.EqualTo(2));
    }
}
```

增加普通与优先级混合订阅、嵌套发布、异常及重复释放等测试，以行为契约为依据，不断言内部字典布局。

## 异步与取消

用 TaskCompletionSource 控制加载、等待和退出时序，避免用睡眠猜测并发先后。NUnit 5 的异步断言必须等待完成：

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Core.Functional;
using NUnit.Framework;

[TestFixture]
public sealed class CancellationTests
{
    [Test]
    public async Task TryAsync_PreservesCancellation()
    {
        var token = new CancellationToken(canceled: true);
        await Assert.CatchAsync<OperationCanceledException>(() =>
            Result<int>.TryAsync(() => Task.FromCanceled<int>(token)));
    }
}
```

资源测试验证单次底层加载、独立租约、取消隔离、失败重试和一次释放；状态机测试覆盖切换拒绝、回退、异常和停止。Result 测试包括默认失败与同步/异步异常一致性。

## 集成与宿主验收

少量组合根测试验证真实注册、同一单例身份、启动顺序、外部所有权与失败清理。纯业务项目通过编译引用或边界测试防止引入 Godot、文件实现或容器依赖。

Godot 场景树、输入、节点释放、导出包及实际存档恢复需要真实宿主验收。托管测试通过不能证明原生行为；同样，预览可运行也不能代替错误、取消与清理测试。

在仓库根目录通过声明的入口运行相应项目：

```bash
dotnet test GFramework.Core.Tests -c Release
dotnet test GFramework.Game.Tests -c Release
```

相关主题：[架构组织](../best-practices/architecture-patterns.md)、[资源租约](../core/resource.md)、[错误处理](../best-practices/error-handling.md)。
