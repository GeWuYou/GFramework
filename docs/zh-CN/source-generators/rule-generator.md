# 规则生成器

> GFramework.SourceGenerators 自动生成规则验证代码

## 概述

规则生成器为实现了规则接口的类型自动生成验证方法。这使得规则定义更加简洁，并确保规则的一致性。

## 基本用法

### 定义规则

```csharp
using GFramework.SourceGenerators.Attributes;

[RuleFor(typeof(Player))]
public class PlayerRule : IRule<Player>
{
    public RuleResult Validate(Player player)
    {
        if (player.Health <= 0)
        {
            return RuleResult.Invalid("玩家生命值不能为零或负数");
        }

        if (string.IsNullOrEmpty(player.Name))
        {
            return RuleResult.Invalid("玩家名称不能为空");
        }

        return RuleResult.Valid();
    }
}
```

### 使用生成的验证器

```csharp
public class PlayerValidator
{
    // 自动生成 Validate 方法
    public void ValidatePlayer(Player player)
    {
        var result = PlayerRuleValidator.Validate(player);
        
        if (!result.IsValid)
        {
            Console.WriteLine($"验证失败: {result.ErrorMessage}");
        }
    }
}
```

## 组合规则

### 多规则组合

```csharp
[RuleFor(typeof(Player), RuleCombinationType.And)]
public class PlayerHealthRule : IRule<Player>
{
    public RuleResult Validate(Player player)
    {
        if (player.Health <= 0)
            return RuleResult.Invalid("生命值必须大于0");
        return RuleResult.Valid();
    }
}

[RuleFor(typeof(Player), RuleCombinationType.And)]
public class PlayerNameRule : IRule<Player>
{
    public RuleResult Validate(Player player)
    {
        if (string.IsNullOrEmpty(player.Name))
            return RuleResult.Invalid("名称不能为空");
        return RuleResult.Valid();
    }
}
```

### 验证所有规则

```csharp
public void ValidateAll(Player player)
{
    var results = PlayerRuleValidator.ValidateAll(player);
    
    foreach (var result in results)
    {
        if (!result.IsValid)
        {
            Console.WriteLine(result.ErrorMessage);
        }
    }
}
```

## 异步规则

```csharp
[RuleFor(typeof(Player), IsAsync = true)]
public class AsyncPlayerRule : IAsyncRule<Player>
{
    public async Task<RuleResult> ValidateAsync(Player player)
    {
        // 异步验证，如检查服务器
        var isBanned = await CheckBanStatus(player.Id);
        
        if (isBanned)
        {
            return RuleResult.Invalid("玩家已被封禁");
        }
        
        return RuleResult.Valid();
    }
}
```

## 最佳实践

### 1. 单一职责

```csharp
// 推荐：每个规则只验证一个方面
[RuleFor(typeof(Player))]
public class PlayerHealthRule : IRule<Player> { }

[RuleFor(typeof(Player))]
public class PlayerNameRule : IRule<Player> { }

// 避免：在一个规则中验证多个方面
[RuleFor(typeof(Player))]
public class PlayerMegaRule : IRule<Player>
{
    public RuleResult Validate(Player player)
    {
        // 验证健康、名称、等级...
        // 不要这样设计
    }
}
```

### 2. 清晰的错误信息

```csharp
public RuleResult Validate(Player player)
{
    // 推荐：具体的错误信息
    return RuleResult.Invalid($"玩家 {player.Name} 的生命值 {player.Health} 不能小于 1");
    
    // 避免：模糊的错误信息
    return RuleResult.Invalid("无效的玩家");
}
```

---

**相关文档**：

- [Source Generators 概述](./overview)
- [日志生成器](./logging-generator)
- [枚举扩展生成器](./enum-generator)
