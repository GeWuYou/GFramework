# Model 包使用说明

## 概述

Model 包定义了数据模型层的接口和基类。Model 是 MVC 架构中的 M 层，负责管理应用程序的数据和状态。Model
层应该只包含数据和简单的数据逻辑，不包含复杂的业务逻辑。

## 核心接口

### [`IModel`](./model.md)

模型接口，定义了模型的基本行为和功能。

**继承的能力接口：**

- [`IContextAware`](./rule.md) - 上下文感知接口
- [`ILogAware`](./rule.md) - 日志感知接口

**核心方法：**

```csharp
void Init();  // 初始化模型
void OnArchitecturePhase(ArchitecturePhase phase);  // 处理架构阶段事件
```

### [`ICanGetModel`](./model.md)

标记接口，表示实现者可以获取模型。继承自 [`IBelongToArchitecture`](./rule.md)。

## 核心类

### [`AbstractModel`](./model.md)

抽象模型基类，实现IModel接口，提供模型的基础实现。该类继承自[
`ContextAwareBase`](./rule.md)，提供了上下文感知能力。

**使用示例：**

```csharp
public class PlayerModel : AbstractModel
{
    // 使用 BindableProperty 定义可监听的属性
    public BindableProperty<string> Name { get; } = new("Player");
    public BindableProperty<int> Level { get; } = new(1);
    public BindableProperty<int> Health { get; } = new(100);
    public BindableProperty<int> MaxHealth { get; } = new(100);
    
    protected override void OnInit()
    {
        // 监听生命值变化
        Health.Register(newHealth =>
        {
            if (newHealth <= 0)
            {
                this.SendEvent(new PlayerDiedEvent());
            }
        });
    }
    
    public override void OnArchitecturePhase(ArchitecturePhase phase)
    {
        switch (phase)
        {
            case ArchitecturePhase.Initializing:
                // 架构初始化阶段的处理
                break;
            case ArchitecturePhase.Ready:
                // 架构就绪阶段的处理
                break;
            // ... 其他阶段处理
        }
    }
}
```

## Model 的职责

### ✅ 应该做的事

1. **存储数据和状态**
2. **提供数据访问接口**
3. **监听自身属性变化并做出响应**
4. **发送数据变化事件**
5. **处理架构生命周期事件**

### ❌ 不应该做的事

1. **不包含复杂业务逻辑** - 业务逻辑应该在 System 中
2. **不直接依赖其他 Model** - 通过 Command 协调
3. **不包含 UI 逻辑** - UI 逻辑应该在 Controller 中

## 常见 Model 示例

### 玩家数据模型

```csharp
public class PlayerModel : AbstractModel
{
    public BindableProperty<string> PlayerId { get; } = new("");
    public BindableProperty<string> PlayerName { get; } = new("Player");
    public BindableProperty<int> Level { get; } = new(1);
    public BindableProperty<int> Health { get; } = new(100);
    public BindableProperty<int> MaxHealth { get; } = new(100);
    public BindableProperty<int> Gold { get; } = new(0);
    
    public Vector3 Position { get; set; }
    
    protected override void OnInit()
    {
        Health.Register(hp =>
        {
            if (hp <= 0)
            {
                this.SendEvent(new PlayerDiedEvent());
            }
        });
    }
    
    public override void OnArchitecturePhase(ArchitecturePhase phase)
    {
        switch (phase)
        {
            case ArchitecturePhase.Ready:
                // 模型准备好后的处理
                _log?.Log("PlayerModel is ready.");
                break;
            default:
                break;
        }
    }
}
```

### 游戏状态模型

```csharp
public class GameModel : AbstractModel
{
    public BindableProperty<GameState> State { get; } = new(GameState.Menu);
    public BindableProperty<int> Score { get; } = new(0);
    public BindableProperty<int> HighScore { get; } = new(0);
    public BindableProperty<int> CurrentLevel { get; } = new(1);
    
    protected override void OnInit()
    {
        Score.Register(newScore =>
        {
            if (newScore > HighScore.Value)
            {
                HighScore.Value = newScore;
                this.SendEvent(new NewHighScoreEvent { Score = newScore });
            }
        });
    }
    
    public override void OnArchitecturePhase(ArchitecturePhase phase)
    {
        switch (phase)
        {
            case ArchitecturePhase.ShuttingDown:
                // 游戏模型清理工作
                break;
            default:
                break;
        }
    }
}
```

## 最佳实践

1. **使用 BindableProperty 存储需要监听的数据**
2. **Model 之间不要直接调用，通过 Command/System 协调**
3. **复杂计算和业务逻辑放在 System 中**
4. **使用事件通知数据的重要变化**
5. **保持 Model 简单纯粹，只做数据管理**

## 相关包

- [`architecture`](./architecture.md) - 提供 Model 的注册和获取
- [`property`](./property.md) - BindableProperty 用于定义可监听属性
- [`events`](./events.md) - Model 发送事件通知变化
- [`utility`](./utility.md) - Model 可以使用工具类
- [`extensions`](./extensions.md) - 提供 GetModel 等扩展方法