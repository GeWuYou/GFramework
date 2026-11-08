// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;

namespace GFramework.Cqrs.Benchmarks;

/// <summary>
///     提供 GFramework.CQRS benchmark 的统一命令行入口。
/// </summary>
internal static class Program
{
    private const string ArtifactsSuffixOption = "--artifacts-suffix";
    private const string ArtifactsSuffixEnvironmentVariable = "GFRAMEWORK_CQRS_BENCHMARK_ARTIFACTS_SUFFIX";
    private const string ArtifactsPathEnvironmentVariable = "GFRAMEWORK_CQRS_BENCHMARK_ARTIFACTS_PATH";
    private const string IsolatedHostEnvironmentVariable = "GFRAMEWORK_CQRS_BENCHMARK_ISOLATED_HOST";
    private const string DefaultArtifactsDirectoryName = "BenchmarkDotNet.Artifacts";
    private const string IsolatedHostDirectoryName = "host";

    /// <summary>
    ///     运行当前程序集中的全部 benchmark。
    /// </summary>
    /// <param name="args">仓库入口参数与透传给 BenchmarkDotNet 的命令行参数。</param>
    private static void Main(string[] args)
    {
        var invocation = ParseInvocation(args);

        ConsoleLogger.Default.WriteLine("Running GFramework.Cqrs benchmarks");

        if (invocation.RequiresHostIsolation &&
            !string.Equals(
                Environment.GetEnvironmentVariable(IsolatedHostEnvironmentVariable),
                "1",
                StringComparison.Ordinal))
        {
            Environment.Exit(RunFromIsolatedHost(invocation, args));
        }

        if (invocation.ArtifactsPath is null)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(invocation.BenchmarkDotNetArguments);
            return;
        }

        ConsoleLogger.Default.WriteLine(
            $"Using isolated BenchmarkDotNet artifacts path: {invocation.ArtifactsPath}");

        BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(invocation.BenchmarkDotNetArguments, DefaultConfig.Instance.WithArtifactsPath(invocation.ArtifactsPath));
    }

    /// <summary>
    ///     解析仓库自定义参数，并生成实际传递给 BenchmarkDotNet 的参数与隔离后的 artifacts 路径。
    /// </summary>
    /// <param name="args">当前进程收到的完整命令行参数。</param>
    /// <returns>入口解析后的 benchmark 调用选项。</returns>
    /// <exception cref="ArgumentException">自定义参数缺失值或包含非法路径片段时抛出。</exception>
    private static BenchmarkInvocation ParseInvocation(string[] args)
    {
        var benchmarkDotNetArguments = new List<string>(args.Length);
        string? commandLineSuffix = null;

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (!string.Equals(argument, ArtifactsSuffixOption, StringComparison.Ordinal))
            {
                benchmarkDotNetArguments.Add(argument);
                continue;
            }

            if (index == args.Length - 1)
            {
                throw new ArgumentException(
                    $"The {ArtifactsSuffixOption} option requires a suffix value.",
                    nameof(args));
            }

            if (commandLineSuffix is not null)
            {
                throw new ArgumentException(
                    $"The {ArtifactsSuffixOption} option can only be provided once.",
                    nameof(args));
            }

            // 剥离仓库自定义参数，避免将它误传给 BenchmarkDotNet 自身的命令行解析器。
            commandLineSuffix = args[++index];
        }

        var artifactsPath = ResolveArtifactsPath(commandLineSuffix);
        return new BenchmarkInvocation(
            [.. benchmarkDotNetArguments],
            commandLineSuffix,
            artifactsPath,
            artifactsPath is not null);
    }

    /// <summary>
    ///     将当前 benchmark 入口重启到独立的宿主工作目录，避免多个并发进程共享同一份 auto-generated build 目录。
    /// </summary>
    /// <param name="invocation">当前入口解析后的 benchmark 调用选项。</param>
    /// <param name="originalArgs">原始命令行参数，用于透传给隔离后的宿主进程。</param>
    /// <returns>隔离后宿主进程的退出码。</returns>
    private static int RunFromIsolatedHost(BenchmarkInvocation invocation, string[] originalArgs)
    {
        var artifactsPath = invocation.ArtifactsPath
            ?? throw new ArgumentNullException(nameof(invocation), "An isolated benchmark host requires an artifacts path.");

        var currentAssemblyPath = typeof(Program).Assembly.Location;
        var sourceHostDirectory = AppContext.BaseDirectory;
        var isolatedHostDirectory = Path.Combine(artifactsPath, IsolatedHostDirectoryName);

        PrepareIsolatedHostDirectory(sourceHostDirectory, isolatedHostDirectory);

        var isolatedAssemblyPath = Path.Combine(
            isolatedHostDirectory,
            Path.GetFileName(currentAssemblyPath));

        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = isolatedHostDirectory,
            UseShellExecute = false,
        };

        startInfo.ArgumentList.Add(isolatedAssemblyPath);
        foreach (var argument in originalArgs)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.Environment[IsolatedHostEnvironmentVariable] = "1";
        startInfo.Environment[ArtifactsPathEnvironmentVariable] = artifactsPath;

        ConsoleLogger.Default.WriteLine(
            $"Launching isolated benchmark host in: {isolatedHostDirectory}");

        using var process = Process.Start(startInfo) ??
            throw new InvalidOperationException("Failed to launch the isolated benchmark host process.");

        process.WaitForExit();
        return process.ExitCode;
    }

    /// <summary>
    ///     根据命令行或环境变量中的 suffix 生成当前 benchmark 运行的独立 artifacts 目录。
    /// </summary>
    /// <param name="commandLineSuffix">命令行显式提供的 suffix。</param>
    /// <returns>隔离后的 artifacts 目录；若未提供 suffix，则返回 <see langword="null"/>。</returns>
    private static string? ResolveArtifactsPath(string? commandLineSuffix)
    {
        var explicitArtifactsPath = Environment.GetEnvironmentVariable(ArtifactsPathEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(explicitArtifactsPath))
        {
            return Path.GetFullPath(explicitArtifactsPath);
        }

        if (!string.IsNullOrWhiteSpace(commandLineSuffix))
        {
            var validatedCommandLineSuffix = ValidateArtifactsSuffix(
                commandLineSuffix,
                ArtifactsSuffixOption);

            return Path.GetFullPath(Path.Combine(DefaultArtifactsDirectoryName, validatedCommandLineSuffix));
        }

        var environmentSuffix = Environment.GetEnvironmentVariable(ArtifactsSuffixEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(environmentSuffix))
        {
            return null;
        }

        var validatedEnvironmentSuffix = ValidateArtifactsSuffix(
            environmentSuffix,
            ArtifactsSuffixEnvironmentVariable);

        return Path.GetFullPath(Path.Combine(DefaultArtifactsDirectoryName, validatedEnvironmentSuffix));
    }

    /// <summary>
    ///     校验自定义 suffix，避免路径穿越、分隔符注入或不可移植字符污染 BenchmarkDotNet 的输出目录。
    /// </summary>
    /// <param name="suffix">待校验的后缀值。</param>
    /// <param name="sourceName">后缀来源名称，用于错误提示。</param>
    /// <returns>可安全用于单级目录名的后缀。</returns>
    /// <exception cref="ArgumentException">当后缀为空或包含未允许字符时抛出。</exception>
    private static string ValidateArtifactsSuffix(string suffix, string sourceName)
    {
        var trimmedSuffix = suffix.Trim();
        if (trimmedSuffix.Length == 0)
        {
            throw new ArgumentException(
                $"The {sourceName} value must not be empty.",
                nameof(suffix));
        }

        foreach (var character in trimmedSuffix)
        {
            if (char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_')
            {
                continue;
            }

            throw new ArgumentException(
                $"The {sourceName} value '{trimmedSuffix}' contains unsupported characters. " +
                "Only ASCII letters, digits, '.', '-' and '_' are allowed.",
                nameof(suffix));
        }

        return trimmedSuffix;
    }

    /// <summary>
    ///     将当前 benchmark 宿主输出复制到独立目录，确保并发运行时的 auto-generated benchmark 项目不会写入同一路径。
    /// </summary>
    /// <param name="sourceHostDirectory">当前 benchmark 宿主输出目录。</param>
    /// <param name="isolatedHostDirectory">当前 suffix 对应的独立宿主目录。</param>
    private static void PrepareIsolatedHostDirectory(string sourceHostDirectory, string isolatedHostDirectory)
    {
        ValidateIsolatedHostDirectory(sourceHostDirectory, isolatedHostDirectory);
        Directory.CreateDirectory(isolatedHostDirectory);
        CopyDirectoryRecursively(sourceHostDirectory, isolatedHostDirectory);
    }

    /// <summary>
    ///     拒绝把隔离宿主目录放到当前宿主输出目录内部，避免递归复制把 `host/host/...` 无限扩张。
    /// </summary>
    /// <param name="sourceHostDirectory">当前 benchmark 宿主输出目录。</param>
    /// <param name="isolatedHostDirectory">目标隔离宿主目录。</param>
    /// <exception cref="InvalidOperationException">
    ///     <paramref name="isolatedHostDirectory"/> 等于或位于 <paramref name="sourceHostDirectory"/> 之内。
    /// </exception>
    private static void ValidateIsolatedHostDirectory(string sourceHostDirectory, string isolatedHostDirectory)
    {
        var normalizedSourceDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(sourceHostDirectory));
        var normalizedIsolatedHostDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(isolatedHostDirectory));

        if (string.Equals(
                normalizedSourceDirectory,
                normalizedIsolatedHostDirectory,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The isolated benchmark host directory must differ from the current host output directory.");
        }

        var relativePath = Path.GetRelativePath(normalizedSourceDirectory, normalizedIsolatedHostDirectory);
        if (IsCurrentDirectoryOrChild(relativePath))
        {
            throw new InvalidOperationException(
                $"The isolated benchmark host directory '{normalizedIsolatedHostDirectory}' must not be nested inside the current host output directory '{normalizedSourceDirectory}'.");
        }
    }

    /// <summary>
    ///     判断一个相对路径是否仍指向当前目录或其子目录。
    /// </summary>
    /// <param name="relativePath">相对路径。</param>
    /// <returns>目标位于当前目录或其子目录时返回 <see langword="true"/>。</returns>
    private static bool IsCurrentDirectoryOrChild(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath) || string.Equals(relativePath, ".", StringComparison.Ordinal))
        {
            return true;
        }

        if (Path.IsPathRooted(relativePath))
        {
            return false;
        }

        return !string.Equals(relativePath, "..", StringComparison.Ordinal) &&
               !relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
               !relativePath.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal);
    }

    /// <summary>
    ///     递归复制 benchmark 宿主输出目录，覆盖同名文件以支持同一 suffix 的重复运行。
    /// </summary>
    /// <param name="sourceDirectory">源目录。</param>
    /// <param name="destinationDirectory">目标目录。</param>
    private static void CopyDirectoryRecursively(string sourceDirectory, string destinationDirectory)
    {
        foreach (var directory in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativeDirectory = Path.GetRelativePath(sourceDirectory, directory);
            Directory.CreateDirectory(Path.Combine(destinationDirectory, relativeDirectory));
        }

        foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativeFile = Path.GetRelativePath(sourceDirectory, file);
            var destinationFile = Path.Combine(destinationDirectory, relativeFile);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
            File.Copy(file, destinationFile, overwrite: true);
        }
    }

    /// <summary>
    ///     表示一次 benchmark 入口调用在剥离仓库自定义参数后的最终配置。
    /// </summary>
    /// <param name="BenchmarkDotNetArguments">实际传递给 BenchmarkDotNet 的命令行参数。</param>
    /// <param name="ArtifactsSuffix">当前运行声明的隔离后缀；若未声明则为 <see langword="null"/>。</param>
    /// <param name="ArtifactsPath">本次运行的 artifacts 目录；若未隔离则为 <see langword="null"/>。</param>
    /// <param name="RequiresHostIsolation">本次运行是否需要重启到隔离宿主目录。</param>
    private readonly record struct BenchmarkInvocation(
        string[] BenchmarkDotNetArguments,
        string? ArtifactsSuffix,
        string? ArtifactsPath,
        bool RequiresHostIsolation);
}
