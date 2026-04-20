# CQRS 重写迁移跟踪

## 目标

围绕 `GFramework` 当前的双轨 CQRS 现状，继续完成以“去外部依赖、降低反射、收口公开入口”为目标的
CQRS 迁移与收敛。

## 当前恢复点

- 恢复点编号：`CQRS-REWRITE-RP-050`
- 当前阶段：`Phase 8`
- 当前焦点：
  - 当前功能历史已归档，active 跟踪仅保留 `Phase 8` 主线的恢复入口
  - 已完成 generated registry 激活路径收敛：`CqrsHandlerRegistrar` 现优先复用缓存工厂委托，避免重复 `ConstructorInfo.Invoke`
  - 已补充私有无参构造 generated registry 的回归测试，确保兼容现有生成器产物
  - 已修正 pointer / function pointer 泛型合同的错误覆盖：生成器不再为这两类类型发射 precise runtime type 重建代码
  - 已补充非法 CQRS 泛型合同的输入诊断断言，明确 `CS0306` 与 fallback / diagnostic 路径的组合语义
  - 已为 registrar 的 reflection 注册路径补充 handler-interface 元数据缓存，减少跨容器重复注册时的 `GetInterfaces()` 反射
  - 已将 registrar 的重复映射判定从线性扫描 `IServiceCollection` 收敛为本地映射索引，减少 fallback 注册路径的重复查找
  - 中期上继续 `Phase 8` 主线：参考 `ai-libs/Mediator`，继续扩大 generator 覆盖，并选择下一个收益明确的 dispatch / invoker 反射收敛点

## 当前状态摘要

- 已完成 `Mediator` 外部依赖移除、CQRS runtime 重建、默认架构接线和显式程序集 handler 注册入口
- 已完成 `GFramework.Cqrs.Abstractions` / `GFramework.Cqrs` 项目骨架与 runtime seam 收敛
- 已完成 handler registry generator 的多轮收敛，当前合法 closed handler contract 已统一收敛到更窄的注册路径
- 已完成一轮公开入口文档与 source-generator 命名空间收口
- 已接入 `$gframework-pr-review`，可直接抓取当前分支对应 PR 的 CodeRabbit 评论、checks 和测试结果

## 当前活跃事实

- `Phase 8` 仍是当前主线，不再回退到 `Phase 7`
- `2026-04-20` 已重新执行 `$gframework-pr-review`：
  - 当前分支对应 `PR #261`，状态为 `OPEN`
  - latest reviewed commit 当前剩余 `1` 条 open CodeRabbit thread，指向 `ai-plan/public/cqrs-rewrite/traces/cqrs-rewrite-migration-trace.md` 中 `RP-047` 与 `RP-050` 的历史语义冲突
  - 本地已同步修正该追踪歧义：`RP-047` 明确标注为已被 `RP-050` 覆盖，后续不得恢复 `MakePointerType()` precise registration
  - 远端测试信号保持通过：最新 CTRF 汇总为 `2118/2118` passed；MegaLinter 仅剩 `dotnet-format` restore failure 预警，当前未提供本地仍然成立的文件级格式问题
- `2026-04-20` 已完成一轮冷启动反射收敛：
  - generated registry 类型首次分析后，会缓存一个可复用的激活工厂，而不是在后续容器注册时重复走 `ConstructorInfo.Invoke`
  - 若运行环境不允许动态方法，仍保留原有的反射激活回退，避免阻塞 generated registry 路径
  - `GFramework.Cqrs.Tests` 已补充“私有无参构造 registry 仍可激活”的回归覆盖
- `2026-04-20` 已完成一轮 generator 覆盖面扩展：
  - `CqrsHandlerRegistryGenerator` 现会在 runtime type 建模入口直接拒绝 `IPointerTypeSymbol` 与 `IFunctionPointerTypeSymbol`
  - `CanReferenceFromGeneratedRegistry` 不再递归判断 pointer / function pointer 的内部元素，而是统一返回 `false`
  - 相关 source-generator 回归已改为区分输入源诊断与生成源诊断，避免把非法泛型合同误判为成功生成
