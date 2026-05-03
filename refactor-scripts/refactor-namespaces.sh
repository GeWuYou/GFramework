#!/bin/bash
# Copyright (c) 2025-2026 GeWuYou
# SPDX-License-Identifier: Apache-2.0

#
# GFramework 命名空间重构脚本 - 将所有文件夹和命名空间从小写改为 PascalCase
#
# 用法:
#   ./refactor-namespaces.sh [phase] [--dry-run] [--skip-tests]
#
# 参数:
#   phase: 1=文件夹重命名, 2=命名空间更新, 3=文档更新, 4=验证, all=全部（默认）
#   --dry-run: 干运行模式，只显示将要执行的操作
#   --skip-tests: 跳过测试验证

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
MAPPING_FILE="$SCRIPT_DIR/folder-mappings.json"

PHASE="${1:-all}"
DRY_RUN=false
SKIP_TESTS=false

# 解析参数
for arg in "$@"; do
    case $arg in
        --dry-run)
            DRY_RUN=true
            ;;
        --skip-tests)
            SKIP_TESTS=true
            ;;
    esac
done

# 颜色输出
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

log_info() {
    echo -e "${CYAN}ℹ $1${NC}"
}

log_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

log_warning() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

log_error() {
    echo -e "${RED}✗ $1${NC}"
}

# 阶段 1: 文件夹重命名
phase1_folder_rename() {
    log_info "=== 阶段 1: 文件夹重命名 ==="

    # 读取配置并处理每个项目
    local projects=$(jq -r '.projects[] | @base64' "$MAPPING_FILE")
    local total_folders=0

    for project_base64 in $projects; do
        local project=$(echo "$project_base64" | base64 --decode)
        local project_name=$(echo "$project" | jq -r '.name')
        local project_path=$(echo "$project" | jq -r '.path')

        log_info "处理项目: $project_name"

        local full_project_path="$ROOT_DIR/$project_path"

        if [ ! -d "$full_project_path" ]; then
            log_warning "项目路径不存在: $full_project_path"
            continue
        fi

        # 获取文件夹列表并按深度排序（深度优先）
        local folders=$(echo "$project" | jq -r '.folders[] | @base64')

        # 先收集所有文件夹并按深度排序
        declare -a folder_array
        for folder_base64 in $folders; do
            local folder=$(echo "$folder_base64" | base64 --decode)
            local from=$(echo "$folder" | jq -r '.from')
            local depth=$(echo "$from" | tr -cd '/' | wc -c)
            folder_array+=("$depth|$folder_base64")
        done

        # 按深度降序排序
        IFS=$'\n' sorted_folders=($(sort -rn <<<"${folder_array[*]}"))
        unset IFS

        for item in "${sorted_folders[@]}"; do
            local folder_base64="${item#*|}"
            local folder=$(echo "$folder_base64" | base64 --decode)
            local from=$(echo "$folder" | jq -r '.from')
            local to=$(echo "$folder" | jq -r '.to')

            local from_path="$full_project_path/$from"
            local to_path="$full_project_path/$to"

            if [ ! -d "$from_path" ]; then
                log_warning "源文件夹不存在: $from_path"
                continue
            fi

            if [ "$from_path" = "$to_path" ]; then
                log_info "跳过（路径相同）: $from"
                continue
            fi

            # Windows/WSL 文件系统不区分大小写，需要两步重命名
            local temp_path="${from_path}_temp"

            if [ "$DRY_RUN" = true ]; then
                log_info "[DRY RUN] git mv $from_path $temp_path"
                log_info "[DRY RUN] git mv $temp_path $to_path"
            else
                log_info "重命名: $from -> $to"

                # 第一步：重命名为临时名称
                git mv "$from_path" "$temp_path" || {
                    log_error "git mv 失败: $from_path -> $temp_path"
                    exit 1
                }

                # 第二步：重命名为目标名称
                git mv "$temp_path" "$to_path" || {
                    log_error "git mv 失败: $temp_path -> $to_path"
                    exit 1
                }

                ((total_folders++))
                log_success "完成: $from -> $to"
            fi
        done

        if [ "$DRY_RUN" = false ]; then
            log_info "提交项目 $project_name 的文件夹重命名"
            git add -A
            git commit -m "refactor($project_name): 重命名文件夹为 PascalCase"
        fi
    done

    log_success "阶段 1 完成: 共重命名 $total_folders 个文件夹"
}

