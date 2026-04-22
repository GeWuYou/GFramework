#!/bin/bash
# 验证 Markdown frontmatter。

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

if ! head -n 5 "$FILE" | grep -q "^---$"; then
    echo "✗ 错误: 文件缺少 frontmatter"
    exit 1
fi

FRONTMATTER=$(sed -n '/^---$/,/^---$/p' "$FILE" | sed '1d;$d')

if [ -z "$FRONTMATTER" ]; then
    echo "✗ 错误: frontmatter 为空"
    exit 1
fi

if ! echo "$FRONTMATTER" | grep -q "^title:"; then
    echo "✗ 错误: 缺少必需字段: title"
    exit 1
fi

if ! echo "$FRONTMATTER" | grep -q "^description:"; then
    echo "✗ 错误: 缺少必需字段: description"
    exit 1
fi

echo "✓ Frontmatter 验证通过"
