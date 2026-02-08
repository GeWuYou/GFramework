using GFramework.Core.Abstractions.logging;
using GFramework.Core.extensions;
using GFramework.Core.logging;
using GFramework.Core.system;
using GFramework.Game.Abstractions.enums;
using GFramework.Game.Abstractions.ui;

namespace GFramework.Game.ui;

/// <summary>
///     UI路由类，提供页面栈管理功能
/// </summary>
public abstract class UiRouterBase : AbstractSystem, IUiRouter
{
    private static readonly ILogger Log = LoggerFactoryResolver.Provider.CreateLogger("UiRouterBase");

    /// <summary>
    ///     路由守卫列表
    /// </summary>
    private readonly List<IUiRouteGuard> _guards = new();

    /// <summary>
    ///     层级管理（非栈层级），用于Overlay、Modal、Toast等浮层
    ///     Key: UiLayer, Value: Dictionary of InstanceId -> PageBehavior
    /// </summary>
    private readonly Dictionary<UiLayer, Dictionary<string, IUiPageBehavior>> _layers = new();

    /// <summary>
    ///     UI切换处理器管道
    /// </summary>
    private readonly UiTransitionPipeline _pipeline = new();

    /// <summary>
    ///     页面栈，用于管理UI页面的显示顺序
    /// </summary>
    private readonly Stack<IUiPageBehavior> _stack = new();

    /// <summary>
    ///     UI工厂实例，用于创建UI相关的对象
    /// </summary>
    private IUiFactory _factory = null!;

    /// <summary>
    ///     实例ID计数器，用于生成唯一ID
    /// </summary>
    private int _instanceCounter;

    private IUiRoot _uiRoot = null!;

    /// <summary>
    ///     注册UI切换处理器
    /// </summary>
    public void RegisterHandler(IUiTransitionHandler handler, UiTransitionHandlerOptions? options = null)
    {
        _pipeline.RegisterHandler(handler, options);
    }

    /// <summary>
    ///     注销UI切换处理器
    /// </summary>
    public void UnregisterHandler(IUiTransitionHandler handler)
    {
        _pipeline.UnregisterHandler(handler);
    }

    /// <summary>
    ///     绑定UI根节点
    /// </summary>
    public void BindRoot(IUiRoot root)
    {
        _uiRoot = root;
        Log.Debug("Bind UI Root: {0}", root.GetType().Name);
    }

    #region Page Stack Management

    /// <summary>
    ///     将指定的UI界面压入路由栈
    /// </summary>
    public void Push(string uiKey, IUiPageEnterParam? param = null,
        UiTransitionPolicy policy = UiTransitionPolicy.Exclusive)
    {
        if (IsTop(uiKey))
        {
            Log.Warn("Push ignored: UI already on top: {0}", uiKey);
            return;
        }

        var @event = CreateEvent(uiKey, UiTransitionType.Push, policy, param);
        Log.Debug("Push UI Page: key={0}, policy={1}, stackBefore={2}", uiKey, policy, _stack.Count);

        BeforeChange(@event);
        DoPushPageInternal(uiKey, param, policy);
        AfterChange(@event);
    }

    /// <summary>
    ///     将已存在的UI页面压入栈顶
    /// </summary>
    public void Push(IUiPageBehavior page, IUiPageEnterParam? param = null,
        UiTransitionPolicy policy = UiTransitionPolicy.Exclusive)
    {
        var uiKey = page.Key;

        if (IsTop(uiKey))
        {
            Log.Warn("Push ignored: UI already on top: {0}", uiKey);
            return;
        }

        var @event = CreateEvent(uiKey, UiTransitionType.Push, policy, param);
        Log.Debug("Push existing UI Page: key={0}, policy={1}, stackBefore={2}", uiKey, policy, _stack.Count);

        BeforeChange(@event);
        DoPushPageInternal(page, param, policy);
        AfterChange(@event);
    }

