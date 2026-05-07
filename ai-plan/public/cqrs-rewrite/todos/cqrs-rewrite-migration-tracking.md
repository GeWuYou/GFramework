# CQRS 重写迁移跟踪

## 目标

围绕 `GFramework` 当前的双轨 CQRS 现状，继续完成以“去外部依赖、降低反射、收口公开入口”为目标的
CQRS 迁移与收敛。

## 当前恢复点

- 恢复点编号：`CQRS-REWRITE-RP-092`
- 当前阶段：`Phase 8`
- 当前 PR 锚点：`待创建（当前分支 feat/cqrs-optimization 尚未为 RP-092 建立新 PR）`
- 当前结论：
  - `GFramework.Cqrs` 已完成对外部 `Mediator` 的生产级替代，当前主线已从“是否可替代”转向“仓库内部收口与能力深化顺序”
  - `dispatch/invoker` 生成前移已扩展到 request / stream 路径，`RP-077` 已补齐 request invoker provider gate 与 stream gate 对称的 descriptor / descriptor entry runtime 合同回归
  - `RP-078` 已补齐 mixed fallback metadata 在 runtime 不允许多个 fallback attribute 实例时的单字符串 attribute 回退回归
  - `RP-079` 已补齐 runtime 缺少 generated handler registry interface 时的 generator 静默跳过回归
  - `RP-080` 已将基础 generation gate 回归扩展到 notification handler interface、stream handler interface 与 registry attribute 缺失分支
  - `RP-081` 已继续补齐基础 generation gate 的 logging 与 DI runtime contract 缺失分支
  - 当前 `RP-082` 已补齐基础 generation gate 的 request handler runtime contract 缺失分支
  - `RP-083` 已补齐 mixed direct / reflected-implementation request 与 stream invoker provider 发射顺序回归
  - `RP-084` 已引入独立 `GFramework.Cqrs.Benchmarks` 项目，作为持续吸收 `Mediator` benchmark 组织方式的第一落点
  - `RP-085` 已补齐 stream request benchmark，对齐 `Mediator` messaging benchmark 的第二个核心场景
  - `RP-086` 已补齐 request pipeline `0 / 1 / 4` 数量矩阵，开始把 benchmark 关注点从单纯 messaging steady-state 扩展到行为编排开销
  - `RP-087` 已补齐 request startup benchmark，把 initialization 与 cold-start 维度正式纳入 `GFramework.Cqrs.Benchmarks`
  - 当前 `RP-088` 已补齐 request invoker reflection / generated-provider 对照，开始直接量化 dispatcher 预热 generated descriptor 的收益
  - 当前 `RP-089` 已补齐 stream invoker reflection / generated-provider 对照，使 generated descriptor 预热收益从 request 扩展到 stream 路径
  - 当前 `RP-090` 已收敛 `PR #326` benchmark review：统一 benchmark 最小宿主构建、冻结 GFramework 容器、限制 MediatR 扫描范围，并恢复 request startup cold-start 对照
  - 当前 `RP-091` 已把 benchmark 项目发布面隔离与包清单校验前移到 PR：`GFramework.Cqrs.Benchmarks` 明确保持不可打包，`publish.yml` 与 `ci.yml` 复用同一份 packed-modules 校验脚本
  - 当前 `RP-092` 已补齐 request handler `Singleton / Transient` 生命周期矩阵 benchmark，并明确把 `Scoped` 对照留到具备真实显式作用域边界的宿主模型后再评估
- `ai-plan` active 入口现以 `RP-092` 为最新恢复锚点；`PR #331`、`PR #326`、`PR #323`、`PR #307` 与其他更早阶段细节均以下方归档或说明为准

## 当前活跃事实

