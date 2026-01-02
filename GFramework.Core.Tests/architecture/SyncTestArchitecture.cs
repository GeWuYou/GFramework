using GFramework.Core.Abstractions.enums;
using GFramework.Core.architecture;
using GFramework.Core.events;
using GFramework.Core.Tests.model;
using GFramework.Core.Tests.system;

namespace GFramework.Core.Tests.architecture;

/// <summary>
/// 同步测试架构类，用于测试架构的生命周期和事件处理
/// </summary>
public sealed class SyncTestArchitecture : Architecture
{
    private Action<SyncTestArchitecture>? _postRegistrationHook;

    /// <summary>
    /// 获取就绪事件是否已触发的状态
    /// </summary>
    public bool ReadyEventFired { get; private set; }

    /// <summary>
    /// 获取初始化方法是否已调用的状态
    /// </summary>
    public bool InitCalled { get; private set; }

    /// <summary>
    /// 获取架构阶段历史记录列表
    /// </summary>
    public List<ArchitecturePhase> PhaseHistory { get; } = [];

    /// <summary>
    /// 添加注册后钩子函数
    /// </summary>
    /// <param name="hook">要添加的钩子函数</param>
    public void AddPostRegistrationHook(Action<SyncTestArchitecture> hook) => _postRegistrationHook = hook;

    /// <summary>
    /// 初始化架构组件，注册模型、系统并设置事件监听器
    /// </summary>
    protected override void Init()
    {
        InitCalled = true;

        RegisterModel(new TestModel());
        RegisterSystem(new TestSystem());
        _postRegistrationHook?.Invoke(this);
        Context.RegisterEvent<ArchitectureEvents.ArchitectureLifecycleReadyEvent>(_ => { ReadyEventFired = true; });
    }

    /// <summary>
    /// 进入指定架构阶段时的处理方法，记录阶段历史
    /// </summary>
    /// <param name="next">要进入的下一个架构阶段</param>
    protected override void EnterPhase(ArchitecturePhase next)
    {
        base.EnterPhase(next);
        // 记录进入的架构阶段到历史列表中
        PhaseHistory.Add(next);
    }
}