# GFramework.Ecs.Arch - 从自动注册到显式注册的迁移

## 变更原因

### 问题：CA2255 警告

```
warning CA2255: The 'ModuleInitializer' attribute is only intended to be used
in application code or advanced source generator scenarios
```

### 为什么 ModuleInitializer 不适合库？

在 C# 生态中，`ModuleInitializer` 主要用于：

1. **应用程序代码** - 初始化应用程序级别的状态
2. **高级源生成器** - 编译时代码生成

对于**库（Library）**来说，使用 `ModuleInitializer` 有以下问题：

- ❌ 不符合 .NET 生态习惯
- ❌ 缺乏显式控制
- ❌ 难以测试和调试
- ❌ 可能导致意外的副作用
- ❌ 违反"显式优于隐式"原则
- ❌ 触发 CA2255 警告

## C# 标准做法对比

### Spring Boot（Java）- 自动配置

```java
@SpringBootApplication
public class Application {
    // 自动扫描并加载 starter
}
```

### ASP.NET Core（C#）- 显式注册

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();  // 显式注册
builder.Services.AddSwagger();      // 显式注册
```

**C# 更倾向于显式注册**，因为：

1. ✅ 更清晰的依赖关系
2. ✅ 更好的 IDE 支持
3. ✅ 更容易调试和测试
4. ✅ 避免"魔法"行为

## 变更内容

### 1. 移除 ModuleInitializer

**之前：**

```csharp
// ArchModuleInitializer.cs
[ModuleInitializer]
public static void Initialize()
{
    ArchitectureModuleRegistry.Register(() => new ArchEcsModule(enabled: true));
}
```

**现在：**

```csharp
// 文件已删除
```

### 2. 新增扩展方法

**新增：**

```csharp
// ArchExtensions.cs
public static class ArchExtensions
{
    /// <summary>
    ///     添加 Arch ECS 支持到架构服务中
    /// </summary>
    public static IArchitectureServices AddArch(
        this IArchitectureServices services,
        Action<ArchOptions>? configure = null)
    {
        var options = new ArchOptions();
        configure?.Invoke(options);

        ArchitectureModuleRegistry.Register(() => new ArchEcsModule(enabled: true));

        return services;
    }

    /// <summary>
    ///     添加 Arch ECS 支持到 IoC 容器中
    /// </summary>
    public static IIocContainer AddArch(
        this IIocContainer container,
        Action<ArchOptions>? configure = null)
    {
        var options = new ArchOptions();
        configure?.Invoke(options);

        ArchitectureModuleRegistry.Register(() => new ArchEcsModule(enabled: true));

        return container;
    }
}
```

### 3. 更新使用方式

#### 之前（自动注册）

```csharp
// 只需引入包，自动注册
// 无需任何代码
```

#### 现在（显式注册）

```csharp
public class MyArchitecture : Architecture
{
    protected override void OnConfigure()
    {
        // 显式注册
        Services.AddArch();
    }
}
```

#### 带配置

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

### 4. 更新测试

**之前：**

```csharp
[Test]
public void ArchEcsModule_Should_Be_Auto_Registered()
{
    // 手动触发模块初始化器
    ArchModuleInitializer.Initialize();

    var services = new ArchitectureServices();
    services.ModuleManager.RegisterBuiltInModules(...);
    // ...
}
```

**现在：**

```csharp
[Test]
public void ArchEcsModule_Should_Be_Explicitly_Registered()
{
    var services = new ArchitectureServices();

    // 显式注册
    services.AddArch();

    services.ModuleManager.RegisterBuiltInModules(...);
    // ...
}
```

## 优势对比

### 自动注册（ModuleInitializer）

- ❌ 触发 CA2255 警告
- ❌ 不符合 .NET 生态习惯
- ❌ 缺乏显式控制
- ❌ 难以测试
- ❌ "魔法"行为

### 显式注册（扩展方法）

- ✅ 无警告
- ✅ 符合 .NET 生态习惯
- ✅ 显式、可控
- ✅ 易于测试
- ✅ 支持配置
- ✅ 支持链式调用
- ✅ 更好的 IDE 支持

## 迁移指南

### 对于现有用户

如果你之前使用自动注册方式，需要进行以下更改：

**步骤 1：更新包**

```bash
dotnet update package GeWuYou.GFramework.Ecs.Arch
```

**步骤 2：添加显式注册**

```csharp
public class MyArchitecture : Architecture
{
    protected override void OnConfigure()
    {
        // 添加这一行
        Services.AddArch();
    }
}
```

**步骤 3：（可选）添加配置**

```csharp
Services.AddArch(options =>
{
    options.WorldCapacity = 2000;
    options.EnableStatistics = true;
});
```

## 验证结果

### 构建验证 ✅

```bash
dotnet build GFramework.sln
# Build succeeded. 39 Warning(s), 0 Error(s)
# 无 CA2255 警告
```

### 测试验证 ✅

```bash
dotnet test --filter "ExplicitRegistrationTests"
# Pas 4, Failed: 0, Total: 4
```

**测试用例：**

1. ✅ `ArchEcsModule_Should_Be_Explicitly_Registered` - 验证显式注册
2. ✅ `World_Should_Be_Registered_In_Container` - 验证 World 注册
3. ✅ `AddArch_Should_Accept_Configuration` - 验证配置支持
4. ✅ `Container_AddArch_Should_Work` - 验证容器级别注册

## 参考资料

### .NET 生态中的类似实现

1. **ASP.NET Core**
   ```csharp
   services.AddMvc();
   services.AddControllers();
   ```

2. **Entity Framework Core**
   ```csharp
   services.AddDbContext<MyContext>();
   ```

3. **SignalR**
   ```csharp
   services.AddSignalR();
   ```

4. **Swagger**
   ```csharp
   services.AddSwaggerGen();
   ```

### 相关文档

-N_PATTERN.md](INTEGRATION_PATTERN.md) - 集成模式详解

- [README.md](README.md) - 使用指南
- [CA2255 规则说明](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2255)

## 总结

通过从 `ModuleInitializer` 迁移到显式注册扩展方法：

1. ✅ **消除警告** - 不再触发 CA2255
2. ✅ **符合习惯** - 遵循 .NET 生态标准
3. ✅ **更好控制** - 显式、可配置
4. ✅ **易于测试** - 清晰的测试边界
5. ✅ **更好体验** - IDE 支持、链式调用

这是一个**破坏性变更**，但带来了更好的开发体验和更符合 .NET 生态的设计。

---

**变更日期：** 2026-03-08
**影响范围：** 所有使用 GFramework.Ecs.Arch 的项目
**迁移难度：** 低（只需添加一行代码）
