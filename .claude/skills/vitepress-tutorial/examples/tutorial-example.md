---
title: 创建第一个 Model
description: 学习如何创建和使用 Model 来管理应用数据
---

# 创建第一个 Model

## 学习目标

完成本教程后，你将能够：
- 理解 Model 在架构中的作用
- 创建自定义的 Model 类
- 在架构中注册 Model
- 从 Controller 中访问 Model
- 使用可绑定属性管理数据

## 前置条件

- 已安装 GFramework.Core NuGet 包
- 了解 C# 基础语法
- 阅读过[架构概览](/zh-CN/getting-started)

## 步骤 1：创建 Model 类

首先，我们需要创建一个 Model 来存储玩家数据。Model 负责管理应用的数据和状态。

```csharp
using GFramework.Core.Abstractions.model;
using GFramework.Core.Abstractions.property;

namespace MyGame.Models
{
    /// <summary>
    /// 玩家数据模型
    /// </summary>
    public class PlayerModel : IModel
    {
        // 玩家名称（可绑定属性）
        public BindableProperty<string> Name { get; } = new("Player");

        // 玩家生命值
        public BindableProperty<int> Health { get; } = new(100);

        // 玩家金币
        public BindableProperty<int> Gold { get; } = new(0);

        // 玩家等级
        public BindableProperty<int> Level { get; } = new(1);

        /// <summary>
        /// Model 初始化方法
        /// </summary>
        public void Init()
        {
            // 在这里可以进行初始化操作
            // 例如：从配置文件加载默认值
        }
    }
}
```

**代码说明**：
- `IModel` 接口标识这是一个数据模型
- `BindableProperty<T>` 是可绑定属性，值变化时会自动通知监听者
- `Init()` 方法在 Model 注册到架构时被调用
- 使用属性初始化器设置默认值

## 步骤 2：在架构中注册 Model

创建架构类并注册 Model：

```csharp
using GFramework.Core.architecture;
using MyGame.Models;

namespace MyGame
{
    /// <summary>
    /// 游戏架构
    /// </summary>
    public class GameArchitecture : Architecture
    {
        // 单例访问点
        public static IArchitecture Interface { get; private set; }

        /// <summary>
        /// 初始化架构
        /// </summary>
        protected override void Init()
        {
            Interface = this;

            // 注册 Model
            RegisterModel(new PlayerModel());
        }
    }
}
```

**代码说明**：
- 继承 `Architecture` 基类
- 在 `Init()` 方法中注册 Model
- 提供静态属性 `Interface` 用于全局访问架构

## 步骤 3：创建 Controller 访问 Model

创建 Controller 来使用 Model：

```csharp
using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.controller;
using GFramework.Core.extensions;
using MyGame.Models;

namespace MyGame.Controllers
{
    /// <summary>
    /// 游戏控制器
    /// </summary>
    public class GameController : IController
    {
        /// <summary>
        /// 获取架构实例
        /// </summary>
        public IArchitecture GetArchitecture() => GameArchitecture.Interface;

        /// <summary>
        /// 初始化玩家数据
        /// </summary>
        public void InitializePlayer()
        {
            // 获取 PlayerModel
            var playerModel = this.GetModel<PlayerModel>();

            // 设置玩家数据
            playerModel.Name.Value = "勇者";
            playerModel.Health.Value = 100;
            playerModel.Gold.Value = 50;
            playerModel.Level.Value = 1;

            // 监听属性变化
            playerModel.Health.RegisterOnValueChanged(health =>
            {
                Console.WriteLine($"玩家生命值变化: {health}");

                if (health <= 0)
                {
                    Console.WriteLine("玩家死亡！");
                }
            });
        }

        /// <summary>
        /// 玩家受到伤害
        /// </summary>
        public void TakeDamage(int damage)
        {
            var playerModel = this.GetModel<PlayerModel>();
            playerModel.Health.Value -= damage;
        }

        /// <summary>
        /// 玩家获得金币
        /// </summary>
        public void AddGold(int amount)
        {
            var playerModel = this.GetModel<PlayerModel>();
            playerModel.Gold.Value += amount;
        }
    }
}
```

