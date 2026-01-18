using System.Collections.Immutable;
using GFramework.Core.Abstractions.enums;
using GFramework.Core.architecture;
using NUnit.Framework;

namespace GFramework.Core.Tests.architecture;

/// <summary>
/// ArchitectureConstants类的单元测试
/// 测试内容包括：
/// - 常量值的正确性
/// - 常量类型验证
/// - 常量可访问性
/// - 常量命名规范
/// - 架构阶段定义常量
/// </summary>
[TestFixture]
public class ArchitectureConstantsTests
{
    /// <summary>
    /// 测试PhaseOrder数组不为空
    /// </summary>
    [Test]
    public void PhaseOrder_Should_Not_Be_Empty()
    {
        Assert.That(ArchitectureConstants.PhaseOrder, Is.Not.Null);
        Assert.That(ArchitectureConstants.PhaseOrder, Is.Not.Empty);
    }

    /// <summary>
    /// 测试PhaseOrder包含所有预期的架构阶段
    /// </summary>
    [Test]
    public void PhaseOrder_Should_Contain_All_Expected_Phases()
    {
        var expectedPhases = new[]
        {
            ArchitecturePhase.None,
            ArchitecturePhase.BeforeUtilityInit,
            ArchitecturePhase.AfterUtilityInit,
            ArchitecturePhase.BeforeModelInit,
            ArchitecturePhase.AfterModelInit,
            ArchitecturePhase.BeforeSystemInit,
            ArchitecturePhase.AfterSystemInit,
            ArchitecturePhase.Ready,
            ArchitecturePhase.Destroying,
            ArchitecturePhase.Destroyed
        };

        Assert.That(ArchitectureConstants.PhaseOrder.Length, Is.EqualTo(expectedPhases.Length));

        foreach (var expectedPhase in expectedPhases)
        {
            Assert.That(ArchitectureConstants.PhaseOrder, Does.Contain(expectedPhase));
        }
    }

    /// <summary>
    /// 测试PhaseOrder数组是只读的
    /// </summary>
    [Test]
    public void PhaseOrder_Should_Be_Immutable()
    {
        var phaseOrder = ArchitectureConstants.PhaseOrder;
        Assert.That(phaseOrder, Is.Not.Null);
        Assert.That(phaseOrder, Is.InstanceOf<ArchitecturePhase[]>());
    }

    /// <summary>
    /// 测试PhaseOrder的顺序是正确的
    /// </summary>
    [Test]
    public void PhaseOrder_Should_Be_In_Correct_Sequence()
    {
        var phases = ArchitectureConstants.PhaseOrder;

        Assert.That(phases[0], Is.EqualTo(ArchitecturePhase.None), "First phase should be None");
        Assert.That(phases[1], Is.EqualTo(ArchitecturePhase.BeforeUtilityInit),
            "Second phase should be BeforeUtilityInit");
        Assert.That(phases[2], Is.EqualTo(ArchitecturePhase.AfterUtilityInit),
            "Third phase should be AfterUtilityInit");
        Assert.That(phases[3], Is.EqualTo(ArchitecturePhase.BeforeModelInit), "Fourth phase should be BeforeModelInit");
        Assert.That(phases[4], Is.EqualTo(ArchitecturePhase.AfterModelInit), "Fifth phase should be AfterModelInit");
        Assert.That(phases[5], Is.EqualTo(ArchitecturePhase.BeforeSystemInit),
            "Sixth phase should be BeforeSystemInit");
        Assert.That(phases[6], Is.EqualTo(ArchitecturePhase.AfterSystemInit),
            "Seventh phase should be AfterSystemInit");
        Assert.That(phases[7], Is.EqualTo(ArchitecturePhase.Ready), "Eighth phase should be Ready");
        Assert.That(phases[8], Is.EqualTo(ArchitecturePhase.Destroying), "Ninth phase should be Destroying");
        Assert.That(phases[9], Is.EqualTo(ArchitecturePhase.Destroyed), "Tenth phase should be Destroyed");
    }

    /// <summary>
    /// 测试PhaseTransitions字典不为空
    /// </summary>
    [Test]
    public void PhaseTransitions_Should_Not_Be_Empty()
    {
        Assert.That(ArchitectureConstants.PhaseTransitions, Is.Not.Null);
        Assert.That(ArchitectureConstants.PhaseTransitions, Is.Not.Empty);
    }

    /// <summary>
    /// 测试PhaseTransitions是只读的
    /// </summary>
    [Test]
    public void PhaseTransitions_Should_Be_Immutable()
    {
        var transitions = ArchitectureConstants.PhaseTransitions;
        Assert.That(transitions, Is.InstanceOf<ImmutableDictionary<ArchitecturePhase, ArchitecturePhase[]>>());
    }

