#!/bin/bash
# 生成代码示例辅助脚本
# 用法: generate-examples.sh <类型> <类名> [命名空间]

set -e

TYPE="$1"  # class/interface/enum
CLASS_NAME="$2"
NAMESPACE="$3"

if [ -z "$TYPE" ] || [ -z "$CLASS_NAME" ]; then
    echo "用法: $0 <类型> <类名> [命名空间]"
    echo "类型: class, interface, enum"
    exit 1
fi

echo "=========================================="
echo "代码示例生成指南"
echo "=========================================="
echo "类型: $TYPE"
echo "类名: $CLASS_NAME"
if [ -n "$NAMESPACE" ]; then
    echo "命名空间: $NAMESPACE"
fi
echo ""

# 根据类型提供示例生成指南
case "$TYPE" in
    class)
        echo "## 类示例生成建议"
        echo ""
        echo "1. 基本用法示例:"
        echo "   - 实例化对象"
        echo "   - 调用主要方法"
        echo "   - 访问公共属性"
        echo ""
        echo "2. 常见场景示例:"
        echo "   - 实际应用案例"
        echo "   - 与其他组件的集成"
        echo ""
        echo "3. 高级用法示例（如适用）:"
        echo "   - 复杂配置"
        echo "   - 扩展和自定义"
        echo ""
        echo "示例模板:"
        echo '```csharp'
        echo "// 创建实例"
        echo "var instance = new $CLASS_NAME();"
        echo ""
        echo "// 使用主要功能"
        echo "instance.MainMethod();"
        echo '```'
        ;;
    interface)
        echo "## 接口示例生成建议"
        echo ""
        echo "1. 实现接口:"
        echo "   - 展示如何实现该接口"
        echo "   - 实现所有必需成员"
        echo ""
        echo "2. 使用接口:"
        echo "   - 通过接口类型使用实例"
        echo "   - 依赖注入场景"
        echo ""
        echo "示例模板:"
        echo '```csharp'
        echo "// 实现接口"
        echo "public class My$CLASS_NAME : $CLASS_NAME"
        echo "{"
        echo "    // 实现成员"
        echo "}"
        echo ""
        echo "// 使用接口"
        echo "$CLASS_NAME instance = new My$CLASS_NAME();"
        echo '```'
        ;;
    enum)
        echo "## 枚举示例生成建议"
        echo ""
        echo "1. 基本用法:"
        echo "   - 枚举值赋值"
        echo "   - 值比较"
        echo ""
        echo "2. Switch 语句:"
        echo "   - 根据枚举值执行不同逻辑"
        echo ""
        echo "3. 枚举转换:"
        echo "   - 字符串转枚举"
        echo "   - 枚举转整数"
        echo ""
        echo "示例模板:"
        echo '```csharp'
        echo "// 使用枚举值"
        echo "var value = $CLASS_NAME.SomeValue;"
        echo ""
        echo "// Switch 语句"
        echo "switch (value)"
        echo "{"
        echo "    case $CLASS_NAME.Value1:"
        echo "        // 处理逻辑"
        echo "        break;"
        echo "    case $CLASS_NAME.Value2:"
        echo "        // 处理逻辑"
        echo "        break;"
        echo "}"
        echo '```'
        ;;
    *)
        echo "错误: 不支持的类型: $TYPE"
        exit 1
        ;;
esac

echo ""
echo "注意事项:"
echo "- 使用项目的命名约定"
echo "- 包含必要的 using 语句"
echo "- 确保示例代码可以编译运行"
echo "- 参考现有教程的代码风格"

exit 0
