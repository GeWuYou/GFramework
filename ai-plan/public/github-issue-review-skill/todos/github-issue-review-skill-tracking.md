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

- 恢复点编号：`ISSUE-SKILL-RP-001`
- 当前阶段：`Phase 2`
- 当前焦点：
  - 保持 `$gframework-issue-review` 可供后续 issue 分诊直接复用
  - 通过 `$gframework-boot` 继续 issue `#327` 的澄清优先处理路径
  - 若后续 issue 数量从 `1` 变为 `0` 或 `>1`，要求显式传 `--issue`

### 已知风险

- GitHub timeline API 可能因响应缺失或字段差异导致部分事件无法结构化
  - 缓解措施：把 timeline 解析作为尽力而为能力，缺失时记录到 `parse_warnings`
- 当前仓库 open issue 数量若在验证时变化为 `0` 或 `>1`，默认自动解析路径将无法通过
  - 缓解措施：脚本明确报错并要求 `--issue <number>`，验证时同时保留显式 issue 号路径
- issue 文本中的模块归因和处理建议只能是启发式结果，不能替代本地代码验证
  - 缓解措施：skill 文档明确要求后续仍通过 `$gframework-boot` 与本地源码核实

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

## 验证

- `python3 .agents/skills/gframework-issue-review/scripts/test_fetch_current_issue_review.py`
  - 结果：通过
  - 备注：`3` 个脚本级测试全部通过
- `python3 .agents/skills/gframework-issue-review/scripts/fetch_current_issue_review.py --section summary --section warnings`
  - 结果：通过
  - 备注：真实 GitHub API 抓取成功，自动解析到当前唯一 open issue `#327`
- `python3 .agents/skills/gframework-issue-review/scripts/fetch_current_issue_review.py --format json --json-output /tmp/gframework-open-issue-review.json`
  - 结果：通过
  - 备注：JSON 文件成功写出，`resolution_mode=auto-single-open-issue`，`next_action=clarify-issue-before-code`
- `dotnet build GFramework.sln -c Release`
  - 结果：通过
  - 备注：`0 Warning(s)`，`0 Error(s)`

## 下一步

1. 使用 `$gframework-issue-review` 重新抓取或显式抓取目标 issue，并把 triage 结果带入 `$gframework-boot`
2. 针对 issue `#327` 先执行“澄清优先”路径，再决定是否创建新的代码改动 topic
3. 若后续需要更细的 issue 事件语义，再补强 timeline 解析与脚本级回归测试
