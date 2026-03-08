---
title: 单元测试实践
description: 学习如何为 GFramework 项目编写单元测试
---

# 单元测试实践

## 学习目标

完成本教程后，你将能够：

- 创建和配置测试项目
- 为架构组件（Model、System、Controller）编写单元测试
- 测试事件系统的发送和订阅功能
- 测试命令和查询的执行
- 使用 Moq 模拟依赖项
- 编写集成测试验证完整流程
- 理解测试最佳实践

## 前置条件

- 已安装 .NET SDK 8.0 或更高版本
- 了解 C# 基础语法
- 熟悉 xUnit 或 NUnit 测试框架
- 阅读过[快速开始](/zh-CN/getting-started/quick-start)
- 了解[架构系统](/zh-CN/core/architecture)

## 步骤 1：创建测试项目

首先，创建一个测试项目并添加必要的依赖。

### 1.1 创建测试项目

```bash
# 创建测试项目
dotnet new nunit -n MyGame.Tests

# 添加项目引用
cd MyGame.Tests
dotnet add reference ../MyGame/MyGame.csproj
```

### 1.2 配置项目文件

编辑 `MyGame.Tests.csproj`，添加必要的包引用：

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <ImplicitUsings>disable</ImplicitUsings>
        <Nullable>enable</Nullable>
        <IsPackable>false</IsPackable>
    </PropertyGroup>

    <ItemGroup>
        <!-- 测试框架 -->
        <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.3.0" />
        <PackageReference Include="NUnit" Version="4.5.0" />
        <PackageReference Include="NUnit3TestAdapter" Version="6.1.0" />

        <!-- Mock 框架 -->
        <PackageReference Include="Moq" Version="4.20.72" />
    </ItemGroup>

    <ItemGroup>
        <!-- 项目引用 -->
        <ProjectReference Include="..\MyGame\MyGame.csproj" />
    </ItemGroup>

</Project>
```

### 1.3 创建 GlobalUsings.cs

创建 `GlobalUsings.cs` 文件，添加常用命名空间：

```csharp
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;
global using NUnit.Framework;
global using Moq;
global using GFramework.Core.Abstractions.architecture;
global using GFramework.Core.Abstractions.model;
global using GFramework.Core.Abstractions.system;
```

## 步骤 2：测试架构组件

### 2.1 测试 Model

Model 是数据层组件，我们需要测试其初始化和数据访问功能。

```csharp
using GFramework.Core.Abstractions.enums;
using GFramework.Core.model;

namespace MyGame.Tests.model;

/// <summary>
/// 测试模型类
/// </summary>
public sealed class TestModel : AbstractModel, ITestModel
{
    public const int DefaultXp = 5;

    public bool Initialized { get; private set; }

    public int GetCurrentXp { get; } = DefaultXp;

    public void Initialize()
    {
        Initialized = true;
    }

    public override void OnArchitecturePhase(ArchitecturePhase phase)
    {
    }

    protected override void OnInit()
    {
    }
}

/// <summary>
/// Model 测试类
/// </summary>
[TestFixture]
public class TestModelTests
{
    private TestModel _model = null!;

    [SetUp]
    public void SetUp()
    {
        _model = new TestModel();
    }

    [Test]
    public void Model_Should_Have_Default_Xp()
    {
        // Assert
        Assert.That(_model.GetCurrentXp, Is.EqualTo(TestModel.DefaultXp));
    }

    [Test]
    public void Model_Should_Initialize_Correctly()
    {
        // Act
        _model.Initialize();

        // Assert
        Assert.That(_model.Initialized, Is.True);
    }

    [Test]
    public void Model_Should_Not_Be_Initialized_By_Default()
    {
        // Assert
        Assert.That(_model.Initialized, Is.False);
    }
}
```

### 2.2 测试 System

System 是业务逻辑层组件，测试其初始化和销毁功能。

```csharp
using GFramework.Core.Abstractions.enums;
using GFramework.Core.Abstractions.system;

namespace MyGame.Tests.system;

/// <summary>
/// 测试系统类
/// </summary>
public sealed class TestSystem : ISystem
{
    private IArchitectureContext _context = null!;

    public bool Initialized { get; private set; }
    public bool DestroyCalled { get; private set; }

    public void SetContext(IArchitectureContext context)
    {
        _context = context;
    }

    public IArchitectureContext GetContext()
    {
        return _context;
    }

    public void Initialize()
    {
        Initialized = true;
    }

