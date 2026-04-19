# GFramework 文档治理执行轨迹

> 说明：本归档由 `2026-04-19` 的 `local-plan/` 迁移生成，原历史内容已按当前 `ai-plan` 目录语义做最小整理。

## 2026-04-18

### 关键事实

- 根 `README.md` 存在错误入口：`docs/zh-CN/tutorials/getting-started.md` 并不存在。
- VitePress 顶层导航已采用 `getting-started / core / game / godot / tutorials / more` 的信息架构，但根 README
  仍在使用另一套命名。
- `docs/zh-CN/abstractions/` 在 sidebar 中已存在分类，但缺少 `index.md` landing page。
- 多个对外模块缺失 `README.md`，尤其是 `GFramework.Cqrs`、`GFramework.Cqrs.Abstractions`、
  `GFramework.Game.SourceGenerators`、`GFramework.Cqrs.SourceGenerators`。
- `GFramework.Game/README.md` 与 `GFramework.Game.Abstractions/README.md` 存在明显失真，无法承担模块入口职责。

### 已执行决策

- 接受“旧文档不可默认信任”的前提，后续文档以代码、测试和 `CoreGrid` 真实用法为准。
- 保留 `abstractions` 为独立栏目，并为其补 landing page 与导航入口，而不是继续让它悬空。
- 把根 `README.md` 重写为站点 IA 的 GitHub 镜像入口，不再维护另一套栏目命名。
- 把 README 命名规范统一为 `README.md`，后续不新增 `ReadMe.md` 变体。

### 并行委派

- 子任务 A：`GFramework.Cqrs/README.md` 与 `GFramework.Cqrs.Abstractions/README.md`
- 子任务 B：`GFramework.Game/README.md` 与 `GFramework.Game.Abstractions/README.md`

### 已接受的子任务结论

- `GFramework.Cqrs/README.md`
  - 已按当前 runtime 结构补充 dispatcher、注册器、上下文扩展、source generator 协作与反射回退说明。
- `GFramework.Cqrs.Abstractions/README.md`
  - 已明确其仅为协议层，不提供默认 dispatcher 或注册逻辑。
- `GFramework.Game/README.md`
  - 已按配置、数据、设置、Scene/UI 路由、存储、序列化等真实运行时子系统重写。
- `GFramework.Game.Abstractions/README.md`
  - 已按契约边界、适用场景与 CoreGrid 的真实依赖方式重写。

### 当前主线改动

- 更新 `AGENTS.md` 文档治理规则
- 建立主题恢复文档
- 重写根 `README.md`
- 调整 `docs/.vitepress/config.mts`
- 重写 `docs/zh-CN/getting-started/index.md`
- 重写 `docs/zh-CN/getting-started/quick-start.md`
- 新增 `docs/zh-CN/abstractions/index.md`
- 重写 `GFramework.Core/README.md`
- 重写 `GFramework.Core.Abstractions/README.md`
- 新增 `GFramework.Game.SourceGenerators/README.md`
- 新增 `GFramework.Cqrs.SourceGenerators/README.md`
- 重写 `GFramework.Core.SourceGenerators/README.md`
- 审阅并接纳 `CQRS` / `Game` 两组 README 子任务结果

### 验证结果

- `rg` 校验确认：
  - 旧错误入口 `docs/zh-CN/tutorials/getting-started.md` 已不再被引用
  - `docs/zh-CN/getting-started/` 内已无跨到仓库根 README 的相对链接
- 文件存在性校验确认：
  - `GFramework.Cqrs`
  - `GFramework.Cqrs.Abstractions`
  - `GFramework.Game`
  - `GFramework.Game.Abstractions`
  - `GFramework.Core`
  - `GFramework.Core.Abstractions`
  - `GFramework.Game.SourceGenerators`
  - `GFramework.Cqrs.SourceGenerators`
  - `GFramework.Core.SourceGenerators`
  均已拥有 `README.md`
- 文档站验证：
  - 在 `docs/` 下执行 `bun install`
  - 在 `docs/` 下执行 `bun run build`
  - 构建成功；仅出现既有的大 chunk warning，无阻塞错误

### 下一步

- 继续核对 `docs/zh-CN/core/*` 的示例真实性
- 按栏目推进 `game/*` 与 `source-generators/*` 的专题页重写
- 视专题页真实情况决定第二轮是否拆分新的恢复子任务
