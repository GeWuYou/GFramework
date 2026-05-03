// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Game.Abstractions.Config;
using GFramework.Game.Config;
using GFramework.Game.Config.Generated;
using NUnit.Framework;

namespace GFramework.Game.Tests.Config;

/// <summary>
///     验证官方配置启动帮助器能够收敛注册、加载与热重载生命周期。
/// </summary>
[TestFixture]
public class GameConfigBootstrapTests
{
    /// <summary>
    ///     为每个测试准备独立的临时配置目录。
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _rootPath = Path.Combine(Path.GetTempPath(), "GFramework.GameConfigBootstrapTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_rootPath);
    }

    /// <summary>
    ///     清理测试期间创建的临时目录。
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, true);
        }
    }

    private string _rootPath = null!;

    /// <summary>
    ///     验证启动帮助器能够加载生成表，并复用调用方显式提供的注册表实例。
    /// </summary>
    [Test]
    public async Task InitializeAsync_Should_Load_Generated_Config_Tables_Into_Registry()
    {
        CreateMonsterFiles();

        var registry = new ConfigRegistry();
        using var bootstrap = CreateBootstrap(registry);

        await bootstrap.InitializeAsync().ConfigureAwait(false);

        var monsterTable = registry.GetMonsterTable();

        Assert.Multiple(() =>
        {
            Assert.That(bootstrap.Registry, Is.SameAs(registry));
            Assert.That(bootstrap.Loader.RegistrationCount, Is.EqualTo(1));
            Assert.That(bootstrap.IsInitialized, Is.True);
            Assert.That(bootstrap.IsHotReloadEnabled, Is.False);
            Assert.That(monsterTable.Get(1).Name, Is.EqualTo("Slime"));
            Assert.That(monsterTable.Get(2).Hp, Is.EqualTo(30));
        });
    }

    /// <summary>
    ///     验证启动帮助器可以在初始化后显式启用热重载，并将刷新结果写回共享注册表。
    /// </summary>
    [Test]
    public async Task StartHotReload_Should_Update_Registered_Table_When_Config_File_Changes()
    {
        CreateMonsterFiles();

        using var bootstrap = CreateBootstrap();
        await bootstrap.InitializeAsync().ConfigureAwait(false);

        var reloadTaskSource = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        bootstrap.StartHotReload(
            new YamlConfigHotReloadOptions
            {
                OnTableReloaded = tableName => reloadTaskSource.TrySetResult(tableName),
                DebounceDelay = TimeSpan.FromMilliseconds(150)
            });

        try
        {
            CreateFile(
                "monster/slime.yaml",
                """
                id: 1
                name: Slime
                hp: 25
                faction: dungeon
                """);

            var tableName = await WaitForTaskWithinAsync(reloadTaskSource.Task, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            var monsterTable = bootstrap.Registry.GetMonsterTable();

            Assert.Multiple(() =>
            {
                Assert.That(tableName, Is.EqualTo(MonsterConfigBindings.TableName));
                Assert.That(bootstrap.IsHotReloadEnabled, Is.True);
                Assert.That(monsterTable.Get(1).Hp, Is.EqualTo(25));
            });
        }
        finally
        {
            bootstrap.StopHotReload();
        }
    }

    /// <summary>
    ///     验证缺少加载器配置回调时会在构造阶段被拒绝，避免启动帮助器静默创建空加载流程。
    /// </summary>
    [Test]
    public void Constructor_Should_Throw_When_ConfigureLoader_Is_Missing()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            _ = new GameConfigBootstrap(
                new GameConfigBootstrapOptions
                {
                    RootPath = _rootPath
                }));

        Assert.That(exception!.ParamName, Is.EqualTo("options"));
    }

    /// <summary>
    ///     验证初始化链路进行中时，第二个调用者不会再次进入并发初始化流程。
    /// </summary>
    [Test]
    public void InitializeAsync_Should_Reject_Concurrent_Caller_While_Initialization_Is_In_Progress()
    {
        CreateMonsterFiles();

        using ManualResetEventSlim initializeEntered = new(false);
        using ManualResetEventSlim continueInitialization = new(false);
        using var bootstrap = new GameConfigBootstrap(
            new GameConfigBootstrapOptions
            {
                RootPath = _rootPath,
                ConfigureLoader = loader =>
                {
                    initializeEntered.Set();
                    Assert.That(
                        continueInitialization.Wait(TimeSpan.FromSeconds(5)),
                        Is.True,
                        "The first initialization attempt did not resume within the expected timeout.");
                    loader.RegisterAllGeneratedConfigTables(
                        new GeneratedConfigRegistrationOptions
                        {
                            IncludedConfigDomains = new[] { MonsterConfigBindings.ConfigDomain }
                        });
                }
            });

        var firstInitializeTask = Task.Run(() => bootstrap.InitializeAsync());

        Assert.That(
            initializeEntered.Wait(TimeSpan.FromSeconds(5)),
            Is.True,
            "The first initialization attempt did not reach the guarded lifecycle section.");

        var secondCallerException = Assert.ThrowsAsync<InvalidOperationException>(() => bootstrap.InitializeAsync());

        continueInitialization.Set();

        Assert.That(async () => await firstInitializeTask.ConfigureAwait(false), Throws.Nothing);

        Assert.Multiple(() =>
        {
            Assert.That(secondCallerException, Is.Not.Null);
            Assert.That(secondCallerException!.Message, Does.Contain("only be initialized once"));
            Assert.That(bootstrap.IsInitialized, Is.True);
        });
    }

    /// <summary>
    ///     验证在可选热重载启动失败时，不会提前公开加载器与初始化成功状态。
    /// </summary>
    [Test]
    public void InitializeAsync_Should_Not_Publish_State_When_HotReload_Enable_Fails()
    {
        CreateMonsterFiles();

        using var bootstrap = new GameConfigBootstrap(
            new GameConfigBootstrapOptions
            {
                RootPath = _rootPath,
                EnableHotReload = true,
                HotReloadOptions = new YamlConfigHotReloadOptions
                {
                    DebounceDelay = TimeSpan.FromMilliseconds(-1)
                },
                ConfigureLoader = static loader =>
                    loader.RegisterAllGeneratedConfigTables(
                        new GeneratedConfigRegistrationOptions
                        {
                            IncludedConfigDomains = new[] { MonsterConfigBindings.ConfigDomain }
                        })
            });

        var exception = Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => bootstrap.InitializeAsync());

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(bootstrap.IsInitialized, Is.False);
            Assert.That(bootstrap.IsHotReloadEnabled, Is.False);
            Assert.Throws<InvalidOperationException>(() => _ = bootstrap.Loader);
        });
    }

    /// <summary>
    ///     创建一个使用生成聚合注册入口的官方启动帮助器。
    /// </summary>
    /// <param name="registry">可选的外部注册表；为空时使用默认注册表。</param>
    /// <returns>已配置但尚未初始化的启动帮助器。</returns>
    private GameConfigBootstrap CreateBootstrap(IConfigRegistry? registry = null)
    {
        return new GameConfigBootstrap(
            new GameConfigBootstrapOptions
            {
                RootPath = _rootPath,
                Registry = registry,
                ConfigureLoader = static loader =>
                    loader.RegisterAllGeneratedConfigTables(
                        new GeneratedConfigRegistrationOptions
                        {
                            IncludedConfigDomains = new[] { MonsterConfigBindings.ConfigDomain }
                        })
            });
    }

    /// <summary>
    ///     在临时消费者根目录中创建测试文件。
    /// </summary>
    /// <param name="relativePath">相对根目录的文件路径。</param>
    /// <param name="content">要写入的文件内容。</param>
    private void CreateFile(string relativePath, string content)
    {
        var path = Path.Combine(_rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var directoryPath = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(path, content.Replace("\n", Environment.NewLine, StringComparison.Ordinal));
    }

    /// <summary>
    ///     在临时目录中创建 monster schema 与 YAML 测试数据。
    /// </summary>
    private void CreateMonsterFiles()
    {
        CreateFile(
            "schemas/monster.schema.json",
            """
            {
              "title": "Monster Config",
              "description": "Defines one monster entry for the bootstrap tests.",
              "type": "object",
              "required": ["id", "name", "hp", "faction"],
              "properties": {
                "id": {
                  "type": "integer",
                  "description": "Monster identifier."
                },
                "name": {
                  "type": "string",
                  "description": "Monster display name."
                },
                "hp": {
                  "type": "integer",
                  "description": "Monster base health."
                },
                "faction": {
                  "type": "string",
                  "description": "Used by the bootstrap tests to validate generated queries."
                }
              }
            }
            """);
        CreateFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            hp: 10
            faction: dungeon
            """);
        CreateFile(
            "monster/goblin.yaml",
            """
            id: 2
            name: Goblin
            hp: 30
            faction: dungeon
            """);
    }

    /// <summary>
    ///     在限定时间内等待异步任务完成，避免文件监听测试无限挂起。
    /// </summary>
    /// <typeparam name="T">任务结果类型。</typeparam>
    /// <param name="task">要等待的任务。</param>
    /// <param name="timeout">超时时间。</param>
    /// <returns>任务结果。</returns>
    private static async Task<T> WaitForTaskWithinAsync<T>(Task<T> task, TimeSpan timeout)
    {
        var completedTask = await Task.WhenAny(task, Task.Delay(timeout)).ConfigureAwait(false);
        if (!ReferenceEquals(completedTask, task))
        {
            Assert.Fail($"Timed out after {timeout} while waiting for file watcher notification.");
        }

        return await task.ConfigureAwait(false);
    }
}