    public void Destroy()
    {
        DestroyCalled = true;
    }

    public void OnArchitecturePhase(ArchitecturePhase phase)
    {
    }
}

/// <summary>
/// System 测试类
/// </summary>
[TestFixture]
public class TestSystemTests
{
    private TestSystem _system = null!;
    private Mock&lt;IArchitectureContext&gt; _mockContext = null!;

    [SetUp]
    public void SetUp()
    {
        _system = new TestSystem();
        _mockContext = new Mock&lt;IArchitectureContext&gt;();
        _system.SetContext(_mockContext.Object);
    }

    [Test]
    public void System_Should_Initialize_Correctly()
    {
        // Act
        _system.Initialize();

        // Assert
        Assert.That(_system.Initialized, Is.True);
    }

    [Test]
    public void System_Should_Destroy_Correctly()
    {
        // Act
        _system.Destroy();

        // Assert
        Assert.That(_system.DestroyCalled, Is.True);
    }

    [Test]
    public void System_Should_Store_Context()
    {
        // Act
        var context = _system.GetContext();

        // Assert
        Assert.That(context, Is.EqualTo(_mockContext.Object));
    }
}
```

## 步骤 3：测试事件系统

事件系统是框架的核心功能之一，需要测试事件的注册、发送和取消注册。

```csharp
using GFramework.Core.events;

namespace MyGame.Tests.events;

/// <summary>
/// 测试事件类
/// </summary>
public class TestEvent
{
    public int ReceivedValue { get; init; }
}

/// <summary>
/// 事件总线测试类
/// </summary>
[TestFixture]
public class EventBusTests
{
    private EventBus _eventBus = null!;

    [SetUp]
    public void SetUp()
    {
        _eventBus = new EventBus();
    }

    [Test]
    public void Register_Should_Add_Handler()
    {
        // Arrange
        var called = false;

        // Act
        _eventBus.Register&lt;TestEvent&gt;(@event =&gt; { called = true; });
        _eventBus.Send&lt;TestEvent&gt;();

        // Assert
        Assert.That(called, Is.True);
    }

    [Test]
    public void UnRegister_Should_Remove_Handler()
    {
        // Arrange
        var count = 0;
        Action&lt;TestEvent&gt; handler = @event =&gt; { count++; };

        // Act
        _eventBus.Register(handler);
        _eventBus.Send&lt;TestEvent&gt;();
        Assert.That(count, Is.EqualTo(1));

        _eventBus.UnRegister(handler);
        _eventBus.Send&lt;TestEvent&gt;();

        // Assert
        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public void SendEvent_Should_Invoke_All_Handlers()
    {
        // Arrange
        var count1 = 0;
        var count2 = 0;

        // Act
        _eventBus.Register&lt;TestEvent&gt;(@event =&gt; { count1++; });
        _eventBus.Register&lt;TestEvent&gt;(@event =&gt; { count2++; });
        _eventBus.Send&lt;TestEvent&gt;();

        // Assert
        Assert.That(count1, Is.EqualTo(1));
        Assert.That(count2, Is.EqualTo(1));
    }

    [Test]
    public void SendEvent_Should_Pass_Event_Data()
    {
        // Arrange
        var receivedValue = 0;
        const int expectedValue = 100;

        // Act
        _eventBus.Register&lt;TestEvent&gt;(e =&gt; { receivedValue = e.ReceivedValue; });
        _eventBus.Send(new TestEvent { ReceivedValue = expectedValue });

        // Assert
        Assert.That(receivedValue, Is.EqualTo(expectedValue));
    }
}
```

## 步骤 4：测试命令和查询

命令和查询是 CQRS 模式的核心，需要测试其执行逻辑。

### 4.1 测试 Command

```csharp
using GFramework.Core.Abstractions.cqrs.command;
using GFramework.Core.command;

namespace MyGame.Tests.command;

/// <summary>
/// 测试命令输入
/// </summary>
public sealed class TestCommandInput : ICommandInput
{
    public int Value { get; init; }
}

/// <summary>
/// 测试命令
/// </summary>
public sealed class TestCommand : AbstractCommand&lt;TestCommandInput&gt;
{
    public TestCommand(TestCommandInput input) : base(input)
    {
    }

    public bool Executed { get; private set; }
    public int ExecutedValue { get; private set; }

