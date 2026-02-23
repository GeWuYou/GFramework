namespace GFramework.Core.ecs.components;

/// <summary>
///     速度组件
/// </summary>
public struct Velocity
{
    public float X;
    public float Y;

    public Velocity(float x, float y)
    {
        X = x;
        Y = y;
    }
}