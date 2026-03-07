# GFramework 文档编写规范

## Markdown 语法规范

### 1. 泛型标记转义

在 Markdown 文档中,所有泛型标记必须转义,否则会被 VitePress 误认为 HTML 标签。

**错误示例**:
```markdown
`Option<T>` 是一个泛型类型
`Result<TValue, TError>` 表示结果
public class Repository<TData> { }
```

**正确示例**:
```markdown
`Option&lt;T&gt;` 是一个泛型类型
`Result&lt;TValue, TError&gt;` 表示结果
public class Repository&lt;TData&gt; { }
```

**常见泛型标记**:
- `<T>` → `&lt;T&gt;`
- `<TResult>` → `&lt;TResult&gt;`
- `<TValue>` → `&lt;TValue&gt;`
- `<TError>` → `&lt;TError&gt;`
- `<TSaveData>` → `&lt;TSaveData&gt;`
- `<TData>` → `&lt;TData&gt;`
- `<TNode>` → `&lt;TNode&gt;`

### 2. HTML 标签转义

如果需要在文档中显示 HTML 标签,必须转义:
- `<summary>` → `&lt;summary&gt;`
- `<param>` → `&lt;param&gt;`
- `<returns>` → `&lt;returns&gt;`

### 3. 链接验证

**内部链接规则**:
- 使用相对路径: `/zh-CN/core/events`
- 确保目标文件存在
- 不要链接到尚未创建的页面

**已存在的文档路径**:

**Core 模块**:
- `/zh-CN/core/architecture` - 架构系统
- `/zh-CN/core/ioc` - IoC 容器
- `/zh-CN/core/events` - 事件系统
- `/zh-CN/core/command` - 命令系统
- `/zh-CN/core/query` - 查询系统
- `/zh-CN/core/model` - Model 系统
- `/zh-CN/core/system` - System 系统
- `/zh-CN/core/utility` - Utility 系统
- `/zh-CN/core/controller` - Controller 系统
- `/zh-CN/core/logging` - 日志系统
- `/zh-CN/core/pool` - 对象池
- `/zh-CN/core/property` - 可绑定属性
- `/zh-CN/core/lifecycle` - 生命周期管理
- `/zh-CN/core/coroutine` - 协程系统
- `/zh-CN/core/resource` - 资源管理
- `/zh-CN/core/state-machine` - 状态机
- `/zh-CN/core/cqrs` - CQRS 与 Mediator
- `/zh-CN/core/functional` - 函数式编程
- `/zh-CN/core/pause` - 暂停管理
- `/zh-CN/core/configuration` - 配置管理
- `/zh-CN/core/ecs` - ECS 系统集成
- `/zh-CN/core/extensions` - 扩展方法
- `/zh-CN/core/rule` - 规则系统
- `/zh-CN/core/environment` - 环境系统
- `/zh-CN/core/context` - 上下文系统
- `/zh-CN/core/async-initialization` - 异步初始化

**Game 模块**:
- `/zh-CN/game/scene` - 场景系统
- `/zh-CN/game/ui` - UI 系统
- `/zh-CN/game/data` - 数据与存档
- `/zh-CN/game/storage` - 存储系统
- `/zh-CN/game/serialization` - 序列化系统
- `/zh-CN/game/setting` - 设置系统

**Godot 模块**:
- `/zh-CN/godot/architecture` - Godot 架构集成
- `/zh-CN/godot/scene` - Godot 场景系统
- `/zh-CN/godot/ui` - Godot UI 系统
- `/zh-CN/godot/pool` - Godot 节点池
- `/zh-CN/godot/resource` - Godot 资源仓储
- `/zh-CN/godot/logging` - Godot 日志系统
- `/zh-CN/godot/pause` - Godot 暂停处理
- `/zh-CN/godot/extensions` - Godot 扩展
- `/zh-CN/godot/coroutine` - Godot 协程
- `/zh-CN/godot/signal` - Godot 信号
- `/zh-CN/godot/storage` - Godot 存储

**教程**:
- `/zh-CN/tutorials/coroutine-tutorial` - 协程系统教程
- `/zh-CN/tutorials/state-machine-tutorial` - 状态机教程
- `/zh-CN/tutorials/resource-management` - 资源管理教程
- `/zh-CN/tutorials/save-system` - 存档系统教程
- `/zh-CN/tutorials/godot-complete-project` - Godot 完整项目
- `/zh-CN/tutorials/functional-programming` - 函数式编程实践
- `/zh-CN/tutorials/pause-system` - 暂停系统实现
- `/zh-CN/tutorials/data-migration` - 数据迁移实践
- `/zh-CN/tutorials/godot-integration` - Godot 集成
- `/zh-CN/tutorials/advanced-patterns` - 高级模式

**其他**:
- `/zh-CN/getting-started/quick-start` - 快速开始
- `/zh-CN/getting-started/installation` - 安装指南
- `/zh-CN/best-practices/architecture-patterns` - 架构模式

**不存在的路径** (不要链接):
- `/zh-CN/best-practices/performance` - 尚未创建
- `/zh-CN/core/serializer` - 错误路径,应使用 `/zh-CN/game/serialization`

## 代码块规范

### 1. 代码块语言标识

始终指定代码块的语言:

```markdown
\`\`\`csharp
public class Example { }
\`\`\`

\`\`\`bash
npm install
\`\`\`
```

### 2. 代码注释

代码示例应包含中文注释:

```csharp
// 创建玩家实体
var player = new Player
{
    Name = "玩家1",  // 玩家名称
    Level = 1        // 初始等级
};
```

## Frontmatter 规范

每个文档必须包含正确的 frontmatter:

```yaml
---
title: 文档标题
description: 简短描述（1-2 句话）
---
```

## 文档结构规范

### 指南文档结构

1. 概述
2. 核心概念
3. 基本用法
4. 高级用法
5. 最佳实践
6. 常见问题
7. 相关文档

### 教程文档结构

1. 学习目标
2. 前置条件
3. 步骤 1-N (3-7 步)
4. 完整代码
5. 运行结果
6. 下一步
7. 相关文档

## 验证清单

生成文档后,必须检查:

- [ ] 所有泛型标记已转义 (`<T>` → `&lt;T&gt;`)
- [ ] 所有内部链接指向存在的页面
- [ ] Frontmatter 格式正确
- [ ] 代码块指定了语言
- [ ] 代码包含中文注释
- [ ] 文档结构完整
- [ ] 没有 HTML 标签错误

## 自动修复脚本

如果文档已生成,可以使用以下脚本修复常见问题:

```bash
# 修复泛型标记
sed -i 's/<T>/\&lt;T\&gt;/g' file.md
sed -i 's/<TResult>/\&lt;TResult\&gt;/g' file.md
sed -i 's/<TValue>/\&lt;TValue\&gt;/g' file.md
sed -i 's/<TError>/\&lt;TError\&gt;/g' file.md

# 验证构建
cd docs && bun run build
```