    protected override void OnExecute(TestCommandInput input)
    {
        Executed = true;
        ExecutedValue = input.Value;
    }
}

/// <summary>
/// 带返回值的测试命令
/// </summary>
public sealed class TestCommandWithResult : AbstractCommand&lt;TestCommandInput, int&gt;
{
    public TestCommandWithResult(TestCommandInput input) : base(input)
    {
    }

    public bool Executed { get; private set; }

    protected override int OnExecute(TestCommandInput input)
    {
        Executed = true;
        return input.Value * 2;
    }
}

/// <summary>
/// 命令执行器测试类
/// </summary>
[TestFixture]
public class CommandExecutorTests
{
    private CommandExecutor _commandExecutor = null!;

    [SetUp]
    public void SetUp()
    {
        _commandExecutor = new CommandExecutor();
    }

    [Test]
    public void Send_Should_Execute_Command()
    {
        // Arrange
        var input = new TestCommandInput { Value = 42 };
        var command = new TestCommand(input);

        // Act
        _commandExecutor.Send(command);

        // Assert
        Assert.That(command.Executed, Is.True);
        Assert.That(command.ExecutedValue, Is.EqualTo(42));
    }

    [Test]
    public void Send_WithNullCommand_Should_ThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws&lt;ArgumentNullException&gt;(() =&gt; _commandExecutor.Send(null!));
    }

    [Test]
    public void Send_WithResult_Should_Return_Value()
    {
        // Arrange
        var input = new TestCommandInput { Value = 100 };
        var command = new TestCommandWithResult(input);

        // Act
        var result = _commandExecutor.Send(command);

        // Assert
        Assert.That(command.Executed, Is.True);
        Assert.That(result, Is.EqualTo(200));
    }
}
```

### 4.2 测试 Query

```csharp
using GFramework.Core.Abstractions.cqrs.query;
using GFramework.Core.query;

namespace MyGame.Tests.query;

/// <summary>
/// 测试查询输入
/// </summary>
public sealed class TestQueryInput : IQueryInput
{
    public int Value { get; init; }
}

/// <summary>
/// 整数查询
/// </summary>
public sealed class TestQuery : AbstractQuery&lt;TestQueryInput, int&gt;
{
    public TestQuery(TestQueryInput input) : base(input)
    {
    }

    protected override int OnDo(TestQueryInput input)
    {
        return input.Value * 2;
    }
}

/// <summary>
/// 字符串查询
/// </summary>
public sealed class TestStringQuery : AbstractQuery&lt;TestQueryInput, string&gt;
{
    public TestStringQuery(TestQueryInput input) : base(input)
    {
    }

    protected override string OnDo(TestQueryInput input)
    {
        return $"Result: {input.Value * 2}";
    }
}

/// <summary>
/// 查询执行器测试类
/// </summary>
[TestFixture]
public class QueryExecutorTests
{
    private QueryExecutor _queryExecutor = null!;

    [SetUp]
    public void SetUp()
    {
        _queryExecutor = new QueryExecutor();
    }

    [Test]
    public void Send_Should_Return_Query_Result()
    {
        // Arrange
        var input = new TestQueryInput { Value = 10 };
        var query = new TestQuery(input);

        // Act
        var result = _queryExecutor.Send(query);

        // Assert
        Assert.That(result, Is.EqualTo(20));
    }

    [Test]
    public void Send_WithNullQuery_Should_ThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws&lt;ArgumentNullException&gt;(() =&gt; _queryExecutor.Send&lt;int&gt;(null!));
    }

    [Test]
    public void Send_WithStringResult_Should_Return_String()
    {
        // Arrange
        var input = new TestQueryInput { Value = 5 };
        var query = new TestStringQuery(input);

        // Act
        var result = _queryExecutor.Send(query);

        // Assert
        Assert.That(result, Is.EqualTo("Result: 10"));
    }
}
```

## 步骤 5：使用 Mock 和 Stub

使用 Moq 框架模拟依赖项，实现单元测试的隔离。

### 5.1 Mock 接口依赖

```csharp
using GFramework.Core.Abstractions.model;
using GFramework.Core.system;

namespace MyGame.Tests.mock;

/// <summary>
/// 玩家数据接口
/// </summary>
public interface IPlayerModel : IModel
{
    int GetScore();
    void AddScore(int points);
}

/// <summary>
/// 游戏系统（依赖 IPlayerModel）
/// </summary>
public class GameSystem : AbstractSystem
{
    private IPlayerModel _playerModel = null!;

    protected override void OnInit()
    {
        _playerModel = this.GetModel&lt;IPlayerModel&gt;();
    }

