# 核心治理

架构理由见[核心评估](../architecture/core-assessment.md)和[ADR](../architecture/adr-001-standard-dotnet-foundation.md)。各阶段只在这里维护当前状态。

## 文档治理与设计评估

状态：done。

目标：评估通用 C# 核心，建立架构、开发和路线的文档职责，停用旧计划及归档体系。

验收结论：核心问题已连接源码与诊断依据；当前实现、目标设计及未复现风险已区分。旧受跟踪计划已移除，公开采用说明与工程索引已修正。临时证据不进入项目文档。

## 第一批：DI、资源与实例隔离

状态：done。依赖：文档治理与设计评估。规格见[首批改造设计](../architecture/first-core-batch.md)。

目标：配置只描述依赖，唯一 provider 构建后激活组件；闭合组件停止与容器释放；移除上下文和日志的隐式全局绑定；同步迁移 Game、Godot、ECS 与生成器接入。

验收结论：托管行为覆盖单例身份、Transient/Scoped、外部所有权、同步/异步一次释放、失败与取消清理、重复销毁、两架构隔离及显式生成上下文。

限制：真实 Godot 场景树、原生输入/节点释放、VS Code 交互、Native AOT 和性能尚未验收，托管测试不替代原生宿主验证。完整标准日志接入、CQRS 包独立与旧消息 API 移除留给后续阶段。

## 第二批：CQRS 独立采用与调用入口

状态：done。依赖：第一批完成。

目标：Core 不再强制引用 CQRS runtime/契约；CQRS runtime 使用标准服务解析，Architecture 的便捷接线放在明确集成边界。保留 request、notification、stream 能力，移除旧 Command/Query 执行器、重复封装及兼容别名。

验收：Core-only 不含 CQRS 传递依赖；CQRS-only 可不带 Architecture 使用；pipeline、handler 生命周期、取消、stream 和生成注册行为保持；仓库调用、快照与采用文档同步更新。

实现：Core 与 Core.Abstractions 仅依赖自身契约，构建产物经程序集引用探针确认不含 CQRS；CQRS runtime 基于标准 IServiceProvider/IServiceCollection，行为存在性探测使用 IServiceProviderIsService；注册入口收敛为 AddCqrsRuntime/AddCqrsPipelineBehavior/AddCqrsHandlersFromAssembly 等组合根扩展，架构接线为 UseCqrsRuntime/CqrsRuntimeModule 加上下文扩展入口（仅异步）。旧 Command/Query 执行器体系、Legacy 桥接、TypeForwarders、兼容别名与同步包装移除；重复安装 runtime 显式拒绝。

验收结论：Core 与 Core.Abstractions 保持 CQRS 独立边界；聚合包保留 all-in-one 语义。CQRS 独立采用、架构集成、生命周期与生成注册行为有托管回归覆盖，采用说明与当前入口一致。

限制：本批未验收真实 Godot 场景树、Native AOT、性能及第三方宿主接入。

下一步：进入第三批。

## 第三批：日志标准化与可选角色组织

状态：done。依赖：第一批完成；与第二批保持改动边界。

目标：标准 ILogger/ILoggerFactory 成为接入契约，保留有价值的 Godot/文件输出与配置能力；普通服务不要求 Model/System/Utility 角色或 ContextAware。Architecture 仅保留经实际场景验证的编排价值。

验收：一个使用标准 DI/日志的最小非游戏应用和一个 Godot 接入路径；无进程级日志覆盖；角色便利层不阻碍独立使用。

实现：自有 ILogger 家族（含 LoggerFactoryResolver 进程级解析器、LogContext、自有 LogLevel）删除，契约收敛为 Microsoft.Extensions.Logging 的 ILogger/ILogger<T>/ILoggerFactory/ILoggerProvider。架构实例工厂经 LoggerProperties.LoggerFactory 注入：缺省由实例创建独立控制台工厂并在销毁时释放，外部工厂所有权归调用方；工厂与 ILogger<> 开放泛型注册进容器，组件经构造注入 ILogger<T>。appender/formatter/filter 输出引擎保留，新增 AppenderLoggerProvider 把标准日志调用转换为 LogEntry；配置加载器返回标准工厂，JSON 级别名采用 MEL 语义。Godot 输出引擎（GD.Print/模板/颜色/appsettings 首载与热重载）以 GodotLog 标准提供者接入，UseAsDefaultProvider 全局覆盖路径删除。[Log] 生成器改为生成经架构上下文解析的实例属性（默认 `_log`），要求上下文访问能力并新增 GF_Logger_001 诊断，IsStatic 移除；CQRS 注册生成器与 CqrsHandlerRegistry 契约同步迁移。AddCqrsRuntime 收敛为 AddCqrsRuntime()/AddCqrsRuntime(ILoggerFactory?)/AddCqrsRuntime(ILogger)。新增 Samples/MinimalApp（纯标准 DI+日志路径与可选 Architecture 路径）与 Samples/GodotIntegration（Godot 接入路径）。

验收结论：标准 DI 与可选 Architecture 两条非游戏路径均能完成启动、处理和销毁；Godot 托管接入采用标准日志提供者。架构实例日志工厂相互隔离，进程级日志覆盖入口已移除，输出转换与生成注册行为有回归覆盖。

限制：本批未验收真实 Godot 场景树与实机日志输出、Native AOT 及性能；托管验证不替代原生宿主验证。

下一步：后续收敛以本路线图为准；Godot 实机日志输出与 Generic Host 集成按需单独立项。
