using GFramework.Core.logging;

namespace GFramework.Core.events;

/// <summary>
///     TypeEventSystem
/// </summary>
public class TypeEventSystem
{
    public static readonly TypeEventSystem Global = new();
    private readonly EasyEvents _mEvents = new();

    public void Send<T>() where T : new()
    {
        var logger = Log.CreateLogger("Event");
        var eventType = typeof(T);
        
        logger.Debug($"Sending event: {eventType.Name}");
        _mEvents.GetEvent<EasyEvent<T>>()?.Trigger(new T());
        logger.Info($"Event sent: {eventType.Name}");
    }

    public void Send<T>(T e)
    {
        var logger = Log.CreateLogger("Event");
        var eventType = typeof(T);
        
        logger.Debug($"Sending event: {eventType.Name}");
        _mEvents.GetEvent<EasyEvent<T>>()?.Trigger(e);
        logger.Info($"Event sent: {eventType.Name}");
    }

    public IUnRegister Register<T>(Action<T> onEvent)
    {
        var logger = Log.CreateLogger("Event");
        var eventType = typeof(T);
        
        logger.Debug($"Registering event handler for: {eventType.Name}");
        var result = _mEvents.GetOrAddEvent<EasyEvent<T>>().Register(onEvent);
        logger.Info($"Event handler registered for: {eventType.Name}");
        return result;
    }

    public void UnRegister<T>(Action<T> onEvent)
    {
        var logger = Log.CreateLogger("Event");
        var eventType = typeof(T);
        
        logger.Debug($"Unregistering event handler for: {eventType.Name}");
        var e = _mEvents.GetEvent<EasyEvent<T>>();
        e?.UnRegister(onEvent);
        logger.Info($"Event handler unregistered for: {eventType.Name}");
    }
}