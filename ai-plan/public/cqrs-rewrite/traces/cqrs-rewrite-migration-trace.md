# CQRS 重写迁移追踪

## 2026-04-30

### 阶段：历史 PR #307 active 入口收敛（CQRS-REWRITE-RP-076）

- 继续沿用 `$gframework-pr-review` 对 `PR #307` 做 latest-head triage，本轮只处理仍成立的 `ai-plan` 恢复入口问题
- 主线程确认当前远端权威信号：
  - 当时分支对应 `PR #307`，状态为 `OPEN`
  - 远端 `CTRF` 最新汇总为 `2247/2247` passed
  - `MegaLinter` 仅剩 `dotnet-format` 的 `Restore operation failed` 环境噪音
  - 仍未闭环的 review 重点集中在 `cqrs-rewrite` active tracking / trace 仍保留过多历史锚点，而非新的运行时代码缺陷
- 本轮决策：
  - 将 active tracking 收敛为单一恢复入口，只保留 `RP-076`、`PR #307`、活跃风险、最近权威验证与下一推荐步骤
  - 将 active trace 收敛为当前阶段的关键事实与决策，不再在默认恢复入口中保留 `RP-062` 之后的长阶段流水账
  - 新增 `archive/traces/cqrs-rewrite-history-rp062-through-rp076.md` 承接 `RP-062` 至 `RP-076` 的详细 trace 历史，保持旧阶段仍可追溯

### 验证（RP-076）

- `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --format json --json-output <temporary-json-output>`
  - 结果：通过
  - 备注：确认 `PR #307` 的当前 review 重点已收敛到 `ai-plan` 文档收尾
- `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`
- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Stream_Invoker_Provider_Metadata_When_Runtime_Lacks_Stream_Provider_Interface|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Stream_Invoker_Provider_Metadata_When_Runtime_Lacks_Stream_Descriptor_Enumerator|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Stream_Invoker_Provider_Metadata_When_Runtime_Lacks_Stream_Descriptor_Type|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Emit_Stream_Invoker_Provider_Metadata_When_Runtime_Lacks_Stream_Descriptor_Entry_Type|FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Emits_Stream_Invoker_Provider_Metadata_When_Runtime_Contract_Is_Available"`
  - 结果：通过，`5/5` passed

### 当前下一步（RP-076）

1. 当时继续按 `PR #307` 的 latest-head review 收尾，优先保持 active tracking 与 active trace 的单一锚点一致
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

### 阶段：基础 generated registry contract gate 回归（CQRS-REWRITE-RP-079）

- 继续沿用 `$gframework-batch-boot 25`，当前 branch diff 仍低于阈值
- 复核 generator 基础启用条件后确认：缺少 `ICqrsHandlerRegistry` 时，runtime 不具备承载 generated registry 的基础接口合同，应整体跳过发射
- 已补齐：
  - `Does_Not_Generate_Registry_When_Runtime_Lacks_Handler_Registry_Interface`

### 验证（RP-079）

- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Generate_Registry_When_Runtime_Lacks_Handler_Registry_Interface"`
  - 结果：通过，`1/1` passed
- `python3 scripts/license-header.py --check`
  - 结果：通过
  - 备注：当前 WSL worktree 需要显式绑定 `GIT_DIR` / `GIT_WORK_TREE` 后运行
- `git diff --check`
  - 结果：通过

### 当前下一步（RP-079）

1. 继续复算 branch diff vs `origin/main`，若仍低于 `25` 个文件可继续下一批
2. 下一批优先复核基础 generation gate 中其他必需 runtime contracts 是否也需要同类回归覆盖

### 阶段：基础 generated registry contract gate 扩展回归（CQRS-REWRITE-RP-080）

- 将 `RP-079` 的单一 handler registry interface 缺失回归扩展为基础 generation gate 参数化测试
- 已补齐缺失分支：
  - `ICqrsHandlerRegistry`
  - `INotificationHandler<TNotification>`
  - `IStreamRequestHandler<TRequest, TResponse>`
  - `CqrsHandlerRegistryAttribute`
- stream handler interface 变体采用类型重命名构造 runtime metadata miss，避免删除命名空间尾部单行接口时引入输入编译错误

### 验证（RP-080）

- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Generate_Registry_When_Runtime_Lacks_Required_Generation_Contract"`
  - 结果：通过，`4/4` passed
- `python3 scripts/license-header.py --check`
  - 结果：通过
  - 备注：当前 WSL worktree 需要显式绑定 `GIT_DIR` / `GIT_WORK_TREE` 后运行
