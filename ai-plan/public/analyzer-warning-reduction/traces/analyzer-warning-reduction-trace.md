# Analyzer Warning Reduction 追踪

## 2026-04-19

### 阶段：local-plan 迁移收口（RP-001）

- 复核当前工作树后确认：`local-plan/` 仅保存 analyzer warning reduction 主题的 durable recovery state，不应继续作为
  worktree-root 遗留目录存在
- 按 `ai-plan` 治理规则建立 `ai-plan/public/analyzer-warning-reduction/` 主题目录，并补齐：
  - `todos/`
  - `traces/`
  - `archive/todos/`
  - `archive/traces/`
- 将旧 `local-plan` 中的详细 tracking / trace 迁入主题内历史归档，保留 `RP-001` 的完整实现与验证上下文
- 新建精简版 active tracking / trace 入口，并在 `ai-plan/public/README.md` 中建立
  `fix/analyzer-warning-reduction-batch` -> `analyzer-warning-reduction` 的 worktree 映射
- 删除旧 `local-plan` 文件，避免 `boot` 或后续协作者继续从过时目录恢复
- 验证通过：
  - `find ai-plan/public/analyzer-warning-reduction -maxdepth 3 -type f | sort`
  - `dotnet build GFramework.Core.Abstractions/GFramework.Core.Abstractions.csproj -c Release -p:RestoreFallbackFolders=`

### Archive Context

- 历史跟踪归档：
  - `ai-plan/public/analyzer-warning-reduction/archive/todos/analyzer-warning-reduction-history-rp001.md`
- 历史 trace 归档：
  - `ai-plan/public/analyzer-warning-reduction/archive/traces/analyzer-warning-reduction-history-rp001.md`

### 下一步

1. 后续若继续 analyzer warning reduction，只从 `ai-plan/public/analyzer-warning-reduction/` 进入，不再恢复 `local-plan/`
2. 若 active 入口再次积累多轮已完成且已验证阶段，继续按同一模式迁入该主题自己的 `archive/`
