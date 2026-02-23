namespace GFramework.Core.ecs.components;

/// <summary>
/// 速度组件，用于表示实体在二维空间中的运动速度。
/// </summary>
/// <param name="x">X轴方向的速度分量</param>
/// <param name="y">Y轴方向的速度分量</param>
public struct Velocity(float x, float y)
{
    /// <summary>
    /// X轴方向的速度分量
    /// </summary>
    public float X { get; set; } = x;

    /// <summary>
    /// Y轴方向的速度分量
    /// </summary>
    public float Y { get; set; } = y;
}