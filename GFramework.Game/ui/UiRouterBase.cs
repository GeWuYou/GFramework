using GFramework.Core.Abstractions.logging;
using GFramework.Core.extensions;
using GFramework.Core.logging;
using GFramework.Core.system;
using GFramework.Game.Abstractions.enums;
using GFramework.Game.Abstractions.ui;

namespace GFramework.Game.ui;

/// <summary>
/// UI路由基类，提供页面栈管理和层级UI管理功能
/// 负责UI页面的导航、显示、隐藏以及生命周期管理
/// </summary>
public abstract class UiRouterBase : AbstractSystem, IUiRouter
{
    private static readonly ILogger Log = LoggerFactoryResolver.Provider.CreateLogger(nameof(UiRouterBase));

    /// <summary>
    /// 路由守卫列表，用于控制UI页面的进入和离开
    /// </summary>
    private readonly List<IUiRouteGuard> _guards = new();

    /// <summary>
    /// 层级管理字典（非栈层级），用于管理Overlay、Modal、Toast等浮层UI
    /// Key: UiLayer枚举值, Value: InstanceId到PageBehavior的映射字典
    /// </summary>
    private readonly Dictionary<UiLayer, Dictionary<string, IUiPageBehavior>> _layers = new();

    /// <summary>
    /// UI切换处理器管道，用于执行UI过渡动画和逻辑
    /// </summary>
    private readonly UiTransitionPipeline _pipeline = new();

    /// <summary>
    /// 页面栈，用于管理UI页面的显示顺序和导航历史
    /// </summary>
    private readonly Stack<IUiPageBehavior> _stack = new();

    /// <summary>
    /// UI工厂实例，用于创建UI页面和相关对象
    /// </summary>
    private IUiFactory _factory = null!;

    /// <summary>
    /// 实例ID计数器，用于生成唯一的UI实例标识符
    /// </summary>
    private int _instanceCounter;

    /// <summary>
    /// UI根节点引用，用于添加和移除UI页面
    /// </summary>
    private IUiRoot _uiRoot = null!;

    /// <summary>
    /// 注册UI切换处理器
    /// </summary>
    /// <param name="handler">UI切换处理器实例</param>
    /// <param name="options">处理器选项配置</param>
    public void RegisterHandler(IUiTransitionHandler handler, UiTransitionHandlerOptions? options = null)
    {
        _pipeline.RegisterHandler(handler, options);
    }

    /// <summary>
    /// 注销UI切换处理器
    /// </summary>
    /// <param name="handler">要注销的UI切换处理器实例</param>
    public void UnregisterHandler(IUiTransitionHandler handler)
    {
        _pipeline.UnregisterHandler(handler);
    }

    /// <summary>
    /// 绑定UI根节点
    /// </summary>
    /// <param name="root">UI根节点实例</param>
    public void BindRoot(IUiRoot root)
    {
        _uiRoot = root;
        Log.Debug("Bind UI Root: {0}", root.GetType().Name);
    }

    #region Page Stack Management

    /// <summary>
    /// 将指定的UI界面压入路由栈
    /// </summary>
    /// <param name="uiKey">UI页面的唯一标识键</param>
    /// <param name="param">页面进入参数</param>
    /// <param name="policy">UI过渡策略</param>
    public async ValueTask PushAsync(string uiKey, IUiPageEnterParam? param = null,
        UiTransitionPolicy policy = UiTransitionPolicy.Exclusive)
    {
        if (IsTop(uiKey))
        {
            Log.Warn("Push ignored: UI already on top: {0}", uiKey);
            return;
        }

        var @event = CreateEvent(uiKey, UiTransitionType.Push, policy, param);
        Log.Debug("Push UI Page: key={0}, policy={1}, stackBefore={2}", uiKey, policy, _stack.Count);

        await _pipeline.ExecuteAroundAsync(@event, async () =>
        {
            await BeforeChangeAsync(@event);
            await DoPushPageInternalAsync(uiKey, param, policy);
            await AfterChangeAsync(@event);
        });
    }

    /// <summary>
    /// 将已存在的UI页面压入栈顶
    /// </summary>
    /// <param name="page">已存在的UI页面行为实例</param>
    /// <param name="param">页面进入参数</param>
    /// <param name="policy">UI过渡策略</param>
    public async ValueTask PushAsync(IUiPageBehavior page, IUiPageEnterParam? param = null,
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

        await _pipeline.ExecuteAroundAsync(@event, async () =>
        {
            await BeforeChangeAsync(@event);
            DoPushPageInternal(page, param, policy);
            await AfterChangeAsync(@event);
        });
    }