    /// <summary>
    ///     弹出栈顶页面
    /// </summary>
    public void Pop(UiPopPolicy policy = UiPopPolicy.Destroy)
    {
        if (_stack.Count == 0)
        {
            Log.Debug("Pop ignored: stack is empty");
            return;
        }

        var leavingUiKey = _stack.Peek().Key;

        if (!ExecuteLeaveGuardsAsync(leavingUiKey).GetAwaiter().GetResult())
        {
            Log.Warn("Pop blocked by guard: {0}", leavingUiKey);
            return;
        }

        var nextUiKey = _stack.Count > 1 ? _stack.ElementAt(1).Key : null;
        var @event = CreateEvent(nextUiKey, UiTransitionType.Pop);

        BeforeChange(@event);
        DoPopInternal(policy);
        AfterChange(@event);
    }

    /// <summary>
    ///     替换当前所有页面为新页面（基于uiKey）
    /// </summary>
    public void Replace(string uiKey, IUiPageEnterParam? param = null,
        UiPopPolicy popPolicy = UiPopPolicy.Destroy,
        UiTransitionPolicy pushPolicy = UiTransitionPolicy.Exclusive)
    {
        var @event = CreateEvent(uiKey, UiTransitionType.Replace, pushPolicy, param);
        Log.Debug("Replace UI Stack with page: key={0}, popPolicy={1}, pushPolicy={2}", uiKey, popPolicy, pushPolicy);

        BeforeChange(@event);
        DoClearInternal(popPolicy);

        var page = _factory.Create(uiKey);
        Log.Debug("Get/Create UI Page instance for Replace: {0}", page.GetType().Name);

        DoPushPageInternal(page, param, pushPolicy);
        AfterChange(@event);
    }

    /// <summary>
    ///     替换当前所有页面为已存在的页面
    /// </summary>
    public void Replace(IUiPageBehavior page, IUiPageEnterParam? param = null,
        UiPopPolicy popPolicy = UiPopPolicy.Destroy,
        UiTransitionPolicy pushPolicy = UiTransitionPolicy.Exclusive)
    {
        var uiKey = page.Key;
        var @event = CreateEvent(uiKey, UiTransitionType.Replace, pushPolicy, param);
        Log.Debug("Replace UI Stack with existing page: key={0}, popPolicy={1}, pushPolicy={2}",
            uiKey, popPolicy, pushPolicy);

        BeforeChange(@event);
        DoClearInternal(popPolicy);
        Log.Debug("Use existing UI Page instance for Replace: {0}", page.GetType().Name);
        DoPushPageInternal(page, param, pushPolicy);
        AfterChange(@event);
    }

    /// <summary>
    ///     清空所有页面栈
    /// </summary>
    public void Clear()
    {
        var @event = CreateEvent(string.Empty, UiTransitionType.Clear);
        Log.Debug("Clear UI Stack, stackCount={0}", _stack.Count);

        BeforeChange(@event);
        DoClearInternal(UiPopPolicy.Destroy);
        AfterChange(@event);
    }

    /// <summary>
    ///     获取栈顶元素的键值
    /// </summary>
    public string PeekKey()
    {
        return _stack.Count == 0 ? string.Empty : _stack.Peek().Key;
    }

    /// <summary>
    ///     获取栈顶元素
    /// </summary>
    public IUiPageBehavior? Peek()
    {
        return _stack.Count == 0 ? null : _stack.Peek();
    }

    /// <summary>
    ///     判断栈顶是否为指定UI
    /// </summary>
    public bool IsTop(string uiKey)
    {
        return _stack.Count != 0 && _stack.Peek().Key.Equals(uiKey);
    }

    /// <summary>
    ///     判断栈中是否包含指定UI
    /// </summary>
    public bool Contains(string uiKey)
    {
        return _stack.Any(p => p.Key.Equals(uiKey));
    }

    /// <summary>
    ///     获取栈深度
    /// </summary>
    public int Count => _stack.Count;

    #endregion

    #region Layer UI Management

    /// <summary>
    ///     在指定层级显示UI（基于 uiKey）
    /// </summary>
    public UiHandle Show(string uiKey, UiLayer layer, IUiPageEnterParam? param = null)
    {
        if (layer == UiLayer.Page)
            throw new ArgumentException("Use Push() for Page layer");

        // 创建实例
        var page = _factory.Create(uiKey);

        return ShowInternal(page, layer, param);
    }

    /// <summary>
    ///     在指定层级显示UI（基于实例）
    /// </summary>
    public UiHandle Show(IUiPageBehavior page, UiLayer layer)
    {
        if (layer == UiLayer.Page)
            throw new ArgumentException("Use Push() for Page layer");

        return ShowInternal(page, layer, null);
    }

