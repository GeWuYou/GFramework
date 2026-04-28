---
title: 实现暂停系统
description: 学习如何使用暂停系统实现多层暂停管理和游戏流程控制
---

# 实现暂停系统

## 学习目标

完成本教程后，你将能够：

- 理解暂停系统的设计原理和应用场景
- 实现基本的游戏暂停和恢复功能
- 使用暂停组实现分层暂停控制
- 管理嵌套暂停状态（暂停栈）
- 实现自定义暂停处理器
- 在实际游戏场景中集成暂停系统

## 前置条件

- 已安装 GFramework.Core NuGet 包
- 了解 C# 基础语法和接口实现
- 阅读过[快速开始](/zh-CN/getting-started/quick-start.md)
- 了解[架构组件](/zh-CN/core/architecture.md)基础

## 步骤 1：注册暂停管理器

首先，在架构中注册暂停栈管理器作为 Utility。

```csharp
using GFramework.Core.Architecture;
using GFramework.Core.Abstractions.Pause;
using GFramework.Core.Pause;

namespace MyGame
{
    public class GameArchitecture : Architecture
    {
        public static IArchitecture Interface { get; private set; }

        protected override void OnInitialize()
        {
            Interface = this;

            // 注册暂停栈管理器
            RegisterUtility<IPauseStackManager>(new PauseStackManager());

            Console.WriteLine("暂停系统初始化完成");
        }
    }
}
```

**代码说明**：

- `PauseStackManager` 是暂停系统的核心管理器
- 注册为 `IPauseStackManager` 接口，方便在架构中访问
- 管理器支持多组暂停、嵌套暂停和状态通知

## 步骤 2：实现基本暂停功能

创建一个简单的游戏系统，实现基本的暂停和恢复功能。

```csharp
using GFramework.Core.Abstractions.Pause;
using GFramework.Core.System;
using GFramework.Core.Extensions;

namespace MyGame.Systems
{
    /// <summary>
    /// 游戏逻辑系统
    /// </summary>
    public class GameLogicSystem : AbstractSystem
    {
        private PauseToken? _currentPauseToken;
        private bool _isRunning;

        protected override void OnInit()
        {
            _isRunning = true;
            Console.WriteLine("游戏逻辑系统初始化");
        }

        /// <summary>
        /// 暂停游戏
        /// </summary>
        public void PauseGame()
        {
            // 获取暂停管理器
            var pauseManager = this.GetUtility<IPauseStackManager>();

            // 推入暂停请求，获取令牌
            _currentPauseToken = pauseManager.Push("游戏暂停");

            Console.WriteLine("游戏已暂停");
        }

        /// <summary>
        /// 恢复游戏
        /// </summary>
        public void ResumeGame()
        {
            if (_currentPauseToken == null)
            {
                Console.WriteLine("游戏未暂停");
                return;
            }

            // 获取暂停管理器
            var pauseManager = this.GetUtility<IPauseStackManager>();

            // 弹出暂停请求
            bool success = pauseManager.Pop(_currentPauseToken.Value);

            if (success)
            {
                _currentPauseToken = null;
                Console.WriteLine("游戏已恢复");
            }
            else
            {
                Console.WriteLine("恢复失败：无效的暂停令牌");
            }
        }

        /// <summary>
        /// 检查游戏是否暂停
        /// </summary>
        public bool IsPaused()
        {
            var pauseManager = this.GetUtility<IPauseStackManager>();
            return pauseManager.IsPaused();
        }

        /// <summary>
        /// 游戏更新（每帧调用）
        /// </summary>
        public void Update(double deltaTime)
        {
            // 如果游戏暂停，跳过更新
            if (IsPaused())
            {
                return;
            }

            // 执行游戏逻辑
            ProcessGameLogic(deltaTime);
        }

        private void ProcessGameLogic(double deltaTime)
        {
            // 游戏逻辑处理
            Console.WriteLine($"游戏运行中... (DeltaTime: {deltaTime:F3}s)");
        }
    }
}
```

**代码说明**：

- 使用 `Push` 方法推入暂停请求，返回 `PauseToken`
- 使用 `Pop` 方法弹出暂停请求，需要提供令牌
- 使用 `IsPaused` 方法检查当前是否暂停
- 在游戏更新循环中检查暂停状态

## 步骤 3：实现分组暂停

使用暂停组实现更精细的暂停控制，例如只暂停游戏逻辑但不暂停 UI。

