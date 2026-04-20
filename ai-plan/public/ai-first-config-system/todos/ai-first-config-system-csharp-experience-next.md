# AI-First Config System C# 体验下一阶段清单

## 目标

继续把主线放在 `C# Runtime + Source Generator + Consumer DX`，以“新配置域接入成本是否足够低”为第一判断标准。

当前阶段不再把 VS Code 工具能力当作阻塞项；工具链只要不拖累 C# 首发可用版本即可。

## 当前状态

- [x] 单表注册辅助：`Register{Entity}Table()`
- [x] 强类型访问入口：`Get{Entity}Table()` / `TryGet{Entity}Table(...)`
- [x] 结构化加载诊断：`ConfigLoadException.Diagnostic`
- [x] 端到端消费者集成测试
- [x] 顶层非主键标量字段查询辅助：`FindBy*` / `TryFindFirstBy*`
- [x] `Architecture` 推荐接入模板
- [x] 项目级聚合注册入口：`RegisterAllGeneratedConfigTables()`
- [x] 项目级生成目录：`GeneratedConfigCatalog`
- [x] 项目级目录筛选 / 启动诊断辅助：`GetTablesInConfigDomain()` / `GetTablesForRegistration()`
- [x] 聚合注册 comparer 覆盖：`GeneratedConfigRegistrationOptions`
- [x] 官方 C# 启动帮助器：`GameConfigBootstrap` / `GameConfigBootstrapOptions`
- [x] 可选只读精确匹配索引：`x-gframework-index`

## P0：下一轮优先做

- [x] 为聚合注册入口增加“按配置域过滤 / 分组注册”能力
  - 目标：大型项目不必在所有场景都一次性注册全部 schema
  - 示例方向：按 `ConfigDomain`、表名集合或调用方谓词选择子集
  - 价值：这是聚合注册落地后的下一步，直接影响多模块项目的启动颗粒度

- [x] 提供官方 C# 启动帮助器
  - 目标：把 `ConfigRegistry + YamlConfigLoader + LoadAsync + 热重载句柄` 收敛成更稳定的框架入口
  - 示例方向：`GameConfigBootstrap` 的框架内版本，或轻量 runtime host / installer
  - 价值：把当前“文档模板”升级为可复用实现，继续减少消费者样板

## P1：强烈建议尽快补齐

- [x] 继续扩展最有价值的 JSON Schema 子集
  - 原则：只做 Runtime / Generator / Tooling 三端都能稳定解释的关键字
  - 已补齐：`enum`（当前覆盖标量、对象、数组节点，以及标量数组元素）、`const`、`not`、`pattern`、`format`（当前稳定子集：`date`、`date-time`、`duration`、`email`、`time`、`uri`、`uuid`）、`minItems`、`maxItems`、`exclusiveMinimum`、`exclusiveMaximum`、`multipleOf`、`uniqueItems`、`minProperties`、`maxProperties`、`dependentRequired`、`dependentSchemas`、`allOf`、object-focused `if` / `then` / `else`
  - 当前产出：运行时拒绝相关约束违规值，VS Code 校验与表单 hint 对齐，生成代码 XML 文档同步暴露新关键字；对象 / 数组 `enum` 当前主要参与校验与文档输出，不额外扩展复杂表单控件；`allOf` 与 `if` / `then` / `else` 当前都收敛为 object-focused constraint block，不做属性合并

- [x] 评估可选只读索引能力
  - 目标：为高频查询字段提供比 `All()` 线性扫描更强的读取体验
  - 约束：不能破坏当前热重载与简单运行时契约，也不能强迫所有表都引入额外索引成本
  - 当前产出：通过 schema 元数据 `x-gframework-index: true` 为“顶层、必填、非主键、非引用标量字段”生成惰性只读精确匹配索引，未声明字段保持线性扫描

- [x] 用 `GeneratedConfigCatalog` 继续补齐启动与诊断辅助
  - 目标：让消费者可以稳定枚举已生成表、按表名反查元数据，并为后续分组注册做铺垫
  - 当前产出：补齐 `GetTablesInConfigDomain()`、`GetTablesForRegistration()` 与 `MatchesRegistrationOptions(...)`，让启动日志和真实聚合注册复用同一套筛选规则

