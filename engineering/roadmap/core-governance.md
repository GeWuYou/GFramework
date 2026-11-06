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

状态：planned。依赖：上述文档阶段完成。规格见[首批改造设计](../architecture/first-core-batch.md)。

验收：同一 Singleton 实例、一次 provider 构建、阶段前完成注册、同步/异步资源一次释放、外部所有权、失败/取消清理、两架构隔离、生成上下文无全局回退，以及 Game/Godot/ECS 直接使用方接线。

涉及入口：Architecture 配置/初始化/销毁，组件注册、容器与 ContextAware、实例日志、异步阶段 hook；不同时拆包或移除全部旧消息 API。

下一步：按设计先增加能复现现有错误的行为回归，再实施最小闭环；真实宿主缺口单独记录。

## 第二批：CQRS 独立采用与调用入口

状态：planned。依赖：第一批完成。

目标：Core 不再强制引用 CQRS runtime/契约；CQRS runtime 使用标准服务解析，Architecture 的便捷接线放在明确集成边界。保留 request、notification、stream 能力，移除旧 Command/Query 执行器、重复封装及兼容别名。

验收：Core-only 不含 CQRS 传递依赖；CQRS-only 可不带 Architecture 使用；pipeline、handler 生命周期、取消、stream 和生成注册行为保持；仓库调用、快照与采用文档同步更新。

下一步：在第一批语义稳定后确定最小集成接口，不提前创建额外桥接包。

## 第三批：日志标准化与可选角色组织

状态：planned。依赖：第一批完成；与第二批保持改动边界。

目标：标准 ILogger/ILoggerFactory 成为接入契约，保留有价值的 Godot/文件输出与配置能力；普通服务不要求 Model/System/Utility 角色或 ContextAware。Architecture 仅保留经实际场景验证的编排价值。

验收：一个使用标准 DI/日志的最小非游戏应用和一个 Godot 接入路径；无进程级日志覆盖；角色便利层不阻碍独立使用。

下一步：以现有 appender/provider 和配置首载场景评估迁移，不为假设的宿主家族扩张 API。
