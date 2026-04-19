# GFramework 文档治理与重写任务单

> 说明：本归档由 `2026-04-19` 的 `local-plan/` 迁移生成，原历史内容已按当前 `ai-plan` 目录语义做最小整理。

目标：

- 重建 `GFramework` 的总入口、模块入口和 `docs/zh-CN` 采用链路，避免继续传播失真的 API、安装方式和目录结构。
- 把文档更新责任写入 `AGENTS.md`，避免未来继续出现“新增模块没有 `README.md`”或“改行为不改文档”的回归。
- 建立主题恢复文档与执行轨迹，让后续 AI 或人工可以在任意阶段无损接手。

阶段完成标准：

- `AGENTS.md` 明确规定 README、docs、示例真实性与恢复文档维护要求
- 根 `README.md` 与 VitePress 顶层信息架构一致，不再包含错误入口
- `getting-started` 入口与最小示例不再依赖失真的旧文档
- 高优先级模块至少补齐或重写以下 README：
  - `GFramework.Cqrs`
  - `GFramework.Cqrs.Abstractions`
  - `GFramework.Game`
  - `GFramework.Game.Abstractions`
  - `GFramework.Core`
  - `GFramework.Core.Abstractions`
  - `GFramework.Game.SourceGenerators`
  - `GFramework.Cqrs.SourceGenerators`
  - `GFramework.Core.SourceGenerators`
- `docs/zh-CN/abstractions/` 拥有真实 landing page，且导航不再悬空

---

## 事实来源

当前文档工作必须遵守以下证据优先级：

1. 源码、`*.csproj`、包关系、生成器 targets
2. 测试与 snapshot
3. `CoreGrid` 的真实接入方式与目录组织
4. 现有 `README.md` 与 `docs/zh-CN`

说明：

- 旧文档只作为待修输出，不作为行为真相。
- `CoreGrid` 仅用于补充真实采用路径，不反向定义框架规范。

---

## 当前阶段

Status: 🟡 In Progress

当前阶段目标：

- [x] 建立主题 todo 与 trace
- [x] 收紧 `AGENTS.md` 文档治理规则
- [x] 修复根 `README.md` 的信息架构和错误入口
- [x] 为 `docs/zh-CN/abstractions/` 补 landing page，并接回导航
- [x] 重写 `getting-started/index.md`
- [x] 重写 `getting-started/quick-start.md`
- [x] 补齐 / 重写高优先级模块 README
- [x] 运行一轮链接与文档存在性校验

---

## P0 必做

### 1. 文档治理规则收口

Status: 🟢 Implemented

- [x] 明确 README 必须随模块建立
- [x] 明确文档证据优先级
- [x] 明确根 `README.md` 必须镜像站点 IA
- [x] 明确文档任务必须维护恢复文档

### 2. 总入口修复

Status: 🟢 Implemented

- [x] 修复根 `README.md` 中不存在的入口
- [x] 统一文档栏目命名
- [x] 调整根 README 到栏目 landing page 的链接
- [x] 校正根 README 中的包选择说明

### 3. 采用链路重写

Status: 🟢 Implemented

- [x] 重写 `docs/zh-CN/getting-started/index.md`
- [x] 重写 `docs/zh-CN/getting-started/quick-start.md`
- [x] 为 `abstractions` 栏目补 landing page

### 4. 模块 README 第一批

Status: 🟢 Implemented

- [x] `GFramework.Cqrs`
- [x] `GFramework.Cqrs.Abstractions`
- [x] `GFramework.Game`
- [x] `GFramework.Game.Abstractions`
- [x] `GFramework.Core`
- [x] `GFramework.Core.Abstractions`
- [x] `GFramework.Game.SourceGenerators`
- [x] `GFramework.Cqrs.SourceGenerators`
- [x] `GFramework.Core.SourceGenerators`

### 5. 最小化验证

Status: 🟢 Implemented

- [x] 确认旧错误入口 `tutorials/getting-started.md` 不再被引用
- [x] 确认 `abstractions` landing page 已接回导航
- [x] 确认第一批高优先级 README 文件均已存在
- [x] 运行 `bun install`
- [x] 运行 `bun run build`

---

## 当前恢复点

Recovery Point ID: `doc-refresh-phase0-1-and-module-readmes`

当前推荐恢复步骤：

1. 以 `core/`、`game/`、`source-generators/` 三个栏目为单位继续核对专题页内容真实性
2. 按栏目整理“旧文档仍失真”的页面清单
3. 把第二轮重写拆成按栏目推进的 todo

---

## 已接受的并行工作

- `CQRS` README 子任务：补写 `GFramework.Cqrs/README.md` 与 `GFramework.Cqrs.Abstractions/README.md`
- `Game` README 子任务：重写 `GFramework.Game/README.md` 与 `GFramework.Game.Abstractions/README.md`

已验收结果：

- `CQRS` README 已按当前代码结构补齐运行时、契约层、生成注册表与反射回退协作说明
- `Game` README 已按运行时实现、契约层边界与 CoreGrid 实际接法重写

主代理负责：

- `AGENTS.md`
- 根 `README.md`
- 主题恢复文档
- `docs/.vitepress/config.mts`
- `docs/zh-CN/getting-started/*`
- `docs/zh-CN/abstractions/index.md`
- `GFramework.Core*` 与 Source Generator 相关 README

---

## 风险与注意事项

- 现有 `docs/zh-CN` 中不少示例看起来“像真的”，但不能直接复用，必须回到代码核实。
- 根包 `GeWuYou.GFramework` 当前只聚合 `Core` 与 `Game`，文档不能再把它描述为全部模块集合。
- `LICENSE` 与部分包元数据之间可能存在历史漂移，本轮先以仓库根许可证和当前用户可见入口为准，不顺带修包元数据。

---

## 下一步

- 按栏目继续重写 `docs/zh-CN/core/*`
- 再推进 `docs/zh-CN/game/*` 与 `docs/zh-CN/source-generators/*`
- 视内容漂移程度决定是否补第二轮模块 README 或直接切专题页