```csharp
using GFramework.Core.Abstractions.Pause;
using GFramework.Core.System;
using GFramework.Core.Extensions;

namespace MyGame.Systems
{
    /// <summary>
    /// 高级游戏系统，支持分组暂停
    /// </summary>
    public class AdvancedGameSystem : AbstractSystem
    {
        /// <summary>
        /// 打开菜单（只暂停游戏逻辑）
        /// </summary>
        public PauseToken OpenMenu()
        {
            var pauseManager = this.GetUtility<IPauseStackManager>();

            // 只暂停游戏逻辑组，UI 仍然可以交互
            var token = pauseManager.Push("菜单打开", PauseGroup.Gameplay);

            Console.WriteLine("菜单已打开（游戏逻辑暂停，UI 正常）");
            return token;
        }

        /// <summary>
        /// 关闭菜单
        /// </summary>
        public void CloseMenu(PauseToken token)
        {
            var pauseManager = this.GetUtility<IPauseStackManager>();
            pauseManager.Pop(token);

            Console.WriteLine("菜单已关闭（游戏逻辑恢复）");
        }

        /// <summary>
        /// 打开对话框（全局暂停）
        /// </summary>
        public PauseToken OpenDialog()
        {
            var pauseManager = this.GetUtility<IPauseStackManager>();

            // 全局暂停，影响所有系统
            var token = pauseManager.Push("对话框打开", PauseGroup.Global);

            Console.WriteLine("对话框已打开（全局暂停）");
            return token;
        }

        /// <summary>
        /// 关闭对话框
        /// </summary>
        public void CloseDialog(PauseToken token)
        {
            var pauseManager = this.GetUtility<IPauseStackManager>();
            pauseManager.Pop(token);

            Console.WriteLine("对话框已关闭（全局恢复）");
        }

        /// <summary>
        /// 静音音频
        /// </summary>
        public PauseToken MuteAudio()
        {
            var pauseManager = this.GetUtility<IPauseStackManager>();

            // 只暂停音频组
            var token = pauseManager.Push("音频静音", PauseGroup.Audio);

            Console.WriteLine("音频已静音");
            return token;
        }

        /// <summary>
        /// 取消静音
        /// </summary>
        public void UnmuteAudio(PauseToken token)
        {
            var pauseManager = this.GetUtility<IPauseStackManager>();
            pauseManager.Pop(token);

            Console.WriteLine("音频已恢复");
        }

        /// <summary>
        /// 游戏逻辑更新
        /// </summary>
        public void UpdateGameplay(double deltaTime)
        {
            var pauseManager = this.GetUtility<IPauseStackManager>();

            // 检查游戏逻辑组是否暂停
            if (pauseManager.IsPaused(PauseGroup.Gameplay))
            {
                Console.WriteLine("游戏逻辑暂停中...");
                return;
            }

            Console.WriteLine($"游戏逻辑更新: {deltaTime:F3}s");
        }

        /// <summary>
        /// UI 更新
        /// </summary>
        public void UpdateUI(double deltaTime)
        {
            var pauseManager = this.GetUtility<IPauseStackManager>();

            // UI 通常不受 Gameplay 组暂停影响
            // 但会受 Global 组暂停影响
            if (pauseManager.IsPaused(PauseGroup.Global))
            {
                Console.WriteLine("UI 暂停中（全局暂停）...");
                return;
            }

            Console.WriteLine($"UI 更新: {deltaTime:F3}s");
        }

        /// <summary>
        /// 音频更新
        /// </summary>
        public void UpdateAudio(double deltaTime)
        {
            var pauseManager = this.GetUtility<IPauseStackManager>();

            // 检查音频组是否暂停
            if (pauseManager.IsPaused(PauseGroup.Audio))
            {
                return; // 静音状态，不播放音频
            }

            Console.WriteLine($"音频播放: {deltaTime:F3}s");
        }
    }
}
```

**代码说明**：

- `PauseGroup.Global` - 全局暂停，影响所有系统
- `PauseGroup.Gameplay` - 游戏逻辑暂停，不影响 UI
- `PauseGroup.Audio` - 音频暂停，用于静音功能
- 不同组的暂停状态相互独立
- 可以根据需要检查特定组的暂停状态

## 步骤 4：实现暂停栈管理

使用暂停栈处理嵌套暂停场景，例如在暂停菜单中打开设置对话框。

