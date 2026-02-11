# 日志生成器

> GFramework.SourceGenerators 自动生成日志代码，减少样板代码

## 概述

日志生成器是一个 Source Generator，它会自动为标记了 `[Log]` 特性的类生成 Logger 字段和日志方法调用。这消除了手动编写日志代码的需要，让开发者专注于业务逻辑。

## 基本用法

### 标记类

```csharp
using GFramework.SourceGenerators.Attributes;

[Log]
public partial class MyService
{
    public void DoSomething()
    {
        // 自动生成 Logger 字段
        // 自动生成日志调用
        Logger.Info("执行操作");
    }
}
```

### 生成代码

上面的代码会被编译时转换为：

```csharp
public partial class MyService
{
    // 自动生成的字段
    [CompilerGenerated]
    private ILogger _logger;

    // 自动生成的属性
    [CompilerGenerated]
    public ILogger Logger
    {
        get
        {
            if (_logger == null)
            {
                _logger = LoggerFactory.CreateLogger<MyService>();
            }
            return _logger;
        }
    }
}
```

## 日志级别

生成的日志方法支持多种级别：

```csharp
[Log]
public partial class MyClass
{
    public void Example()
    {
        // 调试信息
        Logger.Debug($"调试信息: {value}");

        // 普通信息
        Logger.Info("操作成功");

        // 警告
        Logger.Warning($"警告: {message}");

        // 错误
        Logger.Error($"错误: {ex.Message}");

        // 严重错误
        Logger.Critical("系统故障");
    }
}
```

## 自定义日志类别

```csharp
[Log(LogCategory.Gameplay)]
public partial class GameplaySystem
{
    // 日志会标记为 Gameplay 类别
    public void Update()
    {
        Logger.Info("游戏逻辑更新");
    }
}
```

## 与其他模块集成

### 与 Godot 集成

```csharp
[Log]
[ContextAware]
public partial class GodotController : Node
{
    public override void _Ready()
    {
        Logger.Info("控制器已准备就绪");
    }
}
```

### 与架构集成

```csharp
[Log]
public partial class MySystem : AbstractSystem
{
    protected override void OnInit()
    {
        Logger.Info("系统初始化");
    }
}
```

## 配置选项

### 禁用自动生成

```csharp
// 禁用自动日志调用生成
[Log(AutoLog = false)]
public partial class MyClass
{
    // 仍会生成 Logger 字段，但不会自动生成日志调用
    public void DoSomething()
    {
        // 需要手动调用 Logger
        Logger.Info("手动日志");
    }
}
```

### 自定义字段名称

```csharp
[Log(FieldName = "_customLogger")]
public partial class MyClass
{
    // Logger 字段名称为 _customLogger
}
```

---

**相关文档**：

- [Source Generators 概述](./overview)
- [枚举扩展生成器](./enum-generator)
- [规则生成器](./rule-generator)
- [API 参考](../api-reference/source-generators-api)
