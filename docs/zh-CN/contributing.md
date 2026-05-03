---
title: 贡献指南
description: 说明参与 GFramework 仓库贡献时的协作方式、提交流程与社区规范。
---

# 贡献指南

欢迎为 GFramework 贡献代码！本指南将帮助你了解如何参与项目开发。

## 概述

GFramework 是一个开源的游戏开发框架，我们欢迎所有形式的贡献：

- 报告 Bug 和提出功能建议
- 提交代码修复和新功能
- 改进文档和示例
- 参与讨论和代码审查

## 行为准则

### 社区规范

我们致力于为所有贡献者提供友好、安全和包容的环境。参与本项目时，请遵守以下准则：

- **尊重他人**：尊重不同的观点和经验
- **建设性沟通**：提供有建设性的反馈，避免人身攻击
- **协作精神**：帮助新贡献者融入社区
- **专业态度**：保持专业和礼貌的交流方式

### 不可接受的行为

- 使用性别化语言或图像
- 人身攻击或侮辱性评论
- 骚扰行为（公开或私下）
- 未经许可发布他人的私人信息
- 其他不道德或不专业的行为

## 如何贡献

### 报告问题

发现 Bug、有功能建议、文档问题或使用疑问时，请通过 GitHub Issues 提交，并优先使用仓库提供的 Issue Forms：

- **Bug Report / 缺陷报告**：用于可复现缺陷、异常行为、回归问题
- **Feature Request / 功能建议**：用于新能力、API 改进、工作流增强
- **Documentation / 文档改进**：用于文档缺失、过期、错误、示例不足
- **Question / 使用咨询**：用于与 GFramework 行为、API、接入方式直接相关的问题

提交前请遵循以下原则：

1. **先搜索现有 Issue**：避免重复提交
2. **先查阅文档**：优先阅读相关 README、`docs/` 页面和排障内容
3. **选择最合适的模板**：让问题更容易分诊和跟进
4. **补齐最小必要信息**：
    - Bug：复现步骤、预期行为、实际行为、环境信息、最小复现或日志
    - 功能建议：问题背景、使用场景、建议方案、替代方案、兼容性影响
    - 文档问题：文档路径、问题类型、当前问题、期望改进
    - 使用咨询：目标、当前尝试、具体问题、相关环境

### Issue 分诊建议

为便于维护者快速处理，建议按以下方式理解 Issue 类型：

- **bug**：当前行为与契约、文档或既有能力不一致，且可以描述具体异常或错误结果
- **enhancement**：现有行为合理但不足，希望新增能力或改进 API / 工作流
- **documentation**：主要问题在文档内容，而不是运行时行为
- **question**：主要诉求是澄清如何使用、如何设计或如何接入
- **needs-repro / needs-info**：当缺少复现仓库、版本信息或关键上下文时，维护者可在分诊时补充使用

> 仓库中的标签集合可能会继续演进，但以上分类建议应保持稳定。

### 提交 Pull Request

#### 基本流程

1. **Fork 仓库**：在 GitHub 上 Fork 本项目
2. **克隆到本地**：
   ```bash
   git clone https://github.com/your-username/GFramework.git
   cd GFramework
   ```
3. **创建特性分支**：
   ```bash
   git checkout -b feature/your-feature-name
   # 或
   git checkout -b fix/your-bug-fix
   ```
4. **进行开发**：编写代码、添加测试、更新文档
5. **提交更改**：遵循提交规范（见下文）
6. **推送分支**：
   ```bash
   git push origin feature/your-feature-name
   ```
7. **创建 PR**：在 GitHub 上创建 Pull Request

#### PR 要求

- **清晰的标题**：简洁描述变更内容
- **详细的描述**：
    - 变更的背景和动机
    - 实现方案说明
    - 测试验证结果
    - 相关 Issue 链接（如有）
- **代码质量**：通过所有 CI 检查
- **测试覆盖**：为新功能添加测试
- **文档更新**：更新相关文档

#### 许可证文件头

仓库采用 Apache License 2.0。新增或修改仓库维护的源码和配置文件时，请保留或补齐文件头声明。CI 会检查
C#、JavaScript、TypeScript、Python、Shell、YAML 与 MSBuild 项目文件中的 Apache-2.0 声明。

本地检查：

```bash
python3 scripts/license-header.py --check
```

