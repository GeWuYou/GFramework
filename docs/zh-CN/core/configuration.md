# Configuration 包使用说明

## 概述

Configuration 包提供了线程安全的配置管理系统，支持类型安全的配置存储、访问、监听和持久化。配置管理器可以用于管理游戏设置、运行时参数、开发配置等各种键值对数据。

配置系统是 GFramework 架构中的实用工具（Utility），可以在架构的任何层级中使用，提供统一的配置管理能力。

## 核心接口

### IConfigurationManager

配置管理器接口，提供类型安全的配置存储和访问。所有方法都是线程安全的。

**核心方法：**

```csharp
// 配置访问
T? GetConfig&lt;T&gt;(string key);                    // 获取配置值
T GetConfig&lt;T&gt;(string key, T defaultValue);     // 获取配置值（带默认值）
void SetConfig&lt;T&gt;(string key, T value);         // 设置配置值
bool HasConfig(string key);                     // 检查配置是否存在
bool RemoveConfig(string key);                  // 移除配置
void Clear();                                   // 清空所有配置

// 配置监听
IUnRegister WatchConfig&lt;T&gt;(string key, Action&lt;T&gt; onChange);  // 监听配置变化

// 持久化
void LoadFromJson(string json);                 // 从 JSON 加载
string SaveToJson();                            // 保存为 JSON
void LoadFromFile(string path);                 // 从文件加载
void SaveToFile(string path);                   // 保存到文件

// 工具方法
int Count { get; }                              // 获取配置数量
IEnumerable<string> GetAllKeys();               // 获取所有配置键
```

## 核心类

### ConfigurationManager

配置管理器实现，提供线程安全的配置存储和访问。

**特性：**

- 线程安全：所有公共方法都是线程安全的
- 类型安全：支持泛型类型的配置值
- 自动类型转换：支持基本类型的自动转换
- 配置监听：支持监听配置变化并触发回调
- JSON 持久化：支持 JSON 格式的配置加载和保存

**使用示例：**

```csharp
// 创建配置管理器
var configManager = new ConfigurationManager();

// 设置配置
configManager.SetConfig("game.difficulty", "Normal");
configManager.SetConfig("audio.volume", 0.8f);
configManager.SetConfig("graphics.quality", 2);

// 获取配置
var difficulty = configManager.GetConfig<string>("game.difficulty");
var volume = configManager.GetConfig<float>("audio.volume");
var quality = configManager.GetConfig<int>("graphics.quality");

// 使用默认值
var fov = configManager.GetConfig("graphics.fov", 90);
```

## 基本用法

### 1. 设置和获取配置

```csharp
public class GameSettings : IUtility
{
    private readonly IConfigurationManager _config = new ConfigurationManager();

    public void Initialize()
    {
        // 设置游戏配置
        _config.SetConfig("player.name", "Player1");
        _config.SetConfig("player.level", 1);
        _config.SetConfig("player.experience", 0);

        // 设置游戏选项
        _config.SetConfig("options.showTutorial", true);
        _config.SetConfig("options.language", "zh-CN");
    }

    public string GetPlayerName()
    {
        return _config.GetConfig<string>("player.name") ?? "Unknown";
    }

    public int GetPlayerLevel()
    {
        return _config.GetConfig("player.level", 1);
    }
}
```

### 2. 检查和移除配置

```csharp
public class ConfigurationService
{
    private readonly IConfigurationManager _config;

    public ConfigurationService(IConfigurationManager config)
    {
        _config = config;
    }

    public void ResetPlayerData()
    {
        // 检查配置是否存在
        if (_config.HasConfig("player.name"))
        {
            _config.RemoveConfig("player.name");
        }

        if (_config.HasConfig("player.level"))
        {
            _config.RemoveConfig("player.level");
        }

        // 或者清空所有配置
        // _config.Clear();
    }

    public void PrintAllConfigs()
    {
        Console.WriteLine($"Total configs: {_config.Count}");

        foreach (var key in _config.GetAllKeys())
        {
            Console.WriteLine($"Key: {key}");
        }
    }
}
```

