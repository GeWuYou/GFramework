# GFramework.Core.Abstractions

GFramework 框架的抽象层定义模块，包含所有核心组件的接口定义。

## 主要内容

- 架构核心接口 (IArchitecture, IArchitectureContext等)
- 数据模型接口 (IModel)
- 业务系统接口 (ISystem)
- 控制器接口 (IController)
- 命令与查询接口 (ICommand, IQuery)
- 事件系统接口 (IEvent, IEventBus)
- 依赖注入容器接口 (IIocContainer)
- 可绑定属性接口 (IBindableProperty)
- 日志系统接口 (ILogger)

## 设计原则

- 接口隔离，每个接口职责单一
- 依赖倒置，上层依赖抽象接口
- 类型安全，充分利用泛型系统
- 广泛兼容，基于 netstandard2.0

## 详细文档

参见 [docs/zh-CN/abstractions/](../docs/zh-CN/abstractions/) 目录下的详细文档。
