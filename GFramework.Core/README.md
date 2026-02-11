# GFramework.Core

GFramework 框架的核心模块，提供MVC架构的基础设施。

## 主要功能

- **Architecture** - 应用程序架构管理，支持依赖注入、生命周期管理和模块化扩展
- **Model** - 数据模型层，管理应用状态和数据
- **System** - 业务逻辑层，处理核心业务逻辑和事件响应
- **Controller** - 控制器层，处理用户输入和UI协调
- **Command** - 命令模式实现，封装用户操作
- **Query** - 查询模式实现，支持CQRS架构
- **Events** - 事件系统，实现组件间松耦合通信
- **IoC** - 轻量级依赖注入容器
- **Property** - 可绑定属性，支持数据绑定和响应式编程
- **Utility** - 无状态工具类
- **Pool** - 对象池系统，减少GC压力
- **Extensions** - 框架扩展方法
- **Logging** - 日志系统
- **Environment** - 环境配置管理

## 设计原则

- 与平台解耦，不依赖特定游戏引擎
- 接口隔离，职责单一
- 依赖倒置，面向接口编程
- 组合优于继承

## 详细文档

参见 [docs/zh-CN/core/](../docs/zh-CN/core/) 目录下的详细文档。
