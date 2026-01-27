namespace GFramework.Core.Abstractions.coroutine;

/// <summary>
///     时间源接口，提供当前时间、时间增量以及更新功能
/// </summary>
public interface ITimeSource
{
    /// <summary>
    ///     获取当前时间
    /// </summary>
    double CurrentTime { get; }

    /// <summary>
    ///     获取时间增量（上一帧到当前帧的时间差）
    /// </summary>
    double DeltaTime { get; }

    /// <summary>
    ///     更新时间源的状态
    /// </summary>
    void Update();
}