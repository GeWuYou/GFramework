---
title: "代码模组与 Harmony"
description: "为桌面 .NET 游戏接入可信的代码模组。"
---

# 代码模组与 Harmony {#overview}

## 编写模组 {#authoring}

将游戏扩展接口放在共享 API 程序集。模组引用兼容版本，并在 `content-pack.json` 的 `code` 中指定 DLL、入口类型和 API 版本区间。入口是公开、可实例化的 `IGameMod` 实现，使用无参构造函数。把玩法登记放在生命周期方法。

## 启动与信任 {#session}

1. 用内容包解析器取得计划，配置 `CodeApiVersion`。
2. 在界面明确告知 DLL 会以游戏进程权限执行，保存玩家批准的包版本和文件指纹。
3. 调用 `GameModHost.PrepareAsync`，传入共享契约程序集、服务、调度器和 `TrustPolicy`。
4. 将 `modHost.Admission` 传入 `ContentCatalogLoader.LoadAsync`。准备资源后调用 `StartAsync`。
5. 关闭游戏会话时等待模组宿主释放。更改启用状态、版本或顺序后重启游戏。

需要方法补丁时，在宿主安装 `GeWuYou.GFramework.Game.Harmony` 并配置 `HarmonyModExtension`。用模组自己的 owner 管理补丁与撤销。由公开 API 提供的玩法扩展可直接使用原有接口。

参见 [Dodge the Creeps 模组契约](https://github.com/GeWuYou/GFramework/tree/main/Samples/DodgeTheCreeps.ModApi)。
