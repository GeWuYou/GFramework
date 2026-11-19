---
title: "DLC 与资料型模组"
description: "发现、排序和合并游戏内容包。"
---

# DLC 与资料型模组 {#overview}

## 准备包目录 {#packages}

每个包使用一个目录和 `content-pack.json`。清单声明包 ID、版本、游戏兼容区间、依赖和内容文件。用 `major.minor.patch` 表达版本；版本区间包含下界、排除上界。将可选包和冲突包显式写入清单。

## 修改内容 {#patches}

保存和引用稳定 ID，例如 `base:walker`。新增使用 `add`，完整替换使用 `replace`，修改字段使用 `patch`，删除使用 `remove`。

```yaml
- op: patch
  table: enemies
  id: base:walker
  value:
    speed: 180
```

补丁保留现有 ID，递归合并对象，整体替换数组和标量。`null` 删除字段，显式空值用完整记录替换。最终目录会按游戏提供的 schema 和引用规则校验。

## 加入游戏会话 {#session}

用 `ContentPackDiscovery` 查找游戏允许目录下的包。以 `ContentPackProfile` 和宿主兼容参数调用 `ContentPackResolver`。将各个表的 CLR 类型、schema 和 ID 提取方法登记到 `ContentCatalogLoader`，加载成功后再准备原生资源并开放游戏。

## 存档依赖与恢复 {#save-recovery}

<span id="存档依赖与降级恢复"></span>

存档记录启用包的快照及内容的稳定 ID。内容缺失时，用存档准备结果显示差异，由游戏决定替换或移除哪些引用。玩家批准后再备份并提交。见[准备和读取存档](./data.md#load)。

Godot 完整流程见[游戏内容扩展示例](../tutorials/godot-complete-project.md#content-extension)。需要 DLL 入口时参见[代码模组](../game/code-mods.md)。
