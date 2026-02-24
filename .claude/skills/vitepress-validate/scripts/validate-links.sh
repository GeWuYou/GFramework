#!/bin/bash
# 验证内部链接有效性
# 用法: validate-links.sh <文件路径>

set -e

FILE="$1"
BASE_DIR="docs/zh-CN"

if [ -z "$FILE" ]; then
    echo "用法: $0 <文件路径>"
    exit 1
fi

if [ ! -f "$FILE" ]; then
    echo "错误: 文件不存在: $FILE"
    exit 1
fi

echo "验证内部链接: $FILE"

# 获取文件所在目录
FILE_DIR=$(dirname "$FILE")

# 提取所有 Markdown 链接
LINKS=$(grep -oP '\[([^\]]+)\]\(([^)]+)\)' "$FILE" | grep -oP '\(([^)]+)\)' | sed 's/[()]//g' || true)

if [ -z "$LINKS" ]; then
    echo "✓ 未找到链接"
    exit 0
fi

ERROR_COUNT=0

while IFS= read -r LINK; do
    # 跳过外部链接
    if [[ "$LINK" =~ ^https?:// ]]; then
        continue
    fi

    # 跳过锚点链接（仅 #开头）
    if [[ "$LINK" =~ ^# ]]; then
        continue
    fi

    # 移除锚点部分
    LINK_PATH=$(echo "$LINK" | sed 's/#.*//')

    # 跳过空路径
    if [ -z "$LINK_PATH" ]; then
        continue
    fi

    # 处理相对路径
    if [[ "$LINK_PATH" =~ ^\. ]]; then
        TARGET="$FILE_DIR/$LINK_PATH"
    # 处理绝对路径
    elif [[ "$LINK_PATH" =~ ^/ ]]; then
        TARGET="docs$LINK_PATH"
        # 如果没有扩展名，尝试添加 .md
        if [[ ! "$TARGET" =~ \. ]]; then
            TARGET="$TARGET.md"
        fi
    else
        TARGET="$FILE_DIR/$LINK_PATH"
    fi

    # 规范化路径
    TARGET=$(realpath -m "$TARGET" 2>/dev/null || echo "$TARGET")

    # 检查文件是否存在
    if [ ! -f "$TARGET" ] && [ ! -d "$TARGET" ]; then
        echo "✗ 损坏的链接: $LINK"
        echo "  目标不存在: $TARGET"
        ERROR_COUNT=$((ERROR_COUNT + 1))
    fi
done <<< "$LINKS"

if [ $ERROR_COUNT -eq 0 ]; then
    echo "✓ 内部链接验证通过"
    exit 0
else
    echo "✗ 发现 $ERROR_COUNT 个损坏的链接"
    exit 1
fi
