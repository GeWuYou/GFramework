namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     记录双行为 pipeline 的实际执行顺序。
/// </summary>
internal static class DispatcherPipelineOrderState
{
    /// <summary>
    ///     获取按执行顺序追加的步骤名称。
    /// </summary>
    public static List<string> Steps { get; } = [];

    /// <summary>
    ///     清空当前记录，供下一次断言使用。
    /// </summary>
    public static void Reset()
    {
        Steps.Clear();
    }
}
