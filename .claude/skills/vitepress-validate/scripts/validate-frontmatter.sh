#!/bin/bash
# 验证 Frontmatter 格式
# 用法: validate-frontmatter.sh <文件路径>

set -e

FILE="$1"

if [ -z "$FILE" ]; then
    echo "用法: $0 <文件路径>"
    exit 1
fi

if [ ! -f "$FILE" ]; then
    echo "错误: 文件不存在: $FILE"
    exit 1
fi

echo "验证 Frontmatter: $FILE"

# 检查是否有 Frontmatter（限制在前几行，避免匹配正文中的 '---'）
if ! head -n 5 "$FILE" | grep -q "^---$"; then
    echo "✗ 错误: 文件缺少 Frontmatter"
    exit 1
fi

# 提取 Frontmatter 内容（第一个 --- 到第二个 --- 之间）
FRONTMATTER=$(sed -n '/^---$/,/^---$/p' "$FILE" | sed '1d;$d')

if [ -z "$FRONTMATTER" ]; then
    echo "✗ 错误: Frontmatter 为空"
    exit 1
fi

# 检查必需字段: title
if ! echo "$FRONTMATTER" | grep -q "^title:"; then
    echo "✗ 错误: 缺少必需字段: title"
    exit 1
fi

# 检查必需字段: description
if ! echo "$FRONTMATTER" | grep -q "^description:"; then
    echo "✗ 错误: 缺少必需字段: description"
    exit 1
fi

# 检查 outline 字段值（如果存在）
if echo "$FRONTMATTER" | grep -q "^outline:"; then
    OUTLINE_VALUE=$(echo "$FRONTMATTER" | grep "^outline:" | sed 's/outline:\s*//')
    if [ "$OUTLINE_VALUE" != "deep" ] && [ "$OUTLINE_VALUE" != "false" ] && ! echo "$OUTLINE_VALUE" | grep -qE '^\[.*\]$'; then
        echo "⚠ 警告: outline 字段值可能无效: $OUTLINE_VALUE"
        echo "  有效值: deep, false, [2,3]"
    fi
fi

echo "✓ Frontmatter 验证通过"
exit 0
