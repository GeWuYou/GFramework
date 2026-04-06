using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GFramework.Core.Architectures;
using GFramework.Game.Config;
using GFramework.Game.Config.Generated;
using NUnit.Framework;

namespace GFramework.Game.Tests.Config;

/// <summary>
///     验证在 <see cref="Architecture" /> 初始化流程中可以注册配置注册表、执行加载并通过生成的表访问器读取数据。
/// </summary>
[TestFixture]
public class ArchitectureConfigIntegrationTests
{
    /// <summary>
    ///     架构初始化期间，通过 <see cref="YamlConfigLoader" /> 注册生成表，
    ///     并将 <see cref="ConfigRegistry" /> 作为 utility 暴露给架构上下文读取。
    /// </summary>
    [Test]
    public async Task ConfigLoaderCanRunDuringArchitectureInitialization()
    {
        var rootPath = CreateTempConfigRoot();
        ConsumerArchitecture? architecture = null;
        var initialized = false;
        try
        {
            architecture = new ConsumerArchitecture(rootPath);
            await architecture.InitializeAsync();
            initialized = true;

            var table = architecture.MonsterTable;

            Assert.Multiple(() =>
            {
                Assert.That(table.Get(1).Name, Is.EqualTo("Slime"));
                Assert.That(table.Get(2).Hp, Is.EqualTo(30));
                Assert.That(table.FindByFaction("dungeon").Select(static config => config.Name),
                    Is.EquivalentTo(new[] { "Slime", "Goblin" }));
                Assert.That(architecture.Registry.TryGetMonsterTable(out var retrieved), Is.True);
                Assert.That(retrieved, Is.Not.Null);
                Assert.That(retrieved!.Get(1).Name, Is.EqualTo("Slime"));
                Assert.That(architecture.Context.GetUtility<ConfigRegistry>(), Is.SameAs(architecture.Registry));
            });
        }
        finally
        {
            if (architecture is not null && initialized)
            {
                await architecture.DestroyAsync();
            }

            DeleteDirectoryIfExists(rootPath);
        }
    }

    private static string CreateTempConfigRoot()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "GFramework.ConfigArchitecture", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootPath);
        Directory.CreateDirectory(Path.Combine(rootPath, "schemas"));
        Directory.CreateDirectory(Path.Combine(rootPath, "monster"));
        File.WriteAllText(Path.Combine(rootPath, "schemas", "monster.schema.json"), MonsterSchemaJson);
        File.WriteAllText(Path.Combine(rootPath, "monster", "slime.yaml"), MonsterSlimeYaml);
        File.WriteAllText(Path.Combine(rootPath, "monster", "goblin.yaml"), MonsterGoblinYaml);
        return rootPath;
    }

    /// <summary>
    ///     最佳努力尝试删除临时目录。
    /// </summary>
    private static void DeleteDirectoryIfExists(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        try
        {
            Directory.Delete(path, true);
        }
        catch (IOException)
        {
            // Ignored: cleanup is best effort and should not fail the test.
        }
        catch (UnauthorizedAccessException)
        {
            // Ignored: cleanup is best effort and can transiently fail when files are still being released.
        }
    }

    private const string MonsterSchemaJson = @"{
  ""title"": ""Monster Config"",
  ""description"": ""Defines one monster entry for the generated consumer integration test."",
  ""type"": ""object"",
  ""required"": [
    ""id"",
    ""name"",
    ""hp"",
    ""faction""
  ],
  ""properties"": {
    ""id"": {
      ""type"": ""integer"",
      ""description"": ""Monster identifier.""
    },
    ""name"": {
      ""type"": ""string"",
      ""description"": ""Monster display name.""
    },
    ""hp"": {
      ""type"": ""integer"",
      ""description"": ""Monster base health.""
    },
    ""faction"": {
      ""type"": ""string"",
      ""description"": ""Used by the integration test to validate generated non-unique queries.""
    }
  }
}";

    private const string MonsterSlimeYaml =
        "id: 1\nname: Slime\nhp: 10\nfaction: dungeon\n";

    private const string MonsterGoblinYaml =
        "id: 2\nname: Goblin\nhp: 30\nfaction: dungeon\n";

    private sealed class ConsumerArchitecture : Architecture
    {
        private readonly string _configRoot;

        public ConfigRegistry Registry { get; }

        public MonsterTable MonsterTable { get; private set; } = null!;

        public ConsumerArchitecture(string configRoot)
        {
            _configRoot = configRoot ?? throw new ArgumentNullException(nameof(configRoot));
            Registry = new ConfigRegistry();
        }

        protected override void OnInitialize()
        {
            RegisterUtility(Registry);

            var loader = new YamlConfigLoader(_configRoot)
                .RegisterMonsterTable();
            loader.LoadAsync(Registry).GetAwaiter().GetResult();
            MonsterTable = Registry.GetMonsterTable();
        }
    }
}
