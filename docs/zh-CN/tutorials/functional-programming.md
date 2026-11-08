---
title: 函数式组合实践
description: 选择 Option、Result 与明确异常边界组织可读的数据流。
---

# 函数式组合实践

先区分缺席、可预期拒绝、取消和程序故障，再选择表示形式。函数式工具是普通 C# 服务中的可选辅助。

## 选择表示形式

- 值可能缺席：Option 或可空值。
- 多种有业务意义的拒绝：领域结果类型或枚举。
- 操作边界需要携带异常失败：Result。
- 异步取消：CancellationToken 和取消异常。
- 程序故障：传播异常，在合适的宿主边界记录并反馈。

## 组合成功路径

```csharp
using System;
using GFramework.Core.Functional;

static Result<int> ValidateQuantity(int quantity) => quantity > 0
    ? Result<int>.Success(quantity)
    : Result<int>.Failure(new ArgumentOutOfRangeException(nameof(quantity)));

Result<int> total = ValidateQuantity(3)
    .Map(quantity => quantity * 10)
    .Ensure(value => value <= 100, "Quantity exceeds limit");
string message = total.Match(value => $"Total: {value}", error => error.Message);
```

Map 转换成功值，Bind 连接返回 Result 的下一步，Ensure 为成功值增加条件。既有失败跳过成功委托。默认 Result 也是失败，Combine 不会将它当成可忽略的空元素。

## 显式异常边界

```csharp
using System.IO;
using GFramework.Core.Functional;

Result<string> document = await Result<string>.TryAsync(() => File.ReadAllTextAsync("profile.json"));
```

Try/TryAsync 显式捕获普通异常；MapAsync/BindAsync 与同步版本一样传播委托异常。取消异常不转换为普通 Failure。

## 验证与可读性

测试成功、普通失败、默认失败、可空成功值和取消，检查是否保留原异常及失败是否跳过后续成功委托。方法需要大量嵌套 Bind 或无意义的一行委托时，提取有名字的业务步骤。普通可读方法同样是推荐写法。

完整契约见[函数式工具](../core/functional.md)。
