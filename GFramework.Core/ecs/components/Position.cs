namespace GFramework.Core.ecs.components;

/// <summary>
/// 位置组件，用于表示实体在二维空间中的坐标位置。
/// </summary>
/// <param name="x">X轴坐标值</param>
/// <param name="y">Y轴坐标值</param>
public struct Position(float x, float y)
{
    /// <summary>
    /// 获取X轴坐标值。
    /// </summary>
    public float X { get; set; } = x;

    /// <summary>
    /// 获取Y轴坐标值。
    /// </summary>
    public float Y { get; set; } = y;
}