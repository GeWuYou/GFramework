# VitePress API 文档生成

为单个 C# 类、接口或枚举生成符合 VitePress 标准的 API 参考文档。

## 用途

此 skill 用于从 C# 源代码文件自动生成结构化的 API 文档，包括：
- 类型概述和命名空间信息
- 构造函数、方法、属性的详细说明
- 基于 XML 文档注释的描述
- 自动生成的使用示例
- 相关类型的交叉引用

## 调用方式

```bash
/vitepress-api-doc <C# 文件路径>
```

**示例**：
```bash
/vitepress-api-doc GFramework.Core/architecture/Architecture.cs
```

## 工作流程

1. **读取源代码文件**
   - 验证文件存在且为 C# 文件
   - 读取完整的源代码内容

2. **解析代码结构**
   - 提取命名空间、类名、访问修饰符
   - 识别类型（class/interface/enum/struct）
   - 解析继承关系和实现的接口
   - 提取所有公共成员（构造函数、方法、属性、事件、字段）

3. **提取 XML 文档注释**
   - 解析 `/// <summary>` 标签（类型和成员描述）
   - 解析 `/// <param>` 标签（参数说明）
   - 解析 `/// <returns>` 标签（返回值说明）
   - 解析 `/// <exception>` 标签（异常说明）
   - 解析 `/// <example>` 标签（示例代码）
   - 解析 `/// <see cref=""/>` 标签（交叉引用）

4. **生成 Markdown 文档**
   - 根据 `template.md` 填充内容
   - 转义泛型符号（`<T>` → `&lt;T&gt;`）
   - 生成使用示例（基于 API 签名）
   - 添加相关文档链接

5. **确定输出路径**
   - 根据命名空间确定模块（Core/Game/Godot/SourceGenerators）
   - 输出到 `docs/zh-CN/api-reference/<模块>/<类名>.md`

6. **更新 VitePress 配置**
   - 调用共享脚本 `update-vitepress-nav.sh`
   - 在侧边栏配置中添加新文档条目

7. **验证文档质量**
   - 检查 Frontmatter 格式
   - 验证内部链接
   - 确保代码块语法正确

## 输出规范

### Frontmatter 格式

```yaml
---
title: 类名
description: 从 XML <summary> 提取的简短描述
outline: deep
---
```

### 文档结构

1. **标题**：使用类名作为一级标题
2. **概述**：XML summary 内容
3. **命名空间和程序集信息**
4. **继承链**（如果适用）
5. **构造函数**（如果有）
6. **公共方法**（按字母顺序）
7. **公共属性**（按字母顺序）
8. **公共事件**（如果有）
9. **使用示例**（自动生成）
10. **另请参阅**（相关类型链接）

### 代码块格式

所有 C# 代码块必须使用：
```markdown
\`\`\`csharp
// 代码内容
\`\`\`
```

### 泛型符号转义

- `List<T>` → `List&lt;T&gt;`
- `Dictionary<K, V>` → `Dictionary&lt;K, V&gt;`
- `IEnumerable<T>` → `IEnumerable&lt;T&gt;`

### 内部链接格式

- 相对路径：`[Architecture](./architecture.md)`
- 绝对路径：`[Core 架构](/zh-CN/core/architecture)`
- 锚点链接：`[构造函数](#构造函数)`

## 前置条件

1. 项目必须有 VitePress 配置文件（`docs/.vitepress/config.mts`）
2. 目标 C# 文件必须存在且可读
3. C# 文件必须包含 XML 文档注释（`///`）
4. 文件必须包含至少一个公共类型

## 配置选项

### 自动检测模块

根据命名空间自动确定模块：
- `GFramework.Core.*` → `core`
- `GFramework.Game.*` → `game`
- `GFramework.Godot.*` → `godot`
- `GFramework.SourceGenerators.*` → `source-generators`

### 示例生成策略

- **基本用法**：最简单的 API 调用
- **常见场景**：实际应用案例
- **高级用法**：复杂配置（如果适用）

## 示例输出

参考 `examples/` 目录中的示例文档：
- `class-example.md` - 类文档示例
- `interface-example.md` - 接口文档示例
- `enum-example.md` - 枚举文档示例

## 注意事项

1. **仅使用 XML 注释**：不对缺失的注释进行 AI 补充
2. **仅提取公共成员**：忽略 `internal`、`private`、`protected` 成员
3. **保持文档同步**：文档内容直接来源于代码，确保准确性
4. **遵循项目风格**：参考现有文档的格式和术语

## 相关 Skills

- `/vitepress-validate` - 验证生成的文档质量
- `/vitepress-batch-api` - 批量生成整个模块的 API 文档

## 技术细节

### XML 注释标签映射

| XML 标签 | Markdown 输出 |
|---------|--------------|
| `<summary>` | 概述章节 |
| `<param name="x">` | 参数列表 |
| `<returns>` | 返回值说明 |
| `<exception cref="T">` | 异常列表 |
| `<example>` | 示例代码块 |
| `<see cref="T"/>` | 内部链接 |
| `<remarks>` | 备注章节 |

### 成员签名格式

**方法**：
```markdown
### MethodName

描述内容

**签名**：
\`\`\`csharp
public ReturnType MethodName(ParamType param)
\`\`\`

**参数**：
- `param` (ParamType): 参数说明

**返回值**：
- (ReturnType): 返回值说明
```

**属性**：
```markdown
### PropertyName

描述内容

**类型**：`PropertyType`

**访问**：get / set
```

## 故障排除

### 问题：找不到 XML 注释
**解决方案**：确保 C# 文件包含 `///` 注释，而不是 `//` 或 `/* */`

### 问题：泛型符号显示错误
**解决方案**：VitePress 配置中已包含 `safeGenericEscapePlugin`，确保正确转义

### 问题：侧边栏未更新
**解决方案**：检查 `update-vitepress-nav.sh` 脚本是否正确执行

## 版本历史

- v1.0.0 - 初始版本，支持类、接口、枚举的文档生成
