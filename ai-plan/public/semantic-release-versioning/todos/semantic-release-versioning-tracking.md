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
  - 收敛 release notes 的 PR 归属展示方式
  - 确保 `.github/cliff.toml` 不新增独立 PR 索引导致重复输出同一批 commits
  - 让分类变更列表本身承担 `What's Changed` 语义，并继续在每条 entry 末尾展示作者与 PR 链接

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
- 已将 `.github/cliff.toml` 的分类变更列表重新置于 `## What's Changed` 下，但没有恢复未分组平铺列表；
  每条变更继续通过 `print_commit` 输出 `by @user in #PR` 链接，满足 PR 追溯需求同时避免重复章节
- 已将 `.github/workflows/publish.yml` 的 GitHub Release 正文从多行 expression 改为 `body_path: RELEASE_NOTES.md`，
  复用 `git-cliff-action` 写出的 release notes 文件
- 已在 `ai-plan/public/README.md` 中将 `feat/release-summary-notes` 映射到 `semantic-release-versioning`，便于后续
  `boot` 直接找到本次发布说明模板上下文

## 验证

- `python3 -c 'import tomllib; tomllib.load(open(".github/cliff.toml", "rb")); print("cliff.toml OK")'`
  - 结果：通过
  - 备注：确认 `.github/cliff.toml` 仍为合法 TOML
- `command -v git-cliff`
  - 结果：未找到本地 `git-cliff`
  - 备注：本地无法直接预览 git-cliff 输出，发布 workflow 仍通过 `orhun/git-cliff-action@v4` 提供运行时二进制
- `python3 -c 'import yaml; yaml.safe_load(open(".github/workflows/publish.yml", encoding="utf-8")); print("publish.yml OK")'`
  - 结果：通过
  - 备注：确认 `.github/workflows/publish.yml` 仍可解析为 YAML
- `yq '.jobs."create-release".steps[] | select(.name == "Create GitHub Release and Upload Assets") | .with' .github/workflows/publish.yml`
  - 结果：通过
  - 备注：确认 release step 现在使用 `body_path: RELEASE_NOTES.md`
- `dotnet build GFramework.sln -c Release`
  - 结果：通过
  - 备注：Release 构建通过，`0 warning / 0 error`；本轮只改动 GitHub Actions / git-cliff 配置与恢复文档
- 更早阶段的 dry-run / tag /抽象项目验证已归档到
  `ai-plan/public/semantic-release-versioning/archive/todos/semantic-release-versioning-2026-04-26.md`

## 下一步

1. 提交并推送本轮 release notes 模板调整
2. 如 CI 仍报告 release notes 发布问题，再优先复查 `git-cliff-action` 输出文件路径与模板渲染结果
