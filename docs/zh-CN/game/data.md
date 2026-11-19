---
title: "数据与版本化存档"
description: "选择合适的数据仓库，保存、准备与恢复游戏进度。"
---

# 数据与版本化存档 {#overview}

| 任务 | 仓库 |
| --- | --- |
| 读写一份独立对象 | `DataRepository` |
| 将多个设置分区写入同一文件 | `UnifiedSettingsDataRepository` |
| 使用槽位、版本及内容包快照 | `SaveRepository<TSaveData>` |

## 存档仓库 {#repository}

在组合根提供支持 `IStorageBackup` 的存储，例如 `FileStorage`。用 `SaveConfiguration` 设置根目录与槽位命名，用 `SaveRepositoryOptions<T>` 配置游戏 ID、文档版本、迁移和验证。游戏快照实现 `IData`，由仓库将其包装为版本化文档。

## 读取与确认 {#load}

- 用 `ExistsAsync` 决定是否显示继续游戏。用 `LoadAsync` 加载可直接读取的槽位。
- 有迁移或内容协调时，通过 `PrepareLoadAsync` 获得候选结果。先检查 `RequiresConfirmation`，让玩家确认需要批准的调整。
- 若 `RequiresCommit`，以 `CommitPreparedAsync` 保留原始文件备份并提交。成功后再用返回的数据恢复世界。
- 用 `RestoreBackupAsync` 恢复指定备份。随后按正常流程加载该槽位。

## 模组存档 {#mod-data}

<span id="模组数据"></span>

为启用模组配置 `ModSavePolicy`。新游戏创建空的模组草稿，继续游戏时使用准备结果中的 `ModState`。给每个模组单独的作用域，最后用 `SaveDocumentAsync` 保存游戏和草稿。

继续[实现存档](../tutorials/save-system.md)，或阅读[双向迁移](../tutorials/data-migration.md)。
