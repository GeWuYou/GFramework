# Semantic Release 版本迁移归档（SEMREL-RP-004，2026-05-02）

## 归档范围

- `feat/release-summary-notes` 分支的 release notes 模板修复
- PR 归属展示与重复 commit 输出问题收敛
- `SEMREL-RP-004` 的本地验证与合并后分支收尾

## 历史完成项

- 已确认 `.github/cliff.toml` 中旧模板会先输出未分组 commit 列表，再输出 grouped commit 列表，导致同一批变更重复出现。
- 已移除未分组 commit 循环，只保留按 Conventional Commit group 分类后的 `What's Changed` 输出。
- 已保留每条变更末尾的 `by @user in #PR` 输出，避免新增独立 PR 索引章节造成重复。
- 已将 `.github/workflows/publish.yml` 的 GitHub Release 正文改为 `body_path: RELEASE_NOTES.md`，复用 `git-cliff-action` 生成的文件。
- 已将 `feat/release-summary-notes` 合入 `main`，本地 `main` 已快进到合并提交 `35a62e6b`。
- 已从 `ai-plan/public/README.md` 移除 `feat/release-summary-notes` 的 active topic 映射；`semantic-release-versioning` 主题本身仍保持 active。

## 历史验证

- `.github/cliff.toml` 通过 Python `tomllib` 解析。
- `.github/workflows/publish.yml` 通过 PyYAML 解析。
- `yq` 确认 GitHub Release step 使用 `body_path: RELEASE_NOTES.md`。
- `dotnet build GFramework.sln -c Release` 通过，`0 warning / 0 error`。
