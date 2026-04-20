# Documentation Governance And Refresh 跟踪

## 目标

继续以“文档必须可追溯到源码、测试与真实接入方式”为原则，收敛 `GFramework` 的仓库入口、模块入口与
`docs/zh-CN` 采用链路，避免未来再次出现 API、安装方式与目录结构失真。

## 当前恢复点

- 恢复点编号：`DOCUMENTATION-GOVERNANCE-REFRESH-RP-002`
- 当前阶段：`Phase 2`
- 当前焦点：
  - 已完成 `docs/zh-CN/core/index.md`、`docs/zh-CN/game/index.md` 与
    `docs/zh-CN/source-generators/index.md` 的 landing page 重写
  - 栏目入口已改为以模块定位、包关系、最小接入路径和继续阅读为主，不再沿用旧版失真教程结构
  - 下一轮需要继续核对并重写 `docs/zh-CN/core/*`、`docs/zh-CN/game/*` 与
    `docs/zh-CN/source-generators/*` 的专题页内容

## 当前状态摘要

- 文档治理规则已收口到仓库规范，README、站点入口与采用链路不再依赖旧文档自证
- 高优先级模块入口已补齐，栏目 landing page 已回到可作为默认导航入口的状态
- 当前主题仍是 active topic，因为核心栏目下的专题页仍可能包含与实现漂移的旧内容

## 当前活跃事实

- 旧 `local-plan/` 的详细 todo 与 trace 已迁入主题内 `archive/`
- 当前分支 `docs/sdk-update-documentation` 已在 `ai-plan/public/README.md` 建立 topic 映射
- active 跟踪文件只保留当前恢复点、活跃事实、风险与下一步，不再重复保存已完成阶段的长篇历史
- `core`、`game` 与 `source-generators` 三个栏目入口页现在都以模块 README 与当前包拆分为准
- `docs` 站点构建已验证通过，修正了 VitePress 对 `docs/` 目录外相对链接的 dead-link 检查问题

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
- `cd docs && bun run build`

## 下一步

1. 先从 `docs/zh-CN/core/*` 开始，逐页核对架构、上下文、生命周期、命令、查询与 CQRS 的示例和术语
2. 再推进 `docs/zh-CN/game/*` 与 `docs/zh-CN/source-generators/*` 的专题页重写，优先处理仍引用旧安装方式或旧 API 的页面
3. 若专题页批量重写完成且验证通过，将本轮 landing page 收口和下一轮专题页修订过程迁入本 topic 的 `archive/`
