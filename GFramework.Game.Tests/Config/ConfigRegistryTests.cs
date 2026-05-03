// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Config;
using GFramework.Game.Config;

namespace GFramework.Game.Tests.Config;

/// <summary>
///     验证配置注册表的注册、覆盖和类型检查行为。
/// </summary>
[TestFixture]
public class ConfigRegistryTests
{
    /// <summary>
    ///     验证注册后的配置表可以按名称和类型成功解析。
    /// </summary>
    [Test]
    public void RegisterTable_Then_GetTable_Should_Return_Registered_Instance()
    {
        var registry = new ConfigRegistry();
        var table = CreateMonsterTable();

        registry.RegisterTable("monster", table);

        var resolved = registry.GetTable<int, MonsterConfigStub>("monster");

        Assert.That(resolved, Is.SameAs(table));
    }

    /// <summary>
    ///     验证同名注册会覆盖旧表，用于后续热重载场景。
    /// </summary>
    [Test]
    public void RegisterTable_Should_Replace_Previous_Table_With_Same_Name()
    {
        var registry = new ConfigRegistry();
        var oldTable = CreateMonsterTable();
        var newTable = new InMemoryConfigTable<int, MonsterConfigStub>(
            new[]
            {
                new MonsterConfigStub(3, "Orc")
            },
            static config => config.Id);

        registry.RegisterTable("monster", oldTable);
        registry.RegisterTable("monster", newTable);

        var resolved = registry.GetTable<int, MonsterConfigStub>("monster");

        Assert.That(resolved, Is.SameAs(newTable));
        Assert.That(resolved.Count, Is.EqualTo(1));
    }

    /// <summary>
    ///     验证请求类型与实际注册类型不匹配时会抛出异常，避免消费端默默读取错误表。
    /// </summary>
    [Test]
    public void GetTable_Should_Throw_When_Requested_Type_Does_Not_Match_Registered_Table()
    {
        var registry = new ConfigRegistry();
        registry.RegisterTable("monster", CreateMonsterTable());

        Assert.Throws<InvalidOperationException>(() => registry.GetTable<string, MonsterConfigStub>("monster"));
    }

    /// <summary>
    ///     验证弱类型查询入口可以在不知道泛型参数时返回原始配置表。
    /// </summary>
    [Test]
    public void TryGetTable_Should_Return_Raw_Table_When_Name_Exists()
    {
        var registry = new ConfigRegistry();
        var table = CreateMonsterTable();
        registry.RegisterTable("monster", table);

        var found = registry.TryGetTable("monster", out var rawTable);

        Assert.Multiple(() =>
        {
            Assert.That(found, Is.True);
            Assert.That(rawTable, Is.SameAs(table));
            Assert.That(rawTable!.KeyType, Is.EqualTo(typeof(int)));
        });
    }

    /// <summary>
    ///     验证移除和清空操作会更新注册表状态。
    /// </summary>
    [Test]
    public void RemoveTable_And_Clear_Should_Update_Registry_State()
    {
        var registry = new ConfigRegistry();
        registry.RegisterTable("monster", CreateMonsterTable());
        registry.RegisterTable("npc", CreateNpcTable());

        var removed = registry.RemoveTable("monster");

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True);
            Assert.That(registry.HasTable("monster"), Is.False);
            Assert.That(registry.Count, Is.EqualTo(1));
        });

        registry.Clear();

        Assert.That(registry.Count, Is.EqualTo(0));
    }

    /// <summary>
    ///     创建怪物配置表测试实例。
    /// </summary>
    /// <returns>怪物配置表。</returns>
    private static IConfigTable<int, MonsterConfigStub> CreateMonsterTable()
    {
        return new InMemoryConfigTable<int, MonsterConfigStub>(
            new[]
            {
                new MonsterConfigStub(1, "Slime"),
                new MonsterConfigStub(2, "Goblin")
            },
            static config => config.Id);
    }

    /// <summary>
    ///     创建 NPC 配置表测试实例。
    /// </summary>
    /// <returns>NPC 配置表。</returns>
    private static IConfigTable<Guid, NpcConfigStub> CreateNpcTable()
    {
        return new InMemoryConfigTable<Guid, NpcConfigStub>(
            new[]
            {
                new NpcConfigStub(Guid.NewGuid(), "Guide")
            },
            static config => config.Id);
    }

    /// <summary>
    ///     用于怪物配置表测试的最小配置类型。
    /// </summary>
    /// <param name="Id">配置主键。</param>
    /// <param name="Name">配置名称。</param>
    private sealed record MonsterConfigStub(int Id, string Name);

    /// <summary>
    ///     用于 NPC 配置表测试的最小配置类型。
    /// </summary>
    /// <param name="Id">配置主键。</param>
    /// <param name="Name">配置名称。</param>
    private sealed record NpcConfigStub(Guid Id, string Name);
}