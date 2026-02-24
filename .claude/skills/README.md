# VitePress 文档生成 Skills 系统

为 GFramework 项目提供自动化的 VitePress 文档生成能力。

## 概述

这是一套专门为 GFramework 项目设计的文档生成 skills，能够根据 C# 源代码自动生成高质量的 VitePress 文档。系统采用模块化设计，每个 skill 专注于特定的文档生成任务。

## 可用 Skills

### 1. vitepress-api-doc - API 文档生成

为单个 C# 文件生成 API 参考文档。

**用途**：
- 类、接口、枚举的 API 文档
- 方法、属性、事件的详细说明
- 基于 XML 注释生成文档

**调用方式**：
```bash
/vitepress-api-doc <C# 文件路径>
```

**示例**：
```bash
/vitepress-api-doc GFramework.Core/architecture/Architecture.cs
```

**输出位置**：`docs/zh-CN/api-reference/<模块>/<文件名>.md`

[详细文档](./vitepress-api-doc/SKILL.md)

---

### 2. vitepress-guide - 功能指南生成

生成功能模块的使用指南文档。

**用途**：
- 核心功能模块的使用说明
- 设计模式和架构概念
- 最佳实践和常见问题

**调用方式**：
```bash
/vitepress-guide <主题> <目标模块>
```

**示例**：
```bash
/vitepress-guide "事件系统" Core
/vitepress-guide "IoC 容器" Core
```

**输出位置**：`docs/zh-CN/<模块>/<主题>.md`

[详细文档](./vitepress-guide/SKILL.md)

---

### 3. vitepress-tutorial - 分步教程生成

生成分步教程文档，适合初学者学习。

**用途**：
- 框架入门教程
- 功能实现教程
- 问题解决方案

**调用方式**：
```bash
/vitepress-tutorial <教程主题>
```

**示例**：
```bash
/vitepress-tutorial "创建第一个 System"
/vitepress-tutorial "使用事件系统"
```

**输出位置**：`docs/zh-CN/tutorials/<主题>.md`

[详细文档](./vitepress-tutorial/SKILL.md)

---

### 4. vitepress-batch-api - 批量 API 文档生成

为整个模块批量生成 API 文档。

**用途**：
- 初始化模块文档
- 更新整个模块的文档
- 快速生成大量文档

**调用方式**：
```bash
/vitepress-batch-api <模块名>
```

**示例**：
```bash
/vitepress-batch-api Core
/vitepress-batch-api Godot
```

**输出位置**：`docs/zh-CN/api-reference/<模块>/`

[详细文档](./vitepress-batch-api/SKILL.md)

---

### 5. vitepress-validate - 文档验证

验证文档的质量和规范性。

**用途**：
- Frontmatter 格式验证
- 内部链接有效性检查
- 代码块语法验证
- 标点符号规范检查

**调用方式**：
```bash
/vitepress-validate <文件或目录路径>
```

**示例**：
```bash
/vitepress-validate docs/zh-CN/api-reference/core/architecture.md
/vitepress-validate docs/zh-CN/
```

[详细文档](./vitepress-validate/SKILL.md)

---

## 快速开始

### 1. 生成单个 API 文档

```bash
# 为 Architecture 类生成文档
/vitepress-api-doc GFramework.Core/architecture/Architecture.cs
```

### 2. 批量生成模块文档

```bash
# 为整个 Core 模块生成文档
/vitepress-batch-api Core
```

### 3. 生成功能指南

```bash
# 生成事件系统使用指南
/vitepress-guide "事件系统" Core
```

### 4. 生成教程

```bash
# 生成创建 Model 的教程
/vitepress-tutorial "创建第一个 Model"
```

### 5. 验证文档

```bash
# 验证生成的文档
/vitepress-validate docs/zh-CN/api-reference/core/
```

## 工作流程

### 典型工作流程

```mermaid
graph TD
    A[开始] --> B{文档类型?}
    B -->|API 文档| C[/vitepress-api-doc]
    B -->|功能指南| D[/vitepress-guide]
    B -->|教程| E[/vitepress-tutorial]
    C --> F[/vitepress-validate]
    D --> F
    E --> F
    F --> G{验证通过?}
    G -->|是| H[完成]
    G -->|否| I[修复问题]
    I --> F
```

### 推荐流程

1. **初始化模块文档**
   ```bash
   /vitepress-batch-api Core
   ```

2. **生成功能指南**
   ```bash
   /vitepress-guide "IoC 容器" Core
   /vitepress-guide "事件系统" Core
   ```

3. **生成教程**
   ```bash
   /vitepress-tutorial "创建第一个 Model"
   /vitepress-tutorial "使用命令系统"
   ```

4. **验证所有文档**
   ```bash
   /vitepress-validate docs/zh-CN/
   ```

## 目录结构

