# CQRS 重写迁移追踪

## 2026-04-30

### 阶段：CQRS vs Mediator 评估归档（CQRS-REWRITE-RP-063）

- 本轮按用户要求使用 `gframework-boot` 启动上下文后，先完成 `cqrs-rewrite` 现状核对，再并行对照
  `GFramework.Cqrs` 与 `ai-libs/Mediator`
- 只读评估结论已归档到 `ai-plan/public/cqrs-rewrite/archive/todos/cqrs-vs-mediator-assessment-rp063.md`
- 本轮关键判断：
  - `GFramework.Cqrs` 已完成对外部 `Mediator` 作为生产 runtime 依赖的替代
  - 当前尚未完成的是仓库内部旧 `Command` / `Query` API、兼容 seam、fallback 旧语义与测试命名的收口
  - 当前已吸收 `Mediator` 的统一消息模型、generator 优先注册与热路径缓存思路
  - 当前仍未完整吸收 publisher 策略抽象、细粒度 pipeline、telemetry / diagnostics / benchmark 体系与 runtime 主体生成
- 本轮把默认下一步从“继续盯 PR thread”调整为“围绕 publisher seam 与 dispatch/invoker 生成前移做下一轮设计收敛”

### 验证

- `dotnet build GFramework.Cqrs/GFramework.Cqrs.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`

## 活跃事实

- 当前主题仍处于 `Phase 8`
- 当前主题的主问题已从“是否完成外部依赖替代”转为“内部兼容层收口顺序与下一轮能力深化优先级”
- 已完成阶段的详细执行历史不再留在 active trace；默认恢复入口只保留当前恢复点、活跃事实、风险与下一步

## 当前风险

- 当前 `dotnet build GFramework.sln -c Release` 在 WSL 环境仍会受顶层 `GFramework.csproj` 的 Windows NuGet fallback 配置影响
- 若不把“生产替代完成”与“仓库内部收口完成”分开记录，后续很容易重复争论当前 CQRS 迁移是否已经完成

## Archive Context

- 当前评估归档：
  - `ai-plan/public/cqrs-rewrite/archive/todos/cqrs-vs-mediator-assessment-rp063.md`
- 历史 trace 归档：
  - `ai-plan/public/cqrs-rewrite/archive/traces/cqrs-rewrite-history-through-rp043.md`
  - `ai-plan/public/cqrs-rewrite/archive/traces/cqrs-rewrite-history-rp046-through-rp061.md`

## 当前下一步

1. 补一轮最小 Release 构建验证，确认本次 `ai-plan` 与评估文档更新未引入仓库级异常
2. 以 `notification publisher seam` 与 `dispatch/invoker` 生成前移为优先对象，形成下一轮可执行设计
