# Godot 协程系统

> GFramework 在 Godot 引擎中的协程支持，实现异步操作的优雅管理

## 概述

GFramework.Godot 提供了与 Godot 引擎深度集成的协程系统，让异步编程变得简单直观。通过协程，您可以暂停执行、等待条件满足、或延迟执行操作，而不会阻塞主线程。

## 核心特性

- **无缝集成**：与 Godot 的 `_Process`、`_Ready` 等生命周期方法完美配合
- **类型安全**：强类型的协程返回结果处理
- **自动清理**：协程与节点生命周期自动绑定，避免内存泄漏
- **丰富的等待条件**：支持等待信号、时间延迟、帧结束等多种条件

## 基本用法

### 创建协程

使用 `StartCoroutine` 方法启动协程：

```csharp
using GFramework.Godot.coroutine;

[ContextAware]
public partial class MyNode : Node
{
    public override void _Ready()
    {
        // 启动协程
        this.StartCoroutine(DoSomethingAsync());
    }

    private System.Collections.IEnumerator DoSomethingAsync()
    {
        GD.Print("开始执行");
        
        // 等待 2 秒
        yield return new WaitForSeconds(2.0f);
        
        GD.Print("2 秒后继续执行");
        
        // 等待下一帧
        yield return new WaitForEndOfFrame();
        
        GD.Print("下一帧继续");
    }
}
```

### 等待信号

协程可以等待 Godot 信号：

```csharp
private System.Collections.IEnumerator WaitForSignalExample()
{
    GD.Print("等待按钮点击");
    
    // 等待按钮被点击
    var button = GetNode<Button>("Button");
    yield return new WaitSignal(button, Button.SignalName.Pressed);
    
    GD.Print("按钮被点击了！");
}
```

### 等待条件

等待自定义条件满足：

```csharp
private System.Collections.IEnumerator WaitUntilCondition()
{
    GD.Print("等待生命值恢复");
    
    // 等待生命值大于 50
    var playerModel = Context.GetModel<PlayerModel>();
    yield return new WaitUntil(() => playerModel.Health.Value > 50);
    
    GD.Print("生命值已恢复！");
}
```

## 等待类型

### WaitForSeconds

等待指定时间（秒）：

```csharp
private System.Collections.IEnumerator DelayExample()
{
    GD.Print("开始倒计时");
    
    yield return new WaitForSeconds(1.0f);
    GD.Print("1 秒过去了");
    
    yield return new WaitForSeconds(0.5f);
    GD.Print("又过去了 0.5 秒");
}
```

### WaitForSecondsRealtime

等待实时时间（不受游戏暂停影响）：

```csharp
private System.Collections.IEnumerator RealTimeDelay()
{
    // 暂停游戏时也会继续计时
    yield return new WaitForSecondsRealtime(5.0f);
    
    GD.Print("5 秒真实时间已过");
}
```

### WaitForEndOfFrame

等待当前帧结束：

```csharp
private System.Collections.IEnumerator EndOfFrameExample()
{
    // 修改数据
    someData.Value = 100;
    
    // 等待帧结束后再执行渲染相关操作
    yield return new WaitForEndOfFrame();
    
    // 现在可以安全地执行渲染操作
    UpdateRendering();
}
```

### WaitUntil

等待条件满足：

```csharp
private System.Collections.IEnumerator WaitUntilExample()
{
    var health = Context.GetModel<PlayerModel>().Health;
    
    // 持续等待直到条件满足
    yield return new WaitUntil(() => health.Value > 0);
    
    GD.Print("玩家复活了！");
}
```

### WaitWhile

等待条件不再满足：

```csharp
private System.Collections.IEnumerator WaitWhileExample()
{
    var gameState = Context.GetModel<GameModel>();
    
    // 等待游戏不再暂停
    yield return new WaitWhile(() => gameState.IsPaused.Value);
    
    GD.Print("游戏继续");
}
```

## 进阶用法

### 组合等待

可以组合多种等待条件：

```csharp
private System.Collections.IEnumerator CombinedWait()
{
    var health = Context.GetModel<PlayerModel>().Health;
    var button = GetNode<Button>("Button");
    
    // 等待生命值恢复或按钮点击（任一条件满足即可）
    yield return new WaitAny(
        new WaitUntil(() => health.Value > 50),
        new WaitSignal(button, Button.SignalName.Pressed)
    );
    
    GD.Print("条件满足，继续执行");
}
```

### 超时处理

为等待添加超时：

```csharp
private System.Collections.IEnumerator WithTimeout()
{
    var task = new WaitForSeconds(5.0f);
    var timeout = new WaitForSeconds(5.0f);
    
    // 等待任务完成，最多等 5 秒
    bool completed = yield return new WaitRace(task, timeout);
    
    if (completed)
    {
        GD.Print("任务完成");
    }
    else
    {
        GD.Print("任务超时");
    }
}
```

### 协程取消

支持取消正在执行的协程：

