# Game.Harmony

[简体中文](#简体中文) · [English](#english)

## 简体中文

为批准运行的桌面代码模组提供 Harmony 方法补丁。补丁按模组管理应用与撤销。

`GFramework.Game.Harmony` 依赖 `GFramework.Game` 的模组宿主。需要提供方法补丁时，将此包安装到调用 `GameModHost.PrepareAsync` 的宿主工程，并把 `HarmonyModExtension` 加入会话配置。

[查看使用说明](https://gewuyou.github.io/GFramework/zh-CN/game/code-mods.html)

## English

Add Harmony method patches for approved desktop code mods. Apply and remove patches by mod owner.

`GFramework.Game.Harmony` depends on the mod host in `GFramework.Game`. To offer method patches, install it in the host project that calls `GameModHost.PrepareAsync` and add `HarmonyModExtension` to the session options.

[Read the guide](https://gewuyou.github.io/GFramework/game/code-mods.html)

## 安装 · Install

```bash
dotnet add package GeWuYou.GFramework.Game.Harmony
```
