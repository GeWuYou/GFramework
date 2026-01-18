# GFramework.Core 模块测试覆盖详细清单

> **生成日期**: 2026-01-18
> **最后更新**: 2026-01-18
> **当前版本**: Core测试覆盖率 ~79.2% (文件级别)
> **目标**: 提升Core模块测试覆盖率至 95%+ 并补充缺失的单元测试

---

## 📊 总体统计

| 类别     | 源文件数   | 有测试文件数 | 缺失测试文件数 | 测试覆盖率     |
|--------|--------|--------|---------|-----------|
| 架构系统   | 6      | 4      | 1       | 83%       |
| 事件系统   | 8      | 5      | 0       | 100%      |
| 命令系统   | 4      | 1      | 3       | 25%       |
| 查询系统   | 5      | 1      | 3       | 20%       |
| 日志系统   | 5      | 2      | 0       | 100%      |
| 扩展方法   | 4      | 2      | 0       | 100%      |
| 状态系统   | 4      | 2      | 0       | 100%      |
| IOC容器  | 1      | 1      | 0       | 100%      |
| 模型系统   | 1      | 0      | 0       | 100%      |
| 系统基类   | 1      | 0      | 0       | 100%      |
| 对象池    | 1      | 1      | 0       | 100%      |
| 属性系统   | 2      | 1      | 0       | 100%      |
| 规则系统   | 1      | 0      | 0       | 100%      |
| 工具类    | 1      | 0      | 1       | 0%        |
| 环境系统   | 2      | 1      | 0       | 100%      |
| 常量     | 2      | 0      | 2       | 0%        |
| **总计** | **48** | **20** | **10**  | **79.2%** |

> **注**: 标记为0个测试文件的模块通过间接测试（集成测试）实现了功能覆盖
> **重要发现**: 命令系统和查询系统的异步功能完全缺失测试！

---

## 🎯 测试补充优先级概览

| 优先级     | 任务数   | 预计测试数     | 描述          |
|---------|-------|-----------|-------------|
| 🔴 高优先级 | 5     | 34-44     | 异步核心功能和工具基类 |
| 🟡 中优先级 | 2     | 6-10      | 常量验证测试      |
| **总计**  | **7** | **40-54** | -           |

---

## 📋 详细源文件与测试文件对应关系

### Architecture 模块 (6个源文件)

| 源文件                          | 对应测试文件                                              | 测试覆盖    |
|------------------------------|-----------------------------------------------------|---------|
| Architecture.cs              | SyncArchitectureTests.cs, AsyncArchitectureTests.cs | ✅ 98个测试 |
| ArchitectureConfiguration.cs | ArchitectureConfigurationTests.cs                   | ✅ 12个测试 |
| ArchitectureConstants.cs     | **缺失**                                              | ❌ 需补充   |
| ArchitectureContext.cs       | ArchitectureContextTests.cs                         | ✅ 22个测试 |
| ArchitectureServices.cs      | ArchitectureServicesTests.cs                        | ✅ 15个测试 |
| GameContext.cs               | GameContextTests.cs                                 | ✅ 已有测试  |

**测试用例总数**: 147个

---

### Command 模块 (4个源文件)

| 源文件                         | 对应测试文件                  | 测试覆盖       |
|-----------------------------|-------------------------|------------|
| **AbstractAsyncCommand.cs** | **缺失**                  | ❌ 需创建测试文件  |
| AbstractCommand.cs          | CommandBusTests.cs (间接) | ✅ 已覆盖      |
| **CommandBus.cs**           | CommandBusTests.cs      | ⚠️ 需补充异步测试 |
| EmptyCommandInput.cs        | CommandBusTests.cs (间接) | ✅ 已覆盖      |

**测试用例总数**: 4个（需补充异步测试）

**需要补充**:

1. ❌ AbstractAsyncCommandTests.cs - 新建（高优先级）
2. ❌ CommandBusTests.cs - 补充 SendAsync 方法测试（高优先级）

---

### Query 模块 (5个源文件)