**代码说明**：
- 实现 `IController` 接口
- 通过 `this.GetModel<T>()` 扩展方法获取 Model
- 使用 `.Value` 访问和修改属性值
- 使用 `RegisterOnValueChanged` 监听属性变化

## 步骤 4：初始化并使用架构

在程序入口点初始化架构：

```csharp
using MyGame;
using MyGame.Controllers;

// 1. 创建并初始化架构
var architecture = new GameArchitecture();
architecture.Initialize();

// 2. 等待架构就绪
await architecture.WaitUntilReadyAsync();

// 3. 创建 Controller 并使用
var gameController = new GameController();

// 初始化玩家
gameController.InitializePlayer();

// 玩家受到伤害
gameController.TakeDamage(20);
// 输出: 玩家生命值变化: 80

// 玩家获得金币
gameController.AddGold(100);
```

**代码说明**：
- 创建架构实例并调用 `Initialize()`
- 使用 `WaitUntilReadyAsync()` 等待架构就绪
- 创建 Controller 实例并调用方法

## 完整代码

### PlayerModel.cs

```csharp
using GFramework.Core.Abstractions.model;
using GFramework.Core.Abstractions.property;

namespace MyGame.Models
{
    public class PlayerModel : IModel
    {
        public BindableProperty<string> Name { get; } = new("Player");
        public BindableProperty<int> Health { get; } = new(100);
        public BindableProperty<int> Gold { get; } = new(0);
        public BindableProperty<int> Level { get; } = new(1);

        public void Init() { }
    }
}
```

### GameArchitecture.cs

```csharp
using GFramework.Core.architecture;
using MyGame.Models;

namespace MyGame
{
    public class GameArchitecture : Architecture
    {
        public static IArchitecture Interface { get; private set; }

        protected override void Init()
        {
            Interface = this;
            RegisterModel(new PlayerModel());
        }
    }
}
```

### GameController.cs

```csharp
using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.controller;
using GFramework.Core.extensions;
using MyGame.Models;

namespace MyGame.Controllers
{
    public class GameController : IController
    {
        public IArchitecture GetArchitecture() => GameArchitecture.Interface;

        public void InitializePlayer()
        {
            var playerModel = this.GetModel<PlayerModel>();
            playerModel.Name.Value = "勇者";
            playerModel.Health.Value = 100;
            playerModel.Gold.Value = 50;
            playerModel.Level.Value = 1;

            playerModel.Health.RegisterOnValueChanged(health =>
            {
                Console.WriteLine($"玩家生命值变化: {health}");
                if (health <= 0)
                {
                    Console.WriteLine("玩家死亡！");
                }
            });
        }

        public void TakeDamage(int damage)
        {
            var playerModel = this.GetModel<PlayerModel>();
            playerModel.Health.Value -= damage;
        }

        public void AddGold(int amount)
        {
            var playerModel = this.GetModel<PlayerModel>();
            playerModel.Gold.Value += amount;
        }
    }
}
```

### Program.cs

```csharp
using MyGame;
using MyGame.Controllers;

var architecture = new GameArchitecture();
architecture.Initialize();
await architecture.WaitUntilReadyAsync();

var gameController = new GameController();
gameController.InitializePlayer();
gameController.TakeDamage(20);
gameController.AddGold(100);
```

## 运行结果

运行程序后，你将看到以下输出：

```
玩家生命值变化: 100
玩家生命值变化: 80
```

**验证步骤**：
1. 程序成功启动，没有异常
2. 控制台输出生命值变化信息
3. 玩家数据正确更新

## 下一步

恭喜！你已经学会了如何创建和使用 Model。接下来可以学习：

- [创建第一个 System](/zh-CN/tutorials/create-first-system) - 学习如何创建业务逻辑层
- [使用命令系统](/zh-CN/tutorials/use-command-system) - 学习如何封装操作
- [使用事件系统](/zh-CN/tutorials/use-event-system) - 学习组件间通信

## 相关文档

- [Model 层](/zh-CN/core/model) - Model 详细说明
- [属性系统](/zh-CN/core/property) - 可绑定属性详解
- [架构组件](/zh-CN/core/architecture) - 架构基础
- [Controller 层](/zh-CN/core/controller) - Controller 详细说明
