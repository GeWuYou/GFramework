# 核心治理任务

架构理由见[核心评估](../architecture/core-assessment.md)和[ADR](../architecture/adr-001-standard-dotnet-foundation.md)。本文件是各阶段状态的唯一记录。

## 文档治理与设计评估

状态：done。日期：2026-11-07。源码基线：5b727587f8860761a0c132203cb4d7e0cdae30af。

目标：完成当前核心评估与首批改造设计，替换旧开发文档体系；不改运行时代码，不推送、创建 PR、打 tag 或发布。

验收：

- 评估结论连接源码，区分已观察、静态事实与待验证风险。
- 工程索引覆盖架构、开发和路线；清除旧跟踪/归档及无效入链，保留私人文件和其他任务。
- 公开采用说明与实际传递依赖、全局上下文及资源释放限制一致。
- 相关文档校验、站点构建和 Core Release 构建通过，检查结果与未验证范围明确。
- 审查完整 diff，确认源码、公共 API、生成快照、包依赖和发布流程不变。

已观察：最小诊断复现两份 Singleton、第二上下文启动被拒绝，以及 Architecture 销毁未释放正式 provider；显式容器 Dispose 可释放正式 provider 的同步资源。

验证：

- Core Release 构建通过，net8.0/net9.0/net10.0，0 warning / 0 error；使用进程级用户 SDK 10.0.403。工具库存中的系统 dotnet 不在当前 PATH，未修改机器配置。
- 七篇修改的站点文档通过 frontmatter、链接与代码围栏检查；模块/根 README 和十份工程文档通过适用链接/围栏检查，工程索引全部可达。
- VitePress 构建通过；仍提示存在超过 1000 kB 的压缩 bundle，本轮未改变打包阈值或依赖。
- 许可证、doc-refresh skill 结构与 diff 格式检查通过；旧计划有效导航已清除，仅保留保护私人残留的忽略项。
- 三项行为诊断结果见核心评估。全量 .NET 测试、真实 Godot/VS Code 宿主、失败/取消/异步资源回归未执行；本轮未修改运行时，不宣称其问题已修复。
- 探针源码以文本及原始输出保存在忽略的 .local/core-assessment；探针构建输出已移除，避免参与聚合项目的默认源码扫描。

下一步：本轮在本地 docs 提交后结束；需要继续核心代码治理时，从下一阶段和首批设计进入，不恢复历史执行流水。

## 第一批：DI、资源与实例隔离

状态：done。日期：2026-11-07。实现基线：b54d7cac。依赖：上述文档阶段完成。规格见[首批改造设计](../architecture/first-core-batch.md)。

验收：同一 Singleton 实例、一次 provider 构建、阶段前完成注册、同步/异步资源一次释放、外部所有权、失败/取消清理、两架构隔离、生成上下文无全局回退，以及 Game/Godot/ECS 直接使用方接线。

涉及入口：Architecture 配置/初始化/销毁，组件注册、容器与 ContextAware、实例日志、异步阶段 hook；不同时拆包或移除全部旧消息 API。

实现：配置只描述依赖，唯一 provider 构建后激活；异步阶段钩子、取消传播、一次宿主停止和 provider 释放已接通。ContextAware 手写及生成路径均显式实例绑定。Game、Godot、ECS、基准适配器与采用文档同步迁移。

委派结果已由主线审查并通过整体验证：容器所有权/并发释放，上下文生成器/快照/分析器，Game/Godot/ECS 直接使用方。构建和测试由主线串行执行。

实现取舍：标准 DI 会重复跟踪多个工厂 alias 返回的可释放对象。外部实例 alias 使用借用描述；泛型自动 alias 明确拒绝可释放类型，资源组件使用单一 canonical owning 类型键。未新增自有生命周期容器。重复类型组件登记拒绝，相同外部多角色实例只启动一次。

验证：

