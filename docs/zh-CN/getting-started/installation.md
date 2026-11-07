---
title: 安装配置
description: 说明 GFramework 各运行时与 source generator 包的安装选择和配置方式。
---

# 安装配置

GFramework 采用按模块拆分的安装路径。先确认你要接入的是哪一层运行时、抽象层或源码生成器，再决定安装组合，会比直接把所有包一次性带进来更稳妥。

## 包选择说明

GFramework 采用模块化设计，不同包提供不同的功能：

| 包名 | 说明 | 适用场景 |
| --- | --- | --- |
| `GeWuYou.GFramework` | 聚合元包 | 快速试用、原型开发，或先起一个最小运行时骨架 |
| `GeWuYou.GFramework.Core` | Core 运行时 | Architecture 采用起点，不含 CQRS 依赖 |
| `GeWuYou.GFramework.Core.Abstractions` | Core 抽象契约 | 面向接口开发、测试替身、插件化拆分 |
| `GeWuYou.GFramework.Cqrs` | CQRS runtime | request / notification / stream 分发与处理器注册，按需安装 |
| `GeWuYou.GFramework.Cqrs.Abstractions` | CQRS 抽象契约 | 共享 request、handler 与 pipeline 契约 |
| `GeWuYou.GFramework.Game` | Game 运行时 | 配置、存储、设置、Scene、UI 等游戏层能力 |
| `GeWuYou.GFramework.Game.Abstractions` | Game 抽象契约 | 共享 `IConfigRegistry`、`ISceneRouter`、`IUiService` 等接口 |
| `GeWuYou.GFramework.Godot` | Godot 集成 | Godot 项目的运行时接线、节点扩展与宿主适配 |
| `GeWuYou.GFramework.Ecs.Arch` | Arch ECS 运行时 | 需要 `UseArch(...)`、默认 `World` 注册与系统桥接 |
| `GeWuYou.GFramework.Ecs.Arch.Abstractions` | Arch ECS 抽象契约 | 只共享 ECS 模块接口、配置对象与宿主循环边界 |
| `GeWuYou.GFramework.Core.SourceGenerators` | Core 源码生成器 | `[Log]`、`[ContextAware]`、架构注入等 |
| `GeWuYou.GFramework.Game.SourceGenerators` | Game 源码生成器 | 配置 schema、配置类型、表包装与注册辅助生成 |
| `GeWuYou.GFramework.Godot.SourceGenerators` | Godot 源码生成器 | Godot 节点、UI、项目元数据生成 |
| `GeWuYou.GFramework.Cqrs.SourceGenerators` | CQRS 源码生成器 | 处理器注册表生成 |

当前 NuGet 发布按模块拆分 source generator 包，不存在 `GeWuYou.GFramework.SourceGenerators` 聚合包。

`GeWuYou.GFramework` 当前是聚合元包，只聚合：

- `GFramework.Core`
- `GFramework.Game`

Core 不依赖 CQRS。需要 request / notification / stream 消息模型的项目显式安装 `GeWuYou.GFramework.Cqrs`，并在架构中安装 runtime 模块（见 [CQRS 运行时](../core/cqrs.md)）。CQRS-only 应用也可以只使用 `GeWuYou.GFramework.Cqrs` 在标准 DI 上运行，不引入 Architecture。

## 推荐组合

- 最小运行时：`GeWuYou.GFramework.Core` + `GeWuYou.GFramework.Core.Abstractions`
- 消息模型：追加 `GeWuYou.GFramework.Cqrs` + `GeWuYou.GFramework.Cqrs.Abstractions`；纯消息契约项目仅引用后者
- Game 配置工作流：在 Core 基础上追加 `GeWuYou.GFramework.Game` + `GeWuYou.GFramework.Game.Abstractions` + `GeWuYou.GFramework.Game.SourceGenerators`
- Godot 项目：在所需运行时基础上追加 `GeWuYou.GFramework.Godot`，需要生成器辅助时再加 `GeWuYou.GFramework.Godot.SourceGenerators`
- Arch ECS：直接安装 `GeWuYou.GFramework.Ecs.Arch`；如果只想共享宿主循环或接口边界，可改为 `GeWuYou.GFramework.Ecs.Arch.Abstractions`

