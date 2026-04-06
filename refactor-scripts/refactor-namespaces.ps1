#!/usr/bin/env pwsh
<#
.SYNOPSIS
    GFramework 命名空间重构脚本 - 将所有文件夹和命名空间从小写改为 PascalCase
.DESCRIPTION
    此脚本执行以下操作：
    1. 重命名文件夹（使用 git mv 保留历史）
    2. 更新所有 C# 文件中的命名空间声明和 using 语句
    3. 更新文档中的命名空间引用
    4. 验证更改的完整性
.PARAMETER Phase
    指定要执行的阶段：
    - 1: 文件夹重命名
    - 2: 命名空间更新
    - 3: 文档更新
    - 4: 验证
    - All: 执行所有阶段（默认）
.PARAMETER DryRun
    干运行模式，只显示将要执行的操作，不实际执行
.PARAMETER SkipTests
    跳过测试验证
.EXAMPLE
    ./refactor-namespaces.ps1 -Phase 1
    ./refactor-namespaces.ps1 -DryRun
    ./refactor-namespaces.ps1 -Phase All -SkipTests
#>

param(
    [Parameter()]
    [ValidateSet("1", "2", "3", "4", "All")]
    [string]$Phase = "All",

    [Parameter()]
    [switch]$DryRun,

    [Parameter()]
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir
$mappingFile = Join-Path $scriptDir "folder-mappings.json"

# 颜色输出函数
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Write-Success { param([string]$Message) Write-ColorOutput "✓ $Message" "Green" }
function Write-Info { param([string]$Message) Write-ColorOutput "ℹ $Message" "Cyan" }
function Write-Warning { param([string]$Message) Write-ColorOutput "⚠ $Message" "Yellow" }
function Write-Error { param([string]$Message) Write-ColorOutput "✗ $Message" "Red" }

# 阶段 1: 文件夹重命名
function Invoke-FolderRename {
    Write-Info "=== 阶段 1: 文件夹重命名 ==="

    $config = Get-Content $mappingFile | ConvertFrom-Json
    $totalFolders = 0

    foreach ($project in $config.projects) {
        Write-Info "处理项目: $($project.name)"
        $projectPath = Join-Path $rootDir $project.path

        if (-not (Test-Path $projectPath)) {
            Write-Warning "项目路径不存在: $projectPath"
            continue
        }

        # 按深度排序（深度优先，避免路径冲突）
        $sortedFolders = $project.folders | Sort-Object { ($_.from -split '/').Count } -Descending

        foreach ($folder in $sortedFolders) {
            $fromPath = Join-Path $projectPath $folder.from
            $toPath = Join-Path $projectPath $folder.to

            if (-not (Test-Path $fromPath)) {
                Write-Warning "源文件夹不存在: $fromPath"
                continue
            }

            if ($fromPath -eq $toPath) {
                Write-Info "跳过（路径相同）: $($folder.from)"
                continue
            }

            # Windows 文件系统不区分大小写，需要两步重命名
            $tempPath = "$fromPath`_temp"

            if ($DryRun) {
                Write-Info "[DRY RUN] git mv $fromPath $tempPath"
                Write-Info "[DRY RUN] git mv $tempPath $toPath"
            } else {
                try {
                    Write-Info "重命名: $($folder.from) -> $($folder.to)"

                    # 第一步：重命名为临时名称
                    git mv $fromPath $tempPath
                    if ($LASTEXITCODE -ne 0) {
                        throw "git mv 失败: $fromPath -> $tempPath"
                    }

                    # 第二步：重命名为目标名称
                    git mv $tempPath $toPath
                    if ($LASTEXITCODE -ne 0) {
                        throw "git mv 失败: $tempPath -> $toPath"
                    }

                    $totalFolders++
                    Write-Success "完成: $($folder.from) -> $($folder.to)"
                } catch {
                    Write-Error "重命名失败: $_"
                    throw
                }
            }
        }

        if (-not $DryRun) {
            Write-Info "提交项目 $($project.name) 的文件夹重命名"
            git add -A
            git commit -m "refactor($($project.name)): 重命名文件夹为 PascalCase"
        }
    }

    Write-Success "阶段 1 完成: 共重命名 $totalFolders 个文件夹"
}

# 阶段 2: 命名空间更新
function Invoke-NamespaceUpdate {
    Write-Info "=== 阶段 2: 命名空间更新 ==="

    $csFiles = Get-ChildItem -Path $rootDir -Filter "*.cs" -Recurse |
        Where-Object { $_.FullName -notmatch "\\bin\\|\\obj\\|\\Generated\\" }

    Write-Info "找到 $($csFiles.Count) 个 C# 文件"

    $updatedFiles = 0
    $totalReplacements = 0

    # 定义命名空间替换规则（按优先级排序，长的先匹配）
    $namespaceRules = @(
        # CQRS 子命名空间
        @{ Pattern = '\.cqrs\.notification\b'; Replacement = '.CQRS.Notification' }
        @{ Pattern = '\.cqrs\.command\b'; Replacement = '.CQRS.Command' }
        @{ Pattern = '\.cqrs\.request\b'; Replacement = '.CQRS.Request' }
        @{ Pattern = '\.cqrs\.query\b'; Replacement = '.CQRS.Query' }
        @{ Pattern = '\.cqrs\.behaviors\b'; Replacement = '.CQRS.Behaviors' }
        @{ Pattern = '\.cqrs\b'; Replacement = '.CQRS' }

        # 其他嵌套命名空间
        @{ Pattern = '\.coroutine\.instructions\b'; Replacement = '.Coroutine.Instructions' }
        @{ Pattern = '\.coroutine\.extensions\b'; Replacement = '.Coroutine.Extensions' }
        @{ Pattern = '\.coroutine\b'; Replacement = '.Coroutine' }

        @{ Pattern = '\.events\.filters\b'; Replacement = '.Events.Filters' }
        @{ Pattern = '\.events\b'; Replacement = '.Events' }

        @{ Pattern = '\.logging\.appenders\b'; Replacement = '.Logging.Appenders' }
        @{ Pattern = '\.logging\.filters\b'; Replacement = '.Logging.Filters' }
        @{ Pattern = '\.logging\.formatters\b'; Replacement = '.Logging.Formatters' }
        @{ Pattern = '\.logging\b'; Replacement = '.Logging' }

        @{ Pattern = '\.functional\.async\b'; Replacement = '.Functional.Async' }
        @{ Pattern = '\.functional\.control\b'; Replacement = '.Functional.Control' }
        @{ Pattern = '\.functional\.functions\b'; Replacement = '.Functional.Functions' }
        @{ Pattern = '\.functional\.pipe\b'; Replacement = '.Functional.Pipe' }
        @{ Pattern = '\.functional\.result\b'; Replacement = '.Functional.Result' }
        @{ Pattern = '\.functional\b'; Replacement = '.Functional' }

        @{ Pattern = '\.services\.modules\b'; Replacement = '.Services.Modules' }
        @{ Pattern = '\.services\b'; Replacement = '.Services' }

        # 单层命名空间
        @{ Pattern = '\.architecture\b'; Replacement = '.Architecture' }
        @{ Pattern = '\.bases\b'; Replacement = '.Bases' }
        @{ Pattern = '\.command\b'; Replacement = '.Command' }
        @{ Pattern = '\.configuration\b'; Replacement = '.Configuration' }
        @{ Pattern = '\.constants\b'; Replacement = '.Constants' }
        @{ Pattern = '\.data\b'; Replacement = '.Data' }
        @{ Pattern = '\.enums\b'; Replacement = '.Enums' }
        @{ Pattern = '\.environment\b'; Replacement = '.Environment' }
        @{ Pattern = '\.extensions\b'; Replacement = '.Extensions' }
        @{ Pattern = '\.internals\b'; Replacement = '.Internals' }
        @{ Pattern = '\.ioc\b'; Replacement = '.IoC' }
        @{ Pattern = '\.lifecycle\b'; Replacement = '.Lifecycle' }
        @{ Pattern = '\.model\b'; Replacement = '.Model' }
        @{ Pattern = '\.pause\b'; Replacement = '.Pause' }
        @{ Pattern = '\.pool\b'; Replacement = '.Pool' }
        @{ Pattern = '\.properties\b'; Replacement = '.Properties' }
        @{ Pattern = '\.property\b'; Replacement = '.Property' }
        @{ Pattern = '\.query\b'; Replacement = '.Query' }
        @{ Pattern = '\.registries\b'; Replacement = '.Registries' }
        @{ Pattern = '\.resource\b'; Replacement = '.Resource' }
        @{ Pattern = '\.rule\b'; Replacement = '.Rule' }
        @{ Pattern = '\.serializer\b'; Replacement = '.Serializer' }
        @{ Pattern = '\.state\b'; Replacement = '.State' }
        @{ Pattern = '\.storage\b'; Replacement = '.Storage' }
        @{ Pattern = '\.system\b'; Replacement = '.System' }
        @{ Pattern = '\.time\b'; Replacement = '.Time' }
        @{ Pattern = '\.utility\b'; Replacement = '.Utility' }
        @{ Pattern = '\.versioning\b'; Replacement = '.Versioning' }
    )

    foreach ($file in $csFiles) {
        $content = Get-Content $file.FullName -Raw
        $originalContent = $content
        $fileReplacements = 0

        foreach ($rule in $namespaceRules) {
            $matches = [regex]::Matches($content, $rule.Pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
            if ($matches.Count -gt 0) {
                $content = [regex]::Replace($content, $rule.Pattern, $rule.Replacement, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
                $fileReplacements += $matches.Count
            }
        }

        if ($content -ne $originalContent) {
            if ($DryRun) {
                Write-Info "[DRY RUN] 更新文件: $($file.FullName) ($fileReplacements 处替换)"
            } else {
                Set-Content -Path $file.FullName -Value $content -NoNewline
                $updatedFiles++
                $totalReplacements += $fileReplacements
                Write-Info "更新: $($file.Name) ($fileReplacements 处替换)"
            }
        }
    }

    if (-not $DryRun) {
        Write-Info "提交命名空间更新"
        git add -A
        git commit -m "refactor: 更新所有命名空间为 PascalCase"
    }

    Write-Success "阶段 2 完成: 更新了 $updatedFiles 个文件，共 $totalReplacements 处替换"
}

# 阶段 3: 文档更新
function Invoke-DocumentationUpdate {
    Write-Info "=== 阶段 3: 文档更新 ==="

    $mdFiles = Get-ChildItem -Path $rootDir -Filter "*.md" -Recurse |
        Where-Object { $_.FullName -notmatch "\\node_modules\\|\\bin\\|\\obj\\" }

    Write-Info "找到 $($mdFiles.Count) 个 Markdown 文件"

    $updatedFiles = 0
    $totalReplacements = 0

    # 使用与阶段 2 相同的替换规则
    $namespaceRules = @(
        @{ Pattern = '\.cqrs\.notification\b'; Replacement = '.CQRS.Notification' }
        @{ Pattern = '\.cqrs\.command\b'; Replacement = '.CQRS.Command' }
        @{ Pattern = '\.cqrs\.request\b'; Replacement = '.CQRS.Request' }
        @{ Pattern = '\.cqrs\.query\b'; Replacement = '.CQRS.Query' }
        @{ Pattern = '\.cqrs\.behaviors\b'; Replacement = '.CQRS.Behaviors' }
        @{ Pattern = '\.cqrs\b'; Replacement = '.CQRS' }
        @{ Pattern = '\.coroutine\.instructions\b'; Replacement = '.Coroutine.Instructions' }
        @{ Pattern = '\.coroutine\.extensions\b'; Replacement = '.Coroutine.Extensions' }
        @{ Pattern = '\.coroutine\b'; Replacement = '.Coroutine' }
        @{ Pattern = '\.events\.filters\b'; Replacement = '.Events.Filters' }
        @{ Pattern = '\.events\b'; Replacement = '.Events' }
        @{ Pattern = '\.logging\.appenders\b'; Replacement = '.Logging.Appenders' }
        @{ Pattern = '\.logging\.filters\b'; Replacement = '.Logging.Filters' }
        @{ Pattern = '\.logging\.formatters\b'; Replacement = '.Logging.Formatters' }
        @{ Pattern = '\.logging\b'; Replacement = '.Logging' }
        @{ Pattern = '\.functional\.async\b'; Replacement = '.Functional.Async' }
        @{ Pattern = '\.functional\.control\b'; Replacement = '.Functional.Control' }
        @{ Pattern = '\.functional\.functions\b'; Replacement = '.Functional.Functions' }
        @{ Pattern = '\.functional\.pipe\b'; Replacement = '.Functional.Pipe' }
        @{ Pattern = '\.functional\.result\b'; Replacement = '.Functional.Result' }
        @{ Pattern = '\.functional\b'; Replacement = '.Functional' }
        @{ Pattern = '\.services\.modules\b'; Replacement = '.Services.Modules' }
        @{ Pattern = '\.services\b'; Replacement = '.Services' }
        @{ Pattern = '\.architecture\b'; Replacement = '.Architecture' }
        @{ Pattern = '\.bases\b'; Replacement = '.Bases' }
        @{ Pattern = '\.command\b'; Replacement = '.Command' }
        @{ Pattern = '\.configuration\b'; Replacement = '.Configuration' }
        @{ Pattern = '\.constants\b'; Replacement = '.Constants' }
        @{ Pattern = '\.data\b'; Replacement = '.Data' }
        @{ Pattern = '\.enums\b'; Replacement = '.Enums' }
        @{ Pattern = '\.environment\b'; Replacement = '.Environment' }
        @{ Pattern = '\.extensions\b'; Replacement = '.Extensions' }
        @{ Pattern = '\.internals\b'; Replacement = '.Internals' }
        @{ Pattern = '\.ioc\b'; Replacement = '.IoC' }
        @{ Pattern = '\.lifecycle\b'; Replacement = '.Lifecycle' }
        @{ Pattern = '\.model\b'; Replacement = '.Model' }
        @{ Pattern = '\.pause\b'; Replacement = '.Pause' }
        @{ Pattern = '\.pool\b'; Replacement = '.Pool' }
        @{ Pattern = '\.properties\b'; Replacement = '.Properties' }
        @{ Pattern = '\.property\b'; Replacement = '.Property' }
        @{ Pattern = '\.query\b'; Replacement = '.Query' }
        @{ Pattern = '\.registries\b'; Replacement = '.Registries' }
        @{ Pattern = '\.resource\b'; Replacement = '.Resource' }
        @{ Pattern = '\.rule\b'; Replacement = '.Rule' }
        @{ Pattern = '\.serializer\b'; Replacement = '.Serializer' }
        @{ Pattern = '\.state\b'; Replacement = '.State' }
        @{ Pattern = '\.storage\b'; Replacement = '.Storage' }
        @{ Pattern = '\.system\b'; Replacement = '.System' }
        @{ Pattern = '\.time\b'; Replacement = '.Time' }
        @{ Pattern = '\.utility\b'; Replacement = '.Utility' }
        @{ Pattern = '\.versioning\b'; Replacement = '.Versioning' }
    )

    foreach ($file in $mdFiles) {
        $content = Get-Content $file.FullName -Raw
        $originalContent = $content
        $fileReplacements = 0

        foreach ($rule in $namespaceRules) {
            $matches = [regex]::Matches($content, $rule.Pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
            if ($matches.Count -gt 0) {
                $content = [regex]::Replace($content, $rule.Pattern, $rule.Replacement, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
                $fileReplacements += $matches.Count
            }
        }

        if ($content -ne $originalContent) {
            if ($DryRun) {
                Write-Info "[DRY RUN] 更新文档: $($file.FullName) ($fileReplacements 处替换)"
            } else {
                Set-Content -Path $file.FullName -Value $content -NoNewline
                $updatedFiles++
                $totalReplacements += $fileReplacements
                Write-Info "更新: $($file.Name) ($fileReplacements 处替换)"
            }
        }
    }

    if (-not $DryRun) {
        Write-Info "提交文档更新"
        git add -A
        git commit -m "docs: 更新文档中的命名空间为 PascalCase"
    }

    Write-Success "阶段 3 完成: 更新了 $updatedFiles 个文档，共 $totalReplacements 处替换"
}

# 阶段 4: 验证
function Invoke-Verification {
    Write-Info "=== 阶段 4: 验证 ==="

    # 1. 编译验证
    Write-Info "1. 编译验证..."
    if ($DryRun) {
        Write-Info "[DRY RUN] dotnet build"
    } else {
        Push-Location $rootDir
        try {
            dotnet build --no-restore
            if ($LASTEXITCODE -eq 0) {
                Write-Success "编译成功"
            } else {
                Write-Error "编译失败"
                throw "编译失败"
            }
        } finally {
            Pop-Location
        }
    }

    # 2. 测试验证
    if (-not $SkipTests) {
        Write-Info "2. 测试验证..."
        if ($DryRun) {
            Write-Info "[DRY RUN] dotnet test"
        } else {
            Push-Location $rootDir
            try {
                dotnet test --no-build
                if ($LASTEXITCODE -eq 0) {
                    Write-Success "所有测试通过"
                } else {
                    Write-Error "测试失败"
                    throw "测试失败"
                }
            } finally {
                Pop-Location
            }
        }
    } else {
        Write-Warning "跳过测试验证"
    }

    # 3. 检查残留的小写命名空间
    Write-Info "3. 检查残留的小写命名空间..."
    $csFiles = Get-ChildItem -Path $rootDir -Filter "*.cs" -Recurse |
        Where-Object { $_.FullName -notmatch "\\bin\\|\\obj\\|\\Generated\\" }

    $lowercasePatterns = @(
        '\.architecture\b', '\.command\b', '\.configuration\b', '\.coroutine\b',
        '\.cqrs\b', '\.events\b', '\.extensions\b', '\.functional\b',
        '\.ioc\b', '\.logging\b', '\.model\b', '\.query\b',
        '\.resource\b', '\.state\b', '\.system\b', '\.utility\b'
    )

    $foundIssues = @()
    foreach ($file in $csFiles) {
        $content = Get-Content $file.FullName -Raw
        foreach ($pattern in $lowercasePatterns) {
            if ($content -match $pattern) {
                $foundIssues += "$($file.FullName): 找到小写命名空间 $pattern"
            }
        }
    }

    if ($foundIssues.Count -gt 0) {
        Write-Warning "发现 $($foundIssues.Count) 个残留的小写命名空间:"
        $foundIssues | ForEach-Object { Write-Warning $_ }
    } else {
        Write-Success "未发现残留的小写命名空间"
    }

    Write-Success "阶段 4 完成: 验证通过"
}

# 主执行逻辑
try {
    Write-Info "GFramework 命名空间重构脚本"
    Write-Info "工作目录: $rootDir"
    Write-Info "配置文件: $mappingFile"

    if ($DryRun) {
        Write-Warning "*** 干运行模式 - 不会执行实际操作 ***"
    }

    if (-not (Test-Path $mappingFile)) {
        Write-Error "配置文件不存在: $mappingFile"
        exit 1
    }

    switch ($Phase) {
        "1" { Invoke-FolderRename }
        "2" { Invoke-NamespaceUpdate }
        "3" { Invoke-DocumentationUpdate }
        "4" { Invoke-Verification }
        "All" {
            Invoke-FolderRename
            Invoke-NamespaceUpdate
            Invoke-DocumentationUpdate
            Invoke-Verification
        }
    }

    Write-Success "=== 重构完成 ==="

} catch {
    Write-Error "重构失败: $_"
    Write-Error $_.ScriptStackTrace
    exit 1
}
