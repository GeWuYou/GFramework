using GFramework.Core.Abstractions.Pause;
using GFramework.Core.Extensions;
using GFramework.Game.Abstractions.Enums;
using GFramework.Game.Abstractions.UI;
using GFramework.Game.Routing;

namespace GFramework.Game.UI;

/// <summary>
/// UI路由基类，提供页面栈管理和层级UI管理功能
/// 负责UI页面的导航、显示、隐藏以及生命周期管理
/// </summary>
public abstract class UiRouterBase : RouterBase<IUiPageBehavior, IUiPageEnterParam>, IUiRouter
{
    private static readonly ILogger Log = LoggerFactoryResolver.Provider.CreateLogger(nameof(UiRouterBase));

    /// <summary>
    /// 层级管理字典（非栈层级），用于管理Overlay、Modal、Toast等浮层UI
    /// Key: UiLayer枚举值, Value: InstanceId到PageBehavior的映射字典
    /// </summary>
    private readonly Dictionary<UiLayer, Dictionary<string, IUiPageBehavior>> _layers = new();

    /// <summary>
    ///     记录当前由页面可见性驱动持有的暂停令牌。
    /// </summary>
    private readonly Dictionary<IUiPageBehavior, PauseToken> _pauseTokens = new();

    /// <summary>
    /// UI切换处理器管道，用于执行UI过渡动画和逻辑
    /// </summary>
    private readonly UiTransitionPipeline _pipeline = new();

    /// <summary>
    /// UI工厂实例，用于创建UI页面和相关对象
    /// </summary>
    private IUiFactory _factory = null!;

    /// <summary>
    /// 实例ID计数器，用于生成唯一的UI实例标识符
    /// </summary>
    private int _instanceCounter;

    /// <summary>
    ///     可选暂停栈管理器。
    /// </summary>
    private IPauseStackManager? _pauseStackManager;

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
        Log.Debug("Push UI Page: key={0}, policy={1}, stackBefore={2}", uiKey, policy, Stack.Count);

        await _pipeline.ExecuteAroundAsync(@event, async () =>
        {
            await BeforeChangeAsync(@event).ConfigureAwait(true);
            await DoPushPageInternalAsync(uiKey, param, policy).ConfigureAwait(true);
            await AfterChangeAsync(@event).ConfigureAwait(true);
        }).ConfigureAwait(true);
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
        Log.Debug("Push existing UI Page: key={0}, policy={1}, stackBefore={2}", uiKey, policy, Stack.Count);