```csharp
using GFramework.Core.Abstractions.Pause;
using GFramework.Core.Abstractions.Controller;
using GFramework.Core.SourceGenerators.Abstractions.Rule;

namespace MyGame.Controllers
{
    /// <summary>
    /// 暂停控制器，管理复杂的暂停场景
    /// </summary>
    [ContextAware]
    public partial class PauseController : IController
    {
        /// <summary>
        /// 显示暂停状态信息
        /// </summary>
        public void ShowPauseStatus()
        {
            var pauseManager = this.GetUtility<IPauseStackManager>();

            Console.WriteLine("\n=== 暂停状态 ===");
            Console.WriteLine($"全局暂停: {pauseManager.IsPaused(PauseGroup.Global)}");
            Console.WriteLine($"游戏逻辑暂停: {pauseManager.IsPaused(PauseGroup.Gameplay)}");
            Console.WriteLine($"音频暂停: {pauseManager.IsPaused(PauseGroup.Audio)}");

            // 显示暂停深度
            int globalDepth = pauseManager.GetPauseDepth(PauseGroup.Global);
            Console.WriteLine($"全局暂停深度: {globalDepth}");

            // 显示暂停原因
            if (globalDepth > 0)
            {
                var reasons = pauseManager.GetPauseReasons(PauseGroup.Global);
                Console.WriteLine("暂停原因:");
                foreach (var reason in reasons)
                {
                    Console.WriteLine($"  - {reason}");
                }
            }

            Console.WriteLine("================\n");
        }

        /// <summary>
        /// 测试嵌套暂停
        /// </summary>
        public void TestNestedPause()
        {
            var pauseManager = this.GetUtility<IPauseStackManager>();

            Console.WriteLine("--- 测试嵌套暂停 ---\n");

            // 第一层：打开暂停菜单
            Console.WriteLine("1. 打开暂停菜单");
            var pauseMenuToken = pauseManager.Push("暂停菜单", PauseGroup.Global);
            ShowPauseStatus();

            // 第二层：打开设置对话框
            Console.WriteLine("2. 在暂停菜单中打开设置");
            var settingsToken = pauseManager.Push("设置对话框", PauseGroup.Global);
            ShowPauseStatus();

            // 第三层：打开确认对话框
            Console.WriteLine("3. 在设置中打开确认对话框");
            var confirmToken = pauseManager.Push("确认对话框", PauseGroup.Global);
            ShowPauseStatus();

            // 关闭确认对话框
            Console.WriteLine("4. 关闭确认对话框");
            pauseManager.Pop(confirmToken);
            ShowPauseStatus();

            // 关闭设置对话框
            Console.WriteLine("5. 关闭设置对话框");
            pauseManager.Pop(settingsToken);
            ShowPauseStatus();

            // 关闭暂停菜单
            Console.WriteLine("6. 关闭暂停菜单");
            pauseManager.Pop(pauseMenuToken);
            ShowPauseStatus();
        }

        /// <summary>
        /// 测试暂停作用域（using 语法）
        /// </summary>
        public void TestPauseScope()
        {
            var pauseManager = this.GetUtility<IPauseStackManager>();

            Console.WriteLine("--- 测试暂停作用域 ---\n");

            Console.WriteLine("进入外层作用域");
            using (pauseManager.PauseScope("外层暂停"))
            {
                ShowPauseStatus();

                Console.WriteLine("进入内层作用域");
                using (pauseManager.PauseScope("内层暂停"))
                {
                    ShowPauseStatus();
                }

                Console.WriteLine("退出内层作用域");
                ShowPauseStatus();
            }

            Console.WriteLine("退出外层作用域");
            ShowPauseStatus();
        }

        /// <summary>
        /// 紧急清除所有暂停
        /// </summary>
        public void EmergencyClearAll()
        {
            var pauseManager = this.GetUtility<IPauseStackManager>();

            Console.WriteLine("⚠️ 紧急清除所有暂停状态");
            pauseManager.ClearAll();

            ShowPauseStatus();
        }

        /// <summary>
        /// 清除特定组的暂停
        /// </summary>
        public void ClearGroup(PauseGroup group)
        {
            var pauseManager = this.GetUtility<IPauseStackManager>();

            Console.WriteLine($"清除 {group} 组的所有暂停");
            pauseManager.ClearGroup(group);

            ShowPauseStatus();
        }
    }
}
```

**代码说明**：

