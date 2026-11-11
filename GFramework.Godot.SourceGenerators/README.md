# GFramework.Godot.SourceGenerators

`GFramework.Godot.SourceGenerators` 负责把 Godot 项目里的重复样板迁移到编译期。

当前包覆盖三类核心场景：

- `project.godot` 元数据入口：生成 `AutoLoads` 与 `InputActions`
- 节点字段与信号接线：`[GetNode]`、`[BindNodeSignal]`
- Scene 与启动注册样板：`[AutoScene]`、`[AutoRegisterExportedCollections]`

它是 Analyzer 包，不是运行时库。

## 包定位

当前生成器主要减少这些重复代码：

- 从 `project.godot` 手写 AutoLoad / Input Action 字符串
- 在 `_Ready()` 里重复写 `GetNode<T>()`
- 在 `_Ready()` / `_ExitTree()` 里重复写 CLR event 订阅与解绑
- 为 Godot 场景根节点重复声明 `GetScene()` 样板
- 在启动入口里重复遍历导出集合并逐项注册到 registry

它不负责：

- 提供运行时 Scene / UI / 配置实现
- 自动接管完整生命周期方法
- 代替 `GFramework.Godot` 的宿主适配逻辑

## 与相邻包的关系

- `GFramework.Godot`
  - 负责 Godot 运行时适配。
  - 本包只负责编译期入口和样板生成。
- `GFramework.Godot.SourceGenerators.Abstractions`
  - 特性定义所在位置。
  - 当前 `IsPackable=false`，按内部支撑模块处理，不作为独立消费包推广。
- `GFramework.SourceGenerators.Common`
  - 提供公共生成器基础设施与部分类级诊断支持。
  - 同样按内部支撑模块处理。

## 子系统地图

### `GodotProjectMetadataGenerator`

读取 `project.godot`，生成：

- `GFramework.Godot.Generated.AutoLoads`
- `GFramework.Godot.Generated.InputActions`

这是项目级元数据入口，不处理节点字段注入或信号绑定。

### `GetNodeGenerator` 与 `BindNodeSignalGenerator`

- `[GetNode]` 负责生成节点字段注入代码
- `[BindNodeSignal]` 负责生成 CLR event 绑定 / 解绑辅助方法

这两项能力通常一起使用，但职责不同：

- `[GetNode]` 解决“怎么拿到字段实例”
- `[BindNodeSignal]` 解决“字段可用后怎么订阅 / 解绑事件”

### `Behavior/`

- `AutoSceneGenerator`

用于给场景根节点生成稳定的 `GetScene()` 包装入口。

### `Registration/`

- `AutoRegisterExportedCollectionsGenerator`

用于把“遍历导出集合并逐项调用 registry 方法”的启动样板收敛成生成方法。

### `Diagnostics/`

当前诊断围绕这些方向组织：

- `project.godot` 文件与元数据约束
- `GetNode` / `BindNodeSignal` 的目标成员合法性
- `AutoScene` 的宿主类型与参数合法性
- 导出集合注册的成员形状与方法匹配约束

## 最小接入路径

### 1. 安装生成器包

常规 NuGet 引用方式：

```xml
<ItemGroup>
  <PackageReference Include="GeWuYou.GFramework.Godot.SourceGenerators"
                    Version="x.y.z"
                    PrivateAssets="all"
                    ExcludeAssets="runtime" />
</ItemGroup>
```

通常还会同时引用：

```xml
<PackageReference Include="GeWuYou.GFramework.Godot" Version="x.y.z" />
```

### 2. 让 `project.godot` 进入 `AdditionalFiles`

通过 NuGet 包使用时，`GeWuYou.GFramework.Godot.SourceGenerators.targets` 会自动尝试把项目根目录下的 `project.godot`
加入 `AdditionalFiles`。

如果你是仓库内直接通过 `ProjectReference(OutputItemType=Analyzer)` 引用生成器项目，需要手动加入：

```xml
<ItemGroup>
  <ProjectReference Include="..\GFramework.Godot.SourceGenerators\GFramework.Godot.SourceGenerators.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
  <AdditionalFiles Include="project.godot" />
</ItemGroup>
```