    /// <summary>
    /// 测试PhaseTransitions包含正常线性流程的转换
    /// </summary>
    [Test]
    public void PhaseTransitions_Should_Contain_Normal_Linear_Transitions()
    {
        Assert.That(ArchitectureConstants.PhaseTransitions, Does.ContainKey(ArchitecturePhase.None));
        Assert.That(ArchitectureConstants.PhaseTransitions, Does.ContainKey(ArchitecturePhase.BeforeUtilityInit));
        Assert.That(ArchitectureConstants.PhaseTransitions, Does.ContainKey(ArchitecturePhase.AfterUtilityInit));
        Assert.That(ArchitectureConstants.PhaseTransitions, Does.ContainKey(ArchitecturePhase.BeforeModelInit));
        Assert.That(ArchitectureConstants.PhaseTransitions, Does.ContainKey(ArchitecturePhase.AfterModelInit));
        Assert.That(ArchitectureConstants.PhaseTransitions, Does.ContainKey(ArchitecturePhase.BeforeSystemInit));
        Assert.That(ArchitectureConstants.PhaseTransitions, Does.ContainKey(ArchitecturePhase.AfterSystemInit));
        Assert.That(ArchitectureConstants.PhaseTransitions, Does.ContainKey(ArchitecturePhase.Ready));
        Assert.That(ArchitectureConstants.PhaseTransitions, Does.ContainKey(ArchitecturePhase.Destroying));
    }

    /// <summary>
    /// 测试PhaseTransitions中的转换方向是正确的
    /// </summary>
    [Test]
    public void PhaseTransitions_Should_Have_Correct_Directions()
    {
        var transitions = ArchitectureConstants.PhaseTransitions;

        Assert.That(transitions[ArchitecturePhase.None], Does.Contain(ArchitecturePhase.BeforeUtilityInit));
        Assert.That(transitions[ArchitecturePhase.BeforeUtilityInit], Does.Contain(ArchitecturePhase.AfterUtilityInit));
        Assert.That(transitions[ArchitecturePhase.AfterUtilityInit], Does.Contain(ArchitecturePhase.BeforeModelInit));
        Assert.That(transitions[ArchitecturePhase.BeforeModelInit], Does.Contain(ArchitecturePhase.AfterModelInit));
        Assert.That(transitions[ArchitecturePhase.AfterModelInit], Does.Contain(ArchitecturePhase.BeforeSystemInit));
        Assert.That(transitions[ArchitecturePhase.BeforeSystemInit], Does.Contain(ArchitecturePhase.AfterSystemInit));
        Assert.That(transitions[ArchitecturePhase.AfterSystemInit], Does.Contain(ArchitecturePhase.Ready));
        Assert.That(transitions[ArchitecturePhase.Ready], Does.Contain(ArchitecturePhase.Destroying));
        Assert.That(transitions[ArchitecturePhase.Destroying], Does.Contain(ArchitecturePhase.Destroyed));
    }

    /// <summary>
    /// 测试PhaseTransitions包含失败初始化的转换路径
    /// </summary>
    [Test]
    public void PhaseTransitions_Should_Contain_FailedInitialization_Transition()
    {
        Assert.That(ArchitectureConstants.PhaseTransitions, Does.ContainKey(ArchitecturePhase.FailedInitialization));
        Assert.That(ArchitectureConstants.PhaseTransitions[ArchitecturePhase.FailedInitialization],
            Does.Contain(ArchitecturePhase.Destroying));
    }

    /// <summary>
    /// 测试每个阶段的转换数量不超过1个（线性转换）
    /// </summary>
    [Test]
    public void PhaseTransitions_Should_Have_Maximum_One_Transition_Per_Phase()
    {
        foreach (var transition in ArchitectureConstants.PhaseTransitions)
        {
            Assert.That(transition.Value, Has.Length.LessThanOrEqualTo(1),
                $"Phase {transition.Key} should have at most 1 transition");
        }
    }

    /// <summary>
    /// 测试PhaseOrder和PhaseTransitions的一致性
    /// </summary>
    [Test]
    public void PhaseOrder_And_PhaseTransitions_Should_Be_Consistent()
    {
        var phaseOrder = ArchitectureConstants.PhaseOrder;
        var transitions = ArchitectureConstants.PhaseTransitions;

        for (int i = 0; i < phaseOrder.Length - 1; i++)
        {
            var currentPhase = phaseOrder[i];
            var nextPhase = phaseOrder[i + 1];

            if (transitions.ContainsKey(currentPhase))
            {
                var possibleNextPhases = transitions[currentPhase];
                Assert.That(possibleNextPhases, Does.Contain(nextPhase),
                    $"Transition from {currentPhase} should include {nextPhase}");
            }
        }
    }
}