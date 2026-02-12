# 常见问题（FAQ）

## 安装与配置

### Q: 如何安装 GFramework？

A: 使用 NuGet 包管理器：

```bash
dotnet add package GeWuYou.GFramework.Core
```

详见 [安装配置](./getting-started/installation.md)。

### Q: 支持哪些 .NET 版本？

A: 支持 .NET 6.0 及更高版本（包括 .NET 7.0、8.0、9.0、10.0）。

### Q: 可以在 Unity 中使用吗？

A: Core 模块可以在 Unity 中使用。Godot 集成模块仅支持 Godot。

## 架构设计

### Q: 什么时候应该使用 Command？

A: 当需要封装用户操作、支持撤销/重做或记录操作历史时。

### Q: 什么时候应该使用 Query？

A: 当需要查询数据、检查条件或明确只读意图时。

### Q: 什么时候应该使用 Event？

A: 当需要通知其他组件、实现松耦合通信或支持多个监听者时。

### Q: Model 中可以包含业务逻辑吗？

A: 不建议。Model 应该只存储数据。业务逻辑应该在 System 中实现。

## 性能优化

### Q: 频繁调用 GetModel 会有性能问题吗？

A: 不会，但建议缓存引用以提高效率。

### Q: 事件系统的性能如何？

A: 事件系统使用类型安全的泛型，性能优异。

### Q: BindableProperty 有性能开销吗？

A: 开销极小。仅在值变化时触发回调。

## 内存管理

### Q: 如何避免内存泄漏？

A: 使用 `UnRegisterList` 统一管理注销。

### Q: 销毁 Architecture 时会自动清理吗？

A: 是的。调用 `architecture.Destroy()` 会自动销毁所有组件。

## Godot 集成

### Q: 如何在 Godot 中使用 GFramework？

A: 安装 Godot 集成包：

```bash
dotnet add package GeWuYou.GFramework.Godot
```

### Q: 支持哪个版本的 Godot？

A: Godot 4.6 及更高版本。

## 测试

### Q: 如何测试使用 GFramework 的代码？

A: 通过依赖注入模拟架构进行单元测试。

## 最佳实践

### Q: 如何组织大型项目？

A: 按功能模块组织（Models、Systems、Commands、Events 等）。

### Q: 如何处理异步操作？

A: 使用 async/await 结合事件系统。

### Q: 如何处理错误？

A: 使用事件传递错误信息。

## 许可证

### Q: GFramework 采用什么许可证？

A: Apache License 2.0。可用于商业项目。

---

**没有找到答案？** 在 [GitHub](https://github.com/GeWuYou/GFramework) 提交 Issue。