### 3. 在节点脚本里显式接生成方法

当前最重要的生命周期约束是：

- `[GetNode]` 在类型手写 `_Ready()` 时，需要显式调用 `__InjectGetNodes_Generated()`
- `[BindNodeSignal]` 在手写 `_Ready()` / `_ExitTree()` 时，需要显式调用
  `__BindNodeSignals_Generated()` 与 `__UnbindNodeSignals_Generated()`
- `[AutoScene]`、`[AutoRegisterExportedCollections]` 都只生成辅助入口，不会替你织入生命周期

手写生命周期的遗漏调用通过既有 Warning 诊断报告。检查支持当前实例的直接调用、同类型实例辅助方法和直接调用的局部函数；其他实例调用及仅注册的回调体会继续提示接线。具体时序由项目侧决定。

最小接法可以直接写成：

```csharp
using GFramework.Godot.SourceGenerators.Abstractions;
using Godot;

public partial class MainMenu : Control
{
    [GetNode]
    private Button _startButton = null!;

    public override void _Ready()
    {
        __InjectGetNodes_Generated();
        __BindNodeSignals_Generated();
    }

    public override void _ExitTree()
    {
        __UnbindNodeSignals_Generated();
    }

    [BindNodeSignal(nameof(_startButton), nameof(Button.Pressed))]
    private void OnStartPressed()
    {
    }
}
```

### 4. 按场景选特性

- 项目级元数据：
  - `project.godot` -> `AutoLoads`、`InputActions`
- 固定节点字段：
  - `[GetNode]`
- 固定 CLR event 订阅：
  - `[BindNodeSignal]`
- Godot 场景根节点：
  - `[AutoScene]`
- 启动入口中的集合批量注册：
  - `[AutoRegisterExportedCollections]`

## 当前约束

- `GFrameworkGodotProjectFile` 可以改相对路径，但文件名必须仍然是 `project.godot`
- `[GetNode]` 与 `[BindNodeSignal]` 都要求宿主类型是顶层 `partial class`
- `[BindNodeSignal]` 面向 CLR event，不会自动调用 `Connect()` / `Disconnect()`
- `[AutoScene]` 生成场景包装入口；导航由显式构造的 `SceneRouter` 驱动。
- `[AutoRegisterExportedCollections]` 只适合“集合 -> registry -> 单参数注册方法”这类稳定形状

## 文档入口

- 生成器总览：[源码生成器总览](../docs/zh-CN/source-generators/index.md)
- Godot 项目元数据：[Godot 项目生成器](../docs/zh-CN/source-generators/godot-project-generator.md)
- `GetNode`：[GetNode 生成器](../docs/zh-CN/source-generators/get-node-generator.md)
- `BindNodeSignal`：[BindNodeSignal 生成器](../docs/zh-CN/source-generators/bind-node-signal-generator.md)
- `AutoScene`：[AutoScene 生成器](../docs/zh-CN/source-generators/auto-scene-generator.md)
- `AutoRegisterExportedCollections`：[AutoRegisterExportedCollections 生成器](../docs/zh-CN/source-generators/auto-register-exported-collections-generator.md)
- Godot 运行时入口：[Godot 运行时说明](../GFramework.Godot/README.md)
- 集成教程：[Godot 集成教程](../docs/zh-CN/tutorials/godot-integration.md)

## 相关入口

以下场景更适合先回到其他入口：

- 你在确认 Godot 运行时 Scene / UI / 存储 / 设置的默认实现：
  - 先看 `GFramework.Godot`
- 你只需要 `Game` 契约，不需要 Godot 宿主或生成器：
  - 先看 `GFramework.Game` 或 `GFramework.Game.Abstractions`
- 你在确认项目接线顺序，而不是单个生成器契约：
  - 先看 [Godot 集成教程](../docs/zh-CN/tutorials/godot-integration.md)

普通 UI 场景通过 [Godot UI 注册](../docs/zh-CN/godot/ui.md) 直接采用；运行时接入无需页面包装生成步骤。
