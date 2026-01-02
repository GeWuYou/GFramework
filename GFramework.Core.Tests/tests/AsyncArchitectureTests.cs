using GFramework.Core.Tests.architecture;
using NUnit.Framework;

namespace GFramework.Core.Tests.tests;

/// <summary>
/// 异步架构测试类，用于测试异步架构的相关功能
/// </summary>
/// <remarks>
/// 该测试类使用非并行执行模式，确保测试的隔离性
/// </remarks>
[TestFixture]
[NonParallelizable]
public class AsyncArchitectureTests : ArchitectureTestsBase<AsyncTestArchitecture>
{
    /// <summary>
    /// 创建异步测试架构实例
    /// </summary>
    /// <returns>AsyncTestArchitecture实例</returns>
    protected override AsyncTestArchitecture CreateArchitecture() => new();

    /// <summary>
    /// 初始化架构的异步方法
    /// </summary>
    /// <returns>表示异步操作的Task</returns>
    protected override async Task InitializeArchitecture()
    {
        await Architecture!.InitializeAsync();
    }
}