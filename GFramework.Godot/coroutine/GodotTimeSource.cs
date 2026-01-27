using GFramework.Core.Abstractions.coroutine;

namespace GFramework.Godot.coroutine;

/// <summary>
///     Godot时间源实现，用于提供基于Godot引擎的时间信息
/// </summary>
/// <param name="getDeltaFunc">获取增量时间的函数委托</param>
public class GodotTimeSource(Func<double> getDeltaFunc) : ITimeSource
{
    private readonly Func<double> _getDeltaFunc = getDeltaFunc ?? throw new ArgumentNullException(nameof(getDeltaFunc));

    /// <summary>
    ///     获取当前累计时间
    /// </summary>
    public double CurrentTime { get; private set; }

    /// <summary>
    ///     获取上一帧的时间增量
    /// </summary>
    public double DeltaTime { get; private set; }

    /// <summary>
    ///     更新时间源，计算新的增量时间和累计时间
    /// </summary>
    public void Update()
    {
        // 调用外部提供的函数获取当前帧的时间增量
        DeltaTime = _getDeltaFunc();
        // 累加到总时间中
        CurrentTime += DeltaTime;
    }

    /// <summary>
    ///     重置时间源到初始状态
    /// </summary>
    public void Reset()
    {
        CurrentTime = 0;
        DeltaTime = 0;
    }
}