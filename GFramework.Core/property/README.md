# Property 包使用说明

## 概述

Property 包提供了可绑定属性（BindableProperty）的实现，支持属性值的监听和响应式编程。这是实现 MVVM 模式和数据绑定的核心组件。

## 核心接口

### [`IReadonlyBindableProperty<T>`](IReadonlyBindableProperty.cs)

只读可绑定属性接口，提供属性值的读取和变更监听功能。

**核心成员：**
```csharp
T Value { get; }  // 获取属性值
IUnRegister Register(Action<T> onValueChanged);  // 注册监听
IUnRegister RegisterWithInitValue(Action<T> action);  // 注册并立即回调当前值
void UnRegister(Action<T> onValueChanged);  // 取消监听
```

### [`IBindableProperty<T>`](IBindableProperty.cs)

可绑定属性接口，继承自只读接口，增加了修改能力。

**核心成员：**
```csharp
new T Value { get; set; }  // 可读写的属性值
void SetValueWithoutEvent(T newValue);  // 设置值但不触发事件
```

## 核心类

### [`BindableProperty<T>`](BindableProperty.cs)

可绑定属性的完整实现。

**使用示例：**

```csharp
// 创建可绑定属性
var health = new BindableProperty<int>(100);

// 监听值变化
var unregister = health.Register(newValue =>
{
    GD.Print($"Health changed to: {newValue}");
});

// 修改值（会触发监听器）
health.Value = 50;  // 输出: Health changed to: 50

// 取消监听
unregister.UnRegister();

// 设置值但不触发事件
health.SetValueWithoutEvent(75);
```

**高级功能：**

```csharp
// 1. 注册并立即获得当前值
health.RegisterWithInitValue(value =>
{
    GD.Print($"Current health: {value}");  // 立即输出当前值
    // 后续值变化时也会调用
});

// 2. 自定义比较器
var position = new BindableProperty<Vector3>(Vector3.Zero)
    .WithComparer((a, b) => a.DistanceTo(b) < 0.01f);  // 距离小于0.01认为相等

// 3. 链式调用
health.Value = 100;
```

### [`BindablePropertyUnRegister<T>`](BindablePropertyUnRegister.cs)

可绑定属性的注销器，负责清理监听。

## 在 Model 中使用

### 定义可绑定属性

```csharp
public class PlayerModel : AbstractModel
{
    // 可读写属性
    public BindableProperty<string> Name { get; } = new("Player");
    public BindableProperty<int> Level { get; } = new(1);
    public BindableProperty<int> Health { get; } = new(100);
    public BindableProperty<int> MaxHealth { get; } = new(100);
    
    // 只读属性（外部只能读取和监听）
    public IReadonlyBindableProperty<int> ReadonlyHealth => Health;
    
    protected override void OnInit()
    {
        // 内部监听属性变化
        Health.Register(hp =>
        {
            if (hp <= 0)
            {
                this.SendEvent(new PlayerDiedEvent());
            }
        });
    }
}
```

## 在 Controller 中监听

### UI 数据绑定

```csharp
public partial class PlayerUI : Control, IController
{
    [Export] private Label _healthLabel;
    [Export] private Label _nameLabel;
    
    private IUnRegisterList _unregisterList = new UnRegisterList();
    
    public IArchitecture GetArchitecture() => GameArchitecture.Interface;
    
    public override void _Ready()
    {
        var playerModel = this.GetModel<PlayerModel>();
        
        // 绑定生命值到UI（立即显示当前值）
        playerModel.Health
            .RegisterWithInitValue(health =>
            {
                _healthLabel.Text = $"HP: {health}/{playerModel.MaxHealth.Value}";
            })
            .AddToUnregisterList(_unregisterList);
        
        // 绑定名称
        playerModel.Name
            .RegisterWithInitValue(name =>
            {
                _nameLabel.Text = name;
            })
            .AddToUnregisterList(_unregisterList);
    }
    
    public override void _ExitTree()
    {
        _unregisterList.UnRegisterAll();
    }
}
```