本地自动修复：

```bash
python3 scripts/license-header.py --fix
```

如只想预览会修改哪些文件，可运行：

```bash
python3 scripts/license-header.py --fix --dry-run
```

维护者也可以在 GitHub Actions 中手动运行 `License Header Fix` workflow。该流程会基于目标分支创建
`chore/license-headers-<run-id>` 分支，自动补齐缺失文件头，并提交一个待审查的 Pull Request。

### 改进文档

文档改进同样重要：

- **修正错误**：拼写、语法、技术错误
- **补充示例**：添加代码示例和使用场景
- **完善说明**：改进不清晰的描述
- **翻译工作**：帮助翻译文档（如需要）

文档位于 `docs/` 目录，使用 Markdown 格式编写。

## 开发环境设置

当前推荐的项目相关环境、CLI 与 AI 可用工具清单请查看：

- [开发环境能力清单](./contributor/development-environment.md)

### 前置要求

- **.NET SDK**：8.0、9.0 或 10.0
- **Git**：版本控制工具
- **IDE**（推荐）：
    - Visual Studio 2022+
    - JetBrains Rider
    - Visual Studio Code + C# Dev Kit

### 克隆仓库

```bash
# 克隆你 Fork 的仓库
git clone https://github.com/your-username/GFramework.git
cd GFramework

# 添加上游仓库
git remote add upstream https://github.com/GeWuYou/GFramework.git
```

### 安装依赖

```bash
# 恢复 NuGet 包
dotnet restore

# 恢复 .NET 本地工具
dotnet tool restore
```

### 构建项目

```bash
# 构建所有项目
dotnet build

# 构建特定配置
dotnet build -c Release
```

### 运行测试

```bash
# 运行所有测试
dotnet test

# 运行特定测试项目
dotnet test GFramework.Core.Tests
dotnet test GFramework.SourceGenerators.Tests

# 生成测试覆盖率报告
dotnet test --collect:"XPlat Code Coverage"
```

### 验证代码质量

项目使用 MegaLinter 进行代码质量检查：

```bash
# 本地运行 MegaLinter（需要 Docker）
docker run --rm -v $(pwd):/tmp/lint oxsecurity/megalinter:v9

# 或使用 CI 流程验证
git push origin your-branch
```

## 代码规范

### 命名规范

遵循 C# 标准命名约定：

- **类、接口、方法**：PascalCase
  ```csharp
  public class PlayerController { }
  public interface IEventBus { }
  public void ProcessInput() { }
  ```

- **私有字段**：_camelCase（下划线前缀）
  ```csharp
  private int _health;
  private readonly ILogger _logger;
  ```

- **参数、局部变量**：camelCase
  ```csharp
  public void SetHealth(int newHealth)
  {
      var oldHealth = _health;
      _health = newHealth;
  }
  ```

- **常量**：PascalCase
  ```csharp
  public const int MaxPlayers = 4;
  private const string DefaultName = "Player";
  ```

- **接口**：I 前缀
  ```csharp
  public interface IArchitecture { }
  public interface ICommand&lt;TInput&gt; { }
  ```

### 代码风格

- **缩进**：4 个空格（不使用 Tab）
- **大括号**：Allman 风格（独占一行）
  ```csharp
  if (condition)
  {
      DoSomething();
  }
  ```

- **using 指令**：文件顶部，按字母顺序排列
  ```csharp
  using System;
  using System.Collections.Generic;
  using GFramework.Core.Abstractions;
  ```

- **空行**：
    - 命名空间后空一行
    - 类成员之间空一行
    - 逻辑块之间适当空行

- **行长度**：建议不超过 120 字符

### 注释规范

#### XML 文档注释

所有公共 API 必须包含 XML 文档注释：

```csharp
/// &lt;summary&gt;
///     架构基类，提供系统、模型、工具等组件的注册与管理功能。
/// &lt;/summary&gt;
/// &lt;typeparam name="TModel"&gt;模型类型&lt;/typeparam&gt;
/// &lt;param name="configuration"&gt;架构配置&lt;/param&gt;
/// &lt;returns&gt;注册的模型实例&lt;/returns&gt;
/// &lt;exception cref="ArgumentNullException"&gt;当 model 为 null 时抛出&lt;/exception&gt;
public TModel RegisterModel&lt;TModel&gt;(TModel model) where TModel : IModel
{
    // 实现代码
}
```

