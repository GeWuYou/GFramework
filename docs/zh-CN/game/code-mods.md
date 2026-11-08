---
title: 代码模组与 Harmony
description: 在桌面 .NET 和 Godot 宿主中加载可信 C# DLL，管理扩展 API、补丁、生命周期与模组存档。
---

# 代码模组与 Harmony

代码模组通过游戏公开的 API 添加或改变玩法。`GFramework.Game` 提供 DLL 宿主；`GFramework.Game.Abstractions.Modding` 定义入口和上下文。需要修改没有公开扩展入口的内部方法时，另行安装可选包 `GeWuYou.GFramework.Game.Harmony`。

本机制面向 Windows/Linux 桌面 .NET 与 Godot .NET。可信 DLL 具有游戏进程权限，加载上下文用于依赖和类型身份管理，不是权限沙箱。启用、停用、更新和排序在进程重启后生效。

## 模组工程与包目录

游戏把稳定的扩展接口放入独立 API 程序集。模组引用相同契约版本，不把游戏实现或一份私有契约复制进自己的加载上下文。小游戏的契约见 [Dodge 模组 API](https://github.com/GeWuYou/GFramework/tree/main/Samples/DodgeTheCreeps.ModApi)。

```text
Game.ModApi/                 # 游戏公开的 CLR 契约
MyMod/                       # 独立编译的普通 .NET 类库
mods/
  my-mod/
    content-pack.json
    bin/
      MyMod.dll
      MyMod.deps.json
      PrivateDependency.dll
    definitions.yaml         # 可选的数据操作
```

模组入口实现 `IGameMod`，必须是清单指定的公开、非抽象、具有无参构造函数的类型。构造函数不要注册玩法或应用补丁；把可失败的工作放入生命周期方法，以便宿主观察和清理。不要依赖全程序集类型扫描或修改游戏的 DI 注册表。

清单使用格式 v2，并复用[内容包](./content-packs.md)的游戏兼容性、依赖、顺序和数据入口：

```json
{
  "formatVersion": 2,
  "id": "my-mod",
  "name": "My Mod",
  "version": "1.0.0",
  "gameId": "my-game",
  "dependencies": [{ "id": "base" }],
  "code": {
    "assemblyPath": "bin/MyMod.dll",
    "entryType": "MyMod.Entry",
    "apiVersions": {
      "minimumInclusive": "1.0.0",
      "maximumExclusive": "2.0.0"
    },
    "requiredCapabilities": []
  }
}
```

DLL 与私有托管依赖从包内解析；框架契约、游戏 API 及可选扩展程序集由宿主明确共享。打包时保留构建得到的依赖描述和实际需要的私有依赖，不把共享契约当成另一套类型加载。框架不负责安装本机运行时或自动解析任意原生库。

共享边界还包括公开 API 参数和返回值的定义程序集。框架自动共享上下文暴露的契约及 `IServiceProvider` 的定义程序集；游戏为自己的 API 提供同样完整的共享集合。引擎使用独立加载上下文时，以宿主实际持有的类型身份为准，不能假定同名类型都属于默认加载上下文。

自包含宿主需要分发完整运行时目录及应用的 `.deps.json`；框架通过运行时包元数据识别标准库，不因 DLL 与运行时放在同一目录就自动共享它。Godot 导出时保留整个 `data_*` 目录。明确共享的契约优先，其余先解析包内依赖，最后才回退到已确认的标准库。

## 启动会话与授权

宿主先发现并解析内容包，设置 `ContentHostCompatibility.CodeApiVersion`；未声明代码 API 的宿主拒绝启用代码包。之后调用 `GameModHost.PrepareAsync`，通过 `GameModHostOptions` 提供游戏身份、代码 API 版本、共享 API 程序集、明确的服务和 dispatcher。

`TrustPolicy` 接收完整 `ContentPackStamp`。游戏在管理界面展示“将运行代码”的后果，并保存玩家批准的包版本和文件指纹；缺省策略拒绝所有代码。不得用“包 ID 已见过”代替精确版本／指纹批准，更新后的包需要重新授权。所有启用代码包通过授权、API 和能力检查后，宿主才开始加载程序集。

代码包必须通过 `modHost.Admission` 传给 `ContentCatalogLoader.LoadAsync`。普通数据加载入口拒绝代码清单，防止忽略代码而创建不完整的内容会话。程序集准备后再进行依赖它们的原生资源准备；完整目录与资源就绪后调用 `StartAsync`，成功后才开放游戏。

以下展示完整会话的装配顺序；`plan` 来自内容解析器，`loader` 已登记游戏表，`gameServices` 和 `dispatcher` 由宿主提供，`approvedStamps` 只包含玩家明确批准的精确包快照。`IDodgeModApi` 是小游戏的 API 契约，其他游戏替换为自己的公开接口：

```csharp
using DodgeTheCreeps.ModApi;
using GFramework.Game.Harmony;
using GFramework.Game.Modding;

await using var modHost = await GameModHost.PrepareAsync(plan,
    new GameModHostOptions
    {
        GameId = "dodge-the-creeps",
        CodeApiVersion = "1.0.0",
        Services = gameServices,
        Dispatcher = dispatcher,
        TrustPolicy = approvedStamps.Contains,
        SharedAssemblies = new[] { typeof(IDodgeModApi).Assembly },
        Extensions = new[] { new HarmonyModExtension() }
    }, cancellationToken);
var catalog = await loader.LoadAsync(plan, modHost.Admission, cancellationToken);
await PrepareNativeResourcesAsync(catalog, cancellationToken);
await modHost.StartAsync(cancellationToken);
```

`PrepareNativeResourcesAsync` 由游戏准备对应宿主资源；纯 .NET 游戏也可以没有原生资源阶段。上述 `await using` 作用域应覆盖整个游戏会话，先停止玩法再退出该作用域；不要在初始化函数返回时提前释放宿主。只使用公开 API 的宿主可以省略 Harmony 引用和 `Extensions`。


`IGameModContext.Services` 只暴露宿主选择的 API。宿主可使用共享服务，也可通过 `CreateServices` 提供模组独立服务；解析顺序为扩展服务、模组独立 provider、共享 provider，这些游戏传入 provider 的所有权由游戏保留。`Dispatcher` 负责原生线程，`Logger` 带模组身份，`Own`、`OwnAsync`、`OwnRegistration` 跟踪模组独有资源和已有事件订阅。

## 生命周期与失败

每个模组依次执行 `InitializeAsync`、`StartAsync`。初始化登记扩展，启动模块服务；这不代表游戏已经创建或恢复某一局。异步工作必须被返回任务完整观察。即使初始化只完成一部分，宿主也会调用该模组的 `StopAsync`。

退出时先停止玩法和在途业务，再逆加载顺序停止模组、释放其订阅和扩展作用域，最后释放存储与原生宿主。停止使用不可取消的清理令牌；一个清理错误不跳过其余参与者，最终聚合报告。共享 provider、dispatcher 和日志工厂仍由宿主拥有。

每包加载上下文保持到进程结束，停止会释放业务资源但不保证程序集卸载。失败后不在同一实例重试，不在运行中重新拼装模组集。

## Harmony 方法补丁

安装并配置可选 `HarmonyModExtension`，在清单的 `requiredCapabilities` 中声明 `"harmony"`。基础 Game、Godot 和聚合包不自动引用 Harmony。

适配器将进程中的 Harmony 引擎固定到 `AssemblyLoadContext.Default`，并把该确切的 `0Harmony` 程序集共享给各模组。Godot 等宿主可能把游戏程序集放在自己的加载上下文；MonoMod 生成的代理与引擎类型若分别绑定到不同上下文，即使程序集同名也可能触发泛型约束错误。[上游问题记录](https://github.com/pardeike/Harmony/issues/642)说明了这种类型身份问题；这里采用统一默认上下文引擎的接入方式。

适配器使用 `Lib.Harmony 2.4.3`，引擎程序集身份为 `0Harmony 2.4.3.0`。`new HarmonyModExtension()` 优先复用默认上下文中已加载的匹配引擎，否则按名称加载，再查找 `AppContext.BaseDirectory/0Harmony.dll`。自定义宿主可以使用 `new HarmonyModExtension(harmonyAssemblyPath)`，传入宿主拥有的绝对物理 DLL 路径；不要从模组清单取得引擎路径。引擎需要随应用分发，已经加载的不兼容版本会明确报错，不能靠另一个文件路径在同一进程替换。

引擎身份与游戏服务契约分别管理：`0Harmony` 使用默认上下文中的身份，`IServiceProvider`、游戏 API 及其公开签名类型仍共享宿主实际持有的定义程序集。固定补丁引擎不等于把所有标准库或游戏服务强行切换到默认上下文。适配器只通过已知公开 API 桥接引擎，不修改引擎私有缓存。


模组从上下文服务取得 `IModPatchService`。带 `[HarmonyPatch]` 等正常 Harmony 注解的模组继续使用 `PatchAll(assembly)`；这些注解绑定到宿主共享的引擎程序集。手工登记则通过 `ModPatchMethod` 传递 `MethodInfo` 和可选排序信息，不把 `HarmonyMethod` 类型放进适配器的公开签名：

```csharp
var postfix = new ModPatchMethod(postfixMethod)
{
    Priority = 400,
    After = new[] { HarmonyModExtension.GetOwnerId("my-game", "other-mod") }
};
patches.Patch(targetMethod, postfix: postfix);
```

`targetMethod` 是要修改的游戏方法，`postfixMethod` 是模组提供的静态补丁方法，`patches` 为当前模组的 `IModPatchService`。`Patch` 的 prefix、postfix、transpiler 和 finalizer 均使用同一种描述。未指定 `Priority` 时保留方法上的 Harmony 优先级声明；空的 `Before`／`After` 不覆盖方法上的相应排序注解。

每个服务的 owner 由 `HarmonyModExtension.GetOwnerId(gameId, modId)` 生成；before/after 声明应使用该函数结果，不手工拼接 owner。`UnpatchAll` 及退出清理只撤销这个 owner 的补丁，保留其他模组。

补丁执行顺序遵守 Harmony 的 priority、before 和 after 声明，不能把内容包加载顺序当作方法执行顺序。`GetPatches()` 提供目标方法、owner 和排序信息；它帮助定位重叠，不自动判断两个补丁的业务语义是否兼容。游戏内部方法改名、内联或实现变化都可能使补丁失效，稳定玩法扩展优先使用公开 API。[Harmony 排序说明](https://harmony.pardeike.net/v2/articles/priorities.html)

小游戏提供两种独立实现：

- [公开 API 计分模组](https://github.com/GeWuYou/GFramework/tree/main/Samples/DodgeTheCreeps.BonusMod)：通过游戏 API 扩展行为。
- [Harmony 计分模组](https://github.com/GeWuYou/GFramework/tree/main/Samples/DodgeTheCreeps.HarmonyMod)：修改内部计分路径。

在仓库根目录执行 `python3 scripts/build-dodge-mods.py`，脚本分别构建两个独立项目，并将其 DLL 与依赖描述暂存到小游戏的 `content_sources/code-*/`。运行时安装去掉 `.source` 后缀；发布自己的模组时直接分发正常 DLL 文件名和格式 v2 清单。完整桌面构建及跨进程检查见[小游戏说明](https://github.com/GeWuYou/GFramework/blob/main/Samples/DodgeTheCreeps/README.md)。

公开 API 模组源码直接来自独立编译项目：

<<< ../../../Samples/DodgeTheCreeps.BonusMod/BonusScoreMod.cs{csharp}

Harmony 模组通过显式目标方法和 owner 作用域应用补丁：

<<< ../../../Samples/DodgeTheCreeps.HarmonyMod/DoubleScoreMod.cs{csharp}


## 模组状态与内容缺失

游戏通过 `ModSaveSession` 为当前模组提供按 ID 隔离的读写作用域；负载是有独立版本的纯 JSON 数据，与游戏进度写入同一份 `SaveDocument`。不要保存 CLR 类型名、原生节点或程序集路径。

模组生命周期与单局数据生命周期分别管理。小游戏的 `IDodgeModApi.ReadState`／`WriteState` 只在已开始或已恢复且仍运行的局面中可用；初始化、启动时尚无游戏局面，以及无当前局的主菜单阶段会抛出说明原因的 `InvalidOperationException`。在初始化阶段登记回调，在实际玩法回调中读取／更新状态；上方 Bonus 示例正是先登记计分修饰器，首次计分时才访问本局数据。新局使用空草稿，读档则在玩家接受候选结果后切换到恢复草稿。

模组数据版本转换复用存档迁移图，转换钩子只能变换独立数据。继续同一局时保留缺失模组数据，不调用缺失代码；新局使用全新草稿，不继承上一局的状态。游戏的完整文档恢复回调可以检查游戏和模组引用，决定是否继续、替换引用或明确删除指定数据块。完整流程见[数据与存档](./data.md)和[双向存档迁移](../tutorials/data-migration.md)。

## 相关主题

- [DLC 与资料型模组](./content-packs.md)
- [存档教程](../tutorials/save-system.md)
- [官方素材小游戏制作教程](../tutorials/godot-complete-project.md)
