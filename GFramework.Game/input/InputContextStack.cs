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
    /// 弹出堆栈顶部的元素
    /// </summary>
    public void Pop() => _stack.Pop();
    
    /// <summary>
    /// 弹出堆栈顶部的输入上下文
    /// </summary>
    /// <param name="ctx">要弹出的输入上下文对象</param>
    /// <returns>如果成功弹出返回true，否则返回false</returns>
    public bool Pop(IInputContext ctx)
    {
        // 检查堆栈顶部元素是否与指定上下文匹配，如果不匹配则返回false
        if (!_stack.TryPeek(out var top) || top != ctx) return false;
        _stack.Pop();
        return true;
    }

    
    /// <summary>
    /// 获取堆栈顶部的输入上下文但不移除它
    /// </summary>
    /// <returns>堆栈顶部的输入上下文</returns>
    public IInputContext Peek() => _stack.Peek();

    
    /// <summary>
    /// 处理游戏输入事件
    /// </summary>
    /// <param name="input">要处理的游戏输入事件</param>
    /// <returns>如果任何一个输入上下文成功处理了输入事件则返回true，否则返回false</returns>
    public bool Handle(IGameInputEvent input)
    {
        // 从堆栈顶部开始遍历输入上下文，尝试处理输入事件
        return _stack.Any(ctx => ctx.Handle(input));
    }

}

