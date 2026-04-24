---
title: 第 7 章：总结与最佳实践
description: 回顾基础教程的架构演进，整理最佳实践、常见问题与后续学习方向。
prev:
  text: 'Utility 与 System'
  link: './06-utility-system'
---

# 第 7 章：总结与最佳实践

恭喜你完成了 GFramework 基础教程！本章将回顾整个架构设计，总结最佳实践，并解答常见问题。

## 架构演进回顾

### 阶段 1：基础实现

**代码**：

```csharp
private int _count;

AddButton.Pressed += () =>
{
    _count++;
    UpdateView();
};
```

**问题**：

- ❌ 状态、逻辑、UI 混在一起
- ❌ 无法复用
- ❌ 难以测试
- ❌ 扩展困难

---

### 阶段 2：引入 Model + 事件

**代码**：

```csharp
private ICounterModel _counterModel;

AddButton.Pressed += () =>
{
    _counterModel.Increment();
};

this.RegisterEvent<ChangedCountEvent>(e =>
{
    UpdateView(e.Count);
});
```

**改进**：

- ✅ 状态抽离到 Model
- ✅ 通过事件更新 UI
- ✅ Model 可复用、可测试

**剩余问题**：

- ⚠️ 交互逻辑仍在 Controller

---

### 阶段 3：引入 Command

**代码**：

```csharp
AddButton.Pressed += () =>
{
    this.SendCommand(new IncreaseCountCommand());
};
```

**改进**：

- ✅ Controller 不关心"如何"，只负责"转发"
- ✅ 逻辑封装在 Command 中
- ✅ 命令可复用、可测试

**剩余问题**：

- ⚠️ 业务规则写在 Command 里

---

### 阶段 4：引入 Utility + System

**代码**：

```csharp
// Command 使用 Utility 检查规则
if (!utility.CanIncrease(model.Count)) return;
model.Increment();

// System 响应状态变化
this.RegisterEvent<ChangedCountEvent>(e =>
{
    CheckThreshold(e.Count);
});
```

**最终架构**：

- ✅ 完全的关注点分离
- ✅ 单向数据流
- ✅ 各层可测试、可复用
- ✅ 易于扩展和维护

## 完整架构图

```
┌──────────────────────────────────────────────┐
│                   View (UI)                  │
│  Godot Nodes (Label, Button)                │
└────────────┬─────────────────────────────────┘
             │ 用户输入
┌────────────▼─────────────────────────────────┐
│              Controller                      │
│  - 接收用户输入                              │
│  - 转发命令                                  │
│  - 监听事件更新 UI                           │
└────────────┬─────────────────────────────────┘
             │ SendCommand
┌────────────▼─────────────────────────────────┐
│               Command                        │
│  - 获取 Utility 检查规则                     │
│  - 调用 Model 修改状态                       │
└────────────┬─────────────────────────────────┘
             │ GetModel / GetUtility
             │
   ┌─────────┼─────────┐
   │                   │
┌──▼──────────┐ ┌──────▼───────┐
│   Utility   │ │    Model     │
│  - 业务规则 │ │  - 存储状态  │
│  - 纯计算   │ │  - 发送事件  │
└─────────────┘ └──────┬───────┘
                       │ SendEvent
                ┌──────┴───────┐
                │              │
        ┌───────▼─────┐ ┌──────▼──────┐
        │ Controller  │ │   System    │
        │ 更新 UI     │ │ 响应状态    │
        └─────────────┘ └─────────────┘
```

## 各层职责速查表

| 层级             | 职责    | 可以做                 | 不能做                     |
|----------------|-------|---------------------|-------------------------|
| **View**       | UI 展示 | 渲染节点、接收输入           | 包含业务逻辑                  |
| **Controller** | 协调层   | 转发命令、监听事件、更新 UI     | 直接修改 Model              |
| **Command**    | 业务操作  | 调用 Model、使用 Utility | 持有状态、直接更新 UI            |
| **Model**      | 数据状态  | 存储数据、发送事件           | 知道 View、调用 Controller   |
| **Utility**    | 业务规则  | 无状态计算、验证            | 持有状态、依赖场景               |
| **System**     | 系统逻辑  | 监听事件、协调 Model       | 直接修改 Model（应通过 Command） |

