# GFramework.Game.Harmony

可选的可信代码模组补丁适配包。引用 `GeWuYou.GFramework.Game.Harmony`，并在宿主 `GameModHostOptions.Extensions` 中添加 `new HarmonyModExtension()`。普通 Game 和 Godot 消费者不需要此包。

`HarmonyModExtension` 将固定的 `0Harmony` 2.4.3.0 加载到 .NET Default 加载上下文，再将该引擎与本包程序集明确共享给模组，为每个模组提供一个 `IModPatchService`。适配层不静态绑定 Harmony 类型，避免引擎自己使用独立加载上下文时复制补丁引擎。模组从 `IGameModContext.Services` 取得服务，使用 `PatchAll(typeof(MyMod).Assembly)` 或 `Patch` 显式登记补丁。清单的 `code.requiredCapabilities` 声明 `harmony`。

默认从 Default 上下文按程序集名解析引擎，其次读取应用基目录中的 `0Harmony.dll`。自定义宿主可以传入 `new HarmonyModExtension(harmonyAssemblyPath)`，路径必须是宿主提供的绝对物理路径；不扫描模组目录，也不替换进程中已有的不兼容版本。分发必须包含 NuGet 提供的完整引擎文件。

手动补丁使用 `Patch(original, postfix: new ModPatchMethod(patchMethod))`，通过 `Priority`、`Before`、`After` 可选声明排序；不将 `HarmonyMethod` 对象传过宿主边界。程序集注解仍使用正常的 Harmony 属性，并通过 `PatchAll` 交给同一个引擎解释。

每个补丁 owner 由 `HarmonyModExtension.GetOwnerId(gameId, modId)` 生成。`HarmonyBefore`、`HarmonyAfter` 和 priority 仍由 Harmony 解释；包加载顺序不等于方法补丁执行顺序。`GetPatches()` 列出本模组目标、方法、类型和排序声明，不自动推断业务冲突。

停止模组后，宿主释放补丁 scope，只移除对应 owner。应用失败也移除该 owner 的部分登记；其他模组补丁保留。补丁登记、检查和撤销在 scope 内同步串行化，允许普通 .NET 异步初始化跨线程恢复；游戏原生操作仍通过 dispatcher 执行。模组必须观察自己的异步任务，不能让补丁安装逃逸宿主生命周期。原生副作用与模组自行创建的 Harmony 实例不由这个 scope 管理。

程序集及补丁采用进程生命周期，启停或更新模组需要重启；可信 DLL 与游戏具有同样的进程权限，不是沙箱。宿主仍应公开稳定 API 作为通常扩展入口，方法补丁用于确实需要修改内部方法的场景。

采用入口见[代码模组](../docs/zh-CN/game/code-mods.md)与[DLC 和内容包](../docs/zh-CN/game/content-packs.md)。
