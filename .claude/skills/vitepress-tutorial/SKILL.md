# VitePress 教程生成

生成分步教程文档，适合初学者学习框架功能。

## 用途

此 skill 用于生成结构化的分步教程，适用于：
- 框架入门教程
- 功能实现教程
- 最佳实践演示
- 问题解决方案

## 调用方式

```bash
/vitepress-tutorial <教程主题>
```

**示例**：
```bash
/vitepress-tutorial "创建第一个 System"
/vitepress-tutorial "实现自定义命令"
/vitepress-tutorial "使用事件系统"
```

## 工作流程

1. **收集需求**
   - 询问用户教程主题
   - 确定学习目标
   - 了解前置知识要求

2. **设计教程步骤**
   - 将任务分解为 3-7 个步骤
   - 每步聚焦一个具体任务
   - 确保步骤之间逻辑连贯

3. **生成教程内容**
   - 根据 `template.md` 创建文档框架
   - 为每步编写详细说明和代码
   - 添加完整的可运行代码
   - 说明预期结果

4. **确定输出路径**
   - 保存到 `docs/zh-CN/tutorials/`
   - 文件名使用小写加连字符

5. **更新导航配置**
   - 在 VitePress 侧边栏中添加新教程

## 输出规范

### Frontmatter 格式

```yaml
---
title: 教程标题
description: 简短描述（1 句话说明学习内容）
---
```

### 文档结构

1. **学习目标**：完成教程后能够掌握的技能
2. **前置条件**：需要的前置知识和环境
3. **步骤 1-N**：分步说明（3-7 步）
4. **完整代码**：汇总所有代码
5. **运行结果**：预期输出和效果
6. **下一步**：后续学习建议

### 步骤格式

每个步骤应包含：
- 步骤标题（简短、动词开头）
- 步骤说明（为什么要这样做）
- 代码示例（完整且可运行）
- 代码解释（关键部分的说明）

**示例**：
```markdown
## 步骤 1：创建 Model 类

首先，我们需要创建一个 Model 来存储玩家数据。Model 负责管理应用的数据和状态。

\`\`\`csharp
using GFramework.Core.Abstractions.model;
using GFramework.Core.Abstractions.property;

public class PlayerModel : IModel
{
    // 玩家名称（可绑定属性）
    public BindableProperty<string> Name { get; } = new("Player");

    // 玩家生命值
    public BindableProperty<int> Health { get; } = new(100);

    // 玩家金币
    public BindableProperty<int> Gold { get; } = new(0);

    public void Init() { }
}
\`\`\`

**代码说明**：
- `BindableProperty<T>` 是可绑定属性，值变化时会自动通知监听者
- `Init()` 方法在 Model 注册到架构时被调用
- 使用属性初始化器设置默认值
```

## 模板变量

- `{{TUTORIAL_TITLE}}` - 教程标题
- `{{TUTORIAL_DESCRIPTION}}` - 简短描述
- `{{LEARNING_OBJECTIVES}}` - 学习目标
- `{{PREREQUISITES}}` - 前置条件
- `{{STEP_N_TITLE}}` - 步骤标题
- `{{STEP_N_CONTENT}}` - 步骤内容
- `{{FULL_CODE}}` - 完整代码
- `{{EXPECTED_OUTPUT}}` - 预期输出
- `{{NEXT_STEPS}}` - 下一步建议

## 示例输出

参考 `examples/tutorial-example.md`，该示例基于现有的教程文档创建。

## 内容要求

### 学习目标
- 使用列表格式
- 3-5 个具体的学习目标
- 使用"能够..."句式

**示例**：
```markdown
## 学习目标

完成本教程后，你将能够：
- 创建自定义的 Model 类
- 在架构中注册 Model
- 从 Controller 中访问 Model
- 使用可绑定属性管理数据
```

### 前置条件
- 列出必需的知识
- 说明环境要求
- 提供相关文档链接

**示例**：
```markdown
## 前置条件

- 已安装 GFramework.Core NuGet 包
- 了解 C# 基础语法
- 阅读过[架构概览](/zh-CN/getting-started)
```

### 步骤内容
- 每步 100-300 字说明
- 包含完整的代码示例
- 解释关键代码的作用
- 使用注释标注重要部分

### 完整代码
- 汇总所有步骤的代码
- 确保可以直接复制运行
- 包含必要的 using 语句
- 添加文件结构说明

### 运行结果
- 描述预期的输出
- 如果有界面，提供截图或描述
- 说明如何验证结果正确

### 下一步
- 推荐 2-3 个后续教程
- 提供相关文档链接
- 建议进阶学习方向

## 写作风格

### 语气
- 友好、鼓励性
- 使用第二人称（"你"）
- 避免假设读者已有高级知识

### 步骤说明
- 使用主动语态
- 步骤标题使用动词开头
- 说明"为什么"而不仅是"怎么做"

### 代码示例
- 完整且可运行
- 包含详细注释
- 使用有意义的变量名
- 遵循项目代码风格

## 配置选项

### 教程难度

```bash
# 初学者（更多解释，简单示例）
/vitepress-tutorial "创建第一个 System" --level beginner

# 中级（平衡解释和复杂度）
/vitepress-tutorial "实现自定义命令" --level intermediate

# 高级（简洁说明，复杂示例）
/vitepress-tutorial "架构模块开发" --level advanced
```

### 步骤数量

```bash
# 指定步骤数量（3-7 步）
/vitepress-tutorial "使用事件系统" --steps 5
```

## 前置条件

1. 了解教程主题的基本概念
2. 能够访问相关代码文件
3. 了解目标受众的知识水平

## 相关 Skills

- `/vitepress-api-doc` - 生成 API 参考文档
- `/vitepress-guide` - 生成功能指南
- `/vitepress-validate` - 验证生成的文档

## 最佳实践

1. **从简单开始**：第一步应该是最简单的操作
2. **逐步增加复杂度**：每步在前一步基础上增加新内容
3. **提供完整代码**：确保每步的代码都可以运行
4. **解释关键概念**：不要假设读者已经了解所有术语
5. **测试教程**：确保按照步骤操作能够得到预期结果

## 故障排除

### 问题：步骤过多，教程太长
**解决方案**：将教程拆分为多个小教程，或合并相似的步骤

### 问题：代码示例不完整
**解决方案**：在"完整代码"章节提供所有文件的完整代码

### 问题：读者反馈步骤不清晰
**解决方案**：增加更多说明，使用截图或图表辅助

## 版本历史

- v1.0.0 - 初始版本，支持分步教程生成
