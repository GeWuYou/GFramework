namespace GWFramework.framework.events;

/// <summary>
/// TypeEventSystem
/// </summary>
public class TypeEventSystem
{
    private readonly EasyEvents _mEvents = new();

    public static readonly TypeEventSystem Global = new();

    public void Send<T>() where T : new() => _mEvents.GetEvent<EasyEvent<T>>()?.Trigger(new T());

    public void Send<T>(T e) => _mEvents.GetEvent<EasyEvent<T>>()?.Trigger(e);

    public IUnRegister Register<T>(Action<T> onEvent) => _mEvents.GetOrAddEvent<EasyEvent<T>>().Register(onEvent);

    public void UnRegister<T>(Action<T> onEvent)
    {
        var e = _mEvents.GetEvent<EasyEvent<T>>();
        e?.UnRegister(onEvent);
    }
}