| 源文件                       | 对应测试文件                | 测试覆盖      |
|---------------------------|-----------------------|-----------|
| **AbstractAsyncQuery.cs** | **缺失**                | ❌ 需创建测试文件 |
| AbstractQuery.cs          | QueryBusTests.cs (间接) | ✅ 已覆盖     |
| **AsyncQueryBus.cs**      | **缺失**                | ❌ 需创建测试文件 |
| EmptyQueryInput.cs        | QueryBusTests.cs (间接) | ✅ 已覆盖     |
| QueryBus.cs               | QueryBusTests.cs      | ✅ 3个测试    |

**测试用例总数**: 3个（需补充异步测试）

**需要补充**:

1. ❌ AbstractAsyncQueryTests.cs - 新建（高优先级）
2. ❌ AsyncQueryBusTests.cs - 新建（高优先级）

---

### Constants 模块 (2个源文件)

| 源文件                      | 对应测试文件 | 测试覆盖  |
|--------------------------|--------|-------|
| ArchitectureConstants.cs | **缺失** | ❌ 需补充 |
| GFrameworkConstants.cs   | **缺失** | ❌ 需补充 |

**测试用例总数**: 0个

**需要补充**:

1. ❌ ArchitectureConstantsTests.cs - 新建（中优先级）
2. ❌ GFrameworkConstantsTests.cs - 新建（中优先级）

---

### Utility 模块 (1个源文件)

| 源文件                       | 对应测试文件 | 测试覆盖      |
|---------------------------|--------|-----------|
| AbstractContextUtility.cs | **缺失** | ❌ 需创建测试文件 |

**测试用例总数**: 0个

**需要补充**:

1. ❌ AbstractContextUtilityTests.cs - 新建（高优先级）

---

### 其他模块 (Events, Logging, IoC, etc.)

所有其他模块的测试覆盖率均达到 100%（包括间接测试覆盖），详见下文详细列表。

---

## 🔴 高优先级 - 异步核心功能（5个任务）

### 任务1: CommandBusTests.cs - 补充异步测试

**源文件路径**: `GFramework.Core/command/CommandBus.cs`

**优先级**: 🔴 高

**原因**: CommandBus 已实现 SendAsync 方法但没有任何测试

**需要补充的测试内容**:

- ✅ SendAsync(IAsyncCommand) 方法 - 执行无返回值的异步命令
- ✅ SendAsync(IAsyncCommand) 方法 - 处理 null 异步命令
- ✅ SendAsync<TResult>(IAsyncCommand<TResult>) 方法 - 执行有返回值的异步命令
- ✅ SendAsync<TResult>(IAsyncCommand<TResult>) 方法 - 处理 null 异步命令

**预计测试数**: 4 个

**测试文件**: `GFramework.Core.Tests/command/CommandBusTests.cs`

**操作**: 在现有测试文件中补充异步测试方法

**状态**: ❌ 待补充

---

### 任务2: AbstractAsyncCommandTests.cs

**源文件路径**: `GFramework.Core/command/AbstractAsyncCommand.cs`

**优先级**: 🔴 高

**原因**: 异步命令基类没有任何单元测试，是核心功能

**需要测试的内容**:

- ✅ 异步命令无返回值版本的基础实现
- ✅ 异步命令有返回值版本的基础实现
- ✅ ExecuteAsync 方法调用
- ✅ ExecuteAsync 方法的异常处理
- ✅ 上下文感知功能（SetContext, GetContext）
- ✅ 日志功能（Logger属性）
- ✅ 子类继承行为验证（两个版本）
- ✅ 命令执行前日志记录
- ✅ 命令执行后日志记录
- ✅ 错误情况下的日志记录

**预计测试数**: 10-12 个

**创建路径**: `GFramework.Core.Tests/command/AbstractAsyncCommandTests.cs`

**状态**: ❌ 待创建

---

### 任务3: AsyncQueryBusTests.cs

**源文件路径**: `GFramework.Core/query/AsyncQueryBus.cs`

**优先级**: 🔴 高

