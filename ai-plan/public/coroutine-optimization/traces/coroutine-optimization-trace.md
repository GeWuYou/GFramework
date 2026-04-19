# Coroutine Optimization 追踪

## 2026-04-19

### 阶段：legacy local-plan 迁移建档（RP-001）

- 复核当前工作树后确认：`local-plan/` 仅保存 coroutine 主题的早期 todo 基线，共 `5` 份分阶段文档，没有独立 trace
- 按 `ai-plan` 治理规则建立 `ai-plan/public/coroutine-optimization/` 主题目录，并补齐：
  - `todos/`
  - `traces/`
  - `archive/todos/`
  - `archive/traces/`
- 将旧 `local-plan` 中分散的五个阶段计划整合为主题内历史跟踪归档，避免后续恢复仍依赖 worktree-root 私有目录
- 因旧计划没有 durable trace，本次额外补写一份“基于早期 todo 基线整理出的历史 trace”，显式记录信息缺口与推导边界
- 新建精简版 active tracking / trace 入口，并在 `ai-plan/public/README.md` 中建立
  `feat/coroutine-optimization` -> `coroutine-optimization` 的 worktree 映射
- 删除旧 `local-plan` 目录，确保后续 `boot` 只从 `ai-plan/public/coroutine-optimization/` 进入
- 额外完成验证：
  - `find ai-plan/public/coroutine-optimization -maxdepth 3 -type f | sort`
  - `test ! -e local-plan`
  - `dotnet build GFramework.Core.Abstractions/GFramework.Core.Abstractions.csproj -c Release -p:RestoreFallbackFolders=`

### Archive Context

- 历史跟踪归档：
  - `ai-plan/public/coroutine-optimization/archive/todos/coroutine-optimization-history-pre-rp001.md`
- 历史 trace 归档：
  - `ai-plan/public/coroutine-optimization/archive/traces/coroutine-optimization-history-pre-rp001.md`

### 下一步

1. 后续若继续 coroutine 主题，只从 `ai-plan/public/coroutine-optimization/` 进入，不再恢复 `local-plan/`
2. 下一轮只选择一个主切入点推进，避免语义、宿主、测试和文档扩面同时发生
3. 若 active 入口后续积累多轮已完成且已验证阶段，再按同一模式迁入该主题自己的 `archive/`