### 3. 支持的数据类型

```csharp
public class TypeExamples
{
    private readonly IConfigurationManager _config = new ConfigurationManager();

    public void SetupConfigs()
    {
        // 基本类型
        _config.SetConfig("int.value", 42);
        _config.SetConfig("float.value", 3.14f);
        _config.SetConfig("double.value", 2.718);
        _config.SetConfig("bool.value", true);
        _config.SetConfig("string.value", "Hello");

        // 复杂类型
        _config.SetConfig("vector.position", new Vector3(1, 2, 3));
        _config.SetConfig("list.items", new List<string> { "A", "B", "C" });
        _config.SetConfig("dict.data", new Dictionary<string, int>
        {
            ["key1"] = 1,
            ["key2"] = 2
        });
    }

    public void GetConfigs()
    {
        var intValue = _config.GetConfig<int>("int.value");
        var floatValue = _config.GetConfig<float>("float.value");
        var boolValue = _config.GetConfig<bool>("bool.value");
        var stringValue = _config.GetConfig<string>("string.value");

        var position = _config.GetConfig<Vector3>("vector.position");
        var items = _config.GetConfig<List<string>>("list.items");
    }
}
```

## 高级用法

### 1. 配置监听（热更新）

配置监听允许在配置值变化时自动触发回调，实现配置的热更新。

```csharp
public class AudioManager : AbstractSystem
{
    private IUnRegister _volumeWatcher;

    protected override void OnInit()
    {
        var config = this.GetUtility<IConfigurationManager>();

        // 监听音量配置变化
        _volumeWatcher = config.WatchConfig<float>("audio.masterVolume", newVolume =>
        {
            UpdateMasterVolume(newVolume);
            this.GetUtility<ILogger>()?.Info($"Master volume changed to: {newVolume}");
        });

        // 监听音效开关
        config.WatchConfig<bool>("audio.sfxEnabled", enabled =>
        {
            if (enabled)
                EnableSoundEffects();
            else
                DisableSoundEffects();
        });
    }

    private void UpdateMasterVolume(float volume)
    {
        // 更新音频引擎的主音量
        AudioEngine.SetMasterVolume(volume);
    }

    protected override void OnDestroy()
    {
        // 取消监听
        _volumeWatcher?.UnRegister();
    }
}
```

### 2. 多个监听器

```csharp
public class GraphicsManager : AbstractSystem
{
    protected override void OnInit()
    {
        var config = this.GetUtility<IConfigurationManager>();

        // 多个组件监听同一个配置
        config.WatchConfig<int>("graphics.quality", quality =>
        {
            UpdateTextureQuality(quality);
        });

        config.WatchConfig<int>("graphics.quality", quality =>
        {
            UpdateShadowQuality(quality);
        });

        config.WatchConfig<int>("graphics.quality", quality =>
        {
            UpdatePostProcessing(quality);
        });
    }

    private void UpdateTextureQuality(int quality) { }
    private void UpdateShadowQuality(int quality) { }
    private void UpdatePostProcessing(int quality) { }
}
```

### 3. 配置持久化

#### 保存和加载 JSON

```csharp
public class ConfigurationPersistence
{
    private readonly IConfigurationManager _config;
    private readonly string _configPath = "config/game_settings.json";

    public ConfigurationPersistence(IConfigurationManager config)
    {
        _config = config;
    }

    public void SaveConfiguration()
    {
        try
        {
            // 保存到文件
            _config.SaveToFile(_configPath);
            Console.WriteLine($"Configuration saved to {_configPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save configuration: {ex.Message}");
        }
    }

    public void LoadConfiguration()
    {
        try
        {
            // 从文件加载
            if (File.Exists(_configPath))
            {
                _config.LoadFromFile(_configPath);
                Console.WriteLine($"Configuration loaded from {_configPath}");
            }
            else
            {
                Console.WriteLine("Configuration file not found, using defaults");
                SetDefaultConfiguration();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load configuration: {ex.Message}");
            SetDefaultConfiguration();
        }
    }

    private void SetDefaultConfiguration()
    {
        _config.SetConfig("audio.masterVolume", 1.0f);
        _config.SetConfig("audio.musicVolume", 0.8f);
        _config.SetConfig("audio.sfxVolume", 1.0f);
        _config.SetConfig("graphics.quality", 2);
        _config.SetConfig("graphics.fullscreen", true);
    }
}
```