- `GetPauseDepth` 获取暂停栈深度（嵌套层数）
- `GetPauseReasons` 获取所有暂停原因，用于调试
- `PauseScope` 使用 using 语法自动管理暂停生命周期
- `ClearGroup` 清除特定组的所有暂停
- `ClearAll` 紧急清除所有暂停（慎用）

## 步骤 5：实现自定义暂停处理器

创建自定义处理器来响应暂停状态变化，实现具体的暂停逻辑。

```csharp
using GFramework.Core.Abstractions.Pause;

namespace MyGame.Handlers
{
    /// <summary>
    /// 游戏逻辑暂停处理器
    /// </summary>
    public class GameplayPauseHandler : IPauseHandler
    {
        // 优先级：数值越小越先执行
        public int Priority => 10;

        public void OnPauseStateChanged(PauseGroup group, bool isPaused)
        {
            // 只处理游戏逻辑组和全局组
            if (group != PauseGroup.Gameplay && group != PauseGroup.Global)
                return;

            if (isPaused)
            {
                Console.WriteLine($"[GameplayHandler] 暂停游戏逻辑 (Group: {group})");
                PauseGameplay();
            }
            else
            {
                Console.WriteLine($"[GameplayHandler] 恢复游戏逻辑 (Group: {group})");
                ResumeGameplay();
            }
        }

        private void PauseGameplay()
        {
            // 暂停物理模拟
            Console.WriteLine("  - 暂停物理引擎");

            // 暂停 AI 更新
            Console.WriteLine("  - 暂停 AI 系统");

            // 暂停动画
            Console.WriteLine("  - 暂停游戏动画");
        }

        private void ResumeGameplay()
        {
            // 恢复物理模拟
            Console.WriteLine("  - 恢复物理引擎");

            // 恢复 AI 更新
            Console.WriteLine("  - 恢复 AI 系统");

            // 恢复动画
            Console.WriteLine("  - 恢复游戏动画");
        }
    }

    /// <summary>
    /// 音频暂停处理器
    /// </summary>
    public class AudioPauseHandler : IPauseHandler
    {
        public int Priority => 20;

        public void OnPauseStateChanged(PauseGroup group, bool isPaused)
        {
            // 处理音频组和全局组
            if (group != PauseGroup.Audio && group != PauseGroup.Global)
                return;

            if (isPaused)
            {
                Console.WriteLine($"[AudioHandler] 暂停音频 (Group: {group})");
                PauseAudio();
            }
            else
            {
                Console.WriteLine($"[AudioHandler] 恢复音频 (Group: {group})");
                ResumeAudio();
            }
        }

        private void PauseAudio()
        {
            Console.WriteLine("  - 暂停背景音乐");
            Console.WriteLine("  - 暂停音效播放");
        }

        private void ResumeAudio()
        {
            Console.WriteLine("  - 恢复背景音乐");
            Console.WriteLine("  - 恢复音效播放");
        }
    }

    /// <summary>
    /// UI 暂停处理器
    /// </summary>
    public class UIPauseHandler : IPauseHandler
    {
        public int Priority => 5; // 最高优先级，最先执行

        public void OnPauseStateChanged(PauseGroup group, bool isPaused)
        {
            // 只处理全局暂停
            if (group != PauseGroup.Global)
                return;

            if (isPaused)
            {
                Console.WriteLine($"[UIHandler] 显示暂停 UI (Group: {group})");
                ShowPauseUI();
            }
            else
            {
                Console.WriteLine($"[UIHandler] 隐藏暂停 UI (Group: {group})");
                HidePauseUI();
            }
        }

        private void ShowPauseUI()
        {
            Console.WriteLine("  - 显示暂停菜单");
            Console.WriteLine("  - 禁用游戏输入");
        }

        private void HidePauseUI()
        {
            Console.WriteLine("  - 隐藏暂停菜单");
            Console.WriteLine("  - 启用游戏输入");
        }
    }
}
```

**代码说明**：

- 实现 `IPauseHandler` 接口创建自定义处理器
- `Priority` 属性控制执行顺序（数值越小越先执行）
- `OnPauseStateChanged` 在暂停状态变化时被调用
- 可以根据 `group` 参数选择性处理不同的暂停组
- 多个处理器按优先级顺序执行

### 注册暂停处理器

在架构初始化时注册所有处理器：

