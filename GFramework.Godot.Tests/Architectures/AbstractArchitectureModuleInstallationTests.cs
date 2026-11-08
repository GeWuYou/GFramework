// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Abstractions.Environment;
using GFramework.Core.Abstractions.Events;
using GFramework.Core.Abstractions.Model;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Abstractions.Utility;
using GFramework.Godot.Architectures;

namespace GFramework.Godot.Tests.Architectures;

/// <summary>
///     验证 Godot 架构在模块安装前会先检查锚点状态，避免未绑定场景树时留下半安装副作用。
/// </summary>
[TestFixture]
public sealed class AbstractArchitectureModuleInstallationTests
{
    /// <summary>
    ///     验证装饰器把收缩后的非 CQRS 上下文能力面完整转发到底层上下文。
    /// </summary>
    [Test]
    public void GodotArchitectureContext_ShouldForwardNonCqrsMembers_ToInnerContext()
    {
        var inner = new RecordingArchitectureContext();
        var context = new GodotArchitectureContext(inner);

        context.GetService<DummyService>();
        context.GetServices<DummyService>();
        context.GetSystem<DummySystem>();
        context.GetSystems<DummySystem>();
        context.GetModel<DummyModel>();
        context.GetModels<DummyModel>();
        context.GetUtility<DummyUtility>();
        context.GetUtilities<DummyUtility>();
        context.GetServicesByPriority<DummyService>();
        context.GetSystemsByPriority<DummySystem>();
        context.GetModelsByPriority<DummyModel>();
        context.GetUtilitiesByPriority<DummyUtility>();
        context.SendEvent<DummyEvent>();
        context.SendEvent(new DummyEvent());
        context.RegisterEvent<DummyEvent>(_ => { });
        context.UnRegisterEvent<DummyEvent>(_ => { });
        context.GetEnvironment();

        Assert.That(inner.Calls, Is.EquivalentTo(
        [
            nameof(IArchitectureContext.GetService),
            nameof(IArchitectureContext.GetServices),
            nameof(IArchitectureContext.GetSystem),
            nameof(IArchitectureContext.GetSystems),
            nameof(IArchitectureContext.GetModel),
            nameof(IArchitectureContext.GetModels),
            nameof(IArchitectureContext.GetUtility),
            nameof(IArchitectureContext.GetUtilities),
            nameof(IArchitectureContext.GetServicesByPriority),
            nameof(IArchitectureContext.GetSystemsByPriority),
            nameof(IArchitectureContext.GetModelsByPriority),
            nameof(IArchitectureContext.GetUtilitiesByPriority),
            nameof(IArchitectureContext.SendEvent),
            nameof(IArchitectureContext.SendEvent),
            nameof(IArchitectureContext.RegisterEvent),
            nameof(IArchitectureContext.UnRegisterEvent),
            nameof(IArchitectureContext.GetEnvironment),
        ]));
    }

    /// <summary>
    ///     验证未显式传入上下文时，架构默认会包装成 Godot 上下文装饰器实现。
    /// </summary>
    [Test]
    public void AbstractArchitecture_ShouldUseGodotContextWrapper_ByDefault()
    {
        var architecture = new TestArchitecture();

        Assert.That(architecture.Context, Is.TypeOf<GodotArchitectureContext>());
    }

