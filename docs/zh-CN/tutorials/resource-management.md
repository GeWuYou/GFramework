---
title: 资源管理最佳实践
description: 学习如何高效管理游戏资源，避免内存泄漏和性能问题
---

# 资源管理最佳实践

## 学习目标

完成本教程后，你将能够：

- 理解资源管理的核心概念和重要性
- 实现自定义资源加载器
- 使用资源句柄管理资源生命周期
- 实现资源预加载和延迟加载
- 选择合适的资源释放策略
- 避免常见的资源管理陷阱

## 前置条件

- 已安装 GFramework.Core NuGet 包
- 了解 C# 基础语法和 async/await
- 阅读过[快速开始](/zh-CN/getting-started/quick-start.md)
- 了解[协程系统](/zh-CN/core/coroutine.md)

## 步骤 1：创建资源类型和加载器

首先，让我们定义游戏中常用的资源类型，并为它们实现加载器。

```csharp
using GFramework.Core.Abstractions.Resource;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MyGame.Resources
{
    // ===== 资源类型定义 =====

    /// <summary>
    /// 纹理资源
    /// </summary>
    public class Texture : IDisposable
    {
        public string Path { get; set; } = string.Empty;
        public int Width { get; set; }
        public int Height { get; set; }
        public byte[]? Data { get; set; }

        public void Dispose()
        {
            Data = null;
            Console.WriteLine($"纹理已释放: {Path}");
        }
    }

    /// <summary>
    /// 音频资源
    /// </summary>
    public class AudioClip : IDisposable
    {
        public string Path { get; set; } = string.Empty;
        public double Duration { get; set; }
        public byte[]? Data { get; set; }

        public void Dispose()
        {
            Data = null;
            Console.WriteLine($"音频已释放: {Path}");
        }
    }

    /// <summary>
    /// 配置文件资源
    /// </summary>
    public class ConfigData
    {
        public string Path { get; set; } = string.Empty;
        public Dictionary<string, string> Data { get; set; } = new();
    }

    // ===== 资源加载器实现 =====

    /// <summary>
    /// 纹理加载器
    /// </summary>
    public class TextureLoader : IResourceLoader<Texture>
    {
        public Texture Load(string path)
        {
            Console.WriteLine($"同步加载纹理: {path}");

            // 模拟加载纹理
            Thread.Sleep(100); // 模拟 I/O 延迟

            return new Texture
            {
                Path = path,
                Width = 512,
                Height = 512,
                Data = new byte[512 * 512 * 4] // RGBA
            };
        }

        public async Task<Texture> LoadAsync(string path)
        {
            Console.WriteLine($"异步加载纹理: {path}");

            // 模拟异步加载
            await Task.Delay(100);

            return new Texture
            {
                Path = path,
                Width = 512,
                Height = 512,
                Data = new byte[512 * 512 * 4]
            };
        }

        public void Unload(Texture resource)
        {
            resource?.Dispose();
        }
    }

    /// <summary>
    /// 音频加载器
    /// </summary>
    public class AudioLoader : IResourceLoader<AudioClip>
    {
        public AudioClip Load(string path)
        {
            Console.WriteLine($"同步加载音频: {path}");
            Thread.Sleep(150);

            return new AudioClip
            {
                Path = path,
                Duration = 30.0,
                Data = new byte[1024 * 1024] // 1MB
            };
        }

        public async Task<AudioClip> LoadAsync(string path)
        {
            Console.WriteLine($"异步加载音频: {path}");
            await Task.Delay(150);

            return new AudioClip
            {
                Path = path,
                Duration = 30.0,
                Data = new byte[1024 * 1024]
            };
        }

        public void Unload(AudioClip resource)
        {
            resource?.Dispose();
        }
    }

    /// <summary>
    /// 配置文件加载器
    /// </summary>
    public class ConfigLoader : IResourceLoader<ConfigData>
    {
        public ConfigData Load(string path)
        {
            Console.WriteLine($"加载配置: {path}");

            // 模拟解析配置文件
            return new ConfigData
            {
                Path = path,
                Data = new Dictionary<string, string>
                {
                    ["version"] = "1.0",
                    ["difficulty"] = "normal"
                }
            };
        }

        public async Task<ConfigData> LoadAsync(string path)
        {
            await Task.Delay(50);
            return Load(path);
        }

        public void Unload(ConfigData resource)
        {
            resource.Data.Clear();
            Console.WriteLine($"配置已释放: {resource.Path}");
        }
    }
}
```

