using System.Threading;
using System.Threading.Tasks;
using GFramework.Game.Abstractions.enums;

namespace GFramework.Game.Abstractions.ui;

/// <summary>
/// UI切换处理器接口，定义UI切换扩展点的处理逻辑
/// </summary>
public interface IUiTransitionHandler
{
    /// <summary>
    /// 处理器优先级，数值越小越先执行
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// 处理器适用的阶段，默认为所有阶段
    /// 可以使用Flags枚举指定多个阶段
    /// </summary>
    UITransitionPhases Phases { get; }

    /// <summary>
    /// 判断是否应该处理当前事件
    /// 可以根据事件类型、UI key等信息进行条件过滤
    /// </summary>
    /// <param name="event">UI切换事件</param>
    /// <param name="phases">当前阶段</param>
    /// <returns>是否处理</returns>
    bool ShouldHandle(UiTransitionEvent @event, UITransitionPhases phases);

    /// <summary>
    /// 处理UI切换事件
    /// </summary>
    /// <param name="event">UI切换事件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task HandleAsync(UiTransitionEvent @event, CancellationToken cancellationToken);
}