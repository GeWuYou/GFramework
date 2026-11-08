---
title: DLC 与资料型模组
description: 发现、排序和组合内容包，并在读档前检查内容依赖、备份和恢复缺失内容。
---

# DLC 与资料型模组

内容包将本体、官方 DLC 和资料型模组组成一个启动时确定的内容会话。游戏仍负责物品、敌人和关卡的类型、schema、行为模板及存档修复规则；内容包提供符合这些规则的数据和素材。

`GFramework.Game.Abstractions.Content` 定义内容 ID、清单、依赖、玩家配置与存档快照。`GFramework.Game.Content` 提供目录发现、依赖解析和内容合并。Godot 的资源加载适配位于 `GFramework.Godot.Content`。

## 包目录与版本

每个包使用独立目录；安装是复制目录，卸载是移除该目录。根目录列表由游戏提供，不从包清单读取任意扫描路径。

```text
content/
  base/
    content-pack.json
    definitions.yaml
  winter/
    content-pack.json
    definitions.yaml
    winter.pck
mods/
  fast-creeps/
    content-pack.json
    balance.yaml
    images/
      enemy.png
```

清单示例：

```json
{
  "formatVersion": 2,
  "id": "fast-creeps",
  "name": "Fast Creeps",
  "version": "1.0.0",
  "gameId": "my-game",
  "gameVersions": { "minimumInclusive": "1.0.0", "maximumExclusive": "2.0.0" },
  "contentApiVersions": { "minimumInclusive": "1.0.0", "maximumExclusive": "2.0.0" },
  "dependencies": [{ "id": "base", "versions": { "minimumInclusive": "1.0.0" } }],
  "contentFiles": ["balance.yaml"]
}
```

版本是三个非负整数组成的 `major.minor.patch`，不接受预发布后缀。区间下界包含、上界不包含，省略端点表示该方向无界。包版本、游戏版本和游戏拥有的内容 API 版本分别校验。

依赖默认必需；设置 `optional: true` 时，缺席可以接受，但启用后仍约束版本和顺序。`incompatiblePackages` 声明不能同时启用的包。重复包 ID、依赖缺失、版本不兼容或循环依赖均产生结构化诊断，不会自动选取一个版本或忽略错误包。

## 稳定 ID 与字段补丁

内容使用 `包ID:本地ID`，例如 `base:walker`。显示名称、翻译、目录位置和加载顺序不参与身份；存档中保存 ID，不保存列表下标。新增内容属于创建它的包，修改其他包的内容时继续使用原 ID。

操作文件使用 YAML 数组，每项声明 `op`、`table`、`id`，需要数据时提供 `value`：

```yaml
- op: add
  table: enemies
  id: fast-creeps:runner
  value:
    id: fast-creeps:runner
    speed: 220
    health: 10
- op: patch
  table: enemies
  id: base:walker
  value:
    speed: 180
```

| 操作 | 行为 |
| --- | --- |
| `add` | 创建新记录；ID 已存在时报错 |
| `replace` | 替换完整记录；目标必须存在 |
| `patch` | 修改目标的部分字段；目标必须存在，不能改变 ID |
| `remove` | 删除已有记录；最终仍有引用指向它时校验失败 |

