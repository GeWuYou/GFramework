---
title: ContextAware 生成器
description: 说明 ContextAware 的显式实例上下文绑定、同步边界、采用方法与测试方式。
---

# ContextAware 生成器

`[ContextAware]` 为项目类型生成 `IContextAware` 的实现。对象显式绑定所属架构后，可以使用 `this.GetModel<T>()`、`this.GetSystem<T>()`、`this.GetUtility<T>()` 等扩展方法。

生成器只提供上下文访问，不注册服务，也不自动寻找活动架构。

## 当前包关系

- 特性来源：`GFramework.Core.SourceGenerators.Abstractions`
- 生成器实现：`GFramework.Core.SourceGenerators`
- 运行时接口：`GFramework.Core.Abstractions.Rule.IContextAware`
- 常用扩展方法：`GFramework.Core.Extensions`

消费项目需要安装 `GeWuYou.GFramework.Core.SourceGenerators`；仅安装 Core 运行时不会执行生成器。

## 最小用法

下面的业务接口由消费项目定义，并由所属架构注册：

```csharp
using GFramework.Core.Abstractions.Controller;
using GFramework.Core.Extensions;
using GFramework.Core.SourceGenerators.Abstractions.Rule;

[ContextAware]
public partial class PlayerController : IController
{
    public void Initialize()
    {
        var playerModel = this.GetModel<IPlayerModel>();
        var combatSystem = this.GetSystem<ICombatSystem>();

        combatSystem.Bind(playerModel);
    }
}
```

类型需要是非泛型 `partial class`。在架构已经初始化、所需组件可用之后，由组合根绑定这个控制器，再调用它自己的初始化入口：

```csharp
using GFramework.Core.Abstractions.Rule;

var controller = new PlayerController();
((IContextAware)controller).SetContext(architecture.Context);
controller.Initialize();
```

控制器不会因为实现 `IController` 就自动获得上下文。框架登记的组件由架构注入上下文，手动创建的对象由创建者显式装配。

## 当前会生成什么

- `IContextAware.SetContext(...)` 与 `GetContext()` 的显式接口实现。
- 受保护的 `Context` 属性。
- 当前实例的上下文引用与同步锁；成员名自动避开当前类型及继承成员中的字段冲突。

绑定和读取遵循以下规则：

1. `SetContext(context)` 拒绝 null，并设置当前实例的绑定。
2. `GetContext()` 与受保护的 `Context` 返回该绑定。
3. 尚未绑定时抛出 `InvalidOperationException`，提示先调用 `IContextAware.SetContext`。
4. 再次显式绑定会替换当前实例的上下文，不影响同类型的其他对象。

生成代码没有共享上下文提供者，不回退到 `GameContext`。实例锁只保护上下文引用的绑定和读取，不保证上下文内部服务、业务状态或 Godot 对象可以跨线程访问。

## 实例隔离

同一类型的两个对象可以分别属于不同架构：

```csharp
using GFramework.Core.Abstractions.Rule;

var first = new PlayerController();
var second = new PlayerController();

((IContextAware)first).SetContext(firstArchitecture.Context);
((IContextAware)second).SetContext(secondArchitecture.Context);
```

绑定第一个对象不会让第二个对象变得可用。重新绑定已有对象时，消费项目还需重新处理它之前缓存的业务字段；上下文引用变化不会自动刷新 Context Get 已注入的字段。架构销毁后应停止使用依赖它的对象，绑定本身不会延长服务的有效生命周期。

## 何时使用 `[ContextAware]`

适合已经继承其他基类的 Controller、Godot Node/Control 或项目侧包装器，需要显式访问架构服务的场景。由组合根选择上下文，可以让同一类型的实例在不同架构或测试中独立工作。

对于没有上下文访问需求的普通服务，直接通过构造函数声明实际依赖即可。

## 何时改用 `ContextAwareBase`

`ContextAwareBase` 也要求显式实例绑定、拒绝未绑定访问，并且不会读取全局上下文。它使用简单实例字段，不额外同步绑定和读取；调用方应遵循对象自身的线程模型。

已经继承 `AbstractSystem`、`AbstractModel` 等框架基类的类型通常已获得这项能力，无需重复添加 `[ContextAware]`。

## 与 Context Get 注入的关系

Context Get 系列特性根据字段声明生成 `__InjectContextBindings_Generated()`。目标类型可以：

- 标记 `[ContextAware]`；
- 手动实现 `IContextAware`；
- 继承 `ContextAwareBase`。

先为对象绑定上下文，再由类内公开的初始化入口调用该私有生成方法，最后读取注入字段。绑定上下文本身不会自动执行字段注入。完整模式见 [Context Get 注入生成器](./context-get-generator.md)。

## 测试场景

测试为每个对象绑定各自的上下文，无需修改或清理任何类型级状态。下面的片段假设测试夹具已有初始化完成的 `testArchitecture`：

```csharp
using GFramework.Core.Abstractions.Rule;

IContextAware first = new PlayerController();
IContextAware second = new PlayerController();

first.SetContext(testArchitecture.Context);

Assert.That(first.GetContext(), Is.SameAs(testArchitecture.Context));
Assert.That(() => second.GetContext(), Throws.InvalidOperationException);
```

业务初始化测试仍应注册实际需要的 Model/System/Utility 或替身，并通过控制器公开入口触发使用流程。

## 诊断与约束

- 类型必须是 `partial class`；缺少 partial 会报告公共 partial 约束诊断，非 class 会报告 `GF_Rule_001`。
- 当前生成声明不保留目标类的类型参数，泛型目标类不适用本生成路径。
- 字段注入的嵌套类型、只读字段等约束由 Context Get 生成器报告。
- 未绑定访问和 null 绑定是运行时错误，不由生成器推断上下文来源。

## 迁移说明

旧代码若通过类型级提供者或全局活动上下文获得绑定，应改为对每个对象调用 `IContextAware.SetContext(architecture.Context)`。测试不再配置共享 provider；创建第二个实例后也要单独绑定。

若对象已缓存注入字段，切换上下文后应在合法的业务生命周期重新执行其装配入口，或创建新对象。不要仅替换上下文引用就继续使用旧架构提供的资源。

## 推荐阅读

1. [Context Get 注入生成器](./context-get-generator.md)
2. [日志生成器](./logging-generator.md)
3. [Core 模块总览](../core/index.md)
4. [源码生成器总览](./index.md)
