using GFramework.Core.Tests.Model;
using GFramework.Core.Tests.Systems;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     同步测试架构类，用于测试架构的生命周期和事件处理
/// </summary>
public sealed class SyncTestArchitecture : TestArchitectureBase
{
    /// <summary>
    ///     初始化架构组件，注册模型、系统并设置事件监听器
    /// </summary>
    protected override void OnInitialize()
    {
        RegisterModel(new TestModel());
        RegisterSystem(new TestSystem());
        base.OnInitialize();
    }
}