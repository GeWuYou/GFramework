#!/bin/bash
# 共享的模块配置
# 机器可读映射以 .agents/skills/_shared/module-map.json 为准。

normalize_module() {
    local INPUT
    INPUT="$(echo "$1" | tr '[:upper:]' '[:lower:]' | tr ' ' '-' | tr '_' '-')"

    case "$INPUT" in
        core|core-runtime|runtime-core|core-module)
            echo "Core"
            ;;
        core.abstractions|core-abstractions)
            echo "Core.Abstractions"
            ;;
        core.sourcegenerators|core-source-generators|core-sourcegenerators)
            echo "Core.SourceGenerators"
            ;;
        core.sourcegenerators.abstractions|core-source-generators-abstractions)
            echo "Core.SourceGenerators.Abstractions"
            ;;
        game|game-runtime|runtime-game|game-module)
            echo "Game"
            ;;
        game.abstractions|game-abstractions)
            echo "Game.Abstractions"
            ;;
        game.sourcegenerators|game-source-generators)
            echo "Game.SourceGenerators"
            ;;
        godot|godot-runtime|runtime-godot|godot-module)
            echo "Godot"
            ;;
        godot.sourcegenerators|godot-source-generators|godot-generators)
            echo "Godot.SourceGenerators"
            ;;
        godot.sourcegenerators.abstractions|godot-source-generators-abstractions)
            echo "Godot.SourceGenerators.Abstractions"
            ;;
        cqrs|mediator|cqrs-module)
            echo "Cqrs"
            ;;
        cqrs.abstractions|cqrs-abstractions)
            echo "Cqrs.Abstractions"
            ;;
        cqrs.sourcegenerators|cqrs-source-generators)
            echo "Cqrs.SourceGenerators"
            ;;
        ecs|ecs.arch|ecs-arch)
            echo "Ecs.Arch"
            ;;
        ecs.arch.abstractions|ecs-arch-abstractions)
            echo "Ecs.Arch.Abstractions"
            ;;
        sourcegenerators.common|source-generators-common)
            echo "SourceGenerators.Common"
            ;;
        *)
            return 1
            ;;
    esac
}

get_all_modules() {
    cat <<'EOF'
Core
Core.Abstractions
Core.SourceGenerators
Core.SourceGenerators.Abstractions
Game
Game.Abstractions
Game.SourceGenerators
Godot
Godot.SourceGenerators
Godot.SourceGenerators.Abstractions
Cqrs
Cqrs.Abstractions
Cqrs.SourceGenerators
Ecs.Arch
Ecs.Arch.Abstractions
SourceGenerators.Common
EOF
}

is_valid_module() {
    normalize_module "$1" >/dev/null 2>&1
}

get_source_dirs() {
    local MODULE
    MODULE="$(normalize_module "$1")" || return 1

    case "$MODULE" in
        Core)
            echo "GFramework.Core"
            ;;
        Core.Abstractions)
            echo "GFramework.Core.Abstractions"
            ;;
        Core.SourceGenerators)
            echo "GFramework.Core.SourceGenerators"
            ;;
        Core.SourceGenerators.Abstractions)
            echo "GFramework.Core.SourceGenerators.Abstractions"
            ;;
        Game)
            echo "GFramework.Game"
            ;;
        Game.Abstractions)
            echo "GFramework.Game.Abstractions"
            ;;
        Game.SourceGenerators)
            echo "GFramework.Game.SourceGenerators"
            ;;
        Godot)
            echo "GFramework.Godot"
            ;;
        Godot.SourceGenerators)
            echo "GFramework.Godot.SourceGenerators"
            ;;
        Godot.SourceGenerators.Abstractions)
            echo "GFramework.Godot.SourceGenerators.Abstractions"
            ;;
        Cqrs)
            echo "GFramework.Cqrs"
            ;;
        Cqrs.Abstractions)
            echo "GFramework.Cqrs.Abstractions"
            ;;
        Cqrs.SourceGenerators)
            echo "GFramework.Cqrs.SourceGenerators"
            ;;
        Ecs.Arch)
            echo "GFramework.Ecs.Arch"
            ;;
        Ecs.Arch.Abstractions)
            echo "GFramework.Ecs.Arch.Abstractions"
            ;;
        SourceGenerators.Common)
            echo "GFramework.SourceGenerators.Common"
            ;;
    esac
}

