using GFramework.Core.Abstractions.enums;
using GFramework.Core.Abstractions.model;
using GFramework.Core.rule;

namespace GFramework.Core.model;

/// <summary>
///     抽象模型基类，实现IModel接口，提供模型的基本架构支持
/// </summary>
public abstract class AbstractModel : ContextAwareBase, IModel
{
    /// <summary>
    ///     初始化模型，调用抽象方法OnInit执行具体初始化逻辑
    /// </summary>
    void IModel.Init()
    {
        OnInit();
    }

    /// <summary>
    ///     处理架构阶段事件的虚拟方法
    /// </summary>
    /// <param name="phase">当前的架构阶段</param>
    public virtual void OnArchitecturePhase(ArchitecturePhase phase)
    {
    }


    /// <summary>
    ///     抽象初始化方法，由子类实现具体的初始化逻辑
    /// </summary>
    protected abstract void OnInit();
}