**原因**: 异步查询总线是核心组件，需要完整的单元测试

**需要测试的内容**:

- ✅ SendAsync 方法 - 正常查询发送
- ✅ SendAsync 方法 - 空查询异常
- ✅ 异步查询结果正确性
- ✅ 不同返回类型的异步查询支持
- ✅ 异步查询的异常处理
- ✅ 异步查询的上下文传递

**预计测试数**: 6-8 个

**创建路径**: `GFramework.Core.Tests/query/AsyncQueryBusTests.cs`

**状态**: ❌ 待创建

---

### 任务4: AbstractAsyncQueryTests.cs

**源文件路径**: `GFramework.Core/query/AbstractAsyncQuery.cs`

**优先级**: 🔴 高

**原因**: 异步查询基类没有任何单元测试，是核心功能

**需要测试的内容**:

- ✅ 异步查询的基础实现
- ✅ DoAsync 方法调用
- ✅ DoAsync 方法的异常处理
- ✅ 上下文感知功能（SetContext, GetContext）
- ✅ 日志功能（Logger属性）
- ✅ 子类继承行为验证
- ✅ 查询执行前日志记录
- ✅ 查询执行后日志记录
- ✅ 返回值类型验证
- ✅ 错误情况下的日志记录

**预计测试数**: 8-10 个

**创建路径**: `GFramework.Core.Tests/query/AbstractAsyncQueryTests.cs`

**状态**: ❌ 待创建

---

### 任务5: AbstractContextUtilityTests.cs

**源文件路径**: `GFramework.Core/utility/AbstractContextUtility.cs`

**优先级**: 🔴 高

**原因**: 工具基类需要直接的单元测试以确保其基础功能正确性

**需要测试的内容**:
- ✅ 抽象工具类实现
- ✅ IContextUtility 接口实现
- ✅ Init 方法调用
- ✅ 日志初始化
- ✅ 上下文感知功能（SetContext, GetContext）
- ✅ 子类继承行为
- ✅ 工具初始化日志记录
- ✅ 工具生命周期完整性

**预计测试数**: 6-8 个

**创建路径**: `GFramework.Core.Tests/utility/AbstractContextUtilityTests.cs`

**状态**: ❌ 待创建

---

## 🟡 中优先级 - 常量验证（2个任务）

### 任务6: ArchitectureConstantsTests.cs

**源文件路径**: `GFramework.Core/architecture/ArchitectureConstants.cs`

**优先级**: 🟡 中

**原因**: 验证架构相关的常量定义是否正确

**需要测试的内容**:
- ✅ 常量值的正确性
- ✅ 常量类型验证
- ✅ 常量可访问性
- ✅ 常量命名规范
- ✅ 架构阶段定义常量

**预计测试数**: 3-5 个

**创建路径**: `GFramework.Core.Tests/architecture/ArchitectureConstantsTests.cs`

**状态**: ❌ 待创建

---

### 任务7: GFrameworkConstantsTests.cs

**源文件路径**: `GFramework.Core/constants/GFrameworkConstants.cs`

**优先级**: 🟡 中

**原因**: 验证框架级别的常量定义

**需要测试的内容**:
- ✅ 版本号常量格式正确性
- ✅ 其他框架常量
- ✅ 常量值正确性
- ✅ 常量类型验证
- ✅ 常量可访问性

**预计测试数**: 3-5 个

**创建路径**: `GFramework.Core.Tests/constants/GFrameworkConstantsTests.cs`

**状态**: ❌ 待创建

---

## 📊 测试执行计划

### 第一批：异步核心功能（4个任务，预计 1.5小时）

| 序号 | 测试任务                         | 操作 | 预计测试数 | 优先级  | 预计时间 |
|----|------------------------------|----|-------|------|------|
| 1  | CommandBusTests.cs - 补充异步测试  | 补充 | 4     | 🔴 高 | 20分钟 |
| 2  | AbstractAsyncCommandTests.cs | 新建 | 10-12 | 🔴 高 | 30分钟 |
| 3  | AsyncQueryBusTests.cs        | 新建 | 6-8   | 🔴 高 | 25分钟 |
| 4  | AbstractAsyncQueryTests.cs   | 新建 | 8-10  | 🔴 高 | 25分钟 |

