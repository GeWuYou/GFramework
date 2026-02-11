# 枚举扩展生成器

> GFramework.SourceGenerators 自动生成枚举扩展方法

## 概述

枚举扩展生成器为枚举类型自动生成常用的扩展方法，如获取描述、转换为字符串、解析等。这大大简化了枚举的操作。

## 基本用法

### 标记枚举

```csharp
using GFramework.SourceGenerators.Attributes;

[EnumExtensions]
public enum PlayerState
{
    Idle,
    Running,
    Jumping,
    Attacking
}
```

### 生成的方法

上面的代码会被转换为：

```csharp
public static class PlayerStateExtensions
{
    public static string GetDescription(this PlayerState value)
    {
        // 返回枚举的描述
    }

    public static bool HasFlag(this PlayerState value, PlayerState flag)
    {
        // 检查是否包含标志
    }

    public static PlayerState FromString(string value)
    {
        // 从字符串解析枚举
    }
}
```

## 常用方法

### 获取描述

```csharp
[EnumExtensions]
public enum ItemQuality
{
    [Description("普通")]
    Common,
    
    [Description("稀有")]
    Rare,
    
    [Description("史诗")]
    Epic
}

public void PrintQuality(ItemQuality quality)
{
    // 获取描述文本
    Console.WriteLine(quality.GetDescription());
    // 输出: "普通" / "稀有" / "史诗"
}
```

### 安全解析

```csharp
public void ParseState(string input)
{
    // 安全地解析字符串为枚举
    if (PlayerState.Running.TryParse(input, out var state))
    {
        Console.WriteLine($"状态: {state}");
    }
}
```

### 获取所有值

```csharp
public void ListAllStates()
{
    // 获取所有枚举值
    foreach (var state in PlayerState.GetAllValues())
    {
        Console.WriteLine(state);
    }
}
```

## 标志枚举

对于使用 `[Flags]` 特性的枚举：

```csharp
[EnumExtensions]
[Flags]
public enum PlayerPermissions
{
    None = 0,
    Read = 1,
    Write = 2,
    Execute = 4,
    All = Read | Write | Execute
}

public void CheckPermissions()
{
    var permissions = PlayerPermissions.Read | PlayerPermissions.Write;

    // 检查是否包含特定权限
    if (permissions.HasFlag(PlayerPermissions.Write))
    {
        Console.WriteLine("有写入权限");
    }

    // 获取所有设置的标志
    foreach (var flag in permissions.GetFlags())
    {
        Console.WriteLine($"权限: {flag}");
    }
}
```

## 自定义行为

### 忽略某些值

```csharp
[EnumExtensions(IgnoreValues = new[] { ItemQuality.Undefined })]
public enum ItemQuality
{
    Undefined,
    Common,
    Rare,
    Epic
}

// GetAllValues() 不会返回 Undefined
```

### 自定义转换

```csharp
[EnumExtensions(CaseSensitive = false)]
public enum Difficulty
{
    Easy,
    Medium,
    Hard
}

// FromString("EASY") 也能正确解析
```

---

**相关文档**：

- [Source Generators 概述](./index)
- [日志生成器](./logging-generator)
- [规则生成器](./rule-generator)