    /// <summary>
    ///     隐藏指定层级的UI
    /// </summary>
    public void Hide(UiHandle handle, UiLayer layer, bool destroy = false)
    {
        if (!_layers.TryGetValue(layer, out var layerDict))
            return;

        if (!layerDict.TryGetValue(handle.InstanceId, out var page))
            return;

        if (destroy)
        {
            page.OnExit();
            _uiRoot.RemoveUiPage(page);
            layerDict.Remove(handle.InstanceId);
            Log.Debug("Hide & Destroy UI: instanceId={0}, layer={1}", handle.InstanceId, layer);
        }
        else
        {
            page.OnHide();
            Log.Debug("Hide UI (suspend): instanceId={0}, layer={1}", handle.InstanceId, layer);
        }
    }

    /// <summary>
    ///     恢复指定UI的显示
    /// </summary>
    public void Resume(UiHandle handle, UiLayer layer)
    {
        if (!_layers.TryGetValue(layer, out var layerDict))
            return;

        if (!layerDict.TryGetValue(handle.InstanceId, out var page))
            return;

        page.OnShow();
        page.OnResume();
        Log.Debug("Resume UI: instanceId={0}, layer={1}", handle.InstanceId, layer);
    }

    /// <summary>
    ///     清空指定层级的所有UI
    /// </summary>
    public void ClearLayer(UiLayer layer, bool destroy = false)
    {
        if (!_layers.TryGetValue(layer, out var layerDict))
            return;

        var handles = layerDict.Keys
            .Select(instanceId =>
            {
                var page = layerDict[instanceId];
                return new UiHandle(page.Key, instanceId, layer);
            })
            .ToArray();

        foreach (var handle in handles)
            Hide(handle, layer, destroy);

        Log.Debug("Cleared layer: {0}, destroyed={1}", layer, destroy);
    }

    /// <summary>
    ///     获取指定层级的UI实例
    /// </summary>
    public UiHandle? GetFromLayer(UiHandle handle, UiLayer layer)
    {
        if (!_layers.TryGetValue(layer, out var layerDict))
            return null;

        return layerDict.ContainsKey(handle.InstanceId) ? handle : null;
    }

    /// <summary>
    ///     获取指定 uiKey 在指定层级的所有实例
    /// </summary>
    public IReadOnlyList<UiHandle> GetAllFromLayer(string uiKey, UiLayer layer)
    {
        if (!_layers.TryGetValue(layer, out var layerDict))
            return Array.Empty<UiHandle>();

        return layerDict
            .Where(kvp => kvp.Value.Key.Equals(uiKey))
            .Select(kvp => new UiHandle(uiKey, kvp.Key, layer))
            .ToList();
    }

    /// <summary>
    ///     判断指定UI是否在层级中可见
    /// </summary>
    public bool HasVisibleInLayer(UiHandle handle, UiLayer layer)
    {
        if (!_layers.TryGetValue(layer, out var layerDict))
            return false;

        if (!layerDict.TryGetValue(handle.InstanceId, out var page))
            return false;

        return page.IsVisible;
    }

    /// <summary>
    ///     根据UI键隐藏指定层级中的UI。
    /// </summary>
    /// <param name="uiKey">UI的唯一标识键。</param>
    /// <param name="layer">要操作的UI层级。</param>
    /// <param name="destroy">是否销毁UI实例，默认为false。</param>
    /// <param name="hideAll">是否隐藏所有匹配的UI实例，默认为false。</param>
    public void HideByKey(string uiKey, UiLayer layer, bool destroy = false, bool hideAll = false)
    {
        var handles = GetAllFromLayer(uiKey, layer);
        if (handles.Count == 0) return;

        if (hideAll)
            foreach (var h in handles)
            {
                Hide(h, layer, destroy);
            }
        else
            Hide(handles[0], layer, destroy);
    }

    #endregion

    #region Route Guards