- `dotnet test GFramework.sln -c Release`：2435 passed / 0 failed / 0 skipped。Core 1693、CQRS 151、Game 254、Godot 91、ECS 36、通用生成器 162、Godot 生成器 48。
- `dotnet build GFramework.sln -c Release`：所有受影响项目及 net8.0/net9.0/net10.0 目标通过，0 warning / 0 error。
- 新回归覆盖实例身份、Transient/Scoped、外部所有权、异步释放屏障、取消回调抛错、清理失败保留、重入与过期执行上下文、linked CTS 释放、多角色去重、稳定模块顺序、Ready 异步完成边界及主线程调度的 ExecutionContext。
- 48 份修改文档的适用校验、58 项检查结果通过；实际站点 HTML 的 4 处入链/自链标题核对通过。VitePress 构建通过，仍有超过 1000 kB 的压缩 bundle 提示。
- 许可证、运行时/生成器边界与 diff 格式通过。命名检查在最终暂存后覆盖新增文件；完整输出保存在忽略的 .local/core-runtime。

边界：未进行真实 Godot 场景树、原生 InputMap/节点释放、VS Code 交互、Native AOT 或性能验收；托管宿主测试和编译不能替代它们。完整 Microsoft.Extensions.Logging 接入、CQRS 包独立与旧消息 API 移除仍是后续阶段。

交付：本批按独立运行时任务进行本地提交与 GitHub PR；前置开发文档提交一并纳入该变更，原文档工作树保留。准确 PR/head、CI 待办与任务租约仅保存在本地交接，不把临时状态扩写成新的执行流水。

下一步：按 adapter 的 PR/自动合并设置后交接策略返回；后续经允许唤醒核对实际合并与资源释放。继续设计开发时进入第二批，不能把本地通过等同于 CI 或原生宿主验收。

## 第二批：CQRS 独立采用与调用入口

状态：done。日期：2026-11-07。实现基线：f651cd53。依赖：第一批完成。

目标：Core 不再强制引用 CQRS runtime/契约；CQRS runtime 使用标准服务解析，Architecture 的便捷接线放在明确集成边界。保留 request、notification、stream 能力，移除旧 Command/Query 执行器、重复封装及兼容别名。

验收：Core-only 不含 CQRS 传递依赖；CQRS-only 可不带 Architecture 使用；pipeline、handler 生命周期、取消、stream 和生成注册行为保持；仓库调用、快照与采用文档同步更新。

实现：Core 与 Core.Abstractions 仅依赖自身契约，构建产物经程序集引用探针确认不含 CQRS；CQRS runtime 基于标准 IServiceProvider/IServiceCollection，行为存在性探测使用 IServiceProviderIsService；注册入口收敛为 AddCqrsRuntime/AddCqrsPipelineBehavior/AddCqrsHandlersFromAssembly 等组合根扩展，架构接线为 UseCqrsRuntime/CqrsRuntimeModule 加上下文扩展入口（仅异步）。旧 Command/Query 执行器体系、Legacy 桥接、TypeForwarders、兼容别名与同步包装移除；重复安装 runtime 显式拒绝。

验证：

- ：0 warning / 0 error。
- ：2340 passed / 0 failed / 0 skipped；新增架构上下文 CQRS 集成、缺失 runtime、生命周期窗口与生成注册回归；旧执行器/桥接套件随子系统删除。
- 命名、许可证与运行时/生成器边界检查通过；VitePress 构建通过（保留既有大 bundle 提示）。
- 探针确认 Core/Core.Abstractions 构建产物零 CQRS 程序集引用；聚合包显式引用 CQRS 保持 all-in-one 语义。
- 未做真实 Godot 场景树、Native AOT 与性能验收；CQRS-only 独立采用路径由测试与文档示例覆盖，未接入真实第三方宿主。

下一步：进入第三批。

## 第三批：日志标准化与可选角色组织

状态：planned。依赖：第一批完成；与第二批保持改动边界。

目标：标准 ILogger/ILoggerFactory 成为接入契约，保留有价值的 Godot/文件输出与配置能力；普通服务不要求 Model/System/Utility 角色或 ContextAware。Architecture 仅保留经实际场景验证的编排价值。

验收：一个使用标准 DI/日志的最小非游戏应用和一个 Godot 接入路径；无进程级日志覆盖；角色便利层不阻碍独立使用。

下一步：以现有 appender/provider 和配置首载场景评估迁移，不为假设的宿主家族扩张 API。
