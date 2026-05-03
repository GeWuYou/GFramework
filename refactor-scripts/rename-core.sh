#!/bin/bash
# Copyright (c) 2025-2026 GeWuYou
# SPDX-License-Identifier: Apache-2.0

# 重命名 GFramework.Core 文件夹

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

log_info "=== 处理 GFramework.Core ==="

# 嵌套文件夹（深度优先）
rename_folder "GFramework.Core/cqrs/request" "GFramework.Core/cqrs/Request"
rename_folder "GFramework.Core/cqrs/query" "GFramework.Core/cqrs/Query"
rename_folder "GFramework.Core/cqrs/notification" "GFramework.Core/cqrs/Notification"
rename_folder "GFramework.Core/cqrs/command" "GFramework.Core/cqrs/Command"
rename_folder "GFramework.Core/cqrs/behaviors" "GFramework.Core/cqrs/Behaviors"
rename_folder "GFramework.Core/cqrs" "GFramework.Core/CQRS"

rename_folder "GFramework.Core/coroutine/instructions" "GFramework.Core/coroutine/Instructions"
rename_folder "GFramework.Core/coroutine/extensions" "GFramework.Core/coroutine/Extensions"
rename_folder "GFramework.Core/coroutine" "GFramework.Core/Coroutine"

rename_folder "GFramework.Core/events/filters" "GFramework.Core/events/Filters"
rename_folder "GFramework.Core/events" "GFramework.Core/Events"

rename_folder "GFramework.Core/functional/result" "GFramework.Core/functional/Result"
rename_folder "GFramework.Core/functional/pipe" "GFramework.Core/functional/Pipe"
rename_folder "GFramework.Core/functional/functions" "GFramework.Core/functional/Functions"
rename_folder "GFramework.Core/functional/control" "GFramework.Core/functional/Control"
rename_folder "GFramework.Core/functional/async" "GFramework.Core/functional/Async"
rename_folder "GFramework.Core/functional" "GFramework.Core/Functional"

rename_folder "GFramework.Core/logging/formatters" "GFramework.Core/logging/Formatters"
rename_folder "GFramework.Core/logging/filters" "GFramework.Core/logging/Filters"
rename_folder "GFramework.Core/logging/appenders" "GFramework.Core/logging/Appenders"
rename_folder "GFramework.Core/logging" "GFramework.Core/Logging"

rename_folder "GFramework.Core/services/modules" "GFramework.Core/services/Modules"
rename_folder "GFramework.Core/services" "GFramework.Core/Services"

# 单层文件夹
rename_folder "GFramework.Core/utility" "GFramework.Core/Utility"
rename_folder "GFramework.Core/time" "GFramework.Core/Time"
rename_folder "GFramework.Core/system" "GFramework.Core/System"
rename_folder "GFramework.Core/state" "GFramework.Core/State"
rename_folder "GFramework.Core/rule" "GFramework.Core/Rule"
rename_folder "GFramework.Core/resource" "GFramework.Core/Resource"
rename_folder "GFramework.Core/query" "GFramework.Core/Query"
rename_folder "GFramework.Core/property" "GFramework.Core/Property"
rename_folder "GFramework.Core/pool" "GFramework.Core/Pool"
rename_folder "GFramework.Core/pause" "GFramework.Core/Pause"
rename_folder "GFramework.Core/model" "GFramework.Core/Model"
rename_folder "GFramework.Core/ioc" "GFramework.Core/IoC"
rename_folder "GFramework.Core/extensions" "GFramework.Core/Extensions"
rename_folder "GFramework.Core/environment" "GFramework.Core/Environment"
rename_folder "GFramework.Core/constants" "GFramework.Core/Constants"
rename_folder "GFramework.Core/configuration" "GFramework.Core/Configuration"
rename_folder "GFramework.Core/command" "GFramework.Core/Command"
rename_folder "GFramework.Core/architecture" "GFramework.Core/Architecture"

git add -A
git commit -m "refactor(Core): 重命名文件夹为 PascalCase"

log_success "=== GFramework.Core 完成 ==="
