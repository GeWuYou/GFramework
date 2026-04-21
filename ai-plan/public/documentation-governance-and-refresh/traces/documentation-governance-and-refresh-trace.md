# Documentation Governance And Refresh 追踪

## 2026-04-19

### 阶段：local-plan 迁移收口（RP-001）

- 复核当前工作树后确认：worktree 根目录仅剩一个 legacy `local-plan/`，其内容属于文档治理与重写主题的
  durable recovery state，不应继续作为独立根目录入口存在
- 按 `ai-plan` 治理规则建立 `ai-plan/public/documentation-governance-and-refresh/` 主题目录，并补齐：
  - `todos/`
  - `traces/`
  - `archive/todos/`
  - `archive/traces/`
- 将原 `local-plan` 中的详细 tracking / trace 迁入主题内历史归档，并为 active 入口只保留当前恢复点、
  活跃事实、风险与下一步
- 在 `ai-plan/public/README.md` 中建立
  `docs/sdk-update-documentation` -> `documentation-governance-and-refresh` 的 worktree 映射
- 同步更新 `ai-plan-governance` 的 tracking / trace，记录本次迁移已验证当前工作树不再依赖 worktree-root
  `local-plan/`

### Archive Context

- 历史跟踪归档：
  - `ai-plan/public/documentation-governance-and-refresh/archive/todos/documentation-governance-and-refresh-history-through-2026-04-18.md`
- 历史 trace 归档：
  - `ai-plan/public/documentation-governance-and-refresh/archive/traces/documentation-governance-and-refresh-history-through-2026-04-18.md`

### 下一步

1. 后续继续该主题时，只从 `ai-plan/public/documentation-governance-and-refresh/` 进入，不再恢复 `local-plan/`
2. 若 active 入口再次积累多轮已完成且已验证阶段，继续按同一模式迁入该主题自己的 `archive/`

## 2026-04-21

### 阶段：栏目 landing page 收口（RP-002）

- 依据 `ai-plan/public/README.md` 的 worktree 映射恢复 `documentation-governance-and-refresh` 主题，并确认该分支下一步应优先处理 `docs/zh-CN/core/*`、`game/*` 与 `source-generators/*`
- 复核 `docs/zh-CN/core/index.md`、`docs/zh-CN/game/index.md`、`docs/zh-CN/source-generators/index.md` 后确认：这三页仍保留旧版“大而全教程”结构，与当前模块 README、包拆分关系和推荐接入路径明显漂移
- 对照 `GFramework.Core/README.md`、`GFramework.Game/README.md`、`GFramework.Core.SourceGenerators/README.md`、
  `GFramework.Game.SourceGenerators/README.md`、`GFramework.Cqrs.SourceGenerators/README.md` 与
  `GFramework.Godot.SourceGenerators/README.md`，重写三个栏目 landing page，使其回到“模块定位、包关系、最小接入路径、继续阅读”的可信入口形态
- 首次执行 `cd docs && bun run build` 时发现 VitePress 会把跳到 `docs/` 目录外的相对链接判定为 dead link，因此将 landing page 末尾的模块 README 入口改为纯文本路径提示而非站内链接
- 第二次执行 `cd docs && bun run build` 通过，说明当前 landing page 重写没有破坏站点构建

### 当前结论

- 当前默认导航入口已显著收敛，但专题页仍需逐页按源码与测试继续核对
- 后续优先级应从 `core` 专题页开始，再向 `game` 与 `source-generators` 扩展

### 下一步

1. 审核 `docs/zh-CN/core/architecture.md`、`context.md`、`lifecycle.md`、`command.md`、`query.md`、`cqrs.md`
2. 记录每页的失真点、真实 API 名称与应保留的最小示例
3. 完成一轮专题页重写后再次执行 `cd docs && bun run build`

### 补充：2026-04-21 内容引用迁移

- 按当前文档治理主题，继续清理活跃规范与面向读者的内容入口中的旧参考仓库命名
- `AGENTS.md` 已把“secondary evidence source”从特定项目名收口为 `ai-libs/` 下的已验证只读参考实现
- `GFramework.Game/README.md`、`GFramework.Game.Abstractions/README.md` 与
  `docs/zh-CN/game/index.md` 已同步改为 `ai-libs/` 参考表述，并去掉特定参考项目名称与项目内类型名线索
- `documentation-governance-and-refresh` active tracking 已同步把风险缓解中的参考来源更新为
  `ai-libs/` 下已验证参考实现
- 下一次专题页重写时，继续沿用同一表述，不再把特定参考项目名写入新的活跃文档入口
