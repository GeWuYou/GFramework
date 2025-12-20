using GFramework.Core.extensions;
using GFramework.Core.system;

namespace GFramework.Game.input;

/// <summary>
/// 输入系统类，负责管理输入上下文堆栈并处理游戏输入事件
/// </summary>
public class InputSystem : AbstractSystem
{
    private readonly InputContextStack _contextStack = new();

    /// <summary>
    /// 将输入上下文推入上下文堆栈
    /// </summary>
    /// <param name="ctx">要推入的输入上下文对象</param>
    public void PushContext(IInputContext ctx)
        => _contextStack.Push(ctx);

    /// <summary>
    /// 从上下文堆栈中弹出顶层输入上下文
    /// </summary>
    public void PopContext()
        => _contextStack.Pop();

    /// <summary>
    /// 处理游戏输入事件，首先尝试通过上下文堆栈处理，如果未被处理则发送事件
    /// </summary>
    /// <param name="input">要处理的游戏输入事件</param>
    public void Handle(IGameInputEvent input)
    {
        // 尝试通过上下文堆栈处理输入事件
        if (_contextStack.Handle(input))
            return;
        // 如果上下文堆栈未能处理，则发送该事件
        this.SendEvent(input);
    }

    /// <summary>
    /// 系统初始化方法，在系统启动时调用
    /// </summary>
    protected override void OnInit()
    {
        
    }
}
