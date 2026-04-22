#!/bin/bash
# 验证 Markdown 内部链接是否指向当前仓库中的真实页面。

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

FILE_DIR=$(dirname "$FILE")
LINKS=$(grep -oP '\[([^\]]+)\]\(([^)]+)\)' "$FILE" | grep -oP '\(([^)]+)\)' | sed 's/[()]//g' || true)

if [ -z "$LINKS" ]; then
    echo "✓ 未找到需要验证的链接"
    exit 0
fi

ERROR_COUNT=0

while IFS= read -r LINK; do
    if [[ "$LINK" =~ ^https?:// ]] || [[ "$LINK" =~ ^mailto: ]] || [[ "$LINK" =~ ^# ]]; then
        continue
    fi

    LINK_PATH=$(echo "$LINK" | sed 's/#.*//')

    if [ -z "$LINK_PATH" ]; then
        continue
    fi

    if [[ "$LINK_PATH" =~ ^/ ]]; then
        TARGET="docs$LINK_PATH"
        if [[ ! "$TARGET" =~ \.[A-Za-z0-9]+$ ]]; then
            TARGET="$TARGET.md"
        fi
    elif [[ "$LINK_PATH" =~ ^\. ]]; then
        TARGET="$FILE_DIR/$LINK_PATH"
    else
        TARGET="$FILE_DIR/$LINK_PATH"
    fi

    TARGET=$(realpath -m "$TARGET" 2>/dev/null || echo "$TARGET")

    if [ ! -f "$TARGET" ] && [ ! -d "$TARGET" ]; then
        echo "✗ 损坏的链接: $LINK"
        echo "  目标不存在: $TARGET"
        ERROR_COUNT=$((ERROR_COUNT + 1))
    fi
done <<< "$LINKS"

if [ $ERROR_COUNT -eq 0 ]; then
    echo "✓ 链接验证通过"
    exit 0
fi

echo "✗ 共发现 $ERROR_COUNT 个损坏链接"
exit 1
