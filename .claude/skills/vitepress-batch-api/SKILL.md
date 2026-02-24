# VitePress 批量 API 文档生成

为整个模块批量生成 API 参考文档，提高文档生成效率。

## 用途

此 skill 用于批量生成模块的 API 文档，适用于：
- 初始化模块文档
- 更新整个模块的文档
- 为新模块快速生成文档
- 重新生成所有 API 文档

## 调用方式

```bash
/vitepress-batch-api <模块名>
```

**示例**：
```bash
/vitepress-batch-api Core
/vitepress-batch-api Game
/vitepress-batch-api Godot
/vitepress-batch-api SourceGenerators
```

## 工作流程

1. **扫描模块目录**
   - 根据模块名确定源代码目录
   - 递归扫描所有 C# 文件

2. **过滤目标文件**
   - 仅包含公共类型（public class/interface/enum/struct）
   - 排除内部类型（internal）
   - 排除生成的代码（*.g.cs、*.Designer.cs）
   - 排除测试文件（*.Tests.cs）

3. **批量生成文档**
   - 为每个类型调用 `/vitepress-api-doc`
   - 显示进度信息
   - 收集生成结果

4. **生成模块索引页**
   - 创建 `index.md` 列出所有 API
   - 按类别分组（类、接口、枚举）
   - 添加简短描述

5. **批量更新导航**
   - 在 VitePress 配置中添加所有新文档
   - 保持字母顺序
   - 更新模块索引

6. **生成摘要报告**
   - 统计生成的文档数量
   - 列出成功和失败的文件
   - 提供验证建议

## 输出规范

### 模块索引页格式

```markdown
---
title: Core API 参考
description: GFramework.Core 模块的 API 参考文档
---

# Core API 参考

## 概述

GFramework.Core 是框架的核心模块，提供架构基础、依赖注入、事件系统等核心功能。

## 类

- [Architecture](./architecture.md) - 架构基类
- [ArchitectureConfiguration](./architecture-configuration.md) - 架构配置
- [IocContainer](./ioc-container.md) - IoC 容器

## 接口

- [IArchitecture](./iarchitecture.md) - 架构接口
- [IModel](./imodel.md) - 模型接口
- [ISystem](./isystem.md) - 系统接口

## 枚举

- [ArchitecturePhase](./architecture-phase.md) - 架构阶段
```

### 目录结构

```
docs/zh-CN/api-reference/
├── core/
│   ├── index.md              # 模块索引
│   ├── architecture.md
│   ├── iarchitecture.md
│   └── ...
├── game/
│   ├── index.md
│   └── ...
└── godot/
    ├── index.md
    └── ...
```

## 模块映射

### 源代码目录映射

| 模块名 | 源代码目录 | 输出目录 |
|--------|-----------|---------|
| Core | `GFramework.Core/` | `docs/zh-CN/api-reference/core/` |
| Game | `GFramework.Game/` | `docs/zh-CN/api-reference/game/` |
| Godot | `GFramework.Godot/` | `docs/zh-CN/api-reference/godot/` |
| SourceGenerators | `GFramework.SourceGenerators/` | `docs/zh-CN/api-reference/source-generators/` |

### 命名空间映射

- `GFramework.Core.*` → Core 模块
- `GFramework.Game.*` → Game 模块
- `GFramework.Godot.*` → Godot 模块
- `GFramework.SourceGenerators.*` → SourceGenerators 模块

## 过滤规则

### 包含的文件

- 公共类（public class）
- 公共接口（public interface）
- 公共枚举（public enum）
- 公共结构体（public struct）

### 排除的文件

- 内部类型（internal）
- 生成的代码（`*.g.cs`、`*.Designer.cs`）
- 测试文件（`*.Tests.cs`、`*Test.cs`）
- 临时文件（`*.tmp.cs`）
- 编译器生成的文件（`AssemblyInfo.cs`）

### 排除的类型

- 编译器生成的类型（`<>c__DisplayClass`）
- 匿名类型
- 嵌套的私有类型

## 批量处理脚本

### batch-generate.sh

