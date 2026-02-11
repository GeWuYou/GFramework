---
prev:
  text: '教程首页'
  link: './index'
next:
  text: '项目创建与初始化'
  link: './02-project-setup'
---

# 第 1 章：环境准备

在开始使用 GFramework 之前，我们需要配置好开发环境。本章将指导你完成所有必要工具的安装和验证。

## 系统要求

### 操作系统

- **Windows**：Windows 10 或更高版本（推荐 Windows 11）
- **macOS**：macOS 10.15 Catalina 或更高版本
- **Linux**：主流发行版（Ubuntu 20.04+, Fedora 35+, Arch Linux 等）

### 硬件要求

- **CPU**：双核或更高（推荐四核）
- **内存**：8 GB RAM（推荐 16 GB）
- **存储**：至少 5 GB 可用空间
- **显卡**：支持 OpenGL 3.3 或更高

## 安装 .NET SDK

GFramework 基于 .NET 6.0+，需要先安装 .NET SDK。

### Windows

1. 访问 [.NET 官方下载页](https://dotnet.microsoft.com/download)
2. 下载 **.NET 6.0 SDK** 或更高版本
3. 运行安装程序，按照提示完成安装
4. 安装完成后，打开命令提示符或 PowerShell

### macOS

**方式一：使用官方安装包**

```bash
# 访问官网下载 .pkg 安装包
# https://dotnet.microsoft.com/download
```

**方式二：使用 Homebrew**

```bash
brew install --cask dotnet-sdk
```

### Linux

**Ubuntu/Debian**

```bash
# 添加 Microsoft 包仓库
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# 安装 .NET SDK
sudo apt-get update
sudo apt-get install -y dotnet-sdk-6.0
```

**Fedora**

```bash
sudo dnf install dotnet-sdk-6.0
```

**Arch Linux**

```bash
sudo pacman -S dotnet-sdk
```

### 验证 .NET 安装

打开终端或命令提示符，运行：

```bash
dotnet --version
```

你应该看到类似以下输出：

```
6.0.428
```

或更高版本（如 7.0.x, 8.0.x）。

::: tip 提示
建议安装最新的 LTS（长期支持）版本。本教程兼容 .NET 6.0 及以上所有版本。
:::

## 安装 Godot 引擎

GFramework 的 Godot 集成需要 **Godot 4.5.1** 或更高版本，并且必须是 **.NET 版本**（不是标准版）。

### 下载 Godot

1. 访问 [Godot 官方下载页](https://godotengine.org/download)
2. 选择 **Godot 4.5.x - .NET** 版本
    - **Windows**: 下载 `Godot_v4.5.x-stable_mono_win64.zip`
    - **macOS**: 下载 `Godot_v4.5.x-stable_mono_macos.universal.zip`
    - **Linux**: 下载 `Godot_v4.5.x-stable_mono_linux_x86_64.zip`

### 安装 Godot

**Windows**

1. 解压下载的 ZIP 文件
2. 将文件夹移动到合适的位置（如 `C:\Godot\`）
3. 双击 `Godot_v4.5.x-stable_mono_win64.exe` 启动

**macOS**

1. 解压下载的 ZIP 文件
2. 将 `Godot.app` 拖动到 `应用程序` 文件夹
3. 首次运行时，右键点击 → 打开（绕过安全检查）

**Linux**

1. 解压下载的 ZIP 文件
2. 添加可执行权限：

```bash
chmod +x Godot_v4.5.x-stable_mono_linux.x86_64
```

3. 运行：

```bash
./Godot_v4.5.x-stable_mono_linux.x86_64
```

### 配置 Godot .NET 支持

首次启动 Godot 时，编辑器会自动检测 .NET SDK。

1. 打开 Godot 编辑器
2. 点击菜单 **编辑器 → 编辑器设置**
3. 在左侧导航中找到 **Mono → Builds**
4. 确认 **Build Tool** 设置为 `dotnet CLI`

如果 Godot 提示找不到 .NET SDK，确保：

- .NET SDK 已正确安装
- 终端中能运行 `dotnet --version`
- 重启 Godot 编辑器

::: warning 注意
必须下载 **.NET (Mono) 版本** 的 Godot，标准版不支持 C# 开发。文件名中应包含 `mono` 字样。
:::

## 安装 IDE（可选但推荐）

虽然可以使用任何文本编辑器，但专业的 IDE 能大幅提升开发效率。

### Visual Studio 2022（推荐 Windows 用户）

1. 下载 [Visual Studio 2022 Community](https://visualstudio.microsoft.com/downloads/)（免费）
2. 安装时选择以下工作负载：
    - **.NET 桌面开发**
    - **游戏开发 → 使用 Unity 的游戏开发**（可选，提供更好的游戏开发工具）
3. 确保勾选 **.NET 6.0 SDK**

### JetBrains Rider（推荐跨平台用户）

1. 下载 [JetBrains Rider](https://www.jetbrains.com/rider/)
2. 安装并激活（提供 30 天试用，或使用社区许可证）
3. 首次启动时，Rider 会自动检测 .NET SDK

**优势**：

- 跨平台支持（Windows, macOS, Linux）
- 出色的代码补全和重构工具
- 原生支持 Godot 项目

### Visual Studio Code

1. 下载 [Visual Studio Code](https://code.visualstudio.com/)
2. 安装以下扩展：
    - **C# (Microsoft)**
    - **C# Dev Kit**
    - **godot-tools**

::: tip IDE 选择建议

- **Windows 用户**：Visual Studio 2022 或 Rider
- **macOS/Linux 用户**：Rider 或 VS Code
- **轻量级需求**：VS Code
  :::

## 环境验证

完成所有安装后，让我们创建一个测试项目来验证环境。

### 1. 创建测试控制台项目

打开终端，运行：

```bash
# 创建新的控制台项目
dotnet new console -n GFrameworkTest
cd GFrameworkTest

# 添加 GFramework 核心包
dotnet add package GeWuYou.GFramework.Core

# 编译测试
dotnet build
```

如果编译成功，说明 .NET 环境配置正确。

### 2. 创建测试 Godot 项目

1. 打开 Godot 编辑器
2. 点击 **新建项目**
3. 选择项目路径，命名为 `GodotTest`
4. **渲染器** 选择 **Forward+** 或 **Mobile**
5. 点击 **创建并编辑**

项目创建后，Godot 会提示初始化 C# 项目：

![初始化 C# 项目](../assets/basic/image-20260211210657387.png)

点击 **创建 C# 解决方案**，Godot 会自动生成 `.csproj` 文件。

### 3. 验证 Godot C# 支持

在 Godot 编辑器中：

1. 右键点击 **文件系统** 面板的根目录
2. 选择 **创建新脚本**
3. **语言** 选择 **C#**
4. 脚本名称输入 `TestScript.cs`
5. 点击 **创建**

如果脚本成功创建并打开外部编辑器，说明 Godot 和 .NET 集成正常。

### 4. 完整验证脚本

将以下代码粘贴到 `TestScript.cs`：

```csharp
using Godot;
using System;

public partial class TestScript : Node
{
    public override void _Ready()
    {
        GD.Print("✅ Godot + .NET 环境配置成功！");
        GD.Print($"✅ .NET 版本: {Environment.Version}");
        GD.Print("✅ 准备开始使用 GFramework！");
    }
}
```

在场景中添加一个 Node 节点，附加这个脚本，然后点击 **运行场景**（F6）。

如果在输出面板看到：

```
✅ Godot + .NET 环境配置成功！
✅ .NET 版本: 6.0.x
✅ 准备开始使用 GFramework！
```

恭喜！你的环境已经完全配置好了！

## 常见问题排查

### Godot 找不到 .NET SDK

**症状**：Godot 提示"无法找到 .NET SDK"

**解决方案**：

1. 确认终端中能运行 `dotnet --version`
2. 重启 Godot 编辑器
3. 检查 **编辑器设置 → Mono → Builds** 中的 .NET 路径
4. 尝试手动指定 .NET SDK 路径

### MSBuild 错误

**症状**：编译时提示 MSBuild 相关错误

**解决方案**：

```bash
# 清理并重新生成项目
dotnet clean
dotnet build
```

### macOS 安全限制

**症状**：macOS 阻止 Godot 运行

**解决方案**：

1. 右键点击 `Godot.app`
2. 选择 **打开**（不是双击）
3. 在弹出的对话框中点击 **打开**
4. 后续可以正常双击启动

### Linux 缺少依赖

**症状**：Linux 下 Godot 无法启动

**解决方案**：

```bash
# Ubuntu/Debian
sudo apt-get install libxcursor1 libxinerama1 libxi6 libxrandr2 libgl1

# Fedora
sudo dnf install libXcursor libXinerama libXi libXrandr mesa-libGL
```

## 下一步

环境配置完成！现在你已经准备好开始使用 GFramework 了。

在下一章中，我们将：

- 创建一个新的 Godot 项目
- 引入 GFramework NuGet 包
- 搭建基础的项目架构
- 创建游戏入口点

👉 [第 2 章：项目创建与初始化](./02-project-setup.md)

---

::: details 本章检查清单

- [ ] .NET SDK 已安装并可以运行 `dotnet --version`
- [ ] Godot 4.5.1+ .NET 版本已安装
- [ ] IDE 已安装（Visual Studio / Rider / VS Code）
- [ ] 测试项目成功编译
- [ ] Godot 能够创建和运行 C# 脚本
  :::
