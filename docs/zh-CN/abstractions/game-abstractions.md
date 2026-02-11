# Game Abstractions

> GFramework.Game.Abstractions 游戏模块抽象接口定义

## 概述

GFramework.Game.Abstractions 包含了游戏特定功能的抽象接口，这些接口定义了游戏开发中的通用契约。

## 存档接口

### ISaveSystem

存档系统接口：

```csharp
public interface ISaveSystem
{
    void Save(string slotId, SaveData data);
    SaveData Load(string slotId);
    bool HasSave(string slotId);
    void Delete(string slotId);
    List<SaveSlotInfo> GetAllSaveSlots();
}
```

### ISaveData

存档数据接口：

```csharp
public interface ISaveData
{
    int Version { get; }
    DateTime Timestamp { get; }
    void Validate();
}
```

## 设置接口

### IGameSettings

游戏设置接口：

```csharp
public interface IGameSettings
{
    AudioSettings Audio { get; }
    GraphicsSettings Graphics { get; }
    InputSettings Input { get; }
    
    void Save();
    void Load();
    void ResetToDefaults();
}
```

## 场景管理接口

### ISceneManager

场景管理器接口：

```csharp
public interface ISceneManager
{
    void SwitchScene<TScene>() where TScene : IScene;
    Task SwitchSceneAsync<TScene>() where TScnee : IScene;
    
    void PushScene<TScene>() where TScene : IScene;
    void PopScene();
    
    IScene CurrentScene { get; }
}
```

### IScene

场景接口：

```csharp
public interface IScene
{
    void OnEnter();
    void OnExit();
    void OnUpdate(float delta);
}
```

## 资源管理接口

### IAssetManager

资源管理器接口：

```csharp
public interface IAssetManager
{
    T Load<T>(string path) where T : Resource;
    void Preload<T>(string path) where T : Resource;
    void Unload(string path);
    bool IsLoaded(string path);
}
```

---

**相关文档**：

- [Game 概述](../game)
- [Core Abstractions](./core-abstractions)
- [存档系统](../game/storage)
- [场景管理](../game/scene-management)
