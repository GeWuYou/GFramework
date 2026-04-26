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
  - 将 preview / release 的 PAT 校验收敛为同一个复用入口，避免后续修复在两段脚本间漂移
  - 在 PAT 校验阶段提前识别“仅有 read、没有 push”的 token，真正覆盖 `git push --dry-run` 的权限前提
  - 将 active tracking 中已稳定的历史完成项归档，恢复默认入口的可读性

### 已知风险

- `GITHUB_TOKEN` 推送 tag 不会再触发另一个 workflow，真实发布仍需要 `PAT_TOKEN`
- `semantic-release` preview 虽然不会真实推送 tag，但仍会执行远端 `git push --dry-run` 权限探测；如果 PAT 仅具备
  read 权限、没有 `contents:write`，仍然会先于版本分析阶段失败
- `semantic-release` 的版本判断完全依赖 Conventional Commits；不规范提交会直接影响版本计算
- `cycjimmy/semantic-release-action@v6` 需要在 preview / release 两端都安装 `conventional-changelog-conventionalcommits`
  以保证 `conventionalcommits` preset 在 GitHub Actions 中可解析
- 当前仓库本地 `dotnet clean/build` 会带出既有 analyzer warnings；本轮仅修正发版配置与文档，不额外处理这些历史 warning

## 已完成

- 历史迁移结论与 `SEMREL-RP-001` 到 `SEMREL-RP-003` 的稳定完成项已归档到
  `ai-plan/public/semantic-release-versioning/archive/todos/semantic-release-versioning-2026-04-26.md`
- 已将 preview / release 两段重复的 PAT 校验提取到 `.github/actions/validate-pat/action.yml`
- 已在 PAT 校验中补充 `permissions.push` 断言，避免 read-only token 通过 API 探活却在
  `semantic-release` 的 `git push --dry-run` 阶段才失败
- 已为 PAT 校验的 `mktemp` 文件补充 `trap` 清理，避免异常退出时遗留临时文件路径干扰日志
- 已同步更新 active trace 到 `SEMREL-RP-004`，记录本轮 PR review 收敛结果

## 验证

- `dotnet build GFramework.sln -c Release`
  - 结果：通过
  - 备注：Release 构建通过，`639 warning / 0 error`；warning 为仓库既有基线，preview 鉴权修复后与本轮 PAT 校验收敛后复验结果一致
- 更早阶段的 dry-run / tag /抽象项目验证已归档到
  `ai-plan/public/semantic-release-versioning/archive/todos/semantic-release-versioning-2026-04-26.md`

## 下一步

1. 手动重跑 `Semantic Release Version and Tag` 的 preview job，确认 read-only PAT 会在校验步骤提前失败、可写 PAT 不再进入 `git push --dry-run ... 403`
2. 推送本轮修复后重新抓取 PR review，确认 CodeRabbit / Greptile 的 open threads 已转为过时或可关闭
3. 如 CI 仍报告权限边界问题，再决定是否将 PAT 校验升级为更贴近真实链路的远端 git 探测