```csharp
using GFramework.Core.Architecture;
using GFramework.Core.Abstractions.Pause;
using GFramework.Core.Pause;
using MyGame.Handlers;

namespace MyGame
{
    public class GameArchitecture : Architecture
    {
        public static IArchitecture Interface { get; private set; }

        protected override void OnInitialize()
        {
            Interface = this;

            // 创建并注册暂停管理器
            var pauseManager = new PauseStackManager();
            RegisterUtility<IPauseStackManager>(pauseManager);

            // 注册暂停处理器（按优先级顺序）
            pauseManager.RegisterHandler(new UIPauseHandler());        // Priority: 5
            pauseManager.RegisterHandler(new GameplayPauseHandler());  // Priority: 10
            pauseManager.RegisterHandler(new AudioPauseHandler());     // Priority: 20

            Console.WriteLine("暂停系统和处理器初始化完成");
        }
    }
}
```

**代码说明**：

- 先创建 `PauseStackManager` 实例
- 使用 `RegisterHandler` 注册所有处理器
- 处理器会在暂停状态变化时自动被调用
- 注册顺序不重要，执行顺序由 `Priority` 决定

## 步骤 6：集成到游戏场景

创建一个完整的游戏场景示例，展示暂停系统的实际应用。

```csharp
using GFramework.Core.Abstractions.Pause;
using GFramework.Core.Abstractions.Controller;
using GFramework.Core.SourceGenerators.Abstractions.Rule;
using System.Threading.Tasks;

namespace MyGame.Controllers
{
    /// <summary>
    /// 游戏场景控制器
    /// </summary>
    [ContextAware]
    public partial class GameSceneController : IController
    {
        private PauseToken? _pauseMenuToken;
        private PauseToken? _dialogToken;

        /// <summary>
        /// 切换暂停菜单
        /// </summary>
        public void TogglePauseMenu()
        {
            var pauseManager = this.GetUtility<IPauseStackManager>();

            if (_pauseMenuToken.HasValue)
            {
                // 关闭暂停菜单
                pauseManager.Pop(_pauseMenuToken.Value);
                _pauseMenuToken = null;
                Console.WriteLine("暂停菜单已关闭");
            }
            else
            {
                // 打开暂停菜单
                _pauseMenuToken = pauseManager.Push("暂停菜单", PauseGroup.Global);
                Console.WriteLine("暂停菜单已打开");
            }
        }

        /// <summary>
        /// 显示对话框
        /// </summary>
        public void ShowDialog(string message)
        {
            var pauseManager = this.GetUtility<IPauseStackManager>();

            // 对话框使用全局暂停
            _dialogToken = pauseManager.Push($"对话框: {message}", PauseGroup.Global);
            Console.WriteLine($"显示对话框: {message}");
        }

        /// <summary>
        /// 关闭对话框
        /// </summary>
        public void CloseDialog()
        {
            if (!_dialogToken.HasValue)
            {
                Console.WriteLine("没有打开的对话框");
                return;
            }

            var pauseManager = this.GetUtility<IPauseStackManager>();
            pauseManager.Pop(_dialogToken.Value);
            _dialogToken = null;
            Console.WriteLine("对话框已关闭");
        }

        /// <summary>
        /// 模拟游戏循环
        /// </summary>
        public async Task RunGameLoop()
        {
            var pauseManager = this.GetUtility<IPauseStackManager>();
            int frame = 0;

            Console.WriteLine("\n=== 游戏循环开始 ===\n");

            while (frame < 20)
            {
                frame++;
                double deltaTime = 0.016; // 约 60 FPS

                // 检查是否暂停
                bool isGlobalPaused = pauseManager.IsPaused(PauseGroup.Global);
                bool isGameplayPaused = pauseManager.IsPaused(PauseGroup.Gameplay);

                Console.WriteLine($"\n--- 帧 {frame} ---");

                if (!isGlobalPaused)
                {
                    // UI 始终更新（除非全局暂停）
                    UpdateUI(deltaTime);
                }

                if (!isGameplayPaused && !isGlobalPaused)
                {
                    // 游戏逻辑更新
                    UpdateGameplay(deltaTime);
                }

                // 模拟帧延迟
                await Task.Delay(100);

                // 在第 5 帧打开暂停菜单
                if (frame == 5)
                {
                    Console.WriteLine("\n>>> 玩家按下 ESC 键 <<<");
                    TogglePauseMenu();
                }

                // 在第 10 帧关闭暂停菜单
                if (frame == 10)
                {
                    Console.WriteLine("\n>>> 玩家再次按下 ESC 键 <<<");
                    TogglePauseMenu();
                }

                // 在第 15 帧显示对话框
                if (frame == 15)
                {
                    Console.WriteLine("\n>>> 触发剧情对话 <<<");
                    ShowDialog("欢迎来到游戏世界！");
                }

                // 在第 18 帧关闭对话框
                if (frame == 18)
                {
                    Console.WriteLine("\n>>> 玩家点击确定 <<<");
                    CloseDialog();
                }
            }

            Console.WriteLine("\n=== 游戏循环结束 ===\n");
        }

        private void UpdateUI(double deltaTime)
        {
            Console.WriteLine($"  [UI] 更新界面 ({deltaTime:F3}s)");
        }

        private void UpdateGameplay(double deltaTime)
        {
            Console.WriteLine($"  [Gameplay] 更新游戏逻辑 ({deltaTime:F3}s)");
        }
    }
}
```

