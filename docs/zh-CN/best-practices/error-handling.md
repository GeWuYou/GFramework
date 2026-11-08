---
title: 错误处理
description: 区分业务拒绝、取消、基础设施异常与恢复责任。
---

# 错误处理

每种失败需要明确的意义和处理边界。可预期业务拒绝、用户取消和程序错误分别处理。

## 选择结果类型

规则允许拒绝的操作使用领域结果或枚举，例如存档不存在、内容未就绪和用户放弃。可能没有值时使用可空值或 Option。操作边界需要携带异常失败时使用 Result，不需要把每个内部方法都包装成 Result。

## Result 契约

Result 和 `Result<T>` 都只有成功与失败；默认值是带明确未初始化原因的失败，组合不能忽略它。成功结果访问 Error 抛出 InvalidOperationException。

Map、Bind 及异步版本传播委托异常。显式 Try/TryAsync 才把普通异常转换为 Failure，保留原异常：

```csharp
using System.IO;
using GFramework.Core.Functional;

Result<string> read = await Result<string>.TryAsync(() => File.ReadAllTextAsync("profile.json"));
string status = read.Match(_ => "Loaded", error => error.Message);
```

Try/TryAsync 不捕获取消异常。失败的相等比较基于异常类型与消息，不代表堆栈、Data 或业务身份也相等。完整定义见[函数式工具](../core/functional.md)。

## 异步边界

异步方法接收 CancellationToken 并向下传递。取消保留取消语义，不显示成普通故障或无条件重试。退出时先停止新操作，取消并等待进行中的工作，再释放依赖。

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

static async Task ExecuteAsync(Func<CancellationToken, Task> action, ILogger logger, CancellationToken token)
{
    try { await action(token); }
    catch (OperationCanceledException) when (token.IsCancellationRequested)
    {
        // The owner requested cancellation and waits for this operation to finish.
    }
    catch (Exception error)
    {
        logger.LogError(error, "Operation failed");
        throw;
    }
}
```

只有能补充上下文、转换边界结果或真正恢复时才捕获。记录异常对象，而不只记录 Message；避免在每层重复记录同一故障。

## 恢复与清理

恢复需要明确动作，例如重载、选择备份、重试连接或创建新会话。未知结果不能报告成功，非幂等操作不能无条件重试。

初始化中途失败也要清理已开始组件和已创建资源。一项清理失败后继续尝试其他项，最终保留聚合错误。状态回调失败时，不假定已发生的副作用能够自动撤销。

界面提供可采取动作及其后果，详细异常留在日志。没有恢复证据时保持失败状态，不覆盖仍可恢复的数据。
