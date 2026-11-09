---
title: 设置系统
description: 通过显式依赖组织设置数据、迁移、应用和持久化，并观察初始化与应用失败。
---

# 设置系统

`SettingsModel<TRepository>` 管理设置数据、迁移和应用器；`SettingsSystem` 编排应用、保存和重置。两者都是普通服务，通过构造函数接收依赖，可直接使用或交给标准 DI 管理。

## 数据与应用器

- `ISettingsData` 定义版本、重置和 `LoadFrom`。载入后更新已有实例，业务对象可继续持有同一引用。
- `IResetApplyAbleSettings` 包含数据实例、数据类型、重置和带 `CancellationToken` 的 `ApplyAsync`。
- `IDataLocationProvider` 将设置数据类型映射到存储位置。
- `ISettingsDataRepository` 执行加载与保存；统一文件采用 `UnifiedSettingsDataRepository`。

模型通过 `GetData<T>()` 创建并登记数据，通过 `RegisterApplicator` 登记应用器，通过 `RegisterMigration` 登记版本迁移。依赖在构造时确定，无需设置上下文或调用同步初始化。

## 采用流程

```csharp
using GFramework.Game.Data;
using GFramework.Game.Setting;

var repository = new UnifiedSettingsDataRepository(storage, serializer);
var model = new SettingsModel<UnifiedSettingsDataRepository>(locations, repository, dispatcher: dispatcher);
var audio = model.GetData<AudioSettingsData>();
model.RegisterApplicator(audioApplicator);
await model.InitializeAsync(cancellationToken);

var settings = new SettingsSystem(model, dispatcher: dispatcher);
await settings.ApplyAllAsync(cancellationToken);
await settings.SaveAllAsync(cancellationToken);
```

`storage`、`serializer`、`locations`、`AudioSettingsData` 和 `audioApplicator` 由项目提供。`dispatcher` 是宿主共享的 `IGameDispatcher`；每个应用器都通过它运行。纯托管、无线程要求的设置可以省略该参数，缺省使用 `InlineGameDispatcher`。仓库在加载前完成类型登记；应用器的 `Data` 应与模型中使用的数据实例一致。

最小流程是先登记、加载，再应用。保存由明确的用户或业务动作触发，载入失败时保留现有文件。

## 初始化与迁移

`InitializeAsync` 先从仓库获取已登记数据，再独立准备各 section 的迁移结果。所有加载和迁移成功后才调用 `LoadFrom` 更新现有业务实例，并将 `IsInitialized` 设为 `true`。

任一 section 的读取、类型或迁移失败会收集为 `AggregateException`；当前业务数据保留，`IsInitialized` 为 `false`，不发出初始化成功通知。取消通过 `OperationCanceledException` 返回。

迁移必须前进，不能为同一设置类型登记重复的起始版本。加载使用迁移表快照；缺链、非法版本和不兼容结果会失败。设置模型不在初始化失败时保存默认值。

项目实现的 `LoadFrom` 应只负责复制已验证的数据，保持确定性，并避免 I/O 或其他不可恢复副作用。

## 应用、保存和重置

`SettingsSystem` 提供：

- `ApplyAllAsync`：尝试所有独立应用器，收集失败后抛出聚合异常。
- `ApplyAsync<T>`：应用指定应用器；不存在时正常结束。
- `SaveAllAsync`：委托模型和仓库保存所有数据，写入失败向调用方传播。
- `ResetAsync<T>`：重置指定数据，再应用对应应用器。
- `ResetAllAsync`：重置全部数据和应用器，再重新应用。

每个异步入口的最后一个参数都是 `CancellationToken`。取消在后续操作开始前检查；已接纳的底层持久化任务等待到完成。

## 通知与失败

构造函数可接收 `Action<object>? publishEvent`，将通知接入宿主自己的事件机制。未提供回调时，服务只执行操作。

应用器执行前发布 `SettingsApplyingEvent`，完成后发布携带 `Success` 和错误信息的 `SettingsAppliedEvent`。独立应用器的失败不会阻止后续应用器尝试，最终通过聚合异常向调用方报告。保存和初始化成功通知只在对应操作成功后发布。

取消是独立的控制流程，不转成普通应用失败通知。业务按钮、启动流程和关闭流程都应观察返回任务。

## 迁移

- 用构造注入替换 `GetModel`、`GetUtility` 和上下文绑定。
- 将 `ApplyAll`、`Apply<T>`、`SaveAll`、`Reset<T>`、`ResetAll` 分别迁移到带 `Async` 后缀的接口。
- 给设置应用器的 `ApplyAsync` 增加最后一个取消令牌参数。
- 需要通知时显式传入回调，原有设置文件和 section 布局继续使用。

## 相关主题

- [数据与存档系统](./data.md)
- [存储系统](./storage.md)
- [Godot 设置接入](../godot/setting.md)

## 设置观察通知

设置初始化、应用、保存和重置通知只观察操作结果。直接订阅者异常记录到日志后隔离，不撤销已应用的设置，不把成功结果重新发布为失败，也不阻止独立应用器继续执行。`SettingsSystem` 可接收 `observerLogger`，`SettingsModel` 使用其构造日志。应用器、迁移器和持久化本身的失败仍向调用方报告。
