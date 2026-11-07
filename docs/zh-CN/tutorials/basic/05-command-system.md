---
title: 第 5 章：命令系统优化
description: 通过 CQRS 请求模型拆分 Controller 交互逻辑，建立更清晰的执行职责边界。
prev:
  text: '引入 Model 重构'
  link: './04-model-refactor'
next:
  text: 'Utility 与 System'
  link: './06-utility-system'
---

# 第 5 章：命令系统优化

在上一章中，我们通过 Model 和事件系统实现了数据驱动的架构。但 Controller 仍然承担着交互逻辑，本章将引入 **CQRS 请求模型** 进一步优化。

## Controller 的职责问题

### 当前代码

```csharp
public override void _Ready()
{
    _counterModel = this.GetModel<ICounterModel>()!;

    AddButton.Pressed += () =>
    {
        _counterModel.Increment();  // ← 交互逻辑
    };

    SubButton.Pressed += () =>
    {
        _counterModel.Decrement();  // ← 交互逻辑
    };

    // ...
}
```

看起来很简洁，但这段代码同时承担着：

- **表现逻辑**（View Binding）：`AddButton.Pressed +=`
- **交互逻辑**（Interaction Logic）：`_counterModel.Increment()`

### 为什么这是问题？

现在只是简单的增减，但如果功能变复杂：

```csharp
AddButton.Pressed += async () =>
{
    // 1. 验证状态
    if (!CanIncrement()) return;
    
    // 2. 执行业务逻辑
    await DoSomethingAsync();
    _counterModel.Increment();
    
    // 3. 保存数据
    await SaveToFileAsync();
    
    // 4. 播放音效
    PlaySound("increment.wav");
    
    // 5. 统计埋点
    LogAnalytics("counter_incremented");
    
    // 6. 更新成就
    UpdateAchievement();
};
```

**问题**：

- Controller 迅速膨胀
- 逻辑难以复用（如果键盘快捷键也要增加计数？）
- 难以测试（需要 mock 按钮）
- 违反单一职责原则

## 理解请求模型

### 请求的作用

CQRS 请求模型将“请求”封装成对象，处理逻辑放在独立的处理器中：

```text
用户操作 → Request → Handler → Model
```

优势：

- **解耦**：Controller 不关心如何增加计数，只负责“发送请求”
- **复用**：同一个请求可以被多个地方调用
- **扩展**：新增逻辑只需修改处理器，不影响 Controller
- **可测试**：可以独立测试处理器逻辑
- **横切能力**：pipeline behavior 可以统一处理日志、校验等关注点

### 职责划分

| 层级             | 职责         |
|----------------|------------|
| **Controller** | 将用户操作转换为请求 |
| **Handler**    | 封装具体的业务逻辑  |
| **Model**      | 存储状态，发送事件  |

## 创建请求与处理器

### 1. 创建增加计数请求

在 `scripts/cqrs/` 创建 `IncreaseCountRequest.cs`：

```csharp
using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Cqrs.Request;
using MyGFrameworkGame.scripts.Model;

namespace MyGFrameworkGame.scripts.Cqrs;

/// <summary>
/// 增加计数器值的请求
/// </summary>
public sealed record IncreaseCountRequest : IRequest<Unit>;

/// <summary>
/// 增加计数器值的处理器
/// </summary>
public sealed class IncreaseCountHandler
    : AbstractRequestHandler<IncreaseCountRequest, Unit>
{
    public override ValueTask<Unit> Handle(
        IncreaseCountRequest request,
        CancellationToken cancellationToken)
    {
        var model = Context.GetModel<ICounterModel>()!;
        model.Increment();
        return ValueTask.FromResult(Unit.Value);
    }
}
```

::: tip AbstractRequestHandler
`AbstractRequestHandler` 是 CQRS 模块提供的基类，它：

- 在分发前接收 runtime 注入的架构上下文
- 提供 `GetModel`、`GetSystem`、`GetUtility` 等上下文方法
  :::

### 2. 创建减少计数请求

```csharp
public sealed record DecreaseCountRequest : IRequest<Unit>;

public sealed class DecreaseCountHandler
    : AbstractRequestHandler<DecreaseCountRequest, Unit>
{
    public override ValueTask<Unit> Handle(
        DecreaseCountRequest request,
        CancellationToken cancellationToken)
    {
        var model = Context.GetModel<ICounterModel>()!;
        model.Decrement();
        return ValueTask.FromResult(Unit.Value);
    }
}
```

