// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Command;
using GFramework.Core.Abstractions.Environment;
using GFramework.Core.Abstractions.Events;
using GFramework.Core.Abstractions.Model;
using GFramework.Core.Abstractions.Query;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Abstractions.Utility;
using GFramework.Core.Architectures;
using GFramework.Godot.Architectures;
using GFramework.Cqrs.Abstractions.Cqrs;
using ICommand = GFramework.Core.Abstractions.Command.ICommand;

namespace GFramework.Godot.Tests.Architectures;

/// <summary>
///     验证 Godot 架构在模块安装前会先检查锚点状态，避免未绑定场景树时留下半安装副作用。
/// </summary>
[TestFixture]
public sealed class AbstractArchitectureModuleInstallationTests
{
    [Test]
    public void GodotArchitectureContext_ShouldThrow_ForSyncCqrsCalls_WhenGuardIsActive()
    {
        var context = new GodotArchitectureContext(new ArchitectureContext(new GFramework.Core.Ioc.MicrosoftDiContainer()), () => true);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            context.SendRequest(new TestRequest()));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Does.Contain("SendAsync(...)"));
    }

    [Test]
    public void GodotArchitectureContext_ShouldForwardSyncCqrsCalls_WhenGuardIsInactive()
    {
        var context = new GodotArchitectureContext(new ForwardingArchitectureContext(), () => false);

        var result = context.SendRequest(new TestRequest());

        Assert.That(result, Is.EqualTo("ok"));
    }

    [Test]
    public void AbstractArchitecture_ShouldUseGodotContextWrapper_ByDefault()
    {
        var architecture = new TestArchitecture();

        architecture.Initialize();

        Assert.That(architecture.Context, Is.TypeOf<GodotArchitectureContext>());
    }

    /// <summary>
    ///     验证当锚点尚未初始化时，安装流程会直接失败，且不会执行模块安装逻辑。
    /// </summary>
    /// <returns>表示异步断言完成的任务。</returns>
    [Test]
    public async Task InstallGodotModuleAsync_ShouldThrowBeforeInvokingModuleInstall_WhenAnchorIsMissing()
    {
        var architecture = new TestArchitecture();
        var module = new RecordingGodotModule();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            architecture.InstallGodotModuleForTestAsync(module));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Is.EqualTo("Anchor not initialized"));
            Assert.That(module.InstallCalled, Is.False);
        });
    }

    private sealed class TestArchitecture : AbstractArchitecture
    {
        protected override void InstallModules()
        {
        }

        public Task InstallGodotModuleForTestAsync(RecordingGodotModule module)
        {
            return InstallGodotModule(module);
        }
    }

    private sealed class ForwardingArchitectureContext : IArchitectureContext
    {
        public TService GetService<TService>() where TService : class
        {
            throw new NotSupportedException();
        }

        public IReadOnlyList<TService> GetServices<TService>() where TService : class
        {
            throw new NotSupportedException();
        }

        public TSystem GetSystem<TSystem>() where TSystem : class, ISystem
        {
            throw new NotSupportedException();
        }

        public IReadOnlyList<TSystem> GetSystems<TSystem>() where TSystem : class, ISystem
        {
            throw new NotSupportedException();
        }

        public TModel GetModel<TModel>() where TModel : class, IModel
        {
            throw new NotSupportedException();
        }

        public IReadOnlyList<TModel> GetModels<TModel>() where TModel : class, IModel
        {
            throw new NotSupportedException();
        }

        public TUtility GetUtility<TUtility>() where TUtility : class, IUtility
        {
            throw new NotSupportedException();
        }

        public IReadOnlyList<TUtility> GetUtilities<TUtility>() where TUtility : class, IUtility
        {
            throw new NotSupportedException();
        }

        public IReadOnlyList<TService> GetServicesByPriority<TService>() where TService : class
        {
            throw new NotSupportedException();
        }

        public IReadOnlyList<TSystem> GetSystemsByPriority<TSystem>() where TSystem : class, ISystem
        {
            throw new NotSupportedException();
        }

        public IReadOnlyList<TModel> GetModelsByPriority<TModel>() where TModel : class, IModel
        {
            throw new NotSupportedException();
        }

        public IReadOnlyList<TUtility> GetUtilitiesByPriority<TUtility>() where TUtility : class, IUtility
        {
            throw new NotSupportedException();
        }

        public void SendCommand(ICommand command)
        {
            throw new NotSupportedException();
        }

        public TResult SendCommand<TResult>(ICommand<TResult> command)
        {
            throw new NotSupportedException();
        }

        public TResponse SendCommand<TResponse>(GFramework.Cqrs.Abstractions.Cqrs.Command.ICommand<TResponse> command)
        {
            throw new NotSupportedException();
        }

        public Task SendCommandAsync(IAsyncCommand command)
        {
            throw new NotSupportedException();
        }

        public ValueTask<TResponse> SendCommandAsync<TResponse>(
            GFramework.Cqrs.Abstractions.Cqrs.Command.ICommand<TResponse> command,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<TResult> SendCommandAsync<TResult>(IAsyncCommand<TResult> command)
        {
            throw new NotSupportedException();
        }

        public TResult SendQuery<TResult>(IQuery<TResult> query)
        {
            throw new NotSupportedException();
        }

        public TResponse SendQuery<TResponse>(GFramework.Cqrs.Abstractions.Cqrs.Query.IQuery<TResponse> query)
        {
            throw new NotSupportedException();
        }

        public Task<TResult> SendQueryAsync<TResult>(IAsyncQuery<TResult> query)
        {
            throw new NotSupportedException();
        }

        public ValueTask<TResponse> SendQueryAsync<TResponse>(
            GFramework.Cqrs.Abstractions.Cqrs.Query.IQuery<TResponse> query,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public void SendEvent<TEvent>() where TEvent : new()
        {
            throw new NotSupportedException();
        }

        public void SendEvent<TEvent>(TEvent e) where TEvent : class
        {
            throw new NotSupportedException();
        }

        public IUnRegister RegisterEvent<TEvent>(Action<TEvent> handler)
        {
            throw new NotSupportedException();
        }

        public void UnRegisterEvent<TEvent>(Action<TEvent> onEvent)
        {
            throw new NotSupportedException();
        }

        public IEnvironment GetEnvironment()
        {
            throw new NotSupportedException();
        }

        public ValueTask<TResponse> SendRequestAsync<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public TResponse SendRequest<TResponse>(IRequest<TResponse> request)
        {
            return (TResponse)(object)"ok";
        }

        public ValueTask PublishAsync<TNotification>(
            TNotification notification,
            CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            throw new NotSupportedException();
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : IRequest<Unit>
        {
            throw new NotSupportedException();
        }

        public ValueTask<TResponse> SendAsync<TResponse>(
            IRequest<TResponse> command,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class RecordingGodotModule : IGodotModule
    {
        public bool InstallCalled { get; private set; }

        public global::Godot.Node Node => null!;

        public void Install(IArchitecture architecture)
        {
            InstallCalled = true;
        }

        public void OnAttach(GFramework.Core.Architectures.Architecture architecture)
        {
        }

        public void OnDetach()
        {
        }
    }

    private sealed class TestRequest : IRequest<string>;
}
