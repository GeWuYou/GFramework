using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Rule;

namespace GFramework.Core.Tests.Rule;

/// <summary>
///     提供给 ContextAware 相关测试复用的上下文感知对象。
/// </summary>
public class TestContextAware : ContextAwareBase
{
    /// <summary>
    ///     获取当前测试对象已绑定的上下文实例。
    /// </summary>
    public IArchitectureContext? PublicContext => Context;

    /// <summary>
    ///     获取一个值，指示上下文就绪回调是否已经触发。
    /// </summary>
    public bool OnContextReadyCalled { get; private set; }

    /// <summary>
    ///     在上下文完成绑定后记录回调已被触发，供断言使用。
    /// </summary>
    protected override void OnContextReady()
    {
        OnContextReadyCalled = true;
    }
}
