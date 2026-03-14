using GFramework.Core.Abstractions.Architectures;

namespace GFramework.Core.Architectures;

/// <summary>
/// 作用域上下文提供者，用于多架构实例场景
/// </summary>
public sealed class ScopedContextProvider : IArchitectureContextProvider
{
    private readonly IArchitectureContext _context;

    /// <summary>
    /// 初始化作用域上下文提供者
    /// </summary>
    /// <param name="context">要绑定的架构上下文实例</param>
    public ScopedContextProvider(IArchitectureContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 获取当前的架构上下文
    /// </summary>
    /// <returns>架构上下文实例</returns>
    public IArchitectureContext GetContext() => _context;

    /// <summary>
    /// 尝试获取指定类型的架构上下文
    /// </summary>
    /// <typeparam name="T">架构上下文类型</typeparam>
    /// <param name="context">输出的上下文实例</param>
    /// <returns>如果成功获取则返回true，否则返回false</returns>
    public bool TryGetContext<T>(out T? context) where T : class, IArchitectureContext
    {
        if (_context is T typedContext)
        {
            context = typedContext;
            return true;
        }

        context = null;
        return false;
    }
}