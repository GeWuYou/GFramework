using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.enums;
using GFramework.Core.model;

namespace GFramework.Core.Tests.model;

/// <summary>
///     异步测试模型类，实现了IModel和IAsyncInitializable接口
/// </summary>
public sealed class AsyncTestModel : AbstractModel, IAsyncInitializable
{
    /// <summary>
    ///     获取模型是否已初始化的标志
    /// </summary>
    public bool Initialized { get; private set; }

    /// <summary>
    ///     异步初始化方法，模拟异步初始化过程
    /// </summary>
    /// <returns>表示异步操作的Task</returns>
    public async Task InitializeAsync()
    {
        await Task.Delay(10);
        Initialized = true;
    }

    /// <summary>
    ///     同步初始化方法，该方法不应该被调用
    /// </summary>
    /// <exception cref="InvalidOperationException">当该方法被调用时抛出异常</exception>
    public void Init()
    {
        // sync Init 不应该被调用
        throw new InvalidOperationException("Sync Init should not be called");
    }

    /// <summary>
    ///     处理架构阶段事件
    /// </summary>
    /// <param name="phase">架构阶段枚举值</param>
    public void OnArchitecturePhase(ArchitecturePhase phase)
    {
    }

    protected override void OnInit()
    {
    }
}