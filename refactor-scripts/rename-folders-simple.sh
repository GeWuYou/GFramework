#!/bin/bash
# Copyright (c) 2025-2026 GeWuYou
# SPDX-License-Identifier: Apache-2.0

# 简化版文件夹重命名脚本 - 一次处理一个项目

set -e

ROOT_DIR="/mnt/f/gewuyou/System/Documents/WorkSpace/GameDev/GFramework"

# 颜色输出
GREEN='\033[0;32m'
CYAN='\033[0;36m'
NC='\033[0m'

log_info() { echo -e "${CYAN}ℹ $1${NC}"; }
log_success() { echo -e "${GREEN}✓ $1${NC}"; }

# 重命名单个文件夹（两步法）
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

    # 两步重命名
    git mv "$from" "${from}_temp"
    git mv "${from}_temp" "$to"

    log_success "完成: $from -> $to"
}

cd "$ROOT_DIR"

# GFramework.Core.Abstractions
log_info "=== 处理 GFramework.Core.Abstractions ==="

# 先处理嵌套文件夹（深度优先）
rename_folder "GFramework.Core.Abstractions/cqrs/request" "GFramework.Core.Abstractions/cqrs/Request"
rename_folder "GFramework.Core.Abstractions/cqrs/query" "GFramework.Core.Abstractions/cqrs/Query"
rename_folder "GFramework.Core.Abstractions/cqrs/notification" "GFramework.Core.Abstractions/cqrs/Notification"
rename_folder "GFramework.Core.Abstractions/cqrs/command" "GFramework.Core.Abstractions/cqrs/Command"

# 然后处理父文件夹
rename_folder "GFramework.Core.Abstractions/cqrs" "GFramework.Core.Abstractions/CQRS"

# 其他文件夹
rename_folder "GFramework.Core.Abstractions/versioning" "GFramework.Core.Abstractions/Versioning"
rename_folder "GFramework.Core.Abstractions/utility" "GFramework.Core.Abstractions/Utility"
rename_folder "GFramework.Core.Abstractions/time" "GFramework.Core.Abstractions/Time"
rename_folder "GFramework.Core.Abstractions/system" "GFramework.Core.Abstractions/System"
rename_folder "GFramework.Core.Abstractions/storage" "GFramework.Core.Abstractions/Storage"
rename_folder "GFramework.Core.Abstractions/state" "GFramework.Core.Abstractions/State"
rename_folder "GFramework.Core.Abstractions/serializer" "GFramework.Core.Abstractions/Serializer"
rename_folder "GFramework.Core.Abstractions/rule" "GFramework.Core.Abstractions/Rule"
rename_folder "GFramework.Core.Abstractions/resource" "GFramework.Core.Abstractions/Resource"
rename_folder "GFramework.Core.Abstractions/registries" "GFramework.Core.Abstractions/Registries"
rename_folder "GFramework.Core.Abstractions/query" "GFramework.Core.Abstractions/Query"
rename_folder "GFramework.Core.Abstractions/property" "GFramework.Core.Abstractions/Property"
rename_folder "GFramework.Core.Abstractions/properties" "GFramework.Core.Abstractions/Properties"
rename_folder "GFramework.Core.Abstractions/pool" "GFramework.Core.Abstractions/Pool"
rename_folder "GFramework.Core.Abstractions/pause" "GFramework.Core.Abstractions/Pause"
rename_folder "GFramework.Core.Abstractions/model" "GFramework.Core.Abstractions/Model"
rename_folder "GFramework.Core.Abstractions/logging" "GFramework.Core.Abstractions/Logging"
rename_folder "GFramework.Core.Abstractions/lifecycle" "GFramework.Core.Abstractions/Lifecycle"
rename_folder "GFramework.Core.Abstractions/ioc" "GFramework.Core.Abstractions/IoC"
rename_folder "GFramework.Core.Abstractions/internals" "GFramework.Core.Abstractions/Internals"
rename_folder "GFramework.Core.Abstractions/events" "GFramework.Core.Abstractions/Events"
rename_folder "GFramework.Core.Abstractions/environment" "GFramework.Core.Abstractions/Environment"
rename_folder "GFramework.Core.Abstractions/enums" "GFramework.Core.Abstractions/Enums"
rename_folder "GFramework.Core.Abstractions/data" "GFramework.Core.Abstractions/Data"
rename_folder "GFramework.Core.Abstractions/coroutine" "GFramework.Core.Abstractions/Coroutine"
rename_folder "GFramework.Core.Abstractions/configuration" "GFramework.Core.Abstractions/Configuration"
rename_folder "GFramework.Core.Abstractions/command" "GFramework.Core.Abstractions/Command"
rename_folder "GFramework.Core.Abstractions/bases" "GFramework.Core.Abstractions/Bases"
rename_folder "GFramework.Core.Abstractions/architecture" "GFramework.Core.Abstractions/Architecture"

git add -A
git commit -m "refactor(Core.Abstractions): 重命名文件夹为 PascalCase"

log_success "=== GFramework.Core.Abstractions 完成 ==="
