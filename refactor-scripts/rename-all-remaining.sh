#!/bin/bash
# Copyright (c) 2025-2026 GeWuYou
# SPDX-License-Identifier: Apache-2.0

# 重命名所有剩余项目的文件夹

set -e

ROOT_DIR="/mnt/f/gewuyou/System/Documents/WorkSpace/GameDev/GFramework"

GREEN='\033[0;32m'
CYAN='\033[0;36m'
NC='\033[0m'

log_info() { echo -e "${CYAN}ℹ $1${NC}"; }
log_success() { echo -e "${GREEN}✓ $1${NC}"; }

rename_folder() {
    local from="$1"
    local to="$2"

    if [ ! -d "$from" ]; then
        log_info "跳过（不存在）: $from"
        return
    fi

    if [ "$from" = "$to" ]; then
        log_info "跳过（相同）: $from"
        return
    fi

    log_info "重命名: $from -> $to"
    git mv "$from" "${from}_temp"
    git mv "${from}_temp" "$to"
    log_success "完成: $from -> $to"
}

cd "$ROOT_DIR"

# GFramework.Core.Tests
log_info "=== 处理 GFramework.Core.Tests ==="
rename_folder "GFramework.Core.Tests/functional/pipe" "GFramework.Core.Tests/functional/Pipe"
rename_folder "GFramework.Core.Tests/functional/functions" "GFramework.Core.Tests/functional/Functions"
rename_folder "GFramework.Core.Tests/functional/control" "GFramework.Core.Tests/functional/Control"
rename_folder "GFramework.Core.Tests/functional" "GFramework.Core.Tests/Functional"
rename_folder "GFramework.Core.Tests/time" "GFramework.Core.Tests/Time"
rename_folder "GFramework.Core.Tests/tests" "GFramework.Core.Tests/Tests"
rename_folder "GFramework.Core.Tests/system" "GFramework.Core.Tests/System"
rename_folder "GFramework.Core.Tests/state" "GFramework.Core.Tests/State"
rename_folder "GFramework.Core.Tests/rule" "GFramework.Core.Tests/Rule"
rename_folder "GFramework.Core.Tests/resource" "GFramework.Core.Tests/Resource"
rename_folder "GFramework.Core.Tests/query" "GFramework.Core.Tests/Query"
rename_folder "GFramework.Core.Tests/property" "GFramework.Core.Tests/Property"
rename_folder "GFramework.Core.Tests/pool" "GFramework.Core.Tests/Pool"
rename_folder "GFramework.Core.Tests/pause" "GFramework.Core.Tests/Pause"
rename_folder "GFramework.Core.Tests/model" "GFramework.Core.Tests/Model"
rename_folder "GFramework.Core.Tests/mediator" "GFramework.Core.Tests/Mediator"
rename_folder "GFramework.Core.Tests/logging" "GFramework.Core.Tests/Logging"
rename_folder "GFramework.Core.Tests/ioc" "GFramework.Core.Tests/IoC"
rename_folder "GFramework.Core.Tests/extensions" "GFramework.Core.Tests/Extensions"
rename_folder "GFramework.Core.Tests/events" "GFramework.Core.Tests/Events"
rename_folder "GFramework.Core.Tests/environment" "GFramework.Core.Tests/Environment"
rename_folder "GFramework.Core.Tests/coroutine" "GFramework.Core.Tests/Coroutine"
rename_folder "GFramework.Core.Tests/configuration" "GFramework.Core.Tests/Configuration"
rename_folder "GFramework.Core.Tests/command" "GFramework.Core.Tests/Command"
rename_folder "GFramework.Core.Tests/architecture" "GFramework.Core.Tests/Architecture"
git add -A
git commit -m "refactor(Core.Tests): 重命名文件夹为 PascalCase"
log_success "=== GFramework.Core.Tests 完成 ==="

