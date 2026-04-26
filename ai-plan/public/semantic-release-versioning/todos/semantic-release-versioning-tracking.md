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

- 恢复点编号：`SEMREL-RP-001`
- 当前阶段：`Phase 1`
- 当前焦点：
  - 增加 `.releaserc.json`，仅启用版本分析与 release notes 生成，不启用 GitHub Release 发布插件
  - 将 `auto-tag.yml` 改成纯 `workflow_dispatch` 双模式入口，由维护者手动选择 `preview` 或 `release`
  - 明确 `PAT_TOKEN` 与 `GITHUB_TOKEN` 的职责边界，确保 tag 继续触发 `publish.yml`

### 已知风险

- `GITHUB_TOKEN` 推送 tag 不会再触发另一个 workflow，真实发布仍需要 `PAT_TOKEN`
- `semantic-release` 的版本判断完全依赖 Conventional Commits；不规范提交会直接影响版本计算
- 当前仓库本地 `dotnet clean/build` 仍受 WSL fallback NuGet 路径影响，验证时需要继续采用已知可用的直接构建命令

## 已完成

- 已确认当前版本入口为 `.github/workflows/auto-tag.yml`，现状始终执行 `PATCH + 1`
- 已确认当前 `.github/workflows/publish.yml` 由 tag 触发，并负责 `.nupkg` 打包、发布和 GitHub Release
- 已确认最新 tag 为 `v0.0.222`
- 已确认 `v0.0.222..HEAD` 之间存在 `feat(...)` 提交，按目标规则首次 dry-run 预期版本应为 `v0.1.0`
- 已新增 `.releaserc.json`，仅保留 `@semantic-release/commit-analyzer` 与
  `@semantic-release/release-notes-generator`，避免 `semantic-release` 直接创建 GitHub Release
- 已将 `.github/workflows/auto-tag.yml` 重写为：
  - `workflow_dispatch` 由维护者手动选择 `preview` 或 `release`
  - `preview` 只执行 dry-run，输出 `last_tag`、`next_version` 与 `next_tag`
  - `release` 由维护者手动触发真实打 tag，并把结果写入 job summary
- 已明确真实打 tag 仍使用 `PAT_TOKEN`，因为 `GITHUB_TOKEN` 推送的 tag 不会继续触发 `publish.yml`
- 已更新 `AGENTS.md` 的 Conventional Commit 规则，显式禁止把纯文档变更写成 `feat(...)` 或 `feat(docs)`
- 已移除基于 `workflow_run` 和 `[release ci]` 的自动发版门闸，后续版本预览与真实发版都由维护者手动触发

## 验证

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
- `dotnet build GFramework.Core.Abstractions/GFramework.Core.Abstractions.csproj -c Release -p:RestoreFallbackFolders=`（手动发版入口调整后复验）
  - 结果：通过
  - 备注：`0 warning / 0 error`

## 下一步

1. 复核 `workflow_dispatch` 的 `preview` / `release` 两种模式命名是否还要进一步收紧
2. 评估是否要在 release 模式中补充额外输入，例如预期 tag 确认或二次确认文本
3. 若本轮验证通过，按仓库要求创建补充提交并等待你审阅手动发版流程细节