```
.claude/skills/
├── README.md                          # 本文件
├── _shared/                           # 共享资源
│   └── scripts/                       # 共享脚本
│       ├── update-vitepress-nav.sh    # 更新导航配置
│       ├── parse-csharp-xml.sh        # 解析 XML 注释
│       └── generate-examples.sh       # 生成代码示例
│
├── vitepress-api-doc/                 # API 文档生成
│   ├── SKILL.md                       # Skill 说明
│   ├── template.md                    # 文档模板
│   └── examples/                      # 示例文档
│       ├── class-example.md
│       ├── interface-example.md
│       └── enum-example.md
│
├── vitepress-guide/                   # 功能指南生成
│   ├── SKILL.md
│   ├── template.md
│   └── examples/
│       └── guide-example.md
│
├── vitepress-tutorial/                # 教程生成
│   ├── SKILL.md
│   ├── template.md
│   └── examples/
│       └── tutorial-example.md
│
├── vitepress-batch-api/               # 批量 API 文档生成
│   ├── SKILL.md
│   └── scripts/
│       └── batch-generate.sh
│
└── vitepress-validate/                # 文档验证
    ├── SKILL.md
    └── scripts/
        ├── validate-frontmatter.sh
        ├── validate-links.sh
        ├── validate-code-blocks.sh
        └── validate-all.sh
```

## 设计原则

### 1. 单一职责

每个 skill 专注于一个特定任务：
- `vitepress-api-doc` - 单文件 API 文档
- `vitepress-guide` - 功能指南
- `vitepress-tutorial` - 分步教程
- `vitepress-batch-api` - 批量生成
- `vitepress-validate` - 质量验证

### 2. 模块化设计

- 共享脚本放在 `_shared/scripts/`
- 每个 skill 独立维护
- 可以单独使用或组合使用

### 3. 基于源代码

- 仅使用 XML 注释，不添加 AI 补充
- 保持文档与代码同步
- 代码示例由 AI 自动生成

### 4. 质量保证

- 所有生成的文档都应通过验证
- 遵循 VitePress 规范
- 保持一致的文档风格

## 文档规范

### Frontmatter 格式

```yaml
---
title: 文档标题
description: 简短描述（1-2 句话）
outline: deep  # 可选
---
```

### 代码块标记

- C# 代码使用 `csharp`
- Bash 脚本使用 `bash`
- JSON 使用 `json`
- YAML 使用 `yaml`

### 泛型符号转义

在正文中使用 HTML 实体：
- `List<T>` → `List&lt;T&gt;`
- 代码块内保持原样

### 中文标点符号

- 中文句子使用全角标点：，。！？
- 英文句子使用半角标点：,.!?
- 代码周围使用半角符号

## 共享脚本

### update-vitepress-nav.sh

更新 VitePress 侧边栏导航配置。

**用法**：
```bash
.claude/skills/_shared/scripts/update-vitepress-nav.sh <文件路径> <标题>
```

### parse-csharp-xml.sh

解析 C# XML 文档注释。

**用法**：
```bash
.claude/skills/_shared/scripts/parse-csharp-xml.sh <C# 文件路径>
```

### generate-examples.sh

生成代码示例。

**用法**：
```bash
.claude/skills/_shared/scripts/generate-examples.sh <类型名> <命名空间>
```

## 最佳实践

### 1. 文档生成顺序

1. 先生成 API 文档（基础）
2. 再生成功能指南（概念）
3. 最后生成教程（实践）

### 2. 保持文档同步

- 修改代码后及时更新文档
- 使用单文件生成更新特定文档
- 定期批量验证所有文档

### 3. 质量控制

- 生成后立即验证
- 修复所有错误和警告
- 确保链接有效

### 4. 版本控制

- 将生成的文档提交到 Git
- 在 PR 中包含文档更新
- 保持文档与代码版本一致

## 故障排除

### 问题：生成的文档缺少内容

**原因**：源代码缺少 XML 注释

**解决方案**：
1. 在源代码中添加 XML 注释
2. 重新生成文档

### 问题：验证失败

**原因**：文档格式不符合规范

**解决方案**：
1. 查看验证错误信息
2. 根据提示修复问题
3. 重新验证

### 问题：链接损坏

**原因**：文件路径错误或文件不存在

**解决方案**：
1. 检查链接的目标文件是否存在
2. 修正文件路径
3. 重新验证

### 问题：批量生成速度慢

**原因**：文件数量多

**解决方案**：
1. 使用 `--parallel` 选项（如果支持）
2. 分批生成
3. 仅生成修改的文件

## 扩展开发

### 添加新 Skill

1. 在 `.claude/skills/` 下创建新目录
2. 创建 `SKILL.md` 说明文档
3. 创建必要的模板和脚本
4. 在本 README 中添加说明

### 修改现有 Skill

1. 更新 `SKILL.md` 文档
2. 修改模板或脚本
3. 更新示例文档
4. 测试修改后的功能

## 贡献指南

### 报告问题

在 GitHub Issues 中报告问题，包含：
- 使用的 skill 名称
- 输入参数
- 预期结果
- 实际结果
- 错误信息

### 提交改进

1. Fork 项目
2. 创建功能分支
3. 提交修改
4. 创建 Pull Request

## 版本历史

- v1.0.0 (2025-01-XX) - 初始版本
  - 5 个核心 skills
  - 3 个共享脚本
  - 完整的文档和示例

## 许可证

与 GFramework 项目保持一致。

## 联系方式

如有问题或建议，请通过以下方式联系：
- GitHub Issues
- 项目讨论区

---

**注意**：本 skills 系统专为 GFramework 项目设计，使用前请确保了解项目结构和文档规范。
