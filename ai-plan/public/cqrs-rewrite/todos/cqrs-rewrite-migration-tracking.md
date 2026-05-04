# CQRS 重写迁移跟踪

## 目标

围绕 `GFramework` 当前的双轨 CQRS 现状，继续完成以“去外部依赖、降低反射、收口公开入口”为目标的
CQRS 迁移与收敛。

## 当前恢复点

- 恢复点编号：`CQRS-REWRITE-RP-077`
- 当前阶段：`Phase 8`
- 当前 PR 锚点：`PR #307`
- 当前结论：
  - `GFramework.Cqrs` 已完成对外部 `Mediator` 的生产级替代，当前主线已从“是否可替代”转向“仓库内部收口与能力深化顺序”
  - `dispatch/invoker` 生成前移已扩展到 request / stream 路径，当前 `RP-077` 已补齐 request invoker provider gate 与 stream gate 对称的 descriptor / descriptor entry runtime 合同回归
  - `ai-plan` active 入口现以 `PR #307` 和 `RP-077` 为唯一权威恢复锚点；更早 PR 与阶段细节均以下方归档为准

## 当前活跃事实

- 当前分支对应 `PR #307`，状态为 `OPEN`
- latest-head review 仍以 `ai-plan` 恢复文档收敛为主要待闭环项；代码与测试侧的本地有效问题已收敛
- 远端 `CTRF` 最新汇总为 `2247/2247` passed
- `MegaLinter` 当前只暴露 `dotnet-format` 的 `Restore operation failed` 环境噪音，尚未提供本地仍成立的文件级格式诊断

## 当前风险

- 顶层 `GFramework.sln` / `GFramework.csproj` 在 WSL 下仍可能受 Windows NuGet fallback 配置影响，完整 solution 级验证成本高于模块级验证
- 仓库内部仍保留旧 `Command` / `Query` API、`LegacyICqrsRuntime` alias 与部分历史命名语义，后续若不继续分批收口，容易混淆“对外替代已完成”与“内部收口未完成”
- 若继续扩大 generated invoker 覆盖面，需要持续区分“可静态表达的合同”与 `PreciseReflectedRegistrationSpec` 等仍需保守回退的场景

## 最近权威验证

- `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --format json --json-output /tmp/current-pr-review.json`
  - 结果：通过
  - 备注：确认当前分支对应 `PR #307`，本轮剩余 open AI feedback 主要集中在 `ai-plan` 收敛
- `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Stream_Invoker_Provider_Metadata_When_Runtime_Lacks_Stream_Provider_Interface|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Stream_Invoker_Provider_Metadata_When_Runtime_Lacks_Stream_Descriptor_Enumerator|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Stream_Invoker_Provider_Metadata_When_Runtime_Lacks_Stream_Descriptor_Type|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Stream_Invoker_Provider_Metadata_When_Runtime_Lacks_Stream_Descriptor_Entry_Type|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_Stream_Invoker_Provider_Metadata_When_Runtime_Contract_Is_Available"`
  - 结果：通过，`5/5` passed
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Request_Invoker_Provider_Metadata_When_Runtime_Lacks_Request_Descriptor_Type|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Request_Invoker_Provider_Metadata_When_Runtime_Lacks_Request_Descriptor_Entry_Type|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Request_Invoker_Provider_Metadata_When_Runtime_Lacks_Request_Provider_Interface|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Request_Invoker_Provider_Metadata_When_Runtime_Lacks_Request_Descriptor_Enumerator"`
  - 结果：通过，`4/4` passed
- `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`
- `python3 scripts/license-header.py --check`
  - 结果：通过
  - 备注：当前 WSL worktree 需要显式绑定 `GIT_DIR` / `GIT_WORK_TREE` 后运行，避免脚本内部 plain `git ls-files` 误判仓库上下文

## 下一推荐步骤

1. 继续处理 `PR #307` 的剩余 review 收尾，优先保持 `ai-plan` active 入口与 trace 的单一锚点一致
2. 若继续推进代码切片，优先复核 request / stream invoker provider runtime 合同以外是否还有同类对称测试缺口
3. 在进入下一批 runtime / generator 收敛前，保持最小 Release build 或 targeted test 作为权威验证

## 活跃文档

- 历史跟踪归档：[cqrs-rewrite-history-through-rp043.md](../archive/todos/cqrs-rewrite-history-through-rp043.md)
- 验证历史归档：[cqrs-rewrite-validation-history-through-rp062.md](../archive/todos/cqrs-rewrite-validation-history-through-rp062.md)
- `RP-063` 至 `RP-074` 验证归档：[cqrs-rewrite-validation-history-rp063-through-rp074.md](../archive/todos/cqrs-rewrite-validation-history-rp063-through-rp074.md)
- `RP-062` 至 `RP-076` trace 归档：[cqrs-rewrite-history-rp062-through-rp076.md](../archive/traces/cqrs-rewrite-history-rp062-through-rp076.md)
- CQRS 与 Mediator 评估归档：[cqrs-vs-mediator-assessment-rp063.md](../archive/todos/cqrs-vs-mediator-assessment-rp063.md)
- 历史 trace 归档：[cqrs-rewrite-history-through-rp043.md](../archive/traces/cqrs-rewrite-history-through-rp043.md)
- `RP-046` 至 `RP-061` trace 归档：[cqrs-rewrite-history-rp046-through-rp061.md](../archive/traces/cqrs-rewrite-history-rp046-through-rp061.md)

## 说明

- `PR #261`、`PR #302`、`PR #305` 及更早阶段的详细过程已不再作为 active 恢复入口；如需追溯，以对应归档文件为准
- active tracking 仅保留当前恢复点、当前风险、最近权威验证与下一推荐步骤，避免 `boot` 落到历史阶段细节
