using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Architectures;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     测试架构基类，提供通用的测试架构功能
/// </summary>
public abstract class TestArchitectureBase : Architecture
{
    private Action<TestArchitectureBase>? _postRegistrationHook;

    /// <summary>
    ///     获取就绪事件是否已触发的状态
    /// </summary>
    public bool ReadyEventFired { get; protected set; }

    /// <summary>
    ///     获取初始化方法是否已调用的状态
    /// </summary>
    public bool InitCalled { get; protected set; }

    /// <summary>
    ///     获取架构阶段历史记录列表
    /// </summary>
    public List<ArchitecturePhase> PhaseHistory { get; } = [];

    /// <summary>
    ///     添加注册后钩子函数
    /// </summary>
    /// <param name="hook">要添加的钩子函数</param>
    public void AddPostRegistrationHook(Action<TestArchitectureBase> hook)
    {
        _postRegistrationHook = hook;
    }

    /// <summary>
    ///     初始化架构组件，注册模型、系统并设置事件监听器
    /// </summary>
    protected override void OnInitialize()
    {
        InitCalled = true;
        _postRegistrationHook?.Invoke(this);

        // 订阅阶段变更事件以记录历史
        PhaseChanged += phase => PhaseHistory.Add(phase);
    }
}