## P2：可选增强

- [x] 补一条比 `Architecture.OnInitialize()` 更正式的模块化接入建议
  - 当前产出：`GFramework.Game.Config.GameConfigModule`
  - 生命周期：模块安装时注册 `IConfigRegistry` utility，并在 `BeforeUtilityInit` 通过 lifecycle hook 完成首次加载
  - 清理策略：通过内部 context utility 跟随架构销毁自动释放 `GameConfigBootstrap` 和热重载句柄
  - 适用边界：`Architecture` 宿主优先使用模块；非 `Architecture` 场景继续直接使用 `GameConfigBootstrap`

- [ ] 继续扩插件的复杂表单能力
  - 说明：这是可选项，不阻塞 C# 主线

## 暂缓

- [ ] 不追求完整 JSON Schema 全量支持
  - 原因：维护成本高，且容易造成 Runtime / Generator / Tooling 三端漂移

- [ ] 不优先做运行时可写配置
  - 原因：当前系统定位仍然是静态内容只读查询

- [ ] 不让 VS Code 扩展计划反过来主导 Runtime / Generator 设计
  - 原因：当前更大的收益点仍然在 C# 消费体验

## 建议执行顺序

1. 用 `GeneratedConfigCatalog` 继续补齐启动与诊断辅助
2. 补一条比 `Architecture.OnInitialize()` 更正式的模块化接入建议
   当前状态：第 1 项和第 2 项已完成，`allOf` 与 object-focused `if` / `then` / `else` 也已补齐；下一步转到下一批仍不改变生成形状的组合关键字评估，或继续推进 VS Code 复杂编辑体验

## 完成标准

- 消费项目接入多个配置域时，启动代码仍然保持很薄
- 消费者不需要手写重复的注册字符串、目录路径和 schema 路径
- 配置系统能以“官方入口”而不是“文档拼装模板”接入真实项目
- 新增 schema 后，回归测试能覆盖生成、加载、访问与聚合注册链路

## 下次恢复点

- 在当前稳定 `format` 子集（`date`、`date-time`、`duration`、`email`、`time`、`uri`、`uuid`）、object-focused `allOf` 与 object-focused `if` / `then` / `else` 之后，转到下一批仍不改变生成类型形状的关键字评估；仍然不要先回工具 UI
- 恢复时优先检查：
  - `GFramework.Game/Config/YamlConfigSchemaValidator.cs`
  - `GFramework.Game.SourceGenerators/Config/SchemaConfigGenerator.cs`
  - `tools/gframework-config-tool/src/configValidation.js`
  - `tools/gframework-config-tool/src/extension.js`
  - `docs/zh-CN/game/config-system.md`

### 恢复块

- 恢复点编号：`AI-FIRST-CONFIG-RP-003`
- 当前阶段：`C# Runtime + Source Generator + Consumer DX`
- 已知风险：
  - 复杂关键字形状风险：下一批候选关键字若像标准 `oneOf` / `anyOf` 那样影响对象分支形状，可能破坏当前生成契约
  - 工具链非阻塞风险：将 VS Code 功能标为非阻塞后，可能导致 C# 主线补齐新关键字时缺少工具侧同步验证
  - 组合关键字范围风险：`allOf` 与 `if` / `then` / `else` 已收敛为 object-focused constraint block，未来新增组合关键字时需明确是否同样限制范围
- 最近验证：
  - 时间：2026-04-20
  - 内容：`bun run test`、`SchemaConfigGeneratorTests`、`YamlConfigLoaderIfThenElseTests`
  - 结果：通过
- 下一步：
  1. 检查 `YamlConfigSchemaValidator.cs`、`SchemaConfigGenerator.cs`、`configValidation.js` 中当前已支持的关键字列表
  2. 评估 `oneOf` / `anyOf` 是否存在可接受的 object-focused 子集
  3. 若结论否定，选择下一批共享解释关键字而不是先回工具 UI
