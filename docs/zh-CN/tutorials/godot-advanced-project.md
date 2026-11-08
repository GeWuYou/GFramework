---
title: Godot 进阶项目：显式依赖与功能边界
description: 在完整 Dodge the Creeps 游戏中使用标准 DI、纯 C# 用例与明确的宿主生命周期。
---

# Godot 进阶项目：显式依赖与功能边界

本教程以完整的 Dodge the Creeps 玩法、存档、设置、本地化、内容包和代码模组为范围，说明如何组织一个能够继续扩展的个人项目。源码位于 Samples 下的 DodgeTheCreeps.Advanced、DodgeTheCreeps.Advanced.Core 和 DodgeTheCreeps.Advanced.Tests。

建议先完成[基础游戏制作](./godot-complete-project.md)。基础版直接组合服务，适合学习能力和快速做出玩法；进阶版增加了应用端口、明确的功能服务和可选生命周期编排。

## 开发约定

- Model 表示状态与规则，System 表示用例协调，Utility 表示可复用能力。按职责选择名字，不要求继承框架基类。
- 依赖通过构造函数表达。只有装配入口使用 IServiceCollection 和 IServiceProvider。
- 按 Gameplay、Progress、Settings、Content 功能组织代码；功能内部再区分纯规则、应用用例与宿主适配。
- 调用一个明确用例直接使用方法。通知表达已经发生的事情；有统一管线需求时再采用 CQRS。
- 只为实际边界定义接口：本例的存档与棋盘需要替身测试，所以具有端口；无需为每个数据类创建接口。

## 从空目录建立工程

1. 在 Samples 下建立上述三个目录。Godot 工程与纯 C# 工程是同级目录，避免 Godot 的默认编译扫描把测试或领域源码再次编译进宿主。
2. 使用以下项目文件，建立 Godot 对 Core 的项目引用。Core 只引用 .NET 基础库，测试只引用 Core。

<<< ../../../Samples/DodgeTheCreeps.Advanced.Core/DodgeTheCreeps.Advanced.Core.csproj{xml}

<<< ../../../Samples/DodgeTheCreeps.Advanced/DodgeTheCreeps.Advanced.csproj{xml}

3. 从进阶示例复制 project.godot、Main.tscn、export_presets.cfg、独立解决方案、art、fonts、Localization、content-schemas、content_sources 和 official-pack，保留 ASSETS 与许可证。它们与基础版使用相同授权素材，但路径均相对于各自的 Godot 项目。
4. 按本教程的职责分组加入 Core、Composition、Gameplay、Progress、Settings、Content、Modding 和 Presentation 源码。其余带 Smoke 后缀的文件为原生验收，可一并加入以复现验收。
5. 从仓库根准备独立模组并构建：

```bash
bash scripts/prepare-dodge-advanced-mods.sh
dotnet build Samples/DodgeTheCreeps.Advanced/DodgeTheCreeps.Advanced.csproj -c Debug
godot --editor --path Samples/DodgeTheCreeps.Advanced
godot --path Samples/DodgeTheCreeps.Advanced
```

Windows 本轮源项目验收使用官方 4.7.2 mono Windows 编辑器、.NET 10，以及进程级的 DOTNET_ROLL_FORWARD=LatestMajor。游戏和编辑器工具的 runtimeconfig 文件保持原样；编辑器工具的默认目标是 .NET 8，只有 .NET 10 的本机可这样启动：

```powershell
$env:DOTNET_ROLL_FORWARD = "LatestMajor"
godot --path Samples/DodgeTheCreeps.Advanced
```

也可以准备 .NET 8 运行时满足编辑器工具的默认要求。上述环境说明对应本例的实际验收，不扩展为其他运行时版本的兼容承诺。

如果使用 NuGet 而非仓库项目引用，应使用与当前文档对应的 Core、Game、Godot 和 Harmony 包。外部模组共享 ModApi 契约，不能把另一份契约程序集打包成模组私有类型。

## 先确定纯规则和应用端口

GameSession 分别记录当前局是否退休、磁盘检查点是否因死亡而失效、最后成功保存的位置，以及需要明确放弃的变更。它不自行读文件或查找世界。死亡先退休，再尝试清档；清档失败后即使重试新局，旧检查点在当前进程也不会重新提供 Continue。只有清档成功或新局成功保存后，才解除旧检查点的禁用状态。

<<< ../../../Samples/DodgeTheCreeps.Advanced.Core/Gameplay/GameSession.cs{csharp}

RunSnapshot 是普通数值与稳定内容 ID；完整敌人、计时、朝向、动画进度和随机状态必须保留。RunRules 验证这些值，Godot 侧另外检查实际内容表与动画资源兼容性。

应用需要两个端口：

<<< ../../../Samples/DodgeTheCreeps.Advanced.Core/Gameplay/IRunBoard.cs{csharp}

<<< ../../../Samples/DodgeTheCreeps.Advanced.Core/Progress/IRunStore.cs{csharp}

IRunBoard 负责准备、开始、恢复和捕获棋盘；IRunStore 负责完整文档与模组状态。两者不向 Core 暴露 Node、SaveRepository、JSON 或 UI 类型。

## 用例控制副作用顺序

