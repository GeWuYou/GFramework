// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Architectures;
using GFramework.Core.Ioc;
using GFramework.Core.Logging;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     验证 generated request invoker provider 的 registrar 接线与 dispatcher 消费语义。
/// </summary>
[TestFixture]
[NonParallelizable]
internal sealed class CqrsGeneratedRequestInvokerProviderTests
{
    private ILoggerFactoryProvider? _previousLoggerFactoryProvider;

    /// <summary>
    ///     在每个用例前重置 registrar / dispatcher 的静态缓存，避免跨用例共享状态影响断言。
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _previousLoggerFactoryProvider = LoggerFactoryResolver.Provider;
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider();
        ClearRegistrarCaches();
        ClearDispatcherCaches();
    }

    /// <summary>
    ///     在每个用例后清理静态缓存。
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        LoggerFactoryResolver.Provider = _previousLoggerFactoryProvider ?? new ConsoleLoggerFactoryProvider();
        ClearRegistrarCaches();
        ClearDispatcherCaches();
    }

    /// <summary>
    ///     验证 registrar 激活 generated registry 后，会把 request invoker provider 注册到容器中。
    /// </summary>
    [Test]
    public void RegisterHandlers_Should_Register_Generated_Request_Invoker_Provider()
    {
        var generatedAssembly = CreateGeneratedRequestInvokerAssembly();
        var container = new MicrosoftDiContainer();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);

        var providers = container.GetAll<ICqrsRequestInvokerProvider>();

        Assert.That(
            providers.Select(static provider => provider.GetType()),
            Is.EqualTo([typeof(GeneratedRequestInvokerProviderRegistry)]));
    }

    /// <summary>
    ///     验证当实现类型隐藏、但 request handler interface 仍可直接表达时，
    ///     registrar 仍会把 generated request invoker provider 注册到容器中。
    /// </summary>
    [Test]
    public void RegisterHandlers_Should_Register_Generated_Request_Invoker_Provider_For_Hidden_Implementation()
    {
        var generatedAssembly = CreateHiddenImplementationGeneratedRequestInvokerAssembly();
        var container = new MicrosoftDiContainer();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);

        var providers = container.GetAll<ICqrsRequestInvokerProvider>();

        Assert.That(
            providers.Select(static provider => provider.GetType()),
            Is.EqualTo([typeof(HiddenImplementationGeneratedRequestInvokerProviderRegistry)]));
    }

    /// <summary>
    ///     验证 registrar 激活 generated registry 后，会把 stream invoker provider 注册到容器中。
    /// </summary>
    [Test]
    public void RegisterHandlers_Should_Register_Generated_Stream_Invoker_Provider()
    {
        var generatedAssembly = CreateGeneratedStreamInvokerAssembly();
        var container = new MicrosoftDiContainer();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);

        var providers = container.GetAll<ICqrsStreamInvokerProvider>();

        Assert.That(
            providers.Select(static provider => provider.GetType()),
            Is.EqualTo([typeof(GeneratedStreamInvokerProviderRegistry)]));
    }

    /// <summary>
    ///     验证当实现类型隐藏、但 stream handler interface 仍可直接表达时，
    ///     registrar 仍会把 generated stream invoker provider 注册到容器中。
    /// </summary>
    [Test]
    public void RegisterHandlers_Should_Register_Generated_Stream_Invoker_Provider_For_Hidden_Implementation()
    {
        var generatedAssembly = CreateHiddenImplementationGeneratedStreamInvokerAssembly();
        var container = new MicrosoftDiContainer();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);

        var providers = container.GetAll<ICqrsStreamInvokerProvider>();

        Assert.That(
            providers.Select(static provider => provider.GetType()),
            Is.EqualTo([typeof(HiddenImplementationGeneratedStreamInvokerProviderRegistry)]));
    }

    /// <summary>
    ///     验证 dispatcher 在首次创建 request binding 时，会优先消费 generated request invoker provider。
    /// </summary>
    [Test]
    public async Task SendAsync_Should_Use_Generated_Request_Invoker_When_Provider_Is_Registered()
    {
        var generatedAssembly = CreateGeneratedRequestInvokerAssembly();
        var container = new MicrosoftDiContainer();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new ArchitectureContext(container);
        var response = await context.SendRequestAsync(new GeneratedRequestInvokerRequest("payload"));
        Assert.That(response, Is.EqualTo("generated:payload"));
    }

    /// <summary>
    ///     验证当实现类型隐藏、但 request handler interface 仍可直接表达时，
    ///     dispatcher 仍会消费 generated request invoker descriptor。
    /// </summary>
    [Test]
    public async Task SendAsync_Should_Use_Generated_Request_Invoker_For_Hidden_Implementation_When_Provider_Is_Registered()
    {
        var generatedAssembly = CreateHiddenImplementationGeneratedRequestInvokerAssembly();
        var container = new MicrosoftDiContainer();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new ArchitectureContext(container);
        var response = await context.SendRequestAsync(
            new HiddenImplementationRequestInvokerContainer.VisibleRequest("payload"));
        Assert.That(response, Is.EqualTo("generated-hidden:payload"));
    }

    /// <summary>
    ///     验证 dispatcher 在首次创建 stream binding 时，会优先消费 generated stream invoker provider。
    /// </summary>
    [Test]
    public async Task CreateStream_Should_Use_Generated_Stream_Invoker_When_Provider_Is_Registered()
    {
        var generatedAssembly = CreateGeneratedStreamInvokerAssembly();
        var container = new MicrosoftDiContainer();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new ArchitectureContext(container);
        var results = await DrainAsync(context.CreateStream(new GeneratedStreamInvokerRequest(3)));
        Assert.That(results, Is.EqualTo([30, 31]));
    }

    /// <summary>
    ///     验证当实现类型隐藏、但 stream handler interface 仍可直接表达时，
    ///     dispatcher 仍会消费 generated stream invoker descriptor。
    /// </summary>
    [Test]
    public async Task CreateStream_Should_Use_Generated_Stream_Invoker_For_Hidden_Implementation_When_Provider_Is_Registered()
    {
        var generatedAssembly = CreateHiddenImplementationGeneratedStreamInvokerAssembly();
        var container = new MicrosoftDiContainer();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new ArchitectureContext(container);
        var results = await DrainAsync(
            context.CreateStream(new HiddenImplementationStreamInvokerContainer.VisibleStreamRequest(3)));
        Assert.That(results, Is.EqualTo([300, 301]));
    }

    /// <summary>
    ///     验证当 registry 只暴露 request invoker provider 接口、但不提供可枚举描述符契约时，
    ///     dispatcher 仍会回退到既有反射路径，而不是错误依赖未预热的 generated metadata。
    /// </summary>
    [Test]
    public async Task SendAsync_Should_Fall_Back_To_Runtime_Path_When_Request_Provider_Does_Not_Enumerate_Descriptors()
    {
        var generatedAssembly = CreateGeneratedAssembly(
            typeof(NonEnumeratingRequestInvokerProviderRegistry),
            "GFramework.Cqrs.Tests.Cqrs.NonEnumeratingRequestInvokerAssembly, Version=1.0.0.0");
        var container = new MicrosoftDiContainer();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new ArchitectureContext(container);
        var response = await context.SendRequestAsync(new GeneratedRequestInvokerRequest("payload")).ConfigureAwait(false);
        Assert.That(response, Is.EqualTo("runtime:payload"));
    }

    /// <summary>
    ///     验证当 registry 只暴露 stream invoker provider 接口、但不提供可枚举描述符契约时，
    ///     dispatcher 仍会回退到既有流式反射路径。
    /// </summary>
    [Test]
    public async Task CreateStream_Should_Fall_Back_To_Runtime_Path_When_Stream_Provider_Does_Not_Enumerate_Descriptors()
    {
        var generatedAssembly = CreateGeneratedAssembly(
            typeof(NonEnumeratingStreamInvokerProviderRegistry),
            "GFramework.Cqrs.Tests.Cqrs.NonEnumeratingStreamInvokerAssembly, Version=1.0.0.0");
        var container = new MicrosoftDiContainer();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new ArchitectureContext(container);
        var results = await DrainAsync(context.CreateStream(new GeneratedStreamInvokerRequest(3))).ConfigureAwait(false);
        Assert.That(results, Is.EqualTo([3, 4]));
    }

    /// <summary>
    ///     验证当 generated request invoker provider 暴露实例方法时，
    ///     registrar 会放弃该 generated registry 并回退到运行时反射路径。
    /// </summary>
    [Test]
    public async Task SendAsync_Should_Fall_Back_To_Runtime_Path_When_Generated_Request_Invoker_Is_Not_Static()
    {
        var generatedAssembly = CreateGeneratedAssembly(
            typeof(NonStaticRequestInvokerProviderRegistry),
            "GFramework.Cqrs.Tests.Cqrs.NonStaticRequestInvokerAssembly, Version=1.0.0.0");
        var container = new MicrosoftDiContainer();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new ArchitectureContext(container);
        var response = await context.SendRequestAsync(new GeneratedRequestInvokerRequest("payload")).ConfigureAwait(false);
        Assert.That(response, Is.EqualTo("runtime:payload"));
    }

    /// <summary>
    ///     验证当 generated request invoker provider 返回与 dispatcher 委托签名不兼容的方法时，
    ///     dispatcher 会显式抛出契约错误。
    /// </summary>
    [Test]
    public void SendAsync_Should_Throw_When_Generated_Request_Invoker_Is_Incompatible()
    {
        var generatedAssembly = CreateGeneratedAssembly(
            typeof(IncompatibleRequestInvokerProviderRegistry),
            "GFramework.Cqrs.Tests.Cqrs.IncompatibleRequestInvokerAssembly, Version=1.0.0.0");
        var container = new MicrosoftDiContainer();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new ArchitectureContext(container);
        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await context.SendRequestAsync(new GeneratedRequestInvokerRequest("payload")).ConfigureAwait(false));
        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Does.Contain("incompatible invoker"));
    }

    /// <summary>
    ///     验证当 generated stream invoker provider 暴露实例方法时，
    ///     registrar 会放弃该 generated registry 并回退到运行时反射路径。
    /// </summary>
    [Test]
    public async Task CreateStream_Should_Fall_Back_To_Runtime_Path_When_Generated_Stream_Invoker_Is_Not_Static()
    {
        var generatedAssembly = CreateGeneratedAssembly(
            typeof(NonStaticStreamInvokerProviderRegistry),
            "GFramework.Cqrs.Tests.Cqrs.NonStaticStreamInvokerAssembly, Version=1.0.0.0");
        var container = new MicrosoftDiContainer();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new ArchitectureContext(container);
        var results = await DrainAsync(context.CreateStream(new GeneratedStreamInvokerRequest(3))).ConfigureAwait(false);
        Assert.That(results, Is.EqualTo([3, 4]));
    }

    /// <summary>
    ///     验证当 generated stream invoker provider 返回与 dispatcher 委托签名不兼容的方法时，
    ///     dispatcher 会显式抛出契约错误。
    /// </summary>
    [Test]
    public void CreateStream_Should_Throw_When_Generated_Stream_Invoker_Is_Incompatible()
    {
        var generatedAssembly = CreateGeneratedAssembly(
            typeof(IncompatibleStreamInvokerProviderRegistry),
            "GFramework.Cqrs.Tests.Cqrs.IncompatibleStreamInvokerAssembly, Version=1.0.0.0");
        var container = new MicrosoftDiContainer();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new ArchitectureContext(container);
        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await DrainAsync(context.CreateStream(new GeneratedStreamInvokerRequest(3))).ConfigureAwait(false));
        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Does.Contain("incompatible invoker"));
    }

    /// <summary>
    ///     验证当 generated request invoker provider 实现枚举契约、但返回空描述符集合时，
    ///     dispatcher 仍会回退到既有反射路径。
    /// </summary>
    [Test]
    public async Task SendAsync_Should_Fall_Back_To_Runtime_Path_When_Request_Descriptor_Enumeration_Is_Empty()
    {
        var generatedAssembly = CreateGeneratedAssembly(
            typeof(EmptyEnumeratingRequestInvokerProviderRegistry),
            "GFramework.Cqrs.Tests.Cqrs.EmptyEnumeratingRequestInvokerAssembly, Version=1.0.0.0");
        var container = new MicrosoftDiContainer();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new ArchitectureContext(container);
        var response = await context.SendRequestAsync(new GeneratedRequestInvokerRequest("payload")).ConfigureAwait(false);
        Assert.That(response, Is.EqualTo("runtime:payload"));
    }

    /// <summary>
    ///     验证当 generated stream invoker provider 实现枚举契约、但返回空描述符集合时，
    ///     dispatcher 仍会回退到既有流式反射路径。
    /// </summary>
    [Test]
    public async Task CreateStream_Should_Fall_Back_To_Runtime_Path_When_Stream_Descriptor_Enumeration_Is_Empty()
    {
        var generatedAssembly = CreateGeneratedAssembly(
            typeof(EmptyEnumeratingStreamInvokerProviderRegistry),
            "GFramework.Cqrs.Tests.Cqrs.EmptyEnumeratingStreamInvokerAssembly, Version=1.0.0.0");
        var container = new MicrosoftDiContainer();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new ArchitectureContext(container);
        var results = await DrainAsync(context.CreateStream(new GeneratedStreamInvokerRequest(3))).ConfigureAwait(false);
        Assert.That(results, Is.EqualTo([3, 4]));
    }

    /// <summary>
    ///     模拟返回实例 request invoker 方法的 generated registry。
    /// </summary>
    private sealed class NonStaticRequestInvokerProviderRegistry :
        ICqrsHandlerRegistry,
        ICqrsRequestInvokerProvider,
        IEnumeratesCqrsRequestInvokerDescriptors
    {
        /// <inheritdoc />
        public void Register(IServiceCollection services, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(logger);

            services.AddTransient(
                typeof(IRequestHandler<GeneratedRequestInvokerRequest, string>),
                typeof(GeneratedRequestInvokerRequestHandler));
        }

        /// <inheritdoc />
        public bool TryGetDescriptor(
            Type requestType,
            Type responseType,
            out CqrsRequestInvokerDescriptor? descriptor)
        {
            ArgumentNullException.ThrowIfNull(requestType);
            ArgumentNullException.ThrowIfNull(responseType);

            if (requestType == typeof(GeneratedRequestInvokerRequest) && responseType == typeof(string))
            {
                descriptor = new CqrsRequestInvokerDescriptor(
                    typeof(IRequestHandler<GeneratedRequestInvokerRequest, string>),
                    typeof(NonStaticRequestInvokerProviderRegistry).GetMethod(
                        nameof(InvokeGenerated),
                        BindingFlags.NonPublic | BindingFlags.Instance)!);
                return true;
            }

            descriptor = null;
            return false;
        }

        /// <inheritdoc />
        public IReadOnlyList<CqrsRequestInvokerDescriptorEntry> GetDescriptors()
        {
            return
            [
                new CqrsRequestInvokerDescriptorEntry(
                    typeof(GeneratedRequestInvokerRequest),
                    typeof(string),
                    new CqrsRequestInvokerDescriptor(
                        typeof(IRequestHandler<GeneratedRequestInvokerRequest, string>),
                        typeof(NonStaticRequestInvokerProviderRegistry).GetMethod(
                            nameof(InvokeGenerated),
                            BindingFlags.NonPublic | BindingFlags.Instance)!))
            ];
        }

        private ValueTask<string> InvokeGenerated(object handler, object request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(string.Empty);
        }
    }

    /// <summary>
    ///     模拟返回不兼容 request invoker 方法的 generated registry。
    /// </summary>
    private sealed class IncompatibleRequestInvokerProviderRegistry :
        ICqrsHandlerRegistry,
        ICqrsRequestInvokerProvider,
        IEnumeratesCqrsRequestInvokerDescriptors
    {
        private static readonly CqrsRequestInvokerDescriptorEntry DescriptorEntry = new(
            typeof(GeneratedRequestInvokerRequest),
            typeof(string),
            new CqrsRequestInvokerDescriptor(
                typeof(IRequestHandler<GeneratedRequestInvokerRequest, string>),
                typeof(IncompatibleRequestInvokerProviderRegistry).GetMethod(
                    nameof(InvokeGenerated),
                    BindingFlags.NonPublic | BindingFlags.Static)!));

        /// <inheritdoc />
        public void Register(IServiceCollection services, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(logger);

            services.AddTransient(
                typeof(IRequestHandler<GeneratedRequestInvokerRequest, string>),
                typeof(GeneratedRequestInvokerRequestHandler));
        }

        /// <inheritdoc />
        public bool TryGetDescriptor(
            Type requestType,
            Type responseType,
            out CqrsRequestInvokerDescriptor? descriptor)
        {
            ArgumentNullException.ThrowIfNull(requestType);
            ArgumentNullException.ThrowIfNull(responseType);

            if (requestType == typeof(GeneratedRequestInvokerRequest) && responseType == typeof(string))
            {
                descriptor = DescriptorEntry.Descriptor;
                return true;
            }

            descriptor = null;
            return false;
        }

        /// <inheritdoc />
        public IReadOnlyList<CqrsRequestInvokerDescriptorEntry> GetDescriptors()
        {
            return [DescriptorEntry];
        }

        private static string InvokeGenerated(object handler, object request)
        {
            return string.Empty;
        }
    }

    /// <summary>
    ///     模拟返回实例 stream invoker 方法的 generated registry。
    /// </summary>
    private sealed class NonStaticStreamInvokerProviderRegistry :
        ICqrsHandlerRegistry,
        ICqrsStreamInvokerProvider,
        IEnumeratesCqrsStreamInvokerDescriptors
    {
        /// <inheritdoc />
        public void Register(IServiceCollection services, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(logger);

            services.AddTransient(
                typeof(IStreamRequestHandler<GeneratedStreamInvokerRequest, int>),
                typeof(GeneratedStreamInvokerRequestHandler));
        }

        /// <inheritdoc />
        public bool TryGetDescriptor(
            Type requestType,
            Type responseType,
            out CqrsStreamInvokerDescriptor? descriptor)
        {
            ArgumentNullException.ThrowIfNull(requestType);
            ArgumentNullException.ThrowIfNull(responseType);

            if (requestType == typeof(GeneratedStreamInvokerRequest) && responseType == typeof(int))
            {
                descriptor = new CqrsStreamInvokerDescriptor(
                    typeof(IStreamRequestHandler<GeneratedStreamInvokerRequest, int>),
                    typeof(NonStaticStreamInvokerProviderRegistry).GetMethod(
                        nameof(InvokeGenerated),
                        BindingFlags.NonPublic | BindingFlags.Instance)!);
                return true;
            }

            descriptor = null;
            return false;
        }

        /// <inheritdoc />
        public IReadOnlyList<CqrsStreamInvokerDescriptorEntry> GetDescriptors()
        {
            return
            [
                new CqrsStreamInvokerDescriptorEntry(
                    typeof(GeneratedStreamInvokerRequest),
                    typeof(int),
                    new CqrsStreamInvokerDescriptor(
                        typeof(IStreamRequestHandler<GeneratedStreamInvokerRequest, int>),
                        typeof(NonStaticStreamInvokerProviderRegistry).GetMethod(
                            nameof(InvokeGenerated),
                            BindingFlags.NonPublic | BindingFlags.Instance)!))
            ];
        }

        private object InvokeGenerated(object handler, object request, CancellationToken cancellationToken)
        {
            return Array.Empty<int>().ToAsyncEnumerable();
        }
    }

    /// <summary>
    ///     模拟返回不兼容 stream invoker 方法的 generated registry。
    /// </summary>
    private sealed class IncompatibleStreamInvokerProviderRegistry :
        ICqrsHandlerRegistry,
        ICqrsStreamInvokerProvider,
        IEnumeratesCqrsStreamInvokerDescriptors
    {
        private static readonly CqrsStreamInvokerDescriptorEntry DescriptorEntry = new(
            typeof(GeneratedStreamInvokerRequest),
            typeof(int),
            new CqrsStreamInvokerDescriptor(
                typeof(IStreamRequestHandler<GeneratedStreamInvokerRequest, int>),
                typeof(IncompatibleStreamInvokerProviderRegistry).GetMethod(
                    nameof(InvokeGenerated),
                    BindingFlags.NonPublic | BindingFlags.Static)!));

        /// <inheritdoc />
        public void Register(IServiceCollection services, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(logger);

            services.AddTransient(
                typeof(IStreamRequestHandler<GeneratedStreamInvokerRequest, int>),
                typeof(GeneratedStreamInvokerRequestHandler));
        }

        /// <inheritdoc />
        public bool TryGetDescriptor(
            Type requestType,
            Type responseType,
            out CqrsStreamInvokerDescriptor? descriptor)
        {
            ArgumentNullException.ThrowIfNull(requestType);
            ArgumentNullException.ThrowIfNull(responseType);

            if (requestType == typeof(GeneratedStreamInvokerRequest) && responseType == typeof(int))
            {
                descriptor = DescriptorEntry.Descriptor;
                return true;
            }

            descriptor = null;
            return false;
        }

        /// <inheritdoc />
        public IReadOnlyList<CqrsStreamInvokerDescriptorEntry> GetDescriptors()
        {
            return [DescriptorEntry];
        }

        private static object InvokeGenerated(object handler, object request)
        {
            return Array.Empty<int>().ToAsyncEnumerable();
        }
    }

    /// <summary>
    ///     模拟只暴露 request provider 接口、但不暴露描述符枚举契约的 generated registry。
    /// </summary>
    private sealed class NonEnumeratingRequestInvokerProviderRegistry :
        ICqrsHandlerRegistry,
        ICqrsRequestInvokerProvider
    {
        private static readonly CqrsRequestInvokerDescriptor Descriptor = new(
            typeof(IRequestHandler<GeneratedRequestInvokerRequest, string>),
            typeof(GeneratedRequestInvokerProviderRegistry).GetMethod(
                "InvokeGenerated",
                BindingFlags.NonPublic | BindingFlags.Static)!);

        /// <inheritdoc />
        public void Register(IServiceCollection services, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(logger);

            services.AddTransient(
                typeof(IRequestHandler<GeneratedRequestInvokerRequest, string>),
                typeof(GeneratedRequestInvokerRequestHandler));
        }

        /// <inheritdoc />
        public bool TryGetDescriptor(
            Type requestType,
            Type responseType,
            out CqrsRequestInvokerDescriptor? descriptor)
        {
            ArgumentNullException.ThrowIfNull(requestType);
            ArgumentNullException.ThrowIfNull(responseType);

            if (requestType == typeof(GeneratedRequestInvokerRequest) && responseType == typeof(string))
            {
                descriptor = Descriptor;
                return true;
            }

            descriptor = null;
            return false;
        }
    }

    /// <summary>
    ///     模拟只暴露 stream provider 接口、但不暴露描述符枚举契约的 generated registry。
    /// </summary>
    private sealed class NonEnumeratingStreamInvokerProviderRegistry :
        ICqrsHandlerRegistry,
        ICqrsStreamInvokerProvider
    {
        private static readonly CqrsStreamInvokerDescriptor Descriptor = new(
            typeof(IStreamRequestHandler<GeneratedStreamInvokerRequest, int>),
            typeof(GeneratedStreamInvokerProviderRegistry).GetMethod(
                "InvokeGenerated",
                BindingFlags.NonPublic | BindingFlags.Static)!);

        /// <inheritdoc />
        public void Register(IServiceCollection services, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(logger);

            services.AddTransient(
                typeof(IStreamRequestHandler<GeneratedStreamInvokerRequest, int>),
                typeof(GeneratedStreamInvokerRequestHandler));
        }

        /// <inheritdoc />
        public bool TryGetDescriptor(
            Type requestType,
            Type responseType,
            out CqrsStreamInvokerDescriptor? descriptor)
        {
            ArgumentNullException.ThrowIfNull(requestType);
            ArgumentNullException.ThrowIfNull(responseType);

            if (requestType == typeof(GeneratedStreamInvokerRequest) && responseType == typeof(int))
            {
                descriptor = Descriptor;
                return true;
            }

            descriptor = null;
            return false;
        }
    }

    /// <summary>
    ///     模拟实现 request descriptor 枚举契约、但当前不暴露任何 descriptor 的 generated registry。
    /// </summary>
    private sealed class EmptyEnumeratingRequestInvokerProviderRegistry :
        ICqrsHandlerRegistry,
        ICqrsRequestInvokerProvider,
        IEnumeratesCqrsRequestInvokerDescriptors
    {
        /// <inheritdoc />
        public void Register(IServiceCollection services, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(logger);

            services.AddTransient(
                typeof(IRequestHandler<GeneratedRequestInvokerRequest, string>),
                typeof(GeneratedRequestInvokerRequestHandler));
        }

        /// <inheritdoc />
        public bool TryGetDescriptor(
            Type requestType,
            Type responseType,
            out CqrsRequestInvokerDescriptor? descriptor)
        {
            ArgumentNullException.ThrowIfNull(requestType);
            ArgumentNullException.ThrowIfNull(responseType);

            descriptor = null;
            return false;
        }

        /// <inheritdoc />
        public IReadOnlyList<CqrsRequestInvokerDescriptorEntry> GetDescriptors()
        {
            return Array.Empty<CqrsRequestInvokerDescriptorEntry>();
        }
    }

    /// <summary>
    ///     模拟实现 stream descriptor 枚举契约、但当前不暴露任何 descriptor 的 generated registry。
    /// </summary>
    private sealed class EmptyEnumeratingStreamInvokerProviderRegistry :
        ICqrsHandlerRegistry,
        ICqrsStreamInvokerProvider,
        IEnumeratesCqrsStreamInvokerDescriptors
    {
        /// <inheritdoc />
        public void Register(IServiceCollection services, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(logger);

            services.AddTransient(
                typeof(IStreamRequestHandler<GeneratedStreamInvokerRequest, int>),
                typeof(GeneratedStreamInvokerRequestHandler));
        }

        /// <inheritdoc />
        public bool TryGetDescriptor(
            Type requestType,
            Type responseType,
            out CqrsStreamInvokerDescriptor? descriptor)
        {
            ArgumentNullException.ThrowIfNull(requestType);
            ArgumentNullException.ThrowIfNull(responseType);

            descriptor = null;
            return false;
        }

        /// <inheritdoc />
        public IReadOnlyList<CqrsStreamInvokerDescriptorEntry> GetDescriptors()
        {
            return Array.Empty<CqrsStreamInvokerDescriptorEntry>();
        }
    }

    /// <summary>
    ///     创建带有 generated request invoker registry 元数据的程序集替身。
    /// </summary>
    private static Mock<Assembly> CreateGeneratedRequestInvokerAssembly()
    {
        var generatedAssembly = new Mock<Assembly>();
        generatedAssembly
            .SetupGet(static assembly => assembly.FullName)
            .Returns("GFramework.Cqrs.Tests.Cqrs.GeneratedRequestInvokerAssembly, Version=1.0.0.0");
        generatedAssembly
            .Setup(static assembly => assembly.GetCustomAttributes(typeof(CqrsHandlerRegistryAttribute), false))
            .Returns([new CqrsHandlerRegistryAttribute(typeof(GeneratedRequestInvokerProviderRegistry))]);
        return generatedAssembly;
    }

    /// <summary>
    ///     创建带有 generated stream invoker registry 元数据的程序集替身。
    /// </summary>
    private static Mock<Assembly> CreateGeneratedStreamInvokerAssembly()
    {
        var generatedAssembly = new Mock<Assembly>();
        generatedAssembly
            .SetupGet(static assembly => assembly.FullName)
            .Returns("GFramework.Cqrs.Tests.Cqrs.GeneratedStreamInvokerAssembly, Version=1.0.0.0");
        generatedAssembly
            .Setup(static assembly => assembly.GetCustomAttributes(typeof(CqrsHandlerRegistryAttribute), false))
            .Returns([new CqrsHandlerRegistryAttribute(typeof(GeneratedStreamInvokerProviderRegistry))]);
        return generatedAssembly;
    }

    /// <summary>
    ///     创建带有指定 generated registry 元数据的程序集替身。
    /// </summary>
    /// <param name="registryType">测试 registry 类型。</param>
    /// <param name="assemblyFullName">模拟程序集全名。</param>
    /// <returns>可用于 registrar 注册流程的程序集替身。</returns>
    private static Mock<Assembly> CreateGeneratedAssembly(Type registryType, string assemblyFullName)
    {
        ArgumentNullException.ThrowIfNull(registryType);
        ArgumentException.ThrowIfNullOrWhiteSpace(assemblyFullName);

        var generatedAssembly = new Mock<Assembly>();
        generatedAssembly
            .SetupGet(static assembly => assembly.FullName)
            .Returns(assemblyFullName);
        generatedAssembly
            .Setup(static assembly => assembly.GetCustomAttributes(typeof(CqrsHandlerRegistryAttribute), false))
            .Returns([new CqrsHandlerRegistryAttribute(registryType)]);
        return generatedAssembly;
    }

    /// <summary>
    ///     创建带有 hidden implementation request invoker registry 元数据的程序集替身。
    /// </summary>
    private static Mock<Assembly> CreateHiddenImplementationGeneratedRequestInvokerAssembly()
    {
        var generatedAssembly = new Mock<Assembly>();
        generatedAssembly
            .SetupGet(static assembly => assembly.FullName)
            .Returns("GFramework.Cqrs.Tests.Cqrs.HiddenGeneratedRequestInvokerAssembly, Version=1.0.0.0");
        generatedAssembly
            .Setup(static assembly => assembly.GetCustomAttributes(typeof(CqrsHandlerRegistryAttribute), false))
            .Returns([new CqrsHandlerRegistryAttribute(typeof(HiddenImplementationGeneratedRequestInvokerProviderRegistry))]);
        return generatedAssembly;
    }

    /// <summary>
    ///     创建带有 hidden implementation stream invoker registry 元数据的程序集替身。
    /// </summary>
    private static Mock<Assembly> CreateHiddenImplementationGeneratedStreamInvokerAssembly()
    {
        var generatedAssembly = new Mock<Assembly>();
        generatedAssembly
            .SetupGet(static assembly => assembly.FullName)
            .Returns("GFramework.Cqrs.Tests.Cqrs.HiddenGeneratedStreamInvokerAssembly, Version=1.0.0.0");
        generatedAssembly
            .Setup(static assembly => assembly.GetCustomAttributes(typeof(CqrsHandlerRegistryAttribute), false))
            .Returns([new CqrsHandlerRegistryAttribute(typeof(HiddenImplementationGeneratedStreamInvokerProviderRegistry))]);
        return generatedAssembly;
    }

    /// <summary>
    ///     清空 registrar 静态缓存。
    /// </summary>
    private static void ClearRegistrarCaches()
    {
        ClearCache(GetRegistrarCacheField("AssemblyMetadataCache"));
        ClearCache(GetRegistrarCacheField("RegistryActivationMetadataCache"));
        ClearCache(GetRegistrarCacheField("LoadableTypesCache"));
        ClearCache(GetRegistrarCacheField("SupportedHandlerInterfacesCache"));
    }

    /// <summary>
    ///     清空 dispatcher 静态缓存。
    /// </summary>
    private static void ClearDispatcherCaches()
    {
        ClearCache(GetDispatcherCacheField("NotificationDispatchBindings"));
        ClearCache(GetDispatcherCacheField("RequestDispatchBindings"));
        ClearCache(GetDispatcherCacheField("StreamDispatchBindings"));
        ClearCache(GetDispatcherCacheField("GeneratedRequestInvokers"));
        ClearCache(GetDispatcherCacheField("GeneratedStreamInvokers"));
    }

    /// <summary>
    ///     通过反射读取 registrar 的静态缓存字段。
    /// </summary>
    private static object GetRegistrarCacheField(string fieldName)
    {
        var field = typeof(CqrsReflectionFallbackAttribute).Assembly
            .GetType("GFramework.Cqrs.Internal.CqrsHandlerRegistrar", throwOnError: true)!
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(field, Is.Not.Null, $"Missing registrar cache field {fieldName}.");
        return field!.GetValue(null)
               ?? throw new InvalidOperationException($"Registrar cache field {fieldName} returned null.");
    }

    /// <summary>
    ///     通过反射读取 dispatcher 的静态缓存字段。
    /// </summary>
    private static object GetDispatcherCacheField(string fieldName)
    {
        var field = typeof(CqrsReflectionFallbackAttribute).Assembly
            .GetType("GFramework.Cqrs.Internal.CqrsDispatcher", throwOnError: true)!
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(field, Is.Not.Null, $"Missing dispatcher cache field {fieldName}.");
        return field!.GetValue(null)
               ?? throw new InvalidOperationException($"Dispatcher cache field {fieldName} returned null.");
    }

    /// <summary>
    ///     清空目标缓存实例。
    /// </summary>
    private static void ClearCache(object cache)
    {
        _ = cache.GetType()
            .GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .Invoke(cache, Array.Empty<object>());
    }

    /// <summary>
    ///     枚举并收集当前异步流中的全部元素，便于断言 generated stream invoker 的输出。
    /// </summary>
    /// <typeparam name="TItem">流元素类型。</typeparam>
    /// <param name="stream">待消耗的异步流。</param>
    /// <returns>按产出顺序收集得到的元素列表。</returns>
    private static async Task<IReadOnlyList<TItem>> DrainAsync<TItem>(IAsyncEnumerable<TItem> stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var items = new List<TItem>();
        await foreach (var item in stream.ConfigureAwait(false))
        {
            items.Add(item);
        }

        return items;
    }
}
