using System.Threading.Tasks;
using GFramework.Core.Abstractions.rule;

namespace GFramework.Core.Abstractions.command;

/// <summary>
/// 表示一个异步命令接口，该命令不返回结果
/// </summary>
public interface IAsyncCommand : IContextAware
{
    /// <summary>
    /// 异步执行命令
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    Task ExecuteAsync();
}

/// <summary>
/// 表示一个异步命令接口，该命令返回指定类型的结果
/// </summary>
/// <typeparam name="TResult">命令执行结果的类型</typeparam>
public interface IAsyncCommand<TResult> : IContextAware
{
    /// <summary>
    /// 异步执行命令并返回结果
    /// </summary>
    /// <returns>表示异步操作的任务，任务结果为命令执行的返回值</returns>
    Task<TResult> ExecuteAsync();
}