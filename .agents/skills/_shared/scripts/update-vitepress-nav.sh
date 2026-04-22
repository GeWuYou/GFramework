#!/bin/bash
# 更新 VitePress 侧边栏配置
# 用法: update-vitepress-nav.sh <文档路径> <文档标题>

set -e

DOC_PATH="$1"
DOC_TITLE="$2"
CONFIG_FILE="docs/.vitepress/config.mts"

if [ -z "$DOC_PATH" ] || [ -z "$DOC_TITLE" ]; then
    echo "用法: $0 <文档路径> <文档标题>"
    echo "示例: $0 /zh-CN/api-reference/core/architecture Architecture"
    exit 1
fi

if [ ! -f "$CONFIG_FILE" ]; then
    echo "错误: 找不到 VitePress 配置文件: $CONFIG_FILE"
    exit 1
fi

# 提取模块名称（core/game/godot/source-generators）
MODULE=$(echo "$DOC_PATH" | grep -oP '(?<=/zh-CN/)[^/]+' | head -1)

if [ -z "$MODULE" ]; then
    echo "错误: 无法从路径中提取模块名称: $DOC_PATH"
    exit 1
fi

echo "=========================================="
echo "VitePress 导航配置更新"
echo "=========================================="
echo "模块: $MODULE"
echo "路径: $DOC_PATH"
echo "标题: $DOC_TITLE"
echo ""

# 检查配置文件中是否已存在该路径
if grep -q "link: '$DOC_PATH'" "$CONFIG_FILE"; then
    echo "✓ 该文档已存在于导航配置中"
    exit 0
fi

echo "提示: 需要在 VitePress 配置中添加新条目"
echo ""
echo "建议的配置条目:"
echo "{ text: '$DOC_TITLE', link: '$DOC_PATH' }"
echo ""
echo "请使用以下方式之一更新配置:"
echo "1. 让 AI 使用 Edit 工具直接修改 $CONFIG_FILE"
echo "2. 手动编辑配置文件并添加上述条目到对应的 sidebar 部分"
echo ""

# 输出相关的 sidebar 配置路径
echo "相关的 sidebar 配置路径: '/zh-CN/$MODULE/'"

exit 0