## 安装 CQRS 运行时

CQRS runtime 不随 Core 自动安装。在架构中显式接入：

```csharp
using GFramework.Cqrs.Extensions;

var architecture = new CounterArchitecture();
architecture.UseCqrsRuntime(); // 自动包含架构所在程序集的处理器
await architecture.InitializeAsync();
```

## 重构 Controller

### 使用请求替换直接调用

编辑 `App.cs`：

```csharp
using GFramework.Core.Abstractions.Controller;
using GFramework.Core.Extensions;
using GFramework.Core.SourceGenerators.Abstractions.Rule;
using GFramework.Cqrs.Extensions;
using Godot;
using MyGFrameworkGame.scripts.Cqrs;
using MyGFrameworkGame.scripts.Model;

namespace MyGFrameworkGame.scripts.app;

[ContextAware]
public partial class App : Control, IController
{
    private Button AddButton => GetNode<Button>("%AddButton");
    private Button SubButton => GetNode<Button>("%SubButton");
    private Label Label => GetNode<Label>("%Label");

    public override void _Ready()
    {
        // 监听事件
        this.RegisterEvent<CounterModel.ChangedCountEvent>(e =>
        {
            UpdateView(e.Count);
        });

        // 使用请求替换直接调用
        AddButton.Pressed += async () =>
        {
            await this.SendAsync(new IncreaseCountRequest());
        };

        SubButton.Pressed += async () =>
        {
            await this.SendAsync(new DecreaseCountRequest());
        };

        // 初始化界面
        UpdateView();
    }

    private void UpdateView(int count = 0)
    {
        Label.Text = $"Count: {count}";
    }
}
```

### 运行游戏

按 **F5** 运行游戏，功能依然正常！

## 对比重构前后

### 重构前（使用 Model）

```csharp
AddButton.Pressed += () =>
{
    _counterModel.Increment();  // ← 直接调用 Model
};
```

**问题**：

- Controller 知道如何增加计数
- 如果逻辑复杂化，Controller 会变臃肿

### 重构后（使用请求）

```csharp
AddButton.Pressed += async () =>
{
    await this.SendAsync(new IncreaseCountRequest());  // ← 发送请求
};
```

**优势**：

- Controller 不关心如何增加计数
- 逻辑封装在 Handler 中
- Controller 只负责“转发用户意图”

## 请求模型的优势

### 1. 解耦 Controller

**之前**：

```csharp
AddButton.Pressed += () =>
{
    if (!CanIncrement()) return;
    await SaveData();
    _counterModel.Increment();
    PlaySound();
    LogAnalytics();
};
```

Controller 必须知道所有细节。

**现在**：

```csharp
AddButton.Pressed += async () =>
{
    await this.SendAsync(new IncreaseCountRequest());
};
```

所有逻辑在 Handler 中：

```csharp
public override ValueTask<Unit> Handle(
    IncreaseCountRequest request,
    CancellationToken cancellationToken)
{
    if (!CanIncrement()) return ValueTask.FromResult(Unit.Value);
    await SaveData();
    Context.GetModel<ICounterModel>()!.Increment();
    PlaySound();
    LogAnalytics();
    return ValueTask.FromResult(Unit.Value);
}
```

### 2. 逻辑复用

假设需要通过键盘快捷键增加计数：

**之前**：

```csharp
AddButton.Pressed += () => { /* 逻辑 */ };
Input.IsActionPressed("increment") => { /* 复制相同逻辑 */ };
```

代码重复！

**现在**：

```csharp
AddButton.Pressed += async () => await this.SendAsync(new IncreaseCountRequest());
Input.IsActionPressed("increment") => this.SendAsync(new IncreaseCountRequest());
```

逻辑只写一次！

### 3. 易于测试

**之前**：

```csharp
// 无法测试，必须 mock 按钮
AddButton.Pressed += () => { /* 逻辑 */ };
```

**现在**：

```csharp
// 通过架构上下文直接分发请求
[Test]
public async Task IncreaseRequest_ShouldIncrementCount()
{
    var architecture = new CounterArchitecture();
    architecture.UseCqrsRuntime();
    await architecture.InitializeAsync();

    await architecture.Context.SendAsync(new IncreaseCountRequest());

    var model = architecture.Context.GetModel<ICounterModel>();
    Assert.AreEqual(1, model.Count);
}
```

### 4. 统一的横切能力

pipeline behavior 可以统一处理日志、校验、性能统计，而不需要修改每个处理器：