## 常见使用模式

### 1. 双向绑定

```csharp
// Model
public class SettingsModel : AbstractModel
{
    public BindableProperty<float> MasterVolume { get; } = new(1.0f);
    protected override void OnInit() { }
}

// UI Controller
public partial class VolumeSlider : HSlider, IController
{
    private BindableProperty<float> _volumeProperty;
    
    public override void _Ready()
    {
        _volumeProperty = this.GetModel<SettingsModel>().MasterVolume;
        
        // Model -> UI
        _volumeProperty.RegisterWithInitValue(vol => Value = vol)
            .UnRegisterWhenNodeExitTree(this);
        
        // UI -> Model
        ValueChanged += newValue => _volumeProperty.Value = (float)newValue;
    }
}
```

### 2. 计算属性

```csharp
public class PlayerModel : AbstractModel
{
    public BindableProperty<int> Health { get; } = new(100);
    public BindableProperty<int> MaxHealth { get; } = new(100);
    public BindableProperty<float> HealthPercent { get; } = new(1.0f);
    
    protected override void OnInit()
    {
        // 自动计算百分比
        Action updatePercent = () =>
        {
            HealthPercent.Value = (float)Health.Value / MaxHealth.Value;
        };
        
        Health.Register(_ => updatePercent());
        MaxHealth.Register(_ => updatePercent());
        
        updatePercent();  // 初始计算
    }
}
```

### 3. 属性验证

```csharp
public class PlayerModel : AbstractModel
{
    private BindableProperty<int> _health = new(100);
    
    public BindableProperty<int> Health
    {
        get => _health;
        set
        {
            // 限制范围
            var clampedValue = Math.Clamp(value.Value, 0, MaxHealth.Value);
            _health.Value = clampedValue;
        }
    }
    
    public BindableProperty<int> MaxHealth { get; } = new(100);
    
    protected override void OnInit() { }
}
```

### 4. 条件监听

```csharp
public class CombatController : Node, IController
{
    public override void _Ready()
    {
        var playerModel = this.GetModel<PlayerModel>();
        
        // 只在生命值低于30%时显示警告
        playerModel.Health.Register(hp =>
        {
            if (hp < playerModel.MaxHealth.Value * 0.3f)
            {
                ShowLowHealthWarning();
            }
            else
            {
                HideLowHealthWarning();
            }
        }).UnRegisterWhenNodeExitTree(this);
    }
}
```

## 性能优化

### 1. 避免频繁触发

```csharp
// 使用 SetValueWithoutEvent 批量修改
public void LoadPlayerData(SaveData data)
{
    // 临时关闭事件
    Health.SetValueWithoutEvent(data.Health);
    Mana.SetValueWithoutEvent(data.Mana);
    Gold.SetValueWithoutEvent(data.Gold);
    
    // 最后统一触发一次更新事件
    this.SendEvent(new PlayerDataLoadedEvent());
}
```

### 2. 自定义比较器

```csharp
// 避免浮点数精度问题导致的频繁触发
var position = new BindableProperty<Vector3>()
    .WithComparer((a, b) => a.DistanceTo(b) < 0.001f);
```

## 最佳实践

1. **在 Model 中定义属性** - BindableProperty 主要用于 Model 层
2. **使用只读接口暴露** - 防止外部随意修改
3. **及时注销监听** - 使用 UnRegisterList 或 UnRegisterWhenNodeExitTree
4. **使用 RegisterWithInitValue** - UI 绑定时立即获取初始值
5. **避免循环依赖** - 属性监听器中修改其他属性要小心

## 相关包

- [`model`](../model/README.md) - Model 中大量使用 BindableProperty
- [`events`](../events/README.md) - BindableProperty 基于事件系统实现
- [`controller`](../controller/README.md) - Controller 监听属性变化更新 UI
- [`extensions`](../extensions/README.md) - 提供便捷的注销扩展方法