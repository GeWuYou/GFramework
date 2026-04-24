# Analyzer Warning Reduction 追踪

## 2026-04-24 — RP-046

### 阶段：solution warning baseline 采样与 active plan 回写

- 触发背景：
  - 用户纠正本轮目标为“执行 `dotnet build` 收集当前项目 warning，并更新当前工作树激活计划”
  - active topic 仍为 `analyzer-warning-reduction`，因此本轮需要先确认 solution 级 warning baseline 是否可直接从当前工作树获取
- 主线程实施：
  - 直接执行前台 `dotnet build GFramework.sln -c Release`
  - 构建成功，得到 `891 Warning(s)` / `0 Error(s)` / `Time Elapsed 00:00:18.57`
  - 从实时输出中确认 warning 热点主要集中在 `GFramework.Godot.SourceGenerators`、`GFramework.Godot.SourceGenerators.Tests`、`GFramework.Core`、`GFramework.Game`、`GFramework.Cqrs`、`GFramework.Godot`
  - 从实时输出中确认规则热点以 `MA0051`、`MA0158`、`MA0004` 为主，并伴随 `MA0006`、`MA0002`、`MA0009`
  - 追加尝试把同一命令改为“重定向到日志文件”“附加 file logger”“`script` 分配 TTY”几种采集方式；这些路径都未稳定复现前台结果，而是出现 `Build FAILED / 0 Warning(s) / 0 Error(s)` 或 `Restore failed`
  - 基于上述差异，本轮把“前台普通构建”视为 warning baseline 真值来源，并把采集形态漂移记录为环境风险
- 本轮验证结果：
  - `dotnet build GFramework.sln -c Release`
    - 结果：成功；`891 Warning(s)`、`0 Error(s)`
  - `dotnet build GFramework.sln -c Release > /tmp/gframework-build-warnings.log 2>&1`
    - 结果：失败；约 `0.78s` 结束，摘要仅显示 `Build FAILED / 0 Warning(s) / 0 Error(s)`
  - `dotnet build GFramework.sln -c Release '/flp:logfile=/tmp/gframework-build-warnings.log;verbosity=normal'`
    - 结果：成功；但日志文件只保留了构建摘要，没有留下 warning 行
  - `dotnet build GFramework.sln -c Release '/flp1:logfile=/tmp/gframework-build-warnings-only.log;warningsonly'`
    - 结果：成功；但 warning-only 日志文件为空
  - `script -q -c "dotnet build GFramework.sln -c Release" /tmp/gframework-build-full-typescript.log`
    - 结果：失败；TTY 形态下 restore 于约 `0.8s` 退出
- 当前结论：
  - 当前工作树的 solution 级 warning baseline 可以通过普通前台 `dotnet build` 获取，且样本值是 `891` 条 warning、`0` 条 error
  - 当前环境对 stdout/TTY/logger 形态敏感，不能把“把输出落到文件”的结果直接当作同等可信的构建事实
  - 下一轮 warning reduction 应以本轮前台 baseline 为准，而不是继续围绕空日志或快速失败结果做误判

## 2026-04-24 — RP-045

### 阶段：solution no-restore 阻塞面采样与 active plan 回写

- 触发背景：
  - 用户要求显式执行 `dotnet build GFramework.sln -c Release --no-restore`，收集当前项目报错并同步更新当前工作树激活计划
  - active topic 仍为 `analyzer-warning-reduction`，因此本轮的核心工作是把 solution 级失败面与先前的 restore / warning 线索重新归并到同一个恢复点
- 主线程实施：
  - 先执行 `dotnet build GFramework.sln -c Release --no-restore`，发现命令约 `1` 秒即失败，标准摘要只有 `Build FAILED / 0 Warning(s) / 0 Error(s)`
  - 补跑 `dotnet build GFramework.sln -c Release --no-restore -v:diag`，确认 solution 在根 `GFramework.csproj` 的 inner-build dispatch 阶段退出，没有进入各子项目编译
  - 继续把根项目拆成 `net8.0`、`net9.0`、`net10.0` 三个 `--no-restore` 构建，全部稳定复现同一条 `MSB4018`
  - 读取根项目 `obj/project.assets.json`，确认当前资产文件记录了 Windows restore 元数据与不存在的 fallback package folder
  - 按用户追加要求执行默认 `dotnet build` 与 `dotnet build -v:diag`，确认它不是落在相同失败层，而是更早停在 solution restore 图生成阶段
- 本轮验证结果：
  - `dotnet build GFramework.sln -c Release --no-restore`
    - 结果：失败；仅有失败摘要，没有暴露真实阻塞点
  - `dotnet build GFramework.sln -c Release --no-restore -v:diag`
    - 结果：失败；失败位置收敛到根 `GFramework.csproj`
  - `dotnet build`
    - 结果：失败；同样约 `1` 秒退出，摘要仍只有 `0 Warning(s) / 0 Error(s)`
  - `dotnet build -v:diag`
    - 结果：失败；停在 `GFramework.sln` 的 `Restore` 路径 `_FilterRestoreGraphProjectInputItems`
    - 补充：具体落点是根 `GFramework.csproj` 的 `_IsProjectRestoreSupported`，日志记录 `MSB4276`，默认 SDK resolver 找不到 `Microsoft.NET.SDK.WorkloadAutoImportPropsLocator`
  - `dotnet build GFramework.csproj -c Release -f net8.0 --no-restore`
    - 结果：失败；`MSB4018`，`ResolvePackageAssets` 因缺失 `D:\Tool\Development Tools\Microsoft Visual Studio\Shared\NuGetPackages` 退出
  - `dotnet build GFramework.csproj -c Release -f net9.0 --no-restore`
    - 结果：失败；与 `net8.0` 相同
  - `dotnet build GFramework.csproj -c Release -f net10.0 --no-restore`
    - 结果：失败；与 `net8.0` 相同
- 当前结论：
  - 默认 `dotnet build` 与 `dotnet build GFramework.sln -c Release --no-restore` 失败，但二者不是同一层错误；前者先死在 restore 图阶段，后者死在资产解析阶段
  - 当前 solution 级 `--no-restore` 阻塞不是代码编译错误，而是根项目资产文件引用了当前 WSL 不存在的 Windows fallback package folder
  - 当前 restore 路径还额外暴露出 SDK / workload resolver 环境问题，因此仅仅重建资产文件还不足以恢复默认 `dotnet build`
  - 这一层阻塞比先前记录的 `NU1301` 更靠前，因为它会让 `--no-restore` 构建在读取资产阶段直接退出
  - `Meziantou.Polyfill 1.0.116` 缺失 / `NU1301` 仍然是 restore 路径的独立风险；修复资产文件后仍需继续处理
  - active tracking 已升级到 `RP-045`，下一轮恢复应先重建与当前环境一致的根项目资产文件，再回测 solution `--no-restore`

## Archive Context

- 历史跟踪归档：
  - [analyzer-warning-reduction-history-rp001.md](../archive/todos/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/todos/analyzer-warning-reduction-history-rp002-rp041.md)
- 历史 trace 归档：
  - [analyzer-warning-reduction-history-rp001.md](../archive/traces/analyzer-warning-reduction-history-rp001.md)
  - [analyzer-warning-reduction-history-rp002-rp041.md](../archive/traces/analyzer-warning-reduction-history-rp002-rp041.md)