如果你准备采用 AI-First 配置工作流，可以继续阅读 [游戏内容配置系统](../game/config-system.md) 与 [VS Code 配置工具](../game/config-tool.md)。
接入时建议先按 Runtime + Source Generator 的共享 schema 子集设计配置模型，再把 `VS Code` 工具当作编辑辅助层来使用，而不是反过来以工具界面可编辑的 shape 作为正式契约。
尤其需要尽早知道两个当前边界：对象闭合只收口到 `additionalProperties: false`，而 `oneOf` / `anyOf` 会被直接拒绝。若配置模型超出这组共享边界，优先回到 raw YAML 与 schema 本体调整结构，而不是把差异理解成工具遗漏能力。

## 安装方式

### 1. 使用 .NET CLI（推荐）

```bash
# 核心能力（推荐最小起步）
dotnet add package GeWuYou.GFramework.Core
dotnet add package GeWuYou.GFramework.Core.Abstractions

# 消息模型（使用 CQRS request / notification / stream 时安装）
dotnet add package GeWuYou.GFramework.Cqrs
dotnet add package GeWuYou.GFramework.Cqrs.Abstractions

# 游戏扩展
dotnet add package GeWuYou.GFramework.Game
dotnet add package GeWuYou.GFramework.Game.Abstractions

# Arch ECS
dotnet add package GeWuYou.GFramework.Ecs.Arch
dotnet add package GeWuYou.GFramework.Ecs.Arch.Abstractions

# Godot 集成（仅 Godot 项目需要）
dotnet add package GeWuYou.GFramework.Godot

# Core 侧源码生成器（[Log] / [ContextAware] / [GetSystem] 等）
dotnet add package GeWuYou.GFramework.Core.SourceGenerators

# Game 配置 schema 生成器
dotnet add package GeWuYou.GFramework.Game.SourceGenerators

# Godot 生成器（仅 Godot 项目需要）
dotnet add package GeWuYou.GFramework.Godot.SourceGenerators

# CQRS 处理器注册生成器（仅使用 CQRS source generator 时需要）
dotnet add package GeWuYou.GFramework.Cqrs.SourceGenerators
```

### 2. 使用 PackageReference

在您的 `.csproj` 文件中添加：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- 核心框架 -->
    <PackageReference Include="GeWuYou.GFramework.Core" Version="1.0.0" />
    <PackageReference Include="GeWuYou.GFramework.Core.Abstractions" Version="1.0.0" />

    <!-- CQRS runtime（使用消息模型时安装） -->
    <PackageReference Include="GeWuYou.GFramework.Cqrs" Version="1.0.0" />
    <PackageReference Include="GeWuYou.GFramework.Cqrs.Abstractions" Version="1.0.0" />
    
    <!-- 游戏模块 -->
    <PackageReference Include="GeWuYou.GFramework.Game" Version="1.0.0" />
    <PackageReference Include="GeWuYou.GFramework.Game.Abstractions" Version="1.0.0" />

    <!-- Arch ECS -->
    <PackageReference Include="GeWuYou.GFramework.Ecs.Arch" Version="1.0.0" />
    <PackageReference Include="GeWuYou.GFramework.Ecs.Arch.Abstractions" Version="1.0.0" />
    
    <!-- Godot 集成 -->
    <PackageReference Include="GeWuYou.GFramework.Godot" Version="1.0.0" />
    
    <!-- 按场景选择的源码生成器 -->
    <PackageReference Include="GeWuYou.GFramework.Core.SourceGenerators" Version="1.0.0"
                      PrivateAssets="all" ExcludeAssets="runtime" />
    <PackageReference Include="GeWuYou.GFramework.Game.SourceGenerators" Version="1.0.0"
                      PrivateAssets="all" ExcludeAssets="runtime" />
    <PackageReference Include="GeWuYou.GFramework.Godot.SourceGenerators" Version="1.0.0"
                      PrivateAssets="all" ExcludeAssets="runtime" />
    <PackageReference Include="GeWuYou.GFramework.Cqrs.SourceGenerators" Version="1.0.0"
                      PrivateAssets="all" ExcludeAssets="runtime" />
  </ItemGroup>
