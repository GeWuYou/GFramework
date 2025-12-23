# GFramework 日志代码生成器 (LogAttribute)

## 概述

GFramework 提供了一个强大的日志代码生成器，类似于 Java 的 `@Slf4j` 注解。通过在类上使用 `[Log]` 特性，编译器会自动为该类生成一个日志记录器字段，让您在类的任何地方都能方便地使用日志记录功能。

## 快速开始

### 1. 基本使用

在类上添加 `[Log]` 特性：

```csharp
using GFramework.Generator.Attributes.generator.logging;

[Log]
public partial class MyService
{
    public void DoSomething()
    {
        // 自动生成的 Log 字段可以直接使用
        Log.Info("开始执行操作");
        
        try
        {
            // 业务逻辑
            Log.Debug("执行业务逻辑", new { Operation = "DoSomething" });
            Log.Info("操作执行成功");
        }
        catch (Exception ex)
        {
            Log.Error("操作执行失败", ex);
        }
    }
}
```

编译后，生成器会自动为该类添加：

```csharp
public partial class MyService
{
    private static ILog Log = Log.CreateLogger("MyService");
}
```

### 2. 自定义类别名称

```csharp
[Log("CustomService")]
public partial class MyService
{
    // 生成的 logger 类别为 "CustomService" 而不是 "MyService"
}
```

### 3. 自定义字段配置

```csharp
[Log(FieldName = "Logger", AccessModifier = "protected", IsStatic = false)]
public partial class MyService
{
    // 生成: protected ILog Logger = Log.CreateLogger("MyService");
}
```

## 特性参数说明

### LogAttribute 构造函数参数

- **category** (string, 可选): 指定日志类别，默认为类名

### 命名参数

- **FieldName** (string, 默认 "Log"): 指定生成的字段名称
- **AccessModifier** (string, 默认 "private"): 指定字段的访问修饰符
- **IsStatic** (bool, 默认 true): 指定字段是否为静态的

## 使用示例

### 在系统中的使用

```csharp
[Log("System")]
public partial class GameSystem : AbstractSystem
{
    protected override void OnInit()
    {
        Log.Info("GameSystem 初始化开始");
        
        // 初始化逻辑
        Log.Debug("正在加载游戏数据...");
        Log.Info("GameSystem 初始化完成");
    }
    
    protected override void OnUpdate(float deltaTime)
    {
        Log.Trace("系统更新", new { DeltaTime = deltaTime });
    }
    
    protected override void OnDestroy()
    {
        Log.Info("GameSystem 销毁");
    }
}
```

### 在模型中的使用

```csharp
[Log("Model")]
public partial class UserModel : AbstractModel
{
    public string UserName { get; set; }
    
    public void SetUserName(string userName)
    {
        Log.Debug("设置用户名", new { OldValue = UserName, NewValue = userName });
        UserName = userName;
        Log.Info("用户名设置完成", new { UserName = userName });
    }
}
```

### 在工具类中的使用

```csharp
[Log("Utility")]
public partial class FileUtility
{
    public void SaveFile(string filePath, string content)
    {
        Log.Debug("开始保存文件", new { FilePath = filePath });
        
        try
        {
            // 文件保存逻辑
            Log.Info("文件保存成功", new { FilePath = filePath });
        }
        catch (Exception ex)
        {
            Log.Error("文件保存失败", ex, new { FilePath = filePath });
        }
    }
}
```

### 网络服务中的使用

```csharp
[Log("Network")]
public partial class NetworkService
{
    public async Task<string> GetDataAsync(string url)
    {
        Log.Debug("发起网络请求", new { Url = url, Method = "GET" });
        
        try
        {
            var response = await httpClient.GetStringAsync(url);
            Log.Info("网络请求成功", new { Url = url, ResponseLength = response.Length });
            return response;
        }
        catch (Exception ex)
        {
            Log.Error("网络请求失败", ex, new { Url = url });
            throw;
        }
    }
}
```

### 数据库服务中的使用

```csharp
[Log("Database")]
public partial class DatabaseService
{
    public async Task<User> GetUserAsync(int userId)
    {
        Log.Debug("查询用户信息", new { UserId = userId });
        
        try
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user != null)
            {
                Log.Info("用户查询成功", new { UserId = userId, UserName = user.Name });
            }
            else
            {
                Log.Warn("用户不存在", new { UserId = userId });
            }
            return user;
        }
        catch (Exception ex)
        {
            Log.Error("用户查询失败", ex, new { UserId = userId });
            throw;
        }
    }
}
```

## 高级配置示例

### 使用静态字段

```csharp
[Log(IsStatic = true)] // 默认配置
public partial class StaticService
{
    public static void StaticMethod()
    {
        Log.Info("静态方法调用");
    }
}
```

### 使用实例字段

```csharp
[Log(IsStatic = false)]
public partial class InstanceService
{
    private readonly string _instanceId;
    
    public InstanceService(string instanceId)
    {
        _instanceId = instanceId;
    }
    
    public void InstanceMethod()
    {
        Log.Info("实例方法调用", new { InstanceId = _instanceId });
    }
}
```

