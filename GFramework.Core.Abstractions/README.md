# GFramework.Core.Abstractions

`GFramework.Core.Abstractions` 承载 `Core` 运行时对应的接口、枚举和值对象，用来定义跨模块协作边界。

## 什么时候单独依赖它

- 你在做插件、适配层或扩展包，只想依赖契约，不想把完整运行时拉进来
- 你需要为测试、编辑器工具或生成器提供替身实现
- 你在做多模块拆分，希望上层只面向接口编程

如果你只是直接使用框架功能，优先安装 `GFramework.Core`。

## 包关系

- 契约层：`GFramework.Core.Abstractions`
- 实现层：`GFramework.Core`
- 相关扩展：
  - `GFramework.Cqrs.Abstractions`
  - `GFramework.Game.Abstractions`

## 契约地图

| 目录 | 作用 |
| --- | --- |
| `Architectures/` | `IArchitecture`、模块、阶段监听与服务管理契约 |
| `Command/` / `Query/` | 旧版命令与查询执行器接口 |
| `Controller/` | `IController` |
| `Events/` | 事件契约、解绑接口与传播上下文 |
| `Model/` / `Systems/` / `Utility/` | 核心组件接口 |
| `State/` / `StateManagement/` | 状态机、Store、reducer、selector 契约 |
| `Property/` | `IBindableProperty` 与只读属性接口 |
| `Resource/` | 资源管理与释放策略契约 |
| `Localization/` | 本地化表、格式化与异常类型 |
| `Logging/` | logger、log entry、factory 相关契约 |
| `Ioc/` | `IIocContainer` |
| `Lifecycle/` | 初始化 / 销毁生命周期契约 |
| `Coroutine/` | 时间源、yield 指令与协程状态枚举 |
| `Pause/` | 暂停栈、token 与状态事件 |
| `Storage/` / `Serializer/` / `Versioning/` | 通用存储、序列化与版本化契约 |

## 采用建议

- 框架消费者通常同时安装 `GFramework.Core` 与 `GFramework.Core.Abstractions`
- 若你只需要对接口编程，可以仅引用本包，再在应用层自行提供实现
- 若你在写上层模块，优先把公共契约放在 `*.Abstractions`，实现放在对应 runtime 包

## 对应文档

- 抽象接口栏目：[`../docs/zh-CN/abstractions/index.md`](../docs/zh-CN/abstractions/index.md)
- Core 抽象页：[`../docs/zh-CN/abstractions/core-abstractions.md`](../docs/zh-CN/abstractions/core-abstractions.md)
- Core 运行时入口：[`../GFramework.Core/README.md`](../GFramework.Core/README.md)
