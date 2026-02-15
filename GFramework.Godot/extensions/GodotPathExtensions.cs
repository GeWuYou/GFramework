namespace GFramework.Godot.extensions;

/// <summary>
/// 提供对 Godot 路径相关操作的扩展方法。
/// 包含判断路径类型的功能，例如用户数据路径（user://）和资源路径（res://）。
/// </summary>
public static class GodotPathExtensions
{
    /// <summary>
    ///     判断指定路径是否为 Godot 用户数据路径（user://）。
    /// </summary>
    /// <param name="path">待检查的路径字符串。</param>
    /// <returns>如果路径以 "user://" 开头且不为空，则返回 true；否则返回 false。</returns>
    public static bool IsUserPath(this string path)
    {
        return !string.IsNullOrEmpty(path) && path.StartsWith("user://");
    }

    /// <summary>
    ///     判断指定路径是否为 Godot 资源路径（res://）。
    /// </summary>
    /// <param name="path">待检查的路径字符串。</param>
    /// <returns>如果路径以 "res://" 开头且不为空，则返回 true；否则返回 false。</returns>
    public static bool IsResPath(this string path)
    {
        return !string.IsNullOrEmpty(path) && path.StartsWith("res://");
    }

    /// <summary>
    ///     判断指定路径是否为 Godot 特殊路径（user:// 或 res://）。
    /// </summary>
    /// <param name="path">待检查的路径字符串。</param>
    /// <returns>如果路径是用户数据路径或资源路径，则返回 true；否则返回 false。</returns>
    public static bool IsGodotPath(this string path)
    {
        return path.IsUserPath() || path.IsResPath();
    }
}