using GFramework.Game.Abstractions.enums;
using GFramework.Game.Abstractions.ui;
using Godot;

namespace GFramework.Godot.ui;

/// <summary>
/// Godot平台的UI根节点实现
/// 用于管理UI页面的添加、移除和层级排序
/// </summary>
public partial class GodotUiRoot(IReadOnlyDictionary<UiLayer, int>? layerZOrderMap) : Node, IUiRoot
{
    /// <summary>
    /// UI节点的父容器，所有UI页面都添加到这个节点下
    /// </summary>
    private Node? _uiContainer;

    /// <summary>
    /// UI页面的追踪字典，记录每个页面的节点
    /// </summary>
    private readonly Dictionary<IUiPageBehavior, Node> _pageNodes = new();
    
    /// <summary>
    /// UI层级与Z轴顺序的映射表，定义了不同UI层的渲染优先级
    /// </summary>
    /// <remarks>
    /// 默认层级映射关系：
    /// - Page: 0 (基础页面层)
    /// - Overlay: 100 (覆盖层)
    /// - Modal: 200 (模态窗口层)
    /// - Toast: 300 (提示消息层)
    /// - Topmost: 400 (最顶层)
    /// </remarks>
    private readonly IReadOnlyDictionary<UiLayer, int> _layerZOrderMap =layerZOrderMap ??
        new Dictionary<UiLayer, int>
        {
            { UiLayer.Page,     0 },
            { UiLayer.Overlay,  100 },
            { UiLayer.Modal,    200 },
            { UiLayer.Toast,    300 },
            { UiLayer.Topmost,  400 },
        };


    public override void _Ready()
    {
        // 创建UI容器节点
        _uiContainer = new Node
        {
            Name = "UiContainer"
        };

        AddChild(_uiContainer);
    }

    /// <summary>
    /// 向UI根节点添加子页面
    /// </summary>
    public void AddUiPage(IUiPageBehavior child)
    {
        if (_uiContainer == null)
            throw new InvalidOperationException("UiContainer is not initialized");

        var node = GetNodeFromPage(child);
        if (node == null)
            throw new InvalidOperationException($"Page node is null: {child.Key}");

        if (!_pageNodes.TryAdd(child, node))
            return;

        if (node.GetParent() != _uiContainer)
        {
            _uiContainer.AddChild(node);
        }
    }

    /// <summary>
    /// 向UI根节点添加子页面到指定层级
    /// </summary>
    public void AddUiPage(
        IUiPageBehavior child,
        UiLayer layer,
        int orderInLayer = 0)
    {
        AddUiPage(child);

        var z = GetBaseZOrder(layer) + orderInLayer;
        SetZOrder(child, z);
    }


    /// <summary>
    /// 从UI根节点移除子页面
    /// </summary>
    public void RemoveUiPage(IUiPageBehavior child)
    {
        if (!_pageNodes.Remove(child, out var node))
            return;

        if (_uiContainer != null && node.GetParent() == _uiContainer)
        {
            _uiContainer.RemoveChild(node);
        }
    }

    /// <summary>
    /// 设置页面的Z-order（层级顺序）
    /// </summary>
    public void SetZOrder(IUiPageBehavior page, int zOrder)
    {
        if (!_pageNodes.TryGetValue(page, out var node))
            return;

        if (node is CanvasItem canvasItem)
        {
            canvasItem.ZIndex = zOrder;
        }
    }

    /// <summary>
    /// 获取当前所有显示的页面
    /// </summary>
    public IReadOnlyList<IUiPageBehavior> GetVisiblePages()
    {
        return _pageNodes.Keys.ToList().AsReadOnly();
    }
    


    /// <summary>
    /// 从页面行为获取对应的节点
    /// </summary>
    private static Node? GetNodeFromPage(IUiPageBehavior page)
    {
        return page.View as Node;
    }
    private int GetBaseZOrder(UiLayer layer)
    {
        return !_layerZOrderMap.TryGetValue(layer, out var z) ? throw new ArgumentOutOfRangeException(nameof(layer), layer, null) : z;
    }
}
