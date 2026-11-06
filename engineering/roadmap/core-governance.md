# 核心治理

架构理由见[核心评估](../architecture/core-assessment.md)和[ADR](../architecture/adr-001-standard-dotnet-foundation.md)。各阶段只在这里维护当前状态。

## 文档治理与设计评估

状态：done。

目标：评估通用 C# 核心，建立架构、开发和路线的文档职责，停用旧计划及归档体系。

验收结论：核心问题已连接源码与诊断依据；当前实现、目标设计及未复现风险已区分。旧受跟踪计划已移除，公开采用说明与工程索引已修正。临时证据不进入项目文档。

## 第一批：DI、资源与实例隔离

状态：done。依赖：文档治理与设计评估。规格见[首批改造设计](../architecture/first-core-batch.md)。

目标：配置只描述依赖，唯一 provider 构建后激活组件；闭合组件停止与容器释放；移除上下文和日志的隐式全局绑定；同步迁移 Game、Godot、ECS 与生成器接入。

验收结论：托管测试覆盖单例身份、Transient/Scoped、外部所有权、同步/异步一次释放、失败与取消清理、重复销毁、两架构隔离及显式生成上下文；Release 构建与受影响文档检查通过。

限制：真实 Godot 场景树、原生输入/节点释放、VS Code 交互、Native AOT 和性能尚未验收，托管测试不替代原生宿主验证。完整标准日志接入、CQRS 包独立与旧消息 API 移除留给后续阶段。

## 第二批：CQRS 独立采用与调用入口

状态：planned。依赖：第一批完成。

目标：Core 不再强制引用 CQRS runtime/契约；CQRS runtime 使用标准服务解析，Architecture 的便捷接线放在明确集成边界。保留 request、notification、stream 能力，移除旧 Command/Query 执行器、重复封装及兼容别名。

验收：Core-only 不含 CQRS 传递依赖；CQRS-only 可不带 Architecture 使用；pipeline、handler 生命周期、取消、stream 和生成注册行为保持；仓库调用、快照与采用文档同步更新。

边界：依据现有接口确定最小集成方式，不提前创建额外桥接包。

## 第三批：日志标准化与可选角色组织

状态：planned。依赖：第一批完成；与第二批保持改动边界。

目标：标准 ILogger/ILoggerFactory 成为接入契约，保留有价值的 Godot/文件输出与配置能力；普通服务不要求 Model/System/Utility 角色或 ContextAware。Architecture 仅保留经实际场景验证的编排价值。

验收：一个使用标准 DI/日志的最小非游戏应用和一个 Godot 接入路径；无进程级日志覆盖；角色便利层不阻碍独立使用。

边界：以现有 appender/provider 和配置首载场景评估迁移，不为假设的宿主家族扩张 API。
