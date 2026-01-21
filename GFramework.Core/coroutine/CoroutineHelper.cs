using GFramework.Core.Abstractions.coroutine;

namespace GFramework.Core.coroutine;

/// <summary>
/// 协程辅助方法
/// </summary>
public static class CoroutineHelper
{
    /// <summary>
    /// 等待指定秒数
    /// </summary>
    /// <param name="seconds">要等待的秒数</param>
    /// <returns>延迟等待指令</returns>
    public static Delay WaitForSeconds(double seconds)
    {
        return new Delay(seconds);
    }

    /// <summary>
    /// 等待一帧
    /// </summary>
    /// <returns>等待一帧的指令</returns>
    public static WaitOneFrame WaitForOneFrame()
    {
        return new WaitOneFrame();
    }

    /// <summary>
    /// 等待指定帧数
    /// </summary>
    /// <param name="frames">要等待的帧数</param>
    /// <returns>等待帧数指令</returns>
    public static WaitForFrames WaitForFrames(int frames)
    {
        return new WaitForFrames(frames);
    }

    /// <summary>
    /// 等待直到条件满足
    /// </summary>
    /// <param name="predicate">条件判断函数</param>
    /// <returns>等待条件指令</returns>
    public static WaitUntil WaitUntil(Func<bool> predicate)
    {
        return new WaitUntil(predicate);
    }

    /// <summary>
    /// 等待当条件为真时持续等待
    /// </summary>
    /// <param name="predicate">条件判断函数</param>
    /// <returns>等待条件指令</returns>
    public static WaitWhile WaitWhile(Func<bool> predicate)
    {
        return new WaitWhile(predicate);
    }

    /// <summary>
    /// 延迟调用指定的委托
    /// </summary>
    /// <param name="delay">延迟时间（秒）</param>
    /// <param name="action">要执行的动作委托</param>
    /// <returns>返回一个枚举器，用于协程执行</returns>
    public static IEnumerator<IYieldInstruction> DelayedCall(double delay, Action? action)
    {
        yield return new Delay(delay);
        action?.Invoke();
    }

    /// <summary>
    /// 重复调用指定的委托指定次数
    /// </summary>
    /// <param name="interval">每次调用之间的间隔时间（秒）</param>
    /// <param name="count">调用次数</param>
    /// <param name="action">要执行的动作委托</param>
    /// <returns>返回一个枚举器，用于协程执行</returns>
    public static IEnumerator<IYieldInstruction> RepeatCall(double interval, int count, Action? action)
    {
        for (var i = 0; i < count; i++)
        {
            action?.Invoke();
            yield return new Delay(interval);
        }
    }

    /// <summary>
    /// 无限重复调用指定的委托
    /// </summary>
    /// <param name="interval">每次调用之间的间隔时间（秒）</param>
    /// <param name="action">要执行的动作委托</param>
    /// <returns>返回一个枚举器，用于协程执行</returns>
    public static IEnumerator<IYieldInstruction> RepeatCallForever(double interval, Action? action)
    {
        while (true)
        {
            action?.Invoke();
            yield return new Delay(interval);
        }
    }
}