### 使用受保护的字段

```csharp
[Log(AccessModifier = "protected")]
public partial class BaseService
{
    // 子类可以访问 protected 字段
}

[Log] // 派生类也可以有自己的日志记录器
public partial class DerivedService : BaseService
{
    public void DerivedMethod()
    {
        Log.Info("派生类方法");
        // 也可以访问基类的 protected Log 字段
    }
}
```

## 最佳实践

### 1. 使用合适的日志类别名称

```csharp
// 好的做法：使用有意义的类别名称
[Log("UserService")]
public partial class UserService { }

// 避免：使用过于通用的类别名称
[Log("Service")]
public partial class UserService { }
```

### 2. 合理设置日志级别

```csharp
[Log("BusinessLogic")]
public partial class BusinessService
{
    public void ProcessOrder(Order order)
    {
        // 使用 Info 记录重要的业务流程
        Log.Info("开始处理订单", new { OrderId = order.Id });
        
        // 使用 Debug 记录调试信息
        Log.Debug("订单验证通过", new { OrderId = order.Id });
        
        // 使用 Warning 记录异常情况
        if (order.Amount > 10000)
        {
            Log.Warn("大额订单", new { OrderId = order.Id, Amount = order.Amount });
        }
        
        // 使用 Error 记录错误
        try
        {
            // 业务逻辑
        }
        catch (Exception ex)
        {
            Log.Error("订单处理失败", ex, new { OrderId = order.Id });
        }
    }
}
```

### 3. 记录上下文信息

```csharp
[Log("UserManagement")]
public partial class UserManager
{
    public void UpdateUserProfile(int userId, UserProfile profile)
    {
        Log.Info("更新用户资料", new 
        { 
            UserId = userId, 
            Changes = profile.GetChanges(),
            Timestamp = DateTime.Now
        });
    }
}
```

### 4. 在异常处理中使用日志

```csharp
[Log("DataAccess")]
public partial class UserRepository
{
    public async Task<User> GetUserAsync(int userId)
    {
        try
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }
        catch (DbException ex)
        {
            Log.Error("数据库查询失败", ex, new { UserId = userId });
            throw new DataAccessException("无法获取用户信息", ex);
        }
    }
}
```

## 框架组件集成

### 在 GFramework 架构中使用

```csharp
[Log("Architecture")]
public partial class ExampleArchitecture : AbstractArchitecture
{
    protected override void Init()
    {
        Log.Info("ExampleArchitecture 初始化开始");
        
        // 注册系统
        RegisterSystem(new GameSystem());
        RegisterSystem(new UISystem());
        
        // 注册模型
        RegisterModel(new UserModel());
        RegisterModel(new GameModel());
        
        // 注册工具
        RegisterUtility(new FileUtility());
        
        Log.Info("ExampleArchitecture 初始化完成");
    }
}
```

### 在事件处理中使用

```csharp
[Log("Event")]
public partial class UserEventHandler
{
    [Log("EventHandler")]
    public void OnUserLoginEvent(UserLoginEvent evt)
    {
        Log.Info("用户登录事件", new 
        { 
            UserId = evt.UserId, 
            LoginTime = evt.LoginTime,
            IpAddress = evt.IpAddress
        });
    }
    
    [Log("EventHandler")]
    public void OnUserLogoutEvent(UserLogoutEvent evt)
    {
        Log.Info("用户登出事件", new 
        { 
            UserId = evt.UserId, 
            LogoutTime = evt.LogoutTime,
            SessionDuration = evt.SessionDuration
        });
    }
}
```

## 注意事项

1. **部分类**: 使用 `[Log]` 特性的类必须是 `partial` 类，因为生成器会添加字段到同一个类中。

2. **命名空间引用**: 确保项目中引用了 `GFramework.Generator.Attributes` 包。

3. **编译时生成**: 日志字段是在编译时生成的，不会影响运行时性能。

4. **多线程安全**: 生成的 `ILog` 实例是线程安全的，可以在多线程环境中使用。

5. **继承**: 派生类可以有自己的 `[Log]` 特性，也可以访问基类的受保护日志字段。

## 性能考虑

- **编译时生成**: 字段在编译时生成，没有运行时开销
- **延迟初始化**: 日志记录器按需创建，避免不必要的对象创建
- **级别检查**: 生成的代码包含级别检查，性能与手动创建的日志记录器相同

## 与现有日志系统的兼容性

生成的代码完全兼容现有的 `GFramework.Core.logging` 系统：

```csharp
// 现有的方式仍然可以使用
var manualLogger = Log.CreateLogger("Manual");

// 新生成的字段也可以正常使用
[Log]
public partial class MyService
{
    public void Method()
    {
        // 两种方式都可以使用
        manualLogger.Info("手动创建的日志");
        Log.Info("自动生成的日志");
    }
}
```

通过使用 `[Log]` 特性，您可以显著减少样板代码，提高开发效率，同时保持日志记录的一致性和灵活性。