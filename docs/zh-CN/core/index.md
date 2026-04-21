# Core

`Core` 栏目对应 `GFramework` 的基础运行时层，主要覆盖 `GFramework.Core` 与 `GFramework.Core.Abstractions`，以及与之直接相邻的旧版
`Command` / `Query` 执行器和新版 `CQRS` 迁移入口。

如果你第一次接入框架，建议先把这里当作“运行时底座说明”，再按需进入 `Game`、`Godot` 或 Source Generators 栏目。

## 先理解包关系

- `GeWuYou.GFramework.Core`
  - 基础运行时实现，包含 `Architecture`、上下文、生命周期、事件、属性、状态、资源、日志、协程、IoC 等能力。
- `GeWuYou.GFramework.Core.Abstractions`
  - 对应的契约层，适合只依赖接口、做模块拆分或测试替身。
- `GeWuYou.GFramework.Cqrs`
  - 推荐给新功能使用的新请求模型运行时。
- `GeWuYou.GFramework.Game`
  - 在 `Core` 之上叠加游戏层配置、数据、设置、场景与 UI。
- `GeWuYou.GFramework.Core.SourceGenerators`
  - 在编译期补齐日志、上下文注入、模块自动注册等样板代码。

如果你只想先把架构跑起来，最小安装组合仍是：

```bash
dotnet add package GeWuYou.GFramework.Core
dotnet add package GeWuYou.GFramework.Core.Abstractions
```

## 这个栏目应该回答什么

`Core` 栏目不是旧版“完整框架教程”的镜像，而是当前实现的入口导航。这里的页面按能力域组织：

- 架构与上下文
  - [architecture](./architecture.md)
  - [context](./context.md)
  - [lifecycle](./lifecycle.md)
- 旧版命令 / 查询执行器与迁移入口
  - [command](./command.md)
  - [query](./query.md)
  - [cqrs](./cqrs.md)
- 核心横切能力
  - [events](./events.md)
  - [property](./property.md)
  - [logging](./logging.md)
  - [resource](./resource.md)
  - [coroutine](./coroutine.md)
  - [ioc](./ioc.md)
- 状态与扩展能力
  - [state-machine](./state-machine.md)
  - [state-management](./state-management.md)
  - [pause](./pause.md)
  - [localization](./localization.md)
  - [functional](./functional.md)
  - [extensions](./extensions.md)

## 最小接入路径

当前版本的最小运行时入口只有三个关键动作：

1. 继承 `Architecture`
2. 在 `OnInitialize()` 中注册模型、系统、工具或模块
3. 通过 `architecture.Context` 或 `ContextAwareBase` 的扩展方法访问上下文

最小示例：

```csharp
using GFramework.Core.Architectures;

public sealed class CounterArchitecture : Architecture
{
    protected override void OnInitialize()
    {
        RegisterModel(new CounterModel());
        RegisterSystem(new CounterSystem());
    }
}
```

对应的完整起步示例见：

- [快速开始](../getting-started/quick-start.md)

## 新项目如何选择能力

- 只需要基础架构、事件、日志、资源、协程：
  - 先停留在 `Core`
- 要写新的请求/通知处理流：
  - 优先阅读 [cqrs](./cqrs.md)
- 要接入游戏内容配置、设置、数据仓库、Scene 或 UI：
  - 转到 [Game](../game/index.md)
- 要接入 Godot 节点、场景和项目元数据生成：
  - 转到 [Godot](../godot/index.md) 与 [Source Generators](../source-generators/index.md) 栏目

## 推荐阅读顺序

1. [快速开始](../getting-started/quick-start.md)
2. [architecture](./architecture.md)
3. [context](./context.md)
4. [lifecycle](./lifecycle.md)
5. [cqrs](./cqrs.md)

之后再按实际需要进入具体专题页，而不是把 `Core` 当成一次性读完的大杂烩。

## 对应模块入口

- `GFramework.Core/README.md`
- `GFramework.Core.Abstractions/README.md`
- 仓库根 `README.md`