get_test_projects() {
    local MODULE
    MODULE="$(normalize_module "$1")" || return 1

    case "$MODULE" in
        Core|Core.Abstractions)
            echo "GFramework.Core.Tests/GFramework.Core.Tests.csproj"
            ;;
        Core.SourceGenerators|Core.SourceGenerators.Abstractions|Game.SourceGenerators|Cqrs.SourceGenerators|SourceGenerators.Common)
            echo "GFramework.SourceGenerators.Tests/GFramework.SourceGenerators.Tests.csproj"
            ;;
        Game|Game.Abstractions)
            echo "GFramework.Game.Tests/GFramework.Game.Tests.csproj"
            ;;
        Godot)
            echo "GFramework.Godot.Tests/GFramework.Godot.Tests.csproj"
            ;;
        Godot.SourceGenerators|Godot.SourceGenerators.Abstractions)
            echo "GFramework.Godot.SourceGenerators.Tests/GFramework.Godot.SourceGenerators.Tests.csproj"
            ;;
        Cqrs|Cqrs.Abstractions)
            echo "GFramework.Cqrs.Tests/GFramework.Cqrs.Tests.csproj"
            ;;
        Ecs.Arch|Ecs.Arch.Abstractions)
            echo "GFramework.Ecs.Arch.Tests/GFramework.Ecs.Arch.Tests.csproj"
            ;;
    esac
}

get_readme_paths() {
    local MODULE
    MODULE="$(normalize_module "$1")" || return 1

    case "$MODULE" in
        Core)
            echo "GFramework.Core/README.md"
            ;;
        Core.Abstractions)
            echo "GFramework.Core.Abstractions/README.md"
            ;;
        Core.SourceGenerators)
            echo "GFramework.Core.SourceGenerators/README.md"
            ;;
        Game)
            echo "GFramework.Game/README.md"
            ;;
        Game.Abstractions)
            echo "GFramework.Game.Abstractions/README.md"
            ;;
        Game.SourceGenerators)
            echo "GFramework.Game.SourceGenerators/README.md"
            ;;
        Godot)
            echo "GFramework.Godot/README.md"
            ;;
        Godot.SourceGenerators)
            echo "GFramework.Godot.SourceGenerators/README.md"
            ;;
        Cqrs)
            echo "GFramework.Cqrs/README.md"
            ;;
        Cqrs.Abstractions)
            echo "GFramework.Cqrs.Abstractions/README.md"
            ;;
        Cqrs.SourceGenerators)
            echo "GFramework.Cqrs.SourceGenerators/README.md"
            ;;
        Ecs.Arch)
            echo "GFramework.Ecs.Arch/README.md"
            ;;
    esac
}

infer_module_from_namespace() {
    local NAMESPACE="$1"

    if [[ "$NAMESPACE" == GFramework.Core.SourceGenerators.Abstractions* ]]; then
        echo "Core.SourceGenerators.Abstractions"
    elif [[ "$NAMESPACE" == GFramework.Core.SourceGenerators* ]]; then
        echo "Core.SourceGenerators"
    elif [[ "$NAMESPACE" == GFramework.Core.Abstractions* ]]; then
        echo "Core.Abstractions"
    elif [[ "$NAMESPACE" == GFramework.Core* ]]; then
        echo "Core"
    elif [[ "$NAMESPACE" == GFramework.Game.SourceGenerators* ]]; then
        echo "Game.SourceGenerators"
    elif [[ "$NAMESPACE" == GFramework.Game.Abstractions* ]]; then
        echo "Game.Abstractions"
    elif [[ "$NAMESPACE" == GFramework.Game* ]]; then
        echo "Game"
    elif [[ "$NAMESPACE" == GFramework.Godot.SourceGenerators.Abstractions* ]]; then
        echo "Godot.SourceGenerators.Abstractions"
    elif [[ "$NAMESPACE" == GFramework.Godot.SourceGenerators* ]]; then
        echo "Godot.SourceGenerators"
    elif [[ "$NAMESPACE" == GFramework.Godot* ]]; then
        echo "Godot"
    elif [[ "$NAMESPACE" == GFramework.Cqrs.SourceGenerators* ]]; then
        echo "Cqrs.SourceGenerators"
    elif [[ "$NAMESPACE" == GFramework.Cqrs.Abstractions* ]]; then
        echo "Cqrs.Abstractions"
    elif [[ "$NAMESPACE" == GFramework.Cqrs* ]]; then
        echo "Cqrs"
    elif [[ "$NAMESPACE" == GFramework.Ecs.Arch.Abstractions* ]]; then
        echo "Ecs.Arch.Abstractions"
    elif [[ "$NAMESPACE" == GFramework.Ecs.Arch* ]]; then
        echo "Ecs.Arch"
    elif [[ "$NAMESPACE" == GFramework.SourceGenerators.Common* ]]; then
        echo "SourceGenerators.Common"
    else
        return 1
    fi
}
