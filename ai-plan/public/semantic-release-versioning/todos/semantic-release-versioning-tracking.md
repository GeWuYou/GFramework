# Semantic Release 版本迁移跟踪

## 目标

将版本管理从固定 `patch + 1` 的自动打 tag 迁移到 `semantic-release`，同时保留现有 `.github/workflows/publish.yml`
的 tag 触发打包、NuGet 发布、GitHub Packages 发布和 GitHub Release 流程。

- 用 `cycjimmy/semantic-release-action` 替换 `auto-tag.yml` 的版本判断和打 tag 逻辑
- 保留 `publish.yml` 的现有发布实现，不重写 NuGet 流程
- 避免 `semantic-release` 与 `publish.yml` 重复创建 GitHub Release
- 将版本规则固定为 `feat -> minor`、`fix/perf/refactor -> patch`、`BREAKING CHANGE` 或 `! -> major`
- 为手动 `workflow_dispatch` 保留 dry-run 验证入口，先验证最近提交会算出什么版本

## 当前恢复点

- 恢复点编号：`SEMREL-RP-004`
- 当前阶段：`Phase 2`
- 当前焦点：
  - 收敛 PR #312 最新 AI review 的 release notes 输出问题
  - 确保 `.github/cliff.toml` 不再重复输出同一批 commits
  - 确保 `publish.yml` 创建 GitHub Release 时通过文件传递多行 release notes

### 已知风险

- `GITHUB_TOKEN` 推送 tag 不会再触发另一个 workflow，真实发布仍需要 `PAT_TOKEN`
- `semantic-release` preview 虽然不会真实推送 tag，但仍会执行远端 `git push --dry-run` 权限探测；如果 PAT 仅具备
  read 权限、没有 `contents:write`，仍然会先于版本分析阶段失败
- `semantic-release` 的版本判断完全依赖 Conventional Commits；不规范提交会直接影响版本计算
- `cycjimmy/semantic-release-action@v6` 需要在 preview / release 两端都安装 `conventional-changelog-conventionalcommits`
  以保证 `conventionalcommits` preset 在 GitHub Actions 中可解析
- 当前仓库本地 `dotnet clean/build` 会带出既有 analyzer warnings；本轮仅修正发版配置与文档，不额外处理这些历史 warning
- `git-cliff-action` 的 `OUTPUT` 文件需要在 `softprops/action-gh-release` 执行时保留在当前工作目录，后续如调整
  working-directory 或 artifact 路径，需要同步复查 `body_path`

## 已完成

- 历史迁移结论与 `SEMREL-RP-001` 到 `SEMREL-RP-003` 的稳定完成项已归档到
  `ai-plan/public/semantic-release-versioning/archive/todos/semantic-release-versioning-2026-04-26.md`
- 已将 preview / release 两段重复的 PAT 校验提取到 `.github/actions/validate-pat/action.yml`
- 已在 PAT 校验中补充 `permissions.push` 断言，避免 read-only token 通过 API 探活却在
  `semantic-release` 的 `git push --dry-run` 阶段才失败
- 已为 PAT 校验的 `mktemp` 文件补充 `trap` 清理，避免异常退出时遗留临时文件路径干扰日志
- 已同步更新 active trace 到 `SEMREL-RP-004`，记录本轮 PR review 收敛结果
- 已用 `$gframework-pr-review` 抓取 PR #312 最新 review payload，确认未失败测试、未发现 MegaLinter 明细，仍有
  CodeRabbit / Greptile 针对 release notes 的未解决线程
- 已移除 `.github/cliff.toml` 中 `## What's Changed` 下的未分组 commit 循环，仅保留按 Conventional Commit group
  分类后的输出，避免每个 commit 在生成的 changelog 中出现两次
- 已将 `.github/workflows/publish.yml` 的 GitHub Release 正文从多行 expression 改为 `body_path: RELEASE_NOTES.md`，
  复用 `git-cliff-action` 写出的 release notes 文件

## 验证

- `python3 -c 'import tomllib; tomllib.load(open(".github/cliff.toml", "rb")); print("cliff.toml OK")'`
  - 结果：通过
  - 备注：确认 `.github/cliff.toml` 仍为合法 TOML
- `python3 -c 'import yaml; yaml.safe_load(open(".github/workflows/publish.yml", encoding="utf-8")); print("publish.yml OK")'`
  - 结果：通过
  - 备注：确认 `.github/workflows/publish.yml` 仍可解析为 YAML
- `yq '.jobs."create-release".steps[] | select(.name == "Create GitHub Release and Upload Assets") | .with' .github/workflows/publish.yml`
  - 结果：通过
  - 备注：确认 release step 现在使用 `body_path: RELEASE_NOTES.md`
- `dotnet build GFramework.sln -c Release`
  - 结果：通过
  - 备注：Release 构建通过，`0 warning / 0 error`；本轮只改动 GitHub Actions / git-cliff 配置
- 更早阶段的 dry-run / tag /抽象项目验证已归档到
  `ai-plan/public/semantic-release-versioning/archive/todos/semantic-release-versioning-2026-04-26.md`

## 下一步

1. 提交并推送本轮 PR review 修复
2. 重新抓取 PR review，确认 CodeRabbit / Greptile 的 release notes open threads 已转为过时或可关闭
3. 如 CI 仍报告 release notes 发布问题，再优先复查 `git-cliff-action` 输出文件路径与 `action-gh-release` 输入契约
