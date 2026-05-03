// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Godot.Config;

/// <summary>
///     描述一次目录枚举返回的单个子项。
/// </summary>
/// <remarks>
///     该结构只承载目录扫描阶段需要的最小信息。
///     <see cref="Name" /> 必须是单个目录项名称，而不是包含父目录的完整路径；
///     对于 Godot 路径和普通路径都遵循相同约定，便于加载器统一做后续拼接与过滤。
/// </remarks>
internal readonly record struct GodotYamlConfigDirectoryEntry
{
    /// <summary>
    ///     初始化一个目录枚举结果项。
    /// </summary>
    /// <param name="name">当前目录项的名称，不包含父目录路径。</param>
    /// <param name="isDirectory">指示该目录项是否为子目录。</param>
    public GodotYamlConfigDirectoryEntry(string name, bool isDirectory)
    {
        Name = name;
        IsDirectory = isDirectory;
    }

    /// <summary>
    ///     获取当前目录项的名称，不包含父目录路径。
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     获取一个值，指示当前目录项是否为子目录。
    /// </summary>
    public bool IsDirectory { get; }
}
