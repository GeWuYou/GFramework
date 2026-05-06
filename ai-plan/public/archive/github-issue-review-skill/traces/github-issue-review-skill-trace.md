# GitHub Issue Review Skill Trace

## 2026-05-06

### 阶段：能力落地准备（ISSUE-SKILL-RP-001）

- 读取 `AGENTS.md`、`.ai/environment/tools.ai.yaml`、`ai-plan/public/README.md` 与现有
  `.agents/skills/gframework-pr-review/` 实现，确认新 skill 最稳妥的方案是复用现有 PR review 的
  GitHub API、WSL worktree Git 解析、文本 section 输出与脚本级测试骨架
- 确认当前任务属于 `new` + `complex`：
  - `new`：当前没有与 issue review skill 对应的公开恢复主题
  - `complex`：同时涉及 skill 设计、GitHub API 脚本、CLI 契约、测试和 `ai-plan` 恢复入口
- 根据实现前确认的产品决策固定默认行为：
  - 未显式传 issue 号时，只在“仓库当前恰好一个 open issue”时自动选中
  - skill 默认只做“抓取 + 分诊 + boot 衔接”，不在脚本层直接改代码
- 已创建新 topic 目录并将当前分支 `feat/github-issue-review-skill` 映射到该 topic

### 当前执行目标

1. 新增 `gframework-issue-review` skill 文档与默认 prompt
2. 新增 `fetch_current_issue_review.py` 及其最小回归测试
3. 用真实 open issue 抓取验证默认流程，并记录最小验证命令

### 下一步

1. 直接用 `$gframework-issue-review` + `$gframework-boot` 开始 issue `#327` 的后续处理
2. 若后续仓库同时出现多个 open issue，统一改用显式 `--issue <number>` 入口

### 阶段：实现与验证完成（ISSUE-SKILL-RP-001）

- 已落盘新 skill 文件：
  - `.agents/skills/gframework-issue-review/SKILL.md`
  - `.agents/skills/gframework-issue-review/agents/openai.yaml`
  - `.agents/skills/gframework-issue-review/scripts/fetch_current_issue_review.py`
  - `.agents/skills/gframework-issue-review/scripts/test_fetch_current_issue_review.py`
- 真实抓取验证时首次发现：当前 WSL 会话会解析到 `git.exe`，但无法执行
  - 已在新脚本中修正为：只要仓库根目录存在 Linux `git`，就优先绑定显式 `--git-dir` / `--work-tree`
- 完成验证：
  - `python3 .agents/skills/gframework-issue-review/scripts/test_fetch_current_issue_review.py`
  - `python3 .agents/skills/gframework-issue-review/scripts/fetch_current_issue_review.py --section summary --section warnings`
  - `python3 .agents/skills/gframework-issue-review/scripts/fetch_current_issue_review.py --format json --json-output /tmp/gframework-open-issue-review.json`
  - `dotnet build GFramework.sln -c Release`
- 真实 issue 验证结论：
  - 当前 open issue 自动解析为 `#327`
  - `resolution_mode=auto-single-open-issue`
  - `comment_count=0`
  - `next_action=clarify-issue-before-code`
  - `affected_active_topics=cqrs-rewrite`

### 阶段：PR review 跟进修复（ISSUE-SKILL-RP-002）

- 使用 `$gframework-pr-review` 抓取当前分支 PR #328 后，确认以下评论在本地代码中仍然有效：
  - `fetch_current_issue_review.py` 和回归测试缺少 shebang 后 license header
  - issue-review 脚本仍保留开发机特定的 Windows Git 绝对路径回退
  - `open_url()` 无条件禁用代理，且未支持 GitHub token 认证
  - `build_information_flags()` 仍把 bug 场景的澄清门槛套用到 feature / docs issue
  - `--format json --json-output` 组合时 stdout 只输出路径而不是 JSON
  - skill 文档命令和示例中把 issue 号 `312` 写死
- 已在活跃 topic 下同步恢复点：
  - 跟踪文件更新为 `ISSUE-SKILL-RP-002`
  - 记录本轮修复范围、验证待办与后续恢复入口
- 已落盘修复：
  - 为 issue-review 脚本和测试补齐 license header
  - 将 GitHub 请求改为“先按环境代理请求，代理失败再无代理重试”
  - 支持 `GFRAMEWORK_GITHUB_TOKEN` / `GITHUB_TOKEN` / `GH_TOKEN`
  - 将 triage 澄清逻辑改为按主 issue 类型分支
  - 为 docs / feature issue 增加 next-action 回归测试
  - 将 skill 示例 issue 号改为占位符
  - 让 `--format json --json-output` 同时保留 stdout JSON 与落盘副作用
- 已完成验证：
  - `python3 scripts/license-header.py --check`
  - `python3 .agents/skills/gframework-issue-review/scripts/test_fetch_current_issue_review.py`
  - `python3 .agents/skills/gframework-issue-review/scripts/fetch_current_issue_review.py --issue 327 --format json --json-output /tmp/gframework-open-issue-review.json`
  - `dotnet build GFramework.sln -c Release`
- 本轮验证结论：
  - license header 检查通过
  - 脚本级测试 `5/5` 通过
  - `--format json --json-output` 现在会同时输出 stdout JSON 并写出 JSON 文件
  - 仓库 Release build 通过，`0 Warning(s)` / `0 Error(s)`

### 下一步

1. 按仓库规范提交本轮 PR review 修复
2. 需要继续跟进 issue `#327` 时，再切回 `$gframework-issue-review` + `$gframework-boot` 路径
