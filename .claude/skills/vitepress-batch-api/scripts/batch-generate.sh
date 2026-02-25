#!/bin/bash
# 批量生成 API 文档
# 用法: batch-generate.sh <模块名>

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=../../_shared/module-config.sh
source "$SCRIPT_DIR/../../_shared/module-config.sh"

MODULE="$1"

if [ -z "$MODULE" ]; then
    echo "用法: $0 <模块名>"
    echo "可用模块: $(get_all_modules)"
    exit 1
fi

# 验证模块名
if ! is_valid_module "$MODULE"; then
    echo "错误: 未知的模块: $MODULE"
    echo "可用模块: $(get_all_modules)"
    exit 1
fi

# 获取源代码目录
SOURCE_DIR=$(get_source_dir "$MODULE")

if [ ! -d "$SOURCE_DIR" ]; then
    echo "错误: 源代码目录不存在: $SOURCE_DIR"
    exit 1
fi

echo "=========================================="
echo "批量生成 $MODULE 模块的 API 文档"
echo "=========================================="
echo ""

# 查找所有 C# 文件
mapfile -t FILES < <(find "$SOURCE_DIR" -name "*.cs" -type f \
    ! -name "*.g.cs" \
    ! -name "*.Designer.cs" \
    ! -name "*Test.cs" \
    ! -name "*.Tests.cs" \
    ! -name "AssemblyInfo.cs")

FILE_COUNT=${#FILES[@]}
echo "找到 $FILE_COUNT 个文件"
echo ""

GENERATED=0
SKIPPED=0
FAILED=0

for FILE in "${FILES[@]}"; do
    echo "处理: $FILE"

    # 检查是否包含公共类型
    if ! grep -q "public \(class\|interface\|enum\|struct\)" "$FILE"; then
        echo "  ⊘ 跳过（无公共类型）"
        SKIPPED=$((SKIPPED + 1))
        continue
    fi

    # 注意: 实际的文档生成由 AI 调用 /vitepress-api-doc 完成
    # 此脚本仅用于扫描和过滤文件

    echo "  → 待生成"
    GENERATED=$((GENERATED + 1))
    echo ""
done

echo "=========================================="
echo "扫描完成"
echo "=========================================="
echo "总文件数: $FILE_COUNT"
echo "待生成: $GENERATED"
echo "跳过: $SKIPPED"
echo ""

exit 0