# 阶段 2: 命名空间更新
phase2_namespace_update() {
    log_info "=== 阶段 2: 命名空间更新 ==="

    # 查找所有 C# 文件
    local cs_files=$(find "$ROOT_DIR" -name "*.cs" -type f | grep -v "/bin/" | grep -v "/obj/" | grep -v "/Generated/")
    local file_count=$(echo "$cs_files" | wc -l)

    log_info "找到 $file_count 个 C# 文件"

    local updated_files=0
    local total_replacements=0

    # 定义命名空间替换规则（按优先级排序，长的先匹配）
    declare -a patterns=(
        "\.cqrs\.notification\b|.CQRS.Notification"
        "\.cqrs\.command\b|.CQRS.Command"
        "\.cqrs\.request\b|.CQRS.Request"
        "\.cqrs\.query\b|.CQRS.Query"
        "\.cqrs\.behaviors\b|.CQRS.Behaviors"
        "\.cqrs\b|.CQRS"
        "\.coroutine\.instructions\b|.Coroutine.Instructions"
        "\.coroutine\.extensions\b|.Coroutine.Extensions"
        "\.coroutine\b|.Coroutine"
        "\.events\.filters\b|.Events.Filters"
        "\.events\b|.Events"
        "\.logging\.appenders\b|.Logging.Appenders"
        "\.logging\.filters\b|.Logging.Filters"
        "\.logging\.formatters\b|.Logging.Formatters"
        "\.logging\b|.Logging"
        "\.functional\.async\b|.Functional.Async"
        "\.functional\.control\b|.Functional.Control"
        "\.functional\.functions\b|.Functional.Functions"
        "\.functional\.pipe\b|.Functional.Pipe"
        "\.functional\.result\b|.Functional.Result"
        "\.functional\b|.Functional"
        "\.services\.modules\b|.Services.Modules"
        "\.services\b|.Services"
        "\.architecture\b|.Architecture"
        "\.bases\b|.Bases"
        "\.command\b|.Command"
        "\.configuration\b|.Configuration"
        "\.constants\b|.Constants"
        "\.data\b|.Data"
        "\.enums\b|.Enums"
        "\.environment\b|.Environment"
        "\.extensions\b|.Extensions"
        "\.internals\b|.Internals"
        "\.ioc\b|.IoC"
        "\.lifecycle\b|.Lifecycle"
        "\.model\b|.Model"
        "\.pause\b|.Pause"
        "\.pool\b|.Pool"
        "\.properties\b|.Properties"
        "\.property\b|.Property"
        "\.query\b|.Query"
        "\.registries\b|.Registries"
        "\.resource\b|.Resource"
        "\.rule\b|.Rule"
        "\.serializer\b|.Serializer"
        "\.state\b|.State"
        "\.storage\b|.Storage"
        "\.system\b|.System"
        "\.time\b|.Time"
        "\.utility\b|.Utility"
        "\.versioning\b|.Versioning"
        "\.asset\b|.Asset"
        "\.scene\b|.Scene"
        "\.setting\b|.Setting"
        "\.ui\b|.UI"
        "\.components\b|.Components"
        "\.systems\b|.Systems"
        "\.ecs\b|.ECS"
        "\.integration\b|.Integration"
        "\.mediator\b|.Mediator"
        "\.tests\b|.Tests"
        "\.analyzers\b|.Analyzers"
        "\.diagnostics\b|.Diagnostics"
        "\.generator\b|.Generator"
        "\.info\b|.Info"
    )

    while IFS= read -r file; do
        [ -z "$file" ] && continue

        local file_changed=false
        local file_replacements=0
        local temp_file="${file}.tmp"

        cp "$file" "$temp_file"

        for pattern_pair in "${patterns[@]}"; do
            local pattern="${pattern_pair%%|*}"
            local replacement="${pattern_pair##*|}"

            # 使用 sed 进行替换（不区分大小写）
            if grep -qi "$pattern" "$temp_file"; then
                sed -i "s/$pattern/$replacement/gI" "$temp_file"
                file_changed=true
                ((file_replacements++))
            fi
        done

        if [ "$file_changed" = true ]; then
            if [ "$DRY_RUN" = true ]; then
                log_info "[DRY RUN] 更新文件: $file ($file_replacements 处替换)"
                rm "$temp_file"
            else
                mv "$temp_file" "$file"
                ((updated_files++))
                ((total_replacements+=file_replacements))
                log_info "更新: $(basename "$file") ($file_replacements 处替换)"
            fi
        else
            rm "$temp_file"
        fi
    done <<< "$cs_files"

    if [ "$DRY_RUN" = false ]; then
        log_info "提交命名空间更新"
        git add -A
        git commit -m "refactor: 更新所有命名空间为 PascalCase"
    fi

    log_success "阶段 2 完成: 更新了 $updated_files 个文件，共 $total_replacements 处替换"
}

