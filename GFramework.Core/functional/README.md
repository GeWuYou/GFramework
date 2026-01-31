# Functional Extensions - 函数式编程扩展方法

提供了一系列用于函数式编程的扩展方法，已按功能拆分为多个类别。

## 功能概览

这些扩展方法实现了函数式编程的核心概念，包括管道操作、函数组合、集合操作、控制流、类型转换等功能，使C#代码更加简洁和函数式。

## 模块分类

### 1. Pipe Extensions - 管道和函数组合操作
位于 `GFramework.Core.functional.pipe` 命名空间，提供管道和函数组合操作。

#### 方法列表及用法：
- **Pipe** - 把值送进函数
```csharp
// 将值传入函数进行处理
var result = 5.Pipe(x => x * 2); // 结果为 10
```

- **Then** - 函数组合（f1.Then(f2)）
```csharp
// 组合两个函数，先执行first再执行second
Func<int, int> addTwo = x => x + 2;
Func<int, int> multiplyByThree = x => x * 3;
var composed = addTwo.Then(multiplyByThree); // 先+2再*3
var result = composed(5); // (5+2)*3 = 21
```

- **After** - 反向函数组合
```csharp
// 与Then相反，以不同的顺序组合函数
Func<int, int> multiplyByThree = x => x * 3;
Func<int, int> addTwo = x => x + 2;
var composed = multiplyByThree.After(addTwo); // 先+2再*3
var result = composed(5); // (5+2)*3 = 21
```

- **Tap** - 执行副作用操作但返回原值
```csharp
// 执行副作用操作但返回原值，常用于调试或日志记录
var value = 42.Tap(Console.WriteLine); // 输出42，但value仍为42
```

- **Apply** - 将函数应用于值
```csharp
// 将参数应用于函数
Func<int, int> multiplyByTwo = x => x * 2;
var result = multiplyByTwo.Apply(5); // 10
```

- **Also** - 执行操作并返回原值
```csharp
// 执行操作并返回原值
var value = 42.Also(Console.WriteLine); // 输出42，返回42
```

- **Let** - 将值转换为另一个值
```csharp
// 转换值
var result = 5.Let(x => x * 2); // 10
```

### 2. Collections Extensions - 集合操作
位于 `GFramework.Core.functional.collections` 命名空间，提供对集合的函数式操作。

#### 方法列表及用法：
- **Map** - 映射操作
```csharp
// 对集合中的每个元素应用函数
var numbers = new[] {1, 2, 3, 4};
var squared = numbers.Map(x => x * x); // {1, 4, 9, 16}
```

- **Filter** - 过滤操作
```csharp
// 过滤集合中的元素
var numbers = new[] {1, 2, 3, 4, 5, 6};
var evens = numbers.Filter(x => x % 2 == 0); // {2, 4, 6}
```

- **Reduce** - 归约操作
```csharp
// 将集合归约为单个值
var numbers = new[] {1, 2, 3, 4};
var sum = numbers.Reduce(0, (acc, x) => acc + x); // 10
```

### 3. Control Extensions - 控制流操作
位于 `GFramework.Core.functional.control` 命名空间，提供函数式风格的控制结构。

#### 方法列表及用法：
- **Match** - 模式匹配
```csharp
// 基于条件的模式匹配
var result = 5.Match(
    (x => x < 0, _ => "负数"),
    (x => x > 0, _ => "正数"),
    (x => x == 0, _ => "零")
); // "正数"
```

- **MatchOrDefault** - 带默认值的模式匹配
```csharp
// 模式匹配，无匹配时返回默认值
var result = 0.MatchOrDefault("未知数字",
    (x => x < 0, _ => "负数"),
    (x => x > 0, _ => "正数")
); // "未知数字"
```

- **If / IfElse** - 条件执行
```csharp
// 条件执行操作
var result = 5.If(x => x > 0, x => x * 2); // 10 (因为5>0，所以乘以2)
var result2 = 5.IfElse(
    x => x > 10, 
    x => x * 2,      // 条件为真时执行
    x => x + 1       // 条件为假时执行
); // 6 (因为5不大于10，所以+1)
```

