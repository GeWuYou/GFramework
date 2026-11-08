---
title: 实现存档系统
description: 用文档槽位仓库保存游戏快照，准备版本与内容转换，确认后备份并提交。
---

# 实现存档系统

本教程使用 `SaveRepository<TSaveData>`，把业务 DTO 放入带独立元数据的 `SaveDocument`。可运行实现见 [Dodge the Creeps 工程](https://github.com/GeWuYou/GFramework/tree/main/Samples/DodgeTheCreeps)，接口和格式见[数据与存档](../game/data.md)。

## 定义游戏快照

DTO 实现 `IData`，只保存恢复游戏所需的数据。游戏身份、schema 版本、内容包快照和模组数据由外层文档保存，不要求 DTO 实现版本或内容元数据接口。

<<< ../../../Samples/DodgeTheCreeps/DodgeProgress.cs{csharp}

保存时从原生世界捕获独立 DTO，不保存节点、信号或资源实例。小游戏的 `DodgeWorld.CaptureProgress()` 捕获敌人的稳定 ID、位置、速度与动画进度，同时保存玩家朝向、计时和 RNG seed/state；`RestoreProgress` 准备完整棋盘后才提交世界状态。

## 创建仓库

普通 .NET 使用 `FileStorage`，Godot 使用 `GodotFileStorage` 和 `user://`。仓库要求原始快照与备份能力，两个文件存储实现均提供。

```csharp
using GFramework.Game.Data;
using GFramework.Godot.Storage;
using JsonSerializer = GFramework.Game.Serializer.JsonSerializer;

var storage = new GodotFileStorage(new JsonSerializer());
var saves = new SaveRepository<DodgeProgress>(storage,
    new SaveConfiguration { SaveRoot = "user://dodge_the_creeps/saves" },
    new SaveRepositoryOptions<DodgeProgress>
    {
        GameId = "dodge-the-creeps",
        CurrentVersion = 2
    });
```

上例使用工程中的 `DodgeProgress`，省略了游戏专属内容恢复、校验及模组策略。完整接线直接来自游戏组合根：

<<< ../../../Samples/DodgeTheCreeps/DodgeApp.cs#tutorial-load-data{csharp}

游戏拥有的当前 schema、转换图、完整文档引用与恢复策略：

<<< ../../../Samples/DodgeTheCreeps/DodgeApp.Content.cs#tutorial-save-policy{csharp}

显式配置当前 schema，不从 DTO 构造函数推测版本。转换图在组合根建立，运行中不修改。声明具体升级和降级见[双向存档迁移](./data-migration.md)。

## 保存和读取

```csharp
await saves.SaveAsync(1, snapshot, cancellationToken);
if (await saves.ExistsAsync(1, cancellationToken))
{
    var prepared = await saves.PrepareLoadAsync(1, cancellationToken);
    if (prepared.RequiresConfirmation &&
        !await ConfirmPreparedSaveAsync(prepared, cancellationToken))
    {
        return;
    }
    if (prepared.RequiresCommit)
    {
        var result = await saves.CommitPreparedAsync(1, prepared, cancellationToken);
        ApplyRestoredProgress(result.Data);
    }
    else
    {
        ApplyRestoredProgress(prepared.Data);
    }
}
```

`snapshot` 是保存前捕获的 DTO，`ApplyRestoredProgress` 由游戏恢复当前世界，`ConfirmPreparedSaveAsync` 是返回接受／拒绝结果的游戏确认界面。上例提交前的确认由宿主实现：`RequiresConfirmation` 为 true 时展示路径、损失说明及内容差异，拒绝后退出加载流程。小游戏的实际恢复入口为 `DodgeWorld.RestoreProgress`。

准备阶段仅转换独立文档。`CommitPreparedAsync` 校验原文件指纹和目标会话，备份原字节后原子提交，成功后才将数据用于游戏。取消或失败时保留原档；保存成功后再更新“已保存”状态。

普通 `LoadAsync` 不自动回写，需要转换时抛出包含准备结果的异常。文件不存在时返回默认 DTO，因此继续按钮先调用 `ExistsAsync`。新游戏在内存中开始，玩家明确保存前不覆盖旧槽位。

模组状态通过 `CreateModState()` 或 `prepared.ModState` 创建／恢复，游戏将 `ForMod(id)` 作用域交给对应模组；使用 `SaveDocumentAsync` 将当前游戏 DTO 与模组草稿一次保存。详见[模组数据](../game/data.md#模组数据)。

## 单局结束与槽位管理

`ListSlotsAsync` 返回含存档文件的槽位编号，`DeleteAsync` 删除指定槽位文件。槽位命名、删除确认和元数据展示由业务流程组织。

小游戏采用单局存档：玩家死亡后删除当前槽位和备份恢复入口，返回菜单时禁用“继续游戏”；最高分和设置独立保存。

<<< ../../../Samples/DodgeTheCreeps/DodgeApp.cs#tutorial-run-save-retirement{csharp}

清档是本游戏的规则，其他游戏可以采用检查点、多槽位或长期进度。文档读取和迁移机制不会自行决定结束一局或删除角色。

## 内容恢复与备份

使用 `ContentSaveOptions` 提供完整 `SaveDocument` 的引用及纯恢复函数；回调既能访问 `Game.Data`，也能检查和修复 `Mods`，返回完整修复文档。小游戏在缺失 DLC 时准备替换关卡，只移除失效内容定义的敌人，保留其他合法状态和缺失模组的纯数据块。结构转换、内容修复和模组数据共同进入一次候选结果，不分别覆盖文件。

提交结果返回备份位置，游戏可以保留恢复入口。`RestoreBackupAsync` 先备份当前文件再恢复选定原始文件；恢复不绕过游戏身份、结构版本及内容检查，也不会自动安装缺失包。采用规则见[内容包与存档恢复](../game/content-packs.md#存档依赖与降级恢复)。

## 异步操作与退出

按钮和自动保存观察返回任务，避免未观察的 `async void` 异常。宿主决定自动保存时机，按捕获快照保存；错误提供重试机会，不用默认 DTO 覆盖损坏文件。

退出时关闭新业务入口，等待在途保存和导航，停止玩法及模组，再释放 UI／Scene、暂停、存储与原生宿主。取消可阻止尚未接纳的操作，已接纳的底层写入完整等待。

原子文件替换保护单次写入；磁盘空间、权限、损坏文档和跨进程续读仍应在实际分发宿主中验证。

## 相关主题

- [数据与存档](../game/data.md)
- [双向存档迁移](./data-migration.md)
- [代码模组与 Harmony](../game/code-mods.md)
- [Godot 存储](../godot/storage.md)
- [完整游戏教程](./godot-complete-project.md)