#### JSON 字符串操作

```csharp
public class ConfigurationExport
{
    private readonly IConfigurationManager _config;

    public ConfigurationExport(IConfigurationManager config)
    {
        _config = config;
    }

    public string ExportToJson()
    {
        // 导出为 JSON 字符串
        return _config.SaveToJson();
    }

    public void ImportFromJson(string json)
    {
        // 从 JSON 字符串导入
        _config.LoadFromJson(json);
    }

    public void ShareConfiguration()
    {
        // 导出配置用于分享
        var json = _config.SaveToJson();

        // 可以通过网络发送、保存到剪贴板等
        Clipboard.SetText(json);
        Console.WriteLine("Configuration copied to clipboard");
    }
}
```

### 4. 多环境配置

```csharp
public class EnvironmentConfiguration
{
    private readonly IConfigurationManager _config;
    private readonly string _environment;

    public EnvironmentConfiguration(IConfigurationManager config, string environment)
    {
        _config = config;
        _environment = environment;
    }

    public void LoadEnvironmentConfig()
    {
        // 根据环境加载不同的配置文件
        var configPath = _environment switch
        {
            "development" => "config/dev.json",
            "staging" => "config/staging.json",
            "production" => "config/prod.json",
            _ => "config/default.json"
        };

        if (File.Exists(configPath))
        {
            _config.LoadFromFile(configPath);
            Console.WriteLine($"Loaded {_environment} configuration");
        }

        // 设置环境特定的配置
        ApplyEnvironmentOverrides();
    }

    private void ApplyEnvironmentOverrides()
    {
        switch (_environment)
        {
            case "development":
                _config.SetConfig("debug.enabled", true);
                _config.SetConfig("logging.level", "Debug");
                _config.SetConfig("api.endpoint", "http://localhost:3000");
                break;

            case "production":
                _config.SetConfig("debug.enabled", false);
                _config.SetConfig("logging.level", "Warning");
                _config.SetConfig("api.endpoint", "https://api.production.com");
                break;
        }
    }
}
```

### 5. 配置验证

```csharp
public class ConfigurationValidator
{
    private readonly IConfigurationManager _config;

    public ConfigurationValidator(IConfigurationManager config)
    {
        _config = config;
    }

    public bool ValidateConfiguration()
    {
        var isValid = true;

        // 验证必需的配置项
        if (!_config.HasConfig("game.version"))
        {
            Console.WriteLine("Error: game.version is required");
            isValid = false;
        }

        // 验证配置值范围
        var volume = _config.GetConfig("audio.masterVolume", -1f);
        if (volume < 0f || volume > 1f)
        {
            Console.WriteLine("Error: audio.masterVolume must be between 0 and 1");
            isValid = false;
        }

        // 验证配置类型
        try
        {
            var quality = _config.GetConfig<int>("graphics.quality");
            if (quality < 0 || quality > 3)
            {
                Console.WriteLine("Error: graphics.quality must be between 0 and 3");
                isValid = false;
            }
        }
        catch
        {
            Console.WriteLine("Error: graphics.quality must be an integer");
            isValid = false;
        }

        return isValid;
    }

    public void ApplyConstraints()
    {
        // 自动修正超出范围的值
        var volume = _config.GetConfig("audio.masterVolume", 1f);
        if (volume < 0f) _config.SetConfig("audio.masterVolume", 0f);
        if (volume > 1f) _config.SetConfig("audio.masterVolume", 1f);

        var quality = _config.GetConfig("graphics.quality", 2);
        if (quality < 0) _config.SetConfig("graphics.quality", 0);
        if (quality > 3) _config.SetConfig("graphics.quality", 3);
    }
}
```

### 6. 配置分组管理