**代码说明**：

- 定义了三种常见资源类型：纹理、音频、配置
- 实现 `IResourceLoader<T>` 接口提供加载逻辑
- 同步和异步加载方法分别处理不同场景
- `Unload` 方法负责资源清理

## 步骤 2：注册资源管理器

在架构中注册资源管理器和所有加载器。

```csharp
using GFramework.Core.Architecture;
using GFramework.Core.Abstractions.Resource;
using GFramework.Core.Resource;
using MyGame.Resources;

namespace MyGame
{
    public class GameArchitecture : Architecture
    {
        public static IArchitecture Interface { get; private set; }

        protected override void OnInitialize()
        {
            Interface = this;

            // 创建资源管理器
            var resourceManager = new ResourceManager();

            // 注册资源加载器
            resourceManager.RegisterLoader(new TextureLoader());
            resourceManager.RegisterLoader(new AudioLoader());
            resourceManager.RegisterLoader(new ConfigLoader());

            // 设置释放策略（默认手动释放）
            resourceManager.SetReleaseStrategy(new ManualReleaseStrategy());

            // 注册到架构
            RegisterUtility<IResourceManager>(resourceManager);

            Console.WriteLine("资源管理器初始化完成");
        }
    }
}
```

**代码说明**：

- 创建 `ResourceManager` 实例
- 为每种资源类型注册对应的加载器
- 设置资源释放策略
- 将资源管理器注册为 Utility

## 步骤 3：实现资源预加载系统

创建一个系统来管理资源的预加载和卸载。

