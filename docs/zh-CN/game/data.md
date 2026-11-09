---
title: 数据与存档系统
description: 使用文档存档、双向迁移和模组数据，显式准备、确认并原子提交游戏进度。
---

# 数据与存档系统

Game 提供三类普通服务，可直接构造或登记到标准 DI：

| 服务 | 数据组织 | 适用场景 |
| --- | --- | --- |
| `DataRepository` | 一个 `IDataLocation` 对应一份对象 | 玩家档案、缓存与独立业务数据 |
| `UnifiedSettingsDataRepository` | 多个 section 聚合到一个文件 | 音频、图形、语言和输入设置 |
| `SaveRepository<TSaveData>` | 槽位内的一份版本化文档 | 游戏进度、DLC／模组依赖、结构转换及模组状态 |

仓库通过构造函数接收存储与策略，不要求 Architecture、Utility 角色或初始化调用。静态内容配置使用独立的[配置系统](./config-system.md)。

## 文档与业务 DTO

`SaveDocument` 是外层信封：`FormatVersion` 表示文档格式，`GameId` 表示所属游戏，`Game` 包含独立的 `Version` 和 JSON 对象 `Data`，`Content` 保存内容包快照，`Mods` 按模组 ID 保存各自版本和负载。

框架先检查信封，再对完整文档负载执行结构转换和内容修复，最后创建当前版本的 DTO。业务 DTO 只需实现 `IData`，不必携带版本或内容元数据。字段改名、类型变化或删除字段时，转换仍能读取历史原文中的信息，不会先丢给当前 DTO。

默认 `JsonSaveDocumentCodec` 使用 UTF-8 JSON 和 Newtonsoft DOM，不解释文件中的 CLR 类型元数据；`ISaveDocumentCodec` 是文档编解码与 DTO 转换边界。`IStorage` 继续负责键空间和底层读写，不解释游戏 schema。文档仓库要求存储实现 `IStorageBackup`，以保留原始文件字节和原子提交。

## 最小采用

```csharp
using System;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Data;
using GFramework.Game.Serializer;
using GFramework.Game.Storage;

using var storage = new FileStorage("GameData", new JsonSerializer());
var saves = new SaveRepository<PlayerSave>(storage,
    new SaveConfiguration { SaveRoot = "saves" },
    new SaveRepositoryOptions<PlayerSave>
    {
        GameId = "my-game",
        CurrentVersion = 1,
        Validate = static data =>
        {
            if (data.Score < 0) throw new InvalidOperationException("Invalid score.");
        }
    });
await saves.SaveAsync(1, new PlayerSave { Score = 12 });
var restored = await saves.LoadAsync(1);

public sealed class PlayerSave : IData
{
    public int Score { get; set; }
}
```

`ExistsAsync` 检查槽位；`LoadAsync` 在槽位不存在时返回默认对象；`ListSlotsAsync` 按编号列举包含存档文件的槽位。`SaveConfiguration` 定义根目录、槽位前缀和文件名，`SaveRepositoryOptions<T>` 定义游戏身份、当前版本、迁移图、内容策略和业务校验。配置在组合根完成，运行期间不修改。

## 准备、确认与提交

`LoadAsync` 不自动回写。需要版本转换或内容协调时抛出 `SavePreparationRequiredException<T>`，异常携带 `Prepared`；宿主也可以主动调用 `PrepareLoadAsync`。

```csharp
var prepared = await saves.PrepareLoadAsync(1, cancellationToken);
if (prepared.RequiresConfirmation &&
    !await ConfirmPreparedSaveAsync(prepared, cancellationToken))
{
    return;
}
if (prepared.RequiresCommit)
{
    var committed = await saves.CommitPreparedAsync(1, prepared, cancellationToken);
    ApplyRestoredProgress(committed.Data);
    RememberBackup(committed.BackupKey);
}
else
{
    ApplyRestoredProgress(prepared.Data);
}
```

`ConfirmPreparedSaveAsync`、`ApplyRestoredProgress` 和 `RememberBackup` 是游戏自己的 UI、世界恢复和备份入口；确认被拒绝时必须退出当前加载流程。`RequiresCommit` 表示存在待提交的转换或元数据更新，`RequiresConfirmation` 表示降级、有损转换或内容修复需要玩家决定。

准备阶段只操作独立文档，不持久化、不修改活动世界。`Data`、`Document` 和 `ModState` 返回独立副本；修改它们不会改变批准的候选结果。转换和恢复钩子必须是纯数据操作，不能自行保存文件或执行缺失模组代码。

提交核对仓库、槽位、源文件指纹及目标内容会话，保存原字节备份，再原子写入已准备的文档。原文件变化、取消、备份失败、转换结果非法或写入失败时，拒绝覆盖。`SaveCommitResult<T>` 返回提交数据和备份位置；调用方在成功后才进入游戏。

`RestoreBackupAsync` 恢复选定槽位内的原始备份，恢复前也备份当前文件。恢复后的文件仍要经过正常游戏身份、结构版本和内容依赖检查。备份不自动清理，文件替换不等同于跨进程锁或断电落盘保证。

## 双向结构迁移与内容修复

`SaveMigrationGraph` 捕获不可变的 `SaveMigrationStep` 集合。游戏分别注册升级和降级，每一步只处理 `SaveDataBlock`；框架校验方向、路径和输出版本，不推测反向转换。完整示例见[双向存档迁移](../tutorials/data-migration.md)。

