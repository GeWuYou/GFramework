# AI-Plan Governance 跟踪

## 当前恢复点

- 编号：`AI-PLAN-GOVERNANCE-RP-003`
- 分支：`chore/skill-governance`
- 阶段：本地实现与检查完成，准备提交及 GitHub PR；创建后交接。
- 目标：将六个 skills 收敛为文档刷新、Issue 梳理和 PR 审查三个专用入口。

## 决策与边界

- 启动、续作与显式委派规则归 AGENTS.md；删除三个流程包装及其菜单，不复制到适配器或全局 skills。
- 保留模块 JSON 映射、文档校验器、模板和 GitHub 取证脚本 CLI；删除无消费者的重复 shell 映射与提示脚本。
- Issue JSON 保留旧模式标签与 boot_handoff 字段，recommended_skill 改为 null；按 AGENTS.md 直接核实和续作。
- 审阅与梳理本身保持只读；必要启动规则和恢复文档不依赖 harness，框架 API 与运行时不变。
- 工作区接入采用独立本地适配器，机器绑定与证据不进入本仓库。
- GitHub 发布使用 gh；PR 创建后只允许一次自动合并设置与回读，随后保留任务工作树交接。

## 验证与风险

- 通过：三个 skill 的 quick_validate、中文菜单配置、相对链接与失效入口检查。
- 通过：六个模块输入场景（正常、三个别名、歧义、未知）；Issue 7 项、PR 1 项、许可证脚本 7 项单元测试。
- 通过：许可证、1182 个已跟踪 C# 文件命名、运行时/生成器边界、git diff --check。
- 通过：独立适配器 schema 校验、六个能力 dry-run、带任务工具链的 workspace doctor，以及 harness check。
- 通过：Core.Abstractions Release 构建及其 Cqrs.Abstractions 依赖，0 warning / 0 error；本次不修改 C# 代码。
- 未执行：完整 solution 构建与 .NET 测试、文档站构建（本次无运行时或站点页面变更）。
- 文件结构检查与人工场景审阅不能证明未来 agent 行为或效率提升；客户端菜单渲染需单独观察。
- 历史 skill 调用保留在归档中，不应作为当前执行入口。

## 下一步

审查并提交完整任务 diff；通过 gh 核对关联 issue、发布 PR 并设置一次自动合并，回读后停止等待。
准确 PR/head、租约与本地证据保存在任务交接记录；后续在允许的唤醒后核实合并再释放工作树。
历史阶段见 [RP-002 跟踪归档](../archive/todos/ai-plan-governance-tracking-rp002.md)。