# 阶段 3: 文档更新
phase3_documentation_update() {
    log_info "=== 阶段 3: 文档更新 ==="

    # 查找所有 Markdown 文件
    local md_files=$(find "$ROOT_DIR" -name "*.md" -type f | grep -v "/node_modules/" | grep -v "/bin/" | grep -v "/obj/")
    local file_count=$(echo "$md_files" | wc -l)

    log_info "找到 $file_count 个 Markdown 文件"

    local updated_files=0
    local total_replacements=0

    # 使用与阶段 2 相同的替换规则
    declare -a patterns=(
        "\.cqrs\.notification\b|.CQRS.Notification"
        "\.cqrs\.command\b|.CQRS.Command"
        "\.cqrs\.request\b|.CQRS.Request"
        "\.cqrs\.query\b|.CQRS.Query"
        "\.cqrs\.behaviors\b|.CQRS.Behaviors"
        "\.cqrs\b|.CQRS"
        "\.coroutine\.instructions\b|.Coroutine.Instructions"
        "\.coroutine\.extensions\b|.Coroutine.Extensions"
        "\.coroutine\b|.Coroutine"
        "\.events\.filters\b|.Events.Filters"
        "\.events\b|.Events"
        "\.logging\.appenders\b|.Logging.Appenders"
        "\.logging\.filters\b|.Logging.Filters"
        "\.logging\.formatters\b|.Logging.Formatters"
        "\.logging\b|.Logging"
        "\.functional\.async\b|.Functional.Async"
        "\.functional\.control\b|.Functional.Control"
        "\.functional\.functions\b|.Functional.Functions"
        "\.functional\.pipe\b|.Functional.Pipe"
        "\.functional\.result\b|.Functional.Result"
        "\.functional\b|.Functional"
        "\.services\.modules\b|.Services.Modules"
        "\.services\b|.Services"
        "\.architecture\b|.Architecture"
        "\.command\b|.Command"
        "\.configuration\b|.Configuration"
        "\.environment\b|.Environment"
        "\.extensions\b|.Extensions"
        "\.ioc\b|.IoC"
        "\.logging\b|.Logging"
        "\.model\b|.Model"
        "\.query\b|.Query"
        "\.resource\b|.Resource"
        "\.state\b|.State"
        "\.system\b|.System"
        "\.utility\b|.Utility"
    )

    while IFS= read -r file; do
        [ -z "$file" ] && continue

        local file_changed=false
        local file_replacements=0
        local temp_file="${file}.tmp"

        cp "$file" "$temp_file"

        for pattern_pair in "${patterns[@]}"; do
            local pattern="${pattern_pair%%|*}"
            local replacement="${pattern_pair##*|}"

            if grep -qi "$pattern" "$temp_file"; then
                sed -i "s/$pattern/$replacement/gI" "$temp_file"
                file_changed=true
                ((file_replacements++))
            fi
        done

        if [ "$file_changed" = true ]; then
            if [ "$DRY_RUN" = true ]; then
                log_info "[DRY RUN] 更新文档: $file ($file_replacements 处替换)"
                rm "$temp_file"
            else
                mv "$temp_file" "$file"
                ((updated_files++))
                ((total_replacements+=file_replacements))
                log_info "更新: $(basename "$file") ($file_replacements 处替换)"
            fi
        else
            rm "$temp_file"
        fi
    done <<< "$md_files"

    if [ "$DRY_RUN" = false ]; then
        log_info "提交文档更新"
        git add -A
        git commit -m "docs: 更新文档中的命名空间为 PascalCase"
    fi

    log_success "阶段 3 完成: 更新了 $updated_files 个文档，共 $total_replacements 处替换"
}

