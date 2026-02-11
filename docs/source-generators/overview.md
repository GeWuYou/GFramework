# 源码生成器概览

GFramework 源码生成器基于 Roslyn 编译器平台，自动生成常用的样板代码，提高开发效率并减少错误。

## 核心特性

### ⚡ 自动代码生成

- **日志代码生成** - 自动生成日志字段和方法调用
- **枚举扩展生成** - 为枚举类型生成实用扩展方法
- **规则代码生成** - 根据规则定义生成验证代码
- **性能优化** - 编译时生成，运行时零开销

### 🎯 智能识别

- **特性驱动** - 通过特性标记触发生成
- **上下文感知** - 理解代码语义和结构
- **类型安全** - 生成的代码完全类型安全
- **错误预防** - 编译时检查，避免运行时错误

## 核心生成器

### 日志生成器

- **[LogAttribute](/source-generators/logging-generator)** - 自动生成日志字段
- **ContextAwareAttribute** - 生成上下文感知代码
- **性能监控** - 自动生成方法执行时间记录

### 枚举扩展生成器

- **[EnumExtensions](/source-generators/enum-extensions)** - 生成枚举实用方法
- **字符串转换** - 枚举与字符串的双向转换
- **描述获取** - 从特性中提取枚举描述

### 规则生成器

- **[RuleGenerator](/source-generators/rule-generator)** - 生成验证规则代码
- **业务规则** - 基于规则定义生成验证逻辑
- **数据验证** - 自动生成数据完整性检查

## 使用场景

源码生成器适用于需要大量样板代码的场景：

- **日志记录** - 统一的日志格式和级别管理
- **枚举处理** - 频繁的枚举转换和操作
- **数据验证** - 复杂的业务规则验证
- **性能监控** - 方法执行时间跟踪

## 安装配置

```xml
<PackageReference Include="GeWuYou.GFramework.SourceGenerators" 
                  Version="1.0.0" 
                  PrivateAssets="all" 
                  ExcludeAssets="runtime" />
```

## 快速示例

### 日志生成器

```csharp
[Log]  // 自动生成 Logger 字段
public partial class PlayerController : IController
{
    // 生成的代码：
    // private ILogger _logger;
    // public ILogger Logger => _logger ??= LoggerFactory.Create(GetType());
    
    public void Attack()
    {
        Logger.Info("Player attacking");  // 直接使用生成的 Logger
        // 业务逻辑...
    }
}
```

### 枚举扩展生成

```csharp
[GenerateEnumExtensions]  // 生成枚举扩展方法
public enum PlayerState
{
    [Description("空闲")]
    Idle,
    
    [Description("移动中")]
    Moving,
    
    [Description("攻击中")]
    Attacking
}

// 生成的扩展方法：
// PlayerStateExtensions.GetDescription()
// PlayerStateExtensions.FromString()
// PlayerStateExtensions.GetAllValues()
```

### 规则生成器

```csharp
[GenerateValidationRules]  // 生成验证规则
public class PlayerData
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Name { get; set; }
    
    [Range(0, 100)]
    public int Health { get; set; }
    
    [Email]
    public string Email { get; set; }
}

// 生成的验证方法：
// PlayerDataValidator.Validate()
// PlayerDataValidator.ValidateName()
// PlayerDataValidator.ValidateHealth()
```

## 生成器配置

### 全局配置

```xml
<PropertyGroup>
  <GFrameworkLogLevel>Debug</GFrameworkLogLevel>
  <GFrameworkGenerateEnums>true</GFrameworkGenerateEnums>
  <GFrameworkGenerateValidation>true</GFrameworkGenerateValidation>
</PropertyGroup>
```

### 特性配置

```csharp
[Log(LogLevel = LogLevel.Debug, IncludeCallerInfo = true)]
[GenerateEnumExtensions(CamelCase = true)]
[GenerateValidationRules(ThrowOnInvalid = true)]
```

## 性能优势

### 编译时生成

- **零运行时开销** - 代码在编译时生成
- **类型安全** - 编译时类型检查
- **IDE 支持** - 生成的代码完全支持 IntelliSense

### 代码质量

- **一致性** - 统一的代码风格和模式
- **错误减少** - 自动生成减少手写错误
- **维护性** - 集中管理样板代码逻辑

## 学习路径

建议按以下顺序学习源码生成器：

1. **[日志生成器](/source-generators/logging-generator)** - 基础的日志代码生成
2. **[枚举扩展](/source-generators/enum-extensions)** - 枚举处理自动化
3. **[规则生成器](/source-generators/rule-generator)** - 复杂验证逻辑生成

## 调试生成的代码

### 查看生成的代码

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

### 调试配置

```csharp
// 在调试时查看生成的代码
#if DEBUG
// 生成的代码会被写入到指定目录
#endif
```

## 最佳实践

### 1. 合理使用特性

```csharp
// ✅ 好的做法：在需要的地方使用
[Log]
public class ImportantService { }

// ❌ 避免过度使用
[Log]  // 不是每个类都需要日志
public class SimpleDataClass { }
```

### 2. 配置优化

```xml
<!-- 生产环境优化 -->
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <GFrameworkLogLevel>Warning</GFrameworkLogLevel>
</PropertyGroup>

<!-- 开发环境详细日志 -->
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <GFrameworkLogLevel>Debug</GFrameworkLogLevel>
</PropertyGroup>
```

### 3. 性能考虑

```csharp
// ✅ 生成器适合复杂场景
[GenerateValidationRules]  // 复杂验证逻辑
public class BusinessEntity { }

// ❌ 简单场景手动实现更高效
public class SimpleConfig 
{
    public string Name { get; set; }
    // 简单属性不需要生成器
}
```

## 下一步

- [深入了解日志生成器](/source-generators/logging-generator)
- [学习枚举扩展生成](/source-generators/enum-extensions)
- [掌握规则生成器](/source-generators/rule-generator)
- [查看生成器 API](/api-reference/generators-api)