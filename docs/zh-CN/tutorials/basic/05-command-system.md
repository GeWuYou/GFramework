---
prev:
  text: '引入 Model 重构'
  link: './04-model-refactor'
next:
  text: 'Utility 与 System'
  link: './06-utility-system'
---

# 第 5 章：命令系统优化

在上一章中，我们通过 Model 和事件系统实现了数据驱动的架构。但 Controller 仍然承担着交互逻辑，本章将引入 **Command（命令）模式
** 进一步优化。

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

## 理解 Command 模式

### Command 的作用

**Command（命令）** 是一种设计模式，它将"请求"封装成对象：

```
用户操作 → Command → Model
```

优势：

- **解耦**：Controller 不关心如何增加计数，只负责"发送命令"
- **复用**：同一个命令可以被多个地方调用
- **扩展**：新增逻辑只需修改命令，不影响 Controller
- **可测试**：可以独立测试命令逻辑

### 职责划分

| 层级             | 职责         |
|----------------|------------|
| **Controller** | 将用户操作转换为命令 |
| **Command**    | 封装具体的业务逻辑  |
| **Model**      | 存储状态，发送事件  |

## 创建 Command

### 1. 创建增加命令

在 `scripts/command/` 创建 `IncreaseCountCommand.cs`：

```csharp
using GFramework.Core.command;
using GFramework.Core.extensions;
using MyGFrameworkGame.scripts.model;

namespace MyGFrameworkGame.scripts.command;

/// <summary>
/// 增加计数器值的命令
/// </summary>
public class IncreaseCountCommand : AbstractCommand
{
    /// <summary>
    /// 执行命令的核心逻辑
    /// </summary>
    protected override void OnExecute()
    {
        // 获取 Model 并调用方法
        var model = this.GetModel<ICounterModel>()!;
        model.Increment();
    }
}
```

::: tip AbstractCommand
`AbstractCommand` 是 GFramework 提供的基类，它：

- 自动注入 `Architecture` 上下文
- 提供 `GetModel`、`GetSystem`、`GetUtility` 等方法
- 管理命令的生命周期
  :::

### 2. 创建减少命令

在 `scripts/command/` 创建 `DecreaseCountCommand.cs`：

```csharp
using GFramework.Core.command;
using GFramework.Core.extensions;
using MyGFrameworkGame.scripts.model;

namespace MyGFrameworkGame.scripts.command;

/// <summary>
/// 减少计数器值的命令
/// </summary>
public class DecreaseCountCommand : AbstractCommand
{
    /// <summary>
    /// 执行命令的核心逻辑
    /// </summary>
    protected override void OnExecute()
    {
        var model = this.GetModel<ICounterModel>()!;
        model.Decrement();
    }
}
```

## 重构 Controller

### 使用命令替换直接调用

编辑 `App.cs`：

```csharp
using GFramework.Core.Abstractions.controller;
using GFramework.Core.extensions;
using GFramework.SourceGenerators.Abstractions.rule;
using Godot;
using MyGFrameworkGame.scripts.command;
using MyGFrameworkGame.scripts.model;

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

        // 使用命令替换直接调用
        AddButton.Pressed += () =>
        {
            this.SendCommand(new IncreaseCountCommand());
        };

        SubButton.Pressed += () =>
        {
            this.SendCommand(new DecreaseCountCommand());
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

### 重构后（使用 Command）

```csharp
AddButton.Pressed += () =>
{
    this.SendCommand(new IncreaseCountCommand());  // ← 发送命令
};
```

**优势**：

- Controller 不关心如何增加计数
- 逻辑封装在 Command 中
- Controller 只负责"转发用户意图"

## Command 的优势

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
AddButton.Pressed += () =>
{
    this.SendCommand(new IncreaseCountCommand());
};
```

所有逻辑在 Command 中：

```csharp
protected override void OnExecute()
{
    if (!CanIncrement()) return;
    await SaveData();
    this.GetModel<ICounterModel>()!.Increment();
    PlaySound();
    LogAnalytics();
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
AddButton.Pressed += () => this.SendCommand(new IncreaseCountCommand());
Input.IsActionPressed("increment") => this.SendCommand(new IncreaseCountCommand());
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
// 可以直接测试命令
[Test]
public void IncreaseCommand_ShouldIncrementCount()
{
    var model = new CounterModel();
    var command = new IncreaseCountCommand();
    
    command.Execute();
    
    Assert.AreEqual(1, model.Count);
}
```