- 当前分支为 `feat/cqrs-optimization`
- 本轮 `$gframework-batch-boot 50` 以 `origin/main` (`2c58d8b6`, 2026-05-07 13:24:46 +0800) 为基线；本地 `main` (`c2d22285`) 已落后，不作为 branch diff 基线
- `GFramework.Cqrs.Benchmarks` 作为 benchmark 基础设施项目，必须持续排除在 NuGet / GitHub Packages 发布集合之外
- `GFramework.Cqrs.Benchmarks` 现已覆盖 request steady-state、pipeline 数量矩阵、startup、request/stream generated invoker，以及 request handler `Singleton / Transient` 生命周期矩阵
- 发布工作流已有 packed modules 校验，但 PR 工作流此前没有等价的 solution pack 产物名单校验
- 本地 `dotnet pack GFramework.sln -c Release --no-restore -o <temp-dir>` 当前只产出 14 个预期包，未复现 benchmark `.nupkg`
- latest-head review 现仍有少量 open thread，但本地复核后，仍成立的问题已收敛到 benchmark 对照公平性、workflow 输入安全性与 active 文档压缩
- benchmark 场景现统一通过 `BenchmarkHostFactory` 构建最小宿主：GFramework 侧在 runtime 分发前显式 `Freeze()` 容器，MediatR 侧只扫描当前场景需要的 handler / behavior 类型
- `RequestStartupBenchmarks` 已恢复 `ColdStart_GFrameworkCqrs` 结果产出，不再命中 `No CQRS request handler registered`
- `BenchmarkDotNet` 在当前 agent 沙箱里会因自动生成的 bootstrap 脚本异常失败；同一 `dotnet run --no-build` 命令在沙箱外执行通过，因此本轮以沙箱外结果作为 benchmark 权威验证
- 已新增手动触发的 benchmark workflow；默认只验证 benchmark 项目 Release build，只有显式提供过滤器时才执行 BenchmarkDotNet 运行；过滤器输入现通过环境变量传入 shell，避免 workflow_dispatch 输入直接插值到命令行
- 远端 `CTRF` 最新汇总为 `2274/2274` passed
- `MegaLinter` 当前只暴露 `dotnet-format` 的 `Restore operation failed` 环境噪音，尚未提供本地仍成立的文件级格式诊断

## 当前风险

- 顶层 `GFramework.sln` / `GFramework.csproj` 在 WSL 下仍可能受 Windows NuGet fallback 配置影响，完整 solution 级验证成本高于模块级验证
- 若后续新增 benchmark / example / tooling 项目但未同步校验发布面，solution 级 `dotnet pack` 仍可能在 tag 发布前才暴露异常包
- `RequestStartupBenchmarks` 为了量化真正的单次 cold-start，引入了 `InvocationCount=1` / `UnrollFactor=1` 的专用 job；该配置会触发 BenchmarkDotNet 的 `MinIterationTime` 提示，后续若要做稳定基线比较，还需要决定是否引入批量外层循环或自定义 cold-start harness
- 当前 benchmark 宿主仍刻意保持“单根容器最小宿主”模型；若要公平比较 `Scoped` handler 生命周期，需要先引入显式 scope 创建与 scope 内首次解析的对照基线
- 仓库内部仍保留旧 `Command` / `Query` API、`LegacyICqrsRuntime` alias 与部分历史命名语义，后续若不继续分批收口，容易混淆“对外替代已完成”与“内部收口未完成”
- 若继续扩大 generated invoker 覆盖面，需要持续区分“可静态表达的合同”与 `PreciseReflectedRegistrationSpec` 等仍需保守回退的场景

## 最近权威验证

- `dotnet pack GFramework.sln -c Release --no-restore -o /tmp/gframework-pack-validation -p:IncludeSymbols=false`
  - 结果：通过
  - 备注：当前本地产物仅包含 14 个预期发布包，未生成 `GFramework.Cqrs.Benchmarks.*.nupkg`
- `bash scripts/validate-packed-modules.sh /tmp/gframework-pack-validation`
  - 结果：通过
  - 备注：共享脚本确认 actual package set 与预期 14 个发布包完全一致
- `dotnet build GFramework.Cqrs.Benchmarks/GFramework.Cqrs.Benchmarks.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`
- `python3 scripts/license-header.py --check`
  - 结果：通过
- `dotnet run --project GFramework.Cqrs.Benchmarks/GFramework.Cqrs.Benchmarks.csproj -c Release -- --filter "*RequestStartupBenchmarks*" --job short --warmupCount 1 --iterationCount 1 --launchCount 1`
  - 结果：通过
  - 备注：`ColdStart_GFrameworkCqrs` 已恢复出数，最新本地输出约 `220-292 us`，MediatR 对照约 `575-616 us`；当前仅剩 BenchmarkDotNet 对单次 cold-start 场景的 `MinIterationTime` 提示
