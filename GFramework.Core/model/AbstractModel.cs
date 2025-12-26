using GFramework.Core.architecture;

namespace GFramework.Core.model;

/// <summary>
///     抽象模型基类，实现IModel接口，提供模型的基本架构支持
/// </summary>
public abstract class AbstractModel : IModel
{
    /// <summary>
    ///     模型所属的架构实例
    /// </summary>
    protected IArchitecture Architecture;

    /// <summary>
    ///     初始化模型，调用抽象方法OnInit执行具体初始化逻辑
    /// </summary>
    void IModel.Init()
    {
        OnInit();
    }

    /// <summary>
    ///     获取模型所属的架构实例
    /// </summary>
    /// <returns>返回当前模型关联的架构对象</returns>
    public IArchitecture GetArchitecture()
    {
        return Architecture;
    }

    /// <summary>
    ///     设置模型所属的架构实例
    /// </summary>
    /// <param name="architecture">要关联到此模型的架构实例</param>
    public void SetArchitecture(IArchitecture architecture)
    {
        Architecture = architecture;
    }

    /// <summary>
    ///     抽象初始化方法，由子类实现具体的初始化逻辑
    /// </summary>
    protected abstract void OnInit();
}