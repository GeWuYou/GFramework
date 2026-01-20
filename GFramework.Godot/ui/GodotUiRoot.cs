using System.Collections.Generic;
using System;
using GFramework.Game.Abstractions.ui;
using Godot;

namespace GFramework.Godot.ui;

/// <summary>
/// Godot平台的UI根节点实现
/// 用于管理UI页面的添加、移除和层级排序
/// </summary>
public partial class GodotUiRoot : Node, IUiRoot
{
    /// <summary>
    /// UI节点的父容器，所有UI页面都添加到这个节点下
    /// </summary>
    private Node? _uiContainer;

    /// <summary>
    /// UI页面的追踪字典，记录每个页面的节点
    /// </summary>
    private readonly Dictionary<IUiPageBehavior, Node> _pageNodes = new();

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

        if (_pageNodes.ContainsKey(child))
            return;

        _pageNodes[child] = node;

        if (node.GetParent() != _uiContainer)
        {
            _uiContainer.AddChild(node);
        }
    }

    /// <summary>
    /// 向UI根节点添加子页面到指定层级
    /// </summary>
    public void AddUiPage(IUiPageBehavior child, int zOrder = 0)
    {
        AddUiPage(child);
        SetZOrder(child, zOrder);
    }

    /// <summary>
    /// 从UI根节点移除子页面
    /// </summary>
    public void RemoveUiPage(IUiPageBehavior child)
    {
        if (!_pageNodes.TryGetValue(child, out var node))
            return;

        _pageNodes.Remove(child);

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
    /// 强制刷新UI层级排序
    /// </summary>
    public void RefreshLayerOrder()
    {
        if (_uiContainer == null)
            return;

        _uiContainer.MoveChild(_uiContainer.GetChild(0), 0);
    }

    /// <summary>
    /// 从页面行为获取对应的节点
    /// </summary>
    private static Node? GetNodeFromPage(IUiPageBehavior page)
    {
        return page.View as Node;
    }
}
