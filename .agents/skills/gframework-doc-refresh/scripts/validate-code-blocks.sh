#!/bin/bash
# 验证 Markdown 代码块是否闭合并带有语言标记。

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

ERROR_COUNT=0
WARNING_COUNT=0
CODE_FENCE_COUNT=$(grep -c '^```' "$FILE" || true)

if [ $((CODE_FENCE_COUNT % 2)) -ne 0 ]; then
    echo "✗ 错误: 存在未闭合的代码块"
    ERROR_COUNT=$((ERROR_COUNT + 1))
fi

LINE_NUMBER=0
IN_CODE_BLOCK=0
while IFS= read -r LINE || [ -n "$LINE" ]; do
    LINE_NUMBER=$((LINE_NUMBER + 1))

    if [[ "$LINE" =~ ^\`\`\`(cs|c#|C#)$ ]]; then
        echo "⚠ 警告: 第 $LINE_NUMBER 行使用了非标准 C# 标记，建议改为 csharp"
        WARNING_COUNT=$((WARNING_COUNT + 1))
    fi

    if [[ "$LINE" =~ ^\`\`\` ]]; then
        if [ "$IN_CODE_BLOCK" -eq 0 ]; then
            if [[ "$LINE" == '```' ]]; then
                echo "⚠ 警告: 第 $LINE_NUMBER 行的代码块缺少语言标记"
                WARNING_COUNT=$((WARNING_COUNT + 1))
            fi

            IN_CODE_BLOCK=1
        else
            IN_CODE_BLOCK=0
        fi
    fi
done < "$FILE"

if [ $ERROR_COUNT -eq 0 ] && [ $WARNING_COUNT -eq 0 ]; then
    echo "✓ 代码块验证通过"
    exit 0
fi

if [ $ERROR_COUNT -eq 0 ]; then
    echo "⚠ 代码块验证通过，但有 $WARNING_COUNT 个警告"
    exit 0
fi

echo "✗ 代码块验证失败（$ERROR_COUNT 个错误，$WARNING_COUNT 个警告）"
exit 1
