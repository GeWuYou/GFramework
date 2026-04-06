# GFramework.Ecs.Arch - C# 标准集成方式

## 为什么不使用 ModuleInitializer？

`ModuleInitializer` 在 C# 中主要用于：

1. **应用程序代码** - 初始化应用程序级别的状态
2. **高级源生成器** - 编译时代码生成

对于**库（Library）**来说，使用 `ModuleInitializer` 有以下问题：

- ❌ 不符合 .NET 生态习惯
- ❌ 缺乏显式控制
- ❌ 难以测试和调试
- ❌ 可能导致意外的副作用
- ❌ 违反"显式优于隐式"原则

## C# 中的标准做法

### 1. 依赖注入扩展方法（推荐）

这是 .NET 生态中最标准的做法，类似于：

- ASP.NET Core: `services.AddMvc()`
- Entity Framework: `services.AddDbContext()`
- SignalR: `services.AddSignalR()`

**优点：**

- ✅ 符合 .NET 生态习惯
- ✅ 显式、可控
- ✅ 易于测试
- ✅ 支持配置
- ✅ 支持链式调用

### 2. 使用方式

#### 基本用法

```csharp
public class MyArchitecture : Architecture
{
    protected override void OnConfigure()
    {
        // 显式添加 Arch ECS 支持
        Services.AddArch();
    }
}
```

#### 带配置的用法

```csharp
public class MyArchitecture : Architecture
{
    protected override void OnConfigure()
    {
        Services.AddArch(options =>
        {
            options.WorldCapacity = 2000;
            options.EnableStatistics = true;
            options.Priority = 50;
        });
    }
}
```

#### 容器级别的用法

```csharp
var container = new MicrosoftDiContainer();
container.AddArch(options =>
{
    options.WorldCapacity = 1000;
});
```

### 3. 对比其他方案

#### Spring Boot Starter（Java）

```java
// Spring Boot 使用自动配置
@SpringBootApplication
public class Application {
    // 自动扫描并加载 starter
}
```

#### ASP.NET Core（C#）

```csharp
// .NET 使用显式注册
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSwagger();
```

**C# 更倾向于显式注册**，因为：

1. 更清晰的依赖关系
2. 更好的 IDE 支持
3. 更容易调试和测试
4. 避免"魔法"行为

### 4. 其他常见模式

#### 4.1 Builder 模式

```csharp
var architecture = new ArchitectureBuilder()
    .AddArch()
    .AddLogging()
    .Build();
```

#### 4.2 Options 模式

```csharp
services.Configure<ArchOptions>(options =>
{
    options.WorldCapacity = 2000;
});
```

#### 4.3 静态工厂方法

```csharp
var module = ArchEcsModule.Create(options =>
{
    options.WorldCapacity = 1000;
});
```

## 迁移指南

### 从 ModuleInitializer 迁移

**之前（自动注册）：**

```csharp
// 只需引入包，自动注册
// 无需任何代码
```

**现在（显式注册）：**

```csharp
public class MyArchitecture : Architecture
{
    protected override void OnConfigure()
{
        Services.AddArch();
    }
}
```

### 优势

1. **更清晰** - 一眼就能看出使用了哪些模块
2. **更可控** - 可以决定何时、如何注册
3. **更灵活** - 可以传递配置参数
4. **更标准** - 符合 .NET 生态习惯

## 总结

C# 生态更倾向于**显式优于隐式**的设计哲学，因此：

- ✅ **推荐**：使用扩展方法 `AddArch()`
- ❌ **不推荐**：使用 `ModuleInitializer`

这样的设计：

1. 符合 .NET 生态习惯
2. 提供更好的开发体验
3. 更容易理解和维护
4. 避免 CA2255 警告
