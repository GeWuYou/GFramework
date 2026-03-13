using GFramework.Core.Abstractions.Architecture;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Query;

namespace GFramework.Core.Services.Modules;

/// <summary>
///     异步查询执行器模块，用于注册和管理异步查询执行器服务。
///     该模块负责将异步查询执行器注册到依赖注入容器中，并在销毁时释放相关资源。
/// </summary>
public sealed class AsyncQueryExecutorModule : IServiceModule
{
    /// <summary>
    ///     获取模块名称。
    /// </summary>
    public string ModuleName => nameof(AsyncQueryExecutorModule);

    /// <summary>
    ///     获取模块优先级，数值越小优先级越高。
    /// </summary>
    public int Priority => 40;

    /// <summary>
    ///     获取模块启用状态，始终返回 true 表示该模块默认启用。
    /// </summary>
    public bool IsEnabled => true;

    /// <summary>
    ///     注册异步查询执行器到依赖注入容器。
    ///     创建异步查询执行器实例并将其注册为多例服务。
    /// </summary>
    /// <param name="container">依赖注入容器实例。</param>
    public void Register(IIocContainer container)
    {
        container.RegisterPlurality(new AsyncQueryExecutor());
    }

    /// <summary>
    ///     初始化模块。
    ///     当前实现为空，因为异步查询执行器无需额外初始化逻辑。
    /// </summary>
    public void Initialize()
    {
    }

    /// <summary>
    ///     异步销毁模块并释放资源。
    ///     将异步查询执行器引用置空以允许垃圾回收。
    /// </summary>
    /// <returns>表示异步操作完成的任务。</returns>
    public ValueTask DestroyAsync()
    {
        return ValueTask.CompletedTask;
    }
}