**代码说明**：

- `TogglePauseMenu` 实现暂停菜单的开关
- 保存暂停令牌以便后续恢复
- 在游戏循环中检查暂停状态
- 根据不同的暂停组选择性更新系统
- 模拟真实游戏场景中的暂停交互

## 完整代码

### 项目结构

```text
MyGame/
├── GameArchitecture.cs
├── Systems/
│   ├── GameLogicSystem.cs
│   └── AdvancedGameSystem.cs
├── Controllers/
│   ├── PauseController.cs
│   └── GameSceneController.cs
├── Handlers/
│   ├── GameplayPauseHandler.cs
│   ├── AudioPauseHandler.cs
│   └── UIPauseHandler.cs
└── Program.cs
```

### Program.cs

```csharp
using MyGame;
using MyGame.Controllers;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== 暂停系统教程 ===\n");

        // 1. 初始化架构
        var architecture = new GameArchitecture();
        architecture.Initialize();
        await architecture.WaitUntilReadyAsync();

        // 2. 测试基本暂停功能
        Console.WriteLine("\n【测试 1：基本暂停功能】");
        await TestBasicPause();

        await Task.Delay(1000);

        // 3. 测试嵌套暂停
        Console.WriteLine("\n【测试 2：嵌套暂停】");
        var pauseController = new PauseController();
        pauseController.TestNestedPause();

        await Task.Delay(1000);

        // 4. 测试暂停作用域
        Console.WriteLine("\n【测试 3：暂停作用域】");
        pauseController.TestPauseScope();

        await Task.Delay(1000);

        // 5. 测试游戏场景集成
        Console.WriteLine("\n【测试 4：游戏场景集成】");
        var sceneController = new GameSceneController();
        await sceneController.RunGameLoop();

        Console.WriteLine("\n=== 所有测试完成 ===");
    }

    static async Task TestBasicPause()
    {
        var architecture = GameArchitecture.Interface;
        var pauseManager = architecture.GetUtility<IPauseStackManager>();

        Console.WriteLine("游戏运行中...");
        Console.WriteLine($"是否暂停: {pauseManager.IsPaused()}");

        await Task.Delay(500);

        Console.WriteLine("\n按下暂停键...");
        var token = pauseManager.Push("用户暂停");
        Console.WriteLine($"是否暂停: {pauseManager.IsPaused()}");

        await Task.Delay(500);

        Console.WriteLine("\n按下恢复键...");
        pauseManager.Pop(token);
        Console.WriteLine($"是否暂停: {pauseManager.IsPaused()}");
    }
}
```

## 运行结果

运行程序后，你将看到类似以下的输出：