- `2026-04-20` 已完成一轮 registrar reflection 路径收敛：
  - `CqrsHandlerRegistrar` 现会按 `Type` 弱键缓存已筛选且排序好的 supported handler interface 列表
  - 同一 handler 类型跨容器重复注册时，不再重复执行 `GetInterfaces()` 与支持接口筛选
  - `GFramework.Cqrs.Tests` 已补充 registrar 静态缓存隔离与 supported interface 缓存复用回归
- `2026-04-20` 已完成一轮 registrar 去重路径收敛：
  - `CqrsHandlerRegistrar` 现会在单次 reflection 注册流程开始时构建已注册 handler 映射索引
  - 同一批注册中后续 duplicate handler mapping 不再重复线性扫描 `IServiceCollection`
  - `GFramework.Cqrs.Tests` 已补充“程序集返回重复 handler 类型时仍只注册一份映射”的回归
- 当前主线优先级：
  - generator 覆盖面继续扩大
  - dispatch/invoker 反射占比继续下降
  - package / facade / 兼容层继续收口

## 当前风险

- 当前 `dotnet build GFramework.sln -c Release` 在 WSL 环境仍会受顶层 `GFramework.csproj` 的 Windows NuGet fallback 配置影响
- 当前 `GFramework.Cqrs.Tests` 仍直接引用 `GFramework.Core`，说明测试已按模块意图拆分，但 runtime 物理迁移尚未完全切断依赖
- `RegisterMediatorBehavior`、`MediatorCoroutineExtensions` 与 `ContextAwareMediator*Extensions` 仍作为兼容层存在，未来真正移除时仍需单独规划弃用窗口

## 活跃文档

- 模块拆分计划：`ai-plan/migration/CQRS_MODULE_SPLIT_PLAN.md`
- 历史跟踪归档：[cqrs-rewrite-history-through-rp043.md](../archive/todos/cqrs-rewrite-history-through-rp043.md)
- 历史 trace 归档：[cqrs-rewrite-history-through-rp043.md](../archive/traces/cqrs-rewrite-history-through-rp043.md)

## 验证说明

- `RP-043` 之前的详细阶段记录、定向验证命令和阶段性决策均已移入主题内归档
- active 跟踪文件只保留当前恢复点、当前活跃事实、风险和下一步，避免 `boot` 在默认入口中重复扫描 1000+ 行历史 trace
- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --no-restore -p:RestoreFallbackFolders= -m:1 -nodeReuse:false`
  - 结果：通过
  - 备注：`63/63` 测试通过；当前沙箱限制了 MSBuild named pipe，验证需在提权环境下运行
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore -p:RestoreFallbackFolders= -m:1 -nodeReuse:false --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests"`
  - 结果：通过
  - 备注：`14/14` 测试通过；本轮覆盖 pointer / function pointer 合同拒绝、fallback 诊断与现有精确注册路径
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore -p:RestoreFallbackFolders= -m:1 -nodeReuse:false --filter "FullyQualifiedName~Reports_Compilation_Error_And_Skips_Precise_Registration_For_Hidden_Pointer_Response|FullyQualifiedName~Reports_Diagnostic_And_Skips_Registry_When_Fallback_Metadata_Is_Required_But_Runtime_Contract_Lacks_Fallback_Attribute|FullyQualifiedName~Emits_Assembly_Level_Fallback_Metadata_When_Fallback_Is_Required_And_Runtime_Contract_Is_Available"`
  - 结果：通过
  - 备注：`3/3` 测试通过；本轮直接覆盖 PR #261 指向的 3 个 pointer / function pointer 回归场景
- `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --no-restore -p:RestoreFallbackFolders= -m:1 -nodeReuse:false --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsHandlerRegistrarTests"`
  - 结果：通过
  - 备注：`11/11` 测试通过；本轮覆盖 registrar 的 supported handler interface 缓存与 duplicate mapping 去重路径

## 下一步

1. 继续 `Phase 8` 主线，优先再找一个收益明确的 generator 覆盖缺口或 dispatch / invoker 反射收敛点继续推进
2. 若继续文档主线，优先再扫 `docs/zh-CN/api-reference` 与教程入口页，补齐仍过时的 CQRS API / 命名空间表述
3. 若后续再出现新的 PR review 或 review thread 变化，再重新执行 `$gframework-pr-review` 作为独立验证步骤
