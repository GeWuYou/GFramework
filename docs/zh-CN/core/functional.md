# 函数式编程指南

## 概述

GFramework.Core 提供了一套完整的函数式编程工具，帮助开发者编写更安全、更简洁、更易维护的代码。函数式编程强调不可变性、纯函数和声明式编程风格，能够有效减少副作用，提高代码的可测试性和可组合性。

本模块提供以下核心功能：

- **Option 类型**：安全处理可能不存在的值，替代 null 引用
- **Result 类型**：优雅处理操作结果和错误，避免异常传播
- **管道操作**：构建流式的函数调用链
- **函数组合**：组合多个函数形成新函数
- **控制流扩展**：函数式风格的条件执行和重试机制
- **异步函数式编程**：支持异步操作的函数式封装

## 核心概念

### Option 类型

`Option<T>` 表示可能存在或不存在的值，用于替代 null 引用。它有两种状态：

- **Some**：包含一个值
- **None**：不包含值

使用 Option 可以在编译时强制处理"无值"的情况，避免空引用异常。

### Result 类型

`Result<T>` 表示操作的结果，可能是成功值或失败异常。它有三种状态：

- **Success**：操作成功，包含返回值
- **Faulted**：操作失败，包含异常信息
- **Bottom**：未初始化状态

Result 类型将错误处理显式化，避免使用异常进行流程控制。

### 管道操作

管道操作允许将值通过一系列函数进行转换，形成流式的调用链。这种风格使代码更易读，逻辑更清晰。

### 函数组合

函数组合是将多个简单函数组合成复杂函数的技术。通过组合，可以构建可复用的函数库，提高代码的模块化程度。

## 基本用法

### Option 基础

#### 创建 Option

```csharp
using GFramework.Core.Functional;

// 创建包含值的 Option
var someValue = Option<int>.Some(42);

// 创建空 Option
var noneValue = Option<int>.None;

// 隐式转换
Option<string> name = "Alice";  // Some("Alice")
Option<string> empty = null;    // None
```

#### 获取值

```csharp
// 使用默认值
var value1 = someValue.GetOrElse(0);  // 42
var value2 = noneValue.GetOrElse(0);  // 0

// 使用工厂函数（延迟计算）
var value3 = noneValue.GetOrElse(() => ExpensiveDefault());
```

#### 转换值

```csharp
// Map：映射值到新类型
var option = Option<int>.Some(42);
var mapped = option.Map(x => x.ToString());  // Option<string>.Some("42")

// Bind：链式转换（单子绑定）
var result = Option<string>.Some("42")
    .Bind(s => int.TryParse(s, out var i)
        ? Option<int>.Some(i)
        : Option<int>.None);
```

#### 过滤值

```csharp
var option = Option<int>.Some(42);
var filtered = option.Filter(x => x > 0);   // Some(42)
var filtered2 = option.Filter(x => x < 0);  // None
```

#### 模式匹配

```csharp
// 返回值的模式匹配
var message = option.Match(
    some: value => $"Value: {value}",
    none: () => "No value"
);

// 副作用的模式匹配
option.Match(
    some: value => Console.WriteLine($"Value: {value}"),
    none: () => Console.WriteLine("No value")
);
```

### Result 基础

#### 创建 Result

```csharp
using GFramework.Core.Functional;

// 创建成功结果
var success = Result<int>.Succeed(42);
var success2 = Result<int>.Success(42);  // 别名

// 创建失败结果
var failure = Result<int>.Fail(new Exception("Error"));
var failure2 = Result<int>.Failure("Error message");

// 隐式转换
Result<int> result = 42;  // Success(42)
```

#### 安全执行

```csharp
// 自动捕获异常
var result = Result<int>.Try(() => int.Parse("42"));

// 异步安全执行
var asyncResult = await ResultExtensions.TryAsync(async () =>
    await GetDataAsync());
```

#### 获取值

```csharp
// 失败时返回默认值
var value1 = result.IfFail(0);

// 失败时通过函数处理
var value2 = result.IfFail(ex =>
{
    Console.WriteLine($"Error: {ex.Message}");
    return -1;
});
```

#### 转换值