    /// <summary>
    /// 弹出栈顶页面
    /// </summary>
    /// <param name="policy">页面弹出策略</param>
    public async ValueTask PopAsync(UiPopPolicy policy = UiPopPolicy.Destroy)
    {
        if (_stack.Count == 0)
        {
            Log.Debug("Pop ignored: stack is empty");
            return;
        }

        var leavingUiKey = _stack.Peek().Key;

        if (!await ExecuteLeaveGuardsAsync(leavingUiKey))
        {
            Log.Warn("Pop blocked by guard: {0}", leavingUiKey);
            return;
        }

        var nextUiKey = _stack.Count > 1 ? _stack.ElementAt(1).Key : null;
        var @event = CreateEvent(nextUiKey, UiTransitionType.Pop);

        await _pipeline.ExecuteAroundAsync(@event, async () =>
        {
            await BeforeChangeAsync(@event);
            DoPopInternal(policy);
            await AfterChangeAsync(@event);
        });
    }

    /// <summary>
    /// 替换当前所有页面为新页面（基于uiKey）
    /// </summary>
    /// <param name="uiKey">新UI页面的唯一标识键</param>
    /// <param name="param">页面进入参数</param>
    /// <param name="popPolicy">页面弹出策略</param>
    /// <param name="pushPolicy">页面压入策略</param>
    public async ValueTask ReplaceAsync(string uiKey, IUiPageEnterParam? param = null,
        UiPopPolicy popPolicy = UiPopPolicy.Destroy,
        UiTransitionPolicy pushPolicy = UiTransitionPolicy.Exclusive)
    {
        var @event = CreateEvent(uiKey, UiTransitionType.Replace, pushPolicy, param);
        Log.Debug("Replace UI Stack with page: key={0}, popPolicy={1}, pushPolicy={2}", uiKey, popPolicy, pushPolicy);

        await _pipeline.ExecuteAroundAsync(@event, async () =>
        {
            await BeforeChangeAsync(@event);
            DoClearInternal(popPolicy);

            var page = _factory.Create(uiKey);
            Log.Debug("Get/Create UI Page instance for Replace: {0}", page.GetType().Name);

            DoPushPageInternal(page, param, pushPolicy);
            await AfterChangeAsync(@event);
        });
    }

    /// <summary>
    /// 替换当前所有页面为已存在的页面
    /// </summary>
    /// <param name="page">已存在的UI页面行为实例</param>
    /// <param name="param">页面进入参数</param>
    /// <param name="popPolicy">页面弹出策略</param>
    /// <param name="pushPolicy">页面压入策略</param>
    public async ValueTask ReplaceAsync(IUiPageBehavior page, IUiPageEnterParam? param = null,
        UiPopPolicy popPolicy = UiPopPolicy.Destroy,
        UiTransitionPolicy pushPolicy = UiTransitionPolicy.Exclusive)
    {
        var uiKey = page.Key;
        var @event = CreateEvent(uiKey, UiTransitionType.Replace, pushPolicy, param);
        Log.Debug("Replace UI Stack with existing page: key={0}, popPolicy={1}, pushPolicy={2}",
            uiKey, popPolicy, pushPolicy);

        await _pipeline.ExecuteAroundAsync(@event, async () =>
        {
            await BeforeChangeAsync(@event);
            DoClearInternal(popPolicy);
            Log.Debug("Use existing UI Page instance for Replace: {0}", page.GetType().Name);
            DoPushPageInternal(page, param, pushPolicy);
            await AfterChangeAsync(@event);
        });
    }

    /// <summary>
    /// 清空所有页面栈
    /// </summary>
    public async ValueTask ClearAsync()
    {
        var @event = CreateEvent(string.Empty, UiTransitionType.Clear);
        Log.Debug("Clear UI Stack, stackCount={0}", _stack.Count);

        await _pipeline.ExecuteAroundAsync(@event, async () =>
        {
            await BeforeChangeAsync(@event);
            DoClearInternal(UiPopPolicy.Destroy);
            await AfterChangeAsync(@event);
        });
    }

    /// <summary>
    /// 获取栈顶元素的键值
    /// </summary>
    /// <returns>栈顶UI页面的键值，如果栈为空则返回空字符串</returns>
    public string PeekKey()
    {
        return _stack.Count == 0 ? string.Empty : _stack.Peek().Key;
    }

