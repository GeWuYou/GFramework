# Documentation Full Coverage Governance Validation History (RP-049 to RP-052)

## 2026-04-29 / RP-049

### 页面校验

- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/storage.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/setting.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/signal.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/logging.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/index.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/scene.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/config-system.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/core/index.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/ecs/arch.md`
  - 结果：通过；本轮页面的 frontmatter、链接与代码块校验通过。

### README 链接校验

- `bash .agents/skills/gframework-doc-refresh/scripts/validate-links.sh GFramework.Godot/README.md tools/gframework-config-tool/README.md`
  - 结果：通过；reader-facing 链接标签调整后目标有效。

### 站点构建

- `bun run build`（工作目录：`docs/`）
  - 结果：通过；站点仍可构建，仅保留既有大 chunk warning。

## 2026-04-29 / RP-050

### 页面校验

- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/data.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/game/storage.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/godot/ui.md`
  - 结果：通过；本轮 3 个页面的 frontmatter、链接与代码块校验通过。

### README 链接校验

- `bash .agents/skills/gframework-doc-refresh/scripts/validate-links.sh GFramework.Cqrs.Abstractions/README.md GFramework.SourceGenerators.Common/README.md`
  - 结果：通过；README reader-facing 标签调整后目标有效。

### 站点构建

- `bun run build`（工作目录：`docs/`）
  - 结果：通过；本轮 reader-facing 收口后站点仍可构建，仅保留既有大 chunk warning。

## 2026-04-30 / RP-051

### 页面校验

- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators/schema-config-generator.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators/index.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/source-generators/cqrs-handler-registry-generator.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/core/cqrs.md`
- `bash .agents/skills/gframework-doc-refresh/scripts/validate-all.sh docs/zh-CN/api-reference/index.md`
  - 结果：通过；新增专题页与相关入口页校验通过。

### 站点构建

- `bun run build`（工作目录：`docs/`）
  - 结果：通过；新增 `Schema 配置生成器` 入口后站点仍可构建，仅保留既有大 chunk warning。

## 2026-04-30 / RP-052

### 提交后状态确认

- `git status --short --branch`
- `git diff --name-only origin/main...HEAD | wc -l`
- `git diff --numstat origin/main...HEAD`
  - 结果：通过；提交后工作树 clean，相对 `origin/main` 的 committed diff 为 `8` files / `337` lines。