## 设计原则

### 1. 单一职责原则（SRP）

每个类只做一件事：

```csharp
// ✅ Model 只负责状态
public class CounterModel : AbstractModel
{
    public int Count { get; private set; }
    public void Increment() { /* ... */ }
}

// ✅ Command 只负责操作
public class IncreaseCountCommand : AbstractCommand
{
    protected override void OnExecute() { /* ... */ }
}
```

### 2. 依赖倒置原则（DIP）

依赖抽象，不依赖具体实现：

```csharp
// ✅ 依赖接口
private ICounterModel _counterModel;

// ❌ 依赖具体类
private CounterModel _counterModel;
```

### 3. 开闭原则（OCP）

对扩展开放，对修改封闭：

```csharp
// ✅ 新增功能不修改现有代码
architecture.RegisterSystem(new NewFeatureSystem());

// ❌ 修改现有类添加功能
public class CounterModel
{
    // 每次新增功能都修改这个类
}
```

### 4. 事件驱动原则

通过事件解耦组件：

```csharp
// ✅ Model 不知道谁在监听
this.SendEvent(new ChangedCountEvent());

// ❌ Model 直接调用
_view.UpdateView();
```

### 5. 单向数据流

数据总是单向流动：

```
Action → Command → Model → Event → View/System
```

## 最佳实践

### 1. 接口设计

**✅ 推荐**：

```csharp
public interface ICounterModel : IModel
{
    int Count { get; }
    void Increment();
}
```

**❌ 不推荐**：

```csharp
public class CounterModel  // 没有接口
{
    public int Count { get; set; }  // 可被外部直接修改
}
```

### 2. 事件命名

**✅ 推荐**：

```csharp
public sealed record ChangedCountEvent  // 描述事件
{
    public int Count { get; init; }
}
```

**❌ 不推荐**：

```csharp
public class CountEvent { }  // 不清晰
public class Data { }        // 太泛化
```

### 3. Command 职责

**✅ 推荐**：

```csharp
protected override void OnExecute()
{
    // 1. 获取依赖
    var model = this.GetModel<ICounterModel>();
    var utility = this.GetUtility<ICounterUtility>();
    
    // 2. 检查规则
    if (!utility.CanIncrease(model.Count)) return;
    
    // 3. 执行操作
    model.Increment();
}
```

**❌ 不推荐**：

```csharp
protected override void OnExecute()
{
    _count++;  // 直接修改状态
    UpdateUI();  // 直接更新 UI
    PlaySound();  // 混入太多逻辑
}
```

### 4. Utility 设计

**✅ 推荐**：

```csharp
public bool CanIncrease(int current)
{
    return current < _maxCount;  // 纯函数
}
```

**❌ 不推荐**：

```csharp
private int _state;  // 持有状态

public void Increment()
{
    _state++;  // 修改状态
}
```

### 5. System 使用

**✅ 推荐**：

```csharp
protected override void OnInit()
{
    // 监听事件
    this.RegisterEvent<ChangedCountEvent>(e =>
    {
        CheckThreshold(e.Count);
    });
}
```

**❌ 不推荐**：

```csharp
public void UpdateCounter()
{
    // 直接修改 Model
    model.Count++;  // 应该通过 Command
}
```

## 常见问题 FAQ

### Q1: Model 可以调用其他 Model 吗？

**❌ 不推荐**：

```csharp
public class PlayerModel : AbstractModel
{
    public void Attack()
    {
        // 直接调用其他 Model
        this.GetModel<EnemyModel>().TakeDamage(10);
    }
}
```

**✅ 推荐**：通过 Command 或 System 协调：

```csharp
public class AttackCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        var player = this.GetModel<IPlayerModel>();
        var enemy = this.GetModel<IEnemyModel>();
        
        enemy.TakeDamage(player.AttackPower);
    }
}
```