</Project>
```

### 3. 使用 NuGet Package Manager

在 Visual Studio 中：

1. 右键点击项目 → 管理 NuGet 程序包
2. 搜索 `GeWuYou.GFramework`
3. 选择需要的包进行安装

## 环境要求

### 运行时要求

- **.NET 8.0、9.0 或 10.0**
- **Godot 4.6.2**（仅 Godot 项目）

### 开发工具

- Visual Studio 2022 或 VS Code
- .NET 8 SDK 或更高版本
- Godot 4.6.2（可选，仅 Godot 项目需要）

## 项目配置

### 1. 基础配置

如果你通过 NuGet 包使用 GFramework，并且希望自动导入已安装模块的推荐命名空间，可以在项目文件中显式开启：

```xml
<PropertyGroup>
  <EnableGFrameworkGlobalUsings>true</EnableGFrameworkGlobalUsings>
</PropertyGroup>
```

启用后，当前项目已引用的 GFramework 运行时模块会通过 `buildTransitive` 自动注入对应命名空间。

如果你想排除局部导入，可以继续在项目文件中添加排除项：

```xml
<ItemGroup>
  <GFrameworkExcludedUsing Include="GFramework.Core.Environment" />
  <GFrameworkExcludedUsing Include="GFramework.Godot.Extensions" />
</ItemGroup>
```

如果你使用的是本地 `ProjectReference`，或者希望完全手动控制导入范围，仍然可以继续维护自己的 `GlobalUsings.cs` 文件。

### 2. Godot 项目配置

如果使用 Godot 集成，需要在项目设置中启用 C# 支持：

1. 在 Godot 编辑器中打开项目设置
2. 导航到 `Mono` → `Editor Settings`
3. 确保启用了 C# 支持

### 3. 源码生成器配置

源码生成器会自动工作，无需额外配置。如果需要自定义生成器行为，可以在项目文件中添加：

```xml
<PropertyGroup>
  <GFrameworkLogLevel>Debug</GFrameworkLogLevel>
  <GFrameworkGenerateEnums>true</GFrameworkGenerateEnums>
</PropertyGroup>
```

## 验证安装

创建一个简单的测试来验证安装是否成功：

```csharp
using GFramework.Core.Architectures;
using GFramework.Core.Model;
using GFramework.Core.Property;

// 定义简单的架构
public class TestArchitecture : Architecture
{
    protected override void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
    {
        // 注册一个简单的模型
        RegisterModel(new TestModel());
    }
}

public class TestModel : AbstractModel
{
    public BindableProperty<string> Message { get; } = new("Hello GFramework!");

    protected override void OnInit()
    {
    }
}

// 测试代码
var architecture = new TestArchitecture();
architecture.Initialize();

var model = architecture.GetModel<TestModel>();
Console.WriteLine(model.Message.Value); // 输出: Hello GFramework!
```

## 常见问题

### 1. 包版本冲突

如果遇到版本冲突，建议：

```bash
dotnet restore --force
dotnet clean
dotnet build
```

### 2. Godot 集成问题

确保：

- 项目环境与当前文档保持在 Godot 4.6.2 基线
- 已正确安装 Godot C# 模板
- 项目引用了正确的 Godot 包

### 3. 源码生成器不工作

检查：

- 确保安装了与你正在使用的特性对应的拆分生成器包，例如：
  `GeWuYou.GFramework.Core.SourceGenerators`、`GeWuYou.GFramework.Game.SourceGenerators`、
  `GeWuYou.GFramework.Godot.SourceGenerators` 或 `GeWuYou.GFramework.Cqrs.SourceGenerators`
- 重启 IDE
- 清理并重新构建项目
