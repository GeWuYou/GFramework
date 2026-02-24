#!/bin/bash
# 解析 C# XML 文档注释
# 用法: parse-csharp-xml.sh <C# 文件路径>

set -e

FILE_PATH="$1"

if [ -z "$FILE_PATH" ]; then
    echo "用法: $0 <C# 文件路径>"
    exit 1
fi

if [ ! -f "$FILE_PATH" ]; then
    echo "错误: 文件不存在: $FILE_PATH"
    exit 1
fi

echo "解析 C# XML 文档注释: $FILE_PATH"

# 提取 summary 标签内容
echo "=== Summary ==="
grep -A 5 "/// <summary>" "$FILE_PATH" | grep "///" | sed 's/.*\/\/\/\s*//' | sed 's/<summary>//g' | sed 's/<\/summary>//g' || echo "未找到 summary"

# 提取 param 标签内容
echo ""
echo "=== Parameters ==="
grep "/// <param" "$FILE_PATH" | sed 's/.*\/\/\/\s*//' || echo "未找到 param"

# 提取 returns 标签内容
echo ""
echo "=== Returns ==="
grep "/// <returns>" "$FILE_PATH" | sed 's/.*\/\/\/\s*//' | sed 's/<returns>//g' | sed 's/<\/returns>//g' || echo "未找到 returns"

# 提取 exception 标签内容
echo ""
echo "=== Exceptions ==="
grep "/// <exception" "$FILE_PATH" | sed 's/.*\/\/\/\s*//' || echo "未找到 exception"

# 提取 example 标签内容
echo ""
echo "=== Examples ==="
grep -A 10 "/// <example>" "$FILE_PATH" | grep "///" | sed 's/.*\/\/\/\s*//' || echo "未找到 example"

echo ""
echo "解析完成"

exit 0