- `dotnet build GFramework.Cqrs.Benchmarks/GFramework.Cqrs.Benchmarks.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`
  - 备注：用于验证本轮 request invoker / pipeline / stream invoker 调整与 benchmark workflow 改动后的 Release 编译结果
- `python3 .agents/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --format json --json-output <temporary-json-output>`
  - 结果：通过
  - 备注：确认当前分支对应 `PR #331`，本轮 latest-head open AI feedback 已收敛到 `dotnet pack --no-build`、共享包校验脚本跨平台兼容性与 active 文档 PR 锚点同步
- `python3 scripts/license-header.py --check`
  - 结果：通过
  - 备注：当前 WSL worktree 需要显式绑定 `GIT_DIR` / `GIT_WORK_TREE` 后运行
- `git diff --check`
  - 结果：通过
- `dotnet build GFramework.Cqrs.Benchmarks/GFramework.Cqrs.Benchmarks.csproj -c Release`
  - 结果：通过，`0 warning / 0 error`
- `dotnet run --project GFramework.Cqrs.Benchmarks/GFramework.Cqrs.Benchmarks.csproj -c Release -- --filter "*RequestLifetimeBenchmarks*" --job short --warmupCount 1 --iterationCount 1 --launchCount 1`
  - 结果：通过（以沙箱外 `--no-build` 权威结果为准）
  - 备注：`Singleton` 下 baseline / MediatR / GFramework 均值约 `5.633 ns / 58.687 ns / 301.731 ns`；`Transient` 下约 `5.044 ns / 52.274 ns / 287.863 ns`
- `python3 scripts/license-header.py --check`
  - 结果：通过
  - 备注：当前 WSL worktree 需要显式绑定 `GIT_DIR` / `GIT_WORK_TREE`
- `git diff --check`
  - 结果：通过

## 下一推荐步骤

1. 若继续沿用 `$gframework-batch-boot 50`，优先补 `stream handler` 生命周期矩阵，与当前 request 生命周期切片保持对称
2. 若要扩到 `Scoped` 生命周期，对 benchmark 宿主先补显式 scope 基线，而不是直接在根容器上解析 scoped handler
3. 若后续继续跑 BenchmarkDotNet，本地 agent 环境优先直接使用沙箱外命令，避免再次命中自动生成脚本在沙箱内的 bootstrap 异常

## 活跃文档

- 历史跟踪归档：[cqrs-rewrite-history-through-rp043.md](../archive/todos/cqrs-rewrite-history-through-rp043.md)
- 验证历史归档：[cqrs-rewrite-validation-history-through-rp062.md](../archive/todos/cqrs-rewrite-validation-history-through-rp062.md)
- `RP-063` 至 `RP-074` 验证归档：[cqrs-rewrite-validation-history-rp063-through-rp074.md](../archive/todos/cqrs-rewrite-validation-history-rp063-through-rp074.md)
- `RP-062` 至 `RP-076` trace 归档：[cqrs-rewrite-history-rp062-through-rp076.md](../archive/traces/cqrs-rewrite-history-rp062-through-rp076.md)
- CQRS 与 Mediator 评估归档：[cqrs-vs-mediator-assessment-rp063.md](../archive/todos/cqrs-vs-mediator-assessment-rp063.md)
- 历史 trace 归档：[cqrs-rewrite-history-through-rp043.md](../archive/traces/cqrs-rewrite-history-through-rp043.md)
- `RP-046` 至 `RP-061` trace 归档：[cqrs-rewrite-history-rp046-through-rp061.md](../archive/traces/cqrs-rewrite-history-rp046-through-rp061.md)

## 说明

- `PR #261`、`PR #302`、`PR #305`、`PR #307` 及更早阶段的详细过程已不再作为 active 恢复入口；如需追溯，以对应归档文件或历史 trace 段落为准
- active tracking 仅保留当前恢复点、当前风险、最近权威验证与下一推荐步骤，避免 `boot` 落到历史阶段细节
