using GFramework.Core.Abstractions.logging;
using GFramework.Core.extensions;
using GFramework.Core.logging;
using GFramework.Core.system;
using GFramework.Game.Abstractions.enums;
using GFramework.Game.Abstractions.ui;
using System.Linq;

namespace GFramework.Game.ui;

/// <summary>
/// UI路由类，提供页面栈管理功能
/// </summary>
public abstract class UiRouterBase : AbstractSystem, IUiRouter
{
    private static readonly ILogger Log = LoggerFactoryResolver.Provider.CreateLogger("UiRouterBase");

    /// <summary>
    /// UI切换处理器管道
    /// </summary>
    private readonly UiTransitionPipeline _pipeline = new();

    /// <summary>
    /// 页面栈，用于管理UI页面的显示顺序
    /// </summary>
    private readonly Stack<IUiPageBehavior> _stack = new();

    /// <summary>
    /// 层级管理（非栈层级），用于Overlay、Modal、Toast等浮层
    /// </summary>
    private readonly Dictionary<UiLayer, Dictionary<string, IUiPageBehavior>> _layers = new();

    /// <summary>
    /// UI工厂实例，用于创建UI相关的对象
    /// </summary>
    private IUiFactory _factory = null!;

    private IUiRoot _uiRoot = null!;

    /// <summary>
    /// 注册UI切换处理器
    /// </summary>
    /// <param name="handler">处理器实例</param>
    /// <param name="options">执行选项</param>
    public void RegisterHandler(IUiTransitionHandler handler, UiTransitionHandlerOptions? options = null)
    {
        _pipeline.RegisterHandler(handler, options);
    }

    /// <summary>
    /// 注销UI切换处理器
    /// </summary>
    /// <param name="handler">处理器实例</param>
    public void UnregisterHandler(IUiTransitionHandler handler)
    {
        _pipeline.UnregisterHandler(handler);
    }

    /// <summary>
    /// 绑定UI根节点
    /// </summary>
    public void BindRoot(IUiRoot root)
    {
        _uiRoot = root;
        Log.Debug("Bind UI Root: {0}", root.GetType().Name);
    }
    

    /// <summary>
    /// 将指定的UI界面压入路由栈，显示新的UI界面
    /// </summary>
    /// <param name="uiKey">UI界面的唯一标识符</param>
    /// <param name="param">进入界面的参数，可为空</param>
    /// <param name="policy">界面切换策略，默认为Exclusive（独占）</param>
    /// <param name="instancePolicy">实例管理策略，默认为Reuse（复用）</param>
    public void Push(string uiKey, IUiPageEnterParam? param = null, UiTransitionPolicy policy = UiTransitionPolicy.Exclusive,
        UiInstancePolicy instancePolicy = UiInstancePolicy.Reuse)
    {
        if (IsTop(uiKey))
        {
            Log.Warn("Push ignored: UI already on top: {0}", uiKey);
            return;
        }

        var @event = CreateEvent(uiKey, UiTransitionType.Push, policy, param);

        Log.Debug(
            "Push UI Page: key={0}, policy={1}, instancePolicy={2}, stackBefore={3}",
            uiKey, policy, instancePolicy, _stack.Count
        );

        BeforeChange(@event);

        // 使用工厂的增强方法获取实例
        var page = _factory.GetOrCreate(uiKey, instancePolicy);
        Log.Debug("Get/Create UI Page instance: {0}", page.GetType().Name);

        DoPushPageInternal(page, param, policy);

        AfterChange(@event);
    }

    /// <summary>
    /// 将已存在的UI页面压入栈顶并显示
    /// </summary>
    /// <param name="page">已创建的UI页面行为实例</param>
    /// <param name="param">页面进入参数，可为空</param>
    /// <param name="policy">页面切换策略</param>
    public void Push(
        IUiPageBehavior page,
        IUiPageEnterParam? param = null,
        UiTransitionPolicy policy = UiTransitionPolicy.Exclusive
    )
    {
        var uiKey = page.View.GetType().Name;

        if (IsTop(uiKey))
        {
            Log.Warn("Push ignored: UI already on top: {0}", uiKey);
            return;
        }

        var @event = CreateEvent(uiKey, UiTransitionType.Push, policy, param);

        Log.Debug(
            "Push existing UI Page: key={0}, policy={1}, stackBefore={2}",
            uiKey, policy, _stack.Count
        );

        BeforeChange(@event);
        DoPushPageInternal(page, param, policy);
        AfterChange(@event);
    }

