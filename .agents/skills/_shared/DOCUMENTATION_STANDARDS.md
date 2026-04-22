# GFramework 文档编写规范

本文件只保留跨模块稳定生效的写作与校验规则，不再维护容易失真的固定页面清单。

模块到源码、测试、README、`docs/zh-CN` 栏目以及 `ai-libs/` 参考入口的映射，统一以
`.agents/skills/_shared/module-map.json` 为准。

## 证据顺序

统一按以下顺序判断文档应写什么、删什么、保留什么：

1. 源码、公开 XML docs、`*.csproj`
2. 对应测试和 snapshot
3. 模块 `README.md`
4. 当前 `docs/zh-CN` 页面
5. `ai-libs/` 下已验证的消费者项目
6. 归档文档，仅在前述证据无法解释当前行为时回看

不要把旧文档互相抄写当成“更新”。

## 模块驱动规则

- 先按源码模块归一化输入，再决定落到 landing page、专题页、API reference、教程还是仅做校验。
- 如果用户给的是栏目名而不是源码模块名，先映射回模块；若仍有歧义，只给归一化建议，不直接生成文档。
- 文档栏目是派生输出，不是主输入源。

## `ai-libs/` 使用边界

`ai-libs/` 只用于补消费者视角证据：

- 验证真实接入目录结构
- 查最小 wiring、扩展点装配方式
- 给 adoption path 提供端到端例子

不要用 `ai-libs/` 覆盖以下事实：

- 公共 API 契约
- 当前版本支持范围
- Source Generator 诊断与生成语义

如果 `ai-libs/` 与当前源码或测试冲突，以当前仓库实现为准，并在文档里写明迁移或兼容边界。

## Markdown 规则

### 泛型与 HTML 转义

代码块外出现泛型或 XML 标签时必须转义：

- `List&lt;T&gt;`
- `Result&lt;TValue, TError&gt;`
- `&lt;summary&gt;`
- `&lt;param&gt;`

### Frontmatter

每个文档都必须包含合法 frontmatter：

```yaml
---
title: 文档标题
description: 1-2 句话描述当前页面解决什么问题
---
```

### 代码块

- 始终标注语言，如 `csharp`、`bash`、`json`
- 示例只保留当前实现可追溯的最小路径
- 必要时写中文注释解释接入原因或边界，不要堆砌与代码同步无关的注释

### 链接

- 只链接到当前仓库真实存在的页面
- 站内链接优先使用 `/zh-CN/...` 形式
- 如果文档站不允许跳出 `docs/` 根目录，就不要把仓库 README 写成站内链接

## 输出优先级

统一按以下顺序决定产出：

1. 先修模块 README、landing page 与 adoption path
2. 再修失真的专题页
3. 再补 API reference
4. 最后才补教程

## 验证清单

- [ ] frontmatter 正确
- [ ] 代码块语言标记齐全
- [ ] 泛型和 XML 标签已转义
- [ ] 站内链接存在
- [ ] 示例与当前实现一致
- [ ] `ai-libs/` 只作为消费者接入参考，没有覆盖源码契约

## 验证工具

统一复用 `gframework-doc-refresh/scripts/` 下的校验脚本：

- `validate-frontmatter.sh`
- `validate-links.sh`
- `validate-code-blocks.sh`
- `validate-all.sh`

需要站点级验证时，执行：

```bash
cd docs && bun run build
```
