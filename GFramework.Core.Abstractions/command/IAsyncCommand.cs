using System.Threading.Tasks;
using GFramework.Core.Abstractions.rule;

namespace GFramework.Core.Abstractions.command;

public interface IAsyncCommand : IContextAware
{
    Task ExecuteAsync();
}

public interface IAsyncCommand<TResult> : IContextAware
{
    Task<TResult> ExecuteAsync();
}