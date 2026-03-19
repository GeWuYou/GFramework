# Localization 本地化系统

## 概述

Localization 包提供了完整的多语言本地化支持，实现了游戏文本的国际化管理。通过本地化系统，可以轻松实现多语言切换、动态变量替换、回退机制等功能。

本地化系统是 GFramework 架构中的 System 层组件，与其他系统无缝集成，支持类型安全的 API 和流畅的使用体验。

## 核心接口

### ILocalizationManager

本地化管理器接口，继承自 `ISystem`，提供本地化的核心功能。

**核心属性：**

```csharp
string CurrentLanguage { get; }                    // 当前语言代码
CultureInfo CurrentCulture { get; }                // 当前文化信息
IReadOnlyList<string> AvailableLanguages { get; } // 可用语言列表
```

**核心方法：**

```csharp
void SetLanguage(string languageCode);                              // 设置当前语言
ILocalizationTable GetTable(string tableName);                      // 获取本地化表
string GetText(string table, string key);                           // 获取本地化文本
ILocalizationString GetString(string table, string key);            // 获取本地化字符串（支持变量）
bool TryGetText(string table, string key, out string text);         // 尝试获取文本
void RegisterFormatter(string name, ILocalizationFormatter formatter); // 注册格式化器
void SubscribeToLanguageChange(Action<string> callback);            // 订阅语言变化
void UnsubscribeFromLanguageChange(Action<string> callback);        // 取消订阅
```

### ILocalizationTable

本地化表接口，表示单个语言的本地化数据表。

**核心属性：**

```csharp
string Name { get; }                    // 表名
string Language { get; }                // 语言代码
ILocalizationTable? Fallback { get; }  // 回退表
```

**核心方法：**

```csharp
string GetRawText(string key);                                  // 获取原始文本
bool ContainsKey(string key);                                   // 检查键是否存在
IEnumerable<string> GetKeys();                                  // 获取所有键
void Merge(IReadOnlyDictionary<string, string> overrides);     // 合并覆盖数据
```

### ILocalizationString

本地化字符串接口，支持变量替换和格式化。

**核心属性：**

```csharp
string Table { get; }  // 表名
string Key { get; }    // 键名
```

**核心方法：**

```csharp
ILocalizationString WithVariable(string name, object value);                    // 添加变量
ILocalizationString WithVariables(params (string name, object value)[] variables); // 批量添加变量
string Format();                                                                 // 格式化并返回文本
string GetRaw();                                                                 // 获取原始文本
bool Exists();                                                                   // 检查键是否存在
```

### ILocalizationFormatter

格式化器接口，用于自定义变量格式化逻辑。

**核心属性：**

```csharp
string Name { get; }  // 格式化器名称
```

**核心方法：**

```csharp
bool TryFormat(string format, object value, IFormatProvider? provider, out string result);
```

## 配置类

### LocalizationConfig

本地化配置类，用于配置本地化系统的行为。

**配置属性：**

```csharp
string DefaultLanguage { get; set; }      // 默认语言代码，默认 "eng"
string FallbackLanguage { get; set; }     // 回退语言代码，默认 "eng"
string LocalizationPath { get; set; }     // 本地化文件路径，默认 "res://localization"
string OverridePath { get; set; }         // 用户覆盖路径，默认 "user://localization_override" (暂不支持)
bool EnableHotReload { get; set; }        // 是否启用热重载，默认 true (暂不支持)
bool ValidateOnLoad { get; set; }         // 是否在加载时验证，默认 true (暂不支持)
```

**注意：** `OverridePath`、`EnableHotReload` 和 `ValidateOnLoad` 配置项已定义但当前版本暂不支持，将在后续版本中实现。

## 文件组织

### 目录结构

```
res://localization/
├── eng/                    # 英文
│   ├── common.json        # 通用文本
│   ├── ui.json            # UI 文本
│   ├── cards.json         # 卡牌文本
│   └── ...
├── zhs/                    # 简体中文
│   ├── common.json
│   ├── ui.json
│   └── ...
└── ...

user://localization_override/  # 用户覆盖（可选）
├── eng/
└── zhs/
```