    /// <summary>
    ///     注册路由守卫
    /// </summary>
    public void AddGuard(IUiRouteGuard guard)
    {
        ArgumentNullException.ThrowIfNull(guard);

        if (_guards.Contains(guard))
        {
            Log.Debug("Guard already registered: {0}", guard.GetType().Name);
            return;
        }

        _guards.Add(guard);
        _guards.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        Log.Debug("Guard registered: {0}, Priority={1}", guard.GetType().Name, guard.Priority);
    }

    /// <summary>
    ///     注册路由守卫（泛型）
    /// </summary>
    public void AddGuard<T>() where T : IUiRouteGuard, new()
    {
        AddGuard(new T());
    }

    /// <summary>
    ///     移除路由守卫
    /// </summary>
    public void RemoveGuard(IUiRouteGuard guard)
    {
        ArgumentNullException.ThrowIfNull(guard);
        if (_guards.Remove(guard))
            Log.Debug("Guard removed: {0}", guard.GetType().Name);
    }

    #endregion

    #region Initialization

    /// <summary>
    /// 初始化函数，在对象创建时调用。
    /// 该函数负责获取UI工厂实例并注册处理程序。
    /// </summary>
    protected override void OnInit()
    {
        // 获取UI工厂实例，并确保其不为null
        _factory = this.GetUtility<IUiFactory>()!;

        // 输出调试日志，记录UI路由器基类已初始化及使用的工厂类型
        Log.Debug("UiRouterBase initialized. Factory={0}", _factory.GetType().Name);

        // 调用抽象方法以注册具体的处理程序
        RegisterHandlers();
    }

    /// <summary>
    /// 抽象方法，用于注册具体的处理程序。
    /// 子类必须实现此方法以完成特定的处理逻辑注册。
    /// </summary>
    protected abstract void RegisterHandlers();

    #endregion

    #region Internal Helpers

    /// <summary>
    ///     生成唯一实例ID
    /// </summary>
    /// <returns>格式为"ui_000001"的唯一实例标识符</returns>
    private string GenerateInstanceId()
    {
        // 原子操作递增实例计数器，确保多线程环境下的唯一性
        var id = Interlocked.Increment(ref _instanceCounter);
        // 返回格式化的实例ID字符串
        return $"ui_{id:D6}";
    }

    /// <summary>
    ///     内部Show实现，支持重入
    /// </summary>
    private UiHandle ShowInternal(IUiPageBehavior page, UiLayer layer, IUiPageEnterParam? param)
    {
        var instanceId = GenerateInstanceId();
        var handle = new UiHandle(page.Key, instanceId, layer);

        // 初始化层级字典
        if (!_layers.ContainsKey(layer))
            _layers[layer] = new Dictionary<string, IUiPageBehavior>();
        // 设置句柄
        page.Handle = handle;
        var layerDict = _layers[layer];

        // 检查重入性
        if (!page.IsReentrant && layerDict.Values.Any(p => p.Key == page.Key))
        {
            Log.Warn("UI {0} is not reentrant but already exists in layer {1}", page.Key, layer);
            throw new InvalidOperationException(
                $"UI {page.Key} does not support multiple instances in layer {layer}");
        }

        // 添加到层级管理
        layerDict[instanceId] = page;

        // 添加到UiRoot
        _uiRoot.AddUiPage(page, layer);

        // 生命周期
        page.OnEnter(param);
        page.OnShow();

        Log.Debug("Show UI: key={0}, instanceId={1}, layer={2}", page.Key, instanceId, layer);
        return handle;
    }

    private UiTransitionEvent CreateEvent(string? toUiKey, UiTransitionType type,
        UiTransitionPolicy? policy = null, IUiPageEnterParam? param = null)
    {
        return new UiTransitionEvent
        {
            FromUiKey = PeekKey(),
            ToUiKey = toUiKey,
            TransitionType = type,
            Policy = policy ?? UiTransitionPolicy.Exclusive,
            EnterParam = param
        };
    }

    private void BeforeChange(UiTransitionEvent @event)
    {
        Log.Debug("BeforeChange phases started: {0}", @event.TransitionType);
        _pipeline.ExecuteAsync(@event, UITransitionPhases.BeforeChange).GetAwaiter().GetResult();
        Log.Debug("BeforeChange phases completed: {0}", @event.TransitionType);
    }

