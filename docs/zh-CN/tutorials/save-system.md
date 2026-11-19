---
title: "实现存档"
description: "保存可恢复的游戏快照，并在重启后继续。"
---

# 实现存档 {#overview}

## 定义快照 {#snapshot}

创建实现 `IData` 的快照，把恢复局面需要的值保存在其中。示例保存玩家、敌人、计时器和随机状态。

<<< ../../../Samples/DodgeTheCreeps/DodgeProgress.cs{csharp}

## 创建仓库并保存 {#repository}

使用 `GodotFileStorage` 和 `user://` 保存玩家文件。配置游戏身份、槽位路径和当前数据版本，再用 `SaveRepository<T>` 保存。保存完成后更新继续游戏按钮。

<<< ../../../Samples/DodgeTheCreeps/DodgeApp.cs#tutorial-save-game{csharp}

## 加载、恢复和清档 {#restore}

用 `PrepareLoadAsync` 准备需要转换的文档，按需让玩家确认，成功提交后恢复世界。保留内容的稳定键并使用[独立迁移规则](../tutorials/data-migration.md)。游戏结束时按游戏规则退役当前局的存档。

从[原生自检](../tutorials/godot-complete-project.md#export)运行持久化流程，并用首轮标识验证新进程继续。