```csharp
private CoroutineHandle _coroutine;

public override void _Ready()
{
    _coroutine = this.StartCoroutine(LongRunningTask());
}

public void CancelTask()
{
    _coroutine?.Cancel();
}

private System.Collections.IEnumerator LongRunningTask()
{
    try
    {
        for (int i = 0; i < 100; i++)
        {
            GD.Print($"进度: {i}%");
            yield return new WaitForSeconds(0.1f);
        }
    }
    catch (CoroutineCancelledException)
    {
        GD.Print("协程被取消");
        throw;
    }
}
```

## 最佳实践

### 1. 自动生命周期管理

使用 `[ContextAware]` 特性确保协程在节点离开场景树时自动取消：

```csharp
[ContextAware]
public partial class MyController : Node
{
    public override void _Ready()
    {
        // 当节点离开场景树时，协程会自动取消
        this.StartCoroutine(AutoCleanupCoroutine());
    }
}
```

### 2. 避免在协程中直接修改 UI

```csharp
// 不推荐：直接在协程中频繁更新 UI
private System.Collections.IEnumerator BadExample()
{
    for (int i = 0; i < 100; i++)
    {
        label.Text = $"进度: {i}"; // 可能导致性能问题
        yield return new WaitForEndOfFrame();
    }
}

// 推荐：使用 BindableProperty 自动更新
private System.Collections.IEnumerator GoodExample()
{
    var progress = new BindableProperty<int>(0);
    
    // 使用 BindableProperty 注册 UI 更新
    progress.Register(value => label.Text = $"进度: {value}")
        .UnRegisterWhenNodeExitTree(this);
    
    for (int i = 0; i < 100; i++)
    {
        progress.Value = i; // 自动更新 UI
        yield return new WaitForEndOfFrame();
    }
}
```

### 3. 使用协程进行资源加载

```csharp
private System.Collections.IEnumerator LoadResourcesAsync()
{
    GD.Print("开始加载资源");
    
    // 显示加载界面
    loadingScreen.Visible = true;
    
    // 异步加载资源
    var textures = new List<Texture2D>();
    foreach (var path in resourcePaths)
    {
        var texture = ResourceLoader.LoadThreadedGet<Texture2D>(path);
        
        // 等待每张图片加载完成
        yield return new WaitUntil(() => texture.GetLoadingStatus() == ResourceLoader.Loaded);
        
        textures.Add(texture);
        
        // 更新加载进度
        UpdateProgress(textures.Count, resourcePaths.Length);
    }
    
    // 加载完成
    loadingScreen.Visible = false;
    OnResourcesLoaded(textures);
}
```

### 4. 场景切换处理

```csharp
private System.Collections.IEnumerator SceneTransitionAsync()
{
    GD.Print("开始场景切换");
    
    // 淡出当前场景
    fadeAnimation.Play("FadeOut");
    yield return new WaitSignal(fadeAnimation, AnimationPlayer.SignalName.AnimationFinished);
    
    // 卸载当前场景
    GetTree().CurrentScene.QueueFree();
    
    // 加载新场景
    var nextScene = ResourceLoader.Load<PackedScene>("res://scenes/NextScene.tscn");
    var instance = nextScene.Instantiate();
    GetTree().Root.AddChild(instance);
    
    // 淡入新场景
    fadeAnimation.Play("FadeIn");
    yield return new WaitSignal(fadeAnimation, AnimationPlayer.SignalName.AnimationFinished);
    
    GD.Print("场景切换完成");
}
```

## 与 Source Generators 集成

GFramework.SourceGenerators 可以自动为您的节点生成协程相关代码：

```csharp
[Log]
[ContextAware]
public partial class MyNode : Node
{
    // Source Generator 会自动生成 Logger 字段
    // 无需手动编写日志代码
    
    public override void _Ready()
    {
        Logger.Info("节点已准备就绪");
        
        this.StartCoroutine(ComplexAsyncOperation());
    }
    
    private System.Collections.IEnumerator ComplexAsyncOperation()
    {
        Logger.Debug("开始复杂异步操作");
        
        yield return new WaitForSeconds(1.0f);
        
        Logger.Debug("操作完成");
    }
}
```

## 常见问题

### Q: 协程会在游戏暂停时继续执行吗？

A: 默认情况下，`WaitForSeconds` 会受到游戏暂停的影响。如果您需要在暂停时继续计时，请使用 `WaitForSecondsRealtime`。

### Q: 如何调试协程？

A: 您可以在协程内部使用 `GD.Print()` 或 `Logger.Debug()` 来输出调试信息。VS Code 和 Rider 也支持在协程中设置断点。

### Q: 协程中出现异常会怎样？

A: 未捕获的异常会导致协程停止执行，并可能传播到调用方。建议使用 try-catch 包装可能抛出异常的代码。

---

**相关文档**：

- [Godot 概述](./overview)
- [Node 扩展方法](./extensions)
- [信号系统](./signal)
- [事件系统](../core/events)
