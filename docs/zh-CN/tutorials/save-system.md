---
title: 实现存档系统
description: 用独立槽位仓库保存游戏快照，观察错误并在退出前完成持久化。
---

# 实现存档系统

本教程使用 `SaveRepository<TSaveData>`，按槽位保存游戏 DTO。可运行实现见 [Dodge the Creeps 工程](https://github.com/GeWuYou/GFramework/tree/main/Samples/DodgeTheCreeps)，接口与迁移规则见 [数据与存档](../game/data.md)。

## 定义快照

存档 DTO 实现 `IData`，保存恢复游戏所需的数据。需要版本迁移时再实现 `IVersionedData` 并登记迁移器。

小游戏的实际存档定义直接来自可编译工程：

<<< ../../../Samples/DodgeTheCreeps/DodgeProgress.cs{csharp}

原生节点、信号和资源引用保留在宿主层。保存时把当前状态复制为 DTO，避免异步写入期间继续修改同一快照。

## 创建仓库

普通 .NET 宿主使用 `FileStorage`；Godot 使用 `GodotFileStorage` 和 `user://`：

```csharp
using GFramework.Game.Data;
using GFramework.Godot.Storage;
using JsonSerializer = GFramework.Game.Serializer.JsonSerializer;

var storage = new GodotFileStorage(new JsonSerializer());
var saves = new SaveRepository<DodgeProgress>(storage, new SaveConfiguration
{
    SaveRoot = "user://dodge_the_creeps/saves"
});
```

上例的 `DodgeProgress` 来自样例命名空间。项目可使用自己的 DTO。仓库通过构造函数接收依赖，宿主保存并复用实例。

## 保存和恢复

```csharp
await saves.SaveAsync(1, snapshot, cancellationToken);
if (await saves.ExistsAsync(1, cancellationToken))
{
    var restored = await saves.LoadAsync(1, cancellationToken);
    ApplyRestoredProgress(restored);
}
```

`snapshot` 是保存前捕获的 DTO，`ApplyRestoredProgress` 由项目恢复角色、关卡和计分。只有保存成功后更新“已保存”状态。读取失败时报告错误并保留原文件，玩家可以选择新游戏或其他槽位。

`LoadAsync` 在文件缺失时返回新的默认 DTO，因此“继续游戏”先检查存在性。菜单的继续按钮应反映当前槽位是否存在。新游戏创建内存进度，在玩家明确保存前保留已有槽位。

## 槽位管理与迁移

`ListSlotsAsync` 返回包含有效存档文件的槽位编号；`DeleteAsync` 删除指定槽位文件。界面展示、删除确认和元数据由业务流程组织。

版本迁移通过 `RegisterMigration` 登记。加载时使用稳定迁移表快照，成功到达当前版本后才回写；缺链、非法版本或不兼容结果通过异常报告。详细规则见 [数据迁移](./data-migration.md)。

## 异步操作与退出

按钮和定时保存流程都观察返回任务，避免未观察的 `async void` 异常。自动保存由宿主选择触发时机，按快照串行保存；错误显示给用户并保留重试机会。

取消可以阻止尚未接纳的保存，已开始的底层写入会被完整等待。退出时关闭新的保存入口，等待在途保存和导航，再停止 UI／Scene、释放暂停和存储，最后退出宿主。

原子文件替换保证单次写入失败时旧文件保留。磁盘空间、权限和损坏文件应在实际宿主中作为恢复场景验证。

## 相关主题

- [数据与存档](../game/data.md)
- [设置系统](../game/setting.md)
- [Godot 存储](../godot/storage.md)
- [完整游戏教程](./godot-complete-project.md)