        await _pipeline.ExecuteAroundAsync(@event, async () =>
        {
            await BeforeChangeAsync(@event).ConfigureAwait(true);
            DoPushPageInternal(page, param, policy);
            await AfterChangeAsync(@event).ConfigureAwait(true);
        }).ConfigureAwait(true);
    }

    /// <summary>
    /// 弹出栈顶页面
    /// </summary>
    /// <param name="policy">页面弹出策略</param>
    public async ValueTask PopAsync(UiPopPolicy policy = UiPopPolicy.Destroy)
    {
        if (Stack.Count == 0)
        {
            Log.Debug("Pop ignored: stack is empty");
            return;
        }

        var leavingUiKey = Stack.Peek().Key;

        if (!await ExecuteLeaveGuardsAsync(leavingUiKey).ConfigureAwait(true))
        {
            Log.Warn("Pop blocked by guard: {0}", leavingUiKey);
            return;
        }

        var nextUiKey = Stack.Count > 1 ? Stack.ElementAt(1).Key : null;
        var @event = CreateEvent(nextUiKey, UiTransitionType.Pop);

        await _pipeline.ExecuteAroundAsync(@event, async () =>
        {
            await BeforeChangeAsync(@event).ConfigureAwait(true);
            DoPopInternal(policy);
            await AfterChangeAsync(@event).ConfigureAwait(true);
        }).ConfigureAwait(true);
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
            await BeforeChangeAsync(@event).ConfigureAwait(true);
            DoClearInternal(popPolicy);

            var page = _factory.Create(uiKey);
            Log.Debug("Get/Create UI Page instance for Replace: {0}", page.GetType().Name);

            DoPushPageInternal(page, param, pushPolicy);
            await AfterChangeAsync(@event).ConfigureAwait(true);
        }).ConfigureAwait(true);
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
            await BeforeChangeAsync(@event).ConfigureAwait(true);
            DoClearInternal(popPolicy);
            Log.Debug("Use existing UI Page instance for Replace: {0}", page.GetType().Name);
            DoPushPageInternal(page, param, pushPolicy);
            await AfterChangeAsync(@event).ConfigureAwait(true);
        }).ConfigureAwait(true);
    }

    /// <summary>
    /// 清空所有页面栈
    /// </summary>
    public async ValueTask ClearAsync()
    {
        var @event = CreateEvent(string.Empty, UiTransitionType.Clear);
        Log.Debug("Clear UI Stack, stackCount={0}", Stack.Count);

        await _pipeline.ExecuteAroundAsync(@event, async () =>
        {
            await BeforeChangeAsync(@event).ConfigureAwait(true);
            DoClearInternal(UiPopPolicy.Destroy);
            await AfterChangeAsync(@event).ConfigureAwait(true);
        }).ConfigureAwait(true);
    }

    /// <summary>
    /// 获取栈顶元素的键值
    /// </summary>
    /// <returns>栈顶UI页面的键值，如果栈为空则返回空字符串</returns>
    public new string PeekKey()
    {
        return Stack.Count == 0 ? string.Empty : Stack.Peek().Key;
    }

    /// <summary>
    /// 获取栈顶元素
    /// </summary>
    /// <returns>栈顶UI页面行为实例，如果栈为空则返回null</returns>
    public IUiPageBehavior? Peek()
    {
        return Stack.Count == 0 ? null : Stack.Peek();
    }

    /// <summary>
    /// 判断栈顶是否为指定UI
    /// </summary>
    /// <param name="uiKey">要检查的UI页面键值</param>
    /// <returns>如果栈顶是指定UI则返回true，否则返回false</returns>
    public new bool IsTop(string uiKey)
    {
        return Stack.Count != 0 && string.Equals(Stack.Peek().Key, uiKey, StringComparison.Ordinal);
    }

    /// <summary>
    /// 判断栈中是否包含指定UI
    /// </summary>
    /// <param name="uiKey">要检查的UI页面键值</param>
    /// <returns>如果栈中包含指定UI则返回true，否则返回false</returns>
    public new bool Contains(string uiKey)
    {
        return Stack.Any(p => string.Equals(p.Key, uiKey, StringComparison.Ordinal));
    }

    /// <summary>
    /// 获取栈深度
    /// </summary>
    public new int Count => Stack.Count;

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
            throw new ArgumentException("Use Push() for Page layer", nameof(layer));

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
            throw new ArgumentException("Use Push() for Page layer", nameof(layer));

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
            SyncPauseRequest(page, isVisible: false);
            _uiRoot.RemoveUiPage(page);
            layerDict.Remove(handle.InstanceId);
            Log.Debug("Hide & Destroy UI: instanceId={0}, layer={1}", handle.InstanceId, layer);
        }
        else
        {
            page.OnHide();
            SyncPauseRequest(page, isVisible: false);
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
        SyncPauseRequest(page, isVisible: true);
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
            .Where(kvp => string.Equals(kvp.Value.Key, uiKey, StringComparison.Ordinal))
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

    /// <summary>
    ///     获取当前拥有指定 UI 语义动作捕获权的页面。
    /// </summary>
    /// <param name="action">要查询的动作。</param>
    /// <returns>动作所有者；若没有页面声明捕获该动作则返回 <see langword="null" />。</returns>
    public IUiPageBehavior? GetUiActionOwner(UiInputAction action)
    {
        return EnumerateVisiblePagesByPriority()
            .FirstOrDefault(page => UiInteractionProfiles.Captures(page.InteractionProfile, action));
    }

    /// <summary>
    ///     尝试将语义动作分发给当前拥有捕获权的页面。
    /// </summary>
    /// <param name="action">当前动作。</param>
    /// <returns>如果已有页面捕获该动作则返回 <see langword="true" />。</returns>
    public bool TryDispatchUiAction(UiInputAction action)
    {
        var owner = GetUiActionOwner(action);
        if (owner is null)
            return false;

        var handled = owner.TryHandleUiAction(action);
        if (!handled)
            Log.Debug("UI action captured without explicit handler: key={0}, action={1}", owner.Key, action);

        return true;
    }

    /// <summary>
    ///     尝试将语义动作分发给当前拥有捕获权的页面。
    /// </summary>
    /// <param name="action">当前动作。</param>
    /// <returns>如果已有页面捕获该动作则返回 <see langword="true" />。</returns>
    [Obsolete(
        "Use TryDispatchUiAction(UiInputAction action) to emphasize dispatch semantics instead of handler success.")]
    public bool TryHandleUiAction(UiInputAction action)
    {
        return TryDispatchUiAction(action);
    }

    /// <summary>
    ///     判断当前可见 UI 是否阻断 World 指针输入。
    /// </summary>
    /// <returns>如果 World 指针输入应被阻断则返回 <see langword="true" />。</returns>
    public bool BlocksWorldPointerInput()
    {
        return EnumerateVisiblePagesByPriority()
            .Any(page => page.InteractionProfile.BlocksWorldPointerInput);
    }

    /// <summary>
    ///     判断当前可见 UI 是否阻断 World 语义动作输入。
    /// </summary>
    /// <returns>如果 World 语义动作输入应被阻断则返回 <see langword="true" />。</returns>
    public bool BlocksWorldActionInput()
    {
        return EnumerateVisiblePagesByPriority()
            .Any(page => page.InteractionProfile.BlocksWorldActionInput);
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
        TryBindPauseStackManager();

        // 输出调试日志，记录UI路由器基类已初始化及使用的工厂类型
        Log.Debug("UiRouterBase initialized. Factory={0}", _factory.GetType().Name);

        // 调用抽象方法以注册具体的处理程序
        RegisterHandlers();
    }

    /// <summary>
    /// 抽象方法，用于注册具体的处理程序。
    /// 子类必须实现此方法以完成特定的处理逻辑注册。
    /// </summary>
    protected override abstract void RegisterHandlers();

    /// <summary>
    ///     路由销毁时释放所有由页面持有的暂停请求。
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (_pauseStackManager is null)
            return;

        foreach (var token in _pauseTokens.Values.ToArray())
        {
            _pauseStackManager.Pop(token);
        }

        _pauseTokens.Clear();
    }

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
        if (!_layers.TryGetValue(layer, out var layerDict))
        {
            layerDict = new Dictionary<string, IUiPageBehavior>(StringComparer.Ordinal);
            _layers[layer] = layerDict;
        }

        // 设置句柄
        page.Handle = handle;

        // 检查重入性
        if (!page.IsReentrant &&
            layerDict.Values.Any(p => string.Equals(p.Key, page.Key, StringComparison.Ordinal)))
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
        SyncPauseRequest(page, isVisible: true);

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
        await _pipeline.ExecuteAsync(@event, UiTransitionPhases.BeforeChange).ConfigureAwait(true);
        Log.Debug("BeforeChange phases completed: {0}", @event.TransitionType);
    }

    /// <summary>
    /// 执行过渡后阶段
    /// </summary>
    /// <param name="event">UI过渡事件</param>
    private async Task AfterChangeAsync(UiTransitionEvent @event)
    {
        Log.Debug("AfterChange phases started: {0}", @event.TransitionType);
        await _pipeline.ExecuteAsync(@event, UiTransitionPhases.AfterChange).ConfigureAwait(true);
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
        if (!await ExecuteEnterGuardsAsync(uiKey, param).ConfigureAwait(true))
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
        if (Stack.Count > 0)
        {
            var current = Stack.Peek();
            Log.Debug("Pause current page: {0}", current.View.GetType().Name);
            current.OnPause();

            if (policy == UiTransitionPolicy.Exclusive)
            {
                Log.Debug("Suspend current page (Exclusive): {0}", current.View.GetType().Name);
                current.OnHide();
                SyncPauseRequest(current, isVisible: false);
            }
        }

        Log.Debug("Add page to UiRoot: {0}", page.View.GetType().Name);
        _uiRoot.AddUiPage(page);

        Stack.Push(page);

        Log.Debug("Enter & Show page: {0}, stackAfter={1}", page.View.GetType().Name, Stack.Count);
        page.OnEnter(param);
        page.OnShow();
        SyncPauseRequest(page, isVisible: true);
    }

    /// <summary>
    /// 内部弹出页面实现
    /// </summary>
    /// <param name="policy">页面弹出策略</param>
    private void DoPopInternal(UiPopPolicy policy)
    {
        if (Stack.Count == 0)
            return;

        var top = Stack.Pop();
        Log.Debug("Pop UI Page internal: {0}, policy={1}, stackAfterPop={2}",
            top.GetType().Name, policy, Stack.Count);

        if (policy == UiPopPolicy.Destroy)
        {
            top.OnExit();
            _uiRoot.RemoveUiPage(top);
        }
        else
        {
            top.OnHide();
        }

        SyncPauseRequest(top, isVisible: false);

        if (Stack.Count > 0)
        {
            var next = Stack.Peek();
            next.OnShow();
            SyncPauseRequest(next, isVisible: true);
        }
    }

    /// <summary>
    /// 内部清空页面实现
    /// </summary>
    /// <param name="policy">页面弹出策略</param>
    private void DoClearInternal(UiPopPolicy policy)
    {
        Log.Debug("Clear UI Stack internal, count={0}", Stack.Count);
        while (Stack.Count > 0)
            DoPopInternal(policy);
    }

    /// <summary>
    ///     尝试绑定暂停栈管理器。
    /// </summary>
    private void TryBindPauseStackManager()
    {
        try
        {
            _pauseStackManager = this.GetUtility<IPauseStackManager>();
        }
        catch (InvalidOperationException)
        {
            _pauseStackManager = null;
            Log.Debug("PauseStackManager not available. Pause integration is disabled for the UI router.");
        }
    }

    /// <summary>
    ///     根据页面可见性同步暂停请求。
    /// </summary>
    /// <param name="page">页面行为。</param>
    /// <param name="isVisible">页面是否应视为可见。</param>
    private void SyncPauseRequest(IUiPageBehavior page, bool isVisible)
    {
        if (_pauseStackManager is null)
            return;

        var profile = page.InteractionProfile;
        if (!isVisible || profile.PauseMode == UiPauseMode.None)
        {
            ReleasePauseRequest(page);
            return;
        }

        if (_pauseTokens.ContainsKey(page))
            return;

        var reason = string.IsNullOrWhiteSpace(profile.PauseReason)
            ? $"UI:{page.Key}"
            : profile.PauseReason;
        _pauseTokens[page] = _pauseStackManager.Push(reason, profile.PauseGroup);
    }

    /// <summary>
    ///     释放页面此前登记的暂停请求。
    /// </summary>
    /// <param name="page">目标页面。</param>
    private void ReleasePauseRequest(IUiPageBehavior page)
    {
        if (_pauseStackManager is null)
            return;

        if (!_pauseTokens.Remove(page, out var token))
            return;

        _pauseStackManager.Pop(token);
    }

    /// <summary>
    ///     按输入优先级枚举当前所有可见页面。
    /// </summary>
    /// <returns>可见页面序列。</returns>
    private IEnumerable<IUiPageBehavior> EnumerateVisiblePagesByPriority()
    {
        foreach (var page in EnumerateVisibleLayerPages(UiLayer.Topmost))
            yield return page;

        foreach (var page in EnumerateVisibleLayerPages(UiLayer.Modal))
            yield return page;

        foreach (var page in EnumerateVisibleLayerPages(UiLayer.Overlay))
            yield return page;

        foreach (var page in Stack.Where(static page => page.IsAlive && page.IsVisible))
            yield return page;

        foreach (var page in EnumerateVisibleLayerPages(UiLayer.Toast))
            yield return page;
    }

    /// <summary>
    ///     枚举指定层级中的可见页面，层内按最近显示优先。
    /// </summary>
    /// <param name="layer">目标层级。</param>
    /// <returns>该层级中的可见页面。</returns>
    private IEnumerable<IUiPageBehavior> EnumerateVisibleLayerPages(UiLayer layer)
    {
        if (!_layers.TryGetValue(layer, out var layerDict))
            yield break;

        foreach (var page in layerDict
                     // Use the numeric sequence encoded in the instance id so ordering stays correct after width overflow.
                     .OrderByDescending(static pair => ExtractInstanceSequence(pair.Key))
                     .Select(static pair => pair.Value)
                     .Where(static page => page.IsAlive && page.IsVisible))
        {
            yield return page;
        }
    }

    /// <summary>
    ///     从实例标识符中提取自增序号，供层内最近显示优先排序使用。
    /// </summary>
    /// <param name="instanceId">实例标识符，预期格式为 <c>ui_000001</c>。</param>
    /// <returns>提取到的自增序号；若格式异常则返回 <see cref="int.MinValue" />，使异常值排在最后。</returns>
    private static int ExtractInstanceSequence(string instanceId)
    {
        return instanceId.Length > 3 &&
               int.TryParse(instanceId.AsSpan(3), NumberStyles.None, CultureInfo.InvariantCulture, out var sequence)
            ? sequence
            : int.MinValue;
    }

    #endregion
}
