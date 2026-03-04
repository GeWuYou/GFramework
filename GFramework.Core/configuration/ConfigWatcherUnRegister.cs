using GFramework.Core.Abstractions.events;

namespace GFramework.Core.Abstractions.configuration;

/// <summary>
///     配置监听取消注册接口
/// </summary>
internal sealed class ConfigWatcherUnRegister : IUnRegister
{
    private readonly Action _unRegisterAction;

    public ConfigWatcherUnRegister(Action unRegisterAction)
    {
        _unRegisterAction = unRegisterAction ?? throw new ArgumentNullException(nameof(unRegisterAction));
    }

    public void UnRegister()
    {
        _unRegisterAction();
    }
}