```csharp
// Map：映射成功值
var mapped = result.Map(x => x * 2);

// Bind：链式转换
var bound = result.Bind(x => x > 0
    ? Result<string>.Succeed(x.ToString())
    : Result<string>.Fail(new ArgumentException("Must be positive")));

// 异步映射
var asyncMapped = await result.MapAsync(async x =>
    await ProcessAsync(x));
```

#### 模式匹配

```csharp
// 返回值的模式匹配
var message = result.Match(
    succ: value => $"Success: {value}",
    fail: ex => $"Error: {ex.Message}"
);

// 副作用的模式匹配
result.Match(
    succ: value => Console.WriteLine($"Success: {value}"),
    fail: ex => Console.WriteLine($"Error: {ex.Message}")
);
```

### 管道操作

#### Pipe：管道转换

```csharp
using GFramework.Core.Functional.pipe;

var result = 42
    .Pipe(x => x * 2)           // 84
    .Pipe(x => x.ToString())    // "84"
    .Pipe(s => $"Result: {s}"); // "Result: 84"
```

#### Tap：副作用操作

```csharp
var result = GetUser()
    .Tap(user => Console.WriteLine($"User: {user.Name}"))
    .Tap(user => _logger.LogInfo($"Processing user {user.Id}"))
    .Pipe(user => new UserDto { Id = user.Id, Name = user.Name });
```

#### Let：作用域转换

```csharp
var dto = GetUser().Let(user => new UserDto
{
    Id = user.Id,
    Name = user.Name,
    Email = user.Email
});
```

#### PipeIf：条件管道

```csharp
var result = 42.PipeIf(
    predicate: x => x > 0,
    ifTrue: x => $"Positive: {x}",
    ifFalse: x => $"Non-positive: {x}"
);
```

### 函数组合

#### Compose：函数组合

```csharp
using GFramework.Core.Functional.functions;

Func<int, int> addOne = x => x + 1;
Func<int, int> multiplyTwo = x => x * 2;

// f(g(x)) = (x + 1) * 2
var composed = multiplyTwo.Compose(addOne);
var result = composed(5);  // (5 + 1) * 2 = 12
```

#### AndThen：链式组合

```csharp
// g(f(x)) = (x + 1) * 2
var chained = addOne.AndThen(multiplyTwo);
var result = chained(5);  // (5 + 1) * 2 = 12
```

#### Curry：柯里化

```csharp
Func<int, int, int> add = (x, y) => x + y;
var curriedAdd = add.Curry();

var add5 = curriedAdd(5);
var result = add5(3);  // 8
```

## 高级用法

### Result 扩展操作

#### 链式副作用

```csharp
using GFramework.Core.Functional.result;

Result<int>.Succeed(42)
    .OnSuccess(x => Console.WriteLine($"Value: {x}"))
    .OnFailure(ex => Console.WriteLine($"Error: {ex.Message}"))
    .Map(x => x * 2);
```

#### 验证约束

```csharp
var result = Result<int>.Succeed(42)
    .Ensure(x => x > 0, "Value must be positive")
    .Ensure(x => x < 100, "Value must be less than 100");
```

#### 聚合多个结果

```csharp
var results = new[]
{
    Result<int>.Succeed(1),
    Result<int>.Succeed(2),
    Result<int>.Succeed(3)
};

var combined = results.Combine();  // Result<List<int>>
```

### 控制流扩展

#### TakeIf：条件返回

```csharp
using GFramework.Core.Functional.control;

var user = GetUser().TakeIf(u => u.IsActive);  // 活跃用户或 null

// 值类型版本
var number = 42.TakeIfValue(x => x > 0);  // 42 或 null
```

#### When：条件执行

```csharp
var result = 42
    .When(x => x > 0, x => Console.WriteLine($"Positive: {x}"))
    .When(x => x % 2 == 0, x => Console.WriteLine("Even"));
```

#### RepeatUntil：重复执行

```csharp
var result = 1.RepeatUntil(
    func: x => x * 2,
    predicate: x => x >= 100,
    maxIterations: 10
);  // 128
```

#### Retry：同步重试

```csharp
var result = ControlExtensions.Retry(
    func: () => UnstableOperation(),
    maxRetries: 3,
    delayMilliseconds: 100
);
```

### 异步函数式编程

#### 异步重试

