# 安装配置

GFramework 提供多种安装方式，您可以根据项目需求选择合适的包进行安装。

## 包选择说明

GFramework 采用模块化设计，不同包提供不同的功能：

| 包名                                    | 说明      | 适用场景      |
|---------------------------------------|---------|-----------|
| `GeWuYou.GFramework`                  | 聚合元包    | 快速试用、原型开发 |
| `GeWuYou.GFramework.Core`             | 核心框架    | 生产项目推荐    |
| `GeWuYou.GFramework.Game`             | 游戏模块    | 需要游戏特定功能  |
| `GeWuYou.GFramework.Godot`            | Godot集成 | Godot项目必需 |
| `GeWuYou.GFramework.SourceGenerators` | 源码生成器   | 推荐安装      |

## 安装方式

### 1. 使用 .NET CLI（推荐）

```bash
# 核心能力（推荐最小起步）
dotnet add package GeWuYou.GFramework.Core
dotnet add package GeWuYou.GFramework.Core.Abstractions

# 游戏扩展
dotnet add package GeWuYou.GFramework.Game
dotnet add package GeWuYou.GFramework.Game.Abstractions

# Godot 集成（仅 Godot 项目需要）
dotnet add package GeWuYou.GFramework.Godot

# 源码生成器（可选，但推荐）
dotnet add package GeWuYou.GFramework.SourceGenerators
```

### 2. 使用 PackageReference

在您的 `.csproj` 文件中添加：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- 核心框架 -->
    <PackageReference Include="GeWuYou.GFramework.Core" Version="1.0.0" />
    <PackageReference Include="GeWuYou.GFramework.Core.Abstractions" Version="1.0.0" />
    
    <!-- 游戏模块 -->
    <PackageReference Include="GeWuYou.GFramework.Game" Version="1.0.0" />
    <PackageReference Include="GeWuYou.GFramework.Game.Abstractions" Version="1.0.0" />
    
    <!-- Godot 集成 -->
    <PackageReference Include="GeWuYou.GFramework.Godot" Version="1.0.0" />
    
    <!-- 源码生成器 -->
    <PackageReference Include="GeWuYou.GFramework.SourceGenerators" Version="1.0.0" 
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

- **.NET 6.0** 或更高版本
- **Godot 4.5+**（仅 Godot 项目）

### 开发工具

- Visual Studio 2022 或 VS Code
- .NET 6.0 SDK
- Godot 4.5+（可选，仅 Godot 项目需要）

## 项目配置

### 1. 基础配置

创建 `GlobalUsings.cs` 文件：

```csharp
global using GFramework.Core;
global using GFramework.Core.architecture;
global using GFramework.Core.command;
global using GFramework.Core.events;
global using GFramework.Core.model;
global using GFramework.Core.property;
global using GFramework.Core.system;
global using GFramework.Core.utility;
```

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
using GFramework.Core.architecture;

// 定义简单的架构
public class TestArchitecture : Architecture
{
    protected override void Init()
    {
        // 注册一个简单的模型
        RegisterModel(new TestModel());
    }
}

public class TestModel : AbstractModel
{
    public BindableProperty<string> Message { get; } = new("Hello GFramework!");
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

- Godot 版本 >= 4.5
- 已正确安装 Godot C# 模板
- 项目引用了正确的 Godot 包

### 3. 源码生成器不工作

检查：

- 确保安装了 `GeWuYou.GFramework.SourceGenerators`
- 重启 IDE
- 清理并重新构建项目

## 下一步

安装完成后，建议：

1. [快速开始](./getting-started/quick-start) - 构建第一个应用
2. [架构概览](./getting-started/architecture-overview) - 了解核心概念
3. [Core 模块文档](./core) - 深入学习核心功能