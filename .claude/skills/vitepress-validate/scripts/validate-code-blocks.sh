#!/bin/bash
# 验证代码块语法
# 用法: validate-code-blocks.sh <文件路径>

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

echo "验证代码块语法: $FILE"

ERROR_COUNT=0
WARNING_COUNT=0

# 检查未闭合的代码块
OPEN_COUNT=$(grep -c '^```' "$FILE" || true)
if [ $((OPEN_COUNT % 2)) -ne 0 ]; then
    echo "✗ 错误: 存在未闭合的代码块"
    ERROR_COUNT=$((ERROR_COUNT + 1))
fi

# 检查 C# 代码块标记
LINE_NUM=0
while IFS= read -r LINE; do
    LINE_NUM=$((LINE_NUM + 1))

    # 检查是否使用了错误的 C# 标记
    if echo "$LINE" | grep -qE '^```(cs|c#|C#)$'; then
        echo "⚠ 警告: 第 $LINE_NUM 行使用了非标准标记，建议使用 'csharp'"
        echo "  当前: $LINE"
        WARNING_COUNT=$((WARNING_COUNT + 1))
    fi

    # 检查代码块是否有语言标记
    if echo "$LINE" | grep -qE '^```$'; then
        # 检查下一行是否是代码（简单启发式：不是空行且不是 ```）
        NEXT_LINE=$(sed -n "$((LINE_NUM + 1))p" "$FILE")
        if [ -n "$NEXT_LINE" ] && ! echo "$NEXT_LINE" | grep -qE '^```'; then
            echo "⚠ 警告: 第 $LINE_NUM 行的代码块缺少语言标记"
            WARNING_COUNT=$((WARNING_COUNT + 1))
        fi
    fi
done < "$FILE"

# 输出结果
if [ $ERROR_COUNT -eq 0 ] && [ $WARNING_COUNT -eq 0 ]; then
    echo "✓ 代码块验证通过"
    exit 0
elif [ $ERROR_COUNT -eq 0 ]; then
    echo "⚠ 代码块验证通过（有 $WARNING_COUNT 个警告）"
    exit 0
else
    echo "✗ 代码块验证失败（$ERROR_COUNT 个错误，$WARNING_COUNT 个警告）"
    exit 1
fi
