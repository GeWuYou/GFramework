namespace GFramework.Game.input;

/// <summary>
/// 输入上下文接口，用于处理游戏中的输入事件
/// </summary>
public interface IInputContext
{
    /// <summary>
    /// 处理游戏输入事件
    /// </summary>
    /// <param name="input">要处理的游戏输入事件</param>
    /// <returns>返回 true 表示输入被吃掉，不再向下传播；返回 false 表示输入未被处理，可以继续传播给其他处理器</returns>
    bool Handle(IGameInputEvent input);
}