# 阶段 4: 验证
phase4_verification() {
    log_info "=== 阶段 4: 验证 ==="

    # 1. 编译验证
    log_info "1. 编译验证..."
    if [ "$DRY_RUN" = true ]; then
        log_info "[DRY RUN] dotnet build"
    else
        cd "$ROOT_DIR"
        if dotnet build --no-restore; then
            log_success "编译成功"
        else
            log_error "编译失败"
            exit 1
        fi
    fi

    # 2. 测试验证
    if [ "$SKIP_TESTS" = false ]; then
        log_info "2. 测试验证..."
        if [ "$DRY_RUN" = true ]; then
            log_info "[DRY RUN] dotnet test"
        else
            cd "$ROOT_DIR"
            if dotnet test --no-build; then
                log_success "所有测试通过"
            else
                log_error "测试失败"
                exit 1
            fi
        fi
    else
        log_warning "跳过测试验证"
    fi

    # 3. 检查残留的小写命名空间
    log_info "3. 检查残留的小写命名空间..."
    local cs_files=$(find "$ROOT_DIR" -name "*.cs" -type f | grep -v "/bin/" | grep -v "/obj/" | grep -v "/Generated/")

    local lowercase_patterns=(
        "\.architecture\b"
        "\.command\b"
        "\.configuration\b"
        "\.coroutine\b"
        "\.cqrs\b"
        "\.events\b"
        "\.extensions\b"
        "\.functional\b"
        "\.ioc\b"
        "\.logging\b"
        "\.model\b"
        "\.query\b"
        "\.resource\b"
        "\.state\b"
        "\.system\b"
        "\.utility\b"
    )

    local found_issues=0
    while IFS= read -r file; do
        [ -z "$file" ] && continue

        for pattern in "${lowercase_patterns[@]}"; do
            if grep -qi "$pattern" "$file"; then
                log_warning "$file: 找到小写命名空间 $pattern"
                ((found_issues++))
            fi
        done
    done <<< "$cs_files"

    if [ $found_issues -gt 0 ]; then
        log_warning "发现 $found_issues 个残留的小写命名空间"
    else
        log_success "未发现残留的小写命名空间"
    fi

    log_success "阶段 4 完成: 验证通过"
}

# 主执行逻辑
main() {
    log_info "GFramework 命名空间重构脚本"
    log_info "工作目录: $ROOT_DIR"
    log_info "配置文件: $MAPPING_FILE"

    if [ "$DRY_RUN" = true ]; then
        log_warning "*** 干运行模式 - 不会执行实际操作 ***"
    fi

    if [ ! -f "$MAPPING_FILE" ]; then
        log_error "配置文件不存在: $MAPPING_FILE"
        exit 1
    fi

    case "$PHASE" in
        1)
            phase1_folder_rename
            ;;
        2)
            phase2_namespace_update
            ;;
        3)
            phase3_documentation_update
            ;;
        4)
            phase4_verification
            ;;
        all)
            phase1_folder_rename
            phase2_namespace_update
            phase3_documentation_update
            phase4_verification
            ;;
        *)
            log_error "未知阶段: $PHASE"
            log_info "用法: $0 [1|2|3|4|all] [--dry-run] [--skip-tests]"
            exit 1
            ;;
    esac

    log_success "=== 重构完成 ==="
}

main
