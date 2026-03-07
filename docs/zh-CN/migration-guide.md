# 版本迁移指南

本文档提供 GFramework 不同版本之间的迁移指导，帮助开发者平滑升级到新版本。

## 概述

### 迁移指南的使用

本迁移指南旨在帮助开发者：

- **了解版本间的重大变更**：明确不同版本之间的 API 变化和行为差异
- **规划迁移路径**：根据项目实际情况选择合适的迁移策略
- **减少迁移风险**：通过详细的步骤说明和代码示例降低升级风险
- **快速定位问题**：提供常见问题的解决方案和回滚策略

### 阅读建议

1. **确认当前版本**：查看项目中使用的 GFramework 版本号
2. **查看目标版本**：确定要升级到的目标版本
3. **阅读相关章节**：重点关注涉及的版本迁移章节
4. **测试验证**：在测试环境中完成迁移并充分测试
5. **逐步升级**：对于跨多个大版本的升级，建议分步进行

## 版本兼容性

### 版本号说明

GFramework 遵循 [语义化版本](https://semver.org/lang/zh-CN/) 规范：

```
主版本号.次版本号.修订号 (MAJOR.MINOR.PATCH)
```

- **主版本号（MAJOR）**：不兼容的 API 变更
- **次版本号（MINOR）**：向后兼容的功能新增
- **修订号（PATCH）**：向后兼容的问题修正

### 兼容性矩阵

| 源版本   | 目标版本  | 兼容性     | 迁移难度 | 说明            |
|-------|-------|---------|------|---------------|
| 0.0.x | 0.0.y | ✅ 完全兼容  | 低    | 修订版本，直接升级     |
| 0.0.x | 1.0.0 | ⚠️ 部分兼容 | 中    | 需要代码调整        |
| 0.x.x | 1.x.x | ❌ 不兼容   | 高    | 重大变更，需要重构     |
| 1.x.x | 1.y.y | ✅ 向后兼容  | 低    | 次版本升级，可能有废弃警告 |
| 1.x.x | 2.0.0 | ❌ 不兼容   | 高    | 重大变更，需要重构     |

### .NET 版本支持

| GFramework 版本 | .NET 8.0 | .NET 9.0 | .NET 10.0 |
|---------------|----------|----------|-----------|
| 0.0.x         | ✅        | ✅        | ✅         |
| 1.0.x         | ✅        | ✅        | ✅         |
| 2.0.x         | ❌        | ✅        | ✅         |

### Godot 版本支持

| GFramework 版本 | Godot 4.3 | Godot 4.4 | Godot 4.5 | Godot 4.6+ |
|---------------|-----------|-----------|-----------|------------|
| 0.0.x         | ✅         | ✅         | ✅         | ✅          |
| 1.0.x         | ❌         | ✅         | ✅         | ✅          |
| 2.0.x         | ❌         | ❌         | ✅         | ✅          |

## 从 0.x 迁移到 1.x

### 重大变更概述

1.0 版本是 GFramework 的第一个稳定版本，引入了多项重大变更以提升框架的稳定性、性能和可维护性。

#### 架构层面变更

- **架构初始化方式变更**：统一使用异步初始化
- **生命周期阶段调整**：简化阶段流程，移除冗余阶段
- **IOC 容器增强**：支持作用域和生命周期管理
- **模块系统重构**：引入新的模块注册机制

#### API 变更

- **命名空间调整**：部分类型移动到新的命名空间
- **接口签名变更**：部分接口方法签名调整
- **废弃 API 移除**：移除 0.x 中标记为废弃的 API
- **泛型约束调整**：部分泛型方法增加或调整约束

#### 行为变更

- **事件传播机制**：优化事件传播逻辑
- **协程调度策略**：改进协程调度算法
- **资源管理策略**：引入新的资源释放策略

### 迁移前准备

#### 1. 备份项目

```bash
# 创建项目备份
git checkout -b backup-before-migration
git push origin backup-before-migration

# 或使用文件系统备份
cp -r YourProject YourProject-backup
```

#### 2. 检查当前版本

```bash
# 查看当前使用的 GFramework 版本
dotnet list package | grep GFramework
```

#### 3. 更新依赖工具

```bash
# 更新 .NET SDK
dotnet --version

# 更新 NuGet 客户端
dotnet nuget --version
```

#### 4. 运行现有测试

```bash
# 确保所有测试通过
dotnet test

# 记录测试结果作为基准
dotnet test --logger "trx;LogFileName=baseline-tests.trx"
```

### 迁移步骤

#### 步骤 1：更新 NuGet 包

```bash
# 更新核心包
dotnet add package GeWuYou.GFramework.Core --version 1.0.0
dotnet add package GeWuYou.GFramework.Core.Abstractions --version 1.0.0

# 更新游戏扩展包
dotnet add package GeWuYou.GFramework.Game --version 1.0.0
dotnet add package GeWuYou.GFramework.Game.Abstractions --version 1.0.0

# 更新 Godot 集成包（如果使用）
dotnet add package GeWuYou.GFramework.Godot --version 1.0.0

# 更新源码生成器
dotnet add package GeWuYou.GFramework.SourceGenerators --version 1.0.0
```

#### 步骤 2：更新命名空间引用

**0.x 版本：**

```csharp
using GFramework.Core;
using GFramework.Core.Architecture;
using GFramework.Core.Events;
```

**1.x 版本：**

```csharp
using GFramework.Core.Abstractions.Architecture;
using GFramework.Core.Abstractions.Events;
using GFramework.Core.Architecture;
using GFramework.Core.Events;
```

#### 步骤 3：更新架构初始化代码

**0.x 版本：**

```csharp
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        RegisterModel(new PlayerModel());
        RegisterSystem(new GameplaySystem());
    }
}

// 同步初始化
var architecture = new GameArchitecture();
architecture.Initialize();
```

**1.x 版本：**

```csharp
public class GameArchitecture : Architecture
{
    protected override void Init()
    {
        RegisterModel(new PlayerModel());
        RegisterSystem(new GameplaySystem());
    }
}

// 推荐使用异步初始化
var architecture = new GameArchitecture();
await architecture.InitializeAsync();

// 或者使用同步初始化（不推荐）
// architecture.Initialize();
```

#### 步骤 4：更新事件注册代码

**0.x 版本：**

```csharp
// 注册事件
this.RegisterEvent&lt;PlayerDiedEvent&gt;(OnPlayerDied);

// 发送事件
this.SendEvent(new PlayerDiedEvent());
```

**1.x 版本：**

```csharp
// 注册事件（API 保持兼容）
this.RegisterEvent&lt;PlayerDiedEvent&gt;(OnPlayerDied);

// 发送事件（API 保持兼容）
this.SendEvent(new PlayerDiedEvent());

// 新增：带优先级的事件注册
this.RegisterEvent&lt;PlayerDiedEvent&gt;(OnPlayerDied, priority: 100);
```

#### 步骤 5：更新命令和查询代码

**0.x 版本：**

```csharp
public class MovePlayerCommand : AbstractCommand
{
    public Vector2 Direction { get; set; }

    protected override void OnDo()
    {
        // 执行逻辑
    }
}

// 发送命令
this.SendCommand(new MovePlayerCommand { Direction = direction });
```

**1.x 版本：**

```csharp
// 命令 API 保持兼容
public class MovePlayerCommand : AbstractCommand
{
    public Vector2 Direction { get; set; }

    protected override void OnDo()
    {
        // 执行逻辑
    }
}

// 发送命令（API 保持兼容）
this.SendCommand(new MovePlayerCommand { Direction = direction });

// 新增：异步命令支持
public class LoadDataCommand : AbstractAsyncCommand
{
    protected override async Task OnDoAsync()
    {
        await Task.Delay(100);
    }
}
```

#### 步骤 6：更新 IOC 容器使用

**0.x 版本：**

```csharp
// 注册服务
RegisterUtility(new StorageUtility());

// 获取服务
var storage = this.GetUtility&lt;StorageUtility&gt;();
```

**1.x 版本：**

```csharp
// 注册服务（API 保持兼容）
RegisterUtility(new StorageUtility());

// 获取服务（API 保持兼容）
var storage = this.GetUtility&lt;StorageUtility&gt;();

// 新增：按优先级获取服务
var storages = this.GetUtilities&lt;IStorageUtility&gt;();
var primaryStorage = storages.FirstOrDefault();
```

#### 步骤 7：更新协程代码

**0.x 版本：**

```csharp
// 启动协程
var handle = CoroutineHelper.Start(MyCoroutine());

// 等待协程
yield return new WaitForCoroutine(handle);
```

**1.x 版本：**

```csharp
// 启动协程（API 保持兼容）
var handle = CoroutineHelper.Start(MyCoroutine());

// 等待协程（API 保持兼容）
yield return new WaitForCoroutine(handle);

// 新增：协程分组和优先级
var handle = CoroutineHelper.Start(
    MyCoroutine(),
    group: "gameplay",
    priority: CoroutinePriority.High
);
```

### API 变更详解

#### 废弃的 API

以下 API 在 1.0 版本中已被移除：

##### 1. 同步命令查询扩展（已废弃）

**0.x 版本：**

```csharp
// 这些方法在 1.0 中已移除
this.SendCommandSync(command);
this.SendQuerySync(query);
```

**1.x 版本：**

```csharp
// 使用标准方法
this.SendCommand(command);
this.SendQuery(query);

// 或使用异步方法
await this.SendCommandAsync(command);
await this.SendQueryAsync(query);
```

##### 2. 旧版事件 API（已废弃）

**0.x 版本：**

```csharp
// 旧版事件注册方式
EventBus.Register&lt;MyEvent&gt;(handler);
```

**1.x 版本：**

```csharp
// 使用新的事件注册方式
this.RegisterEvent&lt;MyEvent&gt;(handler);

// 或使用事件总线
this.GetEventBus().Register&lt;MyEvent&gt;(handler);
```

##### 3. 直接访问 IOC 容器（已限制）

**0.x 版本：**

```csharp
// 直接访问容器
var container = architecture.Container;
container.Register&lt;IService, ServiceImpl&gt;();
```

**1.x 版本：**

```csharp
// 使用架构提供的注册方法
architecture.RegisterUtility&lt;IService&gt;(new ServiceImpl());

// 或在 Init 方法中注册
protected override void Init()
{
    RegisterUtility&lt;IService&gt;(new ServiceImpl());
}
```

#### 新增的 API

##### 1. 优先级支持

```csharp
// 事件优先级
this.RegisterEvent&lt;MyEvent&gt;(handler, priority: 100);

// 服务优先级
RegisterUtility&lt;IService&gt;(service, priority: 10);

// 协程优先级
CoroutineHelper.Start(routine, priority: CoroutinePriority.High);
```

##### 2. 异步初始化增强

```csharp
// 异步初始化架构
await architecture.InitializeAsync();

// 等待架构就绪
await architecture.WaitUntilReadyAsync();

// 异步初始化组件
public class MyModel : AbstractModel, IAsyncInitializable
{
    public async Task InitializeAsync()
    {
        await LoadDataAsync();
    }
}
```

##### 3. 事件过滤和统计

```csharp
// 事件过滤
this.RegisterEvent&lt;MyEvent&gt;(handler, filter: e => e.IsValid);

// 事件统计
var stats = eventBus.GetStatistics();
Console.WriteLine($"Total events: {stats.TotalEventsSent}");
```

##### 4. 协程分组管理

```csharp
// 创建协程组
var handle = CoroutineHelper.Start(
    routine,
    group: "ui-animations"
);

// 暂停协程组
CoroutineHelper.PauseGroup("ui-animations");

// 恢复协程组
CoroutineHelper.ResumeGroup("ui-animations");

// 停止协程组
CoroutineHelper.StopGroup("ui-animations");
```

### 配置变更

#### 架构配置

**0.x 版本：**

```csharp
var architecture = new GameArchitecture();
```

**1.x 版本：**

```csharp
// 使用配置对象
var config = new ArchitectureConfiguration
{
    ArchitectureProperties = new ArchitectureProperties
    {
        StrictPhaseValidation = true,
        AllowLateRegistration = false
    },
    LoggerProperties = new LoggerProperties
    {
        MinimumLevel = LogLevel.Information
    }
};

var architecture = new GameArchitecture(configuration: config);
```

#### 日志配置

**0.x 版本：**

```csharp
// 使用默认日志
```

**1.x 版本：**

```csharp
// 配置日志系统
var logConfig = new LoggingConfiguration
{
    MinimumLevel = LogLevel.Debug,
    Appenders = new List&lt;ILogAppender&gt;
    {
        new ConsoleAppender(),
        new FileAppender("logs/game.log")
    },
    Filters = new List&lt;ILogFilter&gt;
    {
        new LogLevelFilter(LogLevel.Warning),
        new NamespaceFilter("GFramework.*")
    }
};
```

### 依赖变更

#### NuGet 包更新

| 包名                                       | 0.x 版本 | 1.x 版本 | 变更说明     |
|------------------------------------------|--------|--------|----------|
| Microsoft.Extensions.DependencyInjection | 8.0.0  | 10.0.3 | 升级到最新版本  |
| Arch                                     | 1.x    | 2.1.0  | ECS 框架升级 |
| Arch.System                              | 1.0.x  | 1.1.0  | 系统组件升级   |

#### 包拆分

1.0 版本对包结构进行了优化：

**0.x 版本：**

```xml
<PackageReference Include="GeWuYou.GFramework" Version="0.0.200" />
```

**1.x 版本：**

```xml
<!-- 推荐按需引用 -->
<PackageReference Include="GeWuYou.GFramework.Core" Version="1.0.0" />
<PackageReference Include="GeWuYou.GFramework.Core.Abstractions" Version="1.0.0" />
<PackageReference Include="GeWuYou.GFramework.Game" Version="1.0.0" />
<PackageReference Include="GeWuYou.GFramework.Godot" Version="1.0.0" />
```

### 代码迁移工具

#### 自动化迁移工具

GFramework 提供了迁移工具来自动化部分迁移工作：

```bash
# 安装迁移工具
dotnet tool install -g GFramework.MigrationTool

# 运行迁移分析
gframework-migrate analyze --project YourProject.csproj

# 执行自动迁移
gframework-migrate apply --project YourProject.csproj --target-version 1.0.0

# 生成迁移报告
gframework-migrate report --output migration-report.html
```

#### 手动迁移检查清单

使用以下清单确保完整迁移：

- [ ] 更新所有 NuGet 包到 1.0.0
- [ ] 更新命名空间引用
- [ ] 替换废弃的 API
- [ ] 更新架构初始化代码
- [ ] 更新配置代码
- [ ] 运行所有单元测试
- [ ] 运行集成测试
- [ ] 执行性能测试
- [ ] 更新文档和注释
- [ ] 代码审查

### 测试迁移

#### 单元测试更新

**0.x 版本：**

```csharp
[Test]
public void TestArchitectureInit()
{
    var architecture = new TestArchitecture();
    architecture.Initialize();

    Assert.That(architecture.CurrentPhase, Is.EqualTo(ArchitecturePhase.Ready));
}
```

**1.x 版本：**

```csharp
[Test]
public async Task TestArchitectureInit()
{
    var architecture = new TestArchitecture();
    await architecture.InitializeAsync();

    Assert.That(architecture.CurrentPhase, Is.EqualTo(ArchitecturePhase.Ready));
}
```

#### 集成测试更新

**0.x 版本：**

```csharp
[Test]
public void TestGameFlow()
{
    var game = new GameArchitecture();
    game.Initialize();

    game.SendCommand(new StartGameCommand());
    var score = game.SendQuery(new GetScoreQuery());

    Assert.That(score, Is.EqualTo(0));
}
```

**1.x 版本：**

```csharp
[Test]
public async Task TestGameFlow()
{
    var game = new GameArchitecture();
    await game.InitializeAsync();

    await game.SendCommandAsync(new StartGameCommand());
    var score = await game.SendQueryAsync(new GetScoreQuery());

    Assert.That(score, Is.EqualTo(0));
}
```

## 常见问题

### 编译错误

#### 问题 1：命名空间找不到

**错误信息：**

```
error CS0246: The type or namespace name 'IArchitecture' could not be found
```

**解决方案：**

```csharp
// 添加正确的命名空间引用
using GFramework.Core.Abstractions.Architecture;
```

#### 问题 2：方法签名不匹配

**错误信息：**

```
error CS1501: No overload for method 'RegisterEvent' takes 1 arguments
```

**解决方案：**

```csharp
// 0.x 版本
this.RegisterEvent&lt;MyEvent&gt;(handler);

// 1.x 版本（兼容）
this.RegisterEvent&lt;MyEvent&gt;(handler);

// 1.x 版本（带优先级）
this.RegisterEvent&lt;MyEvent&gt;(handler, priority: 100);
```

#### 问题 3：泛型约束不满足

**错误信息：**

```
error CS0311: The type 'MyType' cannot be used as type parameter 'T'
in the generic type or method. There is no implicit reference conversion
from 'MyType' to 'GFramework.Core.Abstractions.IModel'.
```

**解决方案：**

```csharp
// 确保类型实现了正确的接口
public class MyModel : AbstractModel, IModel
{
    // 实现
}
```

### 运行时错误

#### 问题 1：架构未初始化

**错误信息：**

```
InvalidOperationException: Architecture is not initialized
```

**解决方案：**

```csharp
// 确保在使用前初始化架构
var architecture = new GameArchitecture();
await architecture.InitializeAsync();
await architecture.WaitUntilReadyAsync();

// 然后再使用
this.SendCommand(new MyCommand());
```

#### 问题 2：服务未注册

**错误信息：**

```
InvalidOperationException: Service of type 'IMyService' is not registered
```

**解决方案：**

```csharp
// 在 Init 方法中注册服务
protected override void Init()
{
    RegisterUtility&lt;IMyService&gt;(new MyServiceImpl());
}
```

#### 问题 3：事件处理器未触发

**问题描述：**
事件发送后，注册的处理器没有被调用。

**解决方案：**

```csharp
// 确保事件处理器正确注册
var unregister = this.RegisterEvent&lt;MyEvent&gt;(OnMyEvent);

// 确保在对象销毁时注销
protected override void OnDestroy()
{
    unregister?.UnRegister();
}

// 检查事件类型是否匹配
this.SendEvent(new MyEvent()); // 确保类型完全一致
```

### 性能问题

#### 问题 1：初始化时间过长

**问题描述：**
架构初始化耗时明显增加。

**解决方案：**

```csharp
// 使用异步初始化
await architecture.InitializeAsync();

// 对于耗时的初始化操作，使用异步方法
public class MyModel : AbstractModel, IAsyncInitializable
{
    public async Task InitializeAsync()
    {
        // 异步加载数据
        await LoadDataAsync();
    }
}
```

#### 问题 2：事件处理性能下降

**问题描述：**
事件处理速度变慢。

**解决方案：**

```csharp
// 使用事件过滤减少不必要的处理
this.RegisterEvent&lt;MyEvent&gt;(
    handler,
    filter: e => e.ShouldProcess
);

// 使用优先级控制处理顺序
this.RegisterEvent&lt;MyEvent&gt;(
    criticalHandler,
    priority: 100
);
```

### 兼容性问题

#### 问题 1：Godot 版本不兼容

**问题描述：**
升级后在 Godot 4.3 中无法运行。

**解决方案：**

```bash
# GFramework 1.0 要求 Godot 4.4+
# 升级 Godot 到 4.4 或更高版本
```

#### 问题 2：.NET 版本不兼容

**问题描述：**
项目使用 .NET 7.0，无法使用 GFramework 1.0。

**解决方案：**

```xml
<!-- 升级项目到 .NET 8.0 或更高版本 -->
<PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
</PropertyGroup>
```

## 回滚方案

如果迁移过程中遇到无法解决的问题，可以按以下步骤回滚：

### 步骤 1：恢复包版本

```bash
# 回滚到 0.x 版本
dotnet add package GeWuYou.GFramework.Core --version 0.0.200
dotnet add package GeWuYou.GFramework.Core.Abstractions --version 0.0.200
dotnet add package GeWuYou.GFramework.Game --version 0.0.200
dotnet add package GeWuYou.GFramework.Godot --version 0.0.200
```

### 步骤 2：恢复代码

```bash
# 从备份分支恢复
git checkout backup-before-migration

# 或从文件系统备份恢复
rm -rf YourProject
cp -r YourProject-backup YourProject
```

### 步骤 3：验证回滚

```bash
# 清理构建缓存
dotnet clean
rm -rf bin obj

# 重新构建
dotnet build

# 运行测试
dotnet test
```

### 步骤 4：记录问题

创建问题报告，包含：

- 遇到的具体错误
- 错误发生的环境信息
- 复现步骤
- 相关代码片段

提交到 [GitHub Issues](https://github.com/GeWuYou/GFramework/issues)。

## 获取帮助

### 官方资源

- **文档中心**：[https://gewuyou.github.io/GFramework/](https://gewuyou.github.io/GFramework/)
- **GitHub 仓库**：[https://github.com/GeWuYou/GFramework](https://github.com/GeWuYou/GFramework)
- **问题追踪**：[https://github.com/GeWuYou/GFramework/issues](https://github.com/GeWuYou/GFramework/issues)
- **讨论区**：[https://github.com/GeWuYou/GFramework/discussions](https://github.com/GeWuYou/GFramework/discussions)

### 社区支持

- 在 GitHub Discussions 中提问
- 查看已有的 Issues 和 Pull Requests
- 参考示例项目和教程

### 商业支持

如需专业的迁移支持服务，请联系项目维护团队。

## 附录

### A. 完整的 API 对照表

| 0.x API                     | 1.x API                                | 说明         |
|-----------------------------|----------------------------------------|------------|
| `architecture.Initialize()` | `await architecture.InitializeAsync()` | 推荐使用异步初始化  |
| `this.SendCommandSync()`    | `this.SendCommand()`                   | 移除 Sync 后缀 |
| `this.SendQuerySync()`      | `this.SendQuery()`                     | 移除 Sync 后缀 |
| `EventBus.Register()`       | `this.RegisterEvent()`                 | 使用扩展方法     |
| `Container.Register()`      | `RegisterUtility()`                    | 使用架构方法     |

### B. 迁移时间估算

| 项目规模          | 预估时间  | 说明           |
|---------------|-------|--------------|
| 小型（&lt;10k 行） | 1-2 天 | 主要是包更新和测试    |
| 中型（10k-50k 行） | 3-5 天 | 需要代码审查和重构    |
| 大型（&gt;50k 行） | 1-2 周 | 需要分模块迁移和充分测试 |

### C. 相关资源

- [架构设计文档](./core/architecture.md)
- [事件系统文档](./core/events.md)
- [命令查询文档](./core/command.md)
- [协程系统文档](./core/coroutine.md)
- [最佳实践](./best-practices/architecture-patterns.md)

---

**文档版本**：1.0.0
**最后更新**：2026-03-07
**许可证**：Apache 2.0