```csharp
using GFramework.Core.Functional.async;

var result = await (() => UnreliableOperationAsync())
    .WithRetryAsync(
        maxRetries: 3,
        delay: TimeSpan.FromSeconds(1),
        shouldRetry: ex => ex is TimeoutException
    );
```

#### 异步安全执行

```csharp
var result = await (() => RiskyOperationAsync()).TryAsync();

result.Match(
    value => Console.WriteLine($"Success: {value}"),
    error => Console.WriteLine($"Failed: {error.Message}")
);
```

#### 异步绑定

```csharp
var result = Result<int>.Succeed(42);
var bound = await result.BindAsync(async x =>
    await GetUserAsync(x) is User user
        ? Result<User>.Succeed(user)
        : Result<User>.Fail(new Exception("User not found"))
);
```

### 高级函数操作

#### Partial：偏函数应用

```csharp
Func<int, int, int> add = (x, y) => x + y;
var add5 = add.Partial(5);
var result = add5(3);  // 8
```

#### Repeat：重复应用

```csharp
var result = 2.Repeat(3, x => x * 2);  // 2 * 2 * 2 * 2 = 16
```

#### Once：单次执行

```csharp
var counter = 0;
var once = (() => ++counter).Once();

var result1 = once();  // 1
var result2 = once();  // 1（不会再次执行）
```

#### Defer：延迟执行

```csharp
var lazy = (() => ExpensiveComputation()).Defer();
// 此时尚未执行
var result = lazy.Value;  // 首次访问时才执行
```

#### Memoize：结果缓存

```csharp
Func<int, int> expensive = x =>
{
    Thread.Sleep(1000);
    return x * x;
};

var memoized = expensive.MemoizeUnbounded();
var result1 = memoized(5);  // 耗时 1 秒
var result2 = memoized(5);  // 立即返回（从缓存）
```

## 最佳实践

### 何时使用 Option

1. **替代 null 引用**：当函数可能返回空值时
2. **配置参数**：表示可选的配置项
3. **查找操作**：字典查找、数据库查询等可能失败的操作
4. **链式操作**：需要安全地链式调用多个可能返回空值的方法

```csharp
// 不推荐：使用 null
public User? FindUser(int id)
{
    return _users.ContainsKey(id) ? _users[id] : null;
}

// 推荐：使用 Option
public Option<User> FindUser(int id)
{
    return _users.TryGetValue(id, out var user)
        ? Option<User>.Some(user)
        : Option<User>.None;
}
```

### 何时使用 Result

1. **错误处理**：需要显式处理错误的操作
2. **验证逻辑**：输入验证、业务规则检查
3. **外部调用**：网络请求、文件操作等可能失败的 I/O 操作
4. **避免异常**：不希望使用异常进行流程控制的场景

```csharp
// 不推荐：使用异常
public int ParseNumber(string input)
{
    if (!int.TryParse(input, out var number))
        throw new ArgumentException("Invalid number");
    return number;
}

// 推荐：使用 Result
public Result<int> ParseNumber(string input)
{
    return int.TryParse(input, out var number)
        ? Result<int>.Succeed(number)
        : Result<int>.Failure("Invalid number");
}
```

### 何时使用管道操作

1. **数据转换**：需要对数据进行多步转换
2. **流式处理**：构建数据处理管道
3. **副作用隔离**：使用 Tap 隔离副作用操作
4. **提高可读性**：使复杂的嵌套调用变得线性

```csharp
// 不推荐：嵌套调用
var result = FormatResult(
    ValidateInput(
        ParseInput(
            GetInput()
        )
    )
);

// 推荐：管道操作
var result = GetInput()
    .Pipe(ParseInput)
    .Pipe(ValidateInput)
    .Tap(x => _logger.LogInfo($"Validated: {x}"))
    .Pipe(FormatResult);
```

### 错误处理模式

#### 模式 1：早期返回

```csharp
public Result<User> CreateUser(string name, string email)
{
    return ValidateName(name)
        .Bind(_ => ValidateEmail(email))
        .Bind(_ => CheckDuplicate(email))
        .Bind(_ => SaveUser(name, email));
}
```

#### 模式 2：聚合验证

