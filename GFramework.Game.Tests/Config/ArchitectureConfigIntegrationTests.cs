using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GFramework.Core.Architectures;
using GFramework.Core.Extensions;
using GFramework.Core.Utility;
using GFramework.Game.Abstractions.Config;
using GFramework.Game.Config;
using GFramework.Game.Config.Generated;
using NUnit.Framework;

namespace GFramework.Game.Tests.Config;

/// <summary>
///     验证 <see cref="Architecture" /> 场景下的官方配置模块接入链路。
///     这些测试覆盖模块安装、utility 初始化顺序以及生成表访问，确保模块化入口能够替代手写 bootstrap 模板。
/// </summary>
[TestFixture]
public class ArchitectureConfigIntegrationTests
{
    /// <summary>
    ///     清理全局架构上下文，避免测试之间残留同类型架构绑定。
    /// </summary>
    [SetUp]
    [TearDown]
    public void ResetGlobalArchitectureContext()
    {
        GameContext.Clear();
    }

    /// <summary>
    ///     架构初始化期间，通过 <see cref="GameConfigModule" /> 收敛生成表注册、加载与注册表暴露，
    ///     并将 <see cref="IConfigRegistry" /> 作为 utility 暴露给架构上下文读取。
    /// </summary>
    [Test]
    public async Task ConfigModuleCanRunDuringArchitectureInitialization()
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
                Assert.That(architecture.Registry.TryGetItemTable(out _), Is.False);
                Assert.That(architecture.Context.GetUtility<IConfigRegistry>(), Is.SameAs(architecture.Registry));
                Assert.That(architecture.ConfigModule.IsInitialized, Is.True);
                Assert.That(architecture.ConfigModule.IsHotReloadEnabled, Is.False);
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

    /// <summary>
    ///     验证配置模块会在其他 utility 初始化之前完成首次加载，
    ///     这样依赖配置的 utility 无需再自行阻塞等待配置系统完成启动。
    /// </summary>
    [Test]
    public async Task ConfigModuleShould_Load_Config_Before_Dependent_Utility_Initialization()
    {
        var rootPath = CreateTempConfigRoot();
        ConsumerArchitecture? architecture = null;
        var initialized = false;
        try
        {
            architecture = new ConsumerArchitecture(rootPath);
            await architecture.InitializeAsync();
            initialized = true;

            Assert.Multiple(() =>
            {
                Assert.That(architecture.ProbeUtility.InitializedWithLoadedConfig, Is.True);
                Assert.That(architecture.ProbeUtility.ObservedMonsterName, Is.EqualTo("Slime"));
                Assert.That(architecture.ProbeUtility.ObservedDungeonMonsterCount, Is.EqualTo(2));
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

    /// <summary>
    ///     验证同一个模块实例不会被重复安装到多个架构中，
    ///     避免共享内部 bootstrap 状态导致跨架构生命周期混淆。
    /// </summary>
    [Test]
    public async Task GameConfigModuleShould_Reject_Reusing_The_Same_Module_Instance()
    {
        var rootPath = CreateTempConfigRoot();
        ModuleOnlyArchitecture? firstArchitecture = null;
        var firstDestroyed = false;
        try
        {
            var module = CreateModule(rootPath);

            firstArchitecture = new ModuleOnlyArchitecture(module);
            await firstArchitecture.InitializeAsync();
            var wasInitializedBeforeDestroy = module.IsInitialized;
            await firstArchitecture.DestroyAsync();
            firstDestroyed = true;
            firstArchitecture = null;
            GameContext.Clear();

            var secondArchitecture = new ModuleOnlyArchitecture(module);
            var exception =
                Assert.ThrowsAsync<InvalidOperationException>(async () => await secondArchitecture.InitializeAsync());

            Assert.Multiple(() =>
            {
                Assert.That(wasInitializedBeforeDestroy, Is.True);
                Assert.That(exception, Is.Not.Null);
                Assert.That(exception!.Message, Does.Contain("cannot be installed more than once"));
            });
        }
        finally
        {
            if (firstArchitecture is not null && !firstDestroyed)
            {
                await firstArchitecture.DestroyAsync();
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

    /// <summary>
    ///     创建一个使用配置模块的架构实例。
    /// </summary>
    /// <param name="configRoot">测试配置根目录。</param>
    /// <returns>已配置的模块实例。</returns>
    private static GameConfigModule CreateModule(string configRoot)
    {
        return new GameConfigModule(
            new GameConfigBootstrapOptions
            {
                RootPath = configRoot,
                ConfigureLoader = static loader =>
                    loader.RegisterAllGeneratedConfigTables(
                        new GeneratedConfigRegistrationOptions
                        {
                            IncludedConfigDomains = new[] { MonsterConfigBindings.ConfigDomain }
                        })
            });
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
        private readonly GameConfigModule _configModule;
        private readonly ConfigAwareProbeUtility _probeUtility = new();

        public ConsumerArchitecture(string configRoot)
        {
            if (configRoot == null)
            {
                throw new ArgumentNullException(nameof(configRoot));
            }

            _configModule = CreateModule(configRoot);
        }

        public GameConfigModule ConfigModule => _configModule;

        public IConfigRegistry Registry => _configModule.Registry;

        public MonsterTable MonsterTable => Registry.GetMonsterTable();

        public ConfigAwareProbeUtility ProbeUtility => _probeUtility;

        /// <summary>
        ///     在用户初始化阶段安装配置模块，并注册一个依赖配置的测试 utility，
        ///     以验证模块会在 utility 初始化前完成首次加载。
        /// </summary>
        protected override void OnInitialize()
        {
            InstallModule(_configModule);
            RegisterUtility(_probeUtility);
        }
    }

    /// <summary>
    ///     用于验证模块复用限制的最小架构。
    /// </summary>
    private sealed class ModuleOnlyArchitecture(GameConfigModule configModule) : Architecture
    {
        public GameConfigModule ConfigModule => configModule;

        /// <summary>
        ///     安装外部传入的配置模块。
        /// </summary>
        protected override void OnInitialize()
        {
            InstallModule(configModule);
        }
    }

    /// <summary>
    ///     在 utility 初始化阶段直接读取配置表的探针工具。
    ///     如果模块没有在 utility 阶段开始前完成首次加载，这个探针会在初始化时失败。
    /// </summary>
    private sealed class ConfigAwareProbeUtility : AbstractContextUtility
    {
        /// <summary>
        ///     获取一个值，指示初始化时是否已经读取到有效配置。
        /// </summary>
        public bool InitializedWithLoadedConfig { get; private set; }

        /// <summary>
        ///     获取初始化期间读取到的怪物名称。
        /// </summary>
        public string? ObservedMonsterName { get; private set; }

        /// <summary>
        ///     获取初始化期间读取到的 dungeon 阵营怪物数量。
        /// </summary>
        public int ObservedDungeonMonsterCount { get; private set; }

        /// <summary>
        ///     读取架构上下文中的配置注册表并验证目标表已经可用。
        /// </summary>
        protected override void OnInit()
        {
            var registry = this.GetUtility<IConfigRegistry>();
            var monsterTable = registry.GetMonsterTable();

            ObservedMonsterName = monsterTable.Get(1).Name;
            ObservedDungeonMonsterCount = monsterTable.FindByFaction("dungeon").Count();
            InitializedWithLoadedConfig = true;
        }
    }
}
