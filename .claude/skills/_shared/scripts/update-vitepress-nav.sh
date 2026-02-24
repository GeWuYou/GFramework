#!/bin/bash
# 更新 VitePress 侧边栏配置
# 用法: update-vitepress-nav.sh <文档路径> <文档标题>

set -e

DOC_PATH="$1"
DOC_TITLE="$2"
CONFIG_FILE="docs/.vitepress/config.mts"

if [ -z "$DOC_PATH" ] || [ -z "$DOC_TITLE" ]; then
    echo "用法: $0 <文档路径> <文档标题>"
    echo "示例: $0 /zh-CN/api-reference/core/architecture.md Architecture"
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

echo "正在更新 VitePress 配置..."
echo "  模块: $MODULE"
echo "  路径: $DOC_PATH"
echo "  标题: $DOC_TITLE"

# 注意: 此脚本仅输出提示信息
# 实际的配置更新由 AI 直接编辑 config.mts 文件完成
# 因为 TypeScript 配置文件的复杂性，使用脚本解析和修改容易出错

echo "提示: 请手动更新 $CONFIG_FILE 中的侧边栏配置"
echo "或者让 AI 使用 Edit 工具直接修改配置文件"

exit 0
