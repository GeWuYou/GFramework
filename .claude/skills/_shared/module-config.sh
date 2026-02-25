#!/bin/bash
# 共享的模块配置
# 用于统一管理 GFramework 项目的模块映射关系

# 根据模块名获取源代码目录
get_source_dir() {
    local MODULE="$1"
    case "$MODULE" in
        Core)
            echo "GFramework.Core"
            ;;
        Game)
            echo "GFramework.Game"
            ;;
        Godot)
            echo "GFramework.Godot"
            ;;
        SourceGenerators)
            echo "GFramework.SourceGenerators"
            ;;
        *)
            echo ""
            return 1
            ;;
    esac
}

# 根据模块名获取文档输出目录
get_docs_dir() {
    local MODULE="$1"
    case "$MODULE" in
        Core)
            echo "docs/zh-CN/api-reference/core"
            ;;
        Game)
            echo "docs/zh-CN/api-reference/game"
            ;;
        Godot)
            echo "docs/zh-CN/api-reference/godot"
            ;;
        SourceGenerators)
            echo "docs/zh-CN/api-reference/source-generators"
            ;;
        *)
            echo ""
            return 1
            ;;
    esac
}

# 根据命名空间推断模块名
infer_module_from_namespace() {
    local NAMESPACE="$1"
    if [[ "$NAMESPACE" == GFramework.Core* ]]; then
        echo "Core"
    elif [[ "$NAMESPACE" == GFramework.Game* ]]; then
        echo "Game"
    elif [[ "$NAMESPACE" == GFramework.Godot* ]]; then
        echo "Godot"
    elif [[ "$NAMESPACE" == GFramework.SourceGenerators* ]]; then
        echo "SourceGenerators"
    else
        echo ""
        return 1
    fi
}

# 获取所有可用模块列表
get_all_modules() {
    echo "Core Game Godot SourceGenerators"
}

# 验证模块名是否有效
is_valid_module() {
    local MODULE="$1"
    case "$MODULE" in
        Core|Game|Godot|SourceGenerators)
            return 0
            ;;
        *)
            return 1
            ;;
    esac
}
