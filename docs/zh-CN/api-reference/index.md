# API 参考文档

本文档提供 GFramework 各模块的完整 API 参考。

## 核心命名空间

### GFramework.Core.architecture

核心架构命名空间，包含所有基础组件。

#### 主要类型

| 类型                 | 说明     |
|--------------------|--------|
| `Architecture`     | 应用架构基类 |
| `AbstractModel`    | 数据模型基类 |
| `AbstractSystem`   | 业务系统基类 |
| `AbstractCommand`  | 命令基类   |
| `AbstractQuery<T>` | 查询基类   |
| `IController`      | 控制器接口  |
| `IUtility`         | 工具类接口  |

### GFramework.Core.events

事件系统命名空间。

#### 主要类型

| 类型                | 说明       |
|-------------------|----------|
| `IEvent`          | 事件接口     |
| `IEventSystem`    | 事件系统接口   |
| `TypeEventSystem` | 类型安全事件系统 |

### GFramework.Core.property

属性系统命名空间。

#### 主要类型

| 类型                    | 说明     |
|-----------------------|--------|
| `BindableProperty<T>` | 可绑定属性  |
| `IUnRegister`         | 注销接口   |
| `IUnRegisterList`     | 注销列表接口 |

### GFramework.Core.ioc

IoC 容器命名空间。

#### 主要类型

| 类型           | 说明   |
|--------------|------|
| `IContainer` | 容器接口 |
| `Container`  | 容器实现 |

### GFramework.Core.pool

对象池命名空间。

#### 主要类型

| 类型               | 说明    |
|------------------|-------|
| `IObjectPool<T>` | 对象池接口 |
| `ObjectPool<T>`  | 对象池实现 |

### GFramework.Core.Localization

本地化系统命名空间。

#### 主要类型

| 类型                       | 说明       |
|--------------------------|----------|
| `ILocalizationManager`   | 本地化管理器接口 |
| `ILocalizationTable`     | 本地化表接口   |
| `ILocalizationString`    | 本地化字符串接口 |
| `ILocalizationFormatter` | 格式化器接口   |
| `LocalizationConfig`     | 本地化配置类   |
| `LocalizationManager`    | 本地化管理器实现 |
| `LocalizationTable`      | 本地化表实现   |
| `LocalizationString`     | 本地化字符串实现 |

## 常用 API

### Architecture

```csharp
public abstract class Architecture : IBelongToArchitecture
{
    // 初始化架构
    public void Initialize();

    // 销毁架构
    public void Destroy();

    // 注册模型
    public void RegisterModel<T>(T model) where T : IModel;

    // 获取模型
    public T GetModel<T>() where T : IModel;

    // 注册系统
    public void RegisterSystem<T>(T system) where T : ISystem;

    // 获取系统
    public T GetSystem<T>() where T : ISystem;

    // 注册工具
    public void RegisterUtility<T>(T utility) where T : IUtility;

    // 获取工具
    public T GetUt>() where T : IUtility;

    // 发送命令
    public void SendCommand<T>(T command) where T : ICommand;

    // 发送查询
    public TResult SendQuery<TQuery, TResult>(TQuery query)
        where TQuery : IQuery<TResult>;

    // 发送事件
    public void SendEvent<T>(T e) where T : IEvent;
}
```

### AbstractModel

```csharp
public abstract class AbstractModel : IBelongToArchitecture
{
    // 初始化模型
    protected abstract void OnInit();

    // 销毁模型
    protected virtual void OnDestroy();

    // 获取架构
    public IArchitecture GetArchitecture();

    // 发送事件
    protected void SendEvent<T>(T e) where T : IEvent;

    // 获取模型
    protected T GetModel<T>() where T : IModel;

    // 获取系统
    protected T GetSystem<T>() where T : ISystem;

    // 获取工具
    protected T GetUtility<T>() where T : IUtility;
}
```

### AbstractSystem

```csharp
public abstract class AbstractSystem : IBelongToArchitecture
{
    // 初始化系统
    protected abstract void OnInit();

    // 销毁系统
    protected virtual void OnDestroy();

    // 获取架构
    public IArchitecture GetArchitecture();

    // 发送事件
    protected void SendEvent<T>(T e) where T : IEvent;

    // 注册事件
    protected IUnRegister RegisterEvent<T>(Action<T> onEvent)
        where T : IEvent;

    // 获取模型
    protected T GetModel<T>() where T : IModel;

    // 获取系统
    protected T GetSystem<T>() where T : ISystem;

    // 获取工具
    protected T GetUtility<T>() where T : IUtility;
}
```

### AbstractCommand

