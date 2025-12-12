using GFramework.Core.architecture;
using GFramework.Core.controller;
using GFramework.Core.events;
using GFramework.Core.extensions;
using Godot;

namespace GFramework.Core.Godot.component;

/// <summary>
///     抽象基类，用于实现2D拖拽功能的组件。
///     继承自Godot的Node类并实现了IController接口。
///     提供了拖拽相关的信号定义以及基础属性配置。
/// </summary>
public abstract partial class AbstractDragDrop2DComponentBase: Node, IController
{
    /// <summary>
    ///     取消注册列表，用于管理需要在节点销毁时取消注册的对象
    /// </summary>
    protected readonly IUnRegisterList UnRegisterList = new UnRegisterList();
    
    /// <summary>
    ///     当拖拽被取消时触发的信号。
    /// </summary>
    /// <param name="startingPosition">拖拽起始位置。</param>
    [Signal]
    public delegate void DragCanceledEventHandler(Vector2 startingPosition);

    /// <summary>
    ///     当拖拽开始时触发的信号。
    /// </summary>
    [Signal]
    public delegate void DragStartedEventHandler();

    /// <summary>
    ///     当拖拽结束并放置时触发的信号。
    /// </summary>
    /// <param name="startingPosition">拖拽起始位置。</param>
    [Signal]
    public delegate void DroppedEventHandler(Vector2 startingPosition);
    
    /// <summary>
    ///     是否启用拖拽功能。若为 false，则忽略所有输入事件。
    /// </summary>
    public bool Enable { get; set; }

    /// <summary>
    ///     拖拽组的名称，用于区分不同的拖拽组。
    /// </summary>
    public string GroupName { get; set; } = "dragging";

    /// <summary>
    ///     拖拽时元素的最大Z轴索引值。
    /// </summary>
    public int ZIndexMax { get; set; } = 99;

    /// <summary>
    ///     拖拽时元素的最小Z轴索引值。
    /// </summary>
    public int ZIndexMin { get; set; } = 0;

    /// <summary>
    ///     获取架构实例。
    /// </summary>
    /// <returns>返回实现IArchitecture接口的架构实例。</returns>
    public abstract IArchitecture GetArchitecture();
    
    /// <summary>
    ///     表示是否正在拖拽操作的标志位。
    /// </summary>
    protected bool IsDragging;
    
    /// <summary>
    ///     表示拖拽操作中的偏移量，用于计算当前位置与起始位置的差值。
    /// </summary>
    protected Vector2 Offset = Vector2.Zero;

    /// <summary>
    ///     表示拖拽操作的起始位置坐标。
    /// </summary>
    protected Vector2 StartingPosition;
    
    /// <summary>
    ///     节点退出场景树时的回调方法。
    ///     在节点从场景树移除前调用，用于清理资源。
    /// </summary>
    public override void _ExitTree()
    {
        UnRegisterList.UnRegisterAll();
    }
}
