# VitePress 文档验证

验证 VitePress 文档的质量和规范性，确保文档符合项目标准。

## 用途

此 skill 用于验证 Markdown 文档的格式和内容，包括：
- Frontmatter 格式正确性
- 内部链接有效性
- 代码块语法标记
- 标题层级结构
- 中文标点符号规范
- 泛型符号转义

## 调用方式

```bash
# 验证单个文件
/vitepress-validate <文件路径>

# 验证整个目录
/vitepress-validate <目录路径>

# 验证所有文档
/vitepress-validate docs/zh-CN/
```

**示例**：
```bash
/vitepress-validate docs/zh-CN/api-reference/core/architecture.md
/vitepress-validate docs/zh-CN/core/
```

## 验证项

### 1. Frontmatter 验证

**检查项**：
- YAML 语法正确性
- 必需字段存在（`title`、`description`）
- 字段值类型正确
- `outline` 字段值有效（`deep`、`[2,3]` 等）

**示例**：
```yaml
---
title: Architecture  # 必需
description: 架构基类说明  # 必需
outline: deep  # 可选，但值必须有效
---
```

### 2. 内部链接验证

**检查项**：
- 相对路径链接指向的文件存在
- 绝对路径链接格式正确
- 锚点链接对应的标题存在
- 没有损坏的链接

**有效链接格式**：
- `[文本](./file.md)` - 相对路径
- `[文本](/zh-CN/core/architecture)` - 绝对路径
- `[文本](#标题)` - 锚点链接
- `[文本](./file.md#标题)` - 组合链接

### 3. 代码块验证

**检查项**：
- 代码块有语法标记（```csharp、```bash 等）
- C# 代码块使用 `csharp` 标记（不是 `cs` 或 `c#`）
- 代码块正确闭合
- 没有未闭合的反引号

**正确格式**：
```markdown
\`\`\`csharp
public class Example { }
\`\`\`
```

**错误格式**：
```markdown
\`\`\`cs  // 应该使用 csharp
public class Example { }
\`\`\`
```

### 4. 标题层级验证

**检查项**：
- 标题层级不跳级（不能从 `#` 直接跳到 `###`）
- 每个文档只有一个一级标题（`#`）
- 标题层级递增合理

**正确示例**：
```markdown
# 一级标题
## 二级标题
### 三级标题
## 另一个二级标题
```

**错误示例**：
```markdown
# 一级标题
### 三级标题  ❌ 跳过了二级标题
```

### 5. 中文标点符号验证

**检查项**：
- 中文句子使用全角标点（，。！？）
- 英文句子使用半角标点（,.!?）
- 代码和技术术语周围使用半角符号
- 括号使用规范

**规范示例**：
- "这是一个示例。" ✓（中文全角句号）
- "This is an example." ✓（英文半角句号）
- "`Architecture` 类提供了..." ✓（代码周围半角）

### 6. 泛型符号验证

**检查项**：
- 泛型符号正确转义（`<T>` → `&lt;T&gt;`）
- 仅在代码块外转义
- 代码块内保持原样

**正确示例**：
```markdown
`List&lt;T&gt;` 是一个泛型类。

\`\`\`csharp
List<T> items = new List<T>();  // 代码块内不转义
\`\`\`
```

## 验证脚本

### validate-frontmatter.sh

验证 Frontmatter 格式。

**用法**：
```bash
.claude/skills/vitepress-validate/scripts/validate-frontmatter.sh <文件路径>
```

### validate-links.sh

验证内部链接有效性。

**用法**：
```bash
.claude/skills/vitepress-validate/scripts/validate-links.sh <文件路径>
```

### validate-code-blocks.sh

验证代码块语法。

**用法**：
```bash
.claude/skills/vitepress-validate/scripts/validate-code-blocks.sh <文件路径>
```

### validate-all.sh

执行所有验证。

**用法**：
```bash
.claude/skills/vitepress-validate/scripts/validate-all.sh <文件或目录路径>
```

## 输出格式

### 验证通过

```
✓ docs/zh-CN/core/architecture.md
  - Frontmatter: 通过
  - 内部链接: 通过
  - 代码块: 通过
  - 标题层级: 通过
  - 标点符号: 通过
  - 泛型符号: 通过
```

### 验证失败

```
✗ docs/zh-CN/core/architecture.md
  - Frontmatter: 失败
    × 缺少必需字段: description
  - 内部链接: 失败
    × 损坏的链接: ./missing-file.md (第 45 行)
  - 代码块: 警告
    ⚠ 使用了 'cs' 标记，建议使用 'csharp' (第 78 行)
  - 标题层级: 通过
  - 标点符号: 警告
    ⚠ 中文句子使用了半角句号 (第 102 行)
  - 泛型符号: 失败
    × 未转义的泛型符号: List<T> (第 120 行)
```

## 修复建议

验证失败时，skill 会提供具体的修复建议：

**示例**：
```
修复建议:
1. 在 Frontmatter 中添加 description 字段
2. 修复或删除损坏的链接: ./missing-file.md
3. 将代码块标记从 'cs' 改为 'csharp'
4. 将第 102 行的半角句号改为全角句号
5. 将第 120 行的 List<T> 改为 List&lt;T&gt;
```

## 配置选项

### 严格模式

启用严格模式时，警告也会导致验证失败。

```bash
/vitepress-validate --strict docs/zh-CN/
```

### 忽略特定检查

```bash
# 忽略标点符号检查
/vitepress-validate --ignore-punctuation docs/zh-CN/

# 忽略多个检查
/vitepress-validate --ignore-punctuation --ignore-generics docs/zh-CN/
```

## 集成到工作流

### 生成后自动验证

```bash
# 1. 生成 API 文档
/vitepress-api-doc GFramework.Core/architecture/Architecture.cs

# 2. 自动验证生成的文档
/vitepress-validate docs/zh-CN/api-reference/core/architecture.md
```

### 批量验证

```bash
# 验证所有 API 文档
/vitepress-validate docs/zh-CN/api-reference/

# 验证所有文档
/vitepress-validate docs/zh-CN/
```

## 退出代码

- `0` - 所有验证通过
- `1` - 存在错误
- `2` - 仅存在警告（非严格模式下仍返回 0）

## 相关 Skills

- `/vitepress-api-doc` - 生成 API 文档后自动验证
- `/vitepress-guide` - 生成指南文档后自动验证
- `/vitepress-tutorial` - 生成教程文档后自动验证

## 最佳实践

1. **生成后立即验证**：每次生成文档后立即运行验证
2. **定期批量验证**：定期验证所有文档，确保一致性
3. **修复所有错误**：不要忽略验证错误，及时修复
4. **关注警告**：警告虽不致命，但应该重视并修复
5. **使用严格模式**：在 CI/CD 中使用严格模式确保质量

## 故障排除

### 问题：误报泛型符号错误
**解决方案**：确保泛型符号在代码块外正确转义，代码块内保持原样

### 问题：中文标点符号检查过于严格
**解决方案**：使用 `--ignore-punctuation` 选项，或手动调整规则

### 问题：链接验证失败但文件确实存在
**解决方案**：检查文件路径大小写，确保路径完全匹配

## 版本历史

- v1.0.0 - 初始版本，支持 6 项基本验证