    /// <summary>
    /// 弹出栈顶页面并根据策略处理页面
    /// </summary>
    /// <param name="policy">弹出策略，默认为销毁策略</param>
    public void Pop(UiPopPolicy policy = UiPopPolicy.Destroy)
    {
        if (_stack.Count == 0)
        {
            Log.Debug("Pop ignored: stack is empty");
            return;
        }

        var nextUiKey = _stack.Count > 1
            ? _stack.ElementAt(1).Key // 使用 Key 而不是 View.GetType().Name
            : throw new InvalidOperationException("Stack is empty");
        var @event = CreateEvent(nextUiKey, UiTransitionType.Pop);

        BeforeChange(@event);

        DoPopInternal(policy);

        AfterChange(@event);
    }

    /// <summary>
    /// 替换当前所有页面为新页面（基于uiKey）
    /// </summary>
    /// <param name="uiKey">新UI页面标识符</param>
    /// <param name="param">页面进入参数，可为空</param>
    /// <param name="popPolicy">弹出页面时的销毁策略，默认为销毁</param>
    /// <param name="pushPolicy">推入页面时的过渡策略，默认为独占</param>
    /// <param name="instancePolicy">实例管理策略</param>
    public void Replace(
        string uiKey,
        IUiPageEnterParam? param = null,
        UiPopPolicy popPolicy = UiPopPolicy.Destroy,
        UiTransitionPolicy pushPolicy = UiTransitionPolicy.Exclusive,
        UiInstancePolicy instancePolicy = UiInstancePolicy.Reuse)
    {
        var @event = CreateEvent(uiKey, UiTransitionType.Replace, pushPolicy, param);

        Log.Debug(
            "Replace UI Stack with page: key={0}, popPolicy={1}, pushPolicy={2}, instancePolicy={3}",
            uiKey, popPolicy, pushPolicy, instancePolicy
        );

        BeforeChange(@event);

        // 使用内部方法清空栈，避免触发额外的Pipeline
        DoClearInternal(popPolicy);

        // 使用工厂的增强方法获取实例
        var page = _factory.GetOrCreate(uiKey, instancePolicy);
        Log.Debug("Get/Create UI Page instance for Replace: {0}", page.GetType().Name);
        
        DoPushPageInternal(page, param, pushPolicy);

        AfterChange(@event);
    }
    /// <summary>
    /// 替换当前所有页面为已存在的页面（基于实例）
    /// </summary>
    /// <param name="page">已创建的UI页面行为实例</param>
    /// <param name="param">页面进入参数，可为空</param>
    /// <param name="popPolicy">弹出页面时的销毁策略，默认为销毁</param>
    /// <param name="pushPolicy">推入页面时的过渡策略，默认为独占</param>
    public void Replace(
        IUiPageBehavior page,
        IUiPageEnterParam? param = null,
        UiPopPolicy popPolicy = UiPopPolicy.Destroy,
        UiTransitionPolicy pushPolicy = UiTransitionPolicy.Exclusive)
    {
        var uiKey = page.Key;
        var @event = CreateEvent(uiKey, UiTransitionType.Replace, pushPolicy, param);

        Log.Debug(
            "Replace UI Stack with existing page: key={0}, popPolicy={1}, pushPolicy={2}",
            uiKey, popPolicy, pushPolicy
        );

        BeforeChange(@event);

        // 清空栈
        DoClearInternal(popPolicy);

        Log.Debug("Use existing UI Page instance for Replace: {0}", page.GetType().Name);
        DoPushPageInternal(page, param, pushPolicy);

        AfterChange(@event);
    }

    /// <summary>
    /// 清空所有页面栈中的页面
    /// </summary>
    public void Clear()
    {
        var @event = CreateEvent(string.Empty, UiTransitionType.Clear);

        Log.Debug("Clear UI Stack, stackCount={0}", _stack.Count);

        BeforeChange(@event);

        // 使用内部方法，避免触发额外的Pipeline
        DoClearInternal(UiPopPolicy.Destroy);

        AfterChange(@event);
    }


    /// <summary>
    /// 获取页面栈顶元素的键值，但不移除该元素
    /// </summary>
    /// <returns>如果页面栈为空则返回空字符串，否则返回栈顶元素的键值</returns>
    public string PeekKey()
    {
        return _stack.Count == 0 ? string.Empty : _stack.Peek().Key;
    }

    /// <summary>
    /// 获取页面栈顶元素，但不移除该元素
    /// </summary>
    /// <returns>返回栈顶的IUiPageBehavior元素</returns>
    public IUiPageBehavior Peek()
    {
        return _stack.Peek();
    }