```csharp
using GFramework.Core.System;
using GFramework.Core.Abstractions.Resource;
using GFramework.Core.Extensions;
using MyGame.Resources;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyGame.Systems
{
    /// <summary>
    /// 资源预加载系统
    /// </summary>
    public class ResourcePreloadSystem : AbstractSystem
    {
        // 场景资源配置
        private readonly Dictionary<string, List<string>> _sceneResources = new()
        {
            ["Menu"] = new List<string>
            {
                "textures/menu_bg.png",
                "textures/button.png",
                "audio/menu_bgm.mp3"
            },
            ["Gameplay"] = new List<string>
            {
                "textures/player.png",
                "textures/enemy.png",
                "textures/bullet.png",
                "audio/game_bgm.mp3",
                "audio/shoot.mp3",
                "config/level_1.cfg"
            },
            ["GameOver"] = new List<string>
            {
                "textures/gameover_bg.png",
                "audio/gameover.mp3"
            }
        };

        // 当前场景的资源句柄
        private readonly List<IDisposable> _currentHandles = new();

        /// <summary>
        /// 预加载场景资源
        /// </summary>
        public async Task PreloadSceneAsync(string sceneName)
        {
            if (!_sceneResources.TryGetValue(sceneName, out var resources))
            {
                Console.WriteLine($"场景 {sceneName} 没有配置资源");
                return;
            }

            Console.WriteLine($"\n=== 开始预加载场景: {sceneName} ===");
            var resourceManager = this.GetUtility<IResourceManager>();

            // 并行加载所有资源
            var tasks = new List<Task>();

            foreach (var path in resources)
            {
                if (path.EndsWith(".png"))
                {
                    tasks.Add(resourceManager.PreloadAsync<Texture>(path));
                }
                else if (path.EndsWith(".mp3"))
                {
                    tasks.Add(resourceManager.PreloadAsync<AudioClip>(path));
                }
                else if (path.EndsWith(".cfg"))
                {
                    tasks.Add(resourceManager.PreloadAsync<ConfigData>(path));
                }
            }

            await Task.WhenAll(tasks);
            Console.WriteLine($"场景 {sceneName} 资源预加载完成\n");
        }

        /// <summary>
        /// 预加载场景资源（带进度）
        /// </summary>
        public async Task PreloadSceneWithProgressAsync(
            string sceneName,
            Action<float> onProgress)
        {
            if (!_sceneResources.TryGetValue(sceneName, out var resources))
                return;

            Console.WriteLine($"\n=== 开始预加载场景: {sceneName} ===");
            var resourceManager = this.GetUtility<IResourceManager>();

            int totalCount = resources.Count;
            int loadedCount = 0;

            foreach (var path in resources)
            {
                // 根据扩展名加载不同类型的资源
                if (path.EndsWith(".png"))
                {
                    await resourceManager.PreloadAsync<Texture>(path);
                }
                else if (path.EndsWith(".mp3"))
                {
                    await resourceManager.PreloadAsync<AudioClip>(path);
                }
                else if (path.EndsWith(".cfg"))
                {
                    await resourceManager.PreloadAsync<ConfigData>(path);
                }

                loadedCount++;
                float progress = (float)loadedCount / totalCount;
                onProgress?.Invoke(progress);

                Console.WriteLine($"加载进度: {progress * 100:F0}% ({loadedCount}/{totalCount})");
            }

            Console.WriteLine($"场景 {sceneName} 资源预加载完成\n");
        }

        /// <summary>
        /// 获取场景资源句柄
        /// </summary>
        public void AcquireSceneResources(string sceneName)
        {
            if (!_sceneResources.TryGetValue(sceneName, out var resources))
                return;

            Console.WriteLine($"获取场景 {sceneName} 的资源句柄");
            var resourceManager = this.GetUtility<IResourceManager>();

            // 清理旧句柄
            ReleaseCurrentResources();

            // 获取新句柄
            foreach (var path in resources)
            {
                IDisposable? handle = null;

                if (path.EndsWith(".png"))
                {
                    handle = resourceManager.GetHandle<Texture>(path);
                }
                else if (path.EndsWith(".mp3"))
                {
                    handle = resourceManager.GetHandle<AudioClip>(path);
                }
                else if (path.EndsWith(".cfg"))
                {
                    handle = resourceManager.GetHandle<ConfigData>(path);
                }

                if (handle != null)
                {
                    _currentHandles.Add(handle);
                }
            }

            Console.WriteLine($"已获取 {_currentHandles.Count} 个资源句柄");
        }

        /// <summary>
        /// 释放当前场景资源
        /// </summary>
        public void ReleaseCurrentResources()
        {
            Console.WriteLine($"释放 {_currentHandles.Count} 个资源句柄");

            foreach (var handle in _currentHandles)
            {
                handle.Dispose();
            }

            _currentHandles.Clear();
        }

        /// <summary>
        /// 卸载场景资源
        /// </summary>
        public void UnloadSceneResources(string sceneName)
        {
            if (!_sceneResources.TryGetValue(sceneName, out var resources))
                return;

            Console.WriteLine($"\n卸载场景 {sceneName} 的资源");
            var resourceManager = this.GetUtility<IResourceManager>();

            foreach (var path in resources)
            {
                resourceManager.Unload(path);
            }

            Console.WriteLine($"场景 {sceneName} 资源已卸载\n");
        }

        /// <summary>
        /// 显示资源状态
        /// </summary>
        public void ShowResourceStatus()
        {
            var resourceManager = this.GetUtility<IResourceManager>();

            Console.WriteLine("\n=== 资源状态 ===");
            Console.WriteLine($"已加载资源数: {resourceManager.LoadedResourceCount}");
            Console.WriteLine("已加载资源列表:");

            foreach (var path in resourceManager.GetLoadedResourcePaths())
            {
                Console.WriteLine($"  - {path}");
            }

            Console.WriteLine();
        }
    }
}
```

**代码说明**：

- 使用字典配置每个场景需要的资源
- `PreloadSceneAsync` 并行预加载所有资源
- `PreloadSceneWithProgressAsync` 提供加载进度回调
- `AcquireSceneResources` 获取资源句柄防止被释放
- `ReleaseCurrentResources` 释放不再使用的资源

## 步骤 4：实现资源使用示例

创建一个游戏系统展示如何正确使用资源。

