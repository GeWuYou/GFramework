// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using FileAccess = Godot.FileAccess;

namespace GFramework.Godot.Config;

/// <summary>
///     抽象 <see cref="GodotYamlConfigLoader" /> 与具体宿主环境之间的 Godot 路径和文件访问边界。
/// </summary>
/// <remarks>
///     该抽象存在的原因，是编辑器态与导出态对 <c>res://</c>、<c>user://</c> 的访问方式不同：
///     编辑器态通常可以把 Godot 特殊路径全局化后直接落到普通文件系统，而导出态往往只能通过 Godot API 读取原始文本资源，
///     再把它们复制到运行时缓存目录。<see cref="EnumerateDirectory" /> 在目录不存在或当前环境无法枚举时必须返回
///     <see langword="null" />，用来表达“不可访问”而不是抛出未找到异常；<see cref="ReadAllBytes" /> 则应保留底层读取失败异常，
///     交由加载器包装成配置诊断。对于普通文件系统路径，应遵循 <see cref="Directory" /> / <see cref="File" /> 语义；
///     对于 Godot 特殊路径，则应使用引擎提供的路径解析和读取能力。
/// </remarks>
internal sealed class GodotYamlConfigEnvironment
{
    /// <summary>
    ///     初始化一个可替换的 Godot YAML 配置宿主环境抽象。
    /// </summary>
    /// <param name="isEditor">返回当前进程是否处于 Godot 编辑器态的委托。</param>
    /// <param name="globalizePath">
    ///     把 Godot 特殊路径转换为普通绝对路径的委托。
    ///     当前加载器仅会在输入为 <c>res://</c> 或 <c>user://</c> 时调用它，返回值必须为非空绝对路径。
    /// </param>
    /// <param name="enumerateDirectory">
    ///     枚举指定目录直接子项的委托。
    ///     当目录不存在、无法访问或当前环境无法枚举该路径时，必须返回 <see langword="null" />。
    /// </param>
    /// <param name="fileExists">
    ///     检查指定路径上的文件是否存在的委托。
    ///     输入既可能是 Godot 特殊路径，也可能是普通绝对路径。
    /// </param>
    /// <param name="readAllBytes">
    ///     读取指定文件完整字节内容的委托。
    ///     当文件缺失或读取失败时，应抛出底层异常，由加载器统一包装为配置加载诊断。
    /// </param>
    /// <exception cref="ArgumentNullException">任一委托参数为 <see langword="null" /> 时抛出。</exception>
    public GodotYamlConfigEnvironment(
        Func<bool> isEditor,
        Func<string, string> globalizePath,
        Func<string, IReadOnlyList<GodotYamlConfigDirectoryEntry>?> enumerateDirectory,
        Func<string, bool> fileExists,
        Func<string, byte[]> readAllBytes)
    {
        IsEditor = isEditor ?? throw new ArgumentNullException(nameof(isEditor));
        GlobalizePath = globalizePath ?? throw new ArgumentNullException(nameof(globalizePath));
        EnumerateDirectory = enumerateDirectory ?? throw new ArgumentNullException(nameof(enumerateDirectory));
        FileExists = fileExists ?? throw new ArgumentNullException(nameof(fileExists));
        ReadAllBytes = readAllBytes ?? throw new ArgumentNullException(nameof(readAllBytes));
    }

    /// <summary>
    ///     获取默认的 Godot 运行时环境实现。
    /// </summary>
    /// <remarks>
    ///     默认实现使用 <see cref="OS.HasFeature(string)" /> 检测编辑器态，
    ///     使用 <see cref="ProjectSettings.GlobalizePath(string)" /> 处理 Godot 特殊路径，
    ///     并在 Godot 路径与普通路径之间切换对应的枚举和读取 API。
    /// </remarks>
    public static GodotYamlConfigEnvironment Default { get; } = new(
        static () => OS.HasFeature("editor"),
        static path => ProjectSettings.GlobalizePath(path),
        EnumerateDirectoryCore,
        FileExistsCore,
        ReadAllBytesCore);

    /// <summary>
    ///     获取用于判断当前进程是否处于编辑器态的委托。
    /// </summary>
    public Func<bool> IsEditor { get; }