---

### Q2: Command 可以嵌套调用吗？

**✅ 可以**：

```csharp
protected override void OnExecute()
{
    this.SendCommand(new SaveDataCommand());
    this.SendCommand(new UpdateUICommand());
}
```

但要注意：

- 避免循环依赖
- 考虑使用 System 协调复杂流程

---

### Q3: 什么时候用 Utility，什么时候用 System？

| 场景         | 使用      |
|------------|---------|
| 无状态计算      | Utility |
| 业务规则验证     | Utility |
| 响应状态变化     | System  |
| 协调多个 Model | System  |
| 触发系统级反应    | System  |

**示例**：

```csharp
// Utility：纯计算
utility.CanIncrease(count)

// System：状态响应
if (count > 10) PlaySound();
```

---

### Q4: Controller 可以直接调用 Model 吗？

**部分场景可以**：

```csharp
// ✅ 只读操作
var count = this.GetModel<ICounterModel>().Count;

// ❌ 修改操作（应通过 Command）
this.GetModel<ICounterModel>().Increment();
```

**原则**：

- 读取数据：可以直接调用
- 修改数据：应该通过 Command

---

### Q5: 如何处理异步操作？

使用 `AbstractAsyncCommand`：

```csharp
public class SaveDataCommand : AbstractAsyncCommand
{
    protected override async Task OnExecuteAsync()
    {
        var model = this.GetModel<ICounterModel>();
        await SaveToFileAsync(model.Count);
    }
}
```

---

### Q6: 如何在多个场景共享状态？

**Model 是全局的**：

```csharp
// 场景 A
var count = this.GetModel<ICounterModel>().Count;

// 场景 B
var count = this.GetModel<ICounterModel>().Count;
// 两者是同一个 Model 实例
```

如果需要场景独立的状态，考虑：

- 为每个场景创建独立的 Model
- 使用场景参数传递数据

---

### Q7: 如何测试这些组件？

**Model 测试**：

```csharp
[Test]
public void Increment_ShouldIncreaseCount()
{
    var model = new CounterModel();
    model.Increment();
    Assert.AreEqual(1, model.Count);
}
```

**Utility 测试**：

```csharp
[Test]
public void CanIncrease_WhenAtMax_ReturnsFalse()
{
    var utility = new CounterUtility(maxCount: 20);
    Assert.IsFalse(utility.CanIncrease(20));
}
```

**Command 测试**（需要 mock）：

```csharp
[Test]
public void ExecuteCommand_ShouldIncrementModel()
{
    // 需要 mock IArchitecture
    // 或使用集成测试
}
```

---

### Q8: 项目变大后如何组织代码？

**按功能模块划分**：

```
scripts/
├── counter/
│   ├── model/
│   ├── command/
│   └── system/
├── player/
│   ├── model/
│   ├── command/
│   └── system/
└── inventory/
    ├── model/
    ├── command/
    └── system/
```

**按层级划分**：

```
scripts/
├── model/
│   ├── CounterModel.cs
│   ├── PlayerModel.cs
│   └── InventoryModel.cs
├── command/
│   ├── counter/
│   ├── player/
│   └── inventory/
└── system/
    ├── CounterSystem.cs
    └── PlayerSystem.cs
```

选择适合团队的方式。

## 性能考虑

### 事件系统开销

**问题**：频繁发送事件会影响性能吗？

**答案**：

- GFramework 的事件系统经过优化，开销很小
- 对于游戏逻辑级别的事件（如计数变化），完全没问题
- 如果是高频事件（如每帧更新），考虑批处理

**示例**：

```csharp
// ❌ 高频事件（每帧发送）
public override void _Process(double delta)
{
    this.SendEvent(new PositionChangedEvent());
}

// ✅ 批处理或降频
private float _eventTimer;
public override void _Process(double delta)
{
    _eventTimer += (float)delta;
    if (_eventTimer > 0.1f)  // 每 100ms 发送一次
    {
        this.SendEvent(new PositionChangedEvent());
        _eventTimer = 0;
    }
}
```

