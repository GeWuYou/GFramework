#!/bin/bash
# 运行统一文档校验脚本集合。

set -e

TARGET="$1"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if [ -z "$TARGET" ]; then
    echo "用法: $0 <文件或目录路径>"
    exit 1
fi

if [ ! -e "$TARGET" ]; then
    echo "错误: 路径不存在: $TARGET"
    exit 1
fi

if [ -f "$TARGET" ]; then
    FILES=("$TARGET")
else
    mapfile -t FILES < <(find "$TARGET" -type f -name "*.md" | sort)
fi

if [ ${#FILES[@]} -eq 0 ]; then
    echo "未找到 Markdown 文件"
    exit 0
fi

TOTAL_ERRORS=0
FAILED_FILES=0

for FILE in "${FILES[@]}"; do
    FILE_ERRORS=0

    echo "验证: $FILE"

    if ! bash "$SCRIPT_DIR/validate-frontmatter.sh" "$FILE"; then
        FILE_ERRORS=$((FILE_ERRORS + 1))
    fi

    if ! bash "$SCRIPT_DIR/validate-links.sh" "$FILE"; then
        FILE_ERRORS=$((FILE_ERRORS + 1))
    fi

    if ! bash "$SCRIPT_DIR/validate-code-blocks.sh" "$FILE"; then
        FILE_ERRORS=$((FILE_ERRORS + 1))
    fi

    if [ $FILE_ERRORS -eq 0 ]; then
        echo "✓ $FILE"
    else
        echo "✗ $FILE"
        FAILED_FILES=$((FAILED_FILES + 1))
    fi

    TOTAL_ERRORS=$((TOTAL_ERRORS + FILE_ERRORS))
    echo ""
done

if [ $TOTAL_ERRORS -eq 0 ]; then
    echo "✓ 所有验证通过"
    exit 0
fi

echo "✗ 验证失败：$FAILED_FILES 个文件存在问题"
exit 1