`ContentSaveOptions` 声明当前 `ContentCatalog`、引用枚举和纯恢复函数。`EnumerateReferences` 接收完整 `SaveDocument`，可以同时检查 `document.Game.Data` 和 `document.Mods` 中的稳定引用；`RecoverAsync` 接收独立文档与差异报告，并返回完整 `SaveDocument`。游戏因此可以明确修复游戏负载、调整模组数据或删除指定的失效模组块，而不是只能清理游戏 DTO。

结构转换先于当前引用检查，因此游戏可以先把旧字段整理为当前 schema，再检查缺失物品或关卡。恢复结果必须保持所属游戏、信封格式以及当前游戏／活跃模组的 schema；框架重新验证文档和引用，并附加当前内容快照。缺失模组负载默认保留，只有游戏自己的恢复策略可以明确删除。包版本、指纹和顺序差异来自独立信封，`PreparedSave.SavedContent` 可供界面展示原始包版本。内容规则与包兼容声明见[DLC 与资料型模组](./content-packs.md)。

## 模组数据

在 `ModPolicies` 中声明当前启用模组的 `ModSavePolicy`，每个模组有自己的当前版本和迁移图。新局用 `CreateModState()` 创建草稿，读档用 `prepared.ModState` 取得保留全部历史数据块的草稿。

通过 `modState.ForMod("my-mod")` 为对应模组提供 `ModSaveScope`，作用域的 `Read<T>()` 和 `Write<T>(data)` 只访问该模组的数据。游戏应只向模组暴露匹配的作用域，不暴露整个 session，并明确该作用域所对应的局面生命周期。小游戏只允许已开始或已恢复的当前局访问状态；模组初始化和无当前局的主菜单阶段会拒绝状态访问。

修改草稿不会保存文件；`SaveDocumentAsync(slot, gameData, modState, cancellationToken)` 把游戏数据与该草稿的全部模组块一次提交。读档草稿携带同一局的缺失模组数据；新局的空草稿不会继承目标槽位中上一局的任何模组状态。普通 `SaveAsync` 保留槽位已有模组块。同一局中未启用或缺失模组的块保持纯数据，不执行其代码；业务恢复策略由游戏决定。

## 数据与统一设置仓库

`DataRepository` 接收 `IStorage`；`UnifiedSettingsDataRepository` 同时接收 `IRuntimeTypeSerializer`。统一文件在加载前通过 `RegisterDataType(location, typeof(T))` 登记允许恢复的类型。

`DataRepositoryOptions.AutoBackup` 在覆盖前保存旧快照；独立数据按 key 备份，统一设置按完整文件备份。统一仓库先准备下一份缓存，写入成功后交换，失败不污染当前缓存。`SaveAllAsync` 发出一次批量通知；独立仓库逐项写入，统一设置合并为一次文件提交。

数据仓库通知通过可选 `Action<object>? publishEvent` 接入宿主，`EnableEvents` 控制发布。事件包括 `DataLoadedEvent<T>`、`DataSavedEvent<T>`、`DataDeletedEvent` 和 `DataBatchSavedEvent`；通知发生在对应操作成功后。

## 取消、关闭与接口调整

异步接口最后一个参数为 `CancellationToken`。取消控制尚未接纳的操作和后续步骤，已接纳的底层写入完整等待；关闭时先停止新业务并等待保存，再释放存储。

槽位仓库采用新的 `SaveDocument` 格式和显式策略，不提供旧扁平 DTO 格式或旧恢复 API 的兼容分支。原来的 `ISaveMigration<T>`、`IContentAwareSaveData`、`RegisterMigration`、`InspectContentAsync`、`RecoverContentAsync` 和 `ImportLegacyAsync` 被文档转换、准备与提交流程替代。已有历史格式需要游戏明确编写转换工具或选择新存档，不能通过当前 DTO 猜测旧数据。

设置系统仍使用自己的迁移和应用生命周期；一般业务数据仓库的格式不随槽位存档重构改变。

## 相关主题

- [存档教程](../tutorials/save-system.md)
- [双向存档迁移](../tutorials/data-migration.md)
- [代码模组与 Harmony](./code-mods.md)
- [设置系统](./setting.md)
- [存储系统](./storage.md)

## 异构批量保存的类型登记

`DataRepository.SaveAllAsync` 在写入前检查整批数据，每种实际数据类型须通过 `RegisterDataType<T>()` 显式登记：

```csharp
var data = new DataRepository(storage)
    .RegisterDataType<PlayerSave>();
await data.SaveAllAsync([(playerLocation, new PlayerSave { Score = 12 })]);
```

这里的 `storage` 与 `PlayerSave` 沿用前文定义；`playerLocation` 是游戏提供的 `IDataLocation`。异构批次为每个具体类型分别登记，不用基类登记替代派生类型。单项 `SaveAsync<T>` 无需登记。重复登记同一类型无副作用；空批次无需类型登记。未知类型或空条目在任何写入前拒绝，逐项写入开始后仍可能因真实存储失败留下部分成功项，批处理不是跨文件事务。

升级已有独立数据仓库时，在组合根补齐批次类型登记；统一设置仓库继续使用自己的运行时序列化类型登记，不受这项要求影响。

仓库的完成通知及其诊断失败不会改变已经完成的读写结果；直接挂在 `publishEvent` 上的各个回调独立尝试。可通过 `observerLogger` 接入宿主日志，未指定时写入 Trace。回调内部调用 EventBus 时，其下游投递仍由 EventBus 自己的异常策略决定。