### 依赖注入开销

**问题**：`GetModel()` 会影响性能吗？

**答案**：

- 第一次调用会查找，之后会缓存
- 建议在 `_Ready` 中获取并缓存

**示例**：

```csharp
// ✅ 缓存引用
private ICounterModel _counterModel;

public override void _Ready()
{
    _counterModel = this.GetModel<ICounterModel>();
}

public override void _Process(double delta)
{
    var count = _counterModel.Count;  // 使用缓存的引用
}
```

## 下一步学习

### 进阶主题

1. **高级命令模式**
    - 异步命令
    - 命令队列
    - 撤销/重做

2. **复杂事件系统**
    - 事件优先级
    - 事件过滤
    - 事件链

3. **高级 System**
    - System 之间的通信
    - System 生命周期管理
    - System 优先级

4. **规则系统**
    - 动态规则
    - 规则链
    - 规则引擎

5. **状态机**
    - 使用 GFramework 实现状态机
    - 分层状态机
    - 状态转换规则

### 推荐资源

- **GFramework 文档**：
    - [Core 核心框架](../../core/index.md)
    - [Game 游戏模块](../../game/index.md)
    - [Godot 集成](../../godot/index.md)
    - [源码生成器](../../source-generators/index.md)

- **设计模式**：
    - 命令模式（Command Pattern）
    - 观察者模式（Observer Pattern）
    - 依赖注入（Dependency Injection）

- **架构设计**：
    - Clean Architecture
    - MVC / MVVM
    - Event-Driven Architecture

## 项目示例

查看完整的示例项目：

- [基础教程（本教程）](https://github.com/GeWuYou/GFramework/tree/main/examples/Counter)
- [Godot 集成示例](https://github.com/GeWuYou/GFramework/tree/main/examples/GodotIntegration)
- [高级模式示例](https://github.com/GeWuYou/GFramework/tree/main/examples/AdvancedPatterns)

## 总结

通过本教程，你学到了：

### 核心概念

- ✅ Model：存储状态，发送事件
- ✅ Command：封装业务逻辑
- ✅ Controller：协调用户输入
- ✅ Utility：提供业务规则
- ✅ System：响应状态变化

### 设计原则

- ✅ 单一职责
- ✅ 依赖倒置
- ✅ 事件驱动
- ✅ 单向数据流
- ✅ 关注点分离

### 架构优势

- ✅ 可测试
- ✅ 可复用
- ✅ 可扩展
- ✅ 易维护
- ✅ 解耦合

## 结语

恭喜你完成了 GFramework 基础教程！🎉

你现在已经掌握了使用 GFramework 构建清晰、可维护的游戏架构的核心知识。

记住：

- **架构是为了解决问题，不是为了炫技**
- **从简单开始，逐步优化**
- **理解原则比记住代码更重要**

继续探索，享受编程的乐趣！

---

::: tip 反馈与支持

- 遇到问题？查看 [GitHub Issues](https://github.com/GeWuYou/GFramework/issues)
- 有建议？欢迎提交 PR 或 Issue
- 加入社区，与其他开发者交流
  :::

::: details 完整检查清单
**环境与项目**

- [ ] .NET SDK 和 Godot 已安装
- [ ] GFramework NuGet 包已引入
- [ ] 项目架构已搭建

**核心组件**

- [ ] Model 已创建并注册
- [ ] Command 已创建
- [ ] Utility 已创建并注册
- [ ] System 已创建并注册
- [ ] Controller 实现了 IController

**功能验证**

- [ ] 计数器功能正常
- [ ] 事件系统工作正常
- [ ] 上限限制生效
- [ ] 阈值检查触发

**理解验证**

- [ ] 理解了各层职责
- [ ] 理解了事件驱动架构
- [ ] 理解了单向数据流
- [ ] 理解了设计原则
  :::

👏 再次恭喜你完成教程！期待看到你用 GFramework 创造出精彩的项目！