    /// <summary>
    ///     配置阶段之外的模块安装必须先失败，不能留下半安装副作用。
    /// </summary>
    [Test]
    public void InstallGodotModule_ShouldRejectOutsideConfigurationBeforeModuleInstall()
    {
        var architecture = new TestArchitecture();
        var module = new RecordingGodotModule();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            architecture.InstallGodotModuleForTest(module));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("ConfigureServices"));
            Assert.That(module.InstallCalled, Is.False);
        });
    }

    /// <summary>
    ///     模块声明可以在无原生节点的配置阶段完成；实际主线程附加由宿主启动钩子另行负责。
    /// </summary>
    /// <returns>纯托管配置测试生命周期任务。</returns>
    [Test]
    public async Task InstallGodotModule_ShouldDeclareServicesBeforeHostAttachment()
    {
        var module = new RecordingGodotModule();
        await using var architecture = new ManagedHostArchitecture(module);
        await architecture.InitializeAsync();

        Assert.Multiple(() =>
        {
            Assert.That(module.InstallCalled, Is.True);
            Assert.That(module.AttachCalled, Is.False);
            Assert.That(module.NodeReadCount, Is.Zero);
        });
    }

    /// <summary>
    ///     隔离原生宿主，单独验证配置阶段的模块声明路径。
    /// </summary>
    /// <param name="module">只记录调用的模块。</param>
    private sealed class ManagedHostArchitecture(RecordingGodotModule module) : AbstractArchitecture
    {
        /// <inheritdoc />
        protected override void InstallModules()
        {
            InstallGodotModule(module);
        }

        /// <inheritdoc />
        protected override ValueTask OnStartedAsync(CancellationToken cancellationToken)
        {
            // This fixture tests declarations only; native attachment is deliberately excluded from the managed host.
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>Exposes module installation so configuration-window guards can be observed without native attachment.</summary>
    private sealed class TestArchitecture : AbstractArchitecture
    {
        /// <inheritdoc />
        protected override void InstallModules()
        {
        }

        /// <summary>Forwards the test request through the production module-installation boundary.</summary>
        /// <param name="module">The module whose declaration must not run outside configuration.</param>
        public void InstallGodotModuleForTest(RecordingGodotModule module)
        {
            InstallGodotModule(module);
        }
    }

    /// <summary>
    ///     只记录成员调用名并返回空结果的上下文桩，用于验证装饰器的转发面。
    /// </summary>
    private sealed class RecordingArchitectureContext : IArchitectureContext
    {
        /// <summary>Records forwarded operations so the decorator can be checked without resolving real services.</summary>
        public List<string> Calls { get; } = [];

        /// <inheritdoc />
        public TService GetService<TService>() where TService : class
        {
            Calls.Add(nameof(GetService));
            return null!;
        }

        /// <inheritdoc />
        public IReadOnlyList<TService> GetServices<TService>() where TService : class
        {
            Calls.Add(nameof(GetServices));
            return null!;
        }

        /// <inheritdoc />
        public TSystem GetSystem<TSystem>() where TSystem : class, ISystem
        {
            Calls.Add(nameof(GetSystem));
            return null!;
        }

        /// <inheritdoc />
        public IReadOnlyList<TSystem> GetSystems<TSystem>() where TSystem : class, ISystem
        {
            Calls.Add(nameof(GetSystems));
            return null!;
        }

        /// <inheritdoc />
        public TModel GetModel<TModel>() where TModel : class, IModel
        {
            Calls.Add(nameof(GetModel));
            return null!;
        }

        /// <inheritdoc />
        public IReadOnlyList<TModel> GetModels<TModel>() where TModel : class, IModel
        {
            Calls.Add(nameof(GetModels));
            return null!;
        }

        /// <inheritdoc />
        public TUtility GetUtility<TUtility>() where TUtility : class, IUtility
        {
            Calls.Add(nameof(GetUtility));
            return null!;
        }

        /// <inheritdoc />
        public IReadOnlyList<TUtility> GetUtilities<TUtility>() where TUtility : class, IUtility
        {
            Calls.Add(nameof(GetUtilities));
            return null!;
        }

        /// <inheritdoc />
        public IReadOnlyList<TService> GetServicesByPriority<TService>() where TService : class
        {
            Calls.Add(nameof(GetServicesByPriority));
            return null!;
        }

        /// <inheritdoc />
        public IReadOnlyList<TSystem> GetSystemsByPriority<TSystem>() where TSystem : class, ISystem
        {
            Calls.Add(nameof(GetSystemsByPriority));
            return null!;
        }

        /// <inheritdoc />
        public IReadOnlyList<TModel> GetModelsByPriority<TModel>() where TModel : class, IModel
        {
            Calls.Add(nameof(GetModelsByPriority));
            return null!;
        }

        /// <inheritdoc />
        public IReadOnlyList<TUtility> GetUtilitiesByPriority<TUtility>() where TUtility : class, IUtility
        {
            Calls.Add(nameof(GetUtilitiesByPriority));
            return null!;
        }

        /// <inheritdoc />
        public void SendEvent<TEvent>() where TEvent : new()
        {
            Calls.Add(nameof(SendEvent));
        }

        /// <inheritdoc />
        public void SendEvent<TEvent>(TEvent e) where TEvent : class
        {
            Calls.Add(nameof(SendEvent));
        }

        /// <inheritdoc />
        public IUnRegister RegisterEvent<TEvent>(Action<TEvent> handler)
        {
            Calls.Add(nameof(RegisterEvent));
            return null!;
        }

        /// <inheritdoc />
        public void UnRegisterEvent<TEvent>(Action<TEvent> onEvent)
        {
            Calls.Add(nameof(UnRegisterEvent));
        }

        /// <inheritdoc />
        public IEnvironment GetEnvironment()
        {
            Calls.Add(nameof(GetEnvironment));
            return null!;
        }
    }

    /// <summary>Records declaration and attachment separately; native node access must remain unused in the managed fixture.</summary>
    private sealed class RecordingGodotModule : IGodotModule
    {
        /// <summary>Whether the module's configuration callback has run.</summary>
        public bool InstallCalled
        {
            get; private set;
        }

        /// <summary>Whether native host attachment has been requested.</summary>
        public bool AttachCalled
        {
            get; private set;
        }

        /// <summary>Counts attempted native-node access, which the declaration-only test requires to stay zero.</summary>
        public int NodeReadCount
        {
            get; private set;
        }

        /// <inheritdoc />
        public global::Godot.Node Node
        {
            get
            {
                NodeReadCount++;
                return null!;
            }
        }

        /// <inheritdoc />
        public void Install(IArchitecture architecture)
        {
            InstallCalled = true;
        }

        /// <inheritdoc />
        public void OnAttach(GFramework.Core.Architectures.Architecture architecture)
        {
            AttachCalled = true;
        }

        /// <inheritdoc />
        public void OnDetach()
        {
        }
    }

    /// <summary>Identifies the ordinary-service forwarding path without requiring a constructed service.</summary>
    private sealed class DummyService;

    /// <summary>Supplies the System contract for context-forwarding checks; lifecycle callbacks intentionally do no work.</summary>
    private sealed class DummySystem : ISystem
    {
        private IArchitectureContext? _context;

        /// <inheritdoc />
        public void SetContext(IArchitectureContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public IArchitectureContext GetContext()
        {
            return _context!;
        }

        /// <inheritdoc />
        public void OnArchitecturePhase(ArchitecturePhase phase)
        {
        }

        /// <inheritdoc />
        public void Initialize()
        {
        }

        /// <inheritdoc />
        public void Destroy()
        {
        }
    }

    /// <summary>Supplies the Model contract for context-forwarding checks without owning runtime resources.</summary>
    private sealed class DummyModel : IModel
    {
        private IArchitectureContext? _context;

        /// <inheritdoc />
        public void SetContext(IArchitectureContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public IArchitectureContext GetContext()
        {
            return _context!;
        }

        /// <inheritdoc />
        public void OnArchitecturePhase(ArchitecturePhase phase)
        {
        }

        /// <inheritdoc />
        public void Initialize()
        {
        }
    }

    /// <summary>Identifies Utility forwarding without lifecycle or disposal behavior.</summary>
    private sealed class DummyUtility : IUtility;

    /// <summary>Identifies the event forwarding paths without introducing a payload or handlers.</summary>
    private sealed class DummyEvent;
}
