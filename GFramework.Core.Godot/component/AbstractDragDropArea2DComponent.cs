using GFramework.Core.Godot.extensions;
using Godot;

namespace GFramework.Core.Godot.component;

/// <summary>
///     抽象拖拽组件类，用于处理节点的拖放逻辑。
///     实现了 IController 接口以支持架构通信，并通过信号通知拖拽事件的发生。
/// </summary>
public abstract partial class AbstractDragDropArea2DComponent : AbstractDragDrop2DComponentBase
{
    /// <summary>
    ///     目标区域，通常是可交互的游戏对象（如单位或物品）所在的碰撞区域。
    /// </summary>
    public required Area2D Target { get; set; }

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
        switch (IsDragging)
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
        switch (IsDragging)
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
        if (IsDragging && Target.IsValidNode()) Target.GlobalPosition = Target.GetGlobalMousePosition() + Offset;
    }

    /// <summary>
    ///     结束拖拽流程的基础方法。
    ///     清除拖拽标志位并将目标从拖拽组中移除，恢复其层级顺序。
    /// </summary>
    private void EndDragging()
    {
        IsDragging = false;
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
        EmitSignalDragCanceled(StartingPosition);
    }

    /// <summary>
    ///     开始拖拽操作。
    ///     设置初始位置和偏移量，将目标加入拖拽组并提升显示层级，最后发出 DragStarted 信号。
    /// </summary>
    private void StartDragging()
    {
        IsDragging = true;
        StartingPosition = Target.GlobalPosition;
        Target.AddToGroup(GroupName);
        Target.ZIndex = ZIndexMax;
        Offset = Target.GlobalPosition - Target.GetGlobalMousePosition();
        EmitSignalDragStarted();
    }

    /// <summary>
    ///     完成一次拖拽操作。
    ///     调用 EndDragging 方法并发出 Dropped 信号。
    /// </summary>
    private void Drop()
    {
        EndDragging();
        EmitSignalDropped(StartingPosition);
    }
}