# AI-Plan 治理跟踪

## 目标

继续收口 `ai-plan/` 的目录语义、启动入口与归档策略，避免多 worktree 并行时的公共恢复文档持续膨胀并拖慢
`boot` 的上下文定位。

- 为 `ai-plan/` 建立明确的目录分层
- 区分“可提交共享状态”与“工作树私有状态”
- 明确禁止写入敏感数据、绝对路径和机器本地信息
- 让 `AGENTS.md`、`ai-plan/README.md` 与 boot skill 使用同一套目录语义
- 让 `boot` 能通过公共索引快速定位当前 worktree 的活跃主题
- 为阶段完成和主题完成两类归档建立稳定规则

## 当前恢复点

- 恢复点编号：`AI-PLAN-GOV-RP-004`
- 当前阶段：`Phase 2`
- 当前焦点：
  - 已将共享恢复文档按主题迁移到 `ai-plan/public/<topic>/todos/` 与 `ai-plan/public/<topic>/traces/`
  - 已为 `boot` 新增 `ai-plan/public/README.md` 公共索引，并绑定当前 worktree 的活跃主题顺序
  - 已将完成度更高的 `cqrs-cache-docs-hardening` 移入 `ai-plan/public/archive/`
  - 已同步更新 `.gitignore`、`AGENTS.md`、`ai-plan/README.md`、根 `README.md` 与 `gframework-boot`
  - 已明确主题内归档与主题级归档的双层规则，避免活动区无限增长
  - 已将当前 `feat/ai-first-config` worktree 下遗留的 `local-plan/` 收口到新的公共主题目录，并补齐索引映射

## 已完成

- `.gitignore` 现允许 `ai-plan/public/**/*.md` 以主题目录与归档目录形式进入版本控制
- `AGENTS.md` 已补充：
  - `public/README.md`、活动主题目录、主题内归档与主题级归档的职责划分
  - `boot` 默认忽略 `ai-plan/public/archive/**`
  - worktree 与活跃主题映射变化时，必须同步更新公共索引
- `.codex/skills/gframework-boot/SKILL.md` 与其 `references/startup-artifacts.md` 已切换到：
  - 优先读取 `ai-plan/public/README.md`
  - 命中映射后优先读取对应主题目录
  - 未命中映射时再扫描活动主题目录，并排除公共归档区
- `ai-plan/README.md` 已补充主题命名、归档触发条件与 `boot` 读取顺序
- 根 `README.md` 已改为要求维护公共索引与对应主题目录
- 现有共享文档已迁移为：
  - `ai-plan/public/ai-plan-governance/**`
  - `ai-plan/public/ai-first-config-system/**`
  - `ai-plan/public/cqrs-rewrite/**`
  - `ai-plan/public/archive/cqrs-cache-docs-hardening/**`
- 已根据 PR #253 的最新未解决 review thread 清理 `ai-plan/public/ai-plan-governance/traces/ai-plan-governance-trace.md`
  中重复的 `### 验证` / `### 下一步` 标题，并补充恢复点后缀以消除 MD024 锚点冲突
- 已将当前 `feat/ai-first-config` worktree 的 `local-plan/` 文档迁移到 `ai-plan/public/ai-first-config-system/`
- 已在 `ai-plan/public/README.md` 为 `feat/ai-first-config` 登记 `ai-first-config-system` 活跃主题映射
- 已清洗迁移文档中的 `local-plan` 旧路径、绝对文件系统路径与仅适用于本机的 Git worktree 命令细节

## 验证

- `find ai-plan/public -maxdepth 4 -type f | sort`
  - 结果：通过
  - 备注：活动主题、公共索引与归档主题已按新目录语义落位
- `rg -n "ai-plan/public/README.md|ai-plan/public/<topic>|ai-plan/public/archive|ai-plan/private/" AGENTS.md .codex/skills/gframework-boot/SKILL.md .codex/skills/gframework-boot/references/startup-artifacts.md ai-plan/README.md README.md .gitignore`
  - 结果：通过
  - 备注：新目录语义、索引入口与归档规则已统一到仓库规则与 boot skill
- `dotnet build GFramework.Core.Abstractions/GFramework.Core.Abstractions.csproj -c Release --no-restore`
  - 结果：通过
  - 备注：本轮规则与文档调整未引入构建问题
- `rg -n "^### (验证|下一步)" ai-plan/public/ai-plan-governance/traces/ai-plan-governance-trace.md`
  - 结果：通过
  - 备注：同名标题已按恢复点后缀唯一化，不再产生重复锚点
- `rg -n "local-plan|D:\\\\|/mnt/|GIT_DIR=" ai-plan/public/ai-first-config-system`
  - 结果：通过
  - 备注：迁移后的公共主题文档未保留旧目录入口或机器本地路径
- `find ai-plan/public/ai-first-config-system -maxdepth 3 -type f | sort`
  - 结果：通过
  - 备注：当前 worktree 的 tracking / next / trace 已按主题目录落位
- `test ! -e local-plan`
  - 结果：通过
  - 备注：旧 worktree 根目录入口已删除，不再与新的公共主题目录并存
- `dotnet build GFramework.Core.Abstractions/GFramework.Core.Abstractions.csproj -c Release -p:RestoreFallbackFolders=`
  - 结果：通过
  - 备注：默认 `--no-restore` 构建仍会命中旧的宿主 Windows NuGet fallback 配置，本轮通过显式清空 `RestoreFallbackFolders` 完成最小构建验证

## 下一步

1. 后续再发现 legacy `local-plan/` 或平铺恢复文档时，先迁入对应 `ai-plan/public/<topic>/`，再补 `public/README.md` 映射
2. 阶段完成后优先收入主题内 `archive/`；主题整体完成后，再整目录移入 `ai-plan/public/archive/`
3. 若未来再新增 skill 或仓库规则引用 `ai-plan/`，统一按“公共索引 + 活动主题 + 归档主题 + 私有目录”扩展，不再恢复平铺结构