#### 代码注释

- **何时添加注释**：
    - 复杂的算法逻辑
    - 非显而易见的设计决策
    - 临时解决方案（使用 TODO 或 HACK 标记）
    - 性能关键代码的优化说明

- **注释风格**：
  ```csharp
  // 单行注释使用双斜杠

  // 多行注释可以使用多个单行注释
  // 每行都以双斜杠开始

  /* 或使用块注释
   * 适用于较长的说明
   */
  ```

- **避免无用注释**：
  ```csharp
  // 不好：注释重复代码内容
  // 设置健康值为 100
  health = 100;

  // 好：解释为什么这样做
  // 初始化时设置满血，避免首次战斗时的边界情况
  health = MaxHealth;
  ```

### 设计原则

- **SOLID 原则**：遵循面向对象设计原则
- **依赖注入**：优先使用构造函数注入
- **接口隔离**：定义小而专注的接口
- **不可变性**：优先使用 `readonly` 和不可变类型
- **异步编程**：I/O 操作使用 `async`/`await`

## 提交规范

### Commit 消息格式

使用 Conventional Commits 规范：

```text
<type>(<scope>): <subject>

<body>

<footer>
```

#### Type（类型）

- **feat**：新功能
- **fix**：Bug 修复
- **docs**：文档更新
- **style**：代码格式调整（不影响功能）
- **refactor**：重构（不是新功能也不是修复）
- **perf**：性能优化
- **test**：添加或修改测试
- **chore**：构建过程或辅助工具的变动
- **ci**：CI 配置文件和脚本的变动

#### Scope（范围）

指明变更影响的模块：

- `core`：GFramework.Core
- `game`：GFramework.Game
- `godot`：GFramework.Godot
- `generators`：源码生成器
- `docs`：文档
- `tests`：测试

#### Subject（主题）

- 使用祈使句，现在时态："add" 而不是 "added" 或 "adds"
- 首字母小写
- 结尾不加句号
- 限制在 50 字符以内

#### Body（正文）

- 详细描述变更的动机和实现细节
- 与主题空一行
- 每行不超过 72 字符

#### Footer（页脚）

- 关联 Issue：`Closes #123`
- 破坏性变更：`BREAKING CHANGE: 描述`

#### 示例

```bash
# 简单提交
git commit -m "feat(core): add event priority support"

# 详细提交
git commit -m "fix(godot): resolve scene loading race condition

修复了在快速切换场景时可能出现的资源加载竞态条件。
通过引入场景加载锁机制，确保同一时间只有一个场景在加载。

Closes #456"

# 破坏性变更
git commit -m "refactor(core): change IArchitecture interface

BREAKING CHANGE: IArchitecture.Init() 现在返回 Task 而不是 void。
所有继承 Architecture 的类需要更新为异步初始化。

Migration guide: 将 Init() 改为 async Task Init()
"
```

### 分支策略

