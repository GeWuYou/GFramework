using System;
using System.Threading.Tasks;
using GFramework.Game.Abstractions.enums;
using GFramework.Game.Abstractions.ui;
using Godot;

namespace GFramework.Godot.ui;

/// <summary>
/// Godot平台的UI过渡动画实现
/// 支持多种预定义动画效果
/// </summary>
public class GodotUiTransition : IUiTransition
{
    private readonly UiTransitionAnimation _animation;

    /// <summary>
    /// 创建过渡动画实例
    /// </summary>
    /// <param name="animation">动画类型</param>
    public GodotUiTransition(UiTransitionAnimation animation)
    {
        _animation = animation;
    }

    /// <summary>
    /// 播放进入动画
    /// </summary>
    public async Task PlayEnterAsync(IUiPageBehavior page)
    {
        var node = page.View as Node;
        if (node == null)
            return;

        switch (_animation)
        {
            case UiTransitionAnimation.Fade:
                await PlayFadeEnterAsync(node);
                break;
            case UiTransitionAnimation.Scale:
                await PlayScaleEnterAsync(node);
                break;
            case UiTransitionAnimation.SlideLeft:
            case UiTransitionAnimation.SlideRight:
            case UiTransitionAnimation.SlideUp:
            case UiTransitionAnimation.SlideDown:
                await PlaySlideEnterAsync(node, _animation);
                break;
            case UiTransitionAnimation.None:
            default:
                break;
        }
    }

    /// <summary>
    /// 播放退出动画
    /// </summary>
    public async Task PlayExitAsync(IUiPageBehavior page)
    {
        var node = page.View as Node;
        if (node == null)
            return;

        switch (_animation)
        {
            case UiTransitionAnimation.Fade:
                await PlayFadeExitAsync(node);
                break;
            case UiTransitionAnimation.Scale:
                await PlayScaleExitAsync(node);
                break;
            case UiTransitionAnimation.SlideLeft:
            case UiTransitionAnimation.SlideRight:
            case UiTransitionAnimation.SlideUp:
            case UiTransitionAnimation.SlideDown:
                await PlaySlideExitAsync(node, _animation);
                break;
            case UiTransitionAnimation.None:
            default:
                break;
        }
    }

    private static async Task PlayFadeEnterAsync(Node node)
    {
        if (node is CanvasItem canvasItem)
        {
            canvasItem.Modulate = new Color(1,1, 1, 0);
            canvasItem.Visible = true;
            await Task.Delay(300);
            canvasItem.Modulate = new Color(1,1, 1, 1);
        }
    }

    private static async Task PlayFadeExitAsync(Node node)
    {
        if (node is CanvasItem canvasItem)
        {
            await Task.Delay(300);
            canvasItem.Modulate = new Color(1,1, 1, 0);
        }
    }

    private static async Task PlayScaleEnterAsync(Node node)
    {
        if (node is Control control)
        {
            control.Scale = Vector2.Zero;
            control.PivotOffset = control.Size / 2;
            await Task.Delay(300);
            control.Scale = Vector2.One;
        }
    }

    private static async Task PlayScaleExitAsync(Node node)
    {
        if (node is Control control)
        {
            control.PivotOffset = control.Size / 2;
            await Task.Delay(300);
            control.Scale = Vector2.Zero;
        }
    }

    private static async Task PlaySlideEnterAsync(Node node, UiTransitionAnimation direction)
    {
        if (node is Control control)
        {
            var screenPos = control.GetViewportRect().Size;
            var offset = GetSlideOffset(direction, screenPos);
            control.Position += offset;
            await Task.Delay(300);
            control.Position -= offset;
        }
    }

    private static async Task PlaySlideExitAsync(Node node, UiTransitionAnimation direction)
    {
        if (node is Control control)
        {
            var screenPos = control.GetViewportRect().Size;
            var offset = GetSlideExitOffset(direction, screenPos);
            await Task.Delay(300);
            control.Position += offset;
        }
    }

    private static Vector2 GetSlideOffset(UiTransitionAnimation direction, Vector2 screenPos)
    {
        return direction switch
        {
            UiTransitionAnimation.SlideLeft => Vector2.Right * screenPos.X,
            UiTransitionAnimation.SlideRight => Vector2.Left * screenPos.X,
            UiTransitionAnimation.SlideUp => Vector2.Down * screenPos.Y,
            UiTransitionAnimation.SlideDown => Vector2.Up * screenPos.Y,
            _ => Vector2.Zero
        };
    }

    private static Vector2 GetSlideExitOffset(UiTransitionAnimation direction, Vector2 screenPos)
    {
        return direction switch
        {
            UiTransitionAnimation.SlideLeft => Vector2.Left * screenPos.X,
            UiTransitionAnimation.SlideRight => Vector2.Right * screenPos.X,
            UiTransitionAnimation.SlideUp => Vector2.Up * screenPos.Y,
            UiTransitionAnimation.SlideDown => Vector2.Down * screenPos.Y,
            _ => Vector2.Zero
        };
    }
}