```csharp
using GFramework.Core.System;
using GFramework.Core.Abstractions.Resource;
using GFramework.Core.Extensions;
using MyGame.Resources;

namespace MyGame.Systems
{
    /// <summary>
    /// 游戏系统示例
    /// </summary>
    public class GameplaySystem : AbstractSystem
    {
        private IResourceHandle<Texture>? _playerTexture;
        private IResourceHandle<AudioClip>? _bgmClip;

        /// <summary>
        /// 初始化游戏
        /// </summary>
        public void InitializeGame()
        {
            Console.WriteLine("\n=== 初始化游戏 ===");
            var resourceManager = this.GetUtility<IResourceManager>();

            // 获取玩家纹理句柄
            _playerTexture = resourceManager.GetHandle<Texture>("textures/player.png");
            if (_playerTexture?.Resource != null)
            {
                Console.WriteLine($"玩家纹理已加载: {_playerTexture.Resource.Width}x{_playerTexture.Resource.Height}");
            }

            // 获取背景音乐句柄
            _bgmClip = resourceManager.GetHandle<AudioClip>("audio/game_bgm.mp3");
            if (_bgmClip?.Resource != null)
            {
                Console.WriteLine($"背景音乐已加载: {_bgmClip.Resource.Duration}秒");
            }

            Console.WriteLine("游戏初始化完成\n");
        }

        /// <summary>
        /// 使用临时资源（使用 using 语句）
        /// </summary>
        public void SpawnBullet()
        {
            var resourceManager = this.GetUtility<IResourceManager>();

            // 使用 using 语句自动管理资源生命周期
            using var bulletTexture = resourceManager.GetHandle<Texture>("textures/bullet.png");

            if (bulletTexture?.Resource != null)
            {
                Console.WriteLine("创建子弹，使用纹理");
                // 使用纹理创建子弹...
            }

            // 离开作用域后自动释放句柄
        }

        /// <summary>
        /// 播放音效（临时资源）
        /// </summary>
        public void PlayShootSound()
        {
            var resourceManager = this.GetUtility<IResourceManager>();

            using var shootSound = resourceManager.GetHandle<AudioClip>("audio/shoot.mp3");

            if (shootSound?.Resource != null)
            {
                Console.WriteLine("播放射击音效");
                // 播放音效...
            }
        }

        /// <summary>
        /// 清理游戏资源
        /// </summary>
        public void CleanupGame()
        {
            Console.WriteLine("\n=== 清理游戏资源 ===");

            // 释放长期持有的资源句柄
            _playerTexture?.Dispose();
            _playerTexture = null;

            _bgmClip?.Dispose();
            _bgmClip = null;

            Console.WriteLine("游戏资源已清理\n");
        }
    }
}
```

**代码说明**：

- 长期使用的资源（玩家纹理、BGM）保存句柄
- 临时资源（子弹纹理、音效）使用 `using` 语句
- 在清理时释放所有持有的句柄
- 展示了正确的资源生命周期管理

## 步骤 5：测试资源管理

编写测试代码验证资源管理功能。

```csharp
using MyGame;
using MyGame.Systems;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== 资源管理最佳实践测试 ===\n");

        // 1. 初始化架构
        var architecture = new GameArchitecture();
        architecture.Initialize();
        await architecture.WaitUntilReadyAsync();

        // 2. 获取系统
        var preloadSystem = architecture.GetSystem<ResourcePreloadSystem>();
        var gameplaySystem = architecture.GetSystem<GameplaySystem>();

        // 3. 测试场景资源预加载
        Console.WriteLine("--- 测试 1: 预加载菜单场景 ---");
        await preloadSystem.PreloadSceneWithProgressAsync("Menu", progress =>
        {
            // 进度回调
        });
        preloadSystem.ShowResourceStatus();

        await Task.Delay(500);

        // 4. 切换到游戏场景
        Console.WriteLine("--- 测试 2: 切换到游戏场景 ---");
        await preloadSystem.PreloadSceneWithProgressAsync("Gameplay", progress =>
        {
            // 进度回调
        });
        preloadSystem.ShowResourceStatus();

        await Task.Delay(500);

        // 5. 获取场景资源句柄
        Console.WriteLine("--- 测试 3: 获取游戏场景资源句柄 ---");
        preloadSystem.AcquireSceneResources("Gameplay");

        await Task.Delay(500);

        // 6. 初始化游戏
        Console.WriteLine("--- 测试 4: 初始化游戏 ---");
        gameplaySystem.InitializeGame();

        await Task.Delay(500);

        // 7. 使用临时资源
        Console.WriteLine("--- 测试 5: 使用临时资源 ---");
        gameplaySystem.SpawnBullet();
        gameplaySystem.PlayShootSound();
        preloadSystem.ShowResourceStatus();

        await Task.Delay(500);

        // 8. 清理游戏
        Console.WriteLine("--- 测试 6: 清理游戏 ---");
        gameplaySystem.CleanupGame();

        await Task.Delay(500);

        // 9. 释放场景资源句柄
        Console.WriteLine("--- 测试 7: 释放场景资源句柄 ---");
        preloadSystem.ReleaseCurrentResources();
        preloadSystem.ShowResourceStatus();

        await Task.Delay(500);

        // 10. 卸载旧场景资源
        Console.WriteLine("--- 测试 8: 卸载菜单场景资源 ---");
        preloadSystem.UnloadSceneResources("Menu");
        preloadSystem.ShowResourceStatus();

        Console.WriteLine("=== 测试完成 ===");
    }
}
```

