# Architecture

`Architecture` 是 `GFramework.Core` 的运行时入口。它负责三件事：

- 组织初始化与销毁阶段
- 接入模型、系统、工具和模块
- 暴露 `ArchitectureContext` 作为统一上下文入口

当前版本的 `Architecture` 已经是协调器外观。对外仍保留稳定的注册与生命周期 API，但内部职责已经拆给专门协作者处理。

## 你真正会用到的公开入口

最常见的成员只有这些：

- `OnInitialize()`
  - 子类唯一必须实现的入口，用来注册模型、系统、工具、模块和额外的 CQRS 行为
- `RegisterModel(...)` / `RegisterSystem(...)` / `RegisterUtility(...)`
  - 注册运行时组件
- `InstallModule(...)`
  - 安装实现了 `IArchitectureModule` 的模块
- `RegisterLifecycleHook(...)`
  - 注册阶段钩子
- `RegisterCqrsPipelineBehavior<TBehavior>()`
  - 注册 CQRS pipeline 行为
- `RegisterCqrsHandlersFromAssembly(...)` / `RegisterCqrsHandlersFromAssemblies(...)`
  - 显式接入其他程序集中的 CQRS handlers
- `InitializeAsync()` / `WaitUntilReadyAsync()`
  - 启动架构并等待进入 `Ready`
- `DestroyAsync()`
  - 逆序销毁所有已接入组件

## 最小示例

```csharp
using GFramework.Core.Architectures;

public sealed class GameArchitecture : Architecture
{
    protected override void OnInitialize()
    {
        RegisterModel(new PlayerModel());
        RegisterSystem(new CombatSystem());
        RegisterUtility(new SaveUtility());
    }
}
```

启动方式：

```csharp
var architecture = new GameArchitecture();
await architecture.InitializeAsync();

await architecture.WaitUntilReadyAsync();
```

## 初始化时机

当前版本不再使用旧文档里的 `Init()` 入口。注册逻辑必须写在：

```csharp
protected override void OnInitialize()
{
}
```

框架会在 `InitializeAsync()` 内完成：

1. 基础设施准备
2. 创建并绑定 `ArchitectureContext`
3. 调用用户的 `OnInitialize()`
4. 按阶段初始化 `Utility -> Model -> System`
5. 进入 `Ready`

如果你还看到旧示例里写 `protected override void Init()`，那就是过时内容。

## 组件注册顺序

`Architecture` 仍然维持清晰的组件边界：

- `Model`
  - 承载状态
- `System`
  - 承载业务流程
- `Utility`
  - 承载无状态或基础设施型能力

初始化顺序固定为：

1. `Utility`
2. `Model`
3. `System`

销毁时会按逆序处理，并优先调用异步销毁接口。

## 模块与 CQRS 接入

如果你的功能以模块形式组织，优先通过 `InstallModule(...)` 接入，而不是把所有注册逻辑都堆进一个超大的 `OnInitialize()`。

如果 handlers 不只在当前架构程序集里，需要显式追加程序集：

```csharp
protected override void OnInitialize()
{
    RegisterCqrsPipelineBehavior<LoggingBehavior<,>>();

    RegisterCqrsHandlersFromAssemblies(
    [
        typeof(InventoryCqrsMarker).Assembly,
        typeof(BattleCqrsMarker).Assembly
    ]);
}
```

默认运行时会优先尝试消费端程序集上的生成注册表；缺失或不适用时回退到反射扫描。

## 阶段与钩子

`Architecture` 公开：

- `CurrentPhase`
- `IsReady`
- `PhaseChanged`
- `RegisterLifecycleHook(...)`

如果你需要在 `Ready`、`Destroying` 等阶段执行横切逻辑，比起把这类逻辑塞进某个具体 `System`，更适合单独实现
`IArchitectureLifecycleHook`。

```csharp
public sealed class MetricsHook : IArchitectureLifecycleHook
{
    public void OnPhase(ArchitecturePhase phase, IArchitecture architecture)
    {
        if (phase == ArchitecturePhase.Ready)
        {
            Console.WriteLine("Architecture ready.");
        }
    }
}
```

## 什么时候看别的页面

- 想看上下文 API：转到 [context](./context.md)
- 想看阶段和销毁语义：转到 [lifecycle](./lifecycle.md)
- 想看旧命令 / 查询兼容层：转到 [command](./command.md) 和 [query](./query.md)
- 想看推荐的新请求模型：转到 [cqrs](./cqrs.md)