    /// <summary>
    /// 获取栈顶元素
    /// </summary>
    /// <returns>栈顶UI页面行为实例，如果栈为空则返回null</returns>
    public IUiPageBehavior? Peek()
    {
        return _stack.Count == 0 ? null : _stack.Peek();
    }

    /// <summary>
    /// 判断栈顶是否为指定UI
    /// </summary>
    /// <param name="uiKey">要检查的UI页面键值</param>
    /// <returns>如果栈顶是指定UI则返回true，否则返回false</returns>
    public bool IsTop(string uiKey)
    {
        return _stack.Count != 0 && _stack.Peek().Key.Equals(uiKey);
    }

    /// <summary>
    /// 判断栈中是否包含指定UI
    /// </summary>
    /// <param name="uiKey">要检查的UI页面键值</param>
    /// <returns>如果栈中包含指定UI则返回true，否则返回false</returns>
    public bool Contains(string uiKey)
    {
        return _stack.Any(p => p.Key.Equals(uiKey));
    }

    /// <summary>
    /// 获取栈深度
    /// </summary>
    public int Count => _stack.Count;

    #endregion

    #region Layer UI Management

    /// <summary>
    /// 在指定层级显示UI（基于 uiKey）
    /// </summary>
    /// <param name="uiKey">UI页面的唯一标识键</param>
    /// <param name="layer">UI显示层级</param>
    /// <param name="param">页面进入参数</param>
    /// <returns>UI句柄实例</returns>
    /// <exception cref="ArgumentException">当尝试在Page层级使用此方法时抛出</exception>
    public UiHandle Show(string uiKey, UiLayer layer, IUiPageEnterParam? param = null)
    {
        if (layer == UiLayer.Page)
            throw new ArgumentException("Use Push() for Page layer");

        // 创建实例
        var page = _factory.Create(uiKey);

        return ShowInternal(page, layer, param);
    }

    /// <summary>
    /// 在指定层级显示UI（基于实例）
    /// </summary>
    /// <param name="page">UI页面行为实例</param>
    /// <param name="layer">UI显示层级</param>
    /// <returns>UI句柄实例</returns>
    /// <exception cref="ArgumentException">当尝试在Page层级使用此方法时抛出</exception>
    public UiHandle Show(IUiPageBehavior page, UiLayer layer)
    {
        if (layer == UiLayer.Page)
            throw new ArgumentException("Use Push() for Page layer");

        return ShowInternal(page, layer, null);
    }

    /// <summary>
    /// 隐藏指定层级的UI
    /// </summary>
    /// <param name="handle">UI句柄</param>
    /// <param name="layer">UI层级</param>
    /// <param name="destroy">是否销毁UI实例，默认为false</param>
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
    /// 恢复指定UI的显示
    /// </summary>
    /// <param name="handle">UI句柄</param>
    /// <param name="layer">UI层级</param>
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
    /// 清空指定层级的所有UI
    /// </summary>
    /// <param name="layer">要清空的UI层级</param>
    /// <param name="destroy">是否销毁UI实例，默认为false</param>
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
    /// 获取指定层级的UI实例
    /// </summary>
    /// <param name="handle">UI句柄</param>
    /// <param name="layer">UI层级</param>
    /// <returns>如果找到则返回UI句柄，否则返回null</returns>
    public UiHandle? GetFromLayer(UiHandle handle, UiLayer layer)
    {
        if (!_layers.TryGetValue(layer, out var layerDict))
            return null;

        return layerDict.ContainsKey(handle.InstanceId) ? handle : null;
    }

    /// <summary>
    /// 获取指定 uiKey 在指定层级的所有实例
    /// </summary>
    /// <param name="uiKey">UI页面的唯一标识键</param>
    /// <param name="layer">UI层级</param>
    /// <returns>指定UI在该层级的所有实例句柄列表</returns>
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
    /// 判断指定UI是否在层级中可见
    /// </summary>
    /// <param name="handle">UI句柄</param>
    /// <param name="layer">UI层级</param>
    /// <returns>如果UI在层级中且可见则返回true，否则返回false</returns>
    public bool HasVisibleInLayer(UiHandle handle, UiLayer layer)
    {
        if (!_layers.TryGetValue(layer, out var layerDict))
            return false;

        if (!layerDict.TryGetValue(handle.InstanceId, out var page))
            return false;

        return page.IsVisible;
    }

