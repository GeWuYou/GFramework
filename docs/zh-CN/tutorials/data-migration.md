---
title: 双向存档迁移
description: 在创建当前 DTO 前转换版本化文档，为升级、降级和模组数据声明可检查的转换路径。
---

# 双向存档迁移

本教程接续[存档教程](./save-system.md)，使用 `SaveMigrationGraph` 转换 `SaveDataBlock`。游戏负责每一步的含义和可能损失；框架负责路径、版本验证、独立准备、备份及提交。

## 先转换文档，再创建 DTO

假设 v1 游戏负载用 `Score` 保存分数，v2 改为 `Progress.Score`。把 v1 直接反序列化为当前 DTO 可能丢失旧字段，也可能因类型变化而失败。迁移器应直接处理历史 JSON，成功到达目标 schema 后才创建当前 DTO。

`SaveDocument.FormatVersion` 是框架信封格式；`Game.Version` 是游戏自己的 schema 版本；每个 `Mods[id].Version` 则是对应模组的数据版本。这些版本相互独立，不使用游戏发行号或 DLL 版本代替数据 schema。

## 声明两个方向

`SaveMigrationStep` 接收源版本、目标版本、说明和纯转换函数；`isLossy` 表示即使升级也需要显式确认。两个方向都必须明确实现：

```csharp
using System.Threading.Tasks;
using GFramework.Game.Data;
using Newtonsoft.Json.Linq;

var graph = new SaveMigrationGraph(new[]
{
    new SaveMigrationStep(1, 2, "Move score into Progress", static (source, token) =>
    {
        token.ThrowIfCancellationRequested();
        var data = (JObject)source.Data.DeepClone();
        data["Progress"] = new JObject { ["Score"] = data["Score"]?.DeepClone() ?? 0 };
        data.Remove("Score");
        return Task.FromResult(new SaveDataBlock { Version = 2, Data = data });
    }),
    new SaveMigrationStep(2, 1, "Flatten score for schema 1", static (source, token) =>
    {
        token.ThrowIfCancellationRequested();
        var data = (JObject)source.Data.DeepClone();
        data["Score"] = data["Progress"]?["Score"]?.DeepClone() ?? 0;
        data.Remove("Progress");
        return Task.FromResult(new SaveDataBlock { Version = 1, Data = data });
    })
});
```

这里删除 `Progress` 的降级仅适用于该对象只有分数的 schema；若其中还有不能表示的进度，应在转换说明中列出损失并设置 `isLossy: true`，或直接拒绝。不能把“设置一个较小版本号”当作完成降级。

转换函数只能处理给定数据，不写文件、不修改当前世界，不依赖未安装模组。保留未修改字段，显式删除真正废弃的字段。每一步返回声明的目标版本，空结果或错误版本都会终止准备，原档不变。

## 路径选择

```csharp
var upgrade = graph.Plan(1, 2);
var downgrade = graph.Plan(2, 1);
```

框架只使用向目标版本单调前进的已注册边，不绕远路后再折返，也不自动求逆。默认选择唯一最短路径；例如 `1→2→4` 与 `1→3→4` 同样短时，必须通过 `Plan(1, 4, new[] { 1, 2, 4 })` 明确选择。缺链、重复边、自环、非正版本和非法显式路径被拒绝。

为仓库设置 `SaveRepositoryOptions<T>.Migrations = graph` 和明确的 `CurrentVersion`。仓库需要消除歧义时调用 `PrepareLoadAsync(slot, versionPath, cancellationToken)`；模组策略通过 `ModSavePolicy.VersionPath` 指定对应路径。准备和提交时目标版本由运行时策略决定，不从 `new T()` 推导。实际运行工程的转换定义见[小游戏存档转换](https://github.com/GeWuYou/GFramework/blob/main/Samples/DodgeTheCreeps/DodgeProgressMigration.cs)。

## 准备与显式提交

先调用 `PrepareLoadAsync`，读取 `MigrationPath`、内容差异、`RequiresCommit` 和 `RequiresConfirmation`。准备后的 `Document`、DTO 和模组 session 都是副本，不能通过修改这些副本悄悄更改即将提交的候选结果。

降级始终需要明确决定；声明有损的升级及内容修复同样需要确认。游戏可以展示说明、取消操作、重新启用原内容或选择新游戏。确认后将同一份准备结果交给 `CommitPreparedAsync`，框架核对原文件与内容会话，保存原始字节备份并原子提交。

普通 `LoadAsync` 不自动回写，遇到待转换文档会提供 `SavePreparationRequiredException<T>.Prepared`。不要捕获该异常后将默认 DTO 覆盖回原文件。完整调用和关闭顺序见[数据与存档](../game/data.md)。

## 内容恢复与模组版本

内容元数据位于外层，不依赖业务 DTO。游戏和活跃模组到达各自当前 schema 后，`ContentSaveOptions.EnumerateReferences` 接收完整 `SaveDocument`，检查游戏及模组数据中的稳定 ID；`RecoverAsync` 接收独立文档和差异报告，返回修复后的完整文档。缺失关卡如何替换、物品是否保留、指定失效模组块是否删除均由游戏决定。恢复不能改变所属游戏或绕过当前 schema，完成后还要验证全部引用及业务条件。

模组通过 `ModSavePolicy(modId, currentVersion, graph)` 采用相同的转换执行器，但独立规划其数据版本。未安装模组的负载保持原样，不执行缺失代码；重新启用后再使用该模组声明的迁移图。

## 验证转换

对实际保存文档进行回归，至少覆盖：

- 历史负载无法直接转换为当前 DTO，但文档转换后可以恢复。
- 多步升级、明确实现的降级、缺链、歧义和错误输出版本。
- 未修改字段保留，有损说明与确认准确，取消不写文件。
- 备份、提交失败及准备后源文件变化时，原文件不被覆盖。
- 游戏与模组分别迁移，缺失模组的数据保留，内容恢复后的引用仍有效。

正向再反向不必然无损；只有游戏定义为可逆的转换才检查完整往返相等。有损降级应检查明确保留和明确丢弃的数据，并验证原始备份可恢复。