```text
=== 暂停系统教程 ===

暂停系统和处理器初始化完成

【测试 1：基本暂停功能】
游戏运行中...
是否暂停: False

按下暂停键...
[UIHandler] 显示暂停 UI (Group: Global)
  - 显示暂停菜单
  - 禁用游戏输入
[GameplayHandler] 暂停游戏逻辑 (Group: Global)
  - 暂停物理引擎
  - 暂停 AI 系统
  - 暂停游戏动画
[AudioHandler] 暂停音频 (Group: Global)
  - 暂停背景音乐
  - 暂停音效播放
是否暂停: True

按下恢复键...
[UIHandler] 隐藏暂停 UI (Group: Global)
  - 隐藏暂停菜单
  - 启用游戏输入
[GameplayHandler] 恢复游戏逻辑 (Group: Global)
  - 恢复物理引擎
  - 恢复 AI 系统
  - 恢复游戏动画
[AudioHandler] 恢复音频 (Group: Global)
  - 恢复背景音乐
  - 恢复音效播放
是否暂停: False

【测试 2：嵌套暂停】

--- 测试嵌套暂停 ---

1. 打开暂停菜单

=== 暂停状态 ===
全局暂停: True
游戏逻辑暂停: False
音频暂停: False
全局暂停深度: 1
暂停原因:
  - 暂停菜单
================

2. 在暂停菜单中打开设置

=== 暂停状态 ===
全局暂停: True
游戏逻辑暂停: False
音频暂停: False
全局暂停深度: 2
暂停原因:
  - 暂停菜单
  - 设置对话框
================

3. 在设置中打开确认对话框

=== 暂停状态 ===
全局暂停: True
游戏逻辑暂停: False
音频暂停: False
全局暂停深度: 3
暂停原因:
  - 暂停菜单
  - 设置对话框
  - 确认对话框
================

4. 关闭确认对话框

=== 暂停状态 ===
全局暂停: True
游戏逻辑暂停: False
音频暂停: False
全局暂停深度: 2
暂停原因:
  - 暂停菜单
  - 设置对话框
================

5. 关闭设置对话框

=== 暂停状态 ===
全局暂停: True
游戏逻辑暂停: False
音频暂停: False
全局暂停深度: 1
暂停原因:
  - 暂停菜单
================

6. 关闭暂停菜单

=== 暂停状态 ===
全局暂停: False
游戏逻辑暂停: False
音频暂停: False
全局暂停深度: 0
================

【测试 3：暂停作用域】

--- 测试暂停作用域 ---

进入外层作用域

=== 暂停状态 ===
全局暂停: True
游戏逻辑暂停: False
音频暂停: False
全局暂停深度: 1
暂停原因:
  - 外层暂停
================

进入内层作用域

=== 暂停状态 ===
全局暂停: True
游戏逻辑暂停: False
音频暂停: False
全局暂停深度: 2
暂停原因:
  - 外层暂停
  - 内层暂停
================

退出内层作用域

=== 暂停状态 ===
全局暂停: True
游戏逻辑暂停: False
音频暂停: False
全局暂停深度: 1
暂停原因:
  - 外层暂停
================

退出外层作用域

=== 暂停状态 ===
全局暂停: False
游戏逻辑暂停: False
音频暂停: False
全局暂停深度: 0
================

【测试 4：游戏场景集成】

=== 游戏循环开始 ===

--- 帧 1 ---
  [UI] 更新界面 (0.016s)
  [Gameplay] 更新游戏逻辑 (0.016s)

--- 帧 2 ---
  [UI] 更新界面 (0.016s)
  [Gameplay] 更新游戏逻辑 (0.016s)

...

--- 帧 5 ---
  [UI] 更新界面 (0.016s)
  [Gameplay] 更新游戏逻辑 (0.016s)

>>> 玩家按下 ESC 键 <<<
暂停菜单已打开

--- 帧 6 ---

--- 帧 7 ---

...

--- 帧 10 ---

>>> 玩家再次按下 ESC 键 <<<
暂停菜单已关闭

--- 帧 11 ---
  [UI] 更新界面 (0.016s)
  [Gameplay] 更新游戏逻辑 (0.016s)

...

=== 游戏循环结束 ===

=== 所有测试完成 ===
```

**验证步骤**：

1. 基本暂停和恢复功能正常工作
2. 暂停处理器按优先级顺序执行
3. 嵌套暂停栈正确管理多层暂停
4. 暂停作用域自动管理生命周期
5. 游戏循环正确响应暂停状态

## 下一步

恭喜！你已经掌握了暂停系统的使用。接下来可以学习：

- [使用协程系统](/zh-CN/tutorials/coroutine-tutorial.md) - 在暂停状态下控制协程
- [实现状态机](/zh-CN/tutorials/state-machine-tutorial.md) - 结合状态机管理游戏流程
- [事件系统](/zh-CN/core/events.md) - 使用事件响应暂停状态变化

## 相关文档

- [架构组件](/zh-CN/core/architecture.md) - 架构基础
- [Utility 层](/zh-CN/core/utility.md) - Utility 详细说明
- [生命周期管理](/zh-CN/core/lifecycle.md) - 组件生命周期
- [扩展方法](/zh-CN/core/extensions.md) - 便捷的扩展方法
