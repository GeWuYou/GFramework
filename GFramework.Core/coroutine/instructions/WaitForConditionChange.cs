using GFramework.Core.Abstractions.coroutine;

namespace GFramework.Core.coroutine.instructions;

/// <summary>
///     等待条件状态发生变化的指令
///     当条件从一种状态切换到另一种状态时完成
/// </summary>
/// <param name="conditionGetter">获取当前条件状态的函数</param>
/// <param name="waitForTransitionTo">期望转换到的目标状态</param>
public sealed class WaitForConditionChange(Func<bool> conditionGetter, bool waitForTransitionTo) : IYieldInstruction
{
    private readonly Func<bool> _conditionGetter =
        conditionGetter ?? throw new ArgumentNullException(nameof(conditionGetter));

    private bool? _initialState;
    private bool _isCompleted;

    /// <summary>
    ///     更新方法，检测条件变化
    /// </summary>
    /// <param name="deltaTime">时间增量</param>
    public void Update(double deltaTime)
    {
        if (_isCompleted)
            return;

        if (!_initialState.HasValue)
        {
            _initialState = _conditionGetter();
            return;
        }

        // 检查是否发生了期望的状态转换
        var currentState = _conditionGetter();
        if (currentState == waitForTransitionTo && _initialState.Value != waitForTransitionTo)
        {
            _isCompleted = true;
        }
    }

    /// <summary>
    ///     获取等待是否已完成（条件发生了期望的状态转换）
    /// </summary>
    public bool IsDone => _isCompleted;
}