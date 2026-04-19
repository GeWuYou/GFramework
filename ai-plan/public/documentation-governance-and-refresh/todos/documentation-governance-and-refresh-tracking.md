# Documentation Governance And Refresh 跟踪

## 目标

继续以“文档必须可追溯到源码、测试与真实接入方式”为原则，收敛 `GFramework` 的仓库入口、模块入口与
`docs/zh-CN` 采用链路，避免未来再次出现 API、安装方式与目录结构失真。

## 当前恢复点

- 恢复点编号：`DOCUMENTATION-GOVERNANCE-REFRESH-RP-001`
- 当前阶段：`Phase 1`
- 当前焦点：
  - 已将当前工作树根目录的 legacy `local-plan/` 迁入 `ai-plan/public/documentation-governance-and-refresh/`
  - 第一轮治理已完成 `AGENTS.md`、根 `README.md`、`getting-started` 与第一批高优先级模块 `README.md`
  - 下一轮需要继续按栏目核对并重写 `docs/zh-CN/core/*`、`docs/zh-CN/game/*` 与
    `docs/zh-CN/source-generators/*`

## 当前状态摘要

- 文档治理规则已收口到仓库规范，README、站点入口与采用链路不再依赖旧文档自证
- 高优先级模块入口已补齐，首轮文档站构建校验已经通过
- 当前主题仍是 active topic，因为核心栏目专题页仍可能包含与实现漂移的旧内容

## 当前活跃事实

- 旧 `local-plan/` 的详细 todo 与 trace 已迁入主题内 `archive/`
- 当前分支 `docs/sdk-update-documentation` 已在 `ai-plan/public/README.md` 建立 topic 映射
- active 跟踪文件只保留当前恢复点、活跃事实、风险与下一步，不再重复保存已完成阶段的长篇历史

## 当前风险

- 旧专题页示例失真风险：`docs/zh-CN/core/*`、`game/*` 与 `source-generators/*` 中仍可能保留看似合理但与
  真实实现不一致的示例
  - 缓解措施：继续按源码、测试、`*.csproj` 与 `CoreGrid` 真实接法核对，不把旧文档当事实来源
- 采用路径误导风险：根聚合包与模块边界若再次被写错，会继续误导消费者的包选择
  - 缓解措施：保持“源码与包关系优先”的证据顺序，改动采用说明时同步核对包依赖与生成器 wiring
- Active 入口回膨胀风险：后续若把栏目级重写过程直接追加到 active 文档，会再次拖慢恢复
  - 缓解措施：阶段完成并验证后，继续把细节迁入本 topic 的 `archive/`

## 活跃文档

- 历史跟踪归档：[documentation-governance-and-refresh-history-through-2026-04-18.md](../archive/todos/documentation-governance-and-refresh-history-through-2026-04-18.md)
- 历史 trace 归档：[documentation-governance-and-refresh-history-through-2026-04-18.md](../archive/traces/documentation-governance-and-refresh-history-through-2026-04-18.md)

## 验证说明

- 旧 `local-plan/` 的详细实施历史与文档站构建结果已迁入主题内归档
- active 跟踪文件已按 `ai-plan` 治理规则精简为当前恢复入口

## 下一步

1. 继续按栏目核对 `docs/zh-CN/core/*`，列出仍失真的页面与示例
2. 再推进 `docs/zh-CN/game/*` 与 `docs/zh-CN/source-generators/*` 的专题页重写
3. 若下一轮重写完成且验证通过，将栏目级详细过程迁入本 topic 的 `archive/`
