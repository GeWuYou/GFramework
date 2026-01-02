using GFramework.Core.Abstractions.enums;
using GFramework.Core.architecture;
using GFramework.Core.events;
using GFramework.Core.Tests.model;
using GFramework.Core.Tests.system;

namespace GFramework.Core.Tests.architecture;

/// <summary>
/// 异步测试架构，用于测试异步模型和系统的初始化
/// </summary>
public class AsyncTestArchitecture : Architecture
{
    /// <summary>
    /// 初始化完成事件是否触发
    /// </summary>
    public bool ReadyEventFired { get; private set; }

    /// <summary>
    /// Init 方法是否调用
    /// </summary>
    public bool InitCalled { get; private set; }

    /// <summary>
    /// 阶段进入记录
    /// </summary>
    public List<ArchitecturePhase> PhaseHistory { get; } = new();

    /// <summary>
    /// 异步初始化架构
    /// </summary>
    protected override void Init()
    {
        InitCalled = true;

        // 注册模型
        RegisterModel(new AsyncTestModel());

        // 注册系统
        RegisterSystem(new AsyncTestSystem());

        // 订阅 Ready 事件
        Context.RegisterEvent<ArchitectureEvents.ArchitectureLifecycleReadyEvent>(_ => { ReadyEventFired = true; });
    }

    /// <summary>
    /// 进入阶段时记录
    /// </summary>
    /// <param name="next"></param>
    protected override void EnterPhase(ArchitecturePhase next)
    {
        base.EnterPhase(next);
        PhaseHistory.Add(next);
    }
}