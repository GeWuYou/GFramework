---
title: 源码生成器
---

# 源码生成器

生成器用于消费项目编译期产生确定性代码。按需要安装对应包，框架 runtime 和 abstractions 保持不依赖生成器。

## Core 生成器

- [枚举扩展](./enum-generator.md)：生成枚举判断和转换辅助。
- [Priority](./priority-generator.md)：为普通 partial 类型实现 IPrioritized。

## 其他生成器

- [配置 Schema](./schema-config-generator.md)
- [CQRS 注册表](./cqrs-handler-registry-generator.md)
- [Godot 项目元数据](./godot-project-generator.md)
- [节点获取](./get-node-generator.md)
- [节点信号绑定](./bind-node-signal-generator.md)
- [场景注册](./auto-scene-generator.md)
- [导出集合注册](./auto-register-exported-collections-generator.md)

依赖关系在组合根显式装配，普通业务对象使用构造注入。日志使用标准 ILogger&lt;T&gt;。可运行采用方式见 [最小应用](../getting-started/quick-start.md) 与 [Godot 示例](../tutorials/godot-integration.md)。
