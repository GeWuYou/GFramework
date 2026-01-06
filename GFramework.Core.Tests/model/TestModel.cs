using GFramework.Core.Abstractions.enums;
using GFramework.Core.model;

namespace GFramework.Core.Tests.model;

/// <summary>
///     测试模型类，用于框架测试目的
/// </summary>
public sealed class TestModel : AbstractModel, ITestModel
{
    public const int DefaultXp = 5;

    /// <summary>
    ///     获取模型是否已初始化的状态
    /// </summary>
    public bool Initialized { get; private set; }

    /// <summary>
    ///     初始化模型
    /// </summary>
    public void Init()
    {
        Initialized = true;
    }


    public void OnArchitecturePhase(ArchitecturePhase phase)
    {
    }

    public int GetCurrentXp { get; } = DefaultXp;

    protected override void OnInit()
    {
    }
}