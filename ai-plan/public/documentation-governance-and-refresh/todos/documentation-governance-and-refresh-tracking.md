# Documentation Governance And Refresh 跟踪

## 目标

继续以“文档必须可追溯到源码、测试与真实接入方式”为原则，收敛 `GFramework` 的仓库入口、模块入口与
`docs/zh-CN` 采用链路，避免未来再次出现 API、安装方式与目录结构失真。

## 当前恢复点

- 恢复点编号：`DOCUMENTATION-GOVERNANCE-REFRESH-RP-004`
- 当前阶段：`Phase 3`
- 当前焦点：
  - 已完成 `docs/zh-CN/core/architecture.md`、`context.md`、`lifecycle.md`、`command.md`、`query.md` 与
    `cqrs.md` 的专题页重写
  - `core` 关键专题页已改回当前 `Architecture`、`ArchitectureContext`、旧 Command/Query 兼容层与新 CQRS
    runtime 的真实入口语义
  - 下一轮需要继续推进 `docs/zh-CN/core/*` 余下专题页，以及 `docs/zh-CN/game/*`、
    `docs/zh-CN/source-generators/*` 的专题页核对

## 当前状态摘要

- 文档治理规则已收口到仓库规范，README、站点入口与采用链路不再依赖旧文档自证
- 高优先级模块入口与 `core` 关键专题页已回到可作为默认导航入口的状态
- 当前主题仍是 active topic，因为 `core` 其余专题页及 `game`、`source-generators` 栏目下仍可能包含与实现漂移的旧内容

## 当前活跃事实

- 旧 `local-plan/` 的详细 todo 与 trace 已迁入主题内 `archive/`
- 当前分支 `docs/sdk-update-documentation` 已在 `ai-plan/public/README.md` 建立 topic 映射
- active 跟踪文件只保留当前恢复点、活跃事实、风险与下一步，不再重复保存已完成阶段的长篇历史
- `core`、`game` 与 `source-generators` 三个栏目入口页现在都以模块 README 与当前包拆分为准
- `docs` 站点构建已验证通过，修正了 VitePress 对 `docs/` 目录外相对链接的 dead-link 检查问题
- `core` 关键专题页已移除 `Init()`、属性式 `CommandBus` / `QueryBus`、旧 `Input` 赋值式示例和已移除的
  `RegisterMediatorBehavior` 等过时说明
- `core/index.md` 已把 `Godot` 与 `Source Generators` 栏目入口改成可点击链接，补齐 landing page 导航一致性
- `documentation-governance-and-refresh` active trace 已把重复的 `### 下一步` 标题改成带恢复点标识的唯一标题，消除
  `MD024/no-duplicate-heading` 告警
- `gframework-pr-review` 脚本已修复“空 `APPROVED` review 覆盖非空 CodeRabbit review body”的解析路径，当前分支可重新提取 Nitpick comments

## 当前风险

- 旧专题页示例失真风险：`docs/zh-CN/core/*`、`game/*` 与 `source-generators/*` 中仍可能保留看似合理但与
  真实实现不一致的示例
  - 缓解措施：继续按源码、测试、`*.csproj` 与 `ai-libs/` 下已验证参考实现核对，不把旧文档当事实来源
- 采用路径误导风险：根聚合包与模块边界若再次被写错，会继续误导消费者的包选择
  - 缓解措施：保持“源码与包关系优先”的证据顺序，改动采用说明时同步核对包依赖与生成器 wiring
- Active 入口回膨胀风险：后续若把栏目级重写过程直接追加到 active 文档，会再次拖慢恢复
  - 缓解措施：阶段完成并验证后，继续把细节迁入本 topic 的 `archive/`
- review 跟进遗漏风险：如果 PR review 抓取继续优先选中空 review body，会漏掉 CodeRabbit 的 Nitpick 和
  linter 跟进项
  - 缓解措施：保持当前“最新提交 + 最新非空 CodeRabbit review body”解析策略，并在有疑点时以 API 实抓结果复核

## 活跃文档

- 历史跟踪归档：[documentation-governance-and-refresh-history-through-2026-04-18.md](../archive/todos/documentation-governance-and-refresh-history-through-2026-04-18.md)
- 历史 trace 归档：[documentation-governance-and-refresh-history-through-2026-04-18.md](../archive/traces/documentation-governance-and-refresh-history-through-2026-04-18.md)

## 验证说明

- 旧 `local-plan/` 的详细实施历史与文档站构建结果已迁入主题内归档
- active 跟踪文件已按 `ai-plan` 治理规则精简为当前恢复入口
- `cd docs && bun run build`
- `python3 .codex/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --format json`

## 下一步

1. 继续核对 `docs/zh-CN/core/*` 余下专题页，优先处理 `events`、`property`、`state-management`、`coroutine`
   与 `logging`
2. 再推进 `docs/zh-CN/game/*` 与 `docs/zh-CN/source-generators/*` 的专题页重写，优先处理仍引用旧安装方式或旧 API 的页面
3. 若 active trace 再积累新的已完成阶段，按恢复点粒度迁入 `archive/traces/`，避免默认启动入口再次膨胀
