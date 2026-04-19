# AI-Plan 治理追踪

## 2026-04-19

### 阶段：遗留 local-plan 收口（RP-004）

- 建立 `AI-PLAN-GOV-RP-004` 恢复点
- 发现当前 worktree `feat/ai-first-config` 仍保留旧 `local-plan/`，说明主题化 `ai-plan/public/<topic>/` 落地后还有遗留入口未收口
- 已据此完成本轮治理补齐：
  - 新增 `ai-plan/public/ai-first-config-system/` 主题目录，并迁入 tracking / next / trace 文档
  - 在 `ai-plan/public/README.md` 为 `feat/ai-first-config` 绑定 `ai-first-config-system`
  - 将迁移后的公共文档中的 `local-plan` 旧路径引用更新为新的 `ai-plan/public/...` 路径
  - 将绝对路径、宿主 NuGet fallback 目录和 `GIT_DIR` / `GIT_WORK_TREE` 命令细节改写为可提交的公共表述

### 验证（RP-004）

- `find ai-plan/public/ai-first-config-system -maxdepth 3 -type f | sort`
  - 结果：通过
- `rg -n "local-plan|D:\\\\|/mnt/|GIT_DIR=" ai-plan/public/ai-first-config-system`
  - 结果：通过
- `test ! -e local-plan`
  - 结果：通过
- `dotnet build GFramework.Core.Abstractions/GFramework.Core.Abstractions.csproj -c Release -p:RestoreFallbackFolders=`
  - 结果：通过

### 下一步（RP-004）

1. 后续若发现其他 worktree 仍有旧恢复文档，沿用“主题目录迁移 + 索引登记 + 公共内容清洗”同一流程处理
2. 若某个主题后续再分阶段沉淀恢复文档，优先收入该主题自己的 `archive/`，避免活动入口再次膨胀

### 阶段：目录语义收口（RP-002）

- 建立 `AI-PLAN-GOV-RP-002` 恢复点
- 用户指出当前 `ai-plan/` 存在三个治理问题：
  - 缺少更细的目录分层，容易随着 worktree 增长持续膨胀
  - 缺少“不得写入敏感数据、真实路径、机器信息”的明确约束
  - 目录语义没有区分共享恢复信息与 worktree 私有状态
- 已据此完成以下收口：
  - 将既有共享 tracking / trace 文件迁移到扁平的 `ai-plan/public/` 共享目录
  - 新增 `ai-plan/private/` 作为工作树私有恢复空间，并通过 `.gitignore` 保持未跟踪
  - 新增 `ai-plan/README.md` 作为目录语义与内容规范的单点说明
  - 在 `AGENTS.md` 中补齐 public/private 职责边界，以及敏感信息与绝对路径禁写规则
  - 在 `gframework-boot` 中同步新的读取顺序：优先 public，按需读取当前 worktree 私有目录

### 验证（RP-002）

- `find ai-plan -maxdepth 3 -type f | sort`
  - 结果：通过
- `rg -n "ai-plan/public/|ai-plan/private/" AGENTS.md .codex/skills/gframework-boot/SKILL.md .codex/skills/gframework-boot/references/startup-artifacts.md ai-plan/README.md .gitignore`
  - 结果：通过
- `dotnet build GFramework.Core.Abstractions/GFramework.Core.Abstractions.csproj -c Release`
  - 结果：通过

### 下一步（RP-002）

1. 后续若出现新的 worktree 私有恢复需求，直接在 `ai-plan/private/<branch-or-worktree>/` 下创建，不再向共享目录追加本地临时状态
2. 若将来需要进一步限制格式，可再为 `public/**` 与 `private/` 各自补一个模板文件，但本轮先把目录语义和安全边界固定下来

### 阶段：主题分组与启动索引（RP-003）

- 建立 `AI-PLAN-GOV-RP-003` 恢复点
- 用户进一步指出：即使 public/private 已分层，只要多 worktree 并行，扁平的活动主题集合仍会让 `boot` 随着
  `ai-plan/public` 增长而退化成大范围扫描
- 已据此完成第二轮治理：
  - 将活动共享文档迁移到 `ai-plan/public/<topic>/todos/` 与 `ai-plan/public/<topic>/traces/`
  - 新增 `ai-plan/public/README.md` 作为公共启动索引，维护 worktree 到多个活跃主题的映射与优先顺序
  - 将已完成的 `cqrs-cache-docs-hardening` 整体移入 `ai-plan/public/archive/cqrs-cache-docs-hardening/`
  - 在 `AGENTS.md`、`ai-plan/README.md`、根 `README.md` 与 `gframework-boot` 中统一“公共索引 + 活动主题 +
    主题内归档 + 主题级归档”的语义
  - 将 `.gitignore` 调整为允许 `ai-plan/public/**/*.md` 以新层级进入版本控制

### 验证（RP-003）

- `find ai-plan/public -maxdepth 4 -type f | sort`
  - 结果：通过
- `rg -n "ai-plan/public/README.md|ai-plan/public/<topic>|ai-plan/public/archive|ai-plan/private/" AGENTS.md .codex/skills/gframework-boot/SKILL.md .codex/skills/gframework-boot/references/startup-artifacts.md ai-plan/README.md README.md .gitignore`
  - 结果：通过
- `dotnet build GFramework.Core.Abstractions/GFramework.Core.Abstractions.csproj -c Release --no-restore`
  - 结果：通过

### 下一步（RP-003）

1. 未来每次新增或关闭主题，都同步更新 `ai-plan/public/README.md`，不要让 `boot` 回到全量扫描模式
2. 若某个活跃主题内部继续积累阶段性完成物，优先收入该主题目录下的 `archive/`