    public int CalculateBonus()
    {
        var score = _playerModel.GetScore();
        return score * 2;
    }
}

/// <summary>
/// Mock 测试示例
/// </summary>
[TestFixture]
public class MockExampleTests
{
    private Mock&lt;IPlayerModel&gt; _mockPlayerModel = null!;
    private Mock&lt;IArchitectureContext&gt; _mockContext = null!;
    private GameSystem _gameSystem = null!;

    [SetUp]
    public void SetUp()
    {
        // 创建 Mock 对象
        _mockPlayerModel = new Mock&lt;IPlayerModel&gt;();
        _mockContext = new Mock&lt;IArchitectureContext&gt;();

        // 设置 Mock 行为
        _mockContext
            .Setup(ctx =&gt; ctx.GetModel&lt;IPlayerModel&gt;())
            .Returns(_mockPlayerModel.Object);

        // 创建被测试对象
        _gameSystem = new GameSystem();
        _gameSystem.SetContext(_mockContext.Object);
        _gameSystem.Initialize();
    }

    [Test]
    public void CalculateBonus_Should_Return_Double_Score()
    {
        // Arrange
        _mockPlayerModel.Setup(m =&gt; m.GetScore()).Returns(100);

        // Act
        var bonus = _gameSystem.CalculateBonus();

        // Assert
        Assert.That(bonus, Is.EqualTo(200));
    }

    [Test]
    public void CalculateBonus_Should_Call_GetScore()
    {
        // Arrange
        _mockPlayerModel.Setup(m =&gt; m.GetScore()).Returns(50);

        // Act
        _gameSystem.CalculateBonus();

        // Assert
        _mockPlayerModel.Verify(m =&gt; m.GetScore(), Times.Once);
    }
}
```

### 5.2 验证方法调用

```csharp
namespace MyGame.Tests.mock;

[TestFixture]
public class VerificationTests
{
    [Test]
    public void Should_Verify_Method_Called_With_Specific_Arguments()
    {
        // Arrange
        var mock = new Mock&lt;IPlayerModel&gt;();

        // Act
        mock.Object.AddScore(10);

        // Assert
        mock.Verify(m =&gt; m.AddScore(10), Times.Once);
    }

    [Test]
    public void Should_Verify_Method_Never_Called()
    {
        // Arrange
        var mock = new Mock&lt;IPlayerModel&gt;();

        // Assert
        mock.Verify(m =&gt; m.AddScore(It.IsAny&lt;int&gt;()), Times.Never);
    }

    [Test]
    public void Should_Verify_Property_Access()
    {
        // Arrange
        var mock = new Mock&lt;IPlayerModel&gt;();
        mock.Setup(m =&gt; m.GetScore()).Returns(100);

        // Act
        var score = mock.Object.GetScore();

        // Assert
        mock.VerifyGet(m =&gt; m.GetScore(), Times.Once);
    }
}
```

## 步骤 6：集成测试

集成测试验证多个组件协同工作的完整流程。

### 6.1 创建测试架构

```csharp
using GFramework.Core.architecture;
using GFramework.Core.Abstractions.enums;

namespace MyGame.Tests.integration;

/// <summary>
/// 测试架构基类
/// </summary>
public abstract class TestArchitectureBase : Architecture&lt;TestArchitectureBase&gt;
{
    public bool InitCalled { get; private set; }
    public ArchitecturePhase CurrentPhase { get; private set; }
    public List&lt;ArchitecturePhase&gt; PhaseHistory { get; } = new();

    protected override void OnInitialize()
    {
        InitCalled = true;
    }

    public override void OnArchitecturePhase(ArchitecturePhase phase)
    {
        CurrentPhase = phase;
        PhaseHistory.Add(phase);
        base.OnArchitecturePhase(phase);
    }
}

/// <summary>
/// 同步测试架构
/// </summary>
public sealed class SyncTestArchitecture : TestArchitectureBase
{
    protected override void OnInitialize()
    {
        RegisterModel(new TestModel());
        RegisterSystem(new TestSystem());
        base.OnInitialize();
    }
}

/// <summary>
/// 架构集成测试
/// </summary>
[TestFixture]
[NonParallelizable]
public class ArchitectureIntegrationTests
{
    private SyncTestArchitecture? _architecture;