### 4. 支持撤销/重做（扩展）

Command 模式天然支持撤销功能：

```csharp
public class IncreaseCountCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        // 执行
        this.GetModel<ICounterModel>()!.Increment();
    }
    
    public void Undo()
    {
        // 撤销
        this.GetModel<ICounterModel>()!.Decrement();
    }
}
```

## Command 的实际应用

让我们看一个更复杂的例子：

```csharp
/// <summary>
/// 更改语言命令
/// </summary>
public class ChangeLanguageCommand : AbstractAsyncCommand<ChangeLanguageInput>
{
    protected override async Task OnExecuteAsync(ChangeLanguageInput input)
    {
        // 1. 获取设置 Model
        var settingsModel = this.GetModel<ISettingsModel>()!;
        
        // 2. 获取设置数据
        var settings = settingsModel.GetData();
        
        // 3. 修改语言配置
        settings.Language = input.Language;
        
        // 4. 应用设置（通过 System）
        await this.GetSystem<ISettingsSystem>()!.Apply();
    }
}
```

如果这些逻辑都写在 Controller：

```csharp
LanguageButton.Pressed += async () =>
{
    var settingsModel = this.GetModel<ISettingsModel>()!;
    var settings = settingsModel.GetData();
    settings.Language = newLanguage;
    await this.GetSystem<ISettingsSystem>()!.Apply();
};
```

**问题**：

- Controller 臃肿
- 逻辑分散
- 难以复用

## 理解职责边界

### Controller vs Command

| 层级             | 职责         | 示例          |
|----------------|------------|-------------|
| **Controller** | 将用户操作转换为意图 | "用户点击了增加按钮" |
| **Command**    | 封装业务逻辑     | "如何增加计数"    |
| **Model**      | 存储和管理状态    | "计数的值是多少"   |

**类比**：

- **Controller**：服务员（接收顾客点单）
- **Command**：厨师（制作菜品）
- **Model**：菜单（菜品信息）

### 何时使用 Command？

✅ **应该使用 Command**：

- 逻辑超过 3 行
- 需要复用的操作
- 涉及多个 Model/System 的协作
- 需要异步操作
- 需要撤销/重做

❌ **不需要 Command**：

- 极简单的操作（如 `model.GetData()`）
- 纯 UI 逻辑（如切换界面状态）

## 核心收获

通过这次重构，我们学到了：

| 概念             | 解释                           |
|----------------|------------------------------|
| **Command 模式** | 将请求封装成对象                     |
| **职责分离**       | Controller 负责转发，Command 负责执行 |
| **逻辑复用**       | 同一命令可被多处调用                   |
| **可测试性**       | 命令可独立测试                      |
| **单一职责**       | 每个 Command 只做一件事             |

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

### 阶段 3：引入 Command

```csharp
AddButton.Pressed += () =>
{
    this.SendCommand(new IncreaseCountCommand());
};
```

**完善**：Controller 不再关心"如何"，只负责"转发"

## 下一步

现在我们的架构已经很清晰了：

```
View → Controller → Command → Model → Event → View
```

但还有两个问题：

1. **业务规则**：如何实现"计数不能超过 20"？
2. **状态响应**：如何实现"计数超过 10 时触发某个逻辑"？

这些问题需要 **Utility** 和 **System** 来解决。

在下一章中，我们将：

- 引入 **Utility** 处理业务规则
- 引入 **System** 响应状态变化
- 完成完整的架构设计

👉 [第 6 章：Utility 与 System](./06-utility-system.md)

---

::: details 本章检查清单

- [ ] IncreaseCountCommand 已创建
- [ ] DecreaseCountCommand 已创建
- [ ] App.cs 使用 SendCommand 替换了直接调用
- [ ] 运行游戏，功能正常
- [ ] 理解了 Command 的职责和优势
- [ ] 理解了 Controller、Command、Model 的职责边界
  :::

::: tip 思考题

1. 如果需要实现"撤销"功能，应该如何修改 Command？
2. 异步命令（如网络请求）应该如何实现？
3. 多个 Command 需要按顺序执行时，应该怎么做？

这些高级用法可以在后续深入学习！
:::
