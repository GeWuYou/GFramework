namespace GFramework.Game.input;

/// <summary>
/// 输入转换器接口
/// </summary>
public interface IInputTranslator
{
    /// <summary>
    /// 尝试将引擎输入翻译为游戏输入
    /// </summary>
    /// <param name="rawInput">原始的引擎输入对象</param>
    /// <param name="gameEvent">输出参数，如果翻译成功则包含对应的游戏输入事件</param>
    /// <returns>如果翻译成功返回true，否则返回false</returns>
    bool TryTranslate(object rawInput, out IGameInputEvent gameEvent);
}
