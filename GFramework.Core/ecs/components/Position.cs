namespace GFramework.Core.ecs.components;

/// <summary>
///     位置组件
/// </summary>
public struct Position
{
    public float X;
    public float Y;

    public Position(float x, float y)
    {
        X = x;
        Y = y;
    }
}