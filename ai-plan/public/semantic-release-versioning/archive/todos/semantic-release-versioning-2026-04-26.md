# Semantic Release 版本迁移归档（2026-04-26）

## 归档范围

- Phase 1 初始迁移结论
- `SEMREL-RP-001` 到 `SEMREL-RP-003` 期间已稳定的 PR review 修复
- active tracking 中已不需要继续占据默认恢复入口的历史验证明细

## 历史完成项

- 已确认当前版本入口为 `.github/workflows/auto-tag.yml`，现状始终执行 `PATCH + 1`
- 已确认当前 `.github/workflows/publish.yml` 由 tag 触发，并负责 `.nupkg` 打包、发布和 GitHub Release
- 已确认最新 tag 为 `v0.0.222`
- 已确认 `v0.0.222..HEAD` 之间存在 `feat(...)` 提交，按目标规则首次 dry-run 预期版本应为 `v0.1.0`
- 已新增 `.releaserc.json`，仅保留 `@semantic-release/commit-analyzer` 与
  `@semantic-release/release-notes-generator`，避免 `semantic-release` 直接创建 GitHub Release
- 已将 `.releaserc.json` 的 `commit-analyzer` / `release-notes-generator` 同步切换到 `conventionalcommits`
  preset，并显式声明：
  - `breaking -> major`
  - `revert -> patch`
  - `feat -> minor`
  - `fix/perf/refactor -> patch`
  - `docs/test/chore/build/ci/style -> no release`
- 已将 `.github/workflows/auto-tag.yml` 重写为：
  - `workflow_dispatch` 启动后总是先跑 `preview`
  - `preview` 只执行 dry-run，输出 `last_tag`、`next_version` 与 `next_tag`
  - `release` job 依赖 `preview` 输出，并通过 `release-approval` environment 暂停等待人工确认
  - 人工批准后，`release` 在同一 SHA 上执行真实打 tag，并把 preview / release 结果都写入 job summary
- 已按上一轮 PR review 修复 `auto-tag.yml`：
  - 删除 preview job 中与 job 级 `if` 重复的运行时分支校验
  - 为 release job 增加 `needs.preview.result == 'success'` 守卫
  - 为 preview / release 的 semantic-release action 显式安装 `conventional-changelog-conventionalcommits@9.1.0`
  - 在 release 前通过 GitHub API 校验 `PAT_TOKEN` 是否真实可访问当前仓库
  - 在 preview / release summary 中补充 snapshot 语义与生成的 release notes
- 已修复 preview 链路的第一轮鉴权前提：
  - 在 preview 开始前通过 GitHub API 校验 `PAT_TOKEN`
  - 将 preview 的 `semantic-release` 令牌从 `${{ github.token }}` 切换为 `${{ secrets.PAT_TOKEN }}`
  - 在 preview summary 中明确说明 dry-run 仍会执行远端 push 权限探测，避免将 403 误判为版本计算失败
- 已明确真实打 tag 仍使用 `PAT_TOKEN`，因为 `GITHUB_TOKEN` 推送的 tag 不会继续触发 `publish.yml`
- 已更新 `AGENTS.md` 的 Conventional Commit 规则，显式补充：
  - `fix/perf/refactor -> patch`
  - `docs/test/chore/build/ci/style -> no release`
  - `BREAKING CHANGE` 或 `!` header -> major
- 已移除基于 `workflow_run` 和 `[release ci]` 的自动发版门闸，后续版本预览与真实发版都由维护者手动触发
- 已将 release 流程从“两次独立 workflow_dispatch”收敛为“同一次 run 里 preview + 审批 + release”的链路
- 已精简 active trace，移除已废弃的 `release_mode=preview|release` 中间方案，保留当前有效恢复点

## 历史验证

- `git describe --tags --abbrev=0`
  - 结果：通过
  - 备注：当前最新 tag 为 `v0.0.222`
- `git log --pretty=format:%h%x09%s v0.0.222..HEAD`
  - 结果：通过
  - 备注：最近版本窗口内存在多条 `feat(...)`，后续 dry-run 预期应提升 `minor`
- `dotnet build GFramework.Core.Abstractions/GFramework.Core.Abstractions.csproj -c Release -p:RestoreFallbackFolders=`
  - 结果：通过
  - 备注：`GFramework.Core.Abstractions` 与 `GFramework.Cqrs.Abstractions` Release 构建通过，`0 warning / 0 error`
- `npx --yes semantic-release --dry-run --no-ci`
  - 结果：受阻
  - 备注：当前工作树的本地 tag 历史在 `git fetch --tags` 阶段出现 `would clobber existing tag` 冲突，不能直接作为 dry-run 环境
- `git clone --branch main --single-branch git@github.com:GeWuYou/GFramework.git /tmp/gframework-semrel-dryrun`
  - 结果：通过
  - 备注：已建立干净临时克隆用于 dry-run 验证
- `npx --yes semantic-release --dry-run --no-ci`（在 `/tmp/gframework-semrel-dryrun`）
  - 结果：通过
  - 备注：dry-run 成功识别 `v0.0.222` 为最新 release，并分析 `269` 个提交；按当前规则会提升到下一次 `minor` 发布，预期 tag 为 `v0.1.0`
- `npx --yes -p semantic-release -p conventional-changelog-conventionalcommits@9.1.0 semantic-release --dry-run --no-ci`（在 `/tmp/gframework-semrel-dryrun`）
  - 结果：通过
  - 备注：成功加载 `@semantic-release/commit-analyzer` 与 `@semantic-release/release-notes-generator`，证明
    `conventionalcommits` preset 包可被解析；本次 dry-run 未继续出版本，是因为干净克隆的 `main` 已落后远端
- `dotnet build GFramework.Core.Abstractions/GFramework.Core.Abstractions.csproj -c Release -p:RestoreFallbackFolders=`（手动发版入口调整后复验）
  - 结果：通过
  - 备注：`0 warning / 0 error`
- `dotnet build GFramework.sln -c Release`
  - 结果：通过
  - 备注：Release 构建通过，`639 warning / 0 error`；warning 为仓库既有基线；preview 鉴权修复后复验结果与基线一致
