# Semantic Release 版本迁移追踪

## 2026-04-26

### 阶段：方案落地准备（SEMREL-RP-001）

- 读取当前 `auto-tag.yml` 与 `publish.yml`，确认最小侵入改法应只替换版本判断与打 tag，保留 tag 触发发布链
- 核对最近 tag 与提交历史：
  - 最新 tag 为 `v0.0.222`
  - `v0.0.222..HEAD` 含多条 `feat(...)`，按目标规则首次 dry-run 预期结果为 `v0.1.0`
- 补建本主题的 active tracking / trace 入口，并在 `ai-plan/public/README.md` 中为
  `feat/semantic-release-versioning` 建立 worktree 映射

### 阶段：配置落地与验证（SEMREL-RP-001）

- 新增 `.releaserc.json`，显式固定：
  - `feat -> minor`
  - `fix/perf/refactor -> patch`
  - `docs/test/chore/build/ci/style -> no release`
  - `BREAKING CHANGE` / `BREAKING CHANGES` 作为 major 信号
- 重写 `auto-tag.yml`：
  - 改为纯 `workflow_dispatch` 手动入口
  - 增加 `release_mode=preview|release` 输入
  - `preview` 只跑 dry-run 并输出 `last_tag`、`next_version`、`next_tag`
  - `release` 由维护者手动触发真实打 tag，并要求 `PAT_TOKEN`
- 收紧 `AGENTS.md` 的提交信息约束，明确提交类型必须反映 release semantics，纯文档变更禁止写成 `feat(...)`
- 按用户反馈继续调整发版入口：
  - 删除基于 `workflow_run` 的自动发版路径
  - 统一改为 `workflow_dispatch` 手动触发
  - 通过 `release_mode=preview|release` 区分“查看下个版本”和“真实打 tag”
- 复验最小构建命令：
  - `dotnet build GFramework.Core.Abstractions/GFramework.Core.Abstractions.csproj -c Release -p:RestoreFallbackFolders=`
  - 结果：通过，`0 warning / 0 error`
- 完成最小构建验证：
  - `dotnet build GFramework.Core.Abstractions/GFramework.Core.Abstractions.csproj -c Release -p:RestoreFallbackFolders=`
  - 结果：通过，`0 warning / 0 error`
- 直接在当前工作树执行 `semantic-release --dry-run` 时命中本地 tag 抓取冲突：
  - `git fetch --tags ... would clobber existing tag`
  - 结论：当前工作树不适合作为 dry-run 验证环境
- 改用干净临时克隆 `/tmp/gframework-semrel-dryrun` 再跑 dry-run：
  - 成功识别 `v0.0.222` 为最新 release
  - 成功分析 `269` 个提交
  - 按当前规则得出下一次应为 `minor` 发布，预期版本窗口从 `0.0.222` 提升到 `0.1.0`

### 下一步

1. 复核变更 diff 并创建提交
2. 向用户说明新的发版链路与可优化点
