using GFramework.Core.extensions;
using GFramework.Core.system;

namespace GFramework.Game.input;

/// <summary>
/// 输入系统类，负责管理输入上下文堆栈并处理游戏输入事件
/// </summary>
public class InputSystem : AbstractSystem
{
    private readonly List<IInputTranslator> _translators = [];
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

    /// <summary>
    /// 注销输入转换器
    /// </summary>
    /// <param name="translator">要注销的输入转换器接口实例</param>
    public void UnregisterTranslator(IInputTranslator translator)
        => _translators.Remove(translator);

    /// <summary>
    /// 注册输入转换器
    /// </summary>
    /// <param name="translator">输入转换器接口实例</param>
    /// <param name="highPriority">是否为高优先级，true时插入到转换器列表开头，false时添加到列表末尾</param>
    public void RegisterTranslator(IInputTranslator translator, bool highPriority = false)
    {
        if (_translators.Contains(translator))
            return;
        // 根据优先级设置决定插入位置
        if (highPriority)
            _translators.Insert(0, translator);
        else
            _translators.Add(translator);
    }


    /// <summary>
    /// 处理原始输入数据
    /// </summary>
    /// <param name="rawInput">原始输入对象</param>
    public void HandleRaw(object rawInput)
    {
        // 遍历所有注册的转换器，尝试将原始输入转换为游戏事件
        foreach (var t in _translators)
        {
            if (!t.TryTranslate(rawInput, out var gameEvent)) continue;
            Handle(gameEvent);
            return;
        }
    }

}
