using GFramework.Game.Config;

namespace GFramework.Game.Tests.Config;

/// <summary>
///     验证内存配置表的基础只读查询行为。
/// </summary>
[TestFixture]
public class InMemoryConfigTableTests
{
    /// <summary>
    ///     验证已存在主键可以被正确查询。
    /// </summary>
    [Test]
    public void Get_Should_Return_Config_When_Key_Exists()
    {
        var table = new InMemoryConfigTable<int, MonsterConfigStub>(
            new[]
            {
                new MonsterConfigStub(1, "Slime"),
                new MonsterConfigStub(2, "Goblin")
            },
            static config => config.Id);

        var result = table.Get(2);

        Assert.That(result.Name, Is.EqualTo("Goblin"));
    }

    /// <summary>
    ///     验证重复主键会在加载期被拒绝，避免运行期覆盖旧值。
    /// </summary>
    [Test]
    public void Constructor_Should_Throw_When_Duplicate_Key_Is_Detected()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new InMemoryConfigTable<int, MonsterConfigStub>(
                new[]
                {
                    new MonsterConfigStub(1, "Slime"),
                    new MonsterConfigStub(1, "Goblin")
                },
                static config => config.Id));
    }

    /// <summary>
    ///     验证 All 返回的集合包含完整快照。
    /// </summary>
    [Test]
    public void All_Should_Return_All_Configs()
    {
        var table = new InMemoryConfigTable<int, MonsterConfigStub>(
            new[]
            {
                new MonsterConfigStub(1, "Slime"),
                new MonsterConfigStub(2, "Goblin")
            },
            static config => config.Id);

        var all = table.All();

        Assert.That(all, Has.Count.EqualTo(2));
        Assert.That(all.Select(static config => config.Name), Is.EquivalentTo(new[] { "Slime", "Goblin" }));
    }

    /// <summary>
    ///     用于配置表测试的最小配置类型。
    /// </summary>
    /// <param name="Id">配置主键。</param>
    /// <param name="Name">配置名称。</param>
    private sealed record MonsterConfigStub(int Id, string Name);
}