    /// <summary>
    /// 判断栈顶元素是否指定的UI类型
    /// </summary>
    /// <param name="uiKey">要比较的UI类型名称</param>
    /// <returns>如果栈为空或栈顶元素类型不匹配则返回false，否则返回true</returns>
    public bool IsTop(string uiKey)
    {
        return _stack.Count != 0 && _stack.Peek().Key.Equals(uiKey);
    }

    /// <summary>
    /// 判断栈中是否包含指定类型的UI元素
    /// </summary>
    /// <param name="uiKey">要查找的UI类型名称</param>
    /// <returns>如果栈中存在指定类型的UI元素则返回true，否则返回false</returns>
    public bool Contains(string uiKey)
    {
        return _stack.Any(p => p.Key.Equals(uiKey));
    }

    /// <summary>
    /// 获取栈中元素的数量
    /// </summary>
    public int Count => _stack.Count;

    /// <summary>
    /// 初始化方法，在页面初始化时获取UI工厂实例
    /// </summary>
    protected override void OnInit()
    {
        _factory = this.GetUtility<IUiFactory>()!;
        Log.Debug("UiRouterBase initialized. Factory={0}", _factory.GetType().Name);
        RegisterHandlers();
    }

    /// <summary>
    /// 注册默认的UI切换处理器
    /// </summary>
    protected abstract void RegisterHandlers();

    /// <summary>
    /// 创建UI切换事件
    /// </summary>
    private UiTransitionEvent CreateEvent(
        string toUiKey,
        UiTransitionType type,
        UiTransitionPolicy? policy = null,
        IUiPageEnterParam? param = null
    )
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
    /// 执行UI切换前的Handler（阻塞）
    /// </summary>
    private void BeforeChange(UiTransitionEvent @event)
    {
        Log.Debug("BeforeChange phases started: {0}", @event.TransitionType);
        _pipeline.ExecuteAsync(@event, UITransitionPhases.BeforeChange).GetAwaiter().GetResult();
        Log.Debug("BeforeChange phases completed: {0}", @event.TransitionType);
    }

    /// <summary>
    /// 执行UI切换后的Handler（不阻塞）
    /// </summary>
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

    /// <summary>
    /// 执行Push页面的核心逻辑（统一处理）
    /// 这个方法同时服务于工厂创建和已存在页面两种情况
    /// </summary>
    private void DoPushPageInternal(IUiPageBehavior page, IUiPageEnterParam? param, UiTransitionPolicy policy)
    {
        // 1. 处理当前栈顶页面
        if (_stack.Count > 0)
        {
            var current = _stack.Peek();
            Log.Debug("Pause current page: {0}", current.View.GetType().Name);
            current.OnPause();

            if (policy == UiTransitionPolicy.Exclusive)
            {
                Log.Debug("Hide current page (Exclusive): {0}", current.View.GetType().Name);
                current.OnHide();
            }
        }

        // 2. 将新页面添加到UiRoot
        Log.Debug("Add page to UiRoot: {0}", page.View.GetType().Name);
        _uiRoot.AddUiPage(page);

        // 3. 压入栈
        _stack.Push(page);

        // 4. 触发页面生命周期
        Log.Debug(
            "Enter & Show page: {0}, stackAfter={1}",
            page.View.GetType().Name, _stack.Count
        );

        page.OnEnter(param);
        page.OnShow();
    }

    /// <summary>
    /// 执行Pop的核心逻辑（不触发Pipeline）
    /// </summary>
    private void DoPopInternal(UiPopPolicy policy)
    {
        if (_stack.Count == 0)
            return;

        var top = _stack.Pop();
        Log.Debug(
            "Pop UI Page internal: {0}, policy={1}, stackAfterPop={2}",
            top.GetType().Name, policy, _stack.Count
        );

        top.OnExit();

        if (policy == UiPopPolicy.Destroy)
        {
            Log.Debug("Destroy UI Page: {0}", top.GetType().Name);
            _uiRoot.RemoveUiPage(top);
            // 不回收，直接销毁
        }
        else // UiPopPolicy.Cache
        {
            Log.Debug("Cache UI Page: {0}", top.GetType().Name);
            _uiRoot.RemoveUiPage(top);
            _factory.Recycle(top); // 回收到池中
        }

        if (_stack.Count > 0)
        {
            var next = _stack.Peek();
            Log.Debug("Resume & Show page: {0}", next.GetType().Name);
            next.OnResume();
            next.OnShow();
        }
        else
        {
            Log.Debug("UI stack is now empty");
        }
    }

