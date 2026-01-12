namespace GFramework.Godot.extensions;

public static class GodotPathExtensions
{
    /// <summary>
    /// 判断是否是 Godot 用户数据路径（user://）
    /// </summary>
    public static bool IsUserPath(this string path)
        => !string.IsNullOrEmpty(path) && path.StartsWith("user://");

    /// <summary>
    /// 判断是否是 Godot 资源路径（res://）
    /// </summary>
    public static bool IsResPath(this string path)
        => !string.IsNullOrEmpty(path) && path.StartsWith("res://");

    /// <summary>
    /// 判断是否是 Godot 特殊路径（user:// 或 res://）
    /// </summary>
    public static bool IsGodotPath(this string path)
        => path.IsUserPath() || path.IsResPath();
}