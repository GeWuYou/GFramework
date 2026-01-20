using System.Threading.Tasks;

namespace GFramework.Game.Abstractions.ui;

/// <summary>
/// UI过渡动画接口
/// 定义UI进入和退出时的动画效果
/// </summary>
public interface IUiTransition
{
    /// <summary>
    /// 播放进入动画
    /// </summary>
    /// <param name="page">UI页面</param>
    /// <returns>异步任务，动画完成后完成</returns>
    Task PlayEnterAsync(IUiPageBehavior page);

    /// <summary>
    /// 播放退出动画
    /// </summary>
    /// <param name="page">UI页面</param>
    /// <returns>异步任务，动画完成后完成</returns>
    Task PlayExitAsync(IUiPageBehavior page);
}
