# Semantic Release 版本迁移追踪

## 2026-05-01

### 发布说明 PR 归属展示（SEMREL-RP-004）

- 本轮取舍：
  - 不新增单独 PR 索引章节，避免和分类变更列表重复展示同一批 PR
  - 保留 `print_commit` 的 `by @user in #PR` 输出，让每条变更直接具备 PR 追溯入口
  - 在 grouped 分类列表外层补回 `## What's Changed`，让该区域明确承担完整变更清单语义
- 已更新：
  - `.github/cliff.toml`：分类变更列表现在位于 `## What's Changed` 下，未恢复旧的未分组 commit 循环
  - `ai-plan/public/README.md`：新增 `feat/release-summary-notes` 到 `semantic-release-versioning` 的 active topic 映射
- 验证：
  - `.github/cliff.toml` 通过 Python `tomllib` 解析
  - 本地未安装 `git-cliff`，无法直接预览 action 渲染输出
  - `dotnet build GFramework.sln -c Release` 通过，`0 warning / 0 error`
- 下一步是提交并推送本轮 release notes 模板调整。

### 当前恢复点（SEMREL-RP-004）

- 通过 `$gframework-pr-review` 抓取当前分支 PR #312：
  - CodeRabbit 对 `.github/cliff.toml` 提出 1 个未解决线程，指出 release notes 会重复输出 commit
  - Greptile 对 `.github/cliff.toml` 提出同一问题的未解决线程
  - Greptile 对 `.github/workflows/publish.yml` 提出 1 个未解决线程，指出多行 release notes expression 作为
    `body` 传入 GitHub Release action 风险较高
  - CTRF 测试报告显示 `2247 passed / 0 failed`
  - 未找到 MegaLinter 明细块
- 本地复核结论：
  - `.github/cliff.toml` 先遍历 `commits` 输出平铺列表，再对同一批 `commits` 按 `group` 输出，重复问题成立
  - `.github/workflows/publish.yml` 已让 `git-cliff-action` 写出 `RELEASE_NOTES.md`，因此 `action-gh-release` 可直接使用
    `body_path`
- 已应用修复：
  - 删除 `.github/cliff.toml` 中 `## What's Changed` 下的未分组循环，只保留 grouped 输出
  - 将 `.github/workflows/publish.yml` 的 `body` 改为 `body_path: RELEASE_NOTES.md`
- 已完成语法检查：
  - `.github/cliff.toml` 通过 Python `tomllib` 解析
  - `.github/workflows/publish.yml` 通过 PyYAML 解析
  - `yq` 确认 release step 使用 `body_path`
- `dotnet build GFramework.sln -c Release` 通过，`0 warning / 0 error`。
- 下一步是提交并推送本轮 PR review 修复，然后重新抓取 PR review 确认相关线程状态。

## 2026-04-26

### 当前恢复点（SEMREL-RP-004）

- 当前链路：
  - `workflow_dispatch` 手动启动
  - `preview` 对 dispatch SHA 执行 dry-run
  - `release-approval` environment 审批
  - `release` 在同一次 run、同一 SHA 上执行真实打 tag
- 当前规则：
  - `conventionalcommits` preset 负责解析 `feat!:` / `feat(scope)!:` 与 `BREAKING CHANGE`
  - `feat -> minor`
  - `fix/perf/refactor -> patch`
  - `docs/test/chore/build/ci/style -> no release`
  - `breaking -> major`
- 当前 workflow 加固：
  - `release` 额外要求 `needs.preview.result == 'success'`
  - `PAT_TOKEN` 通过复用的 composite action 统一校验
  - GitHub API 校验额外断言 `.permissions.push == true`，避免 read-only PAT 混过 preview
  - preview / release summary 会展示 snapshot 语义与生成的 release notes
  - `preview` 改为先校验并使用 `PAT_TOKEN`，避免 `github-actions[bot]` 在 dry-run 的远端 push 权限探测中触发 403

### 本轮关键决策

- 保留 `@semantic-release/release-notes-generator`，但不再让它白跑：
  - 继续生成 notes
  - 将 notes 写入 GitHub Actions summary
- preview 与 release 共用 `PAT_TOKEN`：
  - `semantic-release` dry-run 仍会执行 `git push --dry-run`
  - preview 如果继续使用 `${{ github.token }}`，会先被 `github-actions[bot]` 的仓库写权限拦住，日志不再具有可读性
- API 探活必须覆盖 push 权限：
  - 单纯 `GET /repos/{owner}/{repo}` 的 `200` 只能证明 read access
  - 本轮直接读取响应体里的 `permissions.push`，让 preview 在更接近真实失败原因的位置终止
- 不保留已废弃的 `release_mode=preview|release` 中间方案：
  - active trace 只保留当前有效链路
  - 历史演进以 tracking 归档文件为准，active tracking 仅保留当前恢复入口

### 验证结论

1. `npx --yes -p semantic-release -p conventional-changelog-conventionalcommits@9.1.0 semantic-release --dry-run --no-ci`
   - 已确认新 preset 包可加载，`commit-analyzer` 与 `release-notes-generator` 正常初始化
   - 本次 dry-run 未继续出版本，因为干净克隆的 `main` 已落后远端
2. `dotnet build GFramework.sln -c Release`
   - 通过，`639 warning / 0 error`
   - warning 为仓库既有基线，本轮 workflow / ai-plan 调整未新增关联 warning

### 下一步

1. 重跑 `auto-tag.yml` 的 preview，确认 read-only PAT 会在校验步骤提前失败、可写 PAT 不再落到 `EGITNOPERMISSION`
2. 复查当前 PR 的 open review threads 是否已与本地修复对齐
3. 创建提交并推送当前分支
