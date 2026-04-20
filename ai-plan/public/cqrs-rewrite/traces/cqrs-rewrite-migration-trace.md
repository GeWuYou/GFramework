# CQRS 重写迁移追踪

## 2026-04-20

### 阶段：pointer / function pointer 泛型合同拒绝（CQRS-REWRITE-RP-050）

- 重新执行 `$gframework-pr-review` 后，确认当前分支对应 `PR #261`，状态仍为 `OPEN`
- latest reviewed commit 当前剩余 `1` 条 open CodeRabbit thread，指向 `RP-047` 历史记录仍把 `MakePointerType()` precise registration 写成现行路径
- 本地核对后确认该评论有效：当前 pointer / function pointer 语义已由 `RP-050` 收敛为 fallback / diagnostic 路径，历史追踪必须显式标注 `RP-047` 已废弃，避免后续恢复时误回滚到旧方案
- 已在 `GFramework.Cqrs.SourceGenerators/Cqrs/CqrsHandlerRegistryGenerator.cs` 中收紧 `TryCreateRuntimeTypeReference` 与 `CanReferenceFromGeneratedRegistry`
- pointer / function pointer 现统一视为不可精确生成的 CQRS 泛型合同，生成器会保守回退到既有 fallback / diagnostic 路径，而不再发射运行时 `MakeGenericType(...)` 风险代码
- 已在 `GFramework.SourceGenerators.Tests/Cqrs/CqrsHandlerRegistryGeneratorTests.cs` 中补充输入源诊断分离，并将相关测试改为显式断言 `CS0306` 与 fallback / diagnostic 结果
- 已同步修正 `ai-plan/public/cqrs-rewrite/traces/cqrs-rewrite-migration-trace.md` 中 `RP-047` 段落，明确其已被 `RP-050` 覆盖，且不得恢复 `MakePointerType()` precise registration
- 定向验证已通过：
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore -p:RestoreFallbackFolders= -m:1 -nodeReuse:false --filter "FullyQualifiedName~Reports_Compilation_Error_And_Skips_Precise_Registration_For_Hidden_Pointer_Response|FullyQualifiedName~Reports_Diagnostic_And_Skips_Registry_When_Fallback_Metadata_Is_Required_But_Runtime_Contract_Lacks_Fallback_Attribute|FullyQualifiedName~Emits_Assembly_Level_Fallback_Metadata_When_Fallback_Is_Required_And_Runtime_Contract_Is_Available"`
  - `3/3` passed
- 扩展验证已通过：
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore -p:RestoreFallbackFolders= -m:1 -nodeReuse:false --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests"`
  - `14/14` passed

### 阶段：registrar duplicate mapping 索引收敛（CQRS-REWRITE-RP-049）

- 已将 `CqrsHandlerRegistrar` 的重复 handler mapping 判定从逐条线性扫描 `IServiceCollection` 收敛为单次构建的本地映射索引
- reflection fallback 或重复类型输入场景下，后续 duplicate mapping 判定改为 `HashSet` 命中，不再重复遍历已有服务描述符
- `GFramework.Cqrs.Tests/Cqrs/CqrsHandlerRegistrarTests.cs` 已补充“程序集枚举返回重复 handler 类型时仍只注册一份映射”的回归
- 定向验证已通过：
  - `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --no-restore -p:RestoreFallbackFolders= -m:1 -nodeReuse:false --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsHandlerRegistrarTests"`
  - `11/11` passed
  - 当前沙箱限制 MSBuild named pipe，因此验证在提权环境下执行

### 阶段：registrar handler-interface 反射缓存（CQRS-REWRITE-RP-048）

- 已在 `CqrsHandlerRegistrar` 中新增按 `Type` 弱键缓存的 supported handler interface 元数据，reflection 注册路径现会复用已筛选且排序好的接口列表
- 同一 handler 类型跨容器重复注册时，不再重复执行 `GetInterfaces()` 与支持接口筛选；缓存仍保持卸载安全，不会长期钉住 collectible 类型
- `GFramework.Cqrs.Tests/Cqrs/CqrsHandlerRegistrarTests.cs` 已补充 registrar 静态缓存清理与 supported interface 缓存复用回归
- 定向验证已通过：
  - `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --no-restore -p:RestoreFallbackFolders= -m:1 -nodeReuse:false --filter "FullyQualifiedName~GFramework.Cqrs.Tests.Cqrs.CqrsHandlerRegistrarTests"`
  - `10/10` passed
  - 当前沙箱限制 MSBuild named pipe，因此验证在提权环境下执行