# GFramework.Game.Abstractions
log_info "=== 处理 GFramework.Game.Abstractions ==="
rename_folder "GFramework.Game.Abstractions/data/events" "GFramework.Game.Abstractions/data/Events"
rename_folder "GFramework.Game.Abstractions/data" "GFramework.Game.Abstractions/Data"
rename_folder "GFramework.Game.Abstractions/setting/data" "GFramework.Game.Abstractions/setting/Data"
rename_folder "GFramework.Game.Abstractions/setting" "GFramework.Game.Abstractions/Setting"
rename_folder "GFramework.Game.Abstractions/ui" "GFramework.Game.Abstractions/UI"
rename_folder "GFramework.Game.Abstractions/storage" "GFramework.Game.Abstractions/Storage"
rename_folder "GFramework.Game.Abstractions/scene" "GFramework.Game.Abstractions/Scene"
rename_folder "GFramework.Game.Abstractions/internals" "GFramework.Game.Abstractions/Internals"
rename_folder "GFramework.Game.Abstractions/enums" "GFramework.Game.Abstractions/Enums"
rename_folder "GFramework.Game.Abstractions/asset" "GFramework.Game.Abstractions/Asset"
git add -A
git commit -m "refactor(Game.Abstractions): 重命名文件夹为 PascalCase"
log_success "=== GFramework.Game.Abstractions 完成 ==="

# GFramework.Game
log_info "=== 处理 GFramework.Game ==="
rename_folder "GFramework.Game/scene/handler" "GFramework.Game/scene/Handler"
rename_folder "GFramework.Game/scene" "GFramework.Game/Scene"
rename_folder "GFramework.Game/setting/events" "GFramework.Game/setting/Events"
rename_folder "GFramework.Game/setting" "GFramework.Game/Setting"
rename_folder "GFramework.Game/ui/handler" "GFramework.Game/ui/Handler"
rename_folder "GFramework.Game/ui" "GFramework.Game/UI"
rename_folder "GFramework.Game/storage" "GFramework.Game/Storage"
rename_folder "GFramework.Game/state" "GFramework.Game/State"
rename_folder "GFramework.Game/serializer" "GFramework.Game/Serializer"
rename_folder "GFramework.Game/extensions" "GFramework.Game/Extensions"
rename_folder "GFramework.Game/data" "GFramework.Game/Data"
git add -A
git commit -m "refactor(Game): 重命名文件夹为 PascalCase"
log_success "=== GFramework.Game 完成 ==="

# GFramework.Godot
log_info "=== 处理 GFramework.Godot ==="
rename_folder "GFramework.Godot/extensions/signal" "GFramework.Godot/extensions/Signal"
rename_folder "GFramework.Godot/extensions" "GFramework.Godot/Extensions"
rename_folder "GFramework.Godot/setting/data" "GFramework.Godot/setting/Data"
rename_folder "GFramework.Godot/setting" "GFramework.Godot/Setting"
rename_folder "GFramework.Godot/ui" "GFramework.Godot/UI"
rename_folder "GFramework.Godot/storage" "GFramework.Godot/Storage"
rename_folder "GFramework.Godot/scene" "GFramework.Godot/Scene"
rename_folder "GFramework.Godot/pool" "GFramework.Godot/Pool"
rename_folder "GFramework.Godot/pause" "GFramework.Godot/Pause"
rename_folder "GFramework.Godot/logging" "GFramework.Godot/Logging"
rename_folder "GFramework.Godot/data" "GFramework.Godot/Data"
rename_folder "GFramework.Godot/coroutine" "GFramework.Godot/Coroutine"
rename_folder "GFramework.Godot/architecture" "GFramework.Godot/Architecture"
git add -A
git commit -m "refactor(Godot): 重命名文件夹为 PascalCase"
log_success "=== GFramework.Godot 完成 ==="

# GFramework.Ecs.Arch
log_info "=== 处理 GFramework.Ecs.Arch ==="
rename_folder "GFramework.Ecs.Arch/systems" "GFramework.Ecs.Arch/Systems"
rename_folder "GFramework.Ecs.Arch/extensions" "GFramework.Ecs.Arch/Extensions"
rename_folder "GFramework.Ecs.Arch/components" "GFramework.Ecs.Arch/Components"
git add -A
git commit -m "refactor(Ecs.Arch): 重命名文件夹为 PascalCase"
log_success "=== GFramework.Ecs.Arch 完成 ==="

