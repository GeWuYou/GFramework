using GFramework.Core.Tests.model;
using GFramework.Core.Tests.system;

namespace GFramework.Core.Tests.architecture;

/// <summary>
/// 异步测试架构，用于测试异步模型和系统的初始化
/// </summary>
public class AsyncTestArchitecture : TestArchitectureBase
{
    /// <summary>
    /// 异步初始化架构
    /// </summary>
    protected override void Init()
    {
        // 注册模型
        RegisterModel(new AsyncTestModel());
        // 注册系统
        RegisterSystem(new AsyncTestSystem());
        base.Init();
    }
}