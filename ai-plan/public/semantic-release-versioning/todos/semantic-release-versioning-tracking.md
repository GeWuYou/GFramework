# Semantic Release 版本迁移跟踪

## 目标

将版本管理从固定 `patch + 1` 的自动打 tag 迁移到 `semantic-release`，同时保留现有 `.github/workflows/publish.yml`
的 tag 触发打包、NuGet 发布、GitHub Packages 发布和 GitHub Release 流程。

- 用 `cycjimmy/semantic-release-action` 替换 `auto-tag.yml` 的版本判断和打 tag 逻辑
- 保留 `publish.yml` 的现有发布实现，不重写 NuGet 流程
- 避免 `semantic-release` 与 `publish.yml` 重复创建 GitHub Release
- 将版本规则固定为 `feat -> minor`、`fix/perf/refactor/deps/security -> patch`、`BREAKING CHANGE` 或 `! -> major`
- 为手动 `workflow_dispatch` 保留 dry-run 验证入口，先验证最近提交会算出什么版本

## 当前恢复点

- 恢复点编号：SEMREL-RP-007
- 当前阶段：修复 git-cliff 发布说明 PR 链接缺失
- 当前焦点：
  - `.github/workflows/auto-tag.yml` 的 preview / release job 增加 `pull-requests: read`
  - `.github/workflows/auto-tag.yml` 的 `git-cliff-action` 改用 `${{ github.token }}` 读取 PR 元数据，`PAT_TOKEN`
    只保留给 `semantic-release` 的 dry-run push 探测与真实打 tag
  - `.github/workflows/publish.yml` 的 GitHub Release job 增加 `pull-requests: read`
  - 保持 `.github/cliff.toml` 的 `by @user in #PR` 模板不变，只补足 GitHub PR 元数据读取权限
  - `fix/release-notes-pr-links` 分支映射到当前 active topic

### 已知风险

- `GITHUB_TOKEN` 推送 tag 不会再触发另一个 workflow，真实发布仍需要 `PAT_TOKEN`
- `semantic-release` preview 虽然不会真实推送 tag，但仍会执行远端 `git push --dry-run` 权限探测；如果 PAT 仅具备
  read 权限、没有 `contents:write`，仍然会先于版本分析阶段失败
- `semantic-release` 的版本判断完全依赖 Conventional Commits；不规范提交会直接影响版本计算
- patch 级提交类型的发布语义需要同时维护在 `.releaserc.json`、`AGENTS.md`、公开贡献文档和
  `release-notes-generator` 的 notes 类型映射中，避免版本升级原因与 workflow summary 漂移
- `cycjimmy/semantic-release-action@v6` 需要在 preview / release 两端都安装 `conventional-changelog-conventionalcommits`
  以保证 `conventionalcommits` preset 在 GitHub Actions 中可解析
- `git-cliff-action` 的 `OUTPUT` 文件需要在 `softprops/action-gh-release` 执行时保留在当前工作目录，后续如调整
  working-directory 或 artifact 路径，需要同步复查 `body_path`
- `git-cliff-action` 依赖 GitHub API 补充 `commit.remote.pr_number`；生成 release notes 的 workflow job 必须具备
  `pull-requests: read`，否则模板只能稳定输出作者，不能稳定输出 `in #PR`
- `auto-tag.yml` 中 job 级 `permissions` 只约束 `${{ github.token }}`，不约束 `${{ secrets.PAT_TOKEN }}`；生成
  release notes 时必须使用 `${{ github.token }}` 才能让 `pull-requests: read` 声明真正生效

## 已完成

- 历史迁移结论与 `SEMREL-RP-001` 到 `SEMREL-RP-003` 的稳定完成项已归档到
  `ai-plan/public/semantic-release-versioning/archive/todos/semantic-release-versioning-2026-04-26.md`
- 已将 preview / release 两段重复的 PAT 校验提取到 `.github/actions/validate-pat/action.yml`
- 已在 PAT 校验中补充 `permissions.push` 断言，避免 read-only token 通过 API 探活却在
  `semantic-release` 的 `git push --dry-run` 阶段才失败
- 已为 PAT 校验的 `mktemp` 文件补充 `trap` 清理，避免异常退出时遗留临时文件路径干扰日志
- `SEMREL-RP-004` 的 release notes 模板修复、验证和合并后分支收尾已归档到
  `ai-plan/public/semantic-release-versioning/archive/todos/semantic-release-versioning-rp004-2026-05-02.md`
- `SEMREL-RP-005` 已扩展 `deps` / `security` 的 patch 发布规则，并同步提交规范文档
- `SEMREL-RP-006` 已根据 PR review 复核结果补齐 release notes 类型映射，避免 patch 发布原因只触发版本而不进入 notes
- `SEMREL-RP-007` 已为所有 `git-cliff-action` release notes 生成 job 补齐 PR 读取权限，并让 `auto-tag.yml`
  的 `git-cliff-action` 改用 `${{ github.token }}`，避免未来 GitHub Release 正文缺失 PR 链接

## 验证

- `SEMREL-RP-004` 的本地验证结果已归档。
- `SEMREL-RP-005` 已完成本地验证：
  - `jq . .releaserc.json` 通过
  - `semantic-release --dry-run --no-ci` 已成功加载 `commit-analyzer` 和 `release-notes-generator`，随后因远端 tag
    fetch 会 clobber 本地既有 tags 而终止，未暴露配置解析错误
  - `dotnet build GFramework.sln -c Release` 通过，`0 warning / 0 error`
- `SEMREL-RP-006` 已完成本地验证：
  - `jq . .releaserc.json` 通过
  - `semantic-release --dry-run --no-ci` 已成功加载 `commit-analyzer` 和 `release-notes-generator`，随后因远端 tag
    fetch 会 clobber 本地既有 tags 而终止，未暴露 `presetConfig.types` 配置解析错误
  - `dotnet build GFramework.sln -c Release` 通过，`0 warning / 0 error`
- `SEMREL-RP-007` 已完成本地验证：
  - workflow 权限静态检查通过，所有 `git-cliff-action` 所在 job 均使用具备 `pull-requests: read` 的
    `${{ github.token }}`
  - `.github/cliff.toml` 通过 Python `tomllib` 解析
  - `python3 scripts/license-header.py --check` 通过
  - `dotnet build GFramework.sln -c Release` 通过，`0 warning / 0 error`
- 更早阶段的 dry-run / tag /抽象项目验证已归档到
  `ai-plan/public/semantic-release-versioning/archive/todos/semantic-release-versioning-2026-04-26.md`

## 下一步

1. 推送 `SEMREL-RP-007` 的 PR review 修复，并重新抓取 PR review 确认重复标题线程和 PAT token 说明已收敛
2. 如后续需要回填当前 GitHub Release 正文，使用带 PR read 权限的 GitHub CLI 或 API token 重新生成并更新 notes
