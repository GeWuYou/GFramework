using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.enums;
using GFramework.Core.Abstractions.model;

namespace GFramework.Core.Tests.model;

/// <summary>
/// 一个用于测试的失败模型类，实现IModel接口
/// 该模型在初始化时会故意抛出异常，用于测试异常处理机制
/// </summary>
public sealed class FailingModel : IModel
{
    /// <summary>
    /// 初始化模型
    /// 该方法会故意抛出InvalidOperationException异常
    /// </summary>
    /// <exception cref="InvalidOperationException">总是抛出此异常以模拟初始化失败</exception>
    public void Init()
    {
        throw new InvalidOperationException("Model init failed intentionally");
    }

    /// <summary>
    /// 设置架构上下文
    /// 该方法为空实现，不执行任何操作
    /// </summary>
    /// <param name="context">架构上下文对象</param>
    public void SetContext(IArchitectureContext context)
    {
    }

    /// <summary>
    /// 获取架构上下文
    /// 该方法会抛出NotSupportedException异常
    /// </summary>
    /// <returns>不返回任何值，总是抛出异常</returns>
    /// <exception cref="NotSupportedException">总是抛出此异常</exception>
    public IArchitectureContext GetContext()
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// 处理架构阶段事件
    /// 该方法为空实现，不执行任何操作
    /// </summary>
    /// <param name="phase">当前架构阶段</param>
    public void OnArchitecturePhase(ArchitecturePhase phase)
    {
    }
}