```csharp
public class ConfigurationGroups
{
    private readonly IConfigurationManager _config;

    public ConfigurationGroups(IConfigurationManager config)
    {
        _config = config;
    }

    // 音频配置组
    public class AudioConfig
    {
        public float MasterVolume { get; set; } = 1.0f;
        public float MusicVolume { get; set; } = 0.8f;
        public float SfxVolume { get; set; } = 1.0f;
        public bool Muted { get; set; } = false;
    }

    // 图形配置组
    public class GraphicsConfig
    {
        public int Quality { get; set; } = 2;
        public bool Fullscreen { get; set; } = true;
        public int ResolutionWidth { get; set; } = 1920;
        public int ResolutionHeight { get; set; } = 1080;
        public bool VSync { get; set; } = true;
    }

    public void SaveAudioConfig(AudioConfig audio)
    {
        _config.SetConfig("audio.masterVolume", audio.MasterVolume);
        _config.SetConfig("audio.musicVolume", audio.MusicVolume);
        _config.SetConfig("audio.sfxVolume", audio.SfxVolume);
        _config.SetConfig("audio.muted", audio.Muted);
    }

    public AudioConfig LoadAudioConfig()
    {
        return new AudioConfig
        {
            MasterVolume = _config.GetConfig("audio.masterVolume", 1.0f),
            MusicVolume = _config.GetConfig("audio.musicVolume", 0.8f),
            SfxVolume = _config.GetConfig("audio.sfxVolume", 1.0f),
            Muted = _config.GetConfig("audio.muted", false)
        };
    }

    public void SaveGraphicsConfig(GraphicsConfig graphics)
    {
        _config.SetConfig("graphics.quality", graphics.Quality);
        _config.SetConfig("graphics.fullscreen", graphics.Fullscreen);
        _config.SetConfig("graphics.resolutionWidth", graphics.ResolutionWidth);
        _config.SetConfig("graphics.resolutionHeight", graphics.ResolutionHeight);
        _config.SetConfig("graphics.vsync", graphics.VSync);
    }

    public GraphicsConfig LoadGraphicsConfig()
    {
        return new GraphicsConfig
        {
            Quality = _config.GetConfig("graphics.quality", 2),
            Fullscreen = _config.GetConfig("graphics.fullscreen", true),
            ResolutionWidth = _config.GetConfig("graphics.resolutionWidth", 1920),
            ResolutionHeight = _config.GetConfig("graphics.resolutionHeight", 1080),
            VSync = _config.GetConfig("graphics.vsync", true)
        };
    }
}
```

## 在架构中使用

### 注册为 Utility

```csharp
public class GameArchitecture : Architecture<GameArchitecture>
{
    protected override void Init()
    {
        // 注册配置管理器
        this.RegisterUtility<IConfigurationManager>(new ConfigurationManager());
    }
}
```

### 在 System 中使用

```csharp
public class SettingsSystem : AbstractSystem
{
    private IConfigurationManager _config;

    protected override void OnInit()
    {
        _config = this.GetUtility<IConfigurationManager>();

        // 加载配置
        LoadSettings();

        // 监听配置变化
        _config.WatchConfig<string>("game.language", OnLanguageChanged);
    }

    private void LoadSettings()
    {
        try
        {
            _config.LoadFromFile("settings.json");
        }
        catch
        {
            // 使用默认设置
            SetDefaultSettings();
        }
    }

    private void SetDefaultSettings()
    {
        _config.SetConfig("game.language", "en-US");
        _config.SetConfig("game.difficulty", "Normal");
        _config.SetConfig("audio.masterVolume", 1.0f);
    }

    private void OnLanguageChanged(string newLanguage)
    {
        // 切换游戏语言
        LocalizationManager.SetLanguage(newLanguage);
    }

    public void SaveSettings()
    {
        _config.SaveToFile("settings.json");
    }
}
```

### 在 Controller 中使用