字段补丁采用 [JSON Merge Patch 的合并规则](https://www.rfc-editor.org/rfc/rfc7396)，输入可以使用 YAML：对象递归合并，标量与数组整体替换，`null` 删除字段。需要将字段设置为显式空值时，使用完整记录替换。数组不会自动拼接，也不通过数组下标隐式匹配物品。

不同字段的补丁可以叠加。同一字段由后加载的包覆盖，来源记录保留原始创建和后续修改。游戏可以显示最终值来自哪个包、文件和字段，帮助玩家调整顺序。

游戏显式注册可扩展表的 CLR 类型、schema 和 ID 提取方式。包不能指定要反射创建的 CLR 类型，也不能修改游戏拥有的 schema。合并全部记录后，复用配置系统的结构约束与跨表引用校验，通过后才向游戏公开完整内容目录。原有 `ConfigRegistry` 的同名整表替换和开发期热重载行为继续适用于原配置工作流。

## 启动与玩家排序

启动依次执行发现、依赖解析、内容合并与校验、宿主资源准备，最后才进入游戏。使用 `ContentPackDiscovery`、`ContentPackResolver` 和 `ContentCatalogLoader` 可以直接组合普通服务，不要求先创建 Architecture。

```csharp
using GFramework.Game.Abstractions.Content;
using GFramework.Game.Content;

var installed = await new ContentPackDiscovery().DiscoverAsync(new[] { "content", "mods" });
var profile = new ContentPackProfile
{
    EnabledPackages = new[] { "fast-creeps" },
    LoadOrder = new[] { "fast-creeps" }
};
var plan = ContentPackResolver.Resolve(installed, profile, new ContentHostCompatibility
{
    GameId = "my-game", GameVersion = "1.0.0", ContentApiVersion = "1.0.0", BasePackId = "base"
});
var catalog = await new ContentCatalogLoader()
    .RegisterTable<EnemyDefinition>("enemies", "schemas/enemies.schema.json", static enemy => enemy.Id)
    .LoadAsync(plan);
var walker = catalog.GetTable<EnemyDefinition>("enemies").Get(new ContentId("base:walker"));
```

`EnemyDefinition` 是游戏定义的 DTO，其 `Id` 使用字符串，字段与游戏拥有的 schema 保持一致。游戏引用 `GeWuYou.GFramework.Game`；Godot 项目通过现有 `GeWuYou.GFramework.Godot` 接线，无需另加内容包 NuGet。持久化时保存 ID 字符串，查询时构造 `ContentId`，不要求为该值类型配置通用序列化器。

`ContentPackProfile.EnabledPackages` 保存可选包的启用状态，`LoadOrder` 保存玩家偏好。宿主指定的本体包始终启用并排在前面；依赖必须先于使用方，其余按玩家顺序排列。管理界面应展示解析后的实际顺序，不能将未经约束的列表顺序当作最终结果。

启用、停用、更新和排序在重启后生效。运行中的游戏持有同一个内容快照；保存待生效配置不会改变当前局。启动失败时仍应保留内容管理入口，供玩家修正选择，而不是悄悄丢弃出错的模组。

## 存档依赖与降级恢复

`SaveDocument.Content` 将启用包的精确版本、指纹和实际顺序与游戏负载保存到同一份文档。游戏通过 `SaveRepositoryOptions<TSaveData>` 和 `ContentSaveOptions` 提供当前内容会话、完整文档中的引用以及恢复策略。`EnumerateReferences` 检查 `SaveDocument` 的游戏与模组负载，`RecoverAsync` 返回修复后的完整文档，可由游戏明确修复或移除指定模组块。检查元数据和转换历史负载不要求先创建当前版本的 DTO。

`PrepareLoadAsync` 在独立文档中准备结构迁移与内容修复，返回候选结果及需要展示的差异。准备函数只能转换输入数据，不能修改运行中的世界、写入文件或加载缺失模组的代码。缺失包、不兼容版本、缺失 ID 和转换说明共同构成玩家的决定依据；游戏决定替换、保留、清理或拒绝，不由框架猜测。

包可以声明接受的旧存档版本范围；省略时按精确版本和指纹检查。同版本文件被修改不会因为版本区间而自动兼容。新增包及顺序变化会被报告，不能据此静默删除原有内容。

需要转换的存档由游戏展示准备结果并取得确认，再调用 `CommitPreparedAsync`：

1. 核对原文件指纹、候选结果和目标内容会话，拒绝过期确认。
2. 验证准备好的版本、内容引用及游戏业务条件，将原文件按字节备份到当前槽位的独立位置。
3. 再次核对原文件和目标会话，原子提交完整文档。
4. 提交成功后，才把返回的数据交给游戏世界。

`LoadAsync` 不回写迁移结果；需要转换时返回准备要求，由宿主进入显式处理流程。原始备份使用 `RestoreBackupAsync` 恢复，恢复后仍须执行正常的内容与版本检查。已删除能力的旧接口不作为另一路兼容入口保留，完整采用见[数据与存档](./data.md)及[双向存档迁移](../tutorials/data-migration.md)。

文件存储和 Godot 存储通过 `IStorageBackup` 保留原始字节与编码；自定义存储需要提供该能力才能使用文档存档。备份位置随提交结果返回，不自动清理历史备份。备份失败、验证失败、取消或源文件变化时不覆盖原档。

## Godot 素材与官方 PCK

资料型模组使用受限的 PNG、OGG、WAV 和数据入口，关卡通过游戏已有模板解释。读取路径必须位于包内，不接受目录逃逸、外部符号链接目标或脚本／程序集入口。

官方 PCK 由宿主显式校验信任；清单里的 `resourcePacks` 仅声明位置，不证明官方身份。Godot 示例使用固定 SHA-256 白名单。其他游戏可以提供自己的签名或安装来源验证策略。

PCK 在 Godot 主线程、内容资源首次使用之前加载，使用包专属的 `res://content/<包ID>/` 路径和 `replace_files=false`。逻辑覆盖由内容操作表达，不通过全局路径覆盖实现。挂载中途失败时停止启动，修复后重启；不承诺卸载已经挂载的 PCK。

Godot 的资源包可以包含脚本和场景，因此可信官方资源包与第三方资料模组使用不同入口。第三方 C# DLL 通过独立的[代码模组](./code-mods.md)授权与生命周期入口加载，不通过资料素材读取器执行；代码模组也不提供安全沙箱。[Godot 资源包说明](https://docs.godotengine.org/en/stable/tutorials/export/exporting_pcks.html)

## 相关主题

- [配置系统](./config-system.md)：schema、只读配置表与开发期热重载。
- [数据与存档](./data.md)：文档存档、结构版本迁移和持久化。
- [代码模组与 Harmony](./code-mods.md)：可信 DLL、宿主 API、补丁和模组状态。
- [存储系统](./storage.md)：文件存储与原子写入。
- [官方素材小游戏制作教程](../tutorials/godot-complete-project.md)：普通 Godot 宿主的实际采用。