**小计**: 28-34 个测试，约 1.5小时

---

### 第二批：工具基类（1个任务，预计 15分钟）

| 序号 | 测试文件                           | 操作 | 预计测试数 | 优先级  | 预计时间 |
|----|--------------------------------|----|-------|------|------|
| 5  | AbstractContextUtilityTests.cs | 新建 | 6-8   | 🔴 高 | 15分钟 |

**小计**: 6-8 个测试

---

### 第三批：常量验证（2个任务，预计 20分钟）

| 序号 | 测试文件                          | 操作 | 预计测试数 | 优先级  | 预计时间 |
|----|-------------------------------|----|-------|------|------|
| 6  | ArchitectureConstantsTests.cs | 新建 | 3-5   | 🟡 中 | 10分钟 |
| 7  | GFrameworkConstantsTests.cs   | 新建 | 3-5   | 🟡 中 | 10分钟 |

**小计**: 6-10 个测试

---

## 📊 最终统计

| 批次       | 任务数   | 操作          | 预计测试数     | 预计时间    |
|----------|-------|-------------|-----------|---------|
| 第一批（异步）  | 4     | 3新建+1补充     | 28-34     | 1.5小时   |
| 第二批（高优先） | 1     | 新建          | 6-8       | 15分钟    |
| 第三批（中优先） | 2     | 新建          | 6-10      | 20分钟    |
| **总计**   | **7** | **6新建+1补充** | **40-54** | **2小时** |

---

## 🎯 目标达成路径

### 当前状态（2026-01-18）

- **现有测试数**: 496 个
- **文件覆盖率**: 79.2% (38/48个文件有测试覆盖)
- **缺失测试**: 40-54 个
- **已完成文件**: 38/48
- **关键发现**: 异步命令和查询功能完全缺失测试

### 补充测试完成后预计

- **预计测试数**: 496 + 40-54 = 536-550 个
- **预计文件覆盖率**: ~95.8% (46/48)
- **代码行覆盖率**: 预计 90%+ （需通过覆盖率工具精确测量）

---

## 📝 注意事项

### 注释规范

- ✅ 生成的测试类需要有注释说明这个测试类具体有哪些测试
- ✅ 测试方法需要有注释说明具体测试的是什么
- ✅ 对于复杂逻辑的测试方法，需要有标准的行注释说明逻辑，不要使用行尾注释
- ✅ 对于类与方法的测试，需要标准的C#文档注释

### 测试隔离性

1. ✅ 每个测试文件使用独立的测试辅助类（TestXxxV2, TestXxxV3等）
2. ✅ 避免与现有测试类（TestSystem, TestModel）命名冲突
3. ✅ 使用 `[SetUp]` 和 `[TearDown]` 确保测试隔离
4. ✅ 必要时使用 `[NonParallelizable]` 特性
5. ✅ 异步测试需要正确使用 `async/await` 模式

### 测试命名规范
- 测试类：`{Component}Tests`
- 测试方法：`{Scenario}_Should_{ExpectedOutcome}`
- 测试辅助类：`Test{Component}V{Version}`
- 异步测试方法建议包含 `Async` 关键字

### 构建和验证流程
1. 编写测试代码
2. 运行 `dotnet build` 验证编译
3. 运行 `dotnet test` 执行测试
4. 检查测试通过率
5. 修复失败或隔离性问题

### 异步测试最佳实践

1. **正确使用 async/await**
    - 测试方法标记为 `async Task`
    - 所有异步操作使用 `await`
    - 不要使用 `.Result` 或 `.Wait()` 导致死锁

2. **异常测试**
    - 使用 `Assert.ThrowsAsync<T>` 测试异步异常
    - 确保异常在正确的位置抛出