- **main**：主分支，保持稳定
- **feature/***：新功能分支
    - `feature/event-priority`
    - `feature/godot-ui-system`
- **fix/***：Bug 修复分支
    - `fix/memory-leak`
    - `fix/null-reference`
- **docs/***：文档更新分支
    - `docs/api-reference`
    - `docs/tutorial-update`
- **refactor/***：重构分支
    - `refactor/logging-system`

#### 分支命名规范

- 使用小写字母和连字符
- 简洁描述分支目的
- 避免使用个人名称

## 测试要求

### 单元测试

所有新功能和 Bug 修复都应包含单元测试：

```csharp
using Xunit;

namespace GFramework.Core.Tests.Events;

public class EventBusTests
{
    [Fact]
    public void Subscribe_ShouldReceiveEvent()
    {
        // Arrange
        var eventBus = new EventBus();
        var received = false;

        // Act
        eventBus.Subscribe&lt;TestEvent&gt;(e =&gt; received = true);
        eventBus.Publish(new TestEvent());

        // Assert
        Assert.True(received);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void Subscribe_MultipleEvents_ShouldReceiveAll(int count)
    {
        // 测试实现
    }
}
```

### 测试组织

- **测试项目**：`*.Tests` 后缀
- **测试类**：`*Tests` 后缀，与被测试类对应
- **测试方法**：`MethodName_Scenario_ExpectedResult` 格式
- **测试数据**：使用 `[Theory]` 和 `[InlineData]` 进行参数化测试

### 测试覆盖率

- **目标**：新代码覆盖率 &gt; 80%
- **关键路径**：核心功能覆盖率 &gt; 90%
- **边界情况**：测试异常情况和边界值

### 集成测试

对于涉及多个组件交互的功能，添加集成测试：

```csharp
public class ArchitectureIntegrationTests
{
    [Fact]
    public async Task Architecture_FullLifecycle_ShouldWork()
    {
        // Arrange
        var architecture = new TestArchitecture();

        // Act
        await architecture.InitAsync();
        var result = architecture.GetModel&lt;TestModel&gt;();
        await architecture.DestroyAsync();

        // Assert
        Assert.NotNull(result);
    }
}
```

### 性能测试

对性能敏感的代码，添加基准测试：

```csharp
using BenchmarkDotNet.Attributes;

[MemoryDiagnoser]
public class EventBusBenchmarks
{
    private EventBus _eventBus;

    [GlobalSetup]
    public void Setup()
    {
        _eventBus = new EventBus();
    }

    [Benchmark]
    public void Publish_1000Events()
    {
        for (int i = 0; i &lt; 1000; i++)
        {
            _eventBus.Publish(new TestEvent());
        }
    }
}
```

## 文档要求

### XML 注释

所有公共 API 必须包含完整的 XML 文档注释：

```csharp
/// &lt;summary&gt;
///     事件总线接口，提供事件的发布和订阅功能。
/// &lt;/summary&gt;
/// &lt;remarks&gt;
///     事件总线使用观察者模式实现，支持类型安全的事件分发。
///     所有订阅都是弱引用，避免内存泄漏。
/// &lt;/remarks&gt;
public interface IEventBus
{
    /// &lt;summary&gt;
    ///     订阅指定类型的事件。
    /// &lt;/summary&gt;
    /// &lt;typeparam name="TEvent"&gt;事件类型&lt;/typeparam&gt;
    /// &lt;param name="handler"&gt;事件处理器&lt;/param&gt;
    /// &lt;returns&gt;取消订阅的句柄&lt;/returns&gt;
    /// &lt;exception cref="ArgumentNullException"&gt;当 handler 为 null 时抛出&lt;/exception&gt;
    /// &lt;example&gt;
    /// &lt;code&gt;
    /// var unregister = eventBus.Subscribe&amp;lt;PlayerDiedEvent&amp;gt;(e =&amp;gt;
    /// {
    ///     Console.WriteLine($"Player {e.PlayerId} died");
    /// });
    ///
    /// // 取消订阅
    /// unregister.Dispose();
    /// &lt;/code&gt;
    /// &lt;/example&gt;
    IUnRegister Subscribe&lt;TEvent&gt;(Action&lt;TEvent&gt; handler) where TEvent : IEvent;
}
```

### Markdown 文档

#### 文档结构

```markdown
# 标题

简要介绍模块功能和用途。

## 核心概念

解释关键概念和术语。

## 快速开始

提供最简单的使用示例。

## 详细用法

### 功能 A

详细说明和代码示例。

### 功能 B

详细说明和代码示例。

## 最佳实践

推荐的使用模式和注意事项。

## 常见问题

FAQ 列表。

## 相关资源

链接到相关文档和示例。
```

#### 代码示例

- **完整性**：示例代码应该可以直接运行
- **注释**：关键步骤添加注释说明
- **格式化**：使用正确的语法高亮

```csharp
// 创建架构实例
var architecture = new GameArchitecture();

// 初始化架构
await architecture.InitAsync();

// 注册模型
var playerModel = architecture.GetModel&lt;PlayerModel&gt;();

// 发送命令
await architecture.SendCommandAsync(new AttackCommand
{
    TargetId = enemyId
});
```

#### 图表

使用 Mermaid 或 ASCII 图表说明复杂概念：

````markdown
\`\`\`mermaid
graph TD
    A[Controller] --&gt; B[Command]
    B --&gt; C[System]
    C --&gt; D[Model]
\`\`\`

````

## PR 流程

### 创建 PR

1. **确保分支最新**：
   ```bash
   git fetch upstream
   git rebase upstream/main
   ```

2. **推送到 Fork**：
   ```bash
   git push origin feature/your-feature
   ```

3. **创建 PR**：
    - 在 GitHub 上点击 "New Pull Request"
    - 选择 base: `main` ← compare: `your-branch`
    - 填写 PR 模板

### PR 模板

```markdown
## 变更说明

简要描述本 PR 的变更内容。

## 变更类型

- [ ] Bug 修复
- [ ] 新功能
- [ ] 破坏性变更
- [ ] 文档更新
- [ ] 性能优化
- [ ] 代码重构

## 相关 Issue

Closes #123

## 测试

描述如何测试这些变更：

- [ ] 添加了单元测试
- [ ] 添加了集成测试
- [ ] 手动测试通过

## 检查清单

- [ ] 代码遵循项目规范
- [ ] 添加了必要的注释
- [ ] 更新了相关文档
- [ ] 所有测试通过
- [ ] 没有引入新的警告

## 截图（如适用）

添加截图或 GIF 展示变更效果。

## 附加说明

其他需要说明的内容。
```

### 代码审查

PR 提交后，维护者会进行代码审查：

- **响应反馈**：及时回复审查意见
- **修改代码**：根据建议进行调整
- **讨论方案**：对有争议的地方进行讨论
- **保持耐心**：审查可能需要时间

#### 审查关注点

- **功能正确性**：代码是否实现了预期功能
- **代码质量**：是否遵循项目规范
- **测试覆盖**：是否有足够的测试
- **性能影响**：是否有性能问题
- **向后兼容**：是否破坏现有 API

### 合并流程

1. **通过 CI 检查**：所有自动化测试通过
2. **代码审查通过**：至少一位维护者批准
3. **解决冲突**：如有冲突需先解决
4. **合并方式**：
    - 功能分支：Squash and merge
    - 修复分支：Merge commit
    - 文档更新：Squash and merge

## 常见问题

### 如何同步上游更新？

```bash
# 获取上游更新
git fetch upstream

# 合并到本地 main
git checkout main
git merge upstream/main

# 更新你的 Fork
git push origin main

# 更新特性分支
git checkout feature/your-feature
git rebase main
```

### 如何解决合并冲突？

```bash
# 拉取最新代码
git fetch upstream
git rebase upstream/main

# 如果有冲突，手动解决后
git add .
git rebase --continue

# 强制推送（因为 rebase 改变了历史）
git push origin feature/your-feature --force-with-lease
```

### 提交了错误的代码怎么办？

```bash
# 修改最后一次提交
git add .
git commit --amend

# 或者撤销最后一次提交
git reset --soft HEAD~1
# 修改后重新提交
git add .
git commit -m "fix: correct implementation"
```

### 如何运行特定的测试？

```bash
# 运行单个测试类
dotnet test --filter "FullyQualifiedName~EventBusTests"

# 运行单个测试方法
dotnet test --filter "FullyQualifiedName~EventBusTests.Subscribe_ShouldReceiveEvent"

# 运行特定类别的测试
dotnet test --filter "Category=Integration"
```

### 如何生成文档？

```bash
# 安装 VitePress（如果还没安装）
cd docs
npm install

# 本地预览文档
npm run docs:dev

# 构建文档
npm run docs:build
```

### 代码审查需要多长时间？

- **简单修复**：通常 1-3 天
- **新功能**：可能需要 1-2 周
- **大型重构**：可能需要更长时间

请耐心等待，维护者会尽快审查。

### 我的 PR 被拒绝了怎么办？

不要气馁！被拒绝的原因可能是：

- 不符合项目方向
- 需要更多讨论
- 实现方式需要调整

你可以：

- 在 Issue 中讨论方案
- 根据反馈调整实现
- 寻求维护者的建议

### 如何成为维护者？

持续贡献高质量的代码和文档，积极参与社区讨论，帮助其他贡献者。维护者会邀请活跃且负责任的贡献者加入维护团队。

## 获取帮助

如果你在贡献过程中遇到问题：

- **GitHub Issues**：使用对应模板提问、报告问题或提出建议
- **GitHub Discussions**：参与讨论
- **代码注释**：查看现有代码的注释和文档

## 致谢

感谢所有为 GFramework 做出贡献的开发者！你们的努力让这个项目变得更好。

## 许可证

通过向本项目提交代码，你同意你的贡献将在 Apache License 2.0 下发布。
