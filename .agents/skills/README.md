# GFramework Skills

公开入口目前包含 `gframework-doc-refresh` 与 `gframework-batch-boot`。

## 公开入口

### `gframework-doc-refresh`

按源码模块驱动文档刷新，而不是按 `guide`、`tutorial`、`api` 等类型拆入口。

适用场景：

- 刷新某个模块的 landing page
- 复核专题页是否与源码、测试、README 一致
- 评估是否需要补 API reference 或教程
- 在 adoption path 不清晰时引入 `ai-libs/` 消费者接法作为补充证据

推荐调用：

```bash
/gframework-doc-refresh <module>
```

示例：

```bash
/gframework-doc-refresh Core
/gframework-doc-refresh Godot.SourceGenerators
/gframework-doc-refresh Cqrs
```

### `gframework-batch-boot`

在 `gframework-boot` 的基础上，自动推进可分批执行的重复性任务，不需要人工一轮轮重新触发。

适用场景：

- analyzer warning reduction
- 大批量测试结构收口
- 分模块文档刷新 wave
- 任何有明确 stop condition 的多批次任务

推荐调用：

```bash
/gframework-batch-boot <task-or-stop-condition>
```

示例：

```bash
/gframework-batch-boot continue analyzer warning reduction until branch diff vs origin/main approaches 75 files
/gframework-batch-boot keep refactoring repetitive source-generator tests in bounded batches
```

## 共享资源

- `_shared/DOCUMENTATION_STANDARDS.md`
  - 统一的文档规则、证据顺序与验证要求
- `_shared/module-map.json`
  - 机器可读的模块映射表
- `_shared/module-config.sh`
  - 轻量 shell 辅助函数

## 内部资源

`gframework-doc-refresh/` 下包含：

- `references/`
  - 模块选择、证据顺序、输出策略
- `templates/`
  - landing page、专题页、API reference、教程模板
- `scripts/`
  - 模块扫描与文档验证脚本

旧 `vitepress-*` skills 不再作为并列公开入口保留。