```csharp
protected override void ConfigureServices(IServiceCollection services)
{
    services.AddCqrsPipelineBehavior<LoggingBehavior<,>>();
}
```

## 请求模型与异步操作

请求处理器通过构造注入接收普通服务，并传播调用方取消令牌。设置流程可依赖 `ISettingsModel` 与 `ISettingsSystem`：

```csharp
public sealed record ChangeLanguageRequest(string Language) : IRequest<Unit>;

public sealed class ChangeLanguageHandler(ISettingsModel model, ISettingsSystem settings)
    : IRequestHandler<ChangeLanguageRequest, Unit>
{
    public async ValueTask<Unit> Handle(ChangeLanguageRequest request, CancellationToken cancellationToken)
    {
        model.GetData<LocalizationSettings>().Language = request.Language;
        await settings.ApplyAllAsync(cancellationToken);
        return Unit.Value;
    }
}
```

`LocalizationSettings` 是 Game 契约中的语言设置数据。宿主显式登记应用器与 dispatcher，Controller 只负责构造请求并观察任务。设置的实际采用路径见 [设置系统](../../game/setting.md)。

## 理解职责边界

### Controller vs Handler

| 层级             | 职责         | 示例          |
|----------------|------------|-------------|
| **Controller** | 将用户操作转换为意图 | “用户点击了增加按钮” |
| **Handler**    | 封装业务逻辑     | “如何增加计数”    |
| **Model**      | 存储和管理状态    | “计数的值是多少”   |

**类比**：

- **Controller**：服务员（接收顾客点单）
- **Handler**：厨师（制作菜品）
- **Model**：菜单（菜品信息）

### 何时使用请求模型？

✅ **应该使用请求模型**：

- 逻辑超过 3 行
- 需要复用的操作
- 涉及多个 Model/System 的协作
- 需要异步操作
- 需要 pipeline 横切能力

❌ **不需要请求模型**：

- 极简单的操作（如 `model.GetData()`）
- 纯 UI 逻辑（如切换界面状态）

## 核心收获

通过这次重构，我们学到了：

| 概念             | 解释                           |
|----------------|------------------------------|
| **请求模型** | 将请求封装成对象，处理器负责执行      |
| **职责分离**       | Controller 负责转发，Handler 负责执行 |
| **逻辑复用**       | 同一请求可被多处调用                   |
| **可测试性**       | 处理器可通过架构上下文独立测试              |
| **单一职责**       | 每个处理器只做一件事             |

## 对比三个阶段

### 阶段 1：基础实现

```csharp
private int _count;

AddButton.Pressed += () =>
{
    _count++;
    UpdateView();
};
```

**问题**：状态、逻辑、UI 混在一起

### 阶段 2：引入 Model

```csharp
private ICounterModel _counterModel;

AddButton.Pressed += () =>
{
    _counterModel.Increment();
};
```

**改进**：状态抽离到 Model，但交互逻辑仍在 Controller

### 阶段 3：引入请求模型

```csharp
AddButton.Pressed += async () =>
{
    await this.SendAsync(new IncreaseCountRequest());
};
```

**完善**：Controller 不再关心“如何”，只负责“转发”

## 下一步

现在我们的架构已经很清晰了：

```text
View → Controller → Request → Handler → Model → Event → View
```

但还有两个问题：

1. **业务规则**：如何实现“计数不能超过 20”？
2. **状态响应**：如何实现“计数超过 10 时触发某个逻辑”？

这些问题需要 **Utility** 和 **System** 来解决。

在下一章中，我们将：

- 引入 **Utility** 处理业务规则
- 引入 **System** 响应状态变化
- 完成完整的架构设计

👉 [第 6 章：Utility 与 System](./06-utility-system.md)

---

::: details 本章检查清单

- [ ] IncreaseCountRequest 与 IncreaseCountHandler 已创建
- [ ] DecreaseCountRequest 与 DecreaseCountHandler 已创建
- [ ] 架构已通过 UseCqrsRuntime 安装 CQRS 运行时
- [ ] App.cs 使用 SendAsync 替换了直接调用
- [ ] 运行游戏，功能正常
- [ ] 理解了请求模型的职责和优势
- [ ] 理解了 Controller、Handler、Model 的职责边界
  :::

::: tip 思考题

1. 如果需要实现“撤销”功能，应该如何设计请求？
2. 需要返回值的请求（如查询背包数量）应该如何实现？
3. 多个请求需要统一记录日志时，应该怎么做？

这些高级用法可以在后续深入学习！
:::
