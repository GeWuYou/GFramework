using GFramework.Core.Tests.model;
using GFramework.Core.Tests.system;

namespace GFramework.Core.Tests.architecture;

/// <summary>
/// 同步测试架构类，用于测试架构的生命周期和事件处理
/// </summary>
public sealed class SyncTestArchitecture : TestArchitectureBase
{
    /// <summary>
    /// 初始化架构组件，注册模型、系统并设置事件监听器
    /// </summary>
    protected override void Init()
    {
        RegisterModel(new TestModel());
        RegisterSystem(new TestSystem());
        base.Init();
    }
}