---
title: "迁移版本化存档"
description: "定义升级和降级步骤，按需确认后提交转换。"
---

# 迁移版本化存档 {#overview}

## 定义转换 {#migration}

将版本转换实现为 `SaveMigrationStep` 并组成 `SaveMigrationGraph`。每步对历史 `SaveDataBlock` 应用纯数据变换并指定输出版本。需要支持降级时登记相反方向的步骤。标记有损转换，让玩家在提交之前确认。

<<< ../../../Samples/DodgeTheCreeps/DodgeProgressMigration.cs{csharp}

## 准备并提交 {#commit}

调用 `PrepareLoadAsync`，检查是否需要确认和提交。拒绝确认时结束操作。确认后用 `CommitPreparedAsync` 写入，并从成功结果恢复进度。测试用旧数据、缺失内容和取消操作覆盖转换路径，核对原文件和备份。

完整槽位流程见[版本化存档](../game/data.md)。
