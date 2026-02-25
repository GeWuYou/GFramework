#!/bin/bash
# 执行所有验证
# 用法: validate-all.sh <文件或目录路径>

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

echo "=========================================="
echo "VitePress 文档验证"
echo "=========================================="
echo ""

# 收集所有 Markdown 文件
if [ -f "$TARGET" ]; then
    FILES=("$TARGET")
elif [ -d "$TARGET" ]; then
    mapfile -t FILES < <(find "$TARGET" -name "*.md" -type f)
else
    echo "错误: 无效的路径: $TARGET"
    exit 1
fi

if [ ${#FILES[@]} -eq 0 ]; then
    echo "未找到 Markdown 文件"
    exit 0
fi

echo "找到 ${#FILES[@]} 个文件"
echo ""

TOTAL_ERRORS=0
TOTAL_WARNINGS=0
PASSED_FILES=0
FAILED_FILES=0

for FILE in "${FILES[@]}"; do
    echo "验证: $FILE"
    echo "----------------------------------------"

    FILE_ERRORS=0
    FILE_WARNINGS=0

    # 1. Frontmatter 验证
    if bash "$SCRIPT_DIR/validate-frontmatter.sh" "$FILE" 2>&1 | grep -q "✗"; then
        FILE_ERRORS=$((FILE_ERRORS + 1))
    fi

    # 2. 链接验证
    if bash "$SCRIPT_DIR/validate-links.sh" "$FILE" 2>&1 | grep -q "✗"; then
        FILE_ERRORS=$((FILE_ERRORS + 1))
    fi

    # 3. 代码块验证
    OUTPUT=$(bash "$SCRIPT_DIR/validate-code-blocks.sh" "$FILE" 2>&1 || true)
    if echo "$OUTPUT" | grep -q "✗"; then
        FILE_ERRORS=$((FILE_ERRORS + 1))
    fi
    if echo "$OUTPUT" | grep -q "⚠"; then
        FILE_WARNINGS=$((FILE_WARNINGS + 1))
    fi

    # 统计结果
    if [ $FILE_ERRORS -eq 0 ]; then
        echo "✓ 验证通过"
        PASSED_FILES=$((PASSED_FILES + 1))
    else
        echo "✗ 验证失败（$FILE_ERRORS 个错误）"
        FAILED_FILES=$((FAILED_FILES + 1))
    fi

    if [ $FILE_WARNINGS -gt 0 ]; then
        echo "⚠ $FILE_WARNINGS 个警告"
    fi

    TOTAL_ERRORS=$((TOTAL_ERRORS + FILE_ERRORS))
    TOTAL_WARNINGS=$((TOTAL_WARNINGS + FILE_WARNINGS))

    echo ""
done

echo "=========================================="
echo "验证摘要"
echo "=========================================="
echo "总文件数: ${#FILES[@]}"
echo "通过: $PASSED_FILES"
echo "失败: $FAILED_FILES"
echo "总错误数: $TOTAL_ERRORS"
echo "总警告数: $TOTAL_WARNINGS"
echo ""

if [ $TOTAL_ERRORS -eq 0 ]; then
    echo "✓ 所有验证通过"
    exit 0
else
    echo "✗ 验证失败"
    exit 1
fi