```csharp
public Result<User> CreateUser(UserDto dto)
{
    var validations = new[]
    {
        ValidateName(dto.Name),
        ValidateEmail(dto.Email),
        ValidateAge(dto.Age)
    };

    return validations.Combine()
        .Bind(_ => SaveUser(dto));
}
```

#### 模式 3：错误恢复

```csharp
public Result<Data> GetData(int id)
{
    return GetFromCache(id)
        .Match(
            succ: data => Result<Data>.Succeed(data),
            fail: _ => GetFromDatabase(id)
        );
}
```

### 组合模式

#### 模式 1：Option + Result

```csharp
public Result<User> GetActiveUser(int id)
{
    return FindUser(id)  // Option<User>
        .ToResult("User not found")  // Result<User>
        .Ensure(u => u.IsActive, "User is not active");
}
```

#### 模式 2：Result + 管道

```csharp
public Result<UserDto> ProcessUser(int id)
{
    return Result<int>.Succeed(id)
        .Bind(GetUser)
        .Map(user => user.Tap(u => _logger.LogInfo($"Processing {u.Name}")))
        .Map(user => new UserDto { Id = user.Id, Name = user.Name });
}
```

#### 模式 3：异步组合

```csharp
public async Task<Result<Response>> ProcessRequestAsync(Request request)
{
    return await Result<Request>.Succeed(request)
        .Ensure(r => r.IsValid, "Invalid request")
        .BindAsync(async r => await ValidateAsync(r))
        .BindAsync(async r => await ProcessAsync(r))
        .MapAsync(async r => await FormatResponseAsync(r));
}
```

## 常见问题

### Option vs Nullable

**Q: Option 和 `Nullable<T>` 有什么区别？**

A:

- `Nullable<T>` 只能用于值类型，`Option<T>` 可用于任何类型
- `Option<T>` 提供丰富的函数式操作（Map、Bind、Filter 等）
- `Option<T>` 强制显式处理"无值"情况，更安全
- `Option<T>` 可以与 Result 等其他函数式类型组合

### Result vs Exception

**Q: 什么时候应该使用 Result 而不是异常？**

A:

- **使用 Result**：预期的错误情况（验证失败、资源不存在等）
- **使用 Exception**：意外的错误情况（系统错误、编程错误等）
- Result 使错误处理显式化，提高代码可读性
- Result 避免异常的性能开销

### 性能考虑

**Q: 函数式编程会影响性能吗？**

A:

- Option 和 Result 是值类型（struct），性能开销很小
- 管道操作本质是方法调用，JIT 会进行内联优化
- Memoize 等缓存机制可以提高性能
- 对于性能敏感的代码，可以选择性使用函数式特性

### 与 LINQ 的关系

**Q: 函数式扩展与 LINQ 有什么区别？**

A:

- LINQ 主要用于集合操作，函数式扩展用于单值操作
- 两者可以很好地组合使用
- Option 和 Result 可以转换为 IEnumerable 与 LINQ 集成

```csharp
// Option 转 LINQ
var options = new[]
{
    Option<int>.Some(1),
    Option<int>.None,
    Option<int>.Some(3)
};

var values = options
    .SelectMany(o => o.ToEnumerable())
    .ToList();  // [1, 3]
```

### 学习曲线

**Q: 函数式编程难学吗？**

A:

- 从简单的 Option 和 Result 开始
- 逐步引入管道操作和函数组合
- 不需要一次性掌握所有特性
- 在实际项目中逐步应用，积累经验

## 参考资源

### 相关文档

- [Architecture 包使用说明](./architecture.md)
- [Extensions 扩展方法](./extensions.md)
- [CQRS 模式](./cqrs.md)

### 外部资源

- [函数式编程原理](https://en.wikipedia.org/wiki/Functional_programming)
- [Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/)
- [Option 类型模式](https://en.wikipedia.org/wiki/Option_type)

### 示例代码

完整的示例代码可以在测试项目中找到：

- `GFramework.Core.Tests/functional/OptionTests.cs`
- `GFramework.Core.Tests/functional/ResultTests.cs`
- `GFramework.Core.Tests/functional/pipe/PipeExtensionsTests.cs`
- `GFramework.Core.Tests/functional/functions/FunctionExtensionsTests.cs`
- `GFramework.Core.Tests/functional/control/ControlExtensionsTests.cs`