    private void AfterChange(UiTransitionEvent @event)
    {
        Log.Debug("AfterChange phases started: {0}", @event.TransitionType);
        _ = Task.Run<Task>(async () =>
        {
            try
            {
                await _pipeline.ExecuteAsync(@event, UITransitionPhases.AfterChange).ConfigureAwait(false);
                Log.Debug("AfterChange phases completed: {0}", @event.TransitionType);
            }
            catch (Exception ex)
            {
                Log.Error("AfterChange phases failed: {0}, Error: {1}", @event.TransitionType, ex.Message);
            }
        });
    }

    private void DoPushPageInternal(string uiKey, IUiPageEnterParam? param, UiTransitionPolicy policy)
    {
        if (!ExecuteEnterGuardsAsync(uiKey, param).GetAwaiter().GetResult())
        {
            Log.Warn("Push blocked by guard: {0}", uiKey);
            return;
        }

        var page = _factory.Create(uiKey);
        Log.Debug("Get/Create UI Page instance: {0}", page.GetType().Name);
        DoPushPageInternal(page, param, policy);
    }

    private void DoPushPageInternal(IUiPageBehavior page, IUiPageEnterParam? param, UiTransitionPolicy policy)
    {
        if (_stack.Count > 0)
        {
            var current = _stack.Peek();
            Log.Debug("Pause current page: {0}", current.View.GetType().Name);
            current.OnPause();

            if (policy == UiTransitionPolicy.Exclusive)
            {
                Log.Debug("Suspend current page (Exclusive): {0}", current.View.GetType().Name);
                current.OnHide();
            }
        }

        Log.Debug("Add page to UiRoot: {0}", page.View.GetType().Name);
        _uiRoot.AddUiPage(page);

        _stack.Push(page);

        Log.Debug("Enter & Show page: {0}, stackAfter={1}", page.View.GetType().Name, _stack.Count);
        page.OnEnter(param);
        page.OnShow();
    }

    private void DoPopInternal(UiPopPolicy policy)
    {
        if (_stack.Count == 0)
            return;

        var top = _stack.Pop();
        Log.Debug("Pop UI Page internal: {0}, policy={1}, stackAfterPop={2}",
            top.GetType().Name, policy, _stack.Count);

        if (policy == UiPopPolicy.Destroy)
        {
            top.OnExit();
            _uiRoot.RemoveUiPage(top);
        }
        else
        {
            top.OnHide();
        }

        if (_stack.Count > 0)
        {
            var next = _stack.Peek();
            next.OnResume();
            next.OnShow();
        }
    }

    private void DoClearInternal(UiPopPolicy policy)
    {
        Log.Debug("Clear UI Stack internal, count={0}", _stack.Count);
        while (_stack.Count > 0)
            DoPopInternal(policy);
    }

    private async Task<bool> ExecuteEnterGuardsAsync(string uiKey, IUiPageEnterParam? param)
    {
        foreach (var guard in _guards)
        {
            try
            {
                Log.Debug("Executing enter guard: {0} for {1}", guard.GetType().Name, uiKey);
                var canEnter = await guard.CanEnterAsync(uiKey, param);

                if (!canEnter)
                {
                    Log.Debug("Enter guard blocked: {0}", guard.GetType().Name);
                    return false;
                }

                if (guard.CanInterrupt)
                {
                    Log.Debug("Enter guard {0} passed, can interrupt = true", guard.GetType().Name);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Enter guard {0} failed: {1}", guard.GetType().Name, ex.Message);
                if (guard.CanInterrupt)
                    return false;
            }
        }

        return true;
    }

    private async Task<bool> ExecuteLeaveGuardsAsync(string uiKey)
    {
        foreach (var guard in _guards)
        {
            try
            {
                Log.Debug("Executing leave guard: {0} for {1}", guard.GetType().Name, uiKey);
                var canLeave = await guard.CanLeaveAsync(uiKey);

                if (!canLeave)
                {
                    Log.Debug("Leave guard blocked: {0}", guard.GetType().Name);
                    return false;
                }

                if (guard.CanInterrupt)
                {
                    Log.Debug("Leave guard {0} passed, can interrupt = true", guard.GetType().Name);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Leave guard {0} failed: {1}", guard.GetType().Name, ex.Message);
                if (guard.CanInterrupt)
                    return false;
            }
        }

        return true;
    }

    #endregion
}