```csharp
public abstract class AbstractCommand : IBelongToArchitecture
{
    // 执行命令
    public void Execute();

    // 命令实现
    protected abstract void OnDo();

    // 获取架构
    public IArchitecture GetArchitecture();

    // 发送事件
    protected void SendEvent<T>(T e) where T : IEvent;

    // 获取模型
    protected T GetModel<T>() where T : IModel;

    // 获取系统
    protected T GetSystem<T>() where T : ISystem;

    // 获取工具
    protected T GetUtility<T>() where T : IUtility;
}
```

### AbstractQuery`<T>`

```csharp
public abstract class AbstractQuery<T> : IBelongToArchitecture
{
    // 执行查询
    public T Do();

    // 查询实现
    protected abstract T OnDo();

    // 获取架构
    public IArchitecture GetArchitecture();

    // 获取模型
    protected T GetModel<T>() where T : IModel;

    // 获取系统
    protected T GetSystem<T>() where T : ISystem;

    // 获取工具
    protected T GetUtility<T>() where T : IUtility;
}
```

### BindableProperty`<T>`

```csharp
public class BindableProperty<T>
{
    // 构造函数
    public BindableProperty(T initialValue = default);

    // 获取或设置值
    public T Value { get; set; }

    // 注册监听器
    public IUnRegister Register(Action<T> onValueChanged);

    // 注册监听器（包含初始值）
    public IUnRegister RegisterWithInitValue(Action<T> onValueChanged);

    // 获取当前值
    public T GetValue();

    // 设置值
    public void SetValue(T newValue);
}
```

### ILocalizationManager

```csharp
public interface ILocalizationManager : ISystem
{
    // 获取当前语言代码
    string CurrentLanguage { get; }

    // 获取当前文化信息
    CultureInfo CurrentCulture { get; }

    // 获取可用语言列表
    IReadOnlyList<string> AvailableLanguages { get; }

    // 设置当前语言
    void SetLanguage(string languageCode);

    // 获取本地化表
    ILocalizationTable GetTable(string tableName);

    // 获取本地化文本
    string GetText(string table, string key);

    // 获取本地化字符串（支持变量）
    ILocalizationString GetString(string table, string key);

    // 尝试获取本地化文本
    bool TryGetText(string table, string key, out string text);

    // 注册格式化器
    void RegisterFormatter(string name, ILocalizationFormatter formatter);

    // 订阅语言变化事件
    void SubscribeToLanguageChange(Action<string> callback);

    // 取消订阅语言变化事件
    void UnsubscribeFromLanguageChange(Action<string> callback);
}
```

### ILocalizationString

```csharp
public interface ILocalizationString
{
    // 获取表名
    string Table { get; }

    // 获取键名
    string Key { get; }

    // 添加变量
    ILocalizationString WithVariable(string name, object value);

    // 批量添加变量
    ILocalizationString WithVariables(params (string name, object value)[] variables);

    // 格式化并返回文本
    string Format();

    // 获取原始文本
    string GetRaw();

    // 检查键是否存在
    bool Exists();
}
```

### LocalizationConfig

```csharp
public class LocalizationConfig
{
    // 默认语言代码
    public string DefaultLanguage { get; set; } = "eng";

    // 回退语言代码
    public string FallbackLanguage { get; set; } = "eng";

    // 本地化文件路径
    public string LocalizationPath { get; set; } = "res://localization";

    // 用户覆盖路径
    public string OverridePath { get; set; } = "user://localization_override";

    // 是否启用热重载
    public bool EnableHotReload { get; set; } = true;

    // 是否在加载时验证
    public bool ValidateOnLoad { get; set; } = true;
}
```

## 扩展方法

### 架构扩展

```csharp
// 发送命令
public static void SendCommand<T>(this IBelongToArchitecture self, T command)
    where T : ICommand;

// 发送查询
public static TResult SendQuery<TQuery, TResult>(
    this IBelongToArchitecture self, TQuery query)
    where TQuery : IQuery<TResult>;

// 发送事件
public static void SendEvent<T>(this IBelongToArchitecture self, T e)
    where T : IEvent;

// 获取模型
public static T GetModel<T>(this IBelongToArchitecture self)
    where T : IModel;

// 获取系统
public static T GetSystem<T>(this IBelongToArchitecture self)
    where T : ISystem;

// 获取工具
public static T GetUtility<T>(this IBelongToArchitecture self)
    where T : IUtility;

// 注册事件
public static IUnRegister RegisterEvent<T>(
    this IBelongToArchitecture self, Action<T> onEvent)
    where T : IEvent;
```

### 属性扩展

```csharp
// 添加到注销列表
public static IUnRegister AddToUnregisterList(
    this IUnRegister self, IUnRegisterList list);

// 注销所有
public static void UnRegisterAll(this IUnRegisterList self);
```

## 游戏模块 API

### GFramework.Game

游戏业务扩展模块。

#### 主要类型

| 类型            | 说明     |
|---------------|--------|
| `GameSetting` | 游戏设置   |
| `GameState`   | 游戏状态   |
| `IGameModule` | 游戏模块接口 |

