namespace GFramework.Game.input;

/// <summary>
/// 输入上下文堆栈管理器，用于管理多个输入上下文的堆栈结构
/// </summary>
public class InputContextStack
{
    private readonly Stack<IInputContext> _stack = new();

    /// <summary>
    /// 将指定的输入上下文压入堆栈顶部
    /// </summary>
    /// <param name="context">要压入堆栈的输入上下文对象</param>
    public void Push(IInputContext context) => _stack.Push(context);
    
    /// <summary>
    /// 弹出堆栈顶部的输入上下文
    /// </summary>
    public void Pop() => _stack.Pop();

    /// <summary>
    /// 处理游戏输入事件，遍历堆栈中的所有上下文直到找到能够处理该事件的上下文
    /// </summary>
    /// <param name="input">要处理的游戏输入事件</param>
    /// <returns>如果堆栈中任意一个上下文成功处理了输入事件则返回true，否则返回false</returns>
    public bool Handle(IGameInputEvent input)
    {
        // 遍历堆栈中的所有上下文，调用其Handle方法处理输入事件
        // Any方法会在第一个返回true的上下文处停止遍历
        return _stack.Any(ctx => ctx.Handle(input));
    }
}