### JSON 文件格式

```json
{
  "game.title": "My Game",
  "game.version": "Version {version}",
  "ui.button.start": "Start Game",
  "ui.message.welcome": "Welcome, {playerName}!",
  "combat.damage": "Deal {damage} damage",
  "status.health": "Health: {current}/{max}"
}
```

**命名约定：**

- 使用点号分隔的层级结构（如 `ui.button.start`）
- 变量使用花括号包裹（如 `{playerName}`）
- 键名使用小写字母和点号

## 基本使用

### 初始化本地化管理器

```csharp
using GFramework.Core.Abstractions.Localization;
using GFramework.Core.Localization;

// 创建配置
var config = new LocalizationConfig
{
    DefaultLanguage = "eng",
    FallbackLanguage = "eng",
    LocalizationPath = "res://localization"
};

// 创建管理器
var locManager = new LocalizationManager(config);
locManager.Initialize();
```

### 在 Architecture 中注册

```csharp
public class GameArchitecture : Architecture<GameArchitecture>
{
    protected override void OnInit()
    {
        // 注册本地化管理器
        this.RegisterSystem<ILocalizationManager>(new LocalizationManager());
    }
}
```

### 获取本地化文本

```csharp
// 获取管理器
var locManager = this.GetSystem<ILocalizationManager>();

// 简单文本
string title = locManager.GetText("common", "game.title");
// 结果: "My Game"

// 安全获取
if (locManager.TryGetText("common", "game.title", out var text))
{
    Debug.Log(text);
}
```

### 使用变量

```csharp
// 单个变量
var message = locManager.GetString("common", "ui.message.welcome")
    .WithVariable("playerName", "Alice")
    .Format();
// 结果: "Welcome, Alice!"

// 多个变量
var health = locManager.GetString("common", "status.health")
    .WithVariable("current", 80)
    .WithVariable("max", 100)
    .Format();
// 结果: "Health: 80/100"

// 链式调用
var text = locManager.GetString("common", "game.version")
    .WithVariable("version", "1.0.0")
    .Format();
// 结果: "Version 1.0.0"
```

### 切换语言

```csharp
// 切换到简体中文
locManager.SetLanguage("zhs");

// 获取文本（自动使用新语言）
string title = locManager.GetText("common", "game.title");
// 结果: "我的游戏"

// 获取当前语言
string currentLang = locManager.CurrentLanguage;  // "zhs"

// 获取可用语言列表
var languages = locManager.AvailableLanguages;
foreach (var lang in languages)
{
    Debug.Log(lang);  // "eng", "zhs", ...
}
```

### 监听语言变化

```csharp
// 方式 1: 使用 lambda 表达式（无法取消订阅）
locManager.SubscribeToLanguageChange(language =>
{
    Debug.Log($"Language changed to: {language}");
    // 更新 UI、重新加载资源等
});

// 方式 2: 使用命名方法（推荐，可以取消订阅）
void OnLanguageChanged(string language)
{
    Debug.Log($"Language changed to: {language}");
    // 更新 UI、重新加载资源等
}

// 订阅
locManager.SubscribeToLanguageChange(OnLanguageChanged);

// 取消订阅（使用相同的方法引用）
locManager.UnsubscribeFromLanguageChange(OnLanguageChanged);
```

## 高级功能

### 回退机制

当目标语言缺少某个键时，系统会自动回退到默认语言：

```csharp
// 假设 zhs/common.json 中缺少 "new.feature" 键
locManager.SetLanguage("zhs");
var text = locManager.GetText("common", "new.feature");
// 自动从 eng/common.json 获取

// 回退顺序：
// 1. 当前语言的覆盖数据
// 2. 当前语言的原始数据
// 3. 回退语言的数据
```

### 覆盖机制

**注意：** 覆盖机制功能已规划但当前版本暂不支持，将在后续版本中实现。

未来版本中，用户可以在 `user://localization_override/` 目录下放置覆盖文件：

```json
// user://localization_override/eng/common.json
{
  "game.title": "My Custom Game Title"
}
```

覆盖文件会自动合并到主本地化表中，优先级最高。

### 自定义格式化器