## Godot 集成 API

### GFramework.Godot

Godot 引擎集成模块。

#### 主要类型

| 类型               | 说明         |
|------------------|------------|
| `GodotNode`      | Godot 节点扩展 |
| `GodotCoroutine` | Godot 协程   |
| `GodotSignal`    | Godot 信号   |

## 源码生成器

### GFramework.SourceGenerators

自动代码生成工具。

#### 支持的生成器

| 生成器                                        | 说明          |
|--------------------------------------------|-------------|
| `LoggingGenerator`                         | 日志生成器       |
| `EnumGenerator`                            | 枚举扩展生成器     |
| `RuleGenerator`                            | 规则生成器       |
| `AutoRegisterModuleGenerator`              | 架构模块注册生成器   |
| `AutoUiPageGenerator`                      | UI 页面行为生成器  |
| `AutoSceneGenerator`                       | 场景行为生成器     |
| `AutoRegisterExportedCollectionsGenerator` | 导出集合批量注册生成器 |

#### 常用 Attribute

| Attribute                                  | 说明                                | 文档                                                                                                          |
|--------------------------------------------|-----------------------------------|-------------------------------------------------------------------------------------------------------------|
| `AutoRegisterModuleAttribute`              | 为模块类生成 `Install(IArchitecture)`   | [AutoRegisterModule 生成器](../source-generators/auto-register-module-generator.md)                            |
| `RegisterModelAttribute`                   | 声明模块内自动注册的 `IModel` 类型            | [AutoRegisterModule 生成器](../source-generators/auto-register-module-generator.md)                            |
| `RegisterSystemAttribute`                  | 声明模块内自动注册的 `ISystem` 类型           | [AutoRegisterModule 生成器](../source-generators/auto-register-module-generator.md)                            |
| `RegisterUtilityAttribute`                 | 声明模块内自动注册的 `IUtility` 类型          | [AutoRegisterModule 生成器](../source-generators/auto-register-module-generator.md)                            |
| `AutoUiPageAttribute`                      | 为 `CanvasItem` 页面节点生成 `GetPage()` | [AutoUiPage 生成器](../source-generators/auto-ui-page-generator.md)                                            |
| `AutoSceneAttribute`                       | 为场景根节点生成 `GetScene()`             | [AutoScene 生成器](../source-generators/auto-scene-generator.md)                                               |
| `AutoRegisterExportedCollectionsAttribute` | 为宿主类开启导出集合批量注册生成                  | [AutoRegisterExportedCollections 生成器](../source-generators/auto-register-exported-collections-generator.md) |
| `RegisterExportedCollectionAttribute`      | 指定集合与注册器成员的映射关系                   | [AutoRegisterExportedCollections 生成器](../source-generators/auto-register-exported-collections-generator.md) |

## 常见用法示例

### 创建架构

```csharp
public class MyArchitecture : Architecture
{
    protected override void Init()
    {
        RegisterModel(new PlayerModel());
        RegisterSystem(new PlayerSystem());
        RegisterUtility(new StorageUtility());
    }
}

// 使用
var arch = new MyArchitecture();
arch.Initialize();
```

### 发送命令

```csharp
public class AttackCommand : AbstractCommand
{
    public int Damage { get; set; }

    protected override void OnDo()
    {
        var player = this.GetModel<PlayerModel>();
        this.SendEvent(new AttackEvent { Damage = Damage });
    }
}

// 使用
arch.SendCommand(new AttackCommand { Damage = 10 });
```

### 发送查询

```csharp
public class GetPlayerHealthQuery : AbstractQuery<int>
{
    protected override int OnDo()
    {
        return this.GetModel<PlayerModel>().Health.Value;
    }
}

// 使用
var health = arch.SendQuery(new GetPlayerHealthQuery());
```

### 监听事件

```csharp
public class PlayerSystem : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent<PlayerDiedEvent>(OnPlayerDied);
    }

    private void OnPlayerDied(PlayerDiedEvent e)
    {
        Console.WriteLine("Player died!");
    }
}
```

### 使用本地化

```csharp
// 初始化本地化管理器
var config = new LocalizationConfig
{
    DefaultLanguage = "eng",
    LocalizationPath = "res://localization"
};
var locManager = new LocalizationManager(config);
locManager.Initialize();

// 获取简单文本
string title = locManager.GetText("common", "game.title");

// 使用变量
var message = locManager.GetString("common", "ui.message.welcome")
    .WithVariable("playerName", "Alice")
    .Format();

// 切换语言
locManager.SetLanguage("zhs");

// 监听语言变化
locManager.SubscribeToLanguageChange(language =>
{
    Console.WriteLine($"Language changed to: {language}");
});
```

---

更多详情请查看各模块的详细文档。
