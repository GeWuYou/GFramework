---
title: 通用扩展方法
description: 按实际能力选择集合、数值、异步与状态扩展。
---

# 通用扩展方法

GFramework.Core.Extensions 包含独立的集合、数值、异步和状态辅助方法。通过构造函数取得所需服务，再使用相应能力；扩展方法不提供通用上下文或服务定位入口。

## 主要类别

| 类别 | 用途 | 注意事项 |
| --- | --- | --- |
| Array、Collection、Span | 集合与连续内存操作 | 核对修改、枚举和分配语义 |
| ArrayPool | 借用并归还临时数组 | 归还后不继续使用引用，敏感内容按需清理 |
| NumericDisplay | 统一显示工程数量与紧凑格式 | 显示结果不改变内部计算精度 |
| Async | 超时与回退辅助 | 超时依赖操作实际响应取消，回退可能隐藏错误 |
| Store | 选择状态片段与只读绑定 | 选择器应无副作用，订阅令牌有明确拥有者 |
| StoreEventBus | 把状态/分发记录桥接到事件 | 同步本地通知，不是后台消息系统 |

## 数值显示

```csharp
using GFramework.Core.Extensions;

string compact = 12345.ToCompactString();
string standard = 0.00123.ToDisplayString();
```

显示风格由对应数值格式选项控制，详细能力见[数值与工具](./utility.md)。

## 超时

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Core.Extensions;

static Task<int> ReadWithTimeoutAsync(Func<CancellationToken, Task<int>> read, CancellationToken token)
{
    return AsyncExtensions.WithTimeoutAsync(read, TimeSpan.FromSeconds(2), token);
}
```

WithTimeoutAsync 把计时取消传给操作；若操作不遵守令牌，它不会强制终止底层工作。外部取消保留取消语义。WithFallbackAsync 是明确的异常恢复转换，使用前决定取消或其他错误是否允许转换为默认结果。

## 状态与函数组合

Store.Select、ToBindableProperty 和 BridgeToEventBus 的采用方式见[状态管理](./state-management.md)。需要 Option、Result、Pipe 或函数组合时，使用其各自命名空间，契约见[函数式工具](./functional.md)。

只引入当前代码需要的扩展命名空间。直接方法调用更清楚时，不必把普通业务步骤改写成长链。