### 阶段：pointer precise runtime type 覆盖扩展（CQRS-REWRITE-RP-047，已由 RP-050 覆盖）

- 曾在 `CqrsHandlerRegistryGenerator` 中尝试补充 pointer 类型的 runtime type 递归建模与源码发射，计划通过 `MakePointerType()` 还原隐藏 pointer 响应类型
- 该方案后续已被 `RP-050` 明确废弃：pointer / function pointer 不能作为 CQRS 泛型合同的 precise registration 输入，当前实现统一回到 fallback / diagnostic 路径，不能恢复到 `MakePointerType()` 精确注册
- 已同步收紧 function pointer 签名的可直接生成判定，只有当签名中的返回值与参数类型均可从 generated registry 安全引用时才走静态注册
- 已保留含隐藏类型 function pointer handler 的 fallback / 诊断回归覆盖，确保 pointer 支持扩展不会误删原有程序集级 fallback 契约边界
- 后续若需恢复当前 pointer / function pointer 行为，应以 `RP-050` 为权威记录，而不是继续沿用本阶段的旧设计假设
- 定向验证与 `CqrsHandlerRegistryGeneratorTests` 全组验证均已通过：
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore -p:RestoreFallbackFolders= -m:1 -nodeReuse:false --filter "FullyQualifiedName~Generates_Precise_Service_Type_For_Hidden_Pointer_Response|FullyQualifiedName~Reports_Diagnostic_And_Skips_Registry_When_Fallback_Metadata_Is_Required_But_Runtime_Contract_Lacks_Fallback_Attribute|FullyQualifiedName~Emits_Assembly_Level_Fallback_Metadata_When_Fallback_Is_Required_And_Runtime_Contract_Is_Available"`
  - `3/3` passed
  - `dotnet test GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj -c Release --no-restore -p:RestoreFallbackFolders= -m:1 -nodeReuse:false --filter "FullyQualifiedName~CqrsHandlerRegistryGeneratorTests"`
  - `14/14` passed
  - 当前沙箱限制 MSBuild named pipe，因此验证在提权环境下执行

### 阶段：generated registry 激活反射收敛（CQRS-REWRITE-RP-046）

- 已在 `CqrsHandlerRegistrar` 中将 generated registry 的无参构造激活改为类型级缓存工厂
- 默认路径优先使用一次性动态方法直接创建 registry，避免后续每次命中缓存仍走 `ConstructorInfo.Invoke`
- 若运行环境不允许动态方法，则保留原有反射激活回退，确保 generated registry 路径不因运行时限制失效
- 已补充“私有无参构造 generated registry 仍可激活”的回归测试，覆盖现有生成器产物兼容性
- 定向验证已通过：
  - `dotnet test GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj -c Release --no-restore -p:RestoreFallbackFolders= -m:1 -nodeReuse:false`
  - `63/63` passed
  - 当前沙箱限制 MSBuild named pipe，因此验证在提权环境下执行

### Archive Context

- 历史跟踪归档：
  - `ai-plan/public/cqrs-rewrite/archive/todos/cqrs-rewrite-history-through-rp043.md`
- 历史 trace 归档：
  - `ai-plan/public/cqrs-rewrite/archive/traces/cqrs-rewrite-history-through-rp043.md`

### 当前下一步

1. 回到 `Phase 8` 主线，优先选一个明确的 dispatch / invoker 反射缩减点继续推进
2. 若继续文档主线，优先补齐 `docs/zh-CN/api-reference` 与教程入口页中仍过时的 CQRS API / 命名空间表述
3. 若后续 review thread 或 PR 状态再次变化，再重新执行 `$gframework-pr-review` 复核远端信号