```csharp
// 实现自定义格式化器
public class UpperCaseFormatter : ILocalizationFormatter
{
    public string Name => "upper";

    public bool TryFormat(string format, object value, IFormatProvider? provider, out string result)
    {
        result = value?.ToString()?.ToUpper() ?? string.Empty;
        return true;
    }
}

// 注册格式化器
locManager.RegisterFormatter("upper", new UpperCaseFormatter());

// 使用格式化器（需要在 LocalizationString 中实现格式化器支持）
// 格式: {variableName:formatterName:args}
```

### 内置格式化器

#### ConditionalFormatter

条件格式化器，根据布尔值选择不同文本。

```csharp
// 格式: {condition:if:trueText|falseText}
// JSON: "status": "{upgraded:if:Upgraded|Normal}"

var text = locManager.GetString("common", "status")
    .WithVariable("upgraded", true)
    .Format();
// 结果: "Upgraded"
```

#### PluralFormatter

复数格式化器，根据数量选择单复数形式。

```csharp
// 格式: {count:plural:singular|plural}
// JSON: "items": "{count:plural:item|items}"

var text = locManager.GetString("common", "items")
    .WithVariable("count", 1)
    .Format();
// 结果: "item"

var text2 = locManager.GetString("common", "items")
    .WithVariable("count", 3)
    .Format();
// 结果: "items"
```

## 异常处理

### LocalizationException

本地化异常基类。

### LocalizationKeyNotFoundException

当请求的键不存在时抛出。

```csharp
try
{
    var text = locManager.GetText("common", "nonexistent.key");
}
catch (LocalizationKeyNotFoundException ex)
{
    Debug.LogError($"Key not found: {ex.TableName}.{ex.Key}");
}
```

### LocalizationTableNotFoundException

当请求的表不存在时抛出。

```csharp
try
{
    var table = locManager.GetTable("nonexistent_table");
}
catch (LocalizationTableNotFoundException ex)
{
    Debug.LogError($"Table not found: {ex.TableName}");
}
```

## 最佳实践

### 1. 键名组织

```csharp
// 推荐：使用层级结构
"ui.button.start"
"ui.button.quit"
"combat.damage.physical"
"combat.damage.magical"

// 不推荐：扁平结构
"start_button"
"quit_button"
```

### 2. 变量命名

```csharp
// 推荐：使用驼峰命名
"{playerName}"
"{maxHealth}"

// 不推荐：使用下划线或大写
"{player_name}"
"{MAX_HEALTH}"
```

### 3. 表的划分

```csharp
// 按功能模块划分表
common.json    // 通用文本
ui.json        // UI 文本
combat.json    // 战斗文本
items.json     // 物品文本
```

### 4. 安全获取

```csharp
// 推荐：使用 TryGetText 避免异常
if (locManager.TryGetText("common", "key", out var text))
{
    // 使用 text
}

// 或者提供默认值
var text = locManager.TryGetText("common", "key", out var result)
    ? result
    : "Default Text";
```

### 5. 语言变化处理

```csharp
// 在组件初始化时订阅
public override void OnInit()
{
    var locManager = this.GetSystem<ILocalizationManager>();
    locManager.SubscribeToLanguageChange(OnLanguageChanged);
}

// 在组件销毁时取消订阅
public override void OnDestroy()
{
    var locManager = this.GetSystem<ILocalizationManager>();
    locManager.UnsubscribeFromLanguageChange(OnLanguageChanged);
}

private void OnLanguageChanged(string language)
{
    // 更新 UI
    UpdateUI();
}
```

## 性能考虑

### 缓存策略

- 本地化表在加载后会缓存在内存中
- 语言切换时只加载新语言的表
- 建议在游戏启动时预加载常用语言

### 内存优化

```csharp
// 只加载当前语言，不预加载所有语言
var config = new LocalizationConfig
{
    DefaultLanguage = "eng"
};

// 按需切换语言
locManager.SetLanguage(userSelectedLanguage);
```

## 相关资源

- [Architecture 架构系统](./architecture.md)
- [System 系统层](./system.md)
- [Configuration 配置管理](./configuration.md)
