# ContextAware 生成器

> 为 `partial class` 自动实现 `IContextAware`。

## 概述

`ContextAwareGenerator` 位于 `GFramework.SourceGenerators.Rule.ContextAwareGenerator`，用于把 `IContextAware`
的常见实现样板代码收敛到编译期。

当前生成器会为目标类型生成：

- 缓存字段 `_context`
- 静态上下文提供者 `_contextProvider`
- 受保护的 `Context` 属性
- `SetContextProvider(...)`
- `ResetContextProvider()`
- `IContextAware.SetContext(...)`
- `IContextAware.GetContext()`

## 基础使用

```csharp
using GFramework.SourceGenerators.Abstractions.Rule;

[ContextAware]
public partial class PlayerController
{
    public void Initialize()
    {
        var context = ((GFramework.Core.Abstractions.Rule.IContextAware)this).GetContext();
    }
}
```

生成代码的关键结构如下：

```csharp
partial class PlayerController : global::GFramework.Core.Abstractions.Rule.IContextAware
{
    private global::GFramework.Core.Abstractions.Architectures.IArchitectureContext? _context;
    private static global::GFramework.Core.Abstractions.Architectures.IArchitectureContextProvider? _contextProvider;

    protected global::GFramework.Core.Abstractions.Architectures.IArchitectureContext Context
    {
        get
        {
            if (_context == null)
            {
                _contextProvider ??= new global::GFramework.Core.Architectures.GameContextProvider();
                _context = _contextProvider.GetContext();
            }

            return _context;
        }
    }
}
```

## 约束

- 目标必须是 `partial class`
- 不能用于 `struct`、`interface` 或其他非类类型

## 与 Context Get 的关系

`[ContextAware]` 本身只负责提供上下文访问入口。

如果你还需要字段注入，请继续阅读 [Context Get 注入](/zh-CN/source-generators/context-get-generator)。
