# 故障排除与调试

本指南帮助你诊断和解决 GFramework 使用中的常见问题。

## 常见错误

### 1. 组件未注册错误

**错误信息**：`KeyNotFoundException: 未找到类型为 'XXX' 的组件`

**原因**：尝试获取未注册的组件。

**解决方案**：

```csharp
// ❌ 错误：未注册 PlayerModel
var arch = new GameArchitecture();
arch.Initialize();
var model = arch.GetModel<PlayerModel>(); // 抛出异常

// ✅ 正确：先注册再获取
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        RegisterModel(new PlayerModel()); // 注册模型
    }
}
```

### 2. 事件监听器未触发

**问题**：注册了事件监听器但没有被调用。

**原因**：

- 事件类型不匹配
- 监听器在事件发送前注销
- 事件发送时使用了错误的类型

**解决方案**：

```csharp
// ❌ 错误：事件类型不匹配
this.RegisterEvent<PlayerDiedEvent>(OnPlayerDied);
this.SendEvent(new PlayerAttackedEvent()); // 不会触发

// ✅ 正确：事件类型匹配
this.RegisterEvent<PlayerAttackedEvent>(OnPlayerAttacked);
this.SendEvent(new PlayerAttackedEvent()); // 正确触发
```

### 3. 内存泄漏

**问题**：应用内存持续增长。

**原因**：

- 未注销事件监听器
- 未注销属性监听器
- 未销毁 Architecture

**解决方案**：

```csharp
// ✅ 正确：使用 UnRegisterList 管理注销
private IUnRegisterList _unregisterList = new UnRegisterList();

public void Initialize()
{
    this.RegisterEvent<Event1>(OnEvent1)
        .AddToUnregisterList(_unregisterList);

    model.Property.Register(OnPropertyChanged)
        .AddToUnregisterList(_unregisterList);
}

public void Cleanup()
{
    _unregisterList.UnRegisterAll();
}

// ✅ 销毁架构
architecture.Destroy();
```

### 4. 循环依赖

**问题**：两个系统相互依赖导致死循环。

**原因**：系统间直接调用而不是通过事件通信。

**解决方案**：

```csharp
// ❌ 错误：直接调用导致循环依赖
public class SystemA : AbstractSystem
{
    private void OnEvent(EventA e)
    {
        var systemB = this.GetSystem<SystemB>();
        systemB.DoSomething(); // 可能导致循环
    }
}

// ✅ 正确：使用事件解耦
public class SystemA : AbstractSystem
{
    private void OnEvent(EventA e)
    {
        this.SendEvent(new EventB()); // 发送事件
    }
}

public class SystemB : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<EventB>(OnEventB);
    }
}
```

## 调试技巧

### 1. 启用日志

```csharp
// 创建自定义日志系统
public class DebugLogger : ILogger
{
    public void Log(string message)
    {
        Console.WriteLine($"[GFramework] {message}");
    }
}

// 在架构中注册
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        RegisterUtility(new DebugLogger());
    }
}
```

### 2. 追踪事件流

```csharp
// 创建事件追踪系统
public class EventTracer : AbstractSystem
{
    protected override void OnInit()
    {
        // 监听所有事件（需要手动注册）
        this.RegisterEvent<PlayerDiedEvent>(e =>
            Console.WriteLine("Event: PlayerDiedEvent"));
        this.RegisterEvent<PlayerAttackedEvent>(e =>
            Console.WriteLine("Event: PlayerAttackedEvent"));
    }
}
```

### 3. 检查组件状态

```csharp
// 创建调试工具
public class ArchitectureDebugger
{
    public static void PrintArchitectureState(IArchitecture arch)
    {
        Console.WriteLine("=== Architecture State ===");

        // 打印已注册的模型
        Console.WriteLine("Models:");
        // 需要通过反射或其他方式访问

        // 打印已注册的系统
        Console.WriteLine("Systems:");
        // 需要通过反射或其他方式访问
    }
}
```

### 4. 单元测试调试

```csharp
[Test]
public void DebugPlayerDamage()
{
    var arch = new TestArchitecture();
    arch.Initialize();

    var player = arch.GetModel<PlayerModel>();

    // 打印初始状态
    Console.WriteLine($"Initial Health: {player.Health.Value}");

    // 发送伤害事件
    arch.SendEvent(new DamageEvent { Amount = 10 });

    // 打印最终状态
    Console.WriteLine($"Final Health: {player.Health.Value}");

    // 验证
    Assert.AreEqual(90, player.Health.Value);
}
```

## 性能问题

### 1. 事件处理缓慢

**问题**：事件处理耗时过长。

**诊断**：

```csharp
// 测量事件处理时间
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
arch.SendEvent(new HeavyEvent());
stopwatch.Stop();
Console.WriteLine($"Event processing time: {stopwatch.ElapsedMilliseconds}ms");
```

**优化**：

```csharp
// ❌ 低效：在事件处理中进行复杂计算
private void OnEvent(HeavyEvent e)
{
    for (int i = 0; i < 1000000; i++)
    {
        // 复杂计算
    }
}

// ✅ 高效：异步处理
private async void OnEvent(HeavyEvent e)
{
    await Task.Run(() =>
    {
        for (int i = 0; i < 1000000; i++)
        {
            // 复杂计算
        }
    });
}
```

### 2. 频繁的 GetModel 调用

**问题**：每帧都调用 GetModel 导致性能下降。

**优化**：

```csharp
// ❌ 低效
public void Update()
{
    var model = _architecture.GetModel<PlayerModel>(); // 每帧调用
    model.Health.Value -= 1;
}

// ✅ 高效
private PlayerModel _playerModel;

public void Initialize()
{
    _playerModel = _architecture.GetModel<PlayerModel>(); // 只调用一次
}

public void Update()
{
    _playerModel.Health.Value -= 1;
}
```

## 常见问题排查清单

- [ ] 所有组件都已注册？
- [ ] 事件类型是否匹配？
- [ ] 是否正确注销了监听器？
- [ ] Architecture 是否已初始化？
- [ ] 是否有循环依赖？
- [ ] 内存使用是否持续增长？
- [ ] 事件处理是否过于复杂？
- [ ] 是否缓存了频繁访问的组件？

## 获取帮助

如果问题仍未解决：

1. 查看 [Core 文档](/zh-CN/core/) 了解更多细节
2. 查看 [架构组件](/zh-CN/core/architecture) 了解架构设计
3. 在 [GitHub Issues](https://github.com/GeWuYou/GFramework/issues) 提交问题
4. 查看 [教程](/zh-CN/tutorials/) 中的示例代码

---

**提示**：在提交 Issue 时，请提供：

- 错误信息和堆栈跟踪
- 最小化的复现代码
- 你的环境信息（.NET 版本、IDE 等）