    [SetUp]
    public void SetUp()
    {
        _architecture = new SyncTestArchitecture();
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_architecture != null)
        {
            await _architecture.DestroyAsync();
            _architecture = null;
        }
    }

    [Test]
    public void Architecture_Should_Initialize_All_Components_Correctly()
    {
        // Act
        _architecture!.Initialize();

        // Assert
        Assert.That(_architecture.InitCalled, Is.True);
        Assert.That(_architecture.CurrentPhase, Is.EqualTo(ArchitecturePhase.Ready));

        var context = _architecture.Context;
        var model = context.GetModel&lt;TestModel&gt;();
        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Initialized, Is.True);

        var system = context.GetSystem&lt;TestSystem&gt;();
        Assert.That(system, Is.Not.Null);
        Assert.That(system!.Initialized, Is.True);
    }

    [Test]
    public void Architecture_Should_Enter_Phases_In_Correct_Order()
    {
        // Act
        _architecture!.Initialize();

        // Assert
        var phases = _architecture.PhaseHistory;
        CollectionAssert.AreEqual(
            new[]
            {
                ArchitecturePhase.BeforeUtilityInit,
                ArchitecturePhase.AfterUtilityInit,
                ArchitecturePhase.BeforeModelInit,
                ArchitecturePhase.AfterModelInit,
                ArchitecturePhase.BeforeSystemInit,
                ArchitecturePhase.AfterSystemInit,
                ArchitecturePhase.Ready
            },
            phases
        );
    }

    [Test]
    public async Task Architecture_Destroy_Should_Destroy_All_Systems()
    {
        // Arrange
        _architecture!.Initialize();

        // Act
        await _architecture.DestroyAsync();

        // Assert
        var system = _architecture.this.GetSystem&lt;TestSystem&gt;();
        Assert.That(system!.DestroyCalled, Is.True);
        Assert.That(_architecture.CurrentPhase, Is.EqualTo(ArchitecturePhase.Destroyed));
    }

    [Test]
    public void Event_Should_Be_Received()
    {
        // Arrange
        _architecture!.Initialize();
        var context = _architecture.Context;
        var receivedValue = 0;
        const int targetValue = 100;

        // Act
        context.RegisterEvent&lt;TestEvent&gt;(e =&gt; { receivedValue = e.ReceivedValue; });
        context.SendEvent(new TestEvent { ReceivedValue = targetValue });

        // Assert
        Assert.That(receivedValue, Is.EqualTo(targetValue));
    }
}
```

### 6.2 测试 BindableProperty

```csharp
using GFramework.Core.property;

namespace MyGame.Tests.property;

[TestFixture]
public class BindablePropertyTests
{
    [Test]
    public void Value_Get_Should_Return_Default_Value()
    {
        // Arrange
        var property = new BindableProperty&lt;int&gt;(5);

        // Assert
        Assert.That(property.Value, Is.EqualTo(5));
    }

    [Test]
    public void Value_Set_Should_Trigger_Event()
    {
        // Arrange
        var property = new BindableProperty&lt;int&gt;();
        var receivedValue = 0;

        // Act
        property.Register(value =&gt; { receivedValue = value; });
        property.Value = 42;

        // Assert
        Assert.That(receivedValue, Is.EqualTo(42));
    }

    [Test]
    public void Value_Set_To_Same_Value_Should_Not_Trigger_Event()
    {
        // Arrange
        var property = new BindableProperty&lt;int&gt;(5);
        var count = 0;

        // Act
        property.Register(_ =&gt; { count++; });
        property.Value = 5;

        // Assert
        Assert.That(count, Is.EqualTo(0));
    }