- `git diff --check`
  - 结果：通过

### 当前下一步（RP-080）

1. 继续复算 branch diff vs `origin/main`，若仍低于 `25` 个文件可继续下一批
2. 下一批优先复核基础 generation gate 中 logging / DI 依赖是否已有合适的输入编译安全回归覆盖方式

### 阶段：基础 generated registry external contract gate 回归（CQRS-REWRITE-RP-081）

- 延续 `RP-080` 的参数化基础 generation gate 测试，将外部 logging / DI 依赖也纳入同一组静默跳过回归
- 已补齐缺失分支：
  - `GFramework.Core.Abstractions.Logging.ILogger`
  - `Microsoft.Extensions.DependencyInjection.IServiceCollection`
- 两个变体均通过类型重命名构造 runtime metadata miss，保持输入源码可编译，避免把依赖缺失测试误写成编译失败测试

### 验证（RP-081）

- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Generate_Registry_When_Runtime_Lacks_Required_Generation_Contract"`
  - 结果：通过，`6/6` passed
- `python3 scripts/license-header.py --check`
  - 结果：通过
  - 备注：当前 WSL worktree 需要显式绑定 `GIT_DIR` / `GIT_WORK_TREE` 后运行
- `git diff --check`
  - 结果：通过

### 当前下一步（RP-081）

1. 继续复算 branch diff vs `origin/main`，若仍低于 `25` 个文件可继续下一批
2. 下一批优先复核基础 generation gate 中 request handler contract 与 handler registry attribute 以外是否还有可安全构造的缺失分支

### 阶段：基础 generated registry request handler gate 回归（CQRS-REWRITE-RP-082）

- 延续 `RP-081` 的基础 generation gate 参数化测试，补齐 `IRequestHandler<TRequest,TResponse>` 缺失分支
- 该变体同样通过类型重命名构造 runtime metadata miss，保持输入源码可编译
- 至此基础 generation gate 中可安全构造的缺失分支已覆盖：
  - request handler interface
  - notification handler interface
  - stream handler interface
  - handler registry interface
  - handler registry attribute
  - logging interface
  - DI service collection interface

### 验证（RP-082）

- `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests.Does_Not_Generate_Registry_When_Runtime_Lacks_Required_Generation_Contract"`
  - 结果：通过，`7/7` passed
- `python3 scripts/license-header.py --check`
  - 结果：通过
  - 备注：当前 WSL worktree 需要显式绑定 `GIT_DIR` / `GIT_WORK_TREE` 后运行
- `git diff --check`
  - 结果：通过

### 当前下一步（RP-082）

1. 继续复算 branch diff vs `origin/main`，若仍低于 `25` 个文件可继续下一批
2. 下一批优先复核基础 generation gate 之外的 runtime contract 或 fallback selection 分支；基础 gate 的可安全构造缺失分支已覆盖

### 阶段：PR #323 review 锚点收敛（CQRS-REWRITE-RP-082）

- 使用 `$gframework-pr-review` 重新拉取当前分支 PR review payload，确认当前分支对应 `PR #323`，状态为 `OPEN`
- 本轮 latest-head open AI thread 仅指出 active tracking 中仍保留 `PR #307` 作为当前 PR 锚点；本地复核后确认该反馈仍成立
- 已将 active tracking 的当前 PR 锚点、活跃事实、最近 PR review 备注和下一推荐步骤统一到 `PR #323`
- `PR #307` 仅保留为历史 PR 说明和较早 trace 段落，不再作为 active 恢复入口

### 验证（PR #323 review）

- `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --format json --json-output <temporary-json-output>`
  - 结果：通过
  - 备注：确认 `PR #323` 只有 1 个 CodeRabbit open thread，指向 active tracking 的 PR 锚点漂移
- 远端 `CTRF` 最新汇总为 `2274/2274` passed
- `MegaLinter` 当前仅报告 `dotnet-format` 的 `Restore operation failed` 环境噪音，未提供本地仍成立的文件级格式诊断
- `git diff --check`
  - 结果：通过
- `python3 scripts/license-header.py --check`
  - 结果：通过
  - 备注：当前 WSL worktree 需要显式绑定 `GIT_DIR` / `GIT_WORK_TREE` 后运行
- `dotnet build GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`

### 当前下一步（PR #323 review）

1. 若 review 重新触发后仍有 latest-head open thread，继续以 `PR #323` 为当前唯一 PR 恢复锚点复核
2. 后续若继续推进代码切片，优先复核基础 generation gate 之外的 runtime contract 或 fallback selection 分支
