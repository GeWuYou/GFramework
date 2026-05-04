# CQRS 重写迁移追踪

## 2026-04-30

### 阶段：PR #307 active 入口收敛（CQRS-REWRITE-RP-076）

- 继续沿用 `$gframework-pr-review` 对 `PR #307` 做 latest-head triage，本轮只处理仍成立的 `ai-plan` 恢复入口问题
- 主线程确认当前远端权威信号：
  - 当前分支对应 `PR #307`，状态为 `OPEN`
  - 远端 `CTRF` 最新汇总为 `2247/2247` passed
  - `MegaLinter` 仅剩 `dotnet-format` 的 `Restore operation failed` 环境噪音
  - 仍未闭环的 review 重点集中在 `cqrs-rewrite` active tracking / trace 仍保留过多历史锚点，而非新的运行时代码缺陷
- 本轮决策：
  - 将 active tracking 收敛为单一恢复入口，只保留 `RP-076`、`PR #307`、活跃风险、最近权威验证与下一推荐步骤
  - 将 active trace 收敛为当前阶段的关键事实与决策，不再在默认恢复入口中保留 `RP-062` 之后的长阶段流水账
  - 新增 `archive/traces/cqrs-rewrite-history-rp062-through-rp076.md` 承接 `RP-062` 至 `RP-076` 的详细 trace 历史，保持旧阶段仍可追溯

### 验证（RP-076）

- `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --format json --json-output /tmp/current-pr-review.json`
  - 结果：通过
  - 备注：确认 `PR #307` 的当前 review 重点已收敛到 `ai-plan` 文档收尾
- `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Stream_Invoker_Provider_Metadata_When_Runtime_Lacks_Stream_Provider_Interface|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Stream_Invoker_Provider_Metadata_When_Runtime_Lacks_Stream_Descriptor_Enumerator|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Stream_Invoker_Provider_Metadata_When_Runtime_Lacks_Stream_Descriptor_Type|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Stream_Invoker_Provider_Metadata_When_Runtime_Lacks_Stream_Descriptor_Entry_Type|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_Stream_Invoker_Provider_Metadata_When_Runtime_Contract_Is_Available"`
  - 结果：通过，`5/5` passed

### 当前下一步（RP-076）

1. 继续按 `PR #307` 的 latest-head review 收尾，优先保持 active tracking 与 active trace 的单一锚点一致
2. 若继续推进代码切片，先复核 request 侧是否仍存在与 stream invoker gate 对称的生成合同遗漏
3. 进入下一批前继续使用最小 Release build 或 targeted test 作为权威验证，避免把环境噪音误判为代码问题

## 2026-05-04

### 阶段：request invoker provider gate 对称回归（CQRS-REWRITE-RP-077）

- 使用 `$gframework-batch-boot 25` 继续 `feat/cqrs-optimization` 的 CQRS 收口批次
- 批次目标：在 branch diff 相对 `origin/main` 接近 `25` 个文件前，补齐低风险的 generator 合同回归切片
- 本轮先确认当前 worktree 已无 `local-plan` 遗留恢复入口，随后转入 `cqrs-rewrite` 的 request / stream invoker provider gate 对称性复核
- 结论：
  - 生产代码已经同时检查 request provider、enumerator、descriptor 与 descriptor entry 四项 runtime 合同
  - request 侧测试只覆盖缺少 provider / enumerator，缺少 descriptor / descriptor entry 的回归覆盖落后于 stream 侧
- 已补齐：
  - `Does_Not_Emit_Request_Invoker_Provider_Metadata_When_Runtime_Lacks_Request_Descriptor_Type`
  - `Does_Not_Emit_Request_Invoker_Provider_Metadata_When_Runtime_Lacks_Request_Descriptor_Entry_Type`
  - source emission XML 文档同步说明 provider gate 依赖完整 descriptor / descriptor entry 合同

### 验证（RP-077）

- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Request_Invoker_Provider_Metadata_When_Runtime_Lacks_Request_Descriptor_Type|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Request_Invoker_Provider_Metadata_When_Runtime_Lacks_Request_Descriptor_Entry_Type|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Request_Invoker_Provider_Metadata_When_Runtime_Lacks_Request_Provider_Interface|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Request_Invoker_Provider_Metadata_When_Runtime_Lacks_Request_Descriptor_Enumerator"`
  - 结果：通过，`4/4` passed
- `dotnet build GFramework.Cqrs.SourceGenerators/GFramework.Cqrs.SourceGenerators.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`
- `python3 scripts/license-header.py --check`
  - 结果：通过
  - 备注：当前 WSL worktree 需要显式绑定 `GIT_DIR` / `GIT_WORK_TREE` 后运行，避免脚本内部 plain `git ls-files` 误判仓库上下文
- `git diff --check`
  - 结果：通过

### 当前下一步（RP-077）

1. 继续使用 `origin/main` 作为 `$gframework-batch-boot 25` 的基线，复算 branch diff 后决定是否还能接下一批
2. 若继续推进代码切片，优先查找 request / stream invoker provider runtime 合同之外的同类对称测试缺口

### 阶段：mixed fallback attribute usage 回归（CQRS-REWRITE-RP-078）

- 继续沿用 `$gframework-batch-boot 25`，当前 branch diff 仍低于阈值
- 复核 fallback metadata runtime contract 后确认：
  - mixed fallback 在 runtime 允许多个 fallback attribute 实例时已有直接 `Type` + 字符串拆分回归
  - runtime 同时支持 `params Type[]` / `params string[]` 但不允许多个 fallback attribute 实例时，缺少锁定“整体回退到单个字符串 attribute”的回归
- 已补齐：
  - `Emits_String_Fallback_Metadata_For_Mixed_Fallback_When_Runtime_Disallows_Multiple_Fallback_Attributes`
  - `ReplaceAttributeUsageForType` 测试辅助方法，用于构造 runtime attribute usage 变体而不复制大型 source fixture

### 验证（RP-078）

- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_String_Fallback_Metadata_For_Mixed_Fallback_When_Runtime_Disallows_Multiple_Fallback_Attributes"`
  - 结果：通过，`1/1` passed
- `python3 scripts/license-header.py --check`
  - 结果：通过
  - 备注：当前 WSL worktree 需要显式绑定 `GIT_DIR` / `GIT_WORK_TREE` 后运行
- `git diff --check`
  - 结果：通过

### 当前下一步（RP-078）

1. 继续复算 branch diff vs `origin/main`，若仍低于 `25` 个文件可继续下一批
2. 下一批优先查看 fallback metadata 与 generated invoker provider 之外是否还有同类 runtime contract gate 回归缺口