```csharp
public class SettingsController : IController
{
    public IArchitecture GetArchitecture() => GameArchitecture.Interface;

    public void ApplyGraphicsSettings(int quality, bool fullscreen)
    {
        var config = this.GetUtility<IConfigurationManager>();

        // 更新配置（会自动触发监听器）
        config.SetConfig("graphics.quality", quality);
        config.SetConfig("graphics.fullscreen", fullscreen);

        // 保存配置
        SaveSettings();
    }

    public void ResetToDefaults()
    {
        var config = this.GetUtility<IConfigurationManager>();

        // 清空所有配置
        config.Clear();

        // 重新设置默认值
        config.SetConfig("audio.masterVolume", 1.0f);
        config.SetConfig("graphics.quality", 2);
        config.SetConfig("game.language", "en-US");

        SaveSettings();
    }

    private void SaveSettings()
    {
        var config = this.GetUtility<IConfigurationManager>();
        config.SaveToFile("settings.json");
    }
}
```

## 最佳实践

### 1. 配置键命名规范

使用分层的点号命名法，便于组织和管理：

```csharp
// 推荐的命名方式
_config.SetConfig("audio.master.volume", 1.0f);
_config.SetConfig("audio.music.volume", 0.8f);
_config.SetConfig("graphics.quality.level", 2);
_config.SetConfig("graphics.resolution.width", 1920);
_config.SetConfig("player.stats.health", 100);
_config.SetConfig("player.stats.mana", 50);

// 避免的命名方式
_config.SetConfig("AudioMasterVolume", 1.0f);  // 不使用驼峰命名
_config.SetConfig("vol", 1.0f);                // 不使用缩写
_config.SetConfig("config_1", 1.0f);           // 不使用无意义的名称
```

### 2. 使用默认值

始终为 `GetConfig` 提供合理的默认值，避免空引用：

```csharp
// 推荐
var volume = _config.GetConfig("audio.volume", 1.0f);
var quality = _config.GetConfig("graphics.quality", 2);

// 不推荐
var volume = _config.GetConfig<float>("audio.volume");  // 可能返回 0
if (volume == 0) volume = 1.0f;  // 需要额外的检查
```

### 3. 配置文件组织

将配置文件按环境和用途分类：

```
config/
├── default.json          # 默认配置
├── dev.json             # 开发环境配置
├── staging.json         # 测试环境配置
├── prod.json            # 生产环境配置
└── user/
    ├── settings.json    # 用户设置
    └── keybindings.json # 键位绑定
```

### 4. 配置安全

不要在配置中存储敏感信息：

```csharp
// 不要这样做
_config.SetConfig("api.key", "secret_key_12345");
_config.SetConfig("user.password", "password123");

// 应该使用专门的安全存储
SecureStorage.SetSecret("api.key", "secret_key_12345");
```

### 5. 监听器管理

及时注销不再需要的监听器，避免内存泄漏：

```csharp
public class MySystem : AbstractSystem
{
    private readonly List<IUnRegister> _watchers = new();

    protected override void OnInit()
    {
        var config = this.GetUtility<IConfigurationManager>();

        // 保存监听器引用
        _watchers.Add(config.WatchConfig<float>("audio.volume", OnVolumeChanged));
        _watchers.Add(config.WatchConfig<int>("graphics.quality", OnQualityChanged));
    }

    protected override void OnDestroy()
    {
        // 注销所有监听器
        foreach (var watcher in _watchers)
        {
            watcher.UnRegister();
        }
        _watchers.Clear();
    }
}
```

### 6. 线程安全使用

虽然 `ConfigurationManager` 是线程安全的，但在多线程环境中仍需注意：

```csharp
public class ThreadSafeConfigAccess
{
    private readonly IConfigurationManager _config;

    public void UpdateFromMultipleThreads()
    {
        // 可以安全地从多个线程访问
        Parallel.For(0, 10, i =>
        {
            _config.SetConfig($"thread.{i}.value", i);
            var value = _config.GetConfig($"thread.{i}.value", 0);
        });
    }

    public void WatchFromMultipleThreads()
    {
        // 监听器回调可能在不同线程执行
        _config.WatchConfig<int>("shared.value", newValue =>
        {
            // 确保线程安全的操作
            lock (_lockObject)
            {
                UpdateSharedResource(newValue);
            }
        });
    }

    private readonly object _lockObject = new();
    private void UpdateSharedResource(int value) { }
}
```

