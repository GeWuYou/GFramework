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
    /// <summary>
    ///     验证主线程同步 CQRS 保护启用时，请求与查询入口都会抛出异常，并给出对应的异步迁移指引。
    /// </summary>
    [Test]
    public void GodotArchitectureContext_ShouldThrow_ForSyncCqrsCalls_WhenGuardIsActive()
    {
        var context = new GodotArchitectureContext(new ArchitectureContext(new GFramework.Core.Ioc.MicrosoftDiContainer()), () => true);

        var requestException = Assert.Throws<InvalidOperationException>(() =>
            context.SendRequest(new TestRequest()));
        var queryException = Assert.Throws<InvalidOperationException>(() =>
            context.SendQuery(new TestLegacyQuery()));

        Assert.Multiple(() =>
        {
            Assert.That(requestException!.Message, Does.Contain("SendRequestAsync(...)"));
            Assert.That(requestException.Message, Does.Contain("SendAsync(...)"));
            Assert.That(queryException!.Message, Does.Contain("SendQueryAsync(...)"));
        });
    }

    /// <summary>
    ///     验证主线程同步 CQRS 保护关闭时，请求入口会继续委托到底层上下文。
    /// </summary>
    [Test]
    public void GodotArchitectureContext_ShouldForwardSyncCqrsCalls_WhenGuardIsInactive()
    {
        var context = new GodotArchitectureContext(new ForwardingArchitectureContext(), () => false);

        var result = context.SendRequest(new TestRequest());

        Assert.That(result, Is.EqualTo("ok"));
    }

    /// <summary>
    ///     验证未显式传入上下文时，架构默认会包装成带 Godot 主线程保护的上下文实现。
    /// </summary>
    [Test]
    public void AbstractArchitecture_ShouldUseGodotContextWrapper_ByDefault()
    {
        var architecture = new TestArchitecture();

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
        /// <inheritdoc />
        public TService GetService<TService>() where TService : class
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public IReadOnlyList<TService> GetServices<TService>() where TService : class
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public TSystem GetSystem<TSystem>() where TSystem : class, ISystem
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public IReadOnlyList<TSystem> GetSystems<TSystem>() where TSystem : class, ISystem
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public TModel GetModel<TModel>() where TModel : class, IModel
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public IReadOnlyList<TModel> GetModels<TModel>() where TModel : class, IModel
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public TUtility GetUtility<TUtility>() where TUtility : class, IUtility
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public IReadOnlyList<TUtility> GetUtilities<TUtility>() where TUtility : class, IUtility
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public IReadOnlyList<TService> GetServicesByPriority<TService>() where TService : class
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public IReadOnlyList<TSystem> GetSystemsByPriority<TSystem>() where TSystem : class, ISystem
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public IReadOnlyList<TModel> GetModelsByPriority<TModel>() where TModel : class, IModel
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public IReadOnlyList<TUtility> GetUtilitiesByPriority<TUtility>() where TUtility : class, IUtility
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public void SendCommand(ICommand command)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public TResult SendCommand<TResult>(ICommand<TResult> command)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public TResponse SendCommand<TResponse>(GFramework.Cqrs.Abstractions.Cqrs.Command.ICommand<TResponse> command)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public Task SendCommandAsync(IAsyncCommand command)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public ValueTask<TResponse> SendCommandAsync<TResponse>(
            GFramework.Cqrs.Abstractions.Cqrs.Command.ICommand<TResponse> command,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public Task<TResult> SendCommandAsync<TResult>(IAsyncCommand<TResult> command)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public TResult SendQuery<TResult>(IQuery<TResult> query)
        {
            return (TResult)(object)"ok";
        }

        /// <inheritdoc />
        public TResponse SendQuery<TResponse>(GFramework.Cqrs.Abstractions.Cqrs.Query.IQuery<TResponse> query)
        {
            return (TResponse)(object)"ok";
        }

        /// <inheritdoc />
        public Task<TResult> SendQueryAsync<TResult>(IAsyncQuery<TResult> query)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public ValueTask<TResponse> SendQueryAsync<TResponse>(
            GFramework.Cqrs.Abstractions.Cqrs.Query.IQuery<TResponse> query,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public void SendEvent<TEvent>() where TEvent : new()
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public void SendEvent<TEvent>(TEvent e) where TEvent : class
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public IUnRegister RegisterEvent<TEvent>(Action<TEvent> handler)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public void UnRegisterEvent<TEvent>(Action<TEvent> onEvent)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public IEnvironment GetEnvironment()
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public ValueTask<TResponse> SendRequestAsync<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public TResponse SendRequest<TResponse>(IRequest<TResponse> request)
        {
            return (TResponse)(object)"ok";
        }

        /// <inheritdoc />
        public ValueTask PublishAsync<TNotification>(
            TNotification notification,
            CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public ValueTask SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : IRequest<Unit>
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
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

    private sealed class TestLegacyQuery : IQuery<string>
    {
        private IArchitectureContext? _context;

        public void SetContext(IArchitectureContext context)
        {
            _context = context;
        }

        public IArchitectureContext GetContext()
        {
            return _context!;
        }

        public string Do()
        {
            return "ok";
        }
    }
}