    /// <summary>
    /// 根据UI键隐藏指定层级中的UI。
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
    /// 注册路由守卫
    /// </summary>
    /// <param name="guard">路由守卫实例</param>
    /// <exception cref="ArgumentNullException">当守卫实例为null时抛出</exception>
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
    /// 注册路由守卫（泛型）
    /// </summary>
    /// <typeparam name="T">路由守卫类型，必须实现IUiRouteGuard接口且有无参构造函数</typeparam>
    public void AddGuard<T>() where T : IUiRouteGuard, new()
    {
        AddGuard(new T());
    }

    /// <summary>
    /// 移除路由守卫
    /// </summary>
    /// <param name="guard">要移除的路由守卫实例</param>
    /// <exception cref="ArgumentNullException">当守卫实例为null时抛出</exception>
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
    /// 生成唯一实例ID
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
    /// 内部Show实现，支持重入
    /// </summary>
    /// <param name="page">UI页面行为实例</param>
    /// <param name="layer">UI显示层级</param>
    /// <param name="param">页面进入参数</param>
    /// <returns>UI句柄实例</returns>
    /// <exception cref="InvalidOperationException">当UI不支持重入且已在该层级存在时抛出</exception>
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

    /// <summary>
    /// 创建UI过渡事件
    /// </summary>
    /// <param name="toUiKey">目标UI键值</param>
    /// <param name="type">过渡类型</param>
    /// <param name="policy">过渡策略</param>
    /// <param name="param">进入参数</param>
    /// <returns>UI过渡事件实例</returns>
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

    /// <summary>
    /// 执行过渡前阶段
    /// </summary>
    /// <param name="event">UI过渡事件</param>
    private async Task BeforeChangeAsync(UiTransitionEvent @event)
    {
        Log.Debug("BeforeChange phases started: {0}", @event.TransitionType);
        await _pipeline.ExecuteAsync(@event, UiTransitionPhases.BeforeChange);
        Log.Debug("BeforeChange phases completed: {0}", @event.TransitionType);
    }

    /// <summary>
    /// 执行过渡后阶段
    /// </summary>
    /// <param name="event">UI过渡事件</param>
    private async Task AfterChangeAsync(UiTransitionEvent @event)
    {
        Log.Debug("AfterChange phases started: {0}", @event.TransitionType);
        await _pipeline.ExecuteAsync(@event, UiTransitionPhases.AfterChange);
        Log.Debug("AfterChange phases completed: {0}", @event.TransitionType);
    }

    /// <summary>
    /// 内部异步压入页面实现
    /// </summary>
    /// <param name="uiKey">UI页面键值</param>
    /// <param name="param">页面进入参数</param>
    /// <param name="policy">过渡策略</param>
    private async Task DoPushPageInternalAsync(string uiKey, IUiPageEnterParam? param, UiTransitionPolicy policy)
    {
        if (!await ExecuteEnterGuardsAsync(uiKey, param))
        {
            Log.Warn("Push blocked by guard: {0}", uiKey);
            return;
        }

        var page = _factory.Create(uiKey);
        Log.Debug("Get/Create UI Page instance: {0}", page.GetType().Name);
        DoPushPageInternal(page, param, policy);
    }

    /// <summary>
    /// 内部压入页面实现
    /// </summary>
    /// <param name="page">UI页面行为实例</param>
    /// <param name="param">页面进入参数</param>
    /// <param name="policy">过渡策略</param>
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

    /// <summary>
    /// 内部弹出页面实现
    /// </summary>
    /// <param name="policy">页面弹出策略</param>
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

    /// <summary>
    /// 内部清空页面实现
    /// </summary>
    /// <param name="policy">页面弹出策略</param>
    private void DoClearInternal(UiPopPolicy policy)
    {
        Log.Debug("Clear UI Stack internal, count={0}", _stack.Count);
        while (_stack.Count > 0)
            DoPopInternal(policy);
    }

    /// <summary>
    /// 执行进入守卫检查
    /// </summary>
    /// <param name="uiKey">UI页面键值</param>
    /// <param name="param">页面进入参数</param>
    /// <returns>如果允许进入则返回true，否则返回false</returns>
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

    /// <summary>
    /// 执行离开守卫检查
    /// </summary>
    /// <param name="uiKey">UI页面键值</param>
    /// <returns>如果允许离开则返回true，否则返回false</returns>
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