    /// <summary>
    /// 执行Clear的核心逻辑（不触发Pipeline）
    /// </summary>
    /// <param name="policy">UI弹出策略</param>
    private void DoClearInternal(UiPopPolicy policy)
    {
        Log.Debug("Clear UI Stack internal, count={0}", _stack.Count);
        // 循环执行弹出操作直到栈为空
        while (_stack.Count > 0)
            DoPopInternal(policy);
    }

    #region 层级管理

    /// <summary>
    /// 在指定层级显示UI（非栈管理）
    /// </summary>
    public void Show(
        string uiKey,
        Game.Abstractions.enums.UiLayer layer,
        IUiPageEnterParam? param = null,
        UiInstancePolicy instancePolicy = UiInstancePolicy.Reuse)
    {
        if (layer == Game.Abstractions.enums.UiLayer.Page)
        {
            throw new ArgumentException("Use Push() for Page layer");
        }

        // 初始化层级字典
        if (!_layers.ContainsKey(layer))
            _layers[layer] = new Dictionary<string, IUiPageBehavior>();

        var layerDict = _layers[layer];

        // 检查是否已存在
        if (layerDict.TryGetValue(uiKey, out var existing))
        {
            Log.Debug("UI already visible in layer: {0}, layer={1}", uiKey, layer);
            existing.OnEnter(param);
            existing.OnShow();
            return;
        }

        // 获取或创建实例
        var page = _factory.GetOrCreate(uiKey, instancePolicy);
        layerDict[uiKey] = page;

        // 添加到UiRoot，传入层级Z-order
        _uiRoot.AddUiPage(page, (int)layer);

        page.OnEnter(param);
        page.OnShow();

        Log.Debug("Show UI in layer: {0}, layer={1}", uiKey, layer);
    }

    /// <summary>
    /// 在指定层级显示UI（基于实例）
    /// </summary>
    public void Show(IUiPageBehavior page, UiLayer layer)
    {
        if (layer == UiLayer.Page)
            throw new ArgumentException("Use Push() for Page layer");

        var uiKey = page.Key;

        if (!_layers.ContainsKey(layer))
            _layers[layer] = new Dictionary<string, IUiPageBehavior>();

        _layers[layer][uiKey] = page;
        _uiRoot.AddUiPage(page, (int)layer);
        page.OnShow();

        Log.Debug("Show existing UI instance in layer: {0}, layer={1}", uiKey, layer);
    }

    /// <summary>
    /// 隐藏指定层级的UI
    /// </summary>
    public void Hide(string uiKey, Game.Abstractions.enums.UiLayer layer, bool destroy = false)
    {
        if (!_layers.TryGetValue(layer, out var layerDict))
            return;

        if (!layerDict.TryGetValue(uiKey, out var page))
            return;

        page.OnExit();
        page.OnHide();

        if (destroy)
        {
            _uiRoot.RemoveUiPage(page);
            layerDict.Remove(uiKey);
            Log.Debug("Hide & Destroy UI from layer: {0}, layer={1}", uiKey, layer);
        }
        else
        {
            _uiRoot.RemoveUiPage(page);
            _factory.Recycle(page);
            layerDict.Remove(uiKey);
            Log.Debug("Hide & Cache UI from layer: {0}, layer={1}", uiKey, layer);
        }
    }

    /// <summary>
    /// 清空指定层级的所有UI
    /// </summary>
    public void ClearLayer(Game.Abstractions.enums.UiLayer layer, bool destroy = false)
    {
        if (!_layers.TryGetValue(layer, out var layerDict))
            return;

        var keys = layerDict.Keys.ToArray();
        foreach (var key in keys)
        {
            Hide(key, layer, destroy);
        }

        Log.Debug("Cleared layer: {0}, destroyed={1}", layer, destroy);
    }

    /// <summary>
    /// 获取指定层级的UI实例
    /// </summary>
    public IUiPageBehavior? GetFromLayer(string uiKey, UiLayer layer)
    {
        return _layers.TryGetValue(layer, out var layerDict) &&
               layerDict.TryGetValue(uiKey, out var page)
            ? page
            : null;
    }

    /// <summary>
    /// 判断指定层级是否有UI显示
    /// </summary>
    public bool HasVisibleInLayer(UiLayer layer)
    {
        return _layers.TryGetValue(layer, out var layerDict) && layerDict.Count > 0;
    }

    #endregion
}