### 7. 配置变更通知

避免在配置监听器中触发大量的配置变更，可能导致循环调用：

```csharp
// 不推荐：可能导致无限循环
_config.WatchConfig<int>("value.a", a =>
{
    _config.SetConfig("value.b", a + 1);  // 触发 b 的监听器
});

_config.WatchConfig<int>("value.b", b =>
{
    _config.SetConfig("value.a", b + 1);  // 触发 a 的监听器
});

// 推荐：使用标志位避免循环
private bool _isUpdating = false;

_config.WatchConfig<int>("value.a", a =>
{
    if (_isUpdating) return;
    _isUpdating = true;
    _config.SetConfig("value.b", a + 1);
    _isUpdating = false;
});
```

## 常见问题

### Q1: 配置值类型转换失败怎么办？

A: `ConfigurationManager` 会尝试自动转换类型，如果失败会返回默认值。建议使用带默认值的 `GetConfig` 方法：

```csharp
// 如果转换失败，返回默认值 1.0f
var volume = _config.GetConfig("audio.volume", 1.0f);
```

### Q2: 如何处理配置文件不存在的情况？

A: 使用 try-catch 捕获异常，并提供默认配置：

```csharp
try
{
    _config.LoadFromFile("settings.json");
}
catch (FileNotFoundException)
{
    // 使用默认配置
    SetDefaultConfiguration();
    // 保存默认配置
    _config.SaveToFile("settings.json");
}
```

### Q3: 配置监听器何时被触发？

A: 只有当配置值真正发生变化时才会触发监听器。如果设置相同的值，监听器不会被触发：

```csharp
_config.SetConfig("key", 42);

_config.WatchConfig<int>("key", value =>
{
    Console.WriteLine($"Changed to: {value}");
});

_config.SetConfig("key", 42);   // 不会触发（值未变化）
_config.SetConfig("key", 100);  // 会触发
```

### Q4: 如何实现配置的版本控制？

A: 可以在配置中添加版本号，并在加载时进行迁移：

```csharp
public class ConfigurationMigration
{
    private readonly IConfigurationManager _config;

    public void LoadAndMigrate(string path)
    {
        _config.LoadFromFile(path);

        var version = _config.GetConfig("config.version", 1);

        if (version < 2)
        {
            MigrateToV2();
        }

        if (version < 3)
        {
            MigrateToV3();
        }

        _config.SetConfig("config.version", 3);
        _config.SaveToFile(path);
    }

    private void MigrateToV2()
    {
        // 迁移逻辑
        if (_config.HasConfig("old.key"))
        {
            var value = _config.GetConfig<string>("old.key");
            _config.SetConfig("new.key", value);
            _config.RemoveConfig("old.key");
        }
    }

    private void MigrateToV3() { }
}
```

### Q5: 配置管理器的性能如何？

A: `ConfigurationManager` 使用 `ConcurrentDictionary` 实现，具有良好的并发性能。但要注意：

- 避免频繁的文件 I/O 操作
- 监听器回调应保持轻量
- 大量配置项时考虑分组管理

```csharp
// 推荐：批量更新后一次性保存
_config.SetConfig("key1", value1);
_config.SetConfig("key2", value2);
_config.SetConfig("key3", value3);
_config.SaveToFile("settings.json");  // 一次性保存

// 不推荐：每次更新都保存
_config.SetConfig("key1", value1);
_config.SaveToFile("settings.json");
_config.SetConfig("key2", value2);
_config.SaveToFile("settings.json");
```

## 相关包

- [`architecture`](./architecture.md) - 配置管理器作为 Utility 注册到架构
- [`utility`](./utility.md) - 配置管理器实现 IUtility 接口
- [`events`](./events.md) - 配置变化可以触发事件
- [`logging`](./logging.md) - 配置管理器内部使用日志记录