    /// <summary>
    ///     获取把 Godot 特殊路径转换为普通绝对路径的委托。
    /// </summary>
    /// <remarks>
    ///     当前加载器只会对 <c>res://</c> 和 <c>user://</c> 路径调用该委托。
    ///     返回空字符串会被视为无效环境实现，并在后续路径解析阶段触发异常。
    /// </remarks>
    public Func<string, string> GlobalizePath { get; }

    /// <summary>
    ///     获取用于枚举目录直接子项的委托。
    /// </summary>
    /// <remarks>
    ///     当目录不存在、无法访问，或当前环境无法枚举给定路径时，该委托必须返回 <see langword="null" />。
    ///     返回的集合只应包含当前目录下的直接子项，调用方会自行过滤隐藏项、子目录与非 YAML 文件。
    /// </remarks>
    public Func<string, IReadOnlyList<GodotYamlConfigDirectoryEntry>?> EnumerateDirectory { get; }

    /// <summary>
    ///     获取用于检查文件是否存在的委托。
    /// </summary>
    public Func<string, bool> FileExists { get; }

    /// <summary>
    ///     获取用于读取文件完整字节内容的委托。
    /// </summary>
    /// <remarks>
    ///     该委托在路径不存在、权限不足或 I/O 失败时应抛出底层异常，以便加载器保留失败原因并生成诊断信息。
    /// </remarks>
    public Func<string, byte[]> ReadAllBytes { get; }

    private static IReadOnlyList<GodotYamlConfigDirectoryEntry>? EnumerateDirectoryCore(string path)
    {
        return path.IsGodotPath()
            ? EnumerateGodotDirectory(path)
            : EnumerateFileSystemDirectory(path);
    }

    private static IReadOnlyList<GodotYamlConfigDirectoryEntry>? EnumerateFileSystemDirectory(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                return null;
            }

            return Directory
                .EnumerateFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly)
                .Select(static entryPath => new GodotYamlConfigDirectoryEntry(
                    Path.GetFileName(entryPath),
                    Directory.Exists(entryPath)))
                .ToArray();
        }
        catch (Exception ex) when (IsExpectedDirectoryEnumerationException(ex))
        {
            // 非 Godot 路径分支与公开契约保持一致：宿主无法访问目录时返回 null，而不是泄漏底层异常。
            return null;
        }
    }

    private static IReadOnlyList<GodotYamlConfigDirectoryEntry>? EnumerateGodotDirectory(string path)
    {
        using var directory = DirAccess.Open(path);
        if (directory == null)
        {
            return null;
        }

        var entries = new List<GodotYamlConfigDirectoryEntry>();
        var listDirectoryError = directory.ListDirBegin();
        if (listDirectoryError != Error.Ok)
        {
            return null;
        }

        try
        {
            while (true)
            {
                var name = directory.GetNext();
                if (string.IsNullOrEmpty(name))
                {
                    break;
                }

                entries.Add(new GodotYamlConfigDirectoryEntry(name, directory.CurrentIsDir()));
            }
        }
        finally
        {
            // 目录枚举句柄必须成对结束，避免未来循环体扩展后在异常路径上遗留引擎状态。
            directory.ListDirEnd();
        }

        return entries;
    }

    private static bool IsExpectedDirectoryEnumerationException(Exception exception)
    {
        return exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException;
    }

    private static bool FileExistsCore(string path)
    {
        return path.IsGodotPath()
            ? FileAccess.FileExists(path)
            : File.Exists(path);
    }

    private static byte[] ReadAllBytesCore(string path)
    {
        if (!path.IsGodotPath())
        {
            return File.ReadAllBytes(path);
        }

        var bytes = FileAccess.GetFileAsBytes(path);
        var error = FileAccess.GetOpenError();
        if (error == Error.Ok)
        {
            return bytes;
        }

        throw CreateReadException(path, error);
    }

    private static Exception CreateReadException(string path, Error error)
    {
        return error switch
        {
            Error.FileNotFound => new FileNotFoundException($"Godot file not found: {path}", path),
            Error.FileCantOpen => new IOException($"Godot could not open file '{path}'. Error: {error}"),
            _ => new IOException($"Godot failed to read file '{path}'. Error: {error}")
        };
    }
}