**代码说明**：

- 测试资源预加载和进度回调
- 测试场景切换时的资源管理
- 测试资源句柄的获取和释放
- 测试临时资源的自动管理
- 验证资源状态和内存清理

## 完整代码

所有代码文件已在上述步骤中提供。项目结构如下：

```text
MyGame/
├── Resources/
│   ├── Texture.cs
│   ├── AudioClip.cs
│   ├── ConfigData.cs
│   ├── TextureLoader.cs
│   ├── AudioLoader.cs
│   └── ConfigLoader.cs
├── Systems/
│   ├── ResourcePreloadSystem.cs
│   └── GameplaySystem.cs
├── GameArchitecture.cs
└── Program.cs
```

## 运行结果

运行程序后，你将看到类似以下的输出：

```text
=== 资源管理最佳实践测试 ===

资源管理器初始化完成

--- 测试 1: 预加载菜单场景 ---

=== 开始预加载场景: Menu ===
异步加载纹理: textures/menu_bg.png
异步加载纹理: textures/button.png
异步加载音频: audio/menu_bgm.mp3
加载进度: 33% (1/3)
加载进度: 67% (2/3)
加载进度: 100% (3/3)
场景 Menu 资源预加载完成

=== 资源状态 ===
已加载资源数: 3
已加载资源列表:
  - textures/menu_bg.png
  - textures/button.png
  - audio/menu_bgm.mp3

--- 测试 2: 切换到游戏场景 ---

=== 开始预加载场景: Gameplay ===
异步加载纹理: textures/player.png
异步加载纹理: textures/enemy.png
异步加载纹理: textures/bullet.png
异步加载音频: audio/game_bgm.mp3
异步加载音频: audio/shoot.mp3
加载配置: config/level_1.cfg
加载进度: 17% (1/6)
加载进度: 33% (2/6)
加载进度: 50% (3/6)
加载进度: 67% (4/6)
加载进度: 83% (5/6)
加载进度: 100% (6/6)
场景 Gameplay 资源预加载完成

=== 资源状态 ===
已加载资源数: 9

--- 测试 3: 获取游戏场景资源句柄 ---
获取场景 Gameplay 的资源句柄
已获取 6 个资源句柄

--- 测试 4: 初始化游戏 ===

=== 初始化游戏 ===
玩家纹理已加载: 512x512
背景音乐已加载: 30秒
游戏初始化完成

--- 测试 5: 使用临时资源 ---
创建子弹，使用纹理
播放射击音效

--- 测试 6: 清理游戏 ---

=== 清理游戏资源 ===
游戏资源已清理

--- 测试 7: 释放场景资源句柄 ---
释放 6 个资源句柄

--- 测试 8: 卸载菜单场景资源 ---

卸载场景 Menu 的资源
纹理已释放: textures/menu_bg.png
纹理已释放: textures/button.png
音频已释放: audio/menu_bgm.mp3
场景 Menu 资源已卸载

=== 资源状态 ===
已加载资源数: 6

=== 测试完成 ===
```

**验证步骤**：

1. 资源预加载正常工作
2. 加载进度正确显示
3. 资源句柄管理正确
4. 临时资源自动释放
5. 资源卸载成功执行
6. 内存正确清理

## 下一步

恭喜！你已经掌握了资源管理的最佳实践。接下来可以学习：

- [使用协程系统](/zh-CN/tutorials/coroutine-tutorial.md) - 在协程中加载资源
- [实现状态机](/zh-CN/tutorials/state-machine-tutorial.md) - 在状态切换时管理资源
- [实现存档系统](/zh-CN/tutorials/save-system.md) - 保存和加载游戏数据

## 相关文档

- [资源管理系统](/zh-CN/core/resource.md) - 资源系统详细说明
- [对象池系统](/zh-CN/core/pool.md) - 结合对象池复用资源
- [协程系统](/zh-CN/core/coroutine.md) - 异步加载资源
- [System 层](/zh-CN/core/system.md) - System 详细说明
