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

示例使用 `DodgeWorld.CaptureProgress()` 捕获全部敌人及其稳定 ID、位置、速度和动画进度，同时保存玩家朝向、计时和 RNG seed/state。`RestoreProgress` 先准备整个棋盘，再提交恢复结果。早期格式通过 `DodgeProgressMigration` 补充默认值，更新前备份原文件。

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

`snapshot` 是保存前捕获的 DTO，`ApplyRestoredProgress` 由项目恢复角色、关卡、敌人和计时状态。示例的实际入口为 `DodgeWorld.RestoreProgress`。只有保存成功后更新“已保存”状态。读取失败时报告错误并保留原文件，玩家可以选择新游戏或其他槽位。

`LoadAsync` 在文件缺失时返回新的默认 DTO，因此“继续游戏”先检查存在性。菜单的继续按钮应反映当前槽位是否存在。新游戏创建内存进度，在玩家明确保存前保留已有槽位。

本示例采用单局存档：玩家死亡时删除当前槽位和备份恢复入口，返回菜单后禁用“继续游戏”。最高分和设置独立保存，不随本局清除。

清档属于本游戏的结束规则，由 `DodgeApp.GameOverAsync` 等待以下实现；其他游戏可以在自己的业务层选择检查点、多槽位或长期进度：

<<< ../../../Samples/DodgeTheCreeps/DodgeApp.cs#tutorial-run-save-retirement{csharp}

## 槽位管理与迁移

`ListSlotsAsync` 返回包含有效存档文件的槽位编号；`DeleteAsync` 删除指定槽位文件。界面展示、删除确认和元数据由业务流程组织。

版本迁移通过 `RegisterMigration` 登记。加载时使用稳定迁移表快照，成功到达当前版本后才回写；缺链、非法版本或不兼容结果通过异常报告。详细规则见 [数据迁移](./data-migration.md)。

使用 DLC／模组的游戏应采用内容感知仓库：检查内容依赖先于迁移与回写，玩家确认恢复后先备份原始文件，再调用游戏钩子、迁移和验证。示例仅移除缺失定义的敌人并替换缺失关卡，其余合法状态保留。具体设置与恢复入口见 [内容包与存档兼容](../game/content-packs.md)。

## 异步操作与退出

按钮和定时保存流程都观察返回任务，避免未观察的 `async void` 异常。自动保存由宿主选择触发时机，按快照串行保存；错误显示给用户并保留重试机会。

取消可以阻止尚未接纳的保存，已开始的底层写入会被完整等待。退出时关闭新的保存入口，等待在途保存和导航，再停止 UI／Scene、释放暂停和存储，最后退出宿主。

原子文件替换保证单次写入失败时旧文件保留。磁盘空间、权限和损坏文件应在实际宿主中作为恢复场景验证。

## 相关主题

- [数据与存档](../game/data.md)
- [设置系统](../game/setting.md)
- [Godot 存储](../godot/storage.md)
- [完整游戏教程](./godot-complete-project.md)