继续游戏先完成文档准备和玩家确认，再替换原生棋盘。玩家取消恢复时，原有世界不受影响。保存成功后才更新 checkpoint，写入失败必须保留未保存状态。

<<< ../../../Samples/DodgeTheCreeps.Advanced.Core/Gameplay/RunUseCases.cs{csharp}

此处的应用对象可以直接通过构造函数实例化。它不需要 Architecture、生成器或测试容器；测试使用两个明确的端口替身。

## 在宿主实现适配

GameBoardAdapter 将端口映射到 UiService、SceneRouter 和真实棋盘。先清理当前 UI，再切换场景，准备成功后由用例开始或恢复。

<<< ../../../Samples/DodgeTheCreeps.Advanced/Gameplay/GameBoardAdapter.cs{csharp}

GameSceneFactory 显式接收内容、Mod 计分、GameSession 和展示通知。世界结束时只发出类型明确的展示通知，由应用调用死亡用例；世界节点不写文件、不弹对话框。

SaveService 使用现有 SaveDocument 仓储，在返回 Core 的快照前完成版本转换、内容修复及必要的确认。文档恢复先操作独立候选，确认后备份原始字节并提交。ModService 单独维护每模组 API 注册和当前局的 ModSaveSession；初始化 Mod 时只登记回调，不读取尚未开始的局面状态。

## 组合依赖并声明生命周期

AdvancedGameArchitecture 是唯一容器接触面。普通 AddSingleton 仅提供对象；需要启动的内容 IO 通过 AddComponent 显式登记。内容组件顺序为 -100，与 Model/System 命名无关。

<<< ../../../Samples/DodgeTheCreeps.Advanced/Composition/AdvancedGameArchitecture.cs#advanced-composition{csharp}

UiService 在构造时复制注册表，之后修改原 UiRegistry 不会影响已经存在的服务。因此，组合根先登记全部强类型路由；每个路由工厂通过明确的 `Func<DodgeMenus>` 延迟取得菜单实例。注册路由时不构造菜单，避免“菜单 → 应用 → UI → 菜单”的构造环。宿主启动钩子只实例化、接线菜单并显示页面，不在 Ready 后补登记路由。

UI 对话框回复与业务互斥分开：RunUseCases 串行开始、保存、退休和离开；宿主只对开始、返回菜单和结束画面串行导航。关闭对话框的回调不进入导航 gate，因此等待玩家选择不会阻塞其回复。保存前记录变化版本，完成后保留期间的新变更；排队操作还会核对当前局身份，旧保存不能作用于新局。

AdvancedGameRoot 只负责宿主生命周期和原生子节点所有权。所有可能接触场景树的启动、停止操作通过 IGameDispatcher 执行。应用先停止 UI 接入并等待已接受动作，再停止场景和 Mod；延迟删除与音频释放完成后，provider 释放其拥有的资源。

<<< ../../../Samples/DodgeTheCreeps.Advanced/AdvancedGameRoot.cs{csharp}

原生自检从 Ready 后的新引擎回调开始，经 dispatcher 进入宿主上下文，避免从架构初始化回调重入等待自身销毁。读取文件后还要操作节点的异步流程保留该上下文；自检与普通用户动作遵守相同的线程约束。普通项目不需要保留这些自检入口。

## 设置、内容与状态各自拥有边界

SettingsService 从设置视图接收草稿副本，先使用纯 SettingsRules 验证，再捕获原生音频/窗口状态。应用原生设置或持久化失败时恢复原生状态；只有全部完成才替换已发布 Preferences。最高分更新和语言即时保存进入同一串行更新边界。

DodgeContent 负责已经生效的不可变内容会话；ContentSelectionService 修改的是下次启动的选择。修改选择不会改变当前棋盘，代码执行同意绑定版本和指纹。ModService 不参与全局静态服务注册。

GamePresentationState 只提供分数、状态信息和局面结束通知。菜单取得明确的命令入口和状态，退出场景时解除订阅。它不提供任意 GetService 能力。

## 验证并继续扩展

```bash
dotnet test Samples/DodgeTheCreeps.Advanced.Tests -c Release
godot --headless --path Samples/DodgeTheCreeps.Advanced -- --smoke
godot --headless --path Samples/DodgeTheCreeps.Advanced -- --smoke-continue=<上一轮输出GUID>
```

纯测试覆盖恢复取消、持久化失败、死亡后清档、保存后移动、输入验证及程序集依赖方向。原生 smoke 检查真实按钮、输入、暂停和场景，仍需通过 persistence、settings、localization、content、modding 的跨进程入口验证完整功能，命令见[进阶示例说明](https://github.com/GeWuYou/GFramework/blob/main/Samples/DodgeTheCreeps.Advanced/README.md)。

新增一个“结束后统计”功能时，可以沿用以下步骤：

1. 将统计值和计算规则放到相应 Core 功能中，明确它来自已结束的局面。
2. 在明确的用例中取得结果；需要持久化时扩展真实的存储端口。
3. 在宿主映射文档字段和显示内容，通过现有展示通知更新页面。
4. 添加规则与用例测试，再运行受影响的原生入口。

当功能没有外部依赖或生命周期时，直接添加普通类或纯函数即可。目录和接口用于维护这些真实边界，不要求每个新功能经历全部层次。
