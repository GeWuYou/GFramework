# Semantic Release 版本迁移追踪

## 2026-04-26

### 当前恢复点（SEMREL-RP-003）

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
  - `PAT_TOKEN` 在真实 release 前通过 GitHub API 做存活性校验
  - preview / release summary 会展示 snapshot 语义与生成的 release notes
  - `preview` 改为先校验并使用 `PAT_TOKEN`，避免 `github-actions[bot]` 在 dry-run 的远端 push 权限探测中触发 403

### 本轮关键决策

- 保留 `@semantic-release/release-notes-generator`，但不再让它白跑：
  - 继续生成 notes
  - 将 notes 写入 GitHub Actions summary
- preview 与 release 共用 `PAT_TOKEN`：
  - `semantic-release` dry-run 仍会执行 `git push --dry-run`
  - preview 如果继续使用 `${{ github.token }}`，会先被 `github-actions[bot]` 的仓库写权限拦住，日志不再具有可读性
- 不保留已废弃的 `release_mode=preview|release` 中间方案：
  - active trace 只保留当前有效链路
  - 历史演进以 tracking 文档的已完成项为准

### 验证结论

1. `npx --yes -p semantic-release -p conventional-changelog-conventionalcommits@9.1.0 semantic-release --dry-run --no-ci`
   - 已确认新 preset 包可加载，`commit-analyzer` 与 `release-notes-generator` 正常初始化
   - 本次 dry-run 未继续出版本，因为干净克隆的 `main` 已落后远端
2. `dotnet build GFramework.sln -c Release`
   - 通过，`639 warning / 0 error`
   - warning 为仓库既有基线，本轮未新增关联 warning

### 下一步

1. 重跑 `auto-tag.yml` 的 preview，确认 `EGITNOPERMISSION` 已消失
2. 复查当前 PR 的 open review threads 是否只剩等待 push 的已修复项
3. 创建提交并推送当前分支
