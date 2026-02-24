#!/bin/bash
# 生成代码示例辅助脚本
# 用法: generate-examples.sh <类型> <类名>

set -e

TYPE="$1"  # class/interface/enum
CLASS_NAME="$2"

if [ -z "$TYPE" ] || [ -z "$CLASS_NAME" ]; then
    echo "用法: $0 <类型> <类名>"
    echo "类型: class, interface, enum"
    exit 1
fi

echo "生成 $CLASS_NAME 的示例代码..."
echo "类型: $TYPE"

# 注意: 此脚本仅输出提示信息
# 实际的示例代码生成由 AI 根据 API 签名和现有教程风格完成

case "$TYPE" in
    class)
        echo "提示: 为类生成示例，包括实例化、方法调用、属性访问"
        ;;
    interface)
        echo "提示: 为接口生成示例，包括实现类和使用方式"
        ;;
    enum)
        echo "提示: 为枚举生成示例，包括值比较和 switch 语句"
        ;;
    *)
        echo "错误: 不支持的类型: $TYPE"
        exit 1
        ;;
esac

exit 0