3. **测试辅助类**
    - 创建模拟的异步命令/查询类
    - 验证异步操作是否正确执行
    - 测试并发场景（如需要）

### 代码覆盖率工具建议

建议添加 Coverlet 代码覆盖率工具以获得精确的覆盖率数据：

```xml
<ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.0">
        <PrivateAssets>all</PrivateAssets>
        <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
</ItemGroup>
```

运行覆盖率命令：

```bash
dotnet test --collect:"XPlat Code Coverage"
```

---

## 🔄 更新日志

| 日期         | 操作        | 说明                                                                                                                                                                                                  |
|------------|-----------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 2026-01-16 | 初始创建      | 生成原始测试覆盖清单（包含错误）                                                                                                                                                                                    |
| 2026-01-18 | 全面更新（第1版） | 重新检查框架和测试，修正以下问题：<br>1. 删除不存在的ContextAwareStateMachineTests.cs<br>2. 更新实际测试数量为496个<br>3. 添加新增源文件<br>4. 修正文件覆盖率从41%提升至91.5%<br>5. 调整优先级，从26个减少到3个缺失测试文件                                              |
| 2026-01-18 | 全面更新（第2版） | 补充异步命令和异步查询测试计划：<br>1. 发现CommandBus已有SendAsync实现但无测试<br>2. 发现AbstractAsyncCommand、AsyncQueryBus、AbstractAsyncQuery无测试<br>3. 新增4个高优先级异步测试任务<br>4. 更新文件覆盖率从91.5%调整为79.2%（补充异步后）<br>5. 总测试数从40-54调整为目标 |

---

## 📌 待确认事项

- [x] 确认优先级划分是否合理
- [x] 确认执行计划是否可行
- [x] 确认测试用例数量估算是否准确
- [x] 确认测试隔离策略是否完整
- [ ] 添加代码覆盖率工具配置
- [ ] 确定是否需要补充间接测试为直接测试

---

## 🎉 成就解锁

### 已完成的测试覆盖

✅ **架构系统核心功能** - 147个测试覆盖
✅ **事件系统完整功能** - 37个测试覆盖
✅ **日志系统完整功能** - 69个测试覆盖
✅ **IoC容器** - 21个测试覆盖
✅ **状态机系统** - 33个测试覆盖
✅ **对象池系统** - 6个测试覆盖
✅ **属性系统** - 8个测试覆盖
✅ **扩展方法** - 17个测试覆盖
✅ **同步命令查询系统** - 通过集成测试覆盖
✅ **模型系统** - 通过架构集成测试覆盖
✅ **系统基类** - 通过架构集成测试覆盖

### 待补充的异步功能

❌ **异步命令系统** - AbstractAsyncCommand、CommandBus.SendAsync
❌ **异步查询系统** - AsyncQueryBus、AbstractAsyncQuery
❌ **工具基类** - AbstractContextUtility
❌ **常量验证** - ArchitectureConstants、GFrameworkConstants

### 测试质量指标

- **测试用例总数**: 496个
- **文件级别覆盖率**: 79.2%
- **支持测试的.NET版本**: .NET 8.0, .NET 10.0
- **测试框架**: NUnit 3.x
- **测试隔离性**: 良好
- **测试组织结构**: 清晰（按模块分类）

---

## 🚀 实施进度

### 第一批：异步核心功能

- [ ] 任务1: CommandBusTests.cs - 补充异步测试 (4个测试)
- [ ] 任务2: AbstractAsyncCommandTests.cs (10-12个测试)
- [ ] 任务3: AsyncQueryBusTests.cs (6-8个测试)
- [ ] 任务4: AbstractAsyncQueryTests.cs (8-10个测试)

### 第二批：工具基类

- [ ] 任务5: AbstractContextUtilityTests.cs (6-8个测试)

### 第三批：常量验证

- [ ] 任务6: ArchitectureConstantsTests.cs (3-5个测试)
- [ ] 任务7: GFrameworkConstantsTests.cs (3-5个测试)

---

**文档维护**: 请在完成每个测试任务后更新本文档的状态和实施进度