```bash
#!/bin/bash
# 批量生成 API 文档
# 用法: batch-generate.sh <模块名>

set -e

MODULE="$1"

if [ -z "$MODULE" ]; then
    echo "用法: $0 <模块名>"
    echo "可用模块: Core, Game, Godot, SourceGenerators"
    exit 1
fi

# 确定源代码目录
case "$MODULE" in
    Core)
        SOURCE_DIR="GFramework.Core"
        ;;
    Game)
        SOURCE_DIR="GFramework.Game"
        ;;
    Godot)
        SOURCE_DIR="GFramework.Godot"
        ;;
    SourceGenerators)
        SOURCE_DIR="GFramework.SourceGenerators"
        ;;
    *)
        echo "错误: 未知的模块: $MODULE"
        exit 1
        ;;
esac

if [ ! -d "$SOURCE_DIR" ]; then
    echo "错误: 源代码目录不存在: $SOURCE_DIR"
    exit 1
fi

echo "=========================================="
echo "批量生成 $MODULE 模块的 API 文档"
echo "=========================================="
echo ""

# 查找所有 C# 文件
FILES=$(find "$SOURCE_DIR" -name "*.cs" -type f \
    ! -name "*.g.cs" \
    ! -name "*.Designer.cs" \
    ! -name "*Test.cs" \
    ! -name "*.Tests.cs" \
    ! -name "AssemblyInfo.cs")

FILE_COUNT=$(echo "$FILES" | wc -l)
echo "找到 $FILE_COUNT 个文件"
echo ""

GENERATED=0
SKIPPED=0
FAILED=0

for FILE in $FILES; do
    echo "处理: $FILE"

    # 检查是否包含公共类型
    if ! grep -q "public \(class\|interface\|enum\|struct\)" "$FILE"; then
        echo "  ⊘ 跳过（无公共类型）"
        SKIPPED=$((SKIPPED + 1))
        continue
    fi

    # 调用 vitepress-api-doc（由 AI 执行）
    # /vitepress-api-doc "$FILE"

    if [ $? -eq 0 ]; then
        echo "  ✓ 生成成功"
        GENERATED=$((GENERATED + 1))
    else
        echo "  ✗ 生成失败"
        FAILED=$((FAILED + 1))
    fi

    echo ""
done

echo "=========================================="
echo "批量生成完成"
echo "=========================================="
echo "总文件数: $FILE_COUNT"
echo "生成成功: $GENERATED"
echo "跳过: $SKIPPED"
echo "失败: $FAILED"
echo ""

if [ $FAILED -eq 0 ]; then
    echo "✓ 所有文档生成成功"
    exit 0
else
    echo "✗ 部分文档生成失败"
    exit 1
fi
```

## 配置选项

### 过滤选项

```bash
# 包含内部类型
/vitepress-batch-api Core --include-internal

# 包含生成的代码
/vitepress-batch-api Core --include-generated

# 自定义过滤规则
/vitepress-batch-api Core --exclude "*.Tests.cs" --exclude "*.g.cs"
```

### 输出选项

```bash
# 指定输出目录
/vitepress-batch-api Core --output docs/zh-CN/api-reference/core/

# 覆盖现有文档
/vitepress-batch-api Core --force

# 仅生成索引页
/vitepress-batch-api Core --index-only
```

### 并行处理

```bash
# 并行生成（加快速度）
/vitepress-batch-api Core --parallel 4
```

## 进度显示

### 实时进度

```
========================================
批量生成 Core 模块的 API 文档
========================================

找到 45 个文件

[1/45] 处理: GFramework.Core/architecture/Architecture.cs
  ✓ 生成成功

[2/45] 处理: GFramework.Core/architecture/IArchitecture.cs
  ✓ 生成成功

[3/45] 处理: GFramework.Core/command/Command.cs
  ⊘ 跳过（无公共类型）

...

[45/45] 处理: GFramework.Core/utility/Utility.cs
  ✓ 生成成功

========================================
批量生成完成
========================================
总文件数: 45
生成成功: 38
跳过: 5
失败: 2

✗ 部分文档生成失败

失败的文件:
- GFramework.Core/internal/InternalClass.cs (缺少 XML 注释)
- GFramework.Core/legacy/LegacyClass.cs (解析错误)
```

## 前置条件

1. 模块源代码目录存在
2. 源代码文件包含 XML 文档注释
3. 有足够的磁盘空间存储生成的文档

## 相关 Skills

- `/vitepress-api-doc` - 单文件 API 文档生成
- `/vitepress-validate` - 验证生成的文档
- `/vitepress-guide` - 生成功能指南

## 最佳实践

1. **首次生成**：使用批量生成快速创建所有文档
2. **增量更新**：修改代码后使用单文件生成更新对应文档
3. **定期验证**：批量生成后运行验证确保质量
4. **版本控制**：将生成的文档提交到版本控制系统

## 故障排除

### 问题：部分文件生成失败
**解决方案**：检查失败文件的 XML 注释是否完整，手动修复后重新生成

### 问题：生成速度慢
**解决方案**：使用 `--parallel` 选项启用并行处理

### 问题：生成的文档过多
**解决方案**：使用过滤选项排除不需要的文件

## 版本历史

- v1.0.0 - 初始版本，支持批量 API 文档生成