- **TakeIf / TakeUnless** - 条件取值
```csharp
// 条件为真时返回值，否则返回null
string str = "Hello";
var result1 = str.TakeIf(s => s.Length > 3); // "Hello"
var result2 = str.TakeIf(s => s.Length < 3); // null

// 条件为假时返回值，否则返回null
var result3 = str.TakeUnless(s => s.Length > 10); // "Hello" (因为长度不大于10)
var result4 = str.TakeUnless(s => s.Length > 3);  // null (因为长度大于3)
```

### 4. Function Extensions - 函数式操作
位于 `GFramework.Core.functional.functions` 命名空间，提供柯里化、偏函数应用等高级函数式特性。

#### 方法列表及用法：
- **Curry** - 柯里化
```csharp
// 将多参数函数转换为链式单参数函数
Func<int, int, int> add = (x, y) => x + y;
var curriedAdd = add.Curry(); // 返回 Func<int, Func<int, int>>
var addFive = curriedAdd(5); // 返回 Func<int, int>
var result = addFive(3); // 8
```

- **Uncurry** - 取消柯里化
```csharp
// 将柯里化函数转换回多参数函数
var curriedAdd = ((Func<int, int, int>)((x, y) => x + y)).Curry();
var uncurriedAdd = curriedAdd.Uncurry(); // 返回 Func<int, int, int>
var result = uncurriedAdd(5, 3); // 8
```

- **Partial** - 部分应用
```csharp
// 固定函数的部分参数
Func<int, int, int> multiply = (x, y) => x * y;
var double = multiply.Partial(2); // 固定第一个参数为2
var result = double(5); // 10
```

- **Repeat** - 重复执行
```csharp
// 重复执行函数n次
var result = 2.Repeat(3, x => x * 2); // 2 -> 4 -> 8 -> 16
```

- **Try** - 安全执行
```csharp
// 安全执行可能抛出异常的函数
var (success, result, error) = 10.Try(x => 100 / x); // 成功，result为10
var (success2, result2, error2) = 0.Try(x => 100 / x); // 失败，success2为false
```

- **Memoize** - 缓存结果
```csharp
// 缓存函数结果以提高性能
Func<int, int> expensiveFunction = x => 
{
    // 模拟耗时操作
    System.Threading.Thread.Sleep(1000);
    return x * x;
};
var memoized = expensiveFunction.Memoize();
var result1 = memoized(5); // 首次调用需要1秒
var result2 = memoized(5); // 立即返回，使用缓存结果
```

### 5. Type Extensions - 类型转换
位于 `GFramework.Core.functional.types` 命名空间，提供类型转换相关的扩展方法。

#### 方法列表及用法：
- **As** - 安全类型转换
```csharp
// 安全类型转换，失败时返回null
object obj = "Hello";
var str = obj.As<string>(); // "Hello"
var incompatible = obj.As<int>(); // null
```

- **Cast** - 强制类型转换
```csharp
// 强制类型转换，失败时抛出异常
object obj = "Hello";
var str = obj.Cast<string>(); // "Hello"
```

## 使用示例

### 链式操作
```csharp
var result = new[] {1, 2, 3, 4, 5}
    .Filter(x => x % 2 == 0)        // 过滤偶数: {2, 4}
    .Map(x => x * x)               // 平方: {4, 16}
    .Reduce(0, (sum, x) => sum + x); // 求和: 20
```

### 函数组合
```csharp
// 创建复合函数
Func<int, int> addTwo = x => x + 2;
Func<int, int> square = x => x * x;
Func<int, int> subtractOne = x => x - 1;

// 组合多个函数
var complexOperation = addTwo.Then(square).Then(subtractOne);
var result = complexOperation(3); // ((3+2)^2)-1 = 24
```

## 注意事项

- 部分方法（如Memoize）对泛型参数有约束（例如`where TSource : notnull`）
- Try方法返回元组，便于处理可能发生的异常
- 柯里化和部分应用有助于创建更灵活的函数
- 链式操作可以提高代码可读性，但要注意性能影响

## 适用场景

- 数据转换和处理管道
- 函数组合和复用
- 避免中间变量的创建
- 提高代码的声明式风格
- 创建可重用的功能组件