    [Test]
    public void UnRegister_Should_Remove_Handler()
    {
        // Arrange
        var property = new BindableProperty&lt;int&gt;();
        var count = 0;
        Action&lt;int&gt; handler = _ =&gt; { count++; };

        // Act
        property.Register(handler);
        property.Value = 1;
        Assert.That(count, Is.EqualTo(1));

        property.UnRegister(handler);
        property.Value = 2;

        // Assert
        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public void RegisterWithInitValue_Should_Call_Handler_Immediately()
    {
        // Arrange
        var property = new BindableProperty&lt;int&gt;(5);
        var receivedValue = 0;

        // Act
        property.RegisterWithInitValue(value =&gt; { receivedValue = value; });

        // Assert
        Assert.That(receivedValue, Is.EqualTo(5));
    }
}
```

## 完整代码示例

以下是一个完整的测试项目结构示例：

```
MyGame.Tests/
├── GlobalUsings.cs
├── MyGame.Tests.csproj
├── model/
│   └── TestModelTests.cs
├── system/
│   └── TestSystemTests.cs
├── events/
│   └── EventBusTests.cs
├── command/
│   └── CommandExecutorTests.cs
├── query/
│   └── QueryExecutorTests.cs
├── mock/
│   ├── MockExampleTests.cs
│   └── VerificationTests.cs
├── property/
│   └── BindablePropertyTests.cs
└── integration/
    └── ArchitectureIntegrationTests.cs
```

## 运行测试

### 使用命令行

```bash
# 运行所有测试
dotnet test

# 运行特定测试类
dotnet test --filter "FullyQualifiedName~EventBusTests"

# 运行特定测试方法
dotnet test --filter "FullyQualifiedName~EventBusTests.Register_Should_Add_Handler"

# 生成测试覆盖率报告
dotnet test --collect:"XPlat Code Coverage"
```

### 使用 IDE

在 Visual Studio 或 Rider 中：

1. 打开测试资源管理器
2. 选择要运行的测试
3. 点击"运行"或"调试"按钮

## 测试输出示例

```
正在启动测试执行，请稍候...
总共 1 个测试文件与指定模式匹配。

通过!  - 失败:     0, 通过:    15, 跳过:     0, 总计:    15, 持续时间: 234 ms

测试运行成功。
测试总数: 15
     通过: 15
 总时间: 1.2345 秒
```

## 测试最佳实践

### 1. 遵循 AAA 模式

```csharp
[Test]
public void Example_Test()
{
    // Arrange - 准备测试数据和依赖
    var input = new TestInput { Value = 10 };

    // Act - 执行被测试的操作
    var result = PerformOperation(input);

    // Assert - 验证结果
    Assert.That(result, Is.EqualTo(20));
}
```

### 2. 测试命名规范

使用清晰的命名约定：

```csharp
// 格式: MethodName_Scenario_ExpectedBehavior
[Test]
public void Send_WithNullCommand_Should_ThrowArgumentNullException()
{
    // ...
}
```

### 3. 一个测试一个断言

```csharp
// 好的做法
[Test]
public void Model_Should_Initialize()
{
    _model.Initialize();
    Assert.That(_model.Initialized, Is.True);
}

// 避免
[Test]
public void Model_Should_Work()
{
    _model.Initialize();
    Assert.That(_model.Initialized, Is.True);
    Assert.That(_model.GetXp(), Is.EqualTo(5));
    Assert.That(_model.Name, Is.Not.Null);
}
```

### 4. 使用 SetUp 和 TearDown

```csharp
[TestFixture]
public class MyTests
{
    private MyClass _instance = null!;

    [SetUp]
    public void SetUp()
    {
        // 每个测试前执行
        _instance = new MyClass();
    }

    [TearDown]
    public void TearDown()
    {
        // 每个测试后执行
        _instance?.Dispose();
    }
}
```

### 5. 测试边界条件

```csharp
[Test]
[TestCase(0)]
[TestCase(-1)]
[TestCase(int.MinValue)]
public void AddScore_WithInvalidValue_Should_ThrowException(int invalidScore)
{
    Assert.Throws&lt;ArgumentException&gt;(() =&gt; _model.AddScore(invalidScore));
}
```

### 6. 使用测试数据生成器

```csharp
[Test]
[TestCase(1, 2)]
[TestCase(5, 10)]
[TestCase(100, 200)]
public void CalculateBonus_Should_Return_Double(int input, int expected)
{
    var result = Calculator.CalculateBonus(input);
    Assert.That(result, Is.EqualTo(expected));
}
```

## 下一步

完成本教程后，你可以：

1. **提高测试覆盖率**
    - 使用代码覆盖率工具（如 Coverlet）
    - 目标：达到 80% 以上的代码覆盖率

2. **学习 TDD（测试驱动开发）**
    - 先写测试，再写实现
    - 红-绿-重构循环

3. **集成 CI/CD**
    - 在 GitHub Actions 中自动运行测试
    - 配置测试失败时阻止合并

4. **性能测试**
    - 使用 BenchmarkDotNet 进行性能测试
    - 测试关键路径的性能

5. **探索高级测试技术**
    - 参数化测试
    - 数据驱动测试
    - 快照测试

## 相关资源

- [NUnit 官方文档](https://docs.nunit.org/)
- [Moq 快速入门](https://github.com/moq/moq4/wiki/Quickstart)
- [架构设计模式](/zh-CN/best-practices/architecture-patterns)
- [性能优化最佳实践](/zh-CN/best-practices/performance)
