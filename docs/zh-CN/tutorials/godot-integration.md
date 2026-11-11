---
title: Godot 接入教程
description: 通过可运行的精简场景接入 Architecture、类型化页面、确认窗口和资源仓储。
---

# Godot 接入教程

精简入口位于运行时集成示例的 GettingStarted 场景。它展示资源验证、页面打开、确认结果和完整关闭过程。普通服务通过构造函数取得依赖，Godot 创建的页面在挂载前接收 Configure 参数。

## 构建与运行

从仓库根目录使用 global.json 对应的 SDK 和 Godot 4.7.2 .NET。原生运行环境要求见[运行时示例](../godot/runtime-sample.md)。

```bash
dotnet build Samples/GodotIntegration/GodotIntegration.csproj -c Debug
godot --headless --editor --path Samples/GodotIntegration --import
godot --path Samples/GodotIntegration res://GettingStarted/QuickStart.tscn
```

页面展示按键登记的资源标题。点击打开确认窗口，确认或取消后，底部显示对应结果。关闭窗口会取消尚未回答的确认，等待已开始的操作和 UI 停止，再退出场景树。

## 资源引用与启动

主场景导出页面、确认场景和资源数组。资源实现 IHasKey，以稳定 ID 显式加入 GodotResourceRepository。QuickStartResources 是通过 AddComponent 登记的启动组件，原生验证通过 dispatcher 进入 Godot 线程；全部验证成功后才发布索引。

<<< ../../../Samples/GodotIntegration/GettingStarted/QuickStartItem.cs{csharp}

<<< ../../../Samples/GodotIntegration/GettingStarted/QuickStartResources.cs{csharp}

资源目录只有引用和索引。原生资源由主场景持有，业务键与资源文件名分别维护。资源创建、错误场景根、重复键和缺失引用会在启动阶段暴露。详细操作契约见[资源仓储](../godot/resource.md)。

## Architecture 装配

ConfigureServices 记录依赖描述。资源启动组件先完成验证，UiRegistry 随后登记页面和确认工厂，再创建 UiService。GameRuntimeModule 把服务停止接入原生宿主分离之前。

<<< ../../../Samples/GodotIntegration/GettingStarted/QuickStartArchitecture.cs{csharp}

普通页面使用 RegisterGodotScene。确认工厂使用 RegisterGodotDialog，绑定失败时回收刚创建的实例。业务服务与视图取得实际依赖，容器访问集中在装配入口。

## 页面与确认结果

Configure 在入树前保存依赖，_Ready 访问已实例化的子节点并连接输入。

<<< ../../../Samples/GodotIntegration/GettingStarted/QuickStartPage.cs{csharp}

确认窗口通过自己的 UiDialogContext 完成结果。宿主分别跟踪打开操作和回复操作，等待玩家选择期间仍可处理回复。

<<< ../../../Samples/GodotIntegration/GettingStarted/QuickStartConfirm.cs{csharp}

## 宿主关闭

宿主捕获 Godot 同步上下文并观察启动、输入和关闭任务。退出时关闭新操作入口，取消待确认操作，等待已开始的任务，随后由架构停止 UI 并分离原生 root。

<<< ../../../Samples/GodotIntegration/GettingStarted/QuickStartRoot.cs{csharp}

## 编辑器模板

仓库的 Godot/script_templates 提供普通节点与 Control 页面模板。按 Node、Control 目录结构复制到项目的 `script_templates/`，并保留 `.gdignore`。节点类型目录决定编辑器中的模板适用范围，页面模板归属于 Control。

C# 工程还需排除模板原件，展开后的业务脚本正常参与编译：

```xml
<ItemGroup>
  <Compile Remove="script_templates/**/*.cs" />
</ItemGroup>
```

普通节点模板通过项目自己的 Configure 方法接收依赖。页面模板提供可选的打开和关闭回调，原生实例由 UI 服务拥有。模板目录与占位符约定见 [Godot 脚本模板文档](https://docs.godotengine.org/en/stable/tutorials/scripting/creating_script_templates.html)。

## 原生自检

```bash
godot --headless --path Samples/GodotIntegration res://GettingStarted/QuickStart.tscn -- --quickstart-smoke
```

成功时在完整关闭后输出 `GFRAMEWORK_QUICKSTART_RESULT PASS`。自检验证资源查找、重复键与空引用拒绝、部分启动清理、确认与取消，以及待确认期间退出。引擎诊断与退出码应同时检查；进程级 XDG_DATA_HOME 可指定独立验收目录。

节点注入和信号生成按需采用，调用约定见[节点注入](../source-generators/get-node-generator.md)和[信号绑定](../source-generators/bind-node-signal-generator.md)。后续功能可参考[完整游戏](./godot-complete-project.md)与[进阶项目](./godot-advanced-project.md)。
