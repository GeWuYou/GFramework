#!/usr/bin/env python3
# Copyright (c) 2025-2026 GeWuYou
# SPDX-License-Identifier: Apache-2.0

"""
更新所有 C# 文件中的命名空间声明和 using 语句
"""

import os
import re
import sys
from argparse import ArgumentParser

DEFAULT_ROOT_DIR = os.getcwd()

# 命名空间替换规则（按优先级排序，长的先匹配）
NAMESPACE_RULES = [
    # CQRS 子命名空间
    (r'\.cqrs\.notification\b', '.CQRS.Notification'),
    (r'\.cqrs\.command\b', '.CQRS.Command'),
    (r'\.cqrs\.request\b', '.CQRS.Request'),
    (r'\.cqrs\.query\b', '.CQRS.Query'),
    (r'\.cqrs\.behaviors\b', '.CQRS.Behaviors'),
    (r'\.cqrs\b', '.CQRS'),

    # 嵌套命名空间
    (r'\.coroutine\.instructions\b', '.Coroutine.Instructions'),
    (r'\.coroutine\.extensions\b', '.Coroutine.Extensions'),
    (r'\.coroutine\b', '.Coroutine'),

    (r'\.events\.filters\b', '.Events.Filters'),
    (r'\.events\b', '.Events'),

    (r'\.logging\.appenders\b', '.Logging.Appenders'),
    (r'\.logging\.filters\b', '.Logging.Filters'),
    (r'\.logging\.formatters\b', '.Logging.Formatters'),
    (r'\.logging\b', '.Logging'),

    (r'\.functional\.async\b', '.Functional.Async'),
    (r'\.functional\.control\b', '.Functional.Control'),
    (r'\.functional\.functions\b', '.Functional.Functions'),
    (r'\.functional\.pipe\b', '.Functional.Pipe'),
    (r'\.functional\.result\b', '.Functional.Result'),
    (r'\.functional\b', '.Functional'),

    (r'\.services\.modules\b', '.Services.Modules'),
    (r'\.services\b', '.Services'),

    (r'\.extensions\.signal\b', '.Extensions.Signal'),
    (r'\.extensions\b', '.Extensions'),

    (r'\.setting\.data\b', '.Setting.Data'),
    (r'\.setting\.events\b', '.Setting.Events'),
    (r'\.setting\b', '.Setting'),

    (r'\.scene\.handler\b', '.Scene.Handler'),
    (r'\.scene\b', '.Scene'),

    (r'\.ui\.handler\b', '.UI.Handler'),
    (r'\.ui\b', '.UI'),

    (r'\.data\.events\b', '.Data.Events'),
    (r'\.data\b', '.Data'),

    # 单层命名空间
    (r'\.architecture\b', '.Architecture'),
    (r'\.bases\b', '.Bases'),
    (r'\.command\b', '.Command'),
    (r'\.configuration\b', '.Configuration'),
    (r'\.constants\b', '.Constants'),
    (r'\.enums\b', '.Enums'),
    (r'\.environment\b', '.Environment'),
    (r'\.internals\b', '.Internals'),
    (r'\.ioc\b', '.IoC'),
    (r'\.lifecycle\b', '.Lifecycle'),
    (r'\.model\b', '.Model'),
    (r'\.pause\b', '.Pause'),
    (r'\.pool\b', '.Pool'),
    (r'\.properties\b', '.Properties'),
    (r'\.property\b', '.Property'),
    (r'\.query\b', '.Query'),
    (r'\.registries\b', '.Registries'),
    (r'\.resource\b', '.Resource'),
    (r'\.rule\b', '.Rule'),
    (r'\.serializer\b', '.Serializer'),
    (r'\.state\b', '.State'),
    (r'\.storage\b', '.Storage'),
    (r'\.system\b', '.System'),
    (r'\.time\b', '.Time'),
    (r'\.utility\b', '.Utility'),
    (r'\.versioning\b', '.Versioning'),
    (r'\.asset\b', '.Asset'),
    (r'\.components\b', '.Components'),
    (r'\.systems\b', '.Systems'),
    (r'\.ecs\b', '.ECS'),
    (r'\.integration\b', '.Integration'),
    (r'\.mediator\b', '.Mediator'),
    (r'\.tests\b', '.Tests'),
    (r'\.analyzers\b', '.Analyzers'),
    (r'\.diagnostics\b', '.Diagnostics'),
    (r'\.generator\b', '.Generator'),
    (r'\.info\b', '.Info'),
]

def update_file(file_path):
    """更新单个文件中的命名空间"""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()

        original_content = content
        replacements = 0

        for pattern, replacement in NAMESPACE_RULES:
            matches = re.findall(pattern, content, re.IGNORECASE)
            if matches:
                content = re.sub(pattern, replacement, content, flags=re.IGNORECASE)
                replacements += len(matches)

        if content != original_content:
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
            return replacements

        return 0
    except Exception as e:
        raise RuntimeError(f"错误处理文件 {file_path}: {e}") from e

def main():
    parser = ArgumentParser(description="更新 C# 文件中的命名空间声明和 using 语句")
    parser.add_argument(
        "--root-dir",
        default=os.getenv("ROOT_DIR", DEFAULT_ROOT_DIR),
        help="要扫描的仓库根目录，默认使用 ROOT_DIR 环境变量或当前工作目录")
    args = parser.parse_args()
    root_dir = os.path.abspath(args.root_dir)

    if not os.path.isdir(root_dir):
        print(f"根目录不存在或不是目录: {root_dir}", file=sys.stderr)
        return 2

    print("开始更新命名空间...")

    # 查找所有 C# 文件
    cs_files = []
    for root, dirs, files in os.walk(root_dir):
        # 跳过 bin, obj, Generated 目录
        dirs[:] = [d for d in dirs if d not in ['bin', 'obj', 'Generated', '.git', 'node_modules']]

        for file in files:
            if file.endswith('.cs'):
                cs_files.append(os.path.join(root, file))

    print(f"找到 {len(cs_files)} 个 C# 文件")

    updated_files = 0
    total_replacements = 0
    failed_files = []

    for file_path in cs_files:
        try:
            replacements = update_file(file_path)
        except RuntimeError as e:
            failed_files.append((file_path, str(e)))
            continue

        if replacements > 0:
            updated_files += 1
            total_replacements += replacements
            print(f"更新: {os.path.basename(file_path)} ({replacements} 处替换)")

    print(f"\n完成！更新了 {updated_files} 个文件，共 {total_replacements} 处替换")
    if failed_files:
        print(f"失败文件数: {len(failed_files)}", file=sys.stderr)
        for file_path, error in failed_files:
            print(f"- {file_path}: {error}", file=sys.stderr)
        return 1

    return 0

if __name__ == '__main__':
    sys.exit(main())
