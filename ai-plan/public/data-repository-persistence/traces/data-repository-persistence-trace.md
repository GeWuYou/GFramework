# Data Repository Persistence 追踪

## 2026-04-19

### 阶段：legacy local-plan 迁移建档（RP-001）

- 复核当前工作树后确认：根目录 `local-plan/` 只剩一份
  `settings-persistence-serialization-tracking.md`，它同时承担 todo 与 trace 角色
- 按 `ai-plan` 治理规则建立 `ai-plan/public/data-repository-persistence/` 主题目录，并补齐：
  - `todos/`
  - `traces/`
  - `archive/todos/`
  - `archive/traces/`
- 将旧混合文件拆分为主题内历史跟踪归档与基于同一材料整理的历史 trace，active 入口只保留当前恢复点、
  活跃事实、风险与下一步
- 在 `ai-plan/public/README.md` 中建立
  `feat/data-repository-persistence` -> `data-repository-persistence` 的 worktree 映射
- 同步更新 `ai-plan-governance` 的 tracking / trace，记录 legacy 单文件计划也已按新目录语义收口

### Archive Context

- 历史跟踪归档：
  - `ai-plan/public/data-repository-persistence/archive/todos/data-repository-persistence-history-pre-rp001.md`
- 历史 trace 归档：
  - `ai-plan/public/data-repository-persistence/archive/traces/data-repository-persistence-history-pre-rp001.md`

### 下一步

1. 后续继续该主题时，只从 `ai-plan/public/data-repository-persistence/` 进入，不再恢复 `local-plan/`
2. 若 active 入口再次积累多轮已完成且已验证阶段，继续按同一模式迁入该主题自己的 `archive/`

## 2026-04-20

### 阶段：JsonSerializer 配置契约补充（RP-001）

- 复核 `GFramework.Game/Serializer/JsonSerializer.cs` 后确认：当前实现直接复用传入的 `JsonSerializerSettings`，并通过 `Settings` / `Converters` 暴露活动配置对象
- 复核 `docs/zh-CN/game/serialization.md` 后确认：现有 FAQ 把 `JsonSerializer` 写成“本身线程安全”，与当前可变配置契约不一致
- 决定本轮只补齐契约说明而不改变运行时行为：
  - 在源码 XML docs 中说明 settings / converters 的生命周期与并发约束
  - 在定向单测中固定“序列化器暴露活动配置实例”的当前契约
  - 在 `docs/zh-CN/game/serialization.md`、`docs/zh-CN/game/index.md` 与 `GFramework.Game/README.md` 中同步修正接入建议

### 下一步

1. `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~JsonSerializerTests"` 已通过（9/9）
2. 验证过程中出现的 analyzer warning 为仓库既有 warning，未在本轮扩大
3. 下一步回到 migration abstraction 与 codec / persistence pipeline 的后续评估

### 阶段：迁移执行器统一收敛（RP-001）

- 对 `SettingsModel`、`DataRepository`、`UnifiedSettingsDataRepository`、`SaveRepository<T>` 的实现进行并排核对后确认：
  - `DataRepository` 与 `UnifiedSettingsDataRepository` 不直接承担按版本号推进的迁移链
  - 实际重复点只在 `SettingsModel` 与 `SaveRepository<T>` 的“版本迁移链执行与校验”逻辑
- 决定不新增 public migration abstraction，而是抽出 internal `VersionedMigrationRunner`
  - 统一前向注册校验
  - 统一缺链失败
  - 统一声明目标版本与实际结果版本一致性校验
  - 统一非递增 / 超目标版本防护
- `SettingsModel` 本轮额外补强：
  - 拒绝同一设置类型同一 `FromVersion` 的重复注册
  - 以当前内存设置实例的 `Version` 作为目标运行时版本
  - 迁移失败时保持当前实例不被旧数据覆盖，并继续记录错误日志
- `SaveRepository<T>` 改为复用同一个 internal runner，但保留“加载成功后自动回写升级结果”的现有仓库语义
- 同步更新 `docs/zh-CN/game/setting.md` 与 `docs/zh-CN/game/data.md`，补迁移链约束说明
- 新增 / 更新测试：
  - `SettingsModelTests`：重复注册拒绝、不完整链路保持当前实例、缓存失效场景
  - `PersistenceTests`：迁移结果版本与声明版本不一致时显式失败

### 验证

1. `dotnet test GFramework.Game.Tests/GFramework.Game.Tests.csproj -c Release --filter "FullyQualifiedName~SettingsModelTests|FullyQualifiedName~PersistenceTests"` 已通过（20/20）
2. 过程中出现的 analyzer warning 来自仓库既有项，未在本轮扩大

### 下一步

1. 进入 codec / persistence pipeline 边界评估
2. 重点查看压缩、加密、元数据、备份是否仍然跨越 `Serializer` / `Storage` / `Repository` 多层分散