# GFramework.Ecs.Arch.Tests
log_info "=== 处理 GFramework.Ecs.Arch.Tests ==="
rename_folder "GFramework.Ecs.Arch.Tests/integration" "GFramework.Ecs.Arch.Tests/Integration"
rename_folder "GFramework.Ecs.Arch.Tests/ecs" "GFramework.Ecs.Arch.Tests/ECS"
git add -A
git commit -m "refactor(Ecs.Arch.Tests): 重命名文件夹为 PascalCase"
log_success "=== GFramework.Ecs.Arch.Tests 完成 ==="

# GFramework.SourceGenerators.Abstractions
log_info "=== 处理 GFramework.SourceGenerators.Abstractions ==="
rename_folder "GFramework.SourceGenerators.Abstractions/rule" "GFramework.SourceGenerators.Abstractions/Rule"
rename_folder "GFramework.SourceGenerators.Abstractions/logging" "GFramework.SourceGenerators.Abstractions/Logging"
rename_folder "GFramework.SourceGenerators.Abstractions/enums" "GFramework.SourceGenerators.Abstractions/Enums"
rename_folder "GFramework.SourceGenerators.Abstractions/bases" "GFramework.SourceGenerators.Abstractions/Bases"
git add -A
git commit -m "refactor(SourceGenerators.Abstractions): 重命名文件夹为 PascalCase"
log_success "=== GFramework.SourceGenerators.Abstractions 完成 ==="

# GFramework.SourceGenerators.Common
log_info "=== 处理 GFramework.SourceGenerators.Common ==="
rename_folder "GFramework.SourceGenerators.Common/info" "GFramework.SourceGenerators.Common/Info"
rename_folder "GFramework.SourceGenerators.Common/generator" "GFramework.SourceGenerators.Common/Generator"
rename_folder "GFramework.SourceGenerators.Common/extensions" "GFramework.SourceGenerators.Common/Extensions"
rename_folder "GFramework.SourceGenerators.Common/diagnostics" "GFramework.SourceGenerators.Common/Diagnostics"
rename_folder "GFramework.SourceGenerators.Common/constants" "GFramework.SourceGenerators.Common/Constants"
git add -A
git commit -m "refactor(SourceGenerators.Common): 重命名文件夹为 PascalCase"
log_success "=== GFramework.SourceGenerators.Common 完成 ==="

# GFramework.SourceGenerators
log_info "=== 处理 GFramework.SourceGenerators ==="
rename_folder "GFramework.SourceGenerators/rule" "GFramework.SourceGenerators/Rule"
rename_folder "GFramework.SourceGenerators/logging" "GFramework.SourceGenerators/Logging"
rename_folder "GFramework.SourceGenerators/enums" "GFramework.SourceGenerators/Enums"
rename_folder "GFramework.SourceGenerators/diagnostics" "GFramework.SourceGenerators/Diagnostics"
rename_folder "GFramework.SourceGenerators/bases" "GFramework.SourceGenerators/Bases"
rename_folder "GFramework.SourceGenerators/analyzers" "GFramework.SourceGenerators/Analyzers"
git add -A
git commit -m "refactor(SourceGenerators): 重命名文件夹为 PascalCase"
log_success "=== GFramework.SourceGenerators 完成 ==="

# GFramework.SourceGenerators.Tests
log_info "=== 处理 GFramework.SourceGenerators.Tests ==="
rename_folder "GFramework.SourceGenerators.Tests/rule" "GFramework.SourceGenerators.Tests/Rule"
rename_folder "GFramework.SourceGenerators.Tests/logging" "GFramework.SourceGenerators.Tests/Logging"
rename_folder "GFramework.SourceGenerators.Tests/enums" "GFramework.SourceGenerators.Tests/Enums"
rename_folder "GFramework.SourceGenerators.Tests/core" "GFramework.SourceGenerators.Tests/Core"
rename_folder "GFramework.SourceGenerators.Tests/bases" "GFramework.SourceGenerators.Tests/Bases"
git add -A
git commit -m "refactor(SourceGenerators.Tests): 重命名文件夹为 PascalCase"
log_success "=== GFramework.SourceGenerators.Tests 完成 ==="

log_success "=== 所有项目文件夹重命名完成 ==="
