using GFramework.Core.architecture;
using GFramework.Core.controller;
using GFramework.Core.events;
using GFramework.Core.extensions;
using GFramework.Core.Godot.extensions;
using Godot;

namespace GFramework.Core.Godot.component;

/// <summary>
///     抽象拖拽组件类，用于处理节点的拖放逻辑。
///     实现了 IController 接口以支持架构通信，并通过信号通知拖拽事件的发生。
/// </summary>
public abstract partial class AbstractDragDropComponent : Node, IController
{
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
    ///     取消注册列表，用于管理需要在节点销毁时取消注册的对象
    /// </summary>
    private readonly IUnRegisterList _unRegisterList = new UnRegisterList();

    private bool _isDragging;
    private Vector2 _offset = Vector2.Zero;

    private Vector2 _startingPosition;

    /// <summary>
    ///     目标区域，通常是可交互的游戏对象（如单位或物品）所在的碰撞区域。
    /// </summary>
    public required Area2D Target { get; set; }

    /// <summary>
    ///     是否启用拖拽功能。若为 false，则忽略所有输入事件。
    /// </summary>
    public bool Enable { get; set; }

    public string GroupName { get; set; } = "dragging";

    public int ZIndexMax { get; set; } = 99;
    public int ZIndexMin { get; set; } = 0;

    /// <summary>
    ///     获取游戏架构实例
    /// </summary>
    /// <returns>返回游戏架构接口实例</returns>
    public abstract IArchitecture GetArchitecture();

    /// <summary>
    ///     节点准备就绪时的回调方法。
    ///     在节点添加到场景树后调用，绑定目标区域的输入事件处理器。
    /// </summary>
    public override void _Ready()
    {
        Target = GetParent() as Area2D ?? throw new InvalidOperationException("Target must be an Area2D node.");
        Target.InputEvent += OnTargetInputEvent;
    }

    /// <summary>
    ///     处理输入事件的回调方法。
    ///     根据当前拖拽状态和输入事件类型，执行相应的拖拽操作。
    /// </summary>
    /// <param name="event">输入事件对象</param>
    public override void _Input(InputEvent @event)
    {
        switch (_isDragging)
        {
            // 处理取消拖拽操作：当正在拖拽且按下取消拖拽按键时，执行取消拖拽逻辑
            case true when Target.IsValidNode() && @event.IsActionPressed("cancel_drag"):
                CancelDragging();
                // 设置输入为处理，防止输入穿透
                this.SetInputAsHandled();
                break;
            case true when @event.IsActionReleased("select"):
                Drop();
                break;
        }
    }

    /// <summary>
    ///     目标区域的输入事件处理器。
    ///     当目标区域接收到输入事件时被调用，用于控制拖拽的开始和结束。
    /// </summary>
    /// <param name="viewport">事件发生的视口节点</param>
    /// <param name="event">输入事件对象</param>
    /// <param name="_">事件触点ID（未使用）</param>
    private void OnTargetInputEvent(Node viewport, InputEvent @event, long _)
    {
        if (!Enable) return;

        // 获取当前正在拖拽的对象
        var draggingObj = GetTree().GetFirstNodeInGroup(GroupName);
        switch (_isDragging)
        {
            // 处理开始拖拽操作：当未在拖拽状态且按下选择按键时，开始拖拽
            case false when
                // 如果当前没有拖拽操作且已有其他对象正在拖拽，则直接返回
                draggingObj is not null:
                return;
            case false when @event.IsActionPressed("select"):
                StartDragging();
                break;
        }
    }

    /// <summary>
    ///     每帧更新逻辑，在拖拽过程中持续更新目标的位置。
    /// </summary>
    /// <param name="delta">与上一帧的时间间隔（秒）。</param>
    public override void _Process(double delta)
    {
        if (_isDragging && Target.IsValidNode()) Target.GlobalPosition = Target.GetGlobalMousePosition() + _offset;
    }

    /// <summary>
    ///     结束拖拽流程的基础方法。
    ///     清除拖拽标志位并将目标从拖拽组中移除，恢复其层级顺序。
    /// </summary>
    private void EndDragging()
    {
        _isDragging = false;
        Target.RemoveFromGroup(GroupName);
        Target.ZIndex = ZIndexMin;
    }

    /// <summary>
    ///     执行取消拖拽的操作。
    ///     调用 EndDragging 并发出 DragCanceled 信号。
    /// </summary>
    private void CancelDragging()
    {
        EndDragging();
        EmitSignalDragCanceled(_startingPosition);
    }

    /// <summary>
    ///     开始拖拽操作。
    ///     设置初始位置和偏移量，将目标加入拖拽组并提升显示层级，最后发出 DragStarted 信号。
    /// </summary>
    private void StartDragging()
    {
        _isDragging = true;
        _startingPosition = Target.GlobalPosition;
        Target.AddToGroup(GroupName);
        Target.ZIndex = ZIndexMax;
        _offset = Target.GlobalPosition - Target.GetGlobalMousePosition();
        EmitSignalDragStarted();
    }

    /// <summary>
    ///     完成一次拖拽操作。
    ///     调用 EndDragging 方法并发出 Dropped 信号。
    /// </summary>
    private void Drop()
    {
        EndDragging();
        EmitSignalDropped(_startingPosition);
    }

    /// <summary>
    ///     节点退出场景树时的回调方法。
    ///     在节点从场景树移除前调用，用于清理资源。
    /// </summary>
    public override void _ExitTree()
    {
        _unRegisterList.UnRegisterAll();
    }
}