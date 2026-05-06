# GitHub Issue Review Skill 跟踪

## 目标

为仓库新增一个与 `$gframework-pr-review` 并列的 `$gframework-issue-review` skill，让 AI 能够从 GitHub issue
快速提取正文、讨论和关键事件，形成结构化分诊结果，并把后续代码处理明确衔接到 `$gframework-boot`。

- 保持与现有 PR review skill 相同的目录与 CLI 体验
- 支持“当前恰好一个 open issue 时自动选中，否则要求显式传号”的解析策略
- 输出适合 AI 后续验证的结构化 JSON 与高信号文本摘要
- 给出最小回归测试，覆盖自动选中与解析边界
- 用真实仓库 issue 做一次抓取验证，确保默认路径可用

## 当前恢复点

- 恢复点编号：`ISSUE-SKILL-RP-002`
- 当前阶段：`Phase 3`
- 当前焦点：
  - 收敛 PR #328 上仍然有效的 AI review 评论，避免新 skill 在仓库中留下已知漂移
  - 保持 `$gframework-issue-review` 的 GitHub API 抓取在代理、认证与 JSON CLI 契约上更稳健
  - 确保非 bug issue 的 triage 结果不会被错误导向 `clarify-issue-before-code`

### 已知风险

- GitHub timeline API 可能因响应缺失或字段差异导致部分事件无法结构化
  - 缓解措施：把 timeline 解析作为尽力而为能力，缺失时记录到 `parse_warnings`
- 当前仓库 open issue 数量若在验证时变化为 `0` 或 `>1`，默认自动解析路径将无法通过
  - 缓解措施：脚本明确报错并要求 `--issue <number>`，验证时同时保留显式 issue 号路径
- issue 文本中的模块归因和处理建议只能是启发式结果，不能替代本地代码验证
  - 缓解措施：skill 文档明确要求后续仍通过 `$gframework-boot` 与本地源码核实
- GitHub API 仍可能在无 token 环境下命中匿名 rate limit
  - 缓解措施：脚本现已支持从 `GFRAMEWORK_GITHUB_TOKEN`、`GITHUB_TOKEN`、`GH_TOKEN` 读取认证；无 token 时保持匿名降级

## 已完成

- 已建立活跃 topic：
  - `ai-plan/public/github-issue-review-skill/todos/`
  - `ai-plan/public/github-issue-review-skill/traces/`
- 已将分支 `feat/github-issue-review-skill` 映射到该 topic，供后续 `boot` 优先恢复
- 已新增 `.agents/skills/gframework-issue-review/`：
  - `SKILL.md`
  - `agents/openai.yaml`
  - `scripts/fetch_current_issue_review.py`
  - `scripts/test_fetch_current_issue_review.py`
- 已实现与 `gframework-pr-review` 同构的 GitHub API 抓取骨架：
  - 支持 issue 元数据、评论、timeline、引用与 triage hints 输出
  - 支持 `--issue`、`--format`、`--json-output`、`--section`、`--max-description-length`
  - 支持“仅当当前仓库恰好一个 open issue 时自动解析，否则要求显式传号”
- 已修正新脚本在当前 WSL 会话下误回退到 `git.exe` 的兼容问题：
  - 在主仓库根目录且存在 Linux `git` 时，也优先绑定 `--git-dir` / `--work-tree`
- 已根据 PR #328 review 收敛仍然有效的问题：
  - 为 `fetch_current_issue_review.py` 与回归测试补齐 shebang 后 license header
  - 去掉开发机特定的 Windows Git 绝对路径回退，改为环境变量覆盖 + `git.exe` / `git`
  - GitHub 请求先走环境代理，并在代理请求失败且检测到代理环境变量时再无代理重试
  - 支持通过标准 token 环境变量附带 `Authorization` 头，避免高频运行时过早命中匿名限流
  - 将 `needs_clarification` 改为按 issue 主类型分支，避免 feature / docs issue 被 bug 规则误判
  - 修正 `--format json --json-output` 时 stdout 仍输出 JSON，文件写入只作为附加副作用
  - 补充 docs / feature 场景回归测试，并将 skill 示例 issue 号改为占位符

## 验证

- `python3 scripts/license-header.py --check`
  - 结果：通过
  - 备注：本轮修改涉及的受支持文件均包含 Apache-2.0 license header
- `python3 .agents/skills/gframework-issue-review/scripts/test_fetch_current_issue_review.py`
  - 结果：通过
  - 备注：`5` 个脚本级测试全部通过，新增 docs / feature 分诊回归覆盖
- `python3 .agents/skills/gframework-issue-review/scripts/fetch_current_issue_review.py --section summary --section warnings`
  - 结果：通过
  - 备注：真实 GitHub API 抓取成功，自动解析到当前唯一 open issue `#327`
- `python3 .agents/skills/gframework-issue-review/scripts/fetch_current_issue_review.py --format json --json-output /tmp/gframework-open-issue-review.json`
  - 结果：通过
  - 备注：stdout 输出 JSON，文件也成功写出；显式抓取 `#327` 时 `next_action=clarify-issue-before-code`
- `dotnet build GFramework.sln -c Release`
  - 结果：通过
  - 备注：`0 Warning(s)`，`0 Error(s)`

## 下一步

1. 将本轮 PR review 修复提交到当前分支，并回到 PR 线程确认相关评论是否可关闭
2. 需要继续处理 issue `#327` 时，重新用 `$gframework-issue-review` 抓取目标 issue，并把结果带入 `$gframework-boot`
3. 若后续需要更细的 issue 事件语义，再补强 timeline 